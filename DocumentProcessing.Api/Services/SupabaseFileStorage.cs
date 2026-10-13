using System.Net.Http.Headers;

namespace DocumentProcessing.Api.Services;

public class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseSettings _settings;

    public SupabaseFileStorage(HttpClient httpClient, SupabaseSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<string> SaveAsync(
        Stream file,
        string fileName)
    {
        var objectPath = fileName;

        var url = $"storage/v1/object/{_settings.Bucket}/{Uri.EscapeDataString(objectPath)}";

        using var content = new StreamContent(file);
        content.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Supabase upload failed ({(int)response.StatusCode}): {body}");
        }

        return objectPath;
    }
}