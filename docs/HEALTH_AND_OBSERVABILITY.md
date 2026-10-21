# Bookline Health, Observability, and Telemetry (Sections 118 & 119)

## 1. Health Endpoints (Section 118)

Bookline implements an enterprise-grade health-check architecture conforming to ASP.NET Core standards, separating **Liveness** from **Readiness** to prevent destructive cascading restart loops in container orchestrators (e.g., Kubernetes, ECS, Docker Compose).

```
                      ┌─────────────────────────────────────────┐
                      │          HTTP Health Probes             │
                      └────────────────────┬────────────────────┘
                                           │
                    ┌──────────────────────┴──────────────────────┐
                    ▼                                             ▼
       ┌─────────────────────────┐                   ┌─────────────────────────┐
       │   GET /health (Liveness)│                   │   GET /ready (Readiness)│
       └────────────┬────────────┘                   └────────────┬────────────┘
                    │                                             │
      Is the process responsive?                  Are critical dependencies ready?
      Tag: [live]                                 Tag: [ready]
      Does NOT fail on downstream                 - PostgreSQL connectivity (SELECT 1)
      dependency downtime.                        - Redis coordination (Ping)
                                                  Bounded probe timeouts (1-2s).
```

### 1.1 Endpoint Semantics

| Endpoint | Probe Type | Tags | HTTP Success | HTTP Failure | Semantic Meaning |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`/health`** | Liveness | `live` | `200 OK` | `503 Service Unavailable` | Indicates whether the application process is alive and able to accept incoming HTTP requests. Downstream dependency failures do not fail this probe. |
| **`/ready`** | Readiness | `ready` | `200 OK` | `503 Service Unavailable` | Indicates whether the application is fully initialized and capable of serving user traffic. Detects database unavailability and required cache coordination states. |

### 1.2 Bounded Dependency Probes

All health checks enforce strict timeouts and non-disruptive safe operations:
- **PostgreSQL Connectivity Check (`PostgreSqlHealthCheck`)**:
  - **Timeout**: Enforced 2,000 ms timeout via `CancellationTokenSource`.
  - **Probe Query**: Lightweight `CanConnectAsync` and safe `SELECT 1;` when relational.
  - **Safety Guarantees**: Never triggers full table scans, analytics queries, or unbounded retries.
- **Redis Coordination Check (`RedisHealthCheck`)**:
  - **Timeout**: Enforced 1,000 ms timeout on `database.PingAsync()`.
  - **Optional Mode Support**: If Redis is optional (`Redis:Required: false`), missing Redis or transient disconnects report `Healthy` or `Degraded` with message: `"Operating in in-memory coordination mode (Redis optional)"`. Traffic is not blocked unnecessarily.
  - **Safety Guarantees**: Health probes never execute write commands or dummy charge operations.

### 1.3 Sanitized Public Response Contract

To strictly adhere to Section 118 security requirements, public health endpoints **never leak**:
- Connection strings, hostnames, ports, or credentials.
- SQL queries, database usernames, or schema definitions.
- Internal exception messages or stack traces.
- Private environment variables or cloud provider metadata.

#### Sample Liveness Response (`GET /health`)
```json
{
  "status": "Healthy",
  "timestamp": "2026-10-21T12:00:00.0000000Z",
  "totalDurationMs": 0.42,
  "checks": [
    {
      "name": "liveness",
      "status": "Healthy",
      "description": "Bookline application process is alive and responsive.",
      "durationMs": 0.31
    }
  ]
}
```

#### Sample Readiness Response (`GET /ready`)
```json
{
  "status": "Healthy",
  "timestamp": "2026-10-21T12:00:00.0000000Z",
  "totalDurationMs": 2.15,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connectivity verified.",
      "durationMs": 1.45
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": "Redis coordination engine connected (ping: 0.8ms).",
      "durationMs": 0.70
    }
  ]
}
```

---

## 2. Observability & Tracing Architecture (Section 119)

### 2.1 Structured Logging Properties

All logs emitted across HTTP requests, background workers, outbox dispatchers, and external integrations use structured logging enriched with standard operational properties:

| Property | Description | Example |
| :--- | :--- | :--- |
| `Timestamp` | ISO 8601 UTC timestamp of log event | `2026-10-21T12:00:00.000Z` |
| `Severity` | Log level (`Information`, `Warning`, `Error`, `Critical`) | `Information` |
| `Environment` | Hosting environment name | `Development`, `Production` |
| `Service` | Application component identity | `Bookline.Api`, `Bookline.Worker` |
| `OperationName` | Named logical operation | `CreateBooking`, `ProcessOutboxEvent` |
| `CorrelationId` | Causal correlation identifier propagated across tiers | `a09a738daf4c4fe28d6c30428ce752e2` |
| `TraceId` | HTTP trace identifier | `0HNPF89L7NIC1` |
| `EventId` | Outbox message ID or domain event ID | `11111111-2222-3333-4444-555555555555` |
| `TenantId` | Multi-tenant organization partition ID | `00000000-0000-0000-0000-000000000001` |
| `AttemptCount` | Execution or retry attempt sequence number | `1`, `2`, `5` |
| `DurationMs` | Elapsed execution duration in milliseconds | `42.5` |
| `ErrorCategory` | Categorical classification of failure | `SocketException`, `TimeoutException` |

### 2.2 End-to-End Correlation Context Propagation

Correlation IDs are created or accepted at the HTTP boundary via `CorrelationIdMiddleware`:
1. **HTTP Inbound**: Reads `X-Correlation-ID` header. If absent, generates a 32-character hexadecimal GUID.
2. **HTTP Outbound**: Injects `X-Correlation-ID` into every HTTP response header.
3. **Outbox Persistence**: When creating domain outbox events, `CorrelationId` and `TraceId` are saved on `OutboxMessage`.
4. **Worker Dispatch**: `OutboxAndReminderWorker` extracts the causal `CorrelationId` and pushes it into the background logger scope (`_logger.BeginScope`), ensuring every notification, email delivery, and audit log retains full traceability back to the initiating user request.
5. **Customer Error Responses**: When an error occurs, `ApiErrorResponse.TraceId` contains the real correlation identifier for customer support reference without exposing internal exceptions.

### 2.3 Centralized Privacy & Redaction

Bookline enforces centralized automated data redaction through `SensitiveDataMaskingEnricher` registered in Serilog. Developers do not need to rely on manual string stripping:
- **Redacted Field Names**: Any property matching `password`, `newpassword`, `oldpassword`, `passwordhash`, `token`, `accesstoken`, `refreshtoken`, `resettoken`, `clientsecret`, `secret`, `apikey`, `webhooksecret`, `cardnumber`, `cvv`, `cvc`, `authorization`, `privatekey` is replaced with `"[REDACTED]"`.
- **Embedded Tokens**: Regular expressions automatically detect and sanitize `Bearer <JWT>` tokens and credit card numbers embedded within string values.
- **GPS Privacy**: Precise GPS coordinates are not logged in plain text without verified operational justification.

### 2.4 Worker & Outbox Lifecycle Telemetry

`OutboxAndReminderWorker` provides operational observability across key lifecycle events:
- **Job Started**: Worker suite initialization and active recurring job registration.
- **Batch Claimed**: Logged with batch size and the calculated age of the oldest pending event in seconds.
- **Item Processed**: Duration in ms, event type, tenant ID, and attempt count.
- **Retry Scheduled**: Warning log when transient failure occurs, recording attempt number and error category.
- **Permanently Failed**: Error log with attempt count (after 5 failed attempts) marking event status as `Failed`.
- **Batch Completed**: Summary of processed count, failed count, and total batch duration in ms.
- **Shutdown Handled**: Clean graceful shutdown logging on SIGTERM / cancellation token activation.

### 2.5 Operational Metrics (`BooklineMetrics`)

Using .NET `System.Diagnostics.Metrics.Meter` (`"Bookline.Observability"`), telemetry agents (OpenTelemetry, Prometheus) collect low-cardinality time-series data:

| Metric Name | Instrument | Description | Labels |
| :--- | :--- | :--- | :--- |
| `bookline_ratelimited_requests_total` | Counter | Count of rejected rate-limited requests | `path` |
| `bookline_booking_conflicts_total` | Counter | Count of concurrent slot reservation conflicts | `outcome` |
| `bookline_outbox_events_processed_total` | Counter | Successful outbox event deliveries | `event_type`, `outcome` |
| `bookline_outbox_events_failed_total` | Counter | Failed outbox event processing attempts | `event_type`, `error_category` |
| `bookline_notification_deliveries_total` | Counter | Customer and provider notification dispatches | `channel`, `outcome` |
| `bookline_webhook_processed_total` | Counter | Inbound payment gateway webhooks processed | `event_type` |
| `bookline_healthcheck_failures_total` | Counter | Health probe failure occurrences | `check_name` |
| `bookline_outbox_batch_duration_ms` | Histogram | Outbox processing cycle duration in ms | N/A |
