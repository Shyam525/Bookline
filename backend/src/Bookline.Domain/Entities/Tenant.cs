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
    public int OnboardingStep { get; set; } = 1;
    public bool IsOnboardingCompleted { get; set; } = false;
    public int HoldDurationMinutes { get; set; } = 5;
    public int MinimumNoticeHours { get; set; } = 2;
    public int BookingHorizonDays { get; set; } = 30;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
