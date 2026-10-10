using DocumentProcessing.Api.Models;

namespace DocumentProcessing.Api.Services;

public static class JobFailureHandler
{
    public static void ApplyFailure(
        ProcessingJob job,
        string errorMessage,
        DateTime now)
    {
        job.RetryCount++;
        job.ErrorMessage = errorMessage;

        job.LeaseToken = null;
        job.LeaseExpiresAt = null;

        if (job.RetryCount <= job.MaxRetryCount)
        {
            var delaySeconds = 5 * Math.Pow(2, job.RetryCount - 1);

            job.Status = "Pending";
            job.NextRetryAt = now.AddSeconds(delaySeconds);
        }
        else
        {
            job.Status = "Failed";
            job.NextRetryAt = null;
        }
    }
}