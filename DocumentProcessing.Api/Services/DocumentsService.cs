namespace DocumentProcessing.Api.Services;

public class DocumentsService
{
    public object[] GetDocuments()
    {
        return
        [
            new
            {
                id = 1,
                fileName = "invoice.pdf"
            }
        ];
    }
}