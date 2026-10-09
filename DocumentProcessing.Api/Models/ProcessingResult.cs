using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Models;

[Index(nameof(DocumentId), IsUnique = true)]
public class ProcessingResult
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public string ExtractedText { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}