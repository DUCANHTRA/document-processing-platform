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
    public ApiFactory()
    {
        // 1. Use the TEST database
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            TestDatabase.ConnectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 2. Use fake storage, so no real files are written
            services.RemoveAll<IFileStorage>();
            services.AddScoped<IFileStorage, FakeFileStorage>();

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