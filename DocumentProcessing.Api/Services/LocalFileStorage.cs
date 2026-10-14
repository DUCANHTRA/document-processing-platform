namespace DocumentProcessing.Api.Services;

public class LocalFileStorage : IFileStorage
{
    public async Task<string> SaveAsync(
        Stream file,
        string fileName
    )
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(),"uploads");

        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, fileName);

        await using var output = File.Create(filePath);

        await file.CopyToAsync(output);

        return filePath;
    }

    public Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(File.OpenRead(storagePath));
    }
}