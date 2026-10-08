namespace Bookline.Domain.Entities;

using Bookline.Domain.Common;
using Bookline.Domain.Exceptions;

public class Booking : TenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookingReference { get; set; } = $"BL-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? LocationId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public string? CustomerNotes { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DepositPaid { get; set; }
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
        BookingReference = $"BL-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
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

    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.CheckedIn);
        }
        Status = BookingStatus.CheckedIn;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != BookingStatus.Confirmed && Status != BookingStatus.CheckedIn)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.Completed);
        }
        Status = BookingStatus.Completed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null, string? cancelledBy = null)
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Confirmed && Status != BookingStatus.CheckedIn)
        {
            throw new InvalidStatusTransitionException(Status, BookingStatus.Cancelled);
        }
        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        CancelledBy = cancelledBy;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Reschedule(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Confirmed)
        {
            throw new InvalidStatusTransitionException(Status, Status);
        }
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("Booking end time must be after its start time.", nameof(endUtc));
        }

        StartUtc = startUtc;
        EndUtc = endUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkNoShow()
    {
        if (Status != BookingStatus.Confirmed && Status != BookingStatus.CheckedIn)
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
