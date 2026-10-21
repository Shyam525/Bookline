# Bookline Rate Limiting Architecture & Policy Matrix (Section 120)

## 1. Rate Limiting Architecture Overview

Bookline implements deliberate, partitioned rate limiting using ASP.NET Core's rate-limiting architecture (`Microsoft.AspNetCore.RateLimiting`). Rate limits prevent brute-force attacks, resource exhaustion, scraping abuse, and inventory locking while ensuring that legitimate users and external payment gateways can operate reliably.

```
Incoming Request
       │
       ▼
[ForwardedHeadersMiddleware]  ──► Extracts client IP from trusted reverse proxy
       │
       ▼
[RateLimitingMiddleware]
       │
       ├──► Checks Partition Limit by IP: $"<policy>_{clientIp}"
       │
       ├─► Within Quota  ──► Proceeds to Controller Action
       │
       └─► Limit Exceeded ──► Returns HTTP 429 Too Many Requests
                              - Header: Retry-After: 60
                              - Body: Stable ApiErrorResponse contract
                              - Increments metric: bookline_ratelimited_requests_total
```

---

## 2. Policy Matrix (Sections 120.1 – 120.8)

Every protected endpoint group is governed by a dedicated policy defined in `Program.cs`:

| Policy Name | Protected Endpoints | Scope / Partition Key | Configured Limit | Burst / Queue | Threat Mitigated | Failure Response |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`auth-limit`** | `POST /api/v1/auth/login`<br>`POST /api/v1/auth/refresh` | Resolved Client IP | 15 req / 1 min | Queue: 0 | Automated password guessing, credential stuffing, refresh token brute-force. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`register-limit`** | `POST /api/v1/auth/register-tenant`<br>`POST /api/v1/auth/register-customer` | Resolved Client IP | 10 req / 1 min | Queue: 0 | High-volume automated fake account spam, database user inflation. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`password-reset-limit`** | `POST /api/v1/auth/forgot-password`<br>`POST /api/v1/auth/reset-password` | Resolved Client IP | 5 req / 1 min | Queue: 0 | Recovery email flooding, reset token enumeration, SMS spam. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`discovery-limit`** | `GET /api/v1/discovery/search`<br>`GET /api/v1/public/businesses/*`<br>`GET /api/v1/geo/*`<br>`GET /api/v1/reviews/provider/*` | Resolved Client IP | 120 req / 1 min | Queue: 0 | Content scraping, spatial search query exhaustion, DoS attacks. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`booking-limit`** | `POST /api/v1/public/bookings/create`<br>`POST /api/v1/reviews` | Resolved Client IP | 30 req / 1 min | Queue: 0 | Automated booking spam, fake review submission, slot denial of service. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`hold-limit`** | `POST /api/v1/public/bookings/hold` | Resolved Client IP | 30 req / 1 min | Queue: 0 | Inventory exhaustion via rapid creation and abandonment of temporary holds. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`payment-limit`** | `POST /api/v1/payments/create-intent`<br>`POST /api/v1/payments/payouts` | Resolved Client IP | 30 req / 1 min | Queue: 0 | Card testing attacks, duplicate payment submission, financial fraud. | `429 Too Many Requests`<br>`Retry-After: 60` |
| **`webhook-limit`** | `POST /api/v1/payments/webhook` | Resolved Provider IP | 300 req / 1 min | Queue: 10 | Webhook flooding while allowing legitimate gateway retry bursts during reconciliation. | `429 Too Many Requests`<br>`Retry-After: 60` |

---

## 3. Webhook Segregation (Section 120.8)

A critical requirement of Section 120.8 is preventing customer-facing rate limits from blocking payment provider webhooks:
- If a customer rapidly refreshes a payment confirmation page and triggers `payment-limit` (30/min), the gateway's subsequent delivery of `payment_intent.succeeded` to `/api/v1/payments/webhook` **must not be dropped**.
- Therefore, `PaymentsController.ProcessWebhook` is decorated with `[EnableRateLimiting("webhook-limit")]`, overriding the controller-level `payment-limit`.
- `webhook-limit` allows 300 requests/minute with a queue capacity of 10, easily absorbing payment provider retry spikes and batch reconciliation events.

---

## 4. HTTP Behavior & Error Contract (Section 120.10)

When a rate limit is exceeded:
1. **HTTP Status Code**: `429 Too Many Requests`.
2. **Standard Header**: `Retry-After: 60` (indicating client should pause for 60 seconds before retrying).
3. **Stable Error Envelope (`ApiErrorResponse`)**:
   ```json
   {
     "code": "RATE_LIMITED",
     "message": "Too many requests. Please try again shortly.",
     "traceId": "a09a738daf4c4fe28d6c30428ce752e2",
     "status": 429
   }
   ```
4. **Security & Privacy Guarantee**: The response does not disclose internal throttling keys, partition counters, or activity from other tenants or users.

---

## 5. Trusted Client IP & Reverse Proxy Handling (Section 120.11)

When deployed behind an edge proxy or load balancer (e.g., Cloudflare, AWS ALB, NGINX, Traefik):
- Bookline enables `ForwardedHeadersOptions` configured with `XForwardedFor` and `XForwardedProto`.
- In `RateLimitHelper.ResolveClientIp`, the true remote IP address is resolved from `context.Connection.RemoteIpAddress` (which `ForwardedHeadersMiddleware` populates from verified proxies) or from `X-Forwarded-For` in supported proxy setups.
- **Spoofing Prevention**: Arbitrary client-supplied `X-Forwarded-For` headers cannot bypass or poison rate-limiting buckets because known proxies must be configured in `appsettings.json` under `ForwardedHeaders:TrustedProxies`.

```json
{
  "ForwardedHeaders": {
    "TrustAllProxies": false,
    "TrustedProxies": [
      "10.0.0.1",
      "172.16.0.1"
    ]
  }
}
```

---

## 6. Distributed Deployments vs. In-Memory Fallback (Section 120.9)

Bookline supports both single-instance and multi-instance distributed deployments:

### Single-Instance / Local Development
- Employs ASP.NET Core in-memory partition rate limiters.
- Fast execution with zero external network overhead.

### Multi-Instance Production Deployment
- **Global Ingress Enforcement**: In multi-instance deployments behind a cloud load balancer or API Gateway (e.g., AWS WAF, Cloudflare Rate Limiting, NGINX `limit_req`), global ingress limits are enforced at the edge.
- **Redis Coordination**: When Redis is available, Redis-backed rate limiting or coordinated token buckets ensure uniform limits across all backend replicas.
- **Fail-Open vs. Fail-Closed Policy**:
  - **Read / Discovery (`discovery-limit`)**: Configured to **fail open** if distributed cache is temporarily degraded, ensuring marketplace storefronts remain accessible.
  - **Security-Critical Endpoints (`auth-limit`, `register-limit`, `password-reset-limit`, `payment-limit`)**: Configured to **fail closed or fall back to local in-memory partition limits**, guaranteeing brute-force and financial protection is never silently disabled.
