# Excelsior FC Website Design Guidelines

**Version:** 1.0
**Last Updated:** 2025-11-24
**Design System:** Modern Glassmorphism with Gradient Accents

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Color Palette](#color-palette)
3. [Typography](#typography)
4. [Glassmorphism Effects](#glassmorphism-effects)
5. [Hover & Interactive States](#hover--interactive-states)
6. [Component Patterns](#component-patterns)
7. [Spacing & Layout](#spacing--layout)
8. [Theme Support](#theme-support)
9. [Responsive Design](#responsive-design)
10. [Accessibility](#accessibility)

---

## Design Philosophy

The Excelsior FC website uses a **modern glassmorphism aesthetic** with vibrant gradient accents. The design emphasizes:

- **Clarity**: Glass effects with proper blur ensure content remains readable
- **Depth**: Layered shadows and inset highlights create visual hierarchy
- **Interactivity**: Subtle hover effects with blue accent colors provide feedback
- **Consistency**: Unified design language across all components
- **Accessibility**: WCAG AA compliance with proper contrast ratios

---

## Color Palette

### Brand Colors

The primary brand gradient flows from blue through purple to pink:

```css
/* Primary Brand Gradient */
background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);

/* Individual Brand Colors */
--brand-blue: #3b82f6;    /* rgb(59, 130, 246) */
--brand-purple: #8b5cf6;  /* rgb(139, 92, 246) */
--brand-pink: #ec4899;    /* rgb(236, 72, 153) */
```

### Theme Colors

#### Light Theme

```css
--bg: #ffffff;
--fg: #0f172a;           /* slate-900 */
--muted: #64748b;        /* slate-500 */
--border: #e5e7eb;       /* gray-200 */
--card: #ffffff;
--link: #3b82f6;         /* blue-500 */
```

#### Dark Theme

```css
--bg: #0b1020;           /* deep slate */
--fg: #e5e7eb;           /* gray-200 */
--muted: #94a3b8;        /* slate-400 */
--border: #273043;       /* dark border */
--card: #121a2d;
--link: #60a5fa;         /* blue-400 */
```

### Interactive Colors

```css
/* Blue Accent (for hover states, borders) */
--accent-blue-light: rgba(59, 130, 246, 0.3);   /* Light theme borders */
--accent-blue-dark: rgba(59, 130, 246, 0.5);    /* Dark theme borders */

/* Blue Glow (for hover shadows) */
--glow-light: rgba(59, 130, 246, 0.15);
--glow-dark: rgba(59, 130, 246, 0.25);
```

### Background Gradients

```css
/* Animated Background - Light Theme */
background: linear-gradient(135deg,
  rgba(59, 130, 246, 0.15) 0%,    /* Blue */
  rgba(147, 51, 234, 0.15) 50%,   /* Purple */
  rgba(236, 72, 153, 0.15) 100%   /* Pink */
);

/* Animated Background - Dark Theme */
background: linear-gradient(135deg,
  rgba(59, 130, 246, 0.18) 0%,
  rgba(147, 51, 234, 0.18) 50%,
  rgba(236, 72, 153, 0.18) 100%
);
```

---

## Typography

### Page Titles (Gradient Text)

All main page headings use gradient text for brand consistency:

```css
.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  color: transparent;
  letter-spacing: -0.02em;
}
```

**Usage**: Main page headings (Events, Lottery, Members, etc.)

### Section Headers

Section headers within glassmorphism containers:

```css
.section-header {
  margin: 0 0 1.25rem 0;
  font-size: 1.125rem;        /* 18px */
  font-weight: 600;
  color: var(--fg);
  padding-bottom: 0.75rem;
  border-bottom: 1px solid rgba(var(--color-border), 0.3);
}
```

**Usage**: Headers within form sections, card groups

### Subheadings

```css
.section-subheading {
  font-size: 1.5rem;          /* 24px */
  font-weight: 600;
  color: var(--fg);
  margin: 2rem 0 1rem 0;
}
```

**Usage**: Subsection titles, secondary headings

### Body Text

```css
/* Labels */
label {
  font-weight: 500;
  font-size: 0.9rem;          /* ~14px */
  color: var(--fg);
}

/* Regular text */
body {
  font-size: 1rem;            /* 16px base */
  line-height: 1.6;
  color: var(--fg);
}
```

---

## Glassmorphism Effects

### Base Glass Container (Cards, Sections, Forms)

#### Light Theme

```css
.glass-container {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}
```

#### Dark Theme

```css
:root[data-theme='dark'] .glass-container {
  background: rgba(18, 26, 45, 0.7);
  border: 2px solid rgba(255, 255, 255, 0.15);
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}
```

### Glassmorphism Values Reference

| Property               | Light Theme | Dark Theme | Purpose                   |
|------------------------|-------------|------------|---------------------------|
| **Background Opacity** | `0.7`       | `0.7`      | Semi-transparent base     |
| **Backdrop Blur**      | `20px`      | `20px`     | Standard blur amount      |
| **Border Width**       | `2px`       | `2px`      | Visible container outline |
| **Border Opacity**     | `0.4`       | `0.15`     | Subtle definition         |
| **Shadow Opacity**     | `0.08`      | `0.3`      | Depth perception          |
| **Inset Highlight**    | `0.5`       | `0.08`     | Top shine effect          |
| **Border Radius**      | `16px`      | `16px`     | Rounded corners           |

### Header Glass Effect

The sticky navigation uses slightly stronger opacity:

```css
.app-header {
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
}

:root[data-theme='dark'] .app-header {
  background: rgba(18, 26, 45, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.1);
}
```

### Footer Glass Effect (Subtle)

Footer uses minimal opacity for subtle presence:

```css
.app-footer {
  background: rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.06);
}

:root[data-theme='dark'] .app-footer {
  background: rgba(18, 26, 45, 0.12);
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.15);
}
```

### Animated Gradient Orbs

Background orbs create depth and visual interest:

```css
.gradient-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.7;
  animation: float 20s ease-in-out infinite;
  will-change: transform;
}

/* Orb sizes and positions */
.orb-1 {
  width: 500px;
  height: 500px;
  top: 10%;
  left: 5%;
  background: radial-gradient(circle,
    rgba(59, 130, 246, 0.5) 0%,
    rgba(59, 130, 246, 0.1) 70%
  );
  animation: float1 20s ease-in-out infinite;
}

@keyframes float1 {
  0%, 100% { transform: translate(0, 0) scale(1); }
  33% { transform: translate(50px, -40px) scale(1.1); }
  66% { transform: translate(-30px, 40px) scale(0.95); }
}
```

---

## Hover & Interactive States

### Modern Glassmorphism Hover (No Movement)

The design uses blur enhancement and colored glows instead of translateY movement:

#### Light Theme

```css
.glass-container:hover {
  backdrop-filter: blur(24px);                    /* Enhanced blur */
  border-color: rgba(59, 130, 246, 0.4);         /* Blue accent */
  box-shadow:
    0 8px 32px rgba(59, 130, 246, 0.15),         /* Blue glow */
    0 4px 16px rgba(0, 0, 0, 0.1),               /* Base shadow */
    inset 0 1px 0 rgba(255, 255, 255, 0.6);      /* Enhanced highlight */
}
```

#### Dark Theme

```css
:root[data-theme='dark'] .glass-container:hover {
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.5);
  box-shadow:
    0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
}
```

### Hover Effect Breakdown

| Property                 | Default                  | Hover                   | Change          |
|--------------------------|--------------------------|-------------------------|-----------------|
| **Backdrop Blur**        | `20px`                   | `24px`                  | +4px for depth  |
| **Border Color (Light)** | `rgba(255,255,255,0.4)`  | `rgba(59,130,246,0.4)`  | Blue accent     |
| **Border Color (Dark)**  | `rgba(255,255,255,0.15)` | `rgba(59,130,246,0.5)`  | Blue accent     |
| **Blue Glow (Light)**    | None                     | `rgba(59,130,246,0.15)` | Added           |
| **Blue Glow (Dark)**     | None                     | `rgba(59,130,246,0.25)` | Added           |
| **Transform**            | None                     | None                    | **No movement** |

### Link Hover States

```css
.nav a:hover {
  background: rgba(59, 130, 246, 0.1);
}

.nav a.router-link-active {
  background: linear-gradient(135deg,
    rgba(59, 130, 246, 0.15) 0%,
    rgba(147, 51, 234, 0.15) 100%
  );
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.2);
}
```

---

## Component Patterns

### Form Sections

Forms are organized into glassmorphism sections with headers:

```vue
<section class="form-section">
  <h3 class="section-header">
    Section Name
  </h3>
  <div class="form-row">
    <label>Field Label</label>
    <input type="text" placeholder="Placeholder">
  </div>
</section>
```

**Styling**: Full glassmorphism effect with 2px border, hover effects

### Card Avatars

Avatar containers use layered gradients for visual interest:

#### Light Theme

```css
.card__avatar_container {
  background:
    linear-gradient(135deg,
      rgba(59, 130, 246, 0.6) 0%,
      rgba(139, 92, 246, 0.6) 50%,
      rgba(236, 72, 153, 0.6) 100%
    ),
    linear-gradient(to bottom, #f0f4f8, #d9e2ec);
}
```

#### Dark Theme

```css
:root[data-theme='dark'] .card__avatar_container {
  background:
    linear-gradient(135deg,
      rgba(59, 130, 246, 0.4) 0%,
      rgba(139, 92, 246, 0.4) 100%
    ),
    linear-gradient(to bottom, #1e293b, #0f172a);
}
```

**Purpose**: Balances visibility with subtlety (0.4 opacity in dark theme)

### Sticky Navigation

The header becomes sticky and expands to edges when scrolling:

```css
.app-header {
  position: sticky;
  top: -1rem;              /* Accounts for container padding */
  z-index: 1000;           /* Above all content */
  margin-left: -1rem;      /* Expands to edges */
  margin-right: -1rem;
  margin-top: -1rem;
  transition: all 0.3s ease;
}
```

### Tab Navigation

```css
.tab-navigation {
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(20px);
  border-radius: 14px;
  padding: 0.5rem;
  display: flex;
  gap: 0.375rem;
}

.tab-link.active {
  background: linear-gradient(135deg,
    rgba(59, 130, 246, 0.2) 0%,
    rgba(147, 51, 234, 0.2) 100%
  );
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.25);
}
```

---

## Spacing & Layout

### Standard Spacing Scale

```css
/* Spacing values (rem) */
--space-xs: 0.25rem;   /* 4px */
--space-sm: 0.5rem;    /* 8px */
--space-md: 1rem;      /* 16px */
--space-lg: 1.5rem;    /* 24px */
--space-xl: 2rem;      /* 32px */
--space-2xl: 3rem;     /* 48px */
--space-3xl: 4rem;     /* 64px */
```

### Component Spacing

| Component        | Margin/Padding        | Purpose                 |
|------------------|-----------------------|-------------------------|
| **Page Header**  | `margin-bottom: 2rem` | Separation from content |
| **Form Section** | `padding: 1.5rem`     | Internal spacing        |
| **Section Gap**  | `gap: 1.5rem`         | Between sections        |
| **Form Row**     | `margin-bottom: 1rem` | Between fields          |
| **Container**    | `padding: 1rem`       | Edge padding            |

### Container Max-Widths

```css
/* Page containers */
.page {
  max-width: 800px;        /* Forms, edit pages */
  margin: 0 auto;
}

.lottery-view {
  max-width: 1400px;       /* Wide layouts */
  margin: 0 auto;
}

.container {
  max-width: 1100px;       /* Standard content */
  margin: 0 auto;
}
```

### Flexbox Layout

```css
/* Sticky footer pattern */
.container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-content {
  flex: 1;                 /* Grows to fill space */
}

.app-footer {
  margin-top: auto;        /* Sticks to bottom */
}
```

---

## Theme Support

### Theme Detection

The website supports both explicit theme selection and system preference:

```css
/* Explicit dark theme */
:root[data-theme='dark'] {
  /* Dark theme styles */
}

/* System preference fallback */
@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) {
    /* Dark theme styles */
  }
}
```

### Theme-Aware Components

All glassmorphism components must include both theme variants:

```css
.component {
  /* Light theme (default) */
  background: rgba(255, 255, 255, 0.7);
}

:root[data-theme='dark'] .component {
  /* Dark theme */
  background: rgba(18, 26, 45, 0.7);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .component {
    /* System dark theme */
    background: rgba(18, 26, 45, 0.7);
  }
}
```

**Rule**: Always include both explicit and system preference dark theme support

---

## Responsive Design

### Breakpoints

```css
/* Mobile */
@media (max-width: 480px) {
  .form-section { padding: 1rem; }
  .section-header { font-size: 1rem; }
}

/* Tablet */
@media (max-width: 768px) {
  .form-section { padding: 1.25rem; }
  .media-row { grid-template-columns: 1fr; }
}

/* Desktop (default) */
/* No media query needed */
```

### Responsive Patterns

#### Two-Column Forms

```css
.form-row-group {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1rem;
}

@media (max-width: 768px) {
  .form-row-group {
    grid-template-columns: 1fr;
  }
}
```

#### Preset Button Grids

```css
.party-preset-buttons {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 0.5rem;
}

@media (max-width: 768px) {
  .party-preset-buttons {
    grid-template-columns: repeat(2, 1fr);
  }
}
```

---

## Accessibility

### Contrast Requirements

- **Text**: Minimum 4.5:1 contrast ratio (WCAG AA)
- **Large Text**: Minimum 3:1 contrast ratio
- **Interactive Elements**: Minimum 3:1 contrast ratio

### Glassmorphism Accessibility

```css
/* Ensure text has higher opacity than background */
.section-header {
  color: var(--fg);        /* Full opacity text */
}

.glass-container {
  background: rgba(255, 255, 255, 0.7);  /* Lower opacity background */
}
```

**Rule**: Text opacity should always be higher than background opacity for readability

### Footer Copyright Text

```css
.copyright-text {
  font-size: 0.875rem;
  font-weight: 500;
  letter-spacing: 0.5px;   /* Improves readability on glass */
  opacity: 0.85;           /* Higher than background */
  color: rgba(0, 0, 0, 0.75);
}
```

### Reduced Motion

```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### Focus States

All interactive elements must have visible focus states:

```css
input:focus, select:focus, textarea:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}
```

---

## Quick Reference

### Common CSS Patterns

#### Glassmorphism Card

```css
background: rgba(255, 255, 255, 0.7);
backdrop-filter: blur(20px);
border: 2px solid rgba(255, 255, 255, 0.4);
border-radius: 16px;
box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08), inset 0 1px 0 rgba(255, 255, 255, 0.5);
```

#### Gradient Page Title

```css
background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
-webkit-background-clip: text;
-webkit-text-fill-color: transparent;
background-clip: text;
color: transparent;
```

#### Modern Hover (No Movement)

```css
.element:hover {
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15), 0 4px 16px rgba(0, 0, 0, 0.1);
}
```

---

## Design Checklist

When creating new components:

- [ ] Glassmorphism effect applied with correct opacity values
- [ ] Both light and dark theme variants included
- [ ] System preference dark theme fallback added
- [ ] Hover effects use blur + glow (no translateY movement)
- [ ] Text has higher opacity than background
- [ ] Contrast ratio meets WCAG AA standards
- [ ] Responsive breakpoints defined
- [ ] Border radius is 16px (or 12-14px for smaller elements)
- [ ] Backdrop blur is 20px default, 24px on hover
- [ ] Blue accent color used for interactive states
- [ ] Spacing follows the standard scale
- [ ] Focus states are visible and accessible

---

**Document Version:** 1.0
**Maintained by:** Excelsior FC Development Team
**Last Review:** 2025-11-24
