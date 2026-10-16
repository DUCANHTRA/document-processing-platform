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

    public async Task<object[]> GetDocumentsForUser(int userId)
    {
        return await _context.Documents
            .Where(document => document.UserId == userId)
            .Select(document => new
            {
                id = document.Id,
                fileName = document.FileName
            })
            .ToArrayAsync();
    }

    public async Task<Document?> GetDocumentForUser(int id, int userId)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(document =>
                document.Id == id &&
                document.UserId == userId);
    }

    public async Task<Document> CreateDocument(IFormFile file, int userId)
    {
        var storagePath = await _fileStorage.SaveAsync(
            file.OpenReadStream(),
            file.FileName
        );
        var document = new Document
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoragePath = storagePath,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        /*
        Create a new ProcessingJob for the uploaded document and 
        save it to the database. 
        The job is initialized with a "Pending" status 
        and the current UTC time as the CreatedAt timestamp. 
        The job is then added to the ProcessingJobs DbSet and saved to the database.
        */
        var job = new ProcessingJob
        {
            DocumentId = document.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.ProcessingJobs.Add(job);
        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<ProcessingStatusDto> GetProcessingStatus(int documentId)
    {
        var job = await _context.ProcessingJobs
            .FirstOrDefaultAsync(job => job.DocumentId == documentId);

        if (job is null)
        {
            return null;
        }

        return new ProcessingStatusDto
        {
            DocumentId = job.DocumentId,
            JobId = job.Id,
            Status = job.Status,
            ErrorMessage = job.ErrorMessage
        };
    }
}