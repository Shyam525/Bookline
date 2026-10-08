using Bookline.Domain.Enums;

namespace Bookline.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public string BusinessType { get; set; } = "Salon";
    public string Currency { get; set; } = "USD";
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = "Ahmedabad";
    public string State { get; set; } = "Gujarat";
    public string Country { get; set; } = "India";
    public string PostalCode { get; set; } = string.Empty;
    public double Latitude { get; set; } = 23.0225;
    public double Longitude { get; set; } = 72.5714;
    public string Category { get; set; } = "Beauty & Wellness";
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Verified;
    public double AverageRating { get; set; } = 4.8;
    public int ReviewCount { get; set; } = 0;
    public DepositType DepositType { get; set; } = DepositType.None;
    public decimal DepositAmount { get; set; } = 0.00m;
    public decimal CommissionRatePercentage { get; set; } = 10.00m;
    public decimal PendingPayoutBalance { get; set; } = 0.00m;
    public decimal AvailablePayoutBalance { get; set; } = 0.00m;
    public decimal PaidOutBalance { get; set; } = 0.00m;
    public bool IsPublished { get; set; } = true;
    public int OnboardingStep { get; set; } = 1;
    public bool IsOnboardingCompleted { get; set; } = false;
    public int HoldDurationMinutes { get; set; } = 5;
    public int MinimumNoticeHours { get; set; } = 2;
    public int BookingHorizonDays { get; set; } = 30;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
