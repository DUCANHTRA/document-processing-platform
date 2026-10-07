using DocumentProcessing.Api.Data;
using DocumentProcessing.Api.DTOs;
using DocumentProcessing.Api.Models;
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

    public async Task<Document?> GetDocument(int id)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(document => document.Id == id);
    }

    public async Task<Document> CreateDocument(
        CreateDocumentDto dto)
    {
        var document = new Document
        {
            FileName = dto.FileName,
            ContentType = dto.ContentType,
            FileSize = dto.FileSize,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }
}