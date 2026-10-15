using Microsoft.AspNetCore.Mvc;
using DocumentProcessing.Api.Services;
using DocumentProcessing.Api.DTOs;

namespace DocumentProcessing.Api.Controllers;

/// <summary>
/// api/documents endpoint to handle document-related requests
/// </summary>
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
        var documents = await _documentService.GetDocuments();
        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var document = await _documentService.GetDocument(id);

        if (document is null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetProcessingStatus(int id)
    {
        var status = await _documentService.GetProcessingStatus(id);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDocument(IFormFile file)
    {
        // Pass the validated request data to the service and wait for the new document to be saved.
        var document = await _documentService.CreateDocument(file);
    
        // Return HTTP 201 Created, include the new document in the response body,
        // and set the Location header to the URL for retrieving it with GetDocument.
        return CreatedAtAction(
            nameof(GetDocument),
            new { id = document.Id },
            document);
    }
}