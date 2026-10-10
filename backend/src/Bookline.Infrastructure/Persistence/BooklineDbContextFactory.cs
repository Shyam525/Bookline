using Bookline.Application.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookline.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations adhering to Section 118 (PostgreSQL + Migrations).
/// Enables migration generation and scripting without requiring a live PostgreSQL instance.
/// </summary>
public class BooklineDbContextFactory : IDesignTimeDbContextFactory<BooklineDbContext>
{
    public BooklineDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BooklineDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=bookline_db;Username=postgres;Password=postgrespassword",
            b => b.MigrationsAssembly(typeof(BooklineDbContext).Assembly.FullName));

        return new BooklineDbContext(optionsBuilder.Options, new TenantContext());
    }
}
