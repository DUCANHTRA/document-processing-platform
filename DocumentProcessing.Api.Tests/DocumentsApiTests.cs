using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class DocumentsApiTests
{
    // Test
    [Fact]
    public async Task PostDocument_SavesDocumentAndPendingJob_AndReturnsCreated()
    {
        // Arrange
        // Empty test database
        await using var context = await TestDatabase.CreateCleanContextAsync();

        // Create a test copy of the app and client
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "invoice.pdf");

        // Act
        var response = await client.PostAsync("/api/documents", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var document = await context.Documents.AsNoTracking().SingleAsync();
        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal("fake-storage/invoice.pdf", document.StoragePath);

        Assert.EndsWith($"/{document.Id}", response.Headers.Location!.ToString());

        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();
        Assert.Equal(document.Id, job.DocumentId);
        Assert.Equal("Pending", job.Status);
    }

    [Fact]
    public async Task GetDocument_WhenIdDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync(); // empty tables
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/documents/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}