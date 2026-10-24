using DocumentProcessing.Api.Services;
using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;
using DocumentProcessing.Api.Workers;
using DocumentProcessing.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DocumentProcessing.Api.Hubs;

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
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSignalR();

// Add Supabase Setting
var supabaseSettings = builder.Configuration
    .GetSection("Supabase")
    .Get<SupabaseSettings>() ?? new SupabaseSettings();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT signing secret is not configured.");

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSecret));


// JWT Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateIssuer = true,
            ValidIssuer = "DocumentProcessing.Api",

            ValidateAudience = true,
            ValidAudience = "DocumentProcessing.Client",

            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var requestPath = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    requestPath.StartsWithSegments("/hubs/processing"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

supabaseSettings.Validate();

builder.Services.AddSingleton(supabaseSettings);

// Add Supabase File storage
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

// This is the important step:
// We register the background worker here.
// ASP.NET Core will start it when the application starts.
var workerEnabled = builder.Configuration.GetValue<bool?>("Worker:Enabled") ?? true;

if (workerEnabled)
{
    builder.Services.AddHostedService<DocumentProcessingWorker>();
}
builder.Services.AddScoped<IFileProcessor, PdfFileProcessor>();

// Add HTTP Logging
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode;
});

// Build the app after all services are registered.
var app = builder.Build();

//
var migrateOnStartup =
    builder.Configuration.GetValue<bool?>("Database:MigrateOnStartup") ?? true;

if (migrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
}

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

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ProcessingHub>("/hubs/processing");
app.UseHttpLogging();

app.MapControllers();

// Database health check endpoint
app.MapGet("/api/health/database", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();

    return Results.Json(
        new
        {
            database = canConnect ? "reachable" : "unavailable"
        },
        statusCode: canConnect
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable);
});

// Storage health check endpoint
app.MapGet("/api/health/storage", async (
    IFileStorage storage,
    CancellationToken cancellationToken) =>
{
    try
    {
        await storage.CheckHealthAsync(cancellationToken);
        return Results.Ok(new { storage = "reachable" });
    }
    catch (HttpRequestException)
    {
        return Results.Json(
            new { storage = "unavailable" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// Diagnostic endpoint
app.MapGet("/api/health/diagnostics", async (
    AppDbContext db,
    IFileStorage storage,
    CancellationToken cancellationToken) =>
{
    var databaseReachable =
        await db.Database.CanConnectAsync(cancellationToken);

    var storageReachable = true;

    try
    {
        await storage.CheckHealthAsync(cancellationToken);
    }
    catch (HttpRequestException)
    {
        storageReachable = false;
    }

    var allHealthy = databaseReachable && storageReachable;

    return Results.Json(
        new
        {
            status = allHealthy ? "healthy" : "unhealthy",
            database = databaseReachable ? "reachable" : "unavailable",
            storage = storageReachable ? "reachable" : "unavailable"
        },
        statusCode: allHealthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable);
});

//Run the app
app.Run();

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