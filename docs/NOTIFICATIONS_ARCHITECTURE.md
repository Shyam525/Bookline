# Notifications Architecture & Outbox Delivery

## Messaging Delivery Pipeline
1. **Event Trigger**: Appointment created, rescheduled, or cancelled by customer or staff.
2. **Preference Evaluation**: `NotificationService` inspects `NotificationSetting` for the tenant.
3. **Outbox Log Creation**: `NotificationLog` is written with status `Pending`.
4. **Transport Adapter Dispatch**:
   - `Email`: Dispatched via `IEmailSender` (MailKit / SMTP).
   - `SMS`: Dispatched via SMS Gateway adapter.
5. **Log Status Finalization**: Status updated to `Sent` with timestamp `SentAtUtc` or `Failed` with `ErrorMessage`.
