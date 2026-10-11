using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class DocumentsServiceTests
{
    [Fact] //--> this is a test
    public async Task CreateDocument_SavesDocumentAndPendingJob()
    {
        using var context = CreateContext(); // Fake DB
        var service = new DocumentsService(new FakeFileStorage(), context); //Fake services
        var file = CreateFormFile("invoice.pdf"); // fake files

        // Act
        var document = await service.CreateDocument(file);

        // Assert
        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal("fake-storage/invoice.pdf", document.StoragePath);

        var job = Assert.Single(context.ProcessingJobs);
        Assert.Equal(document.Id, job.DocumentId);
        Assert.Equal("Pending", job.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IFormFile CreateFormFile(string fileName)
    {
        var bytes = new byte[] { 1, 2, 3 };
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }
}