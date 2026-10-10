# Commerce & Inventory Engine

## 1. Overview
Bookline supports unified service booking and physical commerce. Customers can browse retail products associated with providers, add items to their unified shopping cart, and purchase online or in-store.

## 2. Inventory Invariants & Concurrency Safety (Section 133)
- **Stock Quantity Invariant**: Inventory can **never** become negative (`StockQuantity >= 0`).
- **Concurrent Purchase Scenario**:
  - Scenario: Product stock is `1`.
  - Two customers attempt to purchase the single item concurrently.
  - Exactly one purchase succeeds: `product.Purchase(1) == true`, reducing `StockQuantity` from 1 to 0.
  - The second purchase fails: `product.Purchase(1) == false`, rejected with `"Insufficient inventory for product"`.
  - Database maintains `StockQuantity = 0`, never `-1`.

## 3. Order Lifecycle & Status Transitions
- Order Statuses: `Pending` -> `Paid` -> `Processing` -> `Ready` -> `Completed` / `Cancelled`.
- Mixed state seeding (Section 125): Seeds representative deterministic orders in pending, processing, completed, and cancelled states.
- Customer order tracking: Real-time progress tracker with delivery address and line items.
