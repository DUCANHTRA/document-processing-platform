namespace DocumentProcessing.Api.Models;

public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }

    public string StoragePath { get; set;} = "";
    public DateTime CreatedAt { get; set; }
}