using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentProcessing.Api.Tests;

public class RecoverJobTests
{
    // Helper
    private static async Task AddProcessingJobAsync(
        AppDbContext context,
        int retryCount,
        int maxRetryCount,
        DateTime leaseExpiresAt)
    {

        var userId = await TestDatabase.CreateUserAsync(context, $"recover-job-{Guid.NewGuid():N}@example.com");

        // Save a new document
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

        // Save a job stuck in processing
        context.ProcessingJobs.Add(new ProcessingJob
        {
            DocumentId = document.Id,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            LeaseToken = Guid.NewGuid(),
            LeaseExpiresAt = leaseExpiresAt,
            RetryCount = retryCount,
            MaxRetryCount = maxRetryCount
        });

        await context.SaveChangesAsync();
    }
    
    // Test
    [Fact]
    public async Task RecoverExpiredJob_WithRetriesLeft_MovesJobBackToPending()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();

        await AddProcessingJobAsync(
            context,
            retryCount: 0,
            maxRetryCount: 3,
            leaseExpiresAt: DateTime.UtcNow.AddMinutes(-1) 
        );

        // Act
        await DocumentProcessingWorker.RecoverExpiredJobAsync(
            context,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance
        );
        
        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Pending", job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal("Worker lease expired", job.ErrorMessage);
        Assert.Null(job.LeaseToken);
        Assert.Null(job.LeaseExpiresAt);
    }

    [Fact]
    public async Task RecoverExpiredJob_WithNoRetriesLeft_MarksJobFailed()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();

        await AddProcessingJobAsync(
            context,
            retryCount: 3,
            maxRetryCount: 3,
            leaseExpiresAt: DateTime.UtcNow.AddMinutes(-1));

        // Act
        await DocumentProcessingWorker.RecoverExpiredJobAsync(
            context,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Failed", job.Status);
        Assert.Equal(3, job.RetryCount);
        Assert.Null(job.LeaseToken);
    }

    [Fact]
    public async Task RecoverExpiredJob_WhenLeaseNotExpired_LeavesJobAlone()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();

        await AddProcessingJobAsync(
            context,
            retryCount: 0,
            maxRetryCount: 3,
            leaseExpiresAt: DateTime.UtcNow.AddMinutes(5));

        // Act
        await DocumentProcessingWorker.RecoverExpiredJobAsync(
            context,
            CancellationToken.None,
            NullLogger<DocumentProcessingWorker>.Instance);

        // Assert
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();

        Assert.Equal("Processing", job.Status);
        Assert.Equal(0, job.RetryCount);
        Assert.NotNull(job.LeaseToken);
    }
}