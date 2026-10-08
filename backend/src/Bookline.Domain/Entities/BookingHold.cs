using Bookline.Domain.Common;

namespace Bookline.Domain.Entities;

public class BookingHold : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? LocationId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid StaffId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string HoldToken { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset ExpiresAtUtc { get; set; } = DateTimeOffset.UtcNow.AddMinutes(5);
    public bool IsReleased { get; set; } = false;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsActive => !IsReleased && ExpiresAtUtc > DateTimeOffset.UtcNow;

    public void Release()
    {
        IsReleased = true;
    }
}
