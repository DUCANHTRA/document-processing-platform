namespace DocumentProcessing.Api.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(
        Stream file,
        string fileName
    );
}