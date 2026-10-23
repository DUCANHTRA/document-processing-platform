using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentProcessing.Api.Tests;

public class ProcessJobTests
{
    // Helper
    private static async Task AddPendingJobAsync(
        AppDbContext context,
        int retryCount = 0,
        int maxRetryCount = 3)
    {
        var userId = await TestDatabase.CreateUserAsync(context, $"process-job-{Guid.NewGuid():N}@example.com");

        var document = new Document
        {
            FileName = "invoice.pdf",
            ContentType = "application/pdf",
            FileSize = 3,
            StoragePath = "fake/invoice.pdf",
            CreatedAt = DateTime.UtcNow,
            UserId = userId
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

    private static async Task TakeOverJobAsync(int jobId, Guid newToken)
    {
        //  Create second context
        await using var otherContext = TestDatabase.CreateContext();

        // Change job's lease token in the database
        await otherContext.ProcessingJobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(jobId => jobId.LeaseToken, newToken));
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
            new FakeFileStorage(),
            new FakeFileProcessor(),
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

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
            new FakeFileStorage(),
            new FailingFileProcessor(),
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

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
            new FakeFileStorage(),
            new FailingFileProcessor(),
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Failed", job.Status);
        Assert.Equal(4, job.RetryCount);
        Assert.Equal("PDF is corrupt", job.ErrorMessage);
        Assert.Null(job.NextRetryAt);
        Assert.Null(job.LeaseToken);
    }

    [Fact]
    public async Task ProcessClaimedJob_WhenLeaseWasLost_DoesNotCompleteJobOrSaveResult()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        
        // 1. Add a pending job 
        await AddPendingJobAsync(context);

        // 2. Claim pending job --> token AAA
        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        var jobId = claimed.Id;
        // Create a new token
        var newToken = Guid.NewGuid();

        // 3. Create a slow file reader
        // Slow file reader reading
        // Job changes owner
        var slowReader = new CallbackFileProcessor(async () =>
        {
            await TakeOverJobAsync(jobId, newToken);
            return "Text from the slow worker";
        });

        // Act
        // 4. Worker A tries to finish
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            new FakeFileStorage(),
            slowReader,
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Processing", job.Status);
        Assert.Equal(newToken, job.LeaseToken);
        Assert.Null(job.CompletedAt);

        var results = await context.ProcessingResults.AsNoTracking().ToListAsync();
        Assert.Empty(results);
    }

    [Fact]
    public async Task ProcessClaimedJob_WhenLeaseWasLostAndFileFails_IgnoresTheFailure()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(context);

        var claimed = await DocumentProcessingWorker.ClaimNextJobAsync(
            context,
            CancellationToken.None);

        Assert.NotNull(claimed);

        var jobId = claimed.Id;
        var newToken = Guid.NewGuid();

        var brokenSlowReader = new CallbackFileProcessor(async () =>
        {
            await TakeOverJobAsync(jobId, newToken);
            throw new InvalidOperationException("PDF is corrupt");
        });

        // Act
        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            context,
            new FakeFileStorage(),
            brokenSlowReader,
            claimed,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Processing", job.Status);
        Assert.Equal(newToken, job.LeaseToken);
        Assert.Equal(0, job.RetryCount);
        Assert.Null(job.ErrorMessage);
    }

}