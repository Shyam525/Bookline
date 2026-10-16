using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookline.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Section85QueryDrivenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkingHours_StaffId",
                table: "WorkingHours");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TenantId_ModerationStatus",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_Status",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StaffId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_LocationId_StartUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHours_StaffId_DayOfWeek",
                table: "WorkingHours",
                columns: new[] { "StaffId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive_IsPublished_ModerationStatus_City_AverageR~",
                table: "Tenants",
                columns: new[] { "IsActive", "IsPublished", "ModerationStatus", "City", "AverageRating" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId_CreatedAtUtc",
                table: "Reviews",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TenantId_ModerationStatus_CreatedAtUtc",
                table: "Reviews",
                columns: new[] { "TenantId", "ModerationStatus", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TenantId_ModerationStatus_Rating_CreatedAtUtc",
                table: "Reviews",
                columns: new[] { "TenantId", "ModerationStatus", "Rating", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_CategoryId_IsActive_Price",
                table: "Products",
                columns: new[] { "TenantId", "CategoryId", "IsActive", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId_CreatedAtUtc",
                table: "Payments",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StripeCheckoutSessionId",
                table: "Payments",
                column: "StripeCheckoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StripePaymentIntentId",
                table: "Payments",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_Status_CreatedAtUtc",
                table: "Payments",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_RetryCount_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "Status", "RetryCount", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Status_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "TenantId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_RecipientEmail_IsRead_CreatedAtUtc",
                table: "NotificationLogs",
                columns: new[] { "RecipientEmail", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_IsActive_IsArchived",
                table: "Locations",
                columns: new[] { "TenantId", "IsActive", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId_StartUtc_Status",
                table: "Bookings",
                columns: new[] { "CustomerId", "StartUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffId_StartUtc_EndUtc_Status",
                table: "Bookings",
                columns: new[] { "StaffId", "StartUtc", "EndUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_LocationId_StartUtc_EndUtc",
                table: "Bookings",
                columns: new[] { "TenantId", "LocationId", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_ResourceId_StartUtc_EndUtc",
                table: "Bookings",
                columns: new[] { "TenantId", "ResourceId", "StartUtc", "EndUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkingHours_StaffId_DayOfWeek",
                table: "WorkingHours");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_IsActive_IsPublished_ModerationStatus_City_AverageR~",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerId_CreatedAtUtc",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TenantId_ModerationStatus_CreatedAtUtc",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_TenantId_ModerationStatus_Rating_CreatedAtUtc",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_CategoryId_IsActive_Price",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BookingId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CustomerId_CreatedAtUtc",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StripeCheckoutSessionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_StripePaymentIntentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_Status_CreatedAtUtc",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Status_RetryCount_CreatedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_TenantId_CreatedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_Status_CreatedAtUtc",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_RecipientEmail_IsRead_CreatedAtUtc",
                table: "NotificationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TenantId_IsActive_IsArchived",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId_StartUtc_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StaffId_StartUtc_EndUtc_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_LocationId_StartUtc_EndUtc",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_ResourceId_StartUtc_EndUtc",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHours_StaffId",
                table: "WorkingHours",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_TenantId_ModerationStatus",
                table: "Reviews",
                columns: new[] { "TenantId", "ModerationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Status",
                table: "Orders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId_Status",
                table: "Bookings",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffId",
                table: "Bookings",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_LocationId_StartUtc",
                table: "Bookings",
                columns: new[] { "TenantId", "LocationId", "StartUtc" });
        }
    }
}
