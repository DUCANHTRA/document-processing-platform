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
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

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

// Build the app after all services are registered.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Middleware to redirect HTTP requests to HTTPS
app.UseHttpsRedirection();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

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