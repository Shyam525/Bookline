namespace Bookline.Domain.Constants;

public static class NotificationEvents
{
    public const string AppointmentCreated = "AppointmentCreated";
    public const string AppointmentConfirmed = "AppointmentConfirmed";
    public const string AppointmentCancelled = "AppointmentCancelled";
    public const string AppointmentRescheduled = "AppointmentRescheduled";
    public const string ReminderDue = "ReminderDue";
    public const string OrderCreated = "OrderCreated";
    public const string PaymentSucceeded = "PaymentSucceeded";
    public const string PaymentFailed = "PaymentFailed";
    public const string RefundCreated = "RefundCreated";
    public const string ReviewCreated = "ReviewCreated";
}
