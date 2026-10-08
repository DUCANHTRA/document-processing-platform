using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocumentProcessing.Api.Workers;

// This class is a background worker.
// It runs while the application is alive and checks the database for work to do.
// In this tutorial, it is used to find jobs with Status = "Pending".
public class DocumentProcessingWorker : BackgroundService
{
    // This is how the worker creates a new database scope.
    // A normal controller can use a scoped DbContext automatically,
    // but a background service does not have an HTTP request to create one.
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    // ASP.NET Core calls this method when the app starts.
    // This is the place where the worker runs its long-lived loop.
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Keep checking until the app is shutting down.
        while (!stoppingToken.IsCancellationRequested)
        {
            // Create a new scope so we can get a fresh AppDbContext.
            // This is important because DbContext is scoped.
            using var scope = _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            // Look for a job that is waiting to be processed.
            // If we find one, we can work on it later.
            var job = await context.ProcessingJobs
                .FirstOrDefaultAsync(
                    job => job.Status == "Pending",
                    stoppingToken);

            if (job is not null)
            {
                // This is just a simple example for learning.
                // Later, we will update the status to "Processing" and do real work.
                Console.WriteLine(
                    $"Found job {job.Id}");
            }

            // Wait a few seconds before checking again.
            // This prevents the worker from hitting the database over and over too fast.
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
    }
}