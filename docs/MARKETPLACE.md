# BOOKLINE MARKETPLACE PLATFORM ARCHITECTURE

## 1. Overview
Bookline is a production-grade multi-vendor marketplace connecting consumers with local service businesses and boutique retail products.
Unlike single-tenant salon or booking apps, Bookline is designed around a three-tier actor model:
1. **Customer**: One universal account discovering businesses across categories, booking services, purchasing retail items, tracking cross-provider orders, and publishing verified reviews.
2. **Provider**: Independent business owners managing one or more operating organizations/branches with tenant isolation, specialists, service catalogs, inventory, and automated payout settlements.
3. **Platform Administrator**: Bookline marketplace governance enforcing business verification, KYC, review moderation, transactional take-rate economics (10%), and system observability.

## 2. Multi-Vendor Model
- **Cross-Vendor Discovery**: Global catalog indexed with PostGIS geography, full-text ranking, and slot availability.
- **Provider-Aware Cart**: Single-provider retail checkout prevents incompatible physical fulfillment across unrelated stores.
- **Deterministic Economics**: 
  - Customer Pays: $100.00
  - Bookline Take-Rate (10%): $10.00
  - Provider Payable (90%): $90.00
- **Automated Payout Pipeline**: Retains available vs. disbursed lifetime balances per vendor.

## 3. Technology Stack
- **Backend**: ASP.NET Core 8, C#, Clean Architecture, EF Core 8, PostgreSQL, PostGIS, Redis, NodaTime.
- **Frontend**: React 18, TypeScript, Vite, Tailwind CSS, Lucide Icons.
- **Testing**: xUnit, MediatR, FluentValidation, Integration test harness.
