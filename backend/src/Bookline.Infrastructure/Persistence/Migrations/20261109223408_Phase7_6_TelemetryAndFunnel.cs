using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_6_TelemetryAndFunnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetryAtUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProviderActivityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClientIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    IsAutomatedOrTest = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorefrontFunnelEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Step = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VisitorSessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RelatedHoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAutomatedOrBot = table.Column<bool>(type: "boolean", nullable: false),
                    IsInternalPreview = table.Column<bool>(type: "boolean", nullable: false),
                    DateKey = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorefrontFunnelEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsDemo",
                table: "Tenants",
                column: "IsDemo");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "NextRetryAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_Status_CompletedAtUtc",
                table: "Bookings",
                columns: new[] { "TenantId", "Status", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderActivityEvents_TenantId_EventType_TimestampUtc",
                table: "ProviderActivityEvents",
                columns: new[] { "TenantId", "EventType", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderActivityEvents_TenantId_TimestampUtc",
                table: "ProviderActivityEvents",
                columns: new[] { "TenantId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderActivityEvents_UserId_TimestampUtc",
                table: "ProviderActivityEvents",
                columns: new[] { "UserId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontFunnelEvents_TenantId_Step_TimestampUtc",
                table: "StorefrontFunnelEvents",
                columns: new[] { "TenantId", "Step", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StorefrontFunnelEvents_TenantId_VisitorSessionId_DateKey_St~",
                table: "StorefrontFunnelEvents",
                columns: new[] { "TenantId", "VisitorSessionId", "DateKey", "Step" });

            migrationBuilder.Sql(@"
                UPDATE ""Tenants""
                SET ""IsDemo"" = true
                WHERE ""Slug"" IN (
                    'acme-salon',
                    'aura-wellness',
                    'glow-studio',
                    'apex-dental',
                    'zenith-fitness',
                    'lumina-studio',
                    'vanguard-advisory',
                    'cambridge-academy',
                    'urbanfix-home',
                    'verdant-sanctuary'
                )
                OR ""Id"" IN (
                    '00000000-0000-0000-0000-000000000001',
                    '30000000-0000-0000-0000-000000000001',
                    '30000000-0000-0000-0000-000000000002',
                    '30000000-0000-0000-0000-000000000003',
                    '30000000-0000-0000-0000-000000000004',
                    '30000000-0000-0000-0000-000000000005',
                    '30000000-0000-0000-0000-000000000006',
                    '30000000-0000-0000-0000-000000000007',
                    '30000000-0000-0000-0000-000000000008',
                    '30000000-0000-0000-0000-000000000009'
                )
                OR ""Slug"" LIKE 'aura-pg-%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderActivityEvents");

            migrationBuilder.DropTable(
                name: "StorefrontFunnelEvents");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_IsDemo",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_Status_CompletedAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "Bookings");
        }
    }
}
