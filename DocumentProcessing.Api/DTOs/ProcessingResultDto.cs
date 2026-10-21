namespace DocumentProcessing.Api.DTOs;

public class ProcessingResultDto
{
    public int DocumentId { get; set; }
    public string ExtractedText { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}