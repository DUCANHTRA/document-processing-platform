namespace DocumentProcessing.Api.Models;

public class ProcessingJob
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;

    public int MaxRetryCount { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public string? CorrelationId { get; set; }
}