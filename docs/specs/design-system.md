<!-- gate:design-system status=COMPLETE at=2026-10-11T12:30:00Z -->

# Design System Specification — Initiative Tracker

| Field | Value |
|---|---|
| Product | User-Owned Initiative Tracker |
| Source Specs | `docs/specs/PRD.md` (gate:prd status=COMPLETE), `docs/specs/HLD.md` (gate:hld status=COMPLETE) |
| Author | Design System Architect |
| Status | **COMPLETE** — Ready for UI Scaffolding & Screen Implementation |
| Base Palette Source | [coolors.co/292f36-4ecdc4-f7fff7-ff6b6b-ffe66d](https://coolors.co/292f36-4ecdc4-f7fff7-ff6b6b-ffe66d) |
| Target Stack | React 18, TypeScript, Tailwind CSS / CSS Custom Properties, Lucide Icons |

---

## 1. Brand & Tone

### 1.1 Product Personality
The Initiative Tracker serves knowledge workers and collaborative teams balancing personal focus with team commitments. The design language expresses three core qualities:
1. **Lightweight & Agile**: Clean, clutter-free surfaces that feel responsive and fast. Interfacechrome stays subordinate to the user's initiatives and cards.
2. **Dependable & Resilient**: Precise typography, structural alignment, and clear status feedback (syncing, offline, conflict alerts) reassure users that their work is safe and synchronized.
3. **Approachable & Modern**: Energetic teal accents and warm highlights avoid enterprise bloat and spreadsheet fatigue, providing a human, focused workspace.

### 1.2 Core Color Palette (from Coolors Baseline)
The palette builds directly upon the specified 5-color foundation:
- **Charcoal Slate (`#292F36`)**: Primary dark neutral; provides deep contrast, grounding typography, and dark-theme surface foundations.
- **Electric Teal (`#4ECDC4`)**: Core brand accent; used for primary actions, active states, focus highlights, and brand identity.
- **Mint Cream (`#F7FFF7`)**: Light background foundation; provides an off-white, calm, low-strain canvas.
- **Coral Red (`#FF6B6B`)**: Semantic destructive/alert token; highlights urgent deadlines, conflicts, and errors.
- **Sunglow Yellow (`#FFE66D`)**: Warning and emphasis token; highlights medium priority, pending synchronization states, and active tags.

---

## 2. Color System & Tokens

### 2.1 Primitive Scale

```css
:root {
  /* Brand Primitives */
  --color-brand-teal-50:  #E8FAF8;
  --color-brand-teal-100: #C7F3EF;
  --color-brand-teal-200: #9CE7E0;
  --color-brand-teal-300: #71DBD1;
  --color-brand-teal-400: #4ECDC4; /* Base Coolors Teal */
  --color-brand-teal-500: #33B3A9;
  --color-brand-teal-600: #23928A;
  --color-brand-teal-700: #1B736D;
  --color-brand-teal-800: #145550;
  --color-brand-teal-900: #0E3B37;

  /* Charcoal / Slate Neutrals */
  --color-slate-50:  #F7F9FA;
  --color-slate-100: #EFF2F4;
  --color-slate-200: #DFE4E8;
  --color-slate-300: #C5CDD5;
  --color-slate-400: #94A1AD;
  --color-slate-500: #687786;
  --color-slate-600: #4B5763;
  --color-slate-700: #373F48;
  --color-slate-800: #292F36; /* Base Coolors Charcoal */
  --color-slate-900: #1A1F24;
  --color-slate-950: #101317;

  /* Background Canvas Base */
  --color-mint-50:  #FFFFFF;
  --color-mint-100: #F7FFF7; /* Base Coolors Mint Cream */
  --color-mint-200: #EEF8EE;
  --color-mint-300: #DEEFDF;

  /* Coral Red (Destructive / Error) */
  --color-coral-100: #FFE5E5;
  --color-coral-200: #FFC7C7;
  --color-coral-300: #FFA3A3;
  --color-coral-400: #FF6B6B; /* Base Coolors Coral */
  --color-coral-500: #FA4B4B;
  --color-coral-600: #D92B2B; /* Accessible text contrast on light */
  --color-coral-700: #B31E1E;

  /* Sunglow Yellow (Warning / Attention) */
  --color-sunglow-100: #FFFBD9;
  --color-sunglow-200: #FFF6B3;
  --color-sunglow-300: #FFEE8D;
  --color-sunglow-400: #FFE66D; /* Base Coolors Yellow */
  --color-sunglow-500: #E6C843;
  --color-sunglow-600: #BA9D22;
  --color-sunglow-700: #8A7210; /* Accessible text contrast on light */

  /* Semantic Success (Green complement) */
  --color-emerald-100: #E6F8F0;
  --color-emerald-500: #2BB673;
  --color-emerald-700: #1A7549;

  /* Semantic Info (Blue complement) */
  --color-sky-100: #E8F4FD;
  --color-sky-500: #3898EC;
  --color-sky-700: #1D63A2;
}
```

### 2.2 Semantic Tokens (Light & Dark Theme)

| Token Name | Light Theme Value | Dark Theme Value | Usage Description |
|---|---|---|---|
| `--bg-app` | `#F7FFF7` (Mint-100) | `#101317` (Slate-950) | Full page canvas background |
| `--bg-surface` | `#FFFFFF` | `#1A1F24` (Slate-900) | Top navigation, card base, modal body |
| `--bg-surface-subtle` | `#EFF2F4` (Slate-100) | `#292F36` (Slate-800) | Board column container, disabled elements |
| `--bg-surface-hover` | `#DFE4E8` (Slate-200) | `#373F48` (Slate-700) | Clickable card/row hover state |
| `--text-primary` | `#292F36` (Slate-800) | `#F7FFF7` (Mint-100) | Main titles, headings, card titles |
| `--text-secondary` | `#4B5763` (Slate-600) | `#94A1AD` (Slate-400) | Subtitles, metadata, timestamps, assignees |
| `--text-muted` | `#687786` (Slate-500) | `#687786` (Slate-500) | Placeholder text, secondary counters |
| `--text-inverse` | `#FFFFFF` | `#101317` | Text on dark badge or primary button |
| `--border-subtle` | `#DFE4E8` (Slate-200) | `#373F48` (Slate-700) | Kanban column borders, card separators |
| `--border-strong` | `#94A1AD` (Slate-400) | `#687786` (Slate-500) | Active inputs, selected item boundaries |
| `--interactive-primary` | `#23928A` (Teal-600) | `#4ECDC4` (Teal-400) | Primary button background, active tab pill |
| `--interactive-primary-hover` | `#1B736D` (Teal-700) | `#71DBD1` (Teal-300) | Primary button hover |
| `--focus-ring` | `#23928A` (Teal-600) | `#4ECDC4` (Teal-400) | 2px accessible focus outline ring |
| `--state-error` | `#D92B2B` (Coral-600) | `#FF6B6B` (Coral-400) | Destructive action, failed sync, form error |
| `--state-warning` | `#8A7210` (Sunglow-700) | `#FFE66D` (Sunglow-400) | Pending sync, conflict review, high priority |
| `--state-success` | `#1A7549` (Emerald-700) | `#2BB673` (Emerald-500) | Synced confirmation, success toast |
| `--state-info` | `#1D63A2` (Sky-700) | `#3898EC` (Sky-500) | Informational callouts, active filters |

### 2.3 Color Usage & Contrast Governance
1. **Teal Accessibility Rule**: Pure Coolors Teal (`#4ECDC4`) has an inherent relative luminance of ~0.49.
   - On `#F7FFF7` (light mode), `#4ECDC4` does NOT achieve 4.5:1 text contrast. Therefore, **`#23928A` (Teal-600) or `#1B736D` (Teal-700)** must be used for text/links and solid button fills in light mode.
   - When `#4ECDC4` is used as a vibrant background (e.g. badges or accent pill), the foreground text MUST be **`#292F36` (Slate-800)** (contrast ratio 6.97:1, passing AAA for large text and AA for normal text). Never use white text on `#4ECDC4`.
2. **Sunglow Yellow Accessibility Rule**: `#FFE66D` has high luminance. Always pair with `#292F36` text. Never use white text on yellow.
3. **Coral Red Rule**: For small error text on white/mint, use `#D92B2B` (contrast > 5.5:1). `#FF6B6B` is reserved for dark mode text, light tints (`#FFE5E5`), and icon fills.
4. **Surface Hierarchy**:
   - Level 0 (Canvas): `--bg-app`
   - Level 1 (Columns/Containers): `--bg-surface-subtle`
   - Level 2 (Draggable Cards/Modals): `--bg-surface`
   - Level 3 (Popovers/Tooltips): Elevated above surface with `--shadow-lg`.

---

## 3. Typography

### 3.1 Font Families
- **Display & Headings**: `Plus Jakarta Sans`, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif.
  - *Rationale*: Geometric yet warm sans-serif with excellent legibility at display sizes; open-source (Google Fonts / SIL Open Font License).
- **Body & UI**: `Inter`, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif.
  - *Rationale*: Industry-standard UI font with balanced x-height, clear numbers, and tabular figures essential for kanban task counters and dates.
- **Monospace & Code**: `JetBrains Mono`, "SF Mono", Consolas, Menlo, monospace.
  - *Rationale*: Used for sequence numbers, IDs, shortcut keys, and technical tokens.

### 3.2 Type Scale

| Token | Size (rem / px) | Line Height | Weight | Tracking | Primary Usage |
|---|---|---|---|---|---|
| `text-2xs` | 0.6875rem (11px) | 1.0rem (16px) | 600 SemiBold | +0.02em | Sync badge, compact tags, avatar labels |
| `text-xs` | 0.75rem (12px) | 1.125rem (18px) | 500 Medium | +0.01em | Task card metadata, timestamps, column counters |
| `text-sm` | 0.875rem (14px) | 1.25rem (20px) | 400 Regular / 500 Med | normal | Task card title, table text, input text, buttons |
| `text-base` | 1.0rem (16px) | 1.5rem (24px) | 400 Regular / 500 Med | normal | Initiative title in swimlane, modal body, comments |
| `text-lg` | 1.125rem (18px) | 1.625rem (26px) | 600 SemiBold | -0.01em | Modal headers, sub-section headers |
| `text-xl` | 1.25rem (20px) | 1.75rem (28px) | 600 SemiBold | -0.015em | Column headers, view names |
| `text-2xl` | 1.5rem (24px) | 2.0rem (32px) | 700 Bold | -0.02em | Board title, dashboard h1 |
| `text-3xl` | 1.875rem (30px) | 2.25rem (36px) | 700 Bold | -0.025em | Landing / splash display titles |

### 3.3 Weight Conventions
- **Regular (400)**: Descriptive body text, comment bodies, input text.
- **Medium (500)**: Form labels, navigation tabs, card titles, table headers.
- **SemiBold (600)**: Column headers, modal headings, buttons, status badges.
- **Bold (700)**: Board title, primary modal titles, high-emphasis metrics.

---

## 4. Spacing & Layout Grid

### 4.1 Base Unit & Spacing Scale
The layout follows a strict **4px base unit** (with 8px standard rhythm).

| Token | Value | Visual Reference | Standard Application |
|---|---|---|---|
| `space-1` | 4px | 0.25rem | Micro-gap between icon and label, card badge padding |
| `space-2` | 8px | 0.5rem | Gap between card metadata items, input vertical padding |
| `space-3` | 12px | 0.75rem | Task card internal padding, list item gaps |
| `space-4` | 16px | 1.0rem | Initiative header padding, standard form field spacing |
| `space-5` | 20px | 1.25rem | Gap between kanban columns |
| `space-6` | 24px | 1.5rem | Board canvas outer margin, modal internal padding |
| `space-8` | 32px | 2.0rem | Section spacing in settings / drawer |
| `space-12` | 48px | 3.0rem | Major page section divide |

### 4.2 Elevation & Shadows
- **Card Default (`--shadow-sm`)**: `0 1px 2px 0 rgba(41, 47, 54, 0.05)`
- **Card Hover (`--shadow-md`)**: `0 4px 6px -1px rgba(41, 47, 54, 0.08), 0 2px 4px -2px rgba(41, 47, 54, 0.04)`
- **Dragging Card (`--shadow-drag`)**: `0 12px 20px -3px rgba(41, 47, 54, 0.18), 0 4px 6px -4px rgba(41, 47, 54, 0.08)` (lifted + 2-degree tilt)
- **Modal / Popover (`--shadow-lg`)**: `0 20px 25px -5px rgba(41, 47, 54, 0.15), 0 8px 10px -6px rgba(41, 47, 54, 0.05)`

### 4.3 Border Radii
- **Tag / Badge (`--radius-sm`)**: 4px
- **Task Card / Input (`--radius-md`)**: 8px
- **Column / Modal / Panel (`--radius-lg`)**: 12px
- **Avatar / Pill Button (`--radius-full`)**: 9999px

### 4.4 Board Layout Geometry & Swimlanes
The core differentiator of the Initiative Tracker is the configurable mixed-column model.
1. **Initiative-Only Column**:
   - Fixed width: `300px` (min 280px, max 340px).
   - Renders whole initiative cards in a single vertical stack.
2. **Initiative + Task Column**:
   - Variable width based on configured sub-stages: `calc(260px * count(sub-stages))` (e.g. 3 sub-stages = 780px wide).
   - Sub-stage sub-columns: `260px` fixed width with a `1px` subtle divider (`--border-subtle`).
   - Swimlane row: An initiative spans across all sub-stages with a horizontal swimlane track.
3. **Horizontal Board Canvas**:
   - `overflow-x: auto; overflow-y: hidden;`
   - Smooth horizontal scrolling with sticky board header and left column pinning support on wide screens.
   - Drag boundaries constrained within sub-stage droppables.

---

## 5. Component Conventions

### 5.1 Buttons
Buttons provide clear visual hierarchy and state feedback.

| Variant | Normal State | Hover State | Active / Pressed | Text / Icon Color |
|---|---|---|---|---|
| **Primary** | `--interactive-primary` (solid teal) | `--interactive-primary-hover` | Inset shadow, slight scale (0.98) | `--text-inverse` (Light) / `#292F36` (Dark) |
| **Secondary** | `--bg-surface` + 1px `--border-subtle` | `--bg-surface-hover` + `--border-strong` | Background `--slate-200` | `--text-primary` |
| **Destructive** | `--state-error` (solid coral) | Darker red shade | Dark red pressed | White (`#FFFFFF`) |
| **Ghost / Link** | Transparent | `--bg-surface-hover` | Background `--slate-200` | `--interactive-primary` |

- **Loading State**: Disables click, replaces leading icon with a 16px CSS spinner, retains button dimensions to avoid layout shift.
- **Focus State**: `outline: 2px solid var(--focus-ring); outline-offset: 2px;`

### 5.2 Form Fields & Inputs
- **Base Style**: 1px border `--border-subtle`, background `--bg-surface`, border-radius `--radius-md`, height `38px` (desktop), `44px` (touch/mobile).
- **Focus**: Border `--interactive-primary`, box-shadow `0 0 0 3px rgba(78, 205, 196, 0.25)`.
- **Error State**: Border `--state-error`, helper text in `--state-error` (`text-xs`), leading error icon.
- **Disabled**: Background `--bg-surface-subtle`, cursor `not-allowed`, opacity `0.6`.

### 5.3 Kanban Task Card
- **Structure**:
  - Header: Initiative chip / Tag badge + Drag handle affordance.
  - Body: Task title (`text-sm font-medium`, max 3 lines truncation).
  - Footer: Due date indicator (colored red if overdue), comment count icon + counter, multiple assignee avatar stack (overlap 8px).
- **Interaction**:
  - Hover: Elevates with `--shadow-md`, reveals quick-action menu (edit, move, assign).
  - Dragging (`dnd-kit`): Opacity 0.9, `--shadow-drag`, 2° rotation, droppable target slot highlights with 2px dashed `--interactive-primary` border and `--color-brand-teal-50` background tint.
  - Keyboard accessible: Space/Enter picks up card, Arrow keys move across sub-stages/initiatives, Escape cancels, Enter drops.

### 5.4 Swimlane Initiative Block
- **Initiative-Only Card**:
  - Progress bar across bottom showing `% of child tasks complete`.
  - Task count pill (e.g., `8 tasks`).
  - Title and status badge.
- **Initiative Swimlane Row Header**:
  - Sticky left bar with collapsible chevron, initiative title, and quick "Add Task" button.

### 5.5 Sync & Connectivity Status Indicator (HLD §3.6 First-Class Component)
The top navigation bar MUST render a persistent **Sync Status Pill**:

| Status | Icon (Lucide) | Visual Style | Popover / Tooltip Text |
|---|---|---|---|
| `synced` | `CheckCircle2` | Green badge (`--emerald-100` / `--emerald-700`) | "All changes saved and synchronized" |
| `syncing` | `RefreshCw` (spinning) | Teal badge (`--brand-teal-100` / `--brand-teal-700`) | "Saving changes to server..." |
| `offline` | `WifiOff` | Slate badge (`--slate-200` / `--slate-700`) | "Working offline (cached HH:MM). Changes queued." |
| `pending` | `Clock` | Yellow badge (`--sunglow-200` / `--sunglow-700`) | "Pending synchronization (n changes in queue)" |
| `failed` | `AlertCircle` | Coral badge (`--coral-200` / `--coral-600`) | "Sync failed. Click to retry." |
| `reconciled` | `GitMerge` | Coral/Yellow badge with notification dot | "3 changes resolved with server. Review updates." |
| `reconnecting`| `Loader2` (spinning) | Slate badge | "Connecting to live session..." |

### 5.6 Comments & Threading Component (HLD §2.5)
- **Top-Level Comment**: Author avatar (32px), author name (`font-semibold text-sm`), timestamp (`text-xs text-secondary`), comment body, "Reply" link, edit/delete actions (author or admin only).
- **Single-Level Threaded Replies**: Indented `24px` with a connecting subtle vertical line. All replies stay flat under the parent comment.
- **Edited Indicator**: Comments edited by the user display `(edited)` in `text-xs text-muted`.
- **Soft-Deleted Tombstone**: "This comment has been deleted." (`italic text-muted`).

### 5.7 Modals & Dialogs
- **Backdrop**: `rgba(41, 47, 54, 0.5)` with `backdrop-filter: blur(2px)`.
- **Dimensions**: Small (440px) for confirmation/delete, Standard (640px) for task detail/editor, Large (840px) for board configuration.
- **Close Behavior**: `Escape` key, click outside backdrop, or explicit `X` button. Focus trapped within modal while open.

---

## 6. Iconography

- **Library**: `lucide-react` (clean, 2px stroke, modern aesthetic).
- **Scale**:
  - Micro / Inline (`14px`): Sync pill icons, card count indicators, dropdown arrows.
  - Default UI (`18px`): Button icons, input leading icons, navigation items.
  - Large / Action (`24px`): Empty state illustrations, modal headers.
- **Rule**: Standalone icon buttons (without visible text) MUST include `aria-label` and a hover tooltip.

---

## 7. Motion & Animation

- **Timing Tokens**:
  - `--motion-fast`: `100ms cubic-bezier(0.4, 0, 0.2, 1)` (hover, active button presses).
  - `--motion-normal`: `200ms cubic-bezier(0.4, 0, 0.2, 1)` (dropdowns, card drag lift, color fades).
  - `--motion-slow`: `350ms cubic-bezier(0, 0, 0.2, 1)` (modal slide-in, drawer expansion).
- **Drag Transitions**: When a card is dropped, animate smoothly to its final slot over `200ms`.
- **Accessibility Safeguard**:
  ```css
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: 0.01ms !important;
      animation-iteration-count: 1 !important;
      transition-duration: 0.01ms !important;
      scroll-behavior: auto !important;
    }
  }
  ```

---

## 8. Accessibility Baseline (WCAG 2.1 AA Non-Negotiables)

1. **Contrast Floor**:
   - All body text: minimum 4.5:1 against its background.
   - Large text (18px+ bold or 24px+ regular) and UI component boundaries/icons: minimum 3.0:1.
2. **Keyboard Navigation**:
   - Full keyboard accessibility for task management: Users can navigate columns and swimlanes via Tab/Arrow keys, trigger card movement via keyboard menu or shortcut (`dnd-kit` keyboard sensor enabled).
   - Visible focus indicators: 2px solid ring with 2px offset on all interactive elements. Never suppress `:focus-visible`.
3. **Touch Targets**:
   - Mobile and tablet minimum interactive target size: `44px x 44px` (task action menus, column add buttons, navigation items).
4. **Screen Reader Landmarks**:
   - `<header>` for navigation and sync status.
   - `<main>` for board canvas.
   - `<section role="region" aria-label="[Column Name]">` for kanban columns.
   - `aria-live="polite"` region for real-time board updates and offline status alerts.

---

## 9. Design Decisions Requiring Product Owner Confirmation

While the technical and accessibility rules are grounded, the following visual/brand options are presented for Product Owner sign-off:

1. **Light Mode Canvas Background (`#F7FFF7` Mint Cream vs Pure `#FFFFFF`)**:
   - *Current Proposal*: Canvas uses `#F7FFF7` (subtle mint warmth) with pure `#FFFFFF` for task cards to establish clear card elevation and reduce eye strain.
   - *Alternative*: Standard pure `#FFFFFF` canvas with light slate cards (`#F4F6F8`).
2. **Column Width Constraints**:
   - *Current Proposal*: Fixed `300px` for initiative-only columns and `260px` per sub-stage in initiative+task columns. With 3 sub-stages, an initiative+task column spans `780px`.
   - *Alternative*: Compact `220px` sub-stages for lower-resolution laptops.
3. **Primary Accent in Light Mode**:
   - *Current Proposal*: Solid interactive buttons use darkened Teal-600 (`#23928A`) in light mode to guarantee WCAG AA contrast (4.6:1), reserving the vibrant Coolors Teal (`#4ECDC4`) for badges, dark mode, and accents with dark slate text.
   - *Alternative*: Keep `#4ECDC4` everywhere, requiring dark text (`#292F36`) on all primary buttons.
