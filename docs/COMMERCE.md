# Commerce & Inventory Engine

## 1. Overview
Bookline supports unified service booking and physical commerce. Customers can browse retail products associated with providers, add items to their unified shopping cart, and purchase online or in-store.

## 2. Section 26: Product Management Domain

A product represents a purchasable physical item, not a time-based appointment.

### 2.1 Product Entity Structure
- **Id**: `Guid` (Unique identifier)
- **BusinessId / TenantId**: `Guid` (Multi-tenant business ownership boundary)
- **Name**: `string` (Product title)
- **Slug**: `string` (SEO-friendly identifier unique within the business)
- **Description**: `string` (Formulation details, usage instructions, ingredients)
- **Price**: `decimal` (Validated non-negative, precision 18, 2)
- **Currency**: `string` (3-letter ISO code e.g., USD, INR, EUR)
- **SKU**: `string?` (Stock Keeping Unit, enforced unique within the business scope)
- **CategoryId**: `Guid?` (Foreign key to `ProductCategory`)
- **Images**: `ICollection<ProductImage>` (Normalized image collection with `Url`, `AltText`, `SortOrder`, and `IsPrimary`)
- **Inventory Tracking**:
  - `TrackInventory`: `bool` (Whether inventory count is tracked or unlimited)
  - `StockQuantity`: `int` (Total physical units in stock, invariant `StockQuantity >= 0`)
  - `ReservedQuantity`: `int` (Units currently reserved during checkout)
  - `SoldQuantity`: `int` (Units fulfilled)
  - `AvailableQuantity`: `int` (`Math.Max(0, StockQuantity - ReservedQuantity)` when tracked, `int.MaxValue` when untracked)
  - `LowStockThreshold`: `int` (Alert threshold for reordering)
  - `AllowBackorders`: `bool` (Default `false`)
- **Active**: `bool` (`IsActive` state for operational status)
- **PurchasableOnline**: `bool` (`IsPurchasableOnline` flag distinguishing omnichannel catalog items from in-store-only merchandise)
- **Published**: `bool` (`IsPublished` storefront publication state distinguishing live items from provider drafts)
- **CreatedAt**: `DateTime` (`CreatedAtUtc`)
- **UpdatedAt**: `DateTime?` (`UpdatedAtUtc`)
- **RowVersion**: `byte[]` (Optimistic concurrency token preventing race conditions and overselling)

### 2.2 Product Lifecycle States Handling
The system deterministically categorizes and handles products across five explicit operational states:

1. **Active and In Stock (`ActiveInStock`)**:
   - `IsActive = true`, `IsPublished = true`, `IsInStock = true`, `IsPurchasableOnline = true`.
   - Discoverable on public storefront and online checkout enabled.
2. **Active but Out of Stock (`ActiveOutOfStock`)**:
   - `IsActive = true`, `IsPublished = true`, `IsInStock = false`, `IsPurchasableOnline = true`.
   - Visible in provider catalog with "Out of Stock" badge. Online checkout disabled until restocked.
3. **Inactive / Archived (`Inactive`)**:
   - `IsActive = false`.
   - Completely excluded from public storefront and discovery. Accessible only in provider inventory view for historical audit.
4. **Not Available for Online Purchase (`StoreOnly`)**:
   - `IsActive = true`, `IsPublished = true`, `IsPurchasableOnline = false`.
   - Displayed in storefront catalog with "In-Store Only" tag for physical venue pickup. Excluded from online checkout.
5. **Unpublished / Draft (`Draft`)**:
   - `IsActive = true`, `IsPublished = false`.
   - Provider draft under preparation. Completely hidden from public storefront until provider publishes.

### 2.3 Strict Domain Separation
- **No Schedule Consumption**: Products do not consume appointment time or room capacity.
- **Schedule Invariance**: Creating, modifying, reserving, restocking, or purchasing products never alters staff working hours, breaks, time-offs, or booking availability.

## 3. Inventory Invariants & Concurrency Safety (Section 133)
- **Strict Non-Negative Invariant**: Inventory can **never** become negative (`StockQuantity >= 0`). Attempting to set or decrement stock below zero throws a `ValidationException`.
- **Concurrent Purchase Scenario**:
  - Scenario: Product stock is `1`.
  - Two customers attempt to purchase the single item concurrently.
  - Exactly one purchase succeeds: `product.Purchase(1) == true`, reducing `StockQuantity` from 1 to 0.
  - The second purchase fails: `product.Purchase(1) == false`, rejected with `"Insufficient inventory for product"`.
  - Database maintains `StockQuantity = 0`, never `-1`.
  - Concurrency token (`RowVersion`) ensures optimistic concurrency resolution.

## 4. Order Lifecycle & Status Transitions
- Order Statuses: `Pending` -> `Paid` -> `Processing` -> `Ready` -> `Completed` / `Cancelled`.
- Mixed state seeding (Section 125): Seeds representative deterministic orders in pending, processing, completed, and cancelled states.
- Customer order tracking: Real-time progress tracker with delivery address and line items.
