namespace DocumentProcessing.Api.Models;

public class ProcessingResult
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public string ExtractedText { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}