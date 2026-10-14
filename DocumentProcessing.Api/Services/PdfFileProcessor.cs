using UglyToad.PdfPig;

namespace DocumentProcessing.Api.Services;

public class PdfFileProcessor : IFileProcessor
{
    public Task<string> ProcessAsync(
        Stream file,
        CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(file);

        var text = string.Join(
            Environment.NewLine,
            document.GetPages()
                .Select(page => page.Text));

        return Task.FromResult(text);
    }
}