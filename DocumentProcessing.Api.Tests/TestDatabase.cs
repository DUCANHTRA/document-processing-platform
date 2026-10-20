using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Tests;

public static class TestDatabase
{
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=document_processing_test;Username=postgres;Password=local_dev_password";

    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        
        return new AppDbContext(options);
    }

    public static async Task<AppDbContext> CreateCleanContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        var context = new AppDbContext(options);

        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "ProcessingResults", "ProcessingJobs", "Documents", "Users" RESTART IDENTITY CASCADE""");

        return context;
    }
}