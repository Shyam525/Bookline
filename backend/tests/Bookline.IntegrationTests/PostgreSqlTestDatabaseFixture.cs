namespace Bookline.IntegrationTests;

using System;
using System.Threading.Tasks;
using Bookline.Application.Common.Models;
using Bookline.Application.Common.Services;
using Bookline.Infrastructure.Persistence;
using Bookline.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

[CollectionDefinition("PostgreSqlConcurrencyCollection")]
public class PostgreSqlConcurrencyCollection : ICollectionFixture<PostgreSqlTestDatabaseFixture>
{
}

public class PostgreSqlTestDatabaseFixture : IAsyncLifetime
{
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION") ??
        "Host=localhost;Port=5432;Database=bookline_test_concurrency;Username=postgres;Password=postgrespassword";

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BooklineDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        var tenantCtx = new TenantContext();
        using var db = new BooklineDbContext(options, tenantCtx);

        // Apply all relational migrations
        await db.Database.MigrateAsync();

        // Ensure extension and exclusion constraints exist
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE EXTENSION IF NOT EXISTS btree_gist;
            ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_overlap;
            ALTER TABLE ""Bookings"" ADD CONSTRAINT no_overlap
              EXCLUDE USING gist (
                ""StaffId"" WITH =,
                tstzrange(""StartUtc"", ""EndUtc"") WITH &&
              )
              WHERE (""Status"" IN ('Pending','Confirmed'));

            ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_resource_overlap;
            ALTER TABLE ""Bookings"" ADD CONSTRAINT no_resource_overlap
              EXCLUDE USING gist (
                ""ResourceId"" WITH =,
                tstzrange(""StartUtc"", ""EndUtc"") WITH &&
              )
              WHERE (""ResourceId"" IS NOT NULL AND ""Status"" IN ('Pending','Confirmed'));
        ");
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
