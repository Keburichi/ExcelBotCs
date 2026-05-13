---
name: ExcelBot
description: A polished guild management platform for FFXIV Free Companies
colors:
  linkshell-blue: "#3b82f6"
  aether-purple: "#8b5cf6"
  crystal-pink: "#ec4899"
  deep-slate: "#0f172a"
  midnight-canvas: "#0b1020"
  frost-surface: "#ffffff"
  mist-muted: "#64748b"
  steel-border: "#e5e7eb"
  dark-card: "#121a2d"
  dark-border: "#273043"
  verdant-success: "#10b981"
  ember-danger: "#dc2626"
  amber-warning: "#eab308"
  tank-blue: "#3b82f6"
  healer-green: "#22c55e"
  dps-red: "#ef4444"
  button-blue: "#2563eb"
  button-blue-hover: "#1d4ed8"
typography:
  display:
    fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica Neue, Arial"
    fontSize: "2rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.02em"
  headline:
    fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica Neue, Arial"
    fontSize: "1.5rem"
    fontWeight: 600
    lineHeight: 1.3
  title:
    fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica Neue, Arial"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: 1.4
  body:
    fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica Neue, Arial"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.6
  label:
    fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica Neue, Arial"
    fontSize: "0.9rem"
    fontWeight: 500
    lineHeight: 1.4
rounded:
  sm: "8px"
  md: "12px"
  lg: "16px"
  full: "999px"
spacing:
  xs: "0.25rem"
  sm: "0.5rem"
  md: "1rem"
  lg: "1.5rem"
  xl: "2rem"
  2xl: "3rem"
  3xl: "4rem"
components:
  button-primary:
    backgroundColor: "#2563eb"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "0.5rem 1rem"
  button-primary-hover:
    backgroundColor: "#1d4ed8"
    textColor: "#ffffff"
  button-secondary:
    backgroundColor: "#4b5563"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "0.5rem 1rem"
  button-danger:
    backgroundColor: "#dc2626"
    textColor: "#ffffff"
    rounded: "{rounded.md}"
    padding: "0.5rem 1rem"
  button-outlined:
    backgroundColor: "transparent"
    textColor: "#2563eb"
    rounded: "{rounded.md}"
    padding: "0.5rem 1rem"
  button-text:
    backgroundColor: "transparent"
    textColor: "#2563eb"
    rounded: "{rounded.md}"
    padding: "0.5rem 1rem"
  card-elevated:
    backgroundColor: "rgba(255, 255, 255, 0.7)"
    rounded: "{rounded.lg}"
    padding: "1rem"
  input-default:
    backgroundColor: "rgba(255, 255, 255, 0.5)"
    textColor: "#0f172a"
    rounded: "{rounded.md}"
    padding: "0.625rem 0.875rem"
  badge-default:
    backgroundColor: "#e5e7eb"
    textColor: "#374151"
    rounded: "{rounded.full}"
    padding: "0.125rem 0.5rem"
  chip-default:
    backgroundColor: "#f3f4f6"
    textColor: "#374151"
    rounded: "{rounded.full}"
    padding: "0.25rem 0.75rem"
---

# Design System: ExcelBot

## 1. Overview

**Creative North Star: "The Crystal Linkshell"**

A polished, precision-crafted tool that connects a community. Named after FFXIV's communication channels, the Crystal Linkshell is modern and clean on the surface, deeply functional underneath. Every element is deliberate: glassmorphism surfaces provide layered depth without decorative excess, interactions respond with precision, and the brand gradient (blue through purple to pink) appears sparingly as a signal of identity, never as wallpaper.

The system rejects two poles equally. It is not a generic SaaS dashboard: no bland metric cards, no Stripe-clone layouts, no cookie-cutter admin chrome. It is not a gaming skin: no fantasy borders, no neon accents, no themed textures. The FFXIV connection lives in contextual data (role colors, fight names, character syncs), not in visual decoration. Personality comes from craft: considered transitions, deliberate spacing, glass surfaces that reward attention.

**Key Characteristics:**
- Glassmorphism as structural language, not decoration. Semi-transparent surfaces with backdrop blur create depth hierarchy.
- Blue-purple-pink brand gradient reserved for identity moments: page titles, avatar placeholders, active navigation states.
- Shadow-assisted layering: glassmorphism and box-shadows collaborate as co-equal depth cues.
- Precise, responsive interactions: 200ms transitions, immediate state feedback, no choreography.
- Dual-theme system (light/dark) with full token parity. Dark mode is the native habitat; light mode is equally considered.

## 2. Colors: The Linkshell Palette

A restrained palette anchored by a single blue accent, with the brand gradient reserved for signature moments. Semantic colors (success, danger, warning) and FFXIV role colors (tank, healer, DPS) provide functional vocabulary without decorative saturation.

### Primary

- **Linkshell Blue** (`#3b82f6`): The anchor accent. Links, active navigation, focus rings, primary interactive states. In dark mode, brightens to `#60a5fa` for contrast. Used on less than 10% of any screen; its rarity is the point.

### Secondary

- **Aether Purple** (`#8b5cf6`): Mid-point of the brand gradient. Appears only in the gradient composition (page titles, avatar placeholders, active nav backgrounds). Never used as a standalone surface or text color.
- **Crystal Pink** (`#ec4899`): Endpoint of the brand gradient. Same constraint as purple: gradient only.

### Neutral

- **Deep Slate** (`#0f172a`): Light-mode foreground text. The darkest neutral. Tinted toward slate, not pure black.
- **Midnight Canvas** (`#0b1020`): Dark-mode background. Deep blue-slate, not `#000`. The entire dark palette breathes in this hue family.
- **Frost Surface** (`#ffffff`): Light-mode background and card base. Glassmorphism surfaces layer at 70% opacity over this.
- **Dark Card** (`#121a2d`): Dark-mode card surfaces at 70% opacity. Slightly warmer than the canvas beneath.
- **Mist Muted** (`#64748b` light / `#94a3b8` dark): Secondary text, labels, timestamps. Slate-tinted, never pure gray.
- **Steel Border** (`#e5e7eb` light / `#273043` dark): Structural borders, dividers, table lines.

### Semantic

- **Verdant Success** (`#10b981`): Confirmation, online status, positive states.
- **Ember Danger** (`#dc2626` light / `#f87171` dark): Errors, destructive actions, alerts.
- **Amber Warning** (`#eab308` light / `#facc15` dark): Caution states.

### FFXIV Role Colors

- **Tank Blue** (`#3b82f6` light / `#60a5fa` dark): Tank role indicator.
- **Healer Green** (`#22c55e` light / `#4ade80` dark): Healer role indicator.
- **DPS Red** (`#ef4444` light / `#f87171` dark): DPS role indicator.

### Named Rules

**The Gradient Reservation Rule.** The blue-purple-pink gradient (`135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%`) appears in exactly three contexts: page title text, avatar placeholders, and active navigation backgrounds. It is never applied to buttons, cards, backgrounds, or decorative elements. Its scarcity makes it the brand signature.

**The Blue Accent Rule.** Linkshell Blue is the sole interactive accent. It marks links, focus rings, active states, and hover glows. No other saturated color competes for interactive attention.

## 3. Typography

**System Font Stack:** `ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Ubuntu, Cantarell, Noto Sans, Helvetica Neue, Arial`

**Character:** A single system-native sans-serif carries the entire hierarchy. No display font, no secondary face. The tool earns its personality through weight contrast and tightened letter-spacing at display scale, not through typeface variety. Native feel on every platform; zero font-loading latency.

### Hierarchy

- **Display** (700, `2rem`, line-height 1.2, letter-spacing `-0.02em`): Page titles only. Rendered with the brand gradient via `background-clip: text`. The tightest tracking in the system.
- **Headline** (600, `1.5rem`, line-height 1.3): Modal titles, section subheadings, feature headings. No gradient treatment.
- **Title** (600, `1.125rem`, line-height 1.4): Section headers, card titles, form group labels. The workhorse heading size.
- **Body** (400, `1rem`, line-height 1.6): Prose, descriptions, form help text. Cap at 65-75ch for readability in content areas; data-dense screens (tables, member lists) run wider.
- **Label** (500, `0.9rem`, line-height 1.4): Form labels, metadata, timestamps, badge text. Muted color by default.

### Named Rules

**The One Family Rule.** Every text element uses the system sans-serif stack. No decorative fonts, no monospace for UI labels, no serif accents. Hierarchy is expressed through weight (400/500/600/700) and size alone.

## 4. Elevation: Shadow-Assisted Glass

Depth is conveyed through two co-equal systems working together: glassmorphism transparency (backdrop blur + semi-transparent backgrounds) provides the primary layering, while box-shadows ground surfaces and signal interactive states. Neither system works alone; the glass creates visual separation, the shadows anchor it.

### Shadow Vocabulary

- **Ambient Rest** (`0 4px 16px rgba(0, 0, 0, 0.08)` light / `0 4px 16px rgba(0, 0, 0, 0.3)` dark, plus `inset 0 1px 0 rgba(255, 255, 255, 0.5/0.08)`): Default state for cards, containers, header, tables. The inset top-edge highlight is the signature of the glass aesthetic.
- **Hover Glow** (`0 8px 32px rgba(59, 130, 246, 0.15/0.25)` + ambient + enhanced inset): Interactive hover on cards and containers. Blue-tinted outer glow signals interactivity. Blur increases from `20px` to `24px` simultaneously.
- **Elevation Standard** (`var(--elev)`: `0 10px 20px rgba(0, 0, 0, 0.06)` light / `0 10px 24px rgba(0, 0, 0, 0.35)` dark): Tooltips, dropdowns, floating elements. Deeper than ambient, no inset highlight.
- **Modal** (`var(--elev)` + `0 20px 40px rgba(0, 0, 0, 0.15)`): Maximum elevation for modal overlays.
- **Subtle** (`0 1px 3px rgba(0, 0, 0, 0.08-0.1)`): Badges, chips, small floating indicators. Barely there.
- **Active Navigation** (`0 2px 8px rgba(59, 130, 246, 0.2)` light / `0.3` dark): Blue-tinted shadow under active nav links, reinforcing the gradient background.

### Glass Properties

Every glass surface shares: `backdrop-filter: blur(20px)`, semi-transparent background (70% opacity), and a `2px` border with reduced opacity. Hover deepens blur to `24px` and shifts border color to blue-tinted.

| Layer | Light Background | Dark Background |
|---|---|---|
| Card / Container | `rgba(255,255,255,0.7)` | `rgba(18,26,45,0.7)` |
| Header | `rgba(255,255,255,0.6)` | `rgba(18,26,45,0.6)` |
| Footer | `rgba(255,255,255,0.08)` | `rgba(18,26,45,0.12)` |
| Input | `rgba(255,255,255,0.5)` | `rgba(15,23,42,0.5)` |

### Named Rules

**The Co-Equal Depth Rule.** Glass and shadow always appear together on elevated surfaces. A transparent surface without a shadow floats ambiguously. A shadow without glass transparency looks dated. Both systems present, always.

## 5. Components

### Buttons

Precise and responsive. Three variants (elevated, outlined, text), five states (primary, secondary, tertiary, danger, pressed), three sizes.

- **Shape:** Gently rounded (12px / `rounded-lg`). Full-round for icon-only buttons.
- **Primary Elevated:** Solid blue (`#2563eb`) with white text. Hover darkens to `#1d4ed8`. Shadow on hover (`shadow-lg`). The default action button.
- **Outlined:** Transparent background, `2px` colored border. Hover fills with a `50`-shade tint. Dark mode adjusts to `400`-shade text and `950`-shade hover fill.
- **Text:** No border, no background. Hover fills with a `50`-shade tint. The lightest commitment.
- **Focus:** `ring-2 ring-offset-2` in the state's accent color. Always visible on keyboard navigation.
- **Disabled:** `opacity: 0.5`, `pointer-events: none`.
- **Sizes:** Small (`px-3 py-1.5 text-sm`), Medium (`px-4 py-2 text-base`), Large (`px-6 py-3 text-lg`).
- **Transition:** `200ms` on all properties.

### Chips / Badges / Pills

Status indicators with full glass treatment.

- **Badge:** Pill-shaped (`999px` radius), `12px` font, `backdrop-filter: blur(10px)`. Semantic variants: default (gray), admin (red-tinted), member (green-tinted). Subtle `0 1px 3px` shadow.
- **Chip:** Same shape as badge, slightly larger padding (`0.25rem 0.75rem`). Used for filter states and categorization.
- **Pill (on/off):** Boolean indicators. Green for on, red for off. Semi-transparent backgrounds with `0.18` opacity in dark mode.

### Cards / Containers

The primary content surface. Glassmorphism is the defining aesthetic.

- **Corner Style:** Generously rounded (16px).
- **Background:** Semi-transparent glass. Light: `rgba(255,255,255,0.7)` with `blur(20px)`. Dark: `rgba(18,26,45,0.7)`.
- **Border:** `2px solid` at reduced opacity. Light: `rgba(255,255,255,0.4)`. Dark: `rgba(255,255,255,0.15)`.
- **Shadow:** Ambient Rest shadow with inset top-edge highlight.
- **Hover:** Border shifts to blue-tinted (`rgba(59,130,246,0.4/0.5)`), blur deepens to `24px`, blue Hover Glow shadow appears.
- **Internal Padding:** `1rem` comfortable, `0.625rem 0.75rem` compact.
- **Variants:** Elevated (default, full glass + shadow), Outlined (no shadow), Flat (no border, no shadow).
- **Grid:** `repeat(auto-fill, minmax(Xpx, 1fr))` with `1rem` gap. Small cards at `200px` min, medium at `300px`, large at `600px`.

### Inputs / Fields

Glass-integrated form controls that feel native to the surface they sit on.

- **Style:** Semi-transparent background (`rgba(255,255,255,0.5)` light, `rgba(15,23,42,0.5)` dark), `blur(10px)`, `1px` border at reduced opacity. Rounded at `12px`.
- **Padding:** `0.625rem 0.875rem`.
- **Focus:** Border shifts to Linkshell Blue (`#3b82f6`), focus ring `0 0 0 3px` at `rgba(59,130,246,0.1/0.4)`.
- **Transition:** `200ms ease` on all properties.

### Navigation

Header-integrated nav with glass treatment, matching the app shell.

- **Link Style:** `0.5rem 1rem` padding, `10px` border-radius. System font at body weight.
- **Hover:** Subtle blue tint background `rgba(59,130,246,0.1)`.
- **Active:** Brand gradient background (`135deg, rgba(59,130,246,0.15) to rgba(147,51,234,0.15)`), blue-tinted shadow, white text.
- **Gap:** `0.375rem` between links. Tight but breathable.

### Glassmorphism Table

Data tables receive full glass treatment rather than sitting on opaque backgrounds.

- **Background:** Same glass properties as cards. `16px` corner radius with `overflow: hidden`.
- **Cell Padding:** `0.75rem`.
- **Row Hover:** Tinted with `color-mix(in oklab, var(--card) 90%, var(--link) 10%)`.
- **Header:** Bold weight, left-aligned, same padding as cells.
- **Border:** `1px solid var(--border)` between rows.

### Modal

Maximum elevation surface for focused tasks.

- **Backdrop:** Semi-transparent overlay (`rgba(255,255,255,0.5)` light / `rgba(0,0,0,0.35)` dark).
- **Container:** Full glass treatment, `16px` radius, combined `var(--elev)` + `0 20px 40px` shadow. Max-widths: small `500px`, medium `900px`, large `1300px`.
- **Entry Animation:** `scale(0.95) translateY(-10px)` to rest, `250ms ease`. Exit reverses in `200ms`.
- **Close Button:** `8px` radius, ghost style. Focus ring on keyboard.

### Toggle Switch

- **Track:** `44px` wide, `24px` tall, `999px` radius. Unchecked: `#d1d5db`. Checked: Verdant Success (`#10b981`).
- **Knob:** `18px` circle, white, `0 1px 2px` shadow. Translates `20px` on check.
- **Transition:** `200ms`.

## 6. Do's and Don'ts

### Do:

- **Do** use the brand gradient exclusively for page titles (`background-clip: text`), avatar placeholders, and active navigation. Three contexts, no exceptions.
- **Do** pair every glass surface with both backdrop blur and a box-shadow. The Co-Equal Depth Rule is load-bearing.
- **Do** use the inset `0 1px 0` highlight on all glass containers. It is the signature finish of the system.
- **Do** brighten accent and semantic colors in dark mode (blue `#3b82f6` to `#60a5fa`, danger `#dc2626` to `#f87171`). Dark mode is not "same colors on a dark background."
- **Do** keep all transitions at `200ms ease`. Consistency in timing is more important than per-element tuning.
- **Do** use `color-mix(in oklab, ...)` for computed tints (table row hover, subtle backgrounds). It keeps derived colors perceptually consistent.
- **Do** honor the FFXIV role color system (tank blue, healer green, DPS red) wherever player roles are displayed. These are functional, not decorative.

### Don't:

- **Don't** apply the brand gradient to buttons, card backgrounds, section dividers, or any surface. It is reserved for text and navigation state. Violating this dilutes the brand signature.
- **Don't** use generic SaaS dashboard layouts: hero-metric grids, identical card grids with icon + heading + text, or corporate admin templates. This serves a community, not a business.
- **Don't** add fantasy borders, neon glowing accents, themed textures, or any FFXIV-aesthetic decoration. The game connection is contextual data, not visual chrome.
- **Don't** use `border-left` or `border-right` greater than `1px` as a colored accent stripe on cards, alerts, or list items. Rewrite with full borders, background tints, or nothing.
- **Don't** use gradient text outside of the Display type role (page titles). Gradient on body text, labels, or buttons is prohibited.
- **Don't** use decorative motion: entrance choreography, scroll-driven animations, loading sequences. Motion conveys state changes only.
- **Don't** introduce a second font family. The system stack carries everything. No decorative serif, no monospace for UI elements, no display font.
- **Don't** use `#000` or `#fff` as literal values. Every neutral is tinted toward the slate hue family.
- **Don't** use glassmorphism purely for decoration (blur effects without functional purpose). Every glass surface must contain or group content.
