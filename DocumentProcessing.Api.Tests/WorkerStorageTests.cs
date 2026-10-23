using System.Text;
using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentProcessing.Api.Tests;

public class WorkerStorageTests
{
    // Helper
    private static async Task AddPendingJobAsync(
        AppDbContext context,
        string storagePath)
    {
        var userId = await TestDatabase.CreateUserAsync(context, $"worker-storage-{Guid.NewGuid():N}@example.com");

        var document = new Document
        {
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSize = 3,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        context.ProcessingJobs.Add(new ProcessingJob
        {
            DocumentId = document.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    // Tests
    [Fact]
    public async Task ProcessClaimedJob_AsksStorageForStoragePath_AndGivesBytesToReader()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(context, "stored-name.pdf");

        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        var storage = new RecordingFileStorage(
            Encoding.UTF8.GetBytes("Hello from storage"));

        // Act
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            storage,
            new ReadTextFileProcessor(),
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var requested = Assert.Single(storage.RequestedPaths);
        Assert.Equal("stored-name.pdf", requested);

        var result = await context.ProcessingResults.AsNoTracking().SingleAsync();
        Assert.Equal("Hello from storage", result.ExtractedText);

        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();
        Assert.Equal("Completed", job.Status);
    }

    [Fact]
    public async Task ProcessClaimedJob_WhenDownloadFails_MovesJobBackToPending()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(context, "stored-name.pdf");

        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        // Act
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            new FailingFileStorage(),
            new ReadTextFileProcessor(),
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Pending", job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal("download failed", job.ErrorMessage);
        Assert.Null(job.LeaseToken);

        Assert.Empty(context.ProcessingResults.AsNoTracking());
    }
}