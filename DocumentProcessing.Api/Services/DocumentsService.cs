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

    // Get Processing Result: Extracted text from documentId and userId
    public async Task<ProcessingResultDto?> GetProcessingResult(
    int documentId,
    int userId)
    {
        var result = await (
            from processingResult in _context.ProcessingResults
            join document in _context.Documents
                on processingResult.DocumentId equals document.Id
            where document.Id == documentId
                && document.UserId == userId
            select new ProcessingResultDto
            {
                DocumentId = processingResult.DocumentId,
                ExtractedText = processingResult.ExtractedText,
                CreatedAt = processingResult.CreatedAt
            })
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<ProcessingStatusDto?> GetProcessingStatus(int documentId, int userId)
    {
        var status = await (
            from job in _context.ProcessingJobs
            join document in _context.Documents
                on job.DocumentId equals document.Id
            where document.Id == documentId
                && document.UserId == userId
            select new ProcessingStatusDto
            {
                DocumentId = job.DocumentId,
                JobId = job.Id,
                Status = job.Status,
                ErrorMessage = job.ErrorMessage
            })
            .FirstOrDefaultAsync();

        return status;
    }

    public async Task<Stream> GetFileForUserAsync(int documentId, int userId, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(document =>
                document.Id == documentId &&
                document.UserId == userId,
                cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException();
        }

        return await _fileStorage.OpenReadAsync(document.StoragePath, cancellationToken);
    }
}