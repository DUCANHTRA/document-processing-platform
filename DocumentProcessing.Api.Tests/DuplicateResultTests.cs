using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Models;
using DocumentProcessing.Api.Workers;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class DuplicateResultTests
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
    public async Task ProcessingSameDocumentTwice_KeepsOneResultWithNewestText()
    {
        // Arrange: run the job once
        await using var firstContext = await TestDatabase.CreateCleanContextAsync();
        await AddPendingJobAsync(firstContext);

        var firstClaim = await DocumentProcessingWorker.ClaimNextJobAsync(
            firstContext,
            CancellationToken.None);

        Assert.NotNull(firstClaim);

        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            firstContext,
            new FakeFileStorage(),
            new FakeFileProcessor(),
            firstClaim,
            CancellationToken.None);

        // Put the same job back in the queue
        // Does this bug exist in current codebase ?
        await firstContext.ProcessingJobs.ExecuteUpdateAsync(setters => setters
            .SetProperty(j => j.Status, "Pending")
            .SetProperty(j => j.CompletedAt, (DateTime?)null));

        // Act: run it a second time, in a new context
        await using var secondContext = TestDatabase.CreateContext();

        var secondClaim = await DocumentProcessingWorker.ClaimNextJobAsync(
            secondContext,
            CancellationToken.None);

        Assert.NotNull(secondClaim);

        var secondReader = new CallbackFileProcessor(
            () => Task.FromResult("Second text"));

        await DocumentProcessingWorker.ProcessClaimedJobAsync(
            secondContext,
            new FakeFileStorage(),
            secondReader,
            secondClaim,
            CancellationToken.None);

        // Assert
        var results = await secondContext.ProcessingResults
            .AsNoTracking()
            .ToListAsync();

        var result = Assert.Single(results);
        Assert.Equal("Second text", result.ExtractedText);
    }

    [Fact]
    public async Task Database_DoesNotAllowTwoResultsForOneDocument()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();

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

        context.ProcessingResults.Add(new ProcessingResult
        {
            DocumentId = document.Id,
            ExtractedText = "first",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act
        context.ProcessingResults.Add(new ProcessingResult
        {
            DocumentId = document.Id,
            ExtractedText = "second",
            CreatedAt = DateTime.UtcNow
        });

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }
}