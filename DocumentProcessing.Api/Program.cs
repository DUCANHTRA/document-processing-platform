using DocumentProcessing.Api.Services;
using DocumentProcessing.Api.Data;
using Microsoft.EntityFrameworkCore;
using DocumentProcessing.Api.Workers;
using DocumentProcessing.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Add Supabase Setting
var supabaseSettings = builder.Configuration
    .GetSection("Supabase")
    .Get<SupabaseSettings>() ?? new SupabaseSettings();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT signing secret is not configured.");

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSecret));


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
    });

builder.Services.AddAuthorization();

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

// Middleware
app.UseAuthentication();
app.UseAuthorization();

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