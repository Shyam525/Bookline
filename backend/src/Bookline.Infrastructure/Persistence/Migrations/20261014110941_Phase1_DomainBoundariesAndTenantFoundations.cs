using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_DomainBoundariesAndTenantFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RequiredResourceId",
                table: "Services",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Services",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Resources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowBackorders",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TrackInventory",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationMemberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedBookingId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedBookingReference",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedOrderId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedOrderNumber",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceLocations",
                columns: table => new
                {
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    PriceOverride = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLocations", x => new { x.ServiceId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_ServiceLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceLocations_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHours_TenantId_StaffId_DayOfWeek",
                table: "WorkingHours",
                columns: new[] { "TenantId", "StaffId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeOffs_TenantId_StaffId_StartUtc_EndUtc",
                table: "TimeOffs",
                columns: new[] { "TenantId", "StaffId", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TenantId_Email",
                table: "Staff",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_TenantId_IsActive",
                table: "Staff",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_RequiredResourceId",
                table: "Services",
                column: "RequiredResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TenantId_Slug",
                table: "Services",
                columns: new[] { "TenantId", "Slug" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_LocationId",
                table: "Resources",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_LocationId_IsActive",
                table: "Resources",
                columns: new[] { "TenantId", "LocationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Sku",
                table: "Products",
                columns: new[] { "TenantId", "Sku" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Slug",
                table: "Products",
                columns: new[] { "TenantId", "Slug" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_TenantId_Role",
                table: "OrganizationMemberships",
                columns: new[] { "TenantId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId",
                table: "OrganizationMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId_TenantId",
                table: "OrganizationMemberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RelatedBookingId",
                table: "Orders",
                column: "RelatedBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId_CreatedAtUtc",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_TenantId_RecipientEmail",
                table: "NotificationLogs",
                columns: new[] { "TenantId", "RecipientEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Name_IsActive",
                table: "Locations",
                columns: new[] { "TenantId", "Name", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Email",
                table: "Customers",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_Phone",
                table: "Customers",
                columns: new[] { "TenantId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_UserId",
                table: "Customers",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                table: "Customers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RelatedOrderId",
                table: "Bookings",
                column: "RelatedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_LocationId_StartUtc",
                table: "Bookings",
                columns: new[] { "TenantId", "LocationId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_ServiceId",
                table: "Bookings",
                columns: new[] { "TenantId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_Status_StartUtc",
                table: "Bookings",
                columns: new[] { "TenantId", "Status", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_Action",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_TenantId_IsActive",
                table: "ProductCategories",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_TenantId_Slug",
                table: "ProductCategories",
                columns: new[] { "TenantId", "Slug" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_TenantId_ProductId",
                table: "ProductImages",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLocations_LocationId",
                table: "ServiceLocations",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCategories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Locations_LocationId",
                table: "Resources",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Resources_RequiredResourceId",
                table: "Services",
                column: "RequiredResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCategories_CategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Locations_LocationId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Resources_RequiredResourceId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ServiceLocations");

            migrationBuilder.DropIndex(
                name: "IX_WorkingHours_TenantId_StaffId_DayOfWeek",
                table: "WorkingHours");

            migrationBuilder.DropIndex(
                name: "IX_TimeOffs_TenantId_StaffId_StartUtc_EndUtc",
                table: "TimeOffs");

            migrationBuilder.DropIndex(
                name: "IX_Staff_TenantId_Email",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Staff_TenantId_IsActive",
                table: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Services_RequiredResourceId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_TenantId_Slug",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Resources_LocationId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_TenantId_LocationId_IsActive",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_TenantId_Role",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_UserId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationMemberships_UserId_TenantId",
                table: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RelatedBookingId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_TenantId_CreatedAtUtc",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_TenantId_RecipientEmail",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TenantId_Name_IsActive",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_Phone",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TenantId_UserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_UserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RelatedOrderId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_LocationId_StartUtc",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_ServiceId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_Status_StartUtc",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_CreatedAtUtc",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "RequiredResourceId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "AllowBackorders",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TrackInventory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationMemberships");

            migrationBuilder.DropColumn(
                name: "RelatedBookingId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RelatedBookingReference",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RelatedOrderId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RelatedOrderNumber",
                table: "Bookings");
        }
    }
}
