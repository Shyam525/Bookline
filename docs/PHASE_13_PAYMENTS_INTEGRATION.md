# Phase 13 — Payments & POS Integration

## Overview
Phase 13 establishes financial management, online Stripe checkout session generation, in-store Point-of-Sale (POS) transaction recording, and refund processing for tenant organizations.

## Key Features
1. **Financial Transactions Entity**: `Payment` domain model tracking deposits, full payments, in-store POS receipts, and refunds.
2. **Stripe Integration**: Online deposit checkout session builder and payment intent lifecycle.
3. **In-Store POS Recording**: Cash, card, and terminal payment capture updating customer lifetime spend totals (`TotalSpentAmount`).
4. **Financial Workspace**: `PaymentsPage.tsx` with revenue KPI cards, transaction filter data table, and refund processing drawer.
