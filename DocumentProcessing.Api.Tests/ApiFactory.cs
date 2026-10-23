using DocumentProcessing.Api.Services;
using DocumentProcessing.Api.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DocumentProcessing.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _storageHealthFails;

    public ApiFactory(bool storageHealthFails = false)
    {
        _storageHealthFails = storageHealthFails;

        // 1. Use the TEST database
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            TestDatabase.ConnectionString);

        // 2. Supabase Setting
        Environment.SetEnvironmentVariable("Supabase__Url", "http://localhost");
        Environment.SetEnvironmentVariable("Supabase__Bucket", "test-bucket");
        Environment.SetEnvironmentVariable("Supabase__SecretKey", "test-key");

        // 3. Deterministic JWT signing key for test host
        Environment.SetEnvironmentVariable("Jwt__Secret", "super-secret-test-key-32-chars-minimum!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 2. Use fake storage, so no real files are written
            services.RemoveAll<IFileStorage>();
            if (_storageHealthFails)
            {
                services.AddScoped<IFileStorage, FailingFileStorage>();
            }
            else
            {
                services.AddScoped<IFileStorage, FakeFileStorage>();
            }

            // 3. Switch off the background worker
            var worker = services.SingleOrDefault(service =>
                service.ServiceType == typeof(IHostedService) &&
                service.ImplementationType == typeof(DocumentProcessingWorker));

            if (worker is not null)
            {
                services.Remove(worker);
            }
        });
    }
}