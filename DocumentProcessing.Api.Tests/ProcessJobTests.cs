using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class ProcessJobTests
{
    // Helper
    private static async Task AddPendingJobAsync(
        AppDbContext context,
        int retryCount = 0,
        int maxRetryCount = 3)
    {
        var document = new Document
        {
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSize = 3,
            StoragePath = "fake/invoice.pdf",
            CreatedAt = DateTime.UtcNow
        };

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        context.ProcessingJobs.Add(new ProcessingJob
        {
            DocumentId = document.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            RetryCount = retryCount,
            MaxRetryCount = maxRetryCount
        });

        await context.SaveChangesAsync();
    }

    // Test
    [Fact]
    public async Task ProcessClaimedJob_WhenFileIsRead_CompletesJobAndSavesText()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(context);

        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        // Act
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            new FakeFileProcessor(),
            claimed,
            CancellationToken.None);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Completed", job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Null(job.ErrorMessage);
        Assert.Null(job.LeaseToken);

        var result = await context.ProcessingResults.AsNoTracking().SingleAsync();

        Assert.Equal(job.DocumentId, result.DocumentId);
        Assert.Equal("Hello from fake PDF", result.ExtractedText);
    }

    [Fact]
    public async Task ProcessClaimedJob_WhenFileFails_MovesJobBackToPending()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(context, retryCount: 0, maxRetryCount: 3);

        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        // Act
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            new FailingFileProcessor(),
            claimed,
            CancellationToken.None);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Pending", job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal("PDF is corrupt", job.ErrorMessage);
        Assert.True(job.NextRetryAt > DateTime.UtcNow);
        Assert.Null(job.LeaseToken);

        Assert.Empty(context.ProcessingResults.AsNoTracking());
    }

    [Fact]
    public async Task ProcessClaimedJob_WhenRetriesAreUsedUp_MarksJobFailed()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(context, retryCount: 3, maxRetryCount: 3);

        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        // Act
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            new FailingFileProcessor(),
            claimed,
            CancellationToken.None);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Failed", job.Status);
        Assert.Equal(4, job.RetryCount);
        Assert.Equal("PDF is corrupt", job.ErrorMessage);
        Assert.Null(job.NextRetryAt);
        Assert.Null(job.LeaseToken);
    }

}