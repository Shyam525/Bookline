# Bookline — Monitoring, Observability & Alerting Guide

This document defines the metrics, health checks, alerting thresholds, and distributed tracing protocols implemented in Bookline.

---

## 1. Health & Readiness Probes

Bookline exposes two dedicated endpoints for container orchestrators (Kubernetes / Docker Compose) and uptime monitors:

| Probe Endpoint | Purpose | Checks Performed | Success Response | Failure Response |
|---|---|---|---|---|
| `GET /health` | **Liveness Probe** | Kestrel web server is accepting HTTP requests. | `HTTP 200 "Healthy"` | Container restart triggered. |
| `GET /ready` | **Readiness Probe** | 1. PostgreSQL DB ping & query.<br>2. Redis connectivity & latency.<br>3. Outbox message backlog lag check. | `HTTP 200 {"status": "Healthy", ...}` | `HTTP 503 Service Unavailable`. Traffic diverted from pod. |

### Sample Readiness Check JSON Output
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0124500",
  "entries": {
    "npgsql": {
      "data": {},
      "duration": "00:00:00.0041200",
      "status": "Healthy"
    },
    "redis": {
      "data": {},
      "duration": "00:00:00.0028900",
      "status": "Healthy"
    },
    "outbox_backlog": {
      "data": { "backlog_count": 0, "oldest_message_age_seconds": 0 },
      "duration": "00:00:00.0054400",
      "status": "Healthy"
    }
  }
}
```

---

## 2. Distributed Log Correlation (`X-Correlation-ID`)

Every incoming HTTP request is assigned a unique correlation ID via `CorrelationIdMiddleware`:

- If the client or reverse proxy supplies an `X-Correlation-ID` header, that value is validated and attached to the request context.
- If missing, a new cryptographically secure GUID is generated.
- The `X-Correlation-ID` is echoed in all response headers and enriched into Serilog's `LogContext`.

### Log Querying Example (Grafana Loki / Datadog)
To trace a customer booking from frontend hold request through outbox notification dispatch:

```logql
{app="bookline-api"} |= "7f9a12c4-33b8-4d2a-9e51-098e7d23a1f4"
```

All log entries emitted during that request lifecycle (SQL queries, validation results, outbox insert, email transport dispatch) share the identical `CorrelationId`.

---

## 3. Key Operational Metrics & Alerting Thresholds

| Metric Name | Warning Threshold | Critical Threshold | Action Required |
|---|---|---|---|
| **API Error Rate (5xx)** | > 0.5% for 3 mins | > 2.0% for 1 min | Inspect error logs filtered by `Level=Error`. Check database connectivity. |
| **P95 Request Latency** | > 400ms for 5 mins | > 1000ms for 2 mins | Check database slow queries (`pg_stat_statements`) and Redis latency. |
| **Outbox Message Backlog** | > 20 unprocessed messages | > 50 messages or age > 5 min | Check outbox worker status in `/ready`. Check SMTP / SMS gateway connectivity. |
| **Outbox Dead Letter Count**| > 1 message | > 5 messages | Inspect `OutboxMessages` where `Status = 'DeadLetter'` for carrier auth/payload errors. |
| **PostgreSQL Connection Pool** | > 75% capacity | > 90% capacity | Increase `MaxPoolSize` or scale read replicas. Check for connection leaks. |
| **Rate Limit Throttles (429)**| > 20 req/min | > 100 req/min | Potential brute-force attack or misconfigured client scraper. Check IP reputation. |

---

## 4. Database Performance Monitoring

Monitor PostgreSQL performance using `pg_stat_statements`:

```sql
-- Top 5 slowest queries by mean execution time
SELECT query, calls, total_exec_time / calls AS avg_ms, rows / calls AS avg_rows
FROM pg_stat_statements
ORDER BY avg_ms DESC
LIMIT 5;

-- Check lock contention on Bookings and Holds
SELECT relation::regclass, mode, granted, count(*) 
FROM pg_locks 
GROUP BY relation, mode, granted;
```

---

## 5. Grafana Dashboard Recommended Panels

1. **Traffic & Latency**: Requests per second (RPS), p50/p95/p99 response time broken down by route.
2. **Booking Funnel**: Active Holds Created -> Holds Confirmed -> Conversion Rate.
3. **Outbox Engine Health**:
   - Messages processed per minute.
   - Current backlog count (`Status = 'Pending'`).
   - Retries count (`RetryCount > 0`).
4. **Database Diagnostics**: Active connections, cache hit ratio, transaction commit/rollback ratio.
5. **Notification Carrier Throughput**: Email deliveries succeeded vs SMS/WhatsApp deliveries succeeded.

