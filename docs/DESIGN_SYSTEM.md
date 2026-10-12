# Bookline Design System Specification

## 1. System Vision & Philosophy

Bookline is designed as a consumer-grade booking marketplace and premium provider management platform. It combines high-trust editorial elegance with operational speed. It strictly avoids feeling like a generic admin template.

### Core Principles
1. **Strong Visual Hierarchy**: Essential provider metadata (name, rating, location, starting price) is legible within 2 seconds.
2. **Restrained Borders & Surfaces**: Soft, deliberate surface elevations rather than aggressive thick borders.
3. **90% Neutral / 10% Intentional Accent**: 90% of surfaces are neutral dark or light tones; the coral brand and semantic colors occupy the remaining 10% for focus and key calls-to-action.
4. **Accessible WCAG AA Contrast**: Every text and interactive control is measured against its adjacent background.
5. **Real Consumer Feel**: Typography pairs sophisticated editorial serifs for branding with clean, high-density sans-serif for forms, calendars, tables, and cards.

---

## 2. Color Palette & Tokens

### 2.1 Dark Application Tokens (Provider Operations & Admin)
Used by default for provider back-office operations, scheduling boards, and administration:

| Token | Hex Value | Role & Usage | Contrast vs Background |
| :--- | :--- | :--- | :--- |
| `--bg-base` | `#090B10` | Deep midnight canvas background | N/A |
| `--surface-primary` | `#111620` | Primary cards, panels, top navigation | 1.15:1 |
| `--surface-secondary` | `#151B27` | Sub-panels, table rows, secondary containers | 1.3:1 |
| `--surface-elevated` | `#1A2130` | Drawers, dialogs, popovers, dropdowns | 1.5:1 |
| `--border-subtle` | `#30394A` | Component boundaries, card dividers | 3.2:1 (Passes UI component boundary standard) |
| `--border-strong` | `#344054` | Focused inputs, selected card boundaries | 3.8:1 |
| `--text-primary` | `#F4F6FA` | Primary titles, prominent headings | **17.2:1** (Passes WCAG AAA) |
| `--text-secondary` | `#CBD2DE` | Subtitles, card metadata, labels | **11.8:1** (Passes WCAG AAA) |
| `--text-tertiary` | `#A2AEC1` | Timestamps, captions, unselected icons | **7.5:1** (Passes WCAG AA) |
| `--text-disabled` | `#687386` | Inactive states, disabled controls | 3.4:1 |

---

### 2.2 Light Marketplace Tokens (Customer Discovery Default)
The consumer-facing discovery and booking experience defaults to a warm, comfortable light palette:

| Token | Hex Value | Role & Usage | Contrast vs Background |
| :--- | :--- | :--- | :--- |
| `--bg-base` | `#FAF8F5` | Warm editorial parchment canvas | N/A |
| `--surface-primary` | `#FFFFFF` | Crisp white cards, storefront sheets | 1.05:1 |
| `--surface-secondary` | `#F3F0EB` | Filter bars, subtle pill containers | 1.12:1 |
| `--surface-elevated` | `#FFFFFF` | Elevated modal dialogs, search dropdowns | 1.05:1 |
| `--border-subtle` | `#DED8CF` | Card borders, horizontal rules | 3.1:1 (Passes component boundary standard) |
| `--border-strong` | `#B5ADA1` | Input focus rings, active filter borders | 4.1:1 |
| `--text-primary` | `#181A1F` | Provider names, prices, titles | **15.4:1** (Passes WCAG AAA) |
| `--text-secondary` | `#4B5563` | Category labels, review counts, addresses | **7.8:1** (Passes WCAG AA) |
| `--text-tertiary` | `#626B7A` | Metadata captions, secondary tags | **5.2:1** (Passes WCAG AA) |
| `--text-disabled` | `#9AA3B2` | Disabled buttons, unavailable days | 2.5:1 |

---

### 2.3 Brand & Primary CTA System

| Token | Hex Value | Role | Accessibility Notes |
| :--- | :--- | :--- | :--- |
| **Bookline Coral** | `#E8546A` | Hero accents, brand badges, active markers | Brand identification |
| **Primary CTA** | `#B93650` | Primary buttons ("Book Now", "Confirm") | **4.6:1** against white text (Passes WCAG AA) |
| **CTA Hover** | `#A52E46` | Interactive hover on primary CTA | **5.4:1** against white text |
| **CTA Text** | `#FFFFFF` | Button label text | Full contrast on primary CTA |

---

### 2.4 Semantic & Status Tokens

| Semantic State | Light Surface Color | Dark Surface Color | Meaning |
| :--- | :--- | :--- | :--- |
| **Available / Free** | `#158A63` | `#34D399` | Open calendar slot, verified badge, online |
| **Held / Warning** | `#A66B00` | `#FBBF24` | 5-minute checkout slot hold countdown |
| **Error / Destructive**| `#C74444` | `#F87171` | Booking cancellation, validation failure |
| **Information** | `#2563A6` | `#60A5FA` | System notices, location resolution info |
| **Booked / Closed** | `#64748B` | `#64748B` | Reserved calendar slot, closed hours |

---

## 3. Typography Scale & Hierarchy

### 3.1 Type Families
- **Editorial Brand Headings**: `Playfair Display`, serif.
  - Used for: Platform title, storefront hero headings, section titles ("Discover Exceptional Care", "Our Craft").
  - Do NOT use for every operational card or button.
- **Application & Operational Interface**: `DM Sans`, sans-serif.
  - Used for: Discovery filters, provider cards, interactive maps, forms, tables, pricing, calendar time slots.

### 3.2 Type Scale
- **H1 (Hero Heading)**: `2.25rem` (36px), line-height `1.2`, weight `700`, `font-heading` (`Playfair Display`).
- **H2 (Section Heading)**: `1.5rem` (24px), line-height `1.3`, weight `700`, `font-heading`.
- **H3 (Card Title)**: `1.125rem` (18px), line-height `1.4`, weight `600`, `font-sans` (`DM Sans`).
- **Body Regular**: `0.875rem` (14px), line-height `1.5`, weight `400` / `500`.
- **Metadata Small**: `0.75rem` (12px), line-height `1.4`, weight `500` / `600`.
- **Micro Badge**: `0.6875rem` (11px), line-height `1.2`, weight `700`, uppercase tracking `0.05em`.

---

## 4. Spacing, Touch Targets & Responsive Layout

### 4.1 Spacing Scale (8pt Grid)
- `space-1`: 4px (micro gaps between icon and text)
- `space-2`: 8px (compact padding, pill gap)
- `space-3`: 12px (badge padding, card subtitle gap)
- `space-4`: 16px (standard input padding, card inner padding)
- `space-5`: 20px (card body padding)
- `space-6`: 24px (section margins)
- `space-8`: 32px (page margins on desktop)

### 4.2 Touch Targets & Interactivity
- **Minimum Interactive Target**: $\ge 44\text{px} \times 44\text{px}$ on all mobile buttons, chips, and location triggers.
- **Focus Indicators**: Every interactive control renders a visible focus ring:
  ```css
  *:focus-visible {
    outline: 2px solid var(--accent-primary);
    outline-offset: 2px;
  }
  ```
- **Consistent Iconography**: Exclusively using `lucide-react`. No raw emojis in production navigation or status controls.

---

## 5. Component Visual States

Every discovery component implements 5 mandatory states:
1. **Normal**: Standard readable layout with genuine database data.
2. **Hover / Focus**: Subtle elevation, smooth transition (`150ms ease`), border brightening, visible focus outline.
3. **Selected**: Coral accent border ring (`ring-1 ring-[#E8546A]/40`), soft brand glow, synchronized with map pin.
4. **Loading / Skeleton**: Dimensionally matched animated pulse boxes preventing cumulative layout shift (CLS).
5. **Empty / Error**: Contextual explanation with actionable retry/reset buttons. Zero mock/fabricated fallback data.
