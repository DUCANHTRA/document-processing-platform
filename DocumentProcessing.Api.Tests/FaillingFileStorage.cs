using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class FailingFileStorage : IFileStorage
{
    public Task<string> SaveAsync(Stream file, string fileName)
    {
        throw new InvalidOperationException("save failed");
    }

    public Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("download failed");
    }
}
