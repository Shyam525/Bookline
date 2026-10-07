# API Endpoints Reference

## Notifications API (`/api/v1/notifications`)
- `GET /api/v1/notifications/logs`: Retrieve paginated notification delivery history (`status`, `channel` filters).
- `GET /api/v1/notifications/settings`: Retrieve tenant notification configuration.
- `PUT /api/v1/notifications/settings`: Update tenant notification settings & sender profile.
- `POST /api/v1/notifications/send-test`: Dispatch live test notification.

## Payments API (`/api/v1/payments`)
- `GET /api/v1/payments`: Retrieve paginated transaction logs (`status`, `type` filters).
- `GET /api/v1/payments/summary`: Retrieve revenue, deposits, and refund KPI summary.
- `POST /api/v1/payments/checkout-session`: Generate online Stripe checkout deposit session.
- `POST /api/v1/payments/refund`: Process full/partial transaction refund.
- `POST /api/v1/payments/pos`: Record in-store terminal/cash POS transaction.
