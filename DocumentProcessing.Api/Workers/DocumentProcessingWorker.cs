using System.Net.Mail;
using System.Security.Claims;
using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Workers;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
    private async Task<ProcessingJob?> ClaimNextJobAsync(
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

        job.Status = "Processing";
        job.StartedAt = DateTime.UtcNow;

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

            var now = DateTime.UtcNow;

            var job = await ClaimNextJobAsync(
                context,
                stoppingToken);

            if (job is not null)
            {
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

                    var result = new ProcessingResult
                    {
                        DocumentId = document.Id,
                        ExtractedText = text,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.ProcessingResults.Add(result);
                    await context.SaveChangesAsync();

                    //Update job status Completed
                    job.Status = "Completed";
                    job.CompletedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();

                    Console.WriteLine(
                        $"Completed job {job.Id}");
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    // Application is shutting down.
                }
                catch (Exception ex)
                {
                    job.RetryCount++;
                    job.ErrorMessage = ex.Message;

                    if (job.RetryCount <= job.MaxRetryCount)
                    {
                        job.Status = "Pending";

                        var delaySecond =
                            5 * Math.Pow(2, job.RetryCount - 1);

                        job.NextRetryAt = 
                            DateTime.UtcNow.AddSeconds(delaySecond);

                        Console.WriteLine(
                            $"Job {job.Id} failed " +
                            $"Retry {job.RetryCount} of {job.MaxRetryCount} " +
                            $"Job {job.Id} will retry at {job.NextRetryAt}."
                        );
                    }
                    else
                    {
                        job.Status = "Failed";
                        job.NextRetryAt = null;

                        Console.WriteLine(
                            $"Job {job.Id} permanently failed after " +
                            $"{job.RetryCount} attempts: {ex.Message}"
                        );
                    }

                    await context.SaveChangesAsync();

                    Console.WriteLine(
                        $"Failed job {job.Id}: {ex.Message}");
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}