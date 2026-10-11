using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class ProcessJobTests
{
    // Helper
    private static async Task AddPendingJobAsync(AppDbContext context)
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
            CreatedAt = DateTime.UtcNow
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

}