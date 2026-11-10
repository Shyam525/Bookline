# Bookline — Environment Variables & Configuration Reference

This document provides a comprehensive specification of all environment variables and configuration settings used across Bookline API backend and frontend.

---

## 1. Environment & Runtime Mode

| Variable Name | Required | Default | Allowed Values | Description |
|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | **Yes** | `Production` | `Production`, `Development`, `Staging` | Controls environment behavior. **CRITICAL**: When set to `Production`, `MarketplaceDbSeeder` is strictly locked out and will throw an exception if invoked. |
| `ASPNETCORE_URLS` | Optional | `http://+:5168` | Valid URL list | HTTP/HTTPS listener bindings. In containerized environments, defaults to port `5168` or `8080`. |

---

## 2. Database (PostgreSQL)

Bookline relies on PostgreSQL (version 15+) with the `btree_gist` and `postgis` extensions enabled.

| Variable Name | Key in `appsettings.json` | Required | Default / Example | Description |
|---|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | **Yes** | `Host=postgres;Port=5432;Database=bookline;Username=postgres;Password=secret;Include Error Detail=false;` | PostgreSQL primary connection string. Set `Include Error Detail=false` in production. |
| `POSTGRES_TEST_CONNECTION` | - | No | `Host=localhost;Port=5432;...` | Used exclusively by test harnesses and CI pipelines. |

---

## 3. Distributed Cache (Redis)

Redis is utilized for distributed caching, session tracking, and rate limiter storage.

| Variable Name | Key in `appsettings.json` | Required | Default / Example | Description |
|---|---|---|---|---|
| `RedisSettings__Configuration` | `RedisSettings:Configuration` | **Yes** (Prod) | `localhost:6379,abortConnect=false` | Redis connection endpoint string. `abortConnect=false` ensures application resiliently retries connection. |
| `RedisSettings__InstanceName` | `RedisSettings:InstanceName` | Optional | `bookline:` | Key prefix namespace applied to all cache keys. |

---

## 4. Authentication & Security (JWT)

| Variable Name | Key in `appsettings.json` | Required | Minimum Security Requirement | Description |
|---|---|---|---|---|
| `JwtSettings__Secret` | `JwtSettings:Secret` | **Yes** | **>= 32 bytes (256-bit cryptographically random)** | HMAC-SHA256 signature key. **Must never use default test secrets in production.** |
| `JwtSettings__Issuer` | `JwtSettings:Issuer` | **Yes** | `Bookline.Api` | Token issuer domain claim (`iss`). |
| `JwtSettings__Audience` | `JwtSettings:Audience` | **Yes** | `Bookline.Client` | Token audience domain claim (`aud`). |
| `JwtSettings__ExpiryMinutes` | `JwtSettings:ExpiryMinutes` | Optional | `120` | Access token lifespan in minutes before refresh is needed. |

---

## 5. Transactional Outbox Worker

| Variable Name | Key in `appsettings.json` | Required | Default | Description |
|---|---|---|---|---|
| `OutboxWorker__IntervalSeconds` | `OutboxWorker:IntervalSeconds` | Optional | `5` | Polling frequency for unprocessed outbox messages in seconds. |
| `OutboxWorker__BatchSize` | `OutboxWorker:BatchSize` | Optional | `20` | Maximum messages claimed per polling transaction. |
| `OutboxWorker__MaxRetries` | `OutboxWorker:MaxRetries` | Optional | `5` | Maximum delivery attempts before marking a message as `DeadLetter`. |

---

## 6. Notification Carrier Gateways

Bookline uses a provider abstraction (`INotificationTransport`). In development, simulated transports run locally without external calls. In production, configure valid credentials.

### Email (SMTP / MailKit)
| Variable Name | Key in `appsettings.json` | Required | Default | Description |
|---|---|---|---|---|
| `SmtpSettings__Host` | `SmtpSettings:Host` | **Yes** (Prod) | `smtp.sendgrid.net` | Outgoing SMTP server hostname. (For local dev: `bookline-mailpit`). |
| `SmtpSettings__Port` | `SmtpSettings:Port` | **Yes** (Prod) | `587` | Outgoing SMTP port (587 for STARTTLS, 465 for SSL, 1025 for Mailpit). |
| `SmtpSettings__UseSsl` | `SmtpSettings:UseSsl` | Optional | `false` | Enable direct SSL on port 465. |
| `SmtpSettings__Username` | `SmtpSettings:Username` | **Yes** (Prod) | `apikey` | SMTP account username. |
| `SmtpSettings__Password` | `SmtpSettings:Password` | **Yes** (Prod) | `[REDACTED]` | SMTP account secret/password. |
| `SmtpSettings__SenderEmail` | `SmtpSettings:SenderEmail` | **Yes** | `notifications@bookline.in` | Verified From address for outgoing emails. |
| `SmtpSettings__SenderName` | `SmtpSettings:SenderName` | Optional | `Bookline Notifications` | Display name for outgoing transactional emails. |

### SMS / WhatsApp Gateway
| Variable Name | Key in `appsettings.json` | Required | Default | Description |
|---|---|---|---|---|
| `NotificationSettings__SmsProvider` | `NotificationSettings:SmsProvider` | Optional | `Simulated` | Options: `Simulated`, `Twilio`, `Gupshup`, `Msg91`. |
| `NotificationSettings__SmsApiKey` | `NotificationSettings:SmsApiKey` | Optional | `""` | API Key for production SMS gateway. |
| `NotificationSettings__WhatsappApiKey` | `NotificationSettings:WhatsappApiKey` | Optional | `""` | API Key for WhatsApp Business Cloud API. |

---

## 7. Rate Limiting Limits

Configured in `appsettings.json` under `RateLimiting`:

```json
"RateLimiting": {
  "Auth": { "PermitLimit": 15, "WindowMinutes": 1 },
  "Hold": { "PermitLimit": 30, "WindowMinutes": 1 },
  "Public": { "PermitLimit": 120, "WindowMinutes": 1 },
  "Admin": { "PermitLimit": 60, "WindowMinutes": 1 }
}
```

---

## 8. Frontend Build Variables (`frontend/.env.production`)

| Variable Name | Required | Example | Description |
|---|---|---|---|
| `VITE_API_BASE_URL` | Optional | `""` (Empty for relative root) | Base API endpoint prefix. Leave empty if frontend is served by ASP.NET Core static files at `/`. |
| `VITE_APP_ENV` | Optional | `production` | Build environment descriptor. |
| `VITE_SUPPORT_EMAIL` | Optional | `support@bookline.in` | Pilot support contact displayed in footer. |

---

## 9. Production Secrets Handling Rules

1. **Never Commit Secrets**: Do not check secrets into git repository.
2. **Environment Variable Injection**: Inject secrets via Docker Secrets, AWS Secrets Manager, Azure Key Vault, or Kubernetes Secrets.
3. **Double Underscore Mapping**: In ASP.NET Core, nested JSON configurations correspond to double underscores in environment variable keys (e.g., `JwtSettings__Secret` maps to `JwtSettings:Secret`).

