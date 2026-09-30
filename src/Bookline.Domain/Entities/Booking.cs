namespace Bookline.Domain.Entities;

using Bookline.Domain.Common;
using Bookline.Domain.Exceptions;

public class Booking : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? ReminderSentAtUtc { get; private set; }

    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public Customer Customer { get; set; } = null!;

    public Booking() { }

    public Booking(Guid tenantId, Guid staffId, Guid serviceId, Guid customerId, DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        TenantId = tenantId;
        StaffId = staffId;
        ServiceId = serviceId;
        CustomerId = customerId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Status = BookingStatus.Pending;
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.Confirmed);
        }
        Status = BookingStatus.Confirmed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.Completed);
        }
        Status = BookingStatus.Completed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Confirmed)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.Cancelled);
        }
        Status = BookingStatus.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkNoShow()
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.NoShow);
        }
        Status = BookingStatus.NoShow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkReminderSent(DateTimeOffset sentAtUtc)
    {
        ReminderSentAtUtc = sentAtUtc;
        UpdatedAtUtc = sentAtUtc;
    }
}
