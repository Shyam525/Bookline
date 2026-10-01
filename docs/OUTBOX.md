# Transactional Outbox Pattern

## Architectural Pattern

To guarantee that appointment creation and notification publishing are strictly atomic, Bookline implements the **Transactional Outbox Pattern**.

```text
Database Transaction
  ├── Insert Booking Entity
  └── Insert OutboxMessage (Status = 'Pending')
        ↓ (Commit)
Background OutboxProcessorJob
  ├── Query Pending Outbox Messages
  ├── Dispatch Notifications (Email via Mailpit / SMTP)
  └── Update OutboxMessage (Status = 'Processed')
```

## Resilience & Dead-Letter Handling

- **Atomic Writes**: Notification events are saved in the same SQL transaction as the booking entity.
- **Retries**: Transient delivery failures trigger incremental retries (up to 5 attempts).
- **Dead-Letter State**: Messages exceeding 5 failed attempts transition to `Failed` status with detailed error logging.
