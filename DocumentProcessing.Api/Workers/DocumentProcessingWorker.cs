using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Workers;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public static async Task RecoverExpiredJobAsync(
        AppDbContext context,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        var requeued = await context.ProcessingJobs
            .Where(job =>
                job.Status == "Processing" &&
                job.LeaseExpiresAt != null &&
                job.LeaseExpiresAt < now &&
                job.RetryCount < job.MaxRetryCount
            )
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, "Pending")
                .SetProperty(job => job.RetryCount, job => job.RetryCount + 1)
                .SetProperty(job => job.ErrorMessage, "Worker lease expired")
                .SetProperty(job => job.NextRetryAt, now)
                .SetProperty(job => job.LeaseToken, (Guid?)null)
                .SetProperty(job => job.LeaseExpiresAt, (DateTime?)null),
                cancellationToken);
        
        var failed = await context.ProcessingJobs
            .Where(job =>
                job.Status == "Processing" &&
                job.LeaseExpiresAt != null &&
                job.LeaseExpiresAt < now &&
                job.RetryCount >= job.MaxRetryCount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, "Failed")
                .SetProperty(job => job.ErrorMessage, "Worker lease expired")
                .SetProperty(job => job.NextRetryAt, (DateTime?)null)
                .SetProperty(job => job.LeaseToken, (Guid?)null)
                .SetProperty(job => job.LeaseExpiresAt, (DateTime?)null),
                cancellationToken);

        if (requeued > 0 || failed > 0)
            {
                Console.WriteLine(
                    $"Recovered expired jobs. Requeued: {requeued}, failed: {failed}");
            }
    }

    /*
     * This method safely claims one pending job.
     *
     * Why do we need this?
     * If two workers run at the same time, both may read the same job as "Pending"
     * before either one updates it to "Processing". That creates a race condition.
     *
     * To prevent this, we use a database transaction with:
     * - FOR UPDATE SKIP LOCKED
     * - select one eligible job
     * - lock that row so another worker cannot claim it at the same time
     * - mark it as "Processing"
     * - commit the transaction
     *
     * Important: we do not keep the transaction open while processing the PDF.
     * We only lock the row long enough to claim the job, then we release the lock.
     * This keeps the system safe without blocking other workers for too long.
     */
    public static async Task<ProcessingJob?> ClaimNextJobAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                cancellationToken);

        var now = DateTime.UtcNow;

        var jobs = await context.ProcessingJobs
            .FromSqlInterpolated($"""
                SELECT *
                FROM "ProcessingJobs"
                WHERE "Status" = 'Pending'
                AND (
                    "NextRetryAt" IS NULL
                    OR "NextRetryAt" <= {now}
                )
                ORDER BY "CreatedAt", "Id"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var job = jobs.FirstOrDefault();

        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var claimedAt = DateTime.UtcNow;

        job.Status = "Processing";
        job.StartedAt = claimedAt;
        job.LeaseToken = Guid.NewGuid();
        job.LeaseExpiresAt = claimedAt.Add(LeaseDuration);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return job;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            var fileProcessor =
                scope.ServiceProvider
                    .GetRequiredService<IFileProcessor>();

            //Recover expired jobs
            await RecoverExpiredJobAsync(context, stoppingToken);

            //Claim Pending Job
            var job = await ClaimNextJobAsync(
                context,
                stoppingToken);

            //Processing Job
            if (job is not null)
            {
                var jobId = job.Id;
                var leaseToken = job.LeaseToken!.Value;

                try
                {
                    Console.WriteLine(
                        $"Started processing job {job.Id}");

                    // Simulate processing
                    var document = await context.Documents
                        .FirstAsync(
                            document => document.Id == job.DocumentId,
                            stoppingToken);

                    var text = await fileProcessor.ProcessAsync(
                        document.StoragePath,
                        stoppingToken
                    );

                    // Save extracted text to ProcessingResult table
                    Console.WriteLine($"Extracted text from {document.FileName}:");

                    var result = await context.ProcessingResults
                        .FirstOrDefaultAsync(
                            //compare Document Id from ProcessingResult and ProcessingJob
                            result => result.DocumentId == document.Id,
                            stoppingToken);

                    if (result is null)
                    {
                        result = new ProcessingResult
                        {
                            DocumentId = document.Id,
                            ExtractedText = text,
                            CreatedAt = DateTime.UtcNow
                        };

                        context.ProcessingResults.Add(result);
                    }
                    else
                    {
                        result.ExtractedText = text;

                    }


                    //Update with lease token check
                    await using var completeTx = await context.Database.BeginTransactionAsync(stoppingToken);

                    await context.SaveChangesAsync(stoppingToken);

                    var completedAt = DateTime.UtcNow;

                    var updated = await context.ProcessingJobs
                        .Where(j => j.Id == jobId && j.LeaseToken == leaseToken)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(j => j.Status, "Completed")
                            .SetProperty(j => j.CompletedAt, completedAt)
                            .SetProperty(j => j.NextRetryAt, (DateTime?)null)
                            .SetProperty(j => j.ErrorMessage, (string?)null)
                            .SetProperty(j => j.LeaseToken, (Guid?)null)
                            .SetProperty(j => j.LeaseExpiresAt, (DateTime?)null),
                            stoppingToken);

                    // Lease expires
                    if (updated == 0)
                    {
                        await completeTx.RollbackAsync(stoppingToken);

                        Console.WriteLine(
                            $"Job {jobId} lost its lease. Result discarded.");
                    }
                    else
                    {
                        await completeTx.CommitAsync(stoppingToken);

                        Console.WriteLine($"Completed job {jobId}");
                    }
                }
                // Catch error 1
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    // Application is shutting down.
                }
                // Catch error if Job Failed or Token Expires
                catch (Exception ex)
                {
                    context.ChangeTracker.Clear();

                    // Check if current job still belongs to this worker
                    var failedJob = await context.ProcessingJobs
                        .FirstOrDefaultAsync(
                            j => j.Id == jobId && j.LeaseToken == leaseToken,
                            stoppingToken);
                    
                    // Failed and expired token
                    if (failedJob is null)
                    {
                        Console.WriteLine(
                            $"Job {jobId} failed but lost its lease. Ignoring.");
                    }

                    // Failed and valid token
                    else
                    {
                        JobFailureHandler.ApplyFailure(
                            failedJob,
                            ex.Message,
                            DateTime.UtcNow);
                        
                        if (failedJob.Status == "Pending")
                        {
                            Console.WriteLine(
                                $"Job {failedJob.Id} failed. " +
                                $"Retry {failedJob.RetryCount} of {failedJob.MaxRetryCount}. " +
                                $"Will retry at {failedJob.NextRetryAt}.");
                        }
                        else
                        {
                            Console.WriteLine(
                                $"Job {failedJob.Id} permanently failed after " +
                                $"{failedJob.RetryCount} attempts: {ex.Message}");
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }

            }
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}