using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class JobFailureHandlerTests
{
    private static readonly DateTime Now =
        new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static ProcessingJob CreateJob(int retryCount, int maxRetryCount)
    {
        return new ProcessingJob
        {
            DocumentId = 1,
            Status = "Processing",
            RetryCount = retryCount,
            MaxRetryCount = maxRetryCount
        };
    }

    [Fact]
    public void ApplyFailure_WithRetriesLeft_MovesJobBackToPending()
    {
        // Arrange
        var job = CreateJob(retryCount: 0, maxRetryCount: 3);

        // Act
        JobFailureHandler.ApplyFailure(job, "boom", Now);

        // Assert
        Assert.Equal("Pending", job.Status);
        Assert.Equal("boom", job.ErrorMessage);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal(Now.AddSeconds(5), job.NextRetryAt);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(3, 40)]
    public void ApplyFailure_DoublesTheDelayEachTime(
        int retryCountBefore,
        int expectedDelaySeconds)
    {
        var job = CreateJob(retryCount: retryCountBefore, maxRetryCount: 10);

        JobFailureHandler.ApplyFailure(job, "boom", Now);

        Assert.Equal(Now.AddSeconds(expectedDelaySeconds), job.NextRetryAt);
    }

    [Fact]
    public void ApplyFailure_OnLastAllowedRetry_StillGoesToPending()
    {
        var job = CreateJob(retryCount: 2, maxRetryCount: 3);

        JobFailureHandler.ApplyFailure(job, "boom", Now);

        Assert.Equal("Pending", job.Status);
        Assert.Equal(3, job.RetryCount);
    }

    [Fact]
    public void ApplyFailure_WhenRetriesAreUsedUp_MarksJobFailed()
    {
        var job = CreateJob(retryCount: 3, maxRetryCount: 3);

        JobFailureHandler.ApplyFailure(job, "boom", Now);

        Assert.Equal("Failed", job.Status);
        Assert.Equal(4, job.RetryCount);
        Assert.Null(job.NextRetryAt);
    }

    [Fact]
    public void ApplyFailure_SavesErrorMessage()
    {
        var job = CreateJob(retryCount: 0, maxRetryCount: 3);

        JobFailureHandler.ApplyFailure(job, "PDF is corrupt", Now);

        Assert.Equal("PDF is corrupt", job.ErrorMessage);
    }

    [Fact]
    public void ApplyFailure_ClearsTheLease()
    {
        var job = CreateJob(retryCount: 0, maxRetryCount: 3);
        job.LeaseToken = Guid.NewGuid();
        job.LeaseExpiresAt = Now.AddMinutes(5);

        JobFailureHandler.ApplyFailure(job, "boom", Now);

        Assert.Null(job.LeaseToken);
        Assert.Null(job.LeaseExpiresAt);
    }
}