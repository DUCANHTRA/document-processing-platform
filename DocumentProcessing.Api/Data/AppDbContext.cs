using DocumentProcessing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DcoumentProcessin.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
}