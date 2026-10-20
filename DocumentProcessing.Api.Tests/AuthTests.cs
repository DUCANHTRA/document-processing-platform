using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DocumentProcessing.Api.Tests;

public class AuthTests
{
    //Test: register a new user
    [Fact]
    public async Task Register_WhenEmailIsNew_ReturnsCreated()
    {
        await using var _ = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "new.user@example.com",
            password = "example-only-password-2026"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("email", out var emailElement));
        Assert.Equal("new.user@example.com", emailElement.GetString());
    }

    //Test: duplicate email
    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsConflict()
    {
        await using var _ = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var request = new
        {
            email = "duplicate@example.com",
            password = "example-only-password-2026"
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    //Test: login returns a token
    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        await using var _ = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "login.user@example.com",
            password = "example-only-password-2026"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "login.user@example.com",
            password = "example-only-password-2026"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.True(body!.ContainsKey("token"));
        Assert.False(string.IsNullOrWhiteSpace(body["token"]));
    }

    // Test: wrong password is rejected
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await using var _ = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "wrong.password@example.com",
            password = "example-only-password-2026"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrong.password@example.com",
            password = "not-the-right-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    //Test: a user cannot see another user’s document
    [Fact]
    public async Task UserCanOnlySeeTheirOwnDocuments()
    {
        await using var _ = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var userA = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "a@example.com",
            password = "example-only-password-2026"
        });

        var userB = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "b@example.com",
            password = "example-only-password-2026"
        });

        var loginA = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "a@example.com",
            password = "example-only-password-2026"
        });

        var loginB = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "b@example.com",
            password = "example-only-password-2026"
        });

        var tokenA = (await loginA.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["token"];
        var tokenB = (await loginB.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["token"];

        var uploadA = await client.PostAsync(
            "/api/documents",
            new MultipartFormDataContent
            {
                { new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "a.pdf" }
            });

        uploadA.Headers.Add("Authorization", $"Bearer {tokenA}");

        var uploadB = await client.PostAsync(
            "/api/documents",
            new MultipartFormDataContent
            {
                { new ByteArrayContent(new byte[] { 4, 5, 6 }), "file", "b.pdf" }
            });

        uploadB.Headers.Add("Authorization", $"Bearer {tokenB}");
    }
}