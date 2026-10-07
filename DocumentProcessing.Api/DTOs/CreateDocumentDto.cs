using System.ComponentModel.DataAnnotations;

namespace DocumentProcessing.Api.DTOs;

public class CreateDocumentDto
{
    [Required]
    public string FileName { get; set; } = "";
    [Required]
    public string ContentType { get; set; } = "";
    [Range(1, 50_000_000)]
    public long FileSize { get; set; }
}