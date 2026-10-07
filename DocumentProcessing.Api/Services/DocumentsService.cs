using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Services;

public class DocumentsService
{
    //Service depends on IFileStorage interface to save files
    private readonly IFileStorage _fileStorage;
    private readonly AppDbContext _context;

    public DocumentsService(IFileStorage fileStorage, AppDbContext context)
    {
        _fileStorage = fileStorage;
        _context = context;
    }
    public async Task<object[]> GetDocuments()
    {
        //_context.Documents is a DbSet<Document> representing the Documents table in the database
        return await _context.Documents
            .Select(document => new
            {
                id = document.Id,
                fileName = document.FileName,
            })
            .ToArrayAsync(); //execute the query and return the results as an array async
    }
}