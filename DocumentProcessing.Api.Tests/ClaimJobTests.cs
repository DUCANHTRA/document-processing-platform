using System.Security.Claims;
using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class ClaimJobTests
{
    // Helper Class
    // Create Document
    // Create new Processing Job
    private static async Task<ProcessingJob> AddPendingJobAsync(AppDbContext context)
    {
        var document = new Document
        {
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSize = 3,
            StoragePath = "faje/invoice.pdf",
            CreatedAt = DateTime.UtcNow
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var job = new ProcessingJob
        {
            DocumentId = document.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.ProcessingJobs.Add(job);
        await context.SaveChangesAsync();

        return job;
    }

    private static async Task<ProcessingJob?[]> ClaimAtTheSameTimeAsync()
    {
        await using var contextA = TestDatabase.CreateContext();
        await using var contextB = TestDatabase.CreateContext();

        var claimA = Task.Run(() => 
            DocumentProcessingWorker.ClaimNextJobAsync(contextA, CancellationToken.None));
        var claimB = Task.Run(() => 
            DocumentProcessingWorker.ClaimNextJobAsync(contextB, CancellationToken.None));

        return await Task.WhenAll(claimA, claimB);
    }

    [Fact]
    public async Task ClaimNextJob_ClaimsPendingJob()
    {
        /* 
        ARRANGE
        - Add empty databse
        - Add 1 pending job
        */
        await using var context = await TestDatabase.CreateCleanContextAsync();
        var pendingJob = await AddPendingJobAsync(context);

        /* 
        ACT
        - Add empty databse
        - Add 1 pending job
        */
        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        //ASSERT
        Assert.NotNull(claimed);
        Assert.Equal(pendingJob.Id, claimed.Id);
        Assert.Equal("Processing", claimed.Status);
        Assert.NotNull(claimed.LeaseToken);
        Assert.NotNull(claimed.LeaseExpiresAt);

        var saved = await context.ProcessingJobs.AsNoTracking().SingleAsync();
        Assert.Equal("Processing", saved.Status);
    }

    [Fact]
    public async Task ClaimNextJob_WhenNoPendingJobs_ReturnsNull()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();

        // Act
        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        // Assert
        Assert.Null(claimed);
    }

    [Fact]
    public async Task TwoWorkers_OneJob_OnlyOneWorkerGetsIt()
    {
        for 
        (var round = 0; round < 20; round++)
        {
            // Arrange
            // 1. Create clean database connection
            // 2. Add document and Processing Job
            await using var setup = await TestDatabase.CreateCleanContextAsync();
            await AddPendingJobAsync(setup);

            // Act
            // 
            var results = await ClaimAtTheSameTimeAsync();

            // Assert
            var gotAJob = results.Count(job => job is not null);
            Assert.Equal(1, gotAJob);
        }
    }

    [Fact]
    public async Task TwoWorkers_TwoJobs_EachWorkerGetsADifferentJob()
    {
        for (var round = 0; round < 20; round++)
        {
            // Arrange
            await using var setup = await TestDatabase.CreateCleanContextAsync();
            await AddPendingJobAsync(setup);
            await AddPendingJobAsync(setup);

            // Act
            var results = await ClaimAtTheSameTimeAsync();

            // Assert
            Assert.NotNull(results[0]);
            Assert.NotNull(results[1]);
            Assert.NotEqual(results[0]!.Id, results[1]!.Id);
        }
    }

}