using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class RecordingFileStorage : IFileStorage
{
    private readonly byte[] _content;

    public List<string> RequestedPaths { get; } = new();

    public RecordingFileStorage(byte[] content)
    {
        _content = content;
    }

    public Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<string> SaveAsync(Stream file, string fileName)
    {
        return Task.FromResult("recorded-" + fileName);
    }

    public Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken)
    {
        RequestedPaths.Add(storagePath);

        return Task.FromResult<Stream>(new MemoryStream(_content));
    }
}