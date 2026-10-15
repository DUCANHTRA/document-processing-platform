using DocumentProcessing.Api.Services;
using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;
using DocumentProcessing.Api.Workers;

// Builder helps configure the app and all services.
// This is the place where we register the app's dependencies.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Register OpenAPI for Swagger-like API documentation.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// These are normal application services.
// They are created once per HTTP request and then disposed.
builder.Services.AddScoped<DocumentsService>();

// Add Supabase Setting
var supabaseSettings = builder.Configuration
    .GetSection("Supabase")
    .Get<SupabaseSettings>() ?? new SupabaseSettings();

supabaseSettings.Validate();

builder.Services.AddSingleton(supabaseSettings);

// Add Local File storage
builder.Services.AddHttpClient<IFileStorage, SupabaseFileStorage>(client =>
{
    client.BaseAddress = new Uri(supabaseSettings.Url.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("apikey", supabaseSettings.SecretKey);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

// Register the database context so the app can talk to PostgreSQL.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// This is the important part for the tutorial:
// We register the background worker here.
// ASP.NET Core will start it when the application starts.
builder.Services.AddHostedService<DocumentProcessingWorker>();
builder.Services.AddScoped<IFileProcessor, PdfFileProcessor>();

// Build the app after all services are registered.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enforce HTTPS outside development. The Vite dev server proxies local /api calls over HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.MapGet("/api/health", () =>
{
    //Return HTTP 200 OK response
    return Results.Ok(new
    {
        status = "healthy"
    });
});

//Run the app
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }

/*
Program starts
     ↓
Create builder
     ↓
Register services
     ↓
Build application
     ↓
Configure middleware/routes
     ↓
Run application
     ↓
Wait for HTTP requests
*/