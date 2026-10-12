using DocumentProcessing.Api.Services;

namespace DocumentProcessing.Api.Tests;

public class CallbackFileProcessor : IFileProcessor
{
    // Worker --> ProcessAsync --> this runs --> return text
    private readonly Func<Task<string>> _callback;

    public CallbackFileProcessor(Func<Task<string>> callback)
    {
        _callback = callback;
    }

    public Task<string> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        return _callback();
    }
}