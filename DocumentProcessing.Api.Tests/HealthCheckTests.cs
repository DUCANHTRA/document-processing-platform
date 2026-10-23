using System.Net;
using System.Net.Http.Json;

namespace DocumentProcessing.Api.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task Diagnostics_WhenDatabaseAndStorageAreReachable_ReturnsHealthy()
    {
        await using var database = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health/diagnostics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DiagnosticsResponse>();

        Assert.NotNull(body);
        Assert.Equal("healthy", body.Status);
        Assert.Equal("reachable", body.Database);
        Assert.Equal("reachable", body.Storage);
    }

    [Fact]
    public async Task Diagnostics_WhenStorageIsUnavailable_ReturnsServiceUnavailable()
    {
        await using var database = await TestDatabase.CreateCleanContextAsync();
        await using var factory = new ApiFactory(storageHealthFails: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health/diagnostics");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DiagnosticsResponse>();

        Assert.NotNull(body);
        Assert.Equal("unhealthy", body.Status);
        Assert.Equal("reachable", body.Database);
        Assert.Equal("unavailable", body.Storage);
    }

    private sealed record DiagnosticsResponse(
        string Status,
        string Database,
        string Storage);
}