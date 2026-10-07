# Domain Schemas Specification

## 1. NotificationLog Entity
- `Id`: `Guid` (Primary Key)
- `TenantId`: `Guid` (Multi-tenant filter)
- `BookingId`: `Guid?`
- `CustomerId`: `Guid?`
- `RecipientEmail`: `string`
- `NotificationType`: `NotificationType` (`BookingConfirmation`, `BookingCancellation`, `BookingRescheduled`, `Reminder24h`, `Reminder1h`, `CustomMessage`)
- `Channel`: `NotificationChannel` (`Email`, `SMS`, `InApp`)
- `Status`: `NotificationStatus` (`Pending`, `Sent`, `Failed`)
- `Subject`: `string`
- `Body`: `string`
- `ErrorMessage`: `string?`

## 2. Payment Entity
- `Id`: `Guid` (Primary Key)
- `TenantId`: `Guid` (Multi-tenant filter)
- `BookingId`: `Guid?`
- `CustomerId`: `Guid?`
- `Amount`: `decimal` (Precision 18, 2)
- `Currency`: `string` (Default "USD")
- `PaymentType`: `PaymentType` (`Deposit`, `FullPayment`, `Refund`, `InStorePOS`)
- `Status`: `PaymentStatus` (`Pending`, `Completed`, `Failed`, `Refunded`, `PartiallyRefunded`)
- `PaymentMethod`: `PaymentMethod` (`CreditCard`, `Stripe`, `ApplePay`, `GooglePay`, `Cash`, `POS`)
