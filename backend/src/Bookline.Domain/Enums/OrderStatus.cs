namespace Bookline.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Processing = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5,
    Refunded = 6
}
