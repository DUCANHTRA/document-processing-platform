using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class FakeFileProcessor : IFileProcessor
{
    public Task<string> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        return Task.FromResult("Hello from fake PDF");
    }
}