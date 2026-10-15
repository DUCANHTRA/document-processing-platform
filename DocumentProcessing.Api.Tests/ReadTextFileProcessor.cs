using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class ReadTextFileProcessor : IFileProcessor
{
    public async Task<string> ProcessAsync(
        Stream file,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file);

        return await reader.ReadToEndAsync(cancellationToken);
    }
}
