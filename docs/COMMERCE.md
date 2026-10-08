# RETAIL COMMERCE & INVENTORY ARCHITECTURE

## 1. Multi-Vendor Commerce
Providers can offer physical boutique retail products (e.g., hair masks, organic massage oils, peptide serums) alongside appointment services.
Unlike appointments, retail products do not consume specialist calendar slots.

## 2. Provider-Aware Cart Architecture
- **Single-Provider Cart First**: Because physical items are fulfilled directly from local business stores or hand-delivered during appointment visits, Bookline enforces a single-provider cart constraint.
- **Cart Conflict Management**: Attempting to add an item from Provider B while holding items from Provider A triggers an explicit confirmation modal before clearing previous items.

## 3. Concurrency & Overselling Prevention
- **Inventory Model**:
  - `StockQuantity`: Total physical inventory in stock.
  - `ReservedQuantity`: Units held in pending checkout orders.
  - `AvailableQuantity`: `StockQuantity - ReservedQuantity` (computable).
  - `SoldQuantity`: Completed historical units.
- **Transactional Consistency**: All inventory allocations utilize database transactions with pessimistic row locking or concurrency tokens, guaranteeing no negative stock under concurrent purchases.
