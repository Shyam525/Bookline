# Bookline REST API Specification

## Key Endpoints Overview

All operations endpoints require JWT Authentication (`Authorization: Bearer <token>`).

### Public Endpoints
- `GET /health` & `GET /ready`: System health probes.
- `POST /api/v1/auth/login`: Authentication.
- `GET /api/v1/availability/slots`: Realtime slot computation.

### Operational Endpoints
- **Locations**: `GET|POST|PUT /api/v1/locations`, `PUT /api/v1/locations/{id}/archive`
- **Services**: `GET|POST|PUT /api/v1/services`, `POST /api/v1/services/{id}/duplicate`, `GET|POST|PUT|DELETE /api/v1/services/categories`
- **Staff**: `GET|POST|PUT /api/v1/staff`, `PUT /api/v1/staff/{id}/services`, `PUT /api/v1/staff/{id}/working-hours`, `GET|POST|DELETE /api/v1/staff/{id}/time-off`
- **Customers**: `GET|POST|PUT /api/v1/customers`, `PUT /api/v1/customers/{id}/archive`

---
*Updated for Phase 6 - Phase 10 API completion.*
