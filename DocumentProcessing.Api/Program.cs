using DocumentProcessing.Api.Services;

//Builder helps configure the app and services
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//Register services for OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddControllers();

//Create one instance for each HTTP request and inject it into the controller
builder.Services.AddScoped<DocumentsService>();

//Build the app
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