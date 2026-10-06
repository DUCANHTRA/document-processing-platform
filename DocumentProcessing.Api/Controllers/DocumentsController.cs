using Microsoft.AspNetCore.Mvc;
using DocumentProcessing.Api.Services;

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
    public IActionResult Documents()//IActionResult return HTTP response
    {
        var documents = _documentService.GetDocuments();
        return Ok(documents);
    }
}