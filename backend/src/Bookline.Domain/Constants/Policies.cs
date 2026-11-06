namespace Bookline.Domain.Constants;

public static class Policies
{
    public const string RequirePlatformAdmin = "RequirePlatformAdmin";
    public const string RequireBusinessOwner = "RequireBusinessOwner";
    public const string RequireBusinessStaff = "RequireBusinessStaff";
    public const string RequireBusinessAccess = "RequireBusinessAccess";
    public const string RequireCustomer = "RequireCustomer";

    // Operational Policies
    public const string ManageBookings = "ManageBookings";
    public const string ManageServices = "ManageServices";
    public const string ManageStaff = "ManageStaff";
    public const string ManageFinancials = "ManageFinancials";
}
