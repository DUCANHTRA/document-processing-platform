using Microsoft.AspNetCore.Mvc;
using DocumentProcessing.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocumentProcessing.Api.Controllers;

/// <summary>
/// api/documents endpoint to handle document-related requests
/// </summary>
[Authorize]
[ApiController] //Indicates that this class is an API controller
[Route("api/[controller]")] //[controller] is a placeholder that will be replaced with the name of the controller, in this case, "Documents"
public class DocumentsController : ControllerBase
{
    private readonly DocumentsService _documentService;

    //Provide dependency injection for the DocumentsService
    public DocumentsController(DocumentsService documentService)
    {
        _documentService = documentService;
    }
    [HttpGet]
    public async Task<IActionResult> Documents()//IActionResult return HTTP response
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var documents = await _documentService.GetDocumentsForUser(userId);
        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var document = await _documentService.GetDocumentForUser(id, userId);

        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetProcessingStatus(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var status = await _documentService.GetProcessingStatus(id, userId);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpGet("{id}/result")]
    public async Task<IActionResult> GetProcessingResult(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _documentService.GetProcessingResult(id, userId);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var stream = await _documentService.GetFileForUserAsync(
                id,
                userId,
                HttpContext.RequestAborted);

            var file = await _documentService.GetDocumentForUser(id, userId);

            if (file is null)
            {
                return NotFound();
            }

            return File(stream, file.ContentType, file.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument(IFormFile file)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        // Pass the validated request data to the service and wait for the new document to be saved.
        var document = await _documentService.CreateDocument(file, userId);
    
        // Return HTTP 201 Created, include the new document in the response body,
        // and set the Location header to the URL for retrieving it with GetDocument.
        return CreatedAtAction(
            nameof(GetDocument),
            new { id = document.Id },
            document);
    }
}