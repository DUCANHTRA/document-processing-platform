namespace DocumentProcessing.Api.Services;

public class SupabaseSettings
{
    public string Url { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string SecretKey { get; set; } = "";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Url))
            throw new InvalidOperationException("Supabase:Url is not set.");

        if (string.IsNullOrWhiteSpace(Bucket))
            throw new InvalidOperationException("Supabase:Bucket is not set.");

        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("Supabase:SecretKey is not set.");
    }
}