using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public class DocumentsApiTests
{
    private static async Task<HttpClient> CreateAuthenticatedClientAsync(ApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var password = "example-only-password-2026";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var token = body!["token"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    // Test
    [Fact]
    public async Task PostDocument_SavesDocumentAndPendingJob_AndReturnsCreated()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory, "documents-user@example.com");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "invoice.pdf");

        // Act
        var response = await client.PostAsync("/api/documents", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await context.Users.SingleAsync(u => u.Email == "documents-user@example.com");

        var document = await context.Documents.AsNoTracking().SingleAsync();
        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal("fake-storage/invoice.pdf", document.StoragePath);
        Assert.Equal(user.Id, document.UserId);

        Assert.EndsWith($"/{document.Id}", response.Headers.Location!.ToString());

        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync();
        Assert.Equal(document.Id, job.DocumentId);
        Assert.Equal("Pending", job.Status);
    }

    [Fact]
    public async Task GetDocument_WhenIdDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await using var context = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = await CreateAuthenticatedClientAsync(factory, "missing-doc-user@example.com");

        // Act
        var response = await client.GetAsync("/api/documents/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}