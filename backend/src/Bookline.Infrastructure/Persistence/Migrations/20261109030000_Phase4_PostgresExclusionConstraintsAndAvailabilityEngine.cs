using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_PostgresExclusionConstraintsAndAvailabilityEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS btree_gist;

                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_overlap;
                ALTER TABLE ""Bookings""
                ADD CONSTRAINT no_overlap
                EXCLUDE USING gist (
                    ""StaffId"" WITH =,
                    tstzrange(""StartUtc"", ""EndUtc"") WITH &&
                )
                WHERE (""Status"" IN ('Pending', 'Confirmed', 'CheckedIn'));

                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_resource_overlap;
                ALTER TABLE ""Bookings""
                ADD CONSTRAINT no_resource_overlap
                EXCLUDE USING gist (
                    ""ResourceId"" WITH =,
                    tstzrange(""StartUtc"", ""EndUtc"") WITH &&
                )
                WHERE (""ResourceId"" IS NOT NULL AND ""Status"" IN ('Pending', 'Confirmed', 'CheckedIn'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_overlap;
                ALTER TABLE ""Bookings"" DROP CONSTRAINT IF EXISTS no_resource_overlap;
            ");
        }
    }
}
