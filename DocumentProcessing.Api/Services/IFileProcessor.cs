namespace DocumentProcessing.Api.Services;

public interface IFileProcessor
{
    Task<string> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken);
}