using DocumentProcessing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<ProcessingJob> ProcessingJobs { get; set; }
    public DbSet<ProcessingResult> ProcessingResults { get; set; }
    public DbSet<User> Users { get; set; }
}