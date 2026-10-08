using UglyToad.PdfPig;

namespace DocumentProcessing.Api.Services;

public class PdfFileProcessor : IFileProcessor
{
    public Task<string> ProcessAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(filePath);

        var text = string.Join(
            Environment.NewLine,
            document.GetPages()
                .Select(page => page.Text));

        return Task.FromResult(text);
    }
}