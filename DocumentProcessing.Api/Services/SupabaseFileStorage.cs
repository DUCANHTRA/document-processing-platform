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
        var extension = Path.GetExtension(fileName).ToLowerInvariant(); // takes ending of the name (.pdf)
        var objectPath = $"{Guid.NewGuid()}{extension}";  //add random id to the front

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

    public async Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken)
    {
        var url = $"storage/v1/object/authenticated/{_settings.Bucket}/{Uri.EscapeDataString(storagePath)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Supabase download failed ({(int)response.StatusCode}): {body}");
        }

        var memory = new MemoryStream();
        await response.Content.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        return memory;
    }
}