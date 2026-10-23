using DocumentProcessing.Api.Services;
using System.Net.Http;

namespace DocumentProcessing.Api.Tests;

public class FailingFileStorage : IFileStorage
{
    public Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        throw new HttpRequestException("storage health check failed");
    }
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
