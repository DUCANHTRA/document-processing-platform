using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Services;
using Microsoft.EntityFrameworkCore;
using DocumentProcessing.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DocumentProcessing.Api.Workers;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ProcessingHub> _hubContext;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<ProcessingHub> hubContext,
        ILogger<DocumentProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    public static async Task RecoverExpiredJobAsync(
        AppDbContext context,
        CancellationToken cancellationToken,
        ILogger<DocumentProcessingWorker> logger
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
                logger.LogInformation(
                    "Recovered expired jobs. Requeued {RequeuedCount}, failed {FailedCount}",
                    requeued,
                    failed);
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

    public static async Task ProcessClaimedJobAsync(
    AppDbContext context,
    IFileStorage fileStorage,
    IFileProcessor fileProcessor,
    ProcessingJob job,
    CancellationToken stoppingToken,
    ILogger<DocumentProcessingWorker> logger)
    {
        var jobId = job.Id;
        var leaseToken = job.LeaseToken!.Value;

        try
        {
            logger.LogInformation(
                "Started processing job {JobId}",
                job.Id);

            var document = await context.Documents
            .FirstAsync(
                document => document.Id == job.DocumentId,
                stoppingToken);

            // close the stream after done reading using 'await using'
            await using var file = await fileStorage.OpenReadAsync(
                document.StoragePath,
                stoppingToken);
            
            var text = await fileProcessor.ProcessAsync(
                file, stoppingToken);
            
            logger.LogInformation(
                "Extracted text from document {DocumentId} ({FileName})",
                document.Id,
                document.FileName);

            var result = await context.ProcessingResults
                .FirstOrDefaultAsync(
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

            await using var completeTx =
                await context.Database.BeginTransactionAsync(stoppingToken);

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
            
            if (updated == 0)
            {
                await completeTx.RollbackAsync(stoppingToken);

                logger.LogWarning(
                    "Job {JobId} lost its lease. Result discarded",
                    jobId);
            }
            else
            {
                await completeTx.CommitAsync(stoppingToken);

                logger.LogInformation(
                    "Completed job {JobId}",
                    jobId);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Application is shutting down.
        }
        catch (Exception ex)
        {
            context.ChangeTracker.Clear();

            var failedJob = await context.ProcessingJobs
                .FirstOrDefaultAsync(
                    j => j.Id == jobId && j.LeaseToken == leaseToken,
                    stoppingToken);

            if (failedJob is null)
            {
                logger.LogWarning(
                    "Job {JobId} failed but lost its lease. Ignoring",
                    jobId);
            }
            else
            {
                JobFailureHandler.ApplyFailure(
                    failedJob,
                    ex.Message,
                    DateTime.UtcNow);

                if (failedJob.Status == "Pending")
                {
                    logger.LogWarning(
                        ex,
                        "Job {JobId} failed. Retry {RetryCount} of {MaxRetryCount}; next retry at {NextRetryAt}",
                        failedJob.Id,
                        failedJob.RetryCount,
                        failedJob.MaxRetryCount,
                        failedJob.NextRetryAt);
                }
                else
                {
                    logger.LogError(
                        ex,
                        "Job {JobId} permanently failed after {RetryCount} attempts",
                        failedJob.Id,
                        failedJob.RetryCount);
                }

                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private async Task NotifyJobStatusAsync(
        AppDbContext context,
        int jobId,
        CancellationToken cancellationToken)
    {
        var update = await (
            from job in context.ProcessingJobs
            join document in context.Documents
                on job.DocumentId equals document.Id
            where job.Id == jobId
            select new
            {
                document.UserId,
                job.DocumentId,
                JobId = job.Id,
                job.Status,
                job.ErrorMessage
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (update is null)
        {
            return;
        }

        await _hubContext.Clients
            .Group($"user-{update.UserId}")
            .SendAsync(
                "JobStatusChanged",
                new
                {
                    update.DocumentId,
                    update.JobId,
                    update.Status,
                    update.ErrorMessage
                },
                cancellationToken);
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

            var fileStorage = 
                scope.ServiceProvider
                    .GetRequiredService<IFileStorage>();

            //Recover expired jobs
            await RecoverExpiredJobAsync(context, stoppingToken, _logger);

            //Claim Pending Job
            var job = await ClaimNextJobAsync(
                context,
                stoppingToken);

            //Processing Job
            if (job is not null)
            {
                await NotifyJobStatusAsync(
                    context,
                    job.Id,
                    stoppingToken);

                await ProcessClaimedJobAsync(
                    context,
                    fileStorage,
                    fileProcessor,
                    job,
                    stoppingToken,
                    _logger);
                
                await NotifyJobStatusAsync(
                    context,
                    job.Id,
                    stoppingToken);
            }
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}