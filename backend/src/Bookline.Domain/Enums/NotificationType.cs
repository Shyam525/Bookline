namespace Bookline.Domain.Enums;

public enum NotificationType
{
    BookingConfirmation = 1,
    BookingCancellation = 2,
    BookingRescheduled = 3,
    Reminder24h = 4,
    Reminder1h = 5,
    CustomMessage = 6,
    Reminder2h = 7,
    OrderUpdate = 8,
    PaymentReceipt = 9,
    SystemAlert = 10
}
