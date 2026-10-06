namespace DocumentProcessing.Api.Services;

public class DocumentsService
{
    //Service depends on IFileStorage interface to save files
    private readonly IFileStorage _fileStorage;

    public DocumentsService(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }
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