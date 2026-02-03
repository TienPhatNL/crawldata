using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebCrawlerService.Infrastructure.Contexts;

public class CrawlerDbContextFactory : IDesignTimeDbContextFactory<CrawlerDbContext>
{
    public CrawlerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CrawlerDbContext>();

        // Use a default connection string for migrations
        optionsBuilder.UseSqlServer("Server=localhost;Database=WebCrawlerDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True");

        return new CrawlerDbContext(optionsBuilder.Options);
    }
}
