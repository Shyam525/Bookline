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
