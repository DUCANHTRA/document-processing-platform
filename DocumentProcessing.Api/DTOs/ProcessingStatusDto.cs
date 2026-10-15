namespace DocumentProcessing.Api.DTOs;

public class ProcessingStatusDto
{
    public int DocumentId { get; set; }
    public int JobId { get; set; }
    public string Status { get; set; }
    public string? ErrorMessage { get; set; }
}