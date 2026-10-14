namespace DocumentProcessing.Api.Services;

public interface IFileProcessor
{
    Task<string> ProcessAsync(
        Stream file,
        CancellationToken cancellationToken);
}