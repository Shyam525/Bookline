# Notifications & Event Dispatching

Bookline implements an event-driven notification architecture:
- Transactional Outbox commits notifications atomically with domain state changes.
- Background worker processes pending outbox items.
- MailKit SMTP provider delivers emails locally to Mailpit (`localhost:8026`).
