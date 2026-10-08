using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocumentProcessing.Api.Workers;

public class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DocumentProcessingWorker(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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

            var job = await context.ProcessingJobs
                .FirstOrDefaultAsync(
                    job => job.Status == "Processing",
                    stoppingToken);

            if (job is not null)
            {
                job.Status = "Processing";
                job.StartedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();

                try
                {
                    Console.WriteLine(
                        $"Started processing job {job.Id}");

                    // Simulate processing
                    await Task.Delay(
                        TimeSpan.FromSeconds(3),
                        stoppingToken);

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
                    job.Status = "Failed";
                    job.ErrorMessage = ex.Message;

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