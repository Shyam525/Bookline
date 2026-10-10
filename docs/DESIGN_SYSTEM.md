# BOOKLINE vNext DESIGN SYSTEM SPECIFICATION

## 1. Core Philosophy
Bookline is engineered to feel:
- **Comfortable**: Optimized for extended daily operational usage (30–60 minutes) without optical fatigue.
- **Quiet & Restrained**: No harsh saturations, no giant neon containers, no gratuitous glow.
- **High-Trust & Editorial**: Balanced typography with Playfair Display for headings and DM Sans for clean tabular precision.
- **90% Neutral / 10% Semantic Rule**: 90% of the interface is neutral dark/light surfaces; 10% is dedicated to purposeful semantic states and primary brand CTA.

---

## 2. Refined Color System

### A. Dark Mode Palette
| Token | Hex Code | Purpose |
| :--- | :--- | :--- |
| **Dark Background** | `#090B10` | Main canvas background |
| **Primary Surface** | `#111620` | Base container, cards, header |
| **Secondary Surface** | `#151B27` | Table rows, secondary panels |
| **Elevated Surface** | `#1A2130` | Drawers, popovers, modals |
| **Subtle Border** | `#273142` | Dividers, card boundaries |
| **Strong Border** | `#344054` | Active inputs, selected boundaries |
| **Primary Text** | `#F4F6FA` | Primary headings, prominent body |
| **Secondary Text** | `#C3CAD6` | Labels, table data, subheadings |
| **Tertiary Text** | `#8F9AAF` | Timestamps, helper text, captions |
| **Disabled Text** | `#687386` | Inactive states, disabled controls |
| **Primary Accent** | `#E8546A` | Primary CTA buttons, brand badges |
| **Accent Hover** | `#F06A7D` | CTA hover state |
| **Accent Pressed** | `#C94358` | CTA active pressed state |

### B. Semantic & Scheduling Slot States
| Semantic Token | Hex Code | Visual Meaning |
| :--- | :--- | :--- |
| **Free / Available** | `#34D399` | Open calendar slot, active status |
| **Held / Warning** | `#FBBF24` | 5-minute temporary hold countdown, warning |
| **Booked / Inactive** | `#64748B` | Reserved calendar slot, disabled |
| **Selected** | `#E8546A` | Customer's active slot choice |
| **Destructive / Error** | `#F87171` | Cancellation, deletion, errors |
| **Informational** | `#60A5FA` | System notices, active sync |

### C. Light Mode Palette
| Token | Hex Code | Purpose |
| :--- | :--- | :--- |
| **Light Background** | `#FAF8F5` | Warm editorial background |
| **Primary Surface** | `#FFFFFF` | Crisp white cards and panels |
| **Secondary Surface** | `#F3F0EB` | Secondary containers |
| **Border** | `#DED8CF` | Subtle dividers |
| **Primary Text** | `#181A1F` | High-contrast editorial text |
| **Secondary Text** | `#555D6B` | Labels and secondary copy |
| **Tertiary Text** | `#707989` | Captions and metadata |
| **Accent** | `#D94C62` | Light mode coral brand accent |
| **Success** | `#158A63` | Positive confirmations |
| **Warning** | `#A66B00` | Warning badges |
| **Error** | `#C74444` | Validation and destructive alerts |

---

## 3. Typography
- **Editorial Headings**: `Playfair Display`, serif — conveys luxury, distinction, and editorial poise.
- **Interface & Operational Data**: `DM Sans`, sans-serif — engineered for high legibility across dense calendars, tables, and financial ledgers.

---

## 4. Location Failure Resilience (Section 103)
The marketplace discovery subsystem implements multi-tier fallback resilience:
1. **GPS Denied**: Never block user flow. Smoothly fallback to citywide directory search and popular neighborhood presets.
2. **Geocoding Failed**: Fall back to manual input of locality, postal code, or business name.
3. **Map Canvas Failed**: The provider list continues functioning seamlessly; map failure displays an inline retry banner without disrupting booking operations.
4. **Search Failed**: Accessible retry UI presented directly in-place without page reload.

---

## 5. Discovery States (Section 104)
Discovery interface guarantees 4 distinct states:
- **`LOADING`**: Coordinated skeleton placeholders matching exact card dimensions.
- **`EMPTY`**: Meaningful guidance when 0 providers match current geographic radius or category filters.
- **`ERROR`**: Actionable error states with retry controls. **Strict contract**: Never render fake or fabricated mock providers following an API failure.
- **`SUCCESS`**: Verified provider cards with genuine ratings, pricing, and next openings.

---

## 6. Provider Image System (Section 105)
Standardized via `ProviderImage` component:
- **Image Roles**: Support for `logo`, `cover`, `service`, `product`, and `gallery` modes.
- **Failover Protection**: Dynamic SVG and category-themed gradient placeholders eliminate broken image icons entirely.
- **Performance**: Native lazy loading (`loading="lazy"`) and optimized thumbnail rendering.
- **Lightbox**: Full-screen modal viewer with keyboard navigation for multi-image galleries.

---

## 7. Mobile Customer Design (Section 106)
Mobile layout follows strict visual hierarchy:
1. Search Bar & Location Chip
2. Category Pill Carousel
3. Verified Provider Results
4. Map Toggle
5. Provider Storefront Profile
6. Booking Flow with Sticky Bottom Sheet
- **Touch Target Standard**: All interactive buttons, chips, and slots maintain $\ge 44\text{px}$ touch targets.

---

## 8. Desktop Provider Design (Section 107)
Desktop dashboard optimized for high productivity:
- High information density with multi-column staff scheduling calendars.
- Global command palette (`Ctrl+K` / `Cmd+K`) for rapid booking creation and customer search.
- Side navigation, collapsible detail drawers, and dense data tables with sticky column headers.

---

## 9. Visual Density Spectrum (Section 108)
Three distinct operational density modes:
- **Customer**: Spacious discovery ($16\text{px}$–$24\text{px}$ padding), large imagery, prominent CTA buttons, breathing room.
- **Provider**: Medium density ($12\text{px}$ padding), compact toolbar, side-by-side staff schedules, quick status updates.
- **Admin**: Highest density ($8\text{px}$ cell padding), compact tabular layout, audit trail logs, moderation controls.

---

## 10. Design System Component Matrix (Section 109)
Reusable primitives across 7 core functional domains:
1. **Navigation**: `AppShell`, `Sidebar`, `TopBar`, `Breadcrumbs`, `OrganizationSwitcher`, `LocationSwitcher`, `UserMenu`, `NotificationBell`.
2. **Forms**: `Input`, `Textarea`, `Select`, `Combobox`, `DatePicker`, `TimePicker`, `MoneyInput`, `PhoneInput`, `SearchInput`, `Switch`.
3. **Feedback**: `Toast`, `Alert`, `Modal`, `Drawer`, `ConfirmDialog`, `Skeleton`, `EmptyState`, `ErrorState`.
4. **Data Display**: `Table`, `DataTable`, `Pagination`, `MetricCard`, `Stat`, `Badge`, `StatusBadge`, `Avatar`.
5. **Scheduling**: `Calendar`, `CalendarHeader`, `TimeAxis`, `StaffColumn`, `AppointmentBlock`, `Slot`, `SlotGrid`, `HoldBanner`, `DateStrip`.
6. **Marketplace**: `ProviderCard`, `ProviderGrid`, `ProviderList`, `CategoryCard`, `SearchBar`, `LocationPicker`, `FilterBar`, `SortSelector`, `MapView`, `MapMarker`, `MapCluster`, `ProviderPreview`.
7. **Commerce**: `ProductCard`, `CartItem`, `CartSummary`, `OrderStatus`, `PaymentSummary`.

---

## 11. Component Contract Standards (Section 110)
Every primitive complies with 10 universal state contracts:
1. **Default**: Pristine resting state with zero visual artifacts.
2. **Hover**: Subtle luminance shift and elevation lift.
3. **Focus**: 2px high-contrast ring with keyboard tab navigation.
4. **Selected**: Brand coral border with semantic accent badge.
5. **Disabled**: 60% opacity reduction with `cursor-not-allowed`.
6. **Loading**: Coordinated skeleton shimmer or micro-spinner.
7. **Error**: High-visibility validation warning and crimson border.
8. **Empty**: Informative placeholder with actionable next step.
9. **Responsive**: Fluid responsive behavior from mobile (320px) to desktop (4K).
10. **Accessible**: WCAG 2.1 AA compliant semantic HTML, ARIA attributes, and screen-reader labels.
