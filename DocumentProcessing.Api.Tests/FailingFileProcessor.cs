using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class FailingFileProcessor : IFileProcessor
{
    public Task<string> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("PDF is corrupt");
    }
}