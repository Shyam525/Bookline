namespace Bookline.Domain.Constants;

public static class Roles
{
    // Canonical MVP Roles
    public const string PlatformAdmin = "PlatformAdmin";
    public const string BusinessOwner = "BusinessOwner";
    public const string BusinessStaff = "BusinessStaff";
    public const string Customer = "Customer";

    // Standard Aliases & Granular Roles
    public const string Owner = "Owner";
    public const string Staff = "Staff";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Receptionist = "Receptionist";
    public const string Viewer = "Viewer";
    public const string Provider = "Provider";

    public static bool IsBusinessOwner(string? role) =>
        string.Equals(role, BusinessOwner, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Owner, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Provider, StringComparison.OrdinalIgnoreCase);

    public static bool IsBusinessStaff(string? role) =>
        string.Equals(role, BusinessStaff, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Staff, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Manager, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Receptionist, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Viewer, StringComparison.OrdinalIgnoreCase) ||
        IsBusinessOwner(role);

    public static bool IsPlatformAdmin(string? role) =>
        string.Equals(role, PlatformAdmin, StringComparison.OrdinalIgnoreCase);

    public static bool IsCustomer(string? role) =>
        string.Equals(role, Customer, StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrEmpty(role);

    public static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return Customer;
        if (IsPlatformAdmin(role)) return PlatformAdmin;
        if (IsBusinessOwner(role)) return BusinessOwner;
        if (IsBusinessStaff(role)) return BusinessStaff;
        return Customer;
    }
}
