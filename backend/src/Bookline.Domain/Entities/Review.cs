using Bookline.Domain.Common;
using Bookline.Domain.Enums;

namespace Bookline.Domain.Entities;

public class Review : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? OrderId { get; set; }
    public string CustomerName { get; set; } = "Verified Customer";
    public int Rating { get; set; } = 5;
    public string? Title { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? ProviderResponse { get; set; }
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Approved;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }

    public void Respond(string response)
    {
        ProviderResponse = response;
        RespondedAtUtc = DateTime.UtcNow;
    }

    public void Moderate(ModerationStatus status)
    {
        ModerationStatus = status;
    }
}
