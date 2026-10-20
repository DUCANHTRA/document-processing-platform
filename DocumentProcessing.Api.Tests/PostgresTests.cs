using DocumentProcessing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class PostgresTests
{
    [Fact]
    public async Task CanSaveAndReadJob_UsingRealPostgres()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();

        var userId = await TestDatabase.CreateUserAsync(context, $"postgres-{Guid.NewGuid():N}@example.com");

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

        context.ProcessingJobs.Add(new ProcessingJob
        {
            DocumentId = document.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var job = await context.ProcessingJobs.SingleAsync();

        // Assert
        Assert.Equal("Pending", job.Status);
        Assert.Equal(document.Id, job.DocumentId);
    }
}