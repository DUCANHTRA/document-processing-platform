using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class FakeFileStorage : IFileStorage
{
    public Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    public Task<string> SaveAsync(Stream file, string fileName)
    {
        return Task.FromResult($"fake-storage/{fileName}");

        /*
        LocalFileStorage:  "C:\...\uploads\invoice.pdf"   
        (a file now exists on disk)
        FakeFileStorage:   "fake-storage/invoice.pdf"      
        (nothing exists)
        */
    }

    public Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(new MemoryStream());
    }
}