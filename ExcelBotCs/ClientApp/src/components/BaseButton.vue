<script lang="ts" setup>
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  state?: 'primary' | 'secondary' | 'tertiary' | 'danger' | 'pressed'
  title?: string
  disabled?: boolean
  icon?: string
  iconPosition?: 'left' | 'right'
  size?: 'small' | 'medium' | 'large'
  variant?: 'elevated' | 'outlined' | 'text'
  clickable?: boolean
  tooltip?: string
  iconOnly?: boolean
  rounded?: boolean
}>(), {
  state: 'primary',
  title: '',
  disabled: false,
  iconPosition: 'left',
  variant: 'elevated',
  clickable: true,
  size: 'medium',
  iconOnly: false,
  rounded: false,
})

const emit = defineEmits<{ (e: 'clicked'): void }>()

const buttonClasses = computed(() => [
  'base-btn',
  `base-btn--${props.variant}`,
  `base-btn--${props.state}`,
  `base-btn--${props.size}`,
  {
    'base-btn--disabled': props.disabled,
    'base-btn--icon-only': props.iconOnly,
    'base-btn--rounded': props.rounded,
    'base-btn--icon-right': props.iconPosition === 'right',
    'base-btn--not-clickable': props.clickable === false,
  },
])
</script>

<template>
  <button
    :aria-label="props.iconOnly ? (props.tooltip || props.title) : undefined"
    :class="buttonClasses"
    :data-tooltip="props.tooltip"
    :disabled="disabled"
    type="button"
    @click="emit('clicked')"
  >
    <slot name="icon">
      <span v-if="props.icon" class="base-btn__icon">
        {{ props.icon }}
      </span>
    </slot>
    <span v-if="!props.iconOnly">{{ props.title }}</span>
  </button>
</template>

<style scoped>
.base-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  border: 2px solid transparent;
  cursor: pointer;
  transition: all 0.2s ease;
  border-radius: 8px;
}

.base-btn:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--ring);
}

/* Sizes */
.base-btn--small { padding: 0.375rem 0.75rem; font-size: 0.875rem; gap: 0.375rem; }
.base-btn--medium { padding: 0.5rem 1rem; font-size: 1rem; gap: 0.5rem; }
.base-btn--large { padding: 0.75rem 1.5rem; font-size: 1.125rem; gap: 0.625rem; }

/* Elevated — primary */
.base-btn--elevated.base-btn--primary { background: #2563eb; color: #fff; box-shadow: 0 4px 6px -1px rgba(37,99,235,.3); }
.base-btn--elevated.base-btn--primary:hover { background: #1d4ed8; box-shadow: 0 6px 10px -1px rgba(37,99,235,.4); }
.base-btn--elevated.base-btn--primary:active { background: #1e40af; }

/* Elevated — secondary */
.base-btn--elevated.base-btn--secondary { background: #4b5563; color: #fff; box-shadow: 0 4px 6px -1px rgba(75,85,99,.3); }
.base-btn--elevated.base-btn--secondary:hover { background: #374151; box-shadow: 0 6px 10px -1px rgba(75,85,99,.4); }
.base-btn--elevated.base-btn--secondary:active { background: #1f2937; }

/* Elevated — tertiary */
.base-btn--elevated.base-btn--tertiary { background: #7c3aed; color: #fff; box-shadow: 0 4px 6px -1px rgba(124,58,237,.3); }
.base-btn--elevated.base-btn--tertiary:hover { background: #6d28d9; box-shadow: 0 6px 10px -1px rgba(124,58,237,.4); }
.base-btn--elevated.base-btn--tertiary:active { background: #5b21b6; }

/* Elevated — danger */
.base-btn--elevated.base-btn--danger { background: #dc2626; color: #fff; box-shadow: 0 4px 6px -1px rgba(220,38,38,.3); }
.base-btn--elevated.base-btn--danger:hover { background: #b91c1c; box-shadow: 0 6px 10px -1px rgba(220,38,38,.4); }
.base-btn--elevated.base-btn--danger:active { background: #991b1b; }

/* Elevated — pressed */
.base-btn--elevated.base-btn--pressed { background: #059669; color: #fff; box-shadow: 0 4px 6px -1px rgba(5,150,105,.3); }
.base-btn--elevated.base-btn--pressed:hover { background: #047857; box-shadow: 0 6px 10px -1px rgba(5,150,105,.4); }
.base-btn--elevated.base-btn--pressed:active { background: #065f46; }

/* Outlined — primary */
.base-btn--outlined.base-btn--primary { background: transparent; color: #2563eb; border-color: #2563eb; }
.base-btn--outlined.base-btn--primary:hover { background: rgba(37,99,235,0.05); }
.base-btn--outlined.base-btn--primary:active { background: rgba(37,99,235,0.1); }

/* Outlined — secondary */
.base-btn--outlined.base-btn--secondary { background: transparent; color: #4b5563; border-color: #4b5563; }
.base-btn--outlined.base-btn--secondary:hover { background: rgba(75,85,99,0.05); }
.base-btn--outlined.base-btn--secondary:active { background: rgba(75,85,99,0.1); }

/* Outlined — tertiary */
.base-btn--outlined.base-btn--tertiary { background: transparent; color: #7c3aed; border-color: #7c3aed; }
.base-btn--outlined.base-btn--tertiary:hover { background: rgba(124,58,237,0.05); }
.base-btn--outlined.base-btn--tertiary:active { background: rgba(124,58,237,0.1); }

/* Outlined — danger */
.base-btn--outlined.base-btn--danger { background: transparent; color: #dc2626; border-color: #dc2626; }
.base-btn--outlined.base-btn--danger:hover { background: rgba(220,38,38,0.05); }
.base-btn--outlined.base-btn--danger:active { background: rgba(220,38,38,0.1); }

/* Outlined — pressed */
.base-btn--outlined.base-btn--pressed { background: transparent; color: #059669; border-color: #059669; }
.base-btn--outlined.base-btn--pressed:hover { background: rgba(5,150,105,0.05); }
.base-btn--outlined.base-btn--pressed:active { background: rgba(5,150,105,0.1); }

/* Text — primary */
.base-btn--text.base-btn--primary { background: transparent; color: #2563eb; border-color: transparent; }
.base-btn--text.base-btn--primary:hover { background: rgba(37,99,235,0.05); }
.base-btn--text.base-btn--primary:active { background: rgba(37,99,235,0.1); }

/* Text — secondary */
.base-btn--text.base-btn--secondary { background: transparent; color: #4b5563; border-color: transparent; }
.base-btn--text.base-btn--secondary:hover { background: rgba(75,85,99,0.05); }
.base-btn--text.base-btn--secondary:active { background: rgba(75,85,99,0.1); }

/* Text — tertiary */
.base-btn--text.base-btn--tertiary { background: transparent; color: #7c3aed; border-color: transparent; }
.base-btn--text.base-btn--tertiary:hover { background: rgba(124,58,237,0.05); }
.base-btn--text.base-btn--tertiary:active { background: rgba(124,58,237,0.1); }

/* Text — danger */
.base-btn--text.base-btn--danger { background: transparent; color: #dc2626; border-color: transparent; }
.base-btn--text.base-btn--danger:hover { background: rgba(220,38,38,0.05); }
.base-btn--text.base-btn--danger:active { background: rgba(220,38,38,0.1); }

/* Text — pressed */
.base-btn--text.base-btn--pressed { background: transparent; color: #059669; border-color: transparent; }
.base-btn--text.base-btn--pressed:hover { background: rgba(5,150,105,0.05); }
.base-btn--text.base-btn--pressed:active { background: rgba(5,150,105,0.1); }

/* Modifiers */
.base-btn--disabled { opacity: 0.5; cursor: not-allowed; pointer-events: none; }
.base-btn--rounded { border-radius: 9999px; }
.base-btn--icon-only { padding: 0; width: 2rem; height: 2rem; }
.base-btn--icon-right { flex-direction: row-reverse; }
.base-btn--not-clickable { cursor: default; }

.base-btn__icon { display: inline-flex; align-items: center; }
</style>

<!-- Dark mode overrides (unscoped — :root selectors can't match from scoped styles) -->
<style>
:root[data-theme='dark'] .base-btn--outlined.base-btn--primary,
:root[data-theme='dark'] .base-btn--text.base-btn--primary { color: #60a5fa; border-color: #60a5fa; }
:root[data-theme='dark'] .base-btn--outlined.base-btn--primary:hover,
:root[data-theme='dark'] .base-btn--text.base-btn--primary:hover { background: rgba(96,165,250,0.1); }

:root[data-theme='dark'] .base-btn--outlined.base-btn--secondary,
:root[data-theme='dark'] .base-btn--text.base-btn--secondary { color: #9ca3af; border-color: #9ca3af; }
:root[data-theme='dark'] .base-btn--outlined.base-btn--secondary:hover,
:root[data-theme='dark'] .base-btn--text.base-btn--secondary:hover { background: rgba(156,163,175,0.1); }

:root[data-theme='dark'] .base-btn--outlined.base-btn--tertiary,
:root[data-theme='dark'] .base-btn--text.base-btn--tertiary { color: #a78bfa; border-color: #a78bfa; }
:root[data-theme='dark'] .base-btn--outlined.base-btn--tertiary:hover,
:root[data-theme='dark'] .base-btn--text.base-btn--tertiary:hover { background: rgba(167,139,250,0.1); }

:root[data-theme='dark'] .base-btn--outlined.base-btn--danger,
:root[data-theme='dark'] .base-btn--text.base-btn--danger { color: #f87171; border-color: #f87171; }
:root[data-theme='dark'] .base-btn--outlined.base-btn--danger:hover,
:root[data-theme='dark'] .base-btn--text.base-btn--danger:hover { background: rgba(248,113,113,0.1); }

:root[data-theme='dark'] .base-btn--outlined.base-btn--pressed,
:root[data-theme='dark'] .base-btn--text.base-btn--pressed { color: #34d399; border-color: #34d399; }
:root[data-theme='dark'] .base-btn--outlined.base-btn--pressed:hover,
:root[data-theme='dark'] .base-btn--text.base-btn--pressed:hover { background: rgba(52,211,153,0.1); }

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .base-btn--outlined.base-btn--primary,
  :root:not([data-theme='light']) .base-btn--text.base-btn--primary { color: #60a5fa; border-color: #60a5fa; }
  :root:not([data-theme='light']) .base-btn--outlined.base-btn--primary:hover,
  :root:not([data-theme='light']) .base-btn--text.base-btn--primary:hover { background: rgba(96,165,250,0.1); }

  :root:not([data-theme='light']) .base-btn--outlined.base-btn--secondary,
  :root:not([data-theme='light']) .base-btn--text.base-btn--secondary { color: #9ca3af; border-color: #9ca3af; }
  :root:not([data-theme='light']) .base-btn--outlined.base-btn--secondary:hover,
  :root:not([data-theme='light']) .base-btn--text.base-btn--secondary:hover { background: rgba(156,163,175,0.1); }

  :root:not([data-theme='light']) .base-btn--outlined.base-btn--tertiary,
  :root:not([data-theme='light']) .base-btn--text.base-btn--tertiary { color: #a78bfa; border-color: #a78bfa; }
  :root:not([data-theme='light']) .base-btn--outlined.base-btn--tertiary:hover,
  :root:not([data-theme='light']) .base-btn--text.base-btn--tertiary:hover { background: rgba(167,139,250,0.1); }

  :root:not([data-theme='light']) .base-btn--outlined.base-btn--danger,
  :root:not([data-theme='light']) .base-btn--text.base-btn--danger { color: #f87171; border-color: #f87171; }
  :root:not([data-theme='light']) .base-btn--outlined.base-btn--danger:hover,
  :root:not([data-theme='light']) .base-btn--text.base-btn--danger:hover { background: rgba(248,113,113,0.1); }

  :root:not([data-theme='light']) .base-btn--outlined.base-btn--pressed,
  :root:not([data-theme='light']) .base-btn--text.base-btn--pressed { color: #34d399; border-color: #34d399; }
  :root:not([data-theme='light']) .base-btn--outlined.base-btn--pressed:hover,
  :root:not([data-theme='light']) .base-btn--text.base-btn--pressed:hover { background: rgba(52,211,153,0.1); }
}
</style>
