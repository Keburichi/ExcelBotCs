<script lang="ts" setup>

</script>

<template>
  <section class="rules container">
    <div class="card">
      <div class="card__header">
        <h2 class="card__title">
          Rules:
        </h2>
      </div>
      <div class="card__body rules__body">
        <!-- Rules -->
        <p class="rules__paragraph rules__paragraph--heading">
          <span aria-hidden="true" class="section-badge section-badge--rule">Rule 1</span>
          Don't be a dick
        </p>
        <p class="rules__paragraph rules__paragraph--heading">
          <span aria-hidden="true" class="section-badge section-badge--rule">Rule 2</span>
          Honour system of the FC chest:
        </p>

        <!-- Intro -->
        <p class="rules__paragraph">
          Here is what is available in the FC chest to <b>all members!</b>
        </p>

        <!-- Slot 1 -->
        <p class="rules__paragraph rules__slot-title">
          <span aria-hidden="true" class="section-badge section-badge--slot">Slot 1</span>
          <b><u>Collectables Exchange</u></b>
        </p>
        <p class="rules__paragraph">
          This is where any collectables such as minions and orchestrion rolls have been
          deposited. All members have full access to this slot and you are welcome to take anything that you have not
          yet collected.
        </p>
        <p class="rules__paragraph">
          Please don't take items to sell on the marketboard, it is just to share any
          collectables.
        </p>
        <p class="rules__paragraph">
          Naturally we would appreciate any donations if you have any collectables that you
          already have and would like to pass it on.
        </p>

        <!-- Slot 4 -->
        <p class="rules__paragraph rules__slot-title">
          <span aria-hidden="true" class="section-badge section-badge--slot slot-4">Slot 4</span>
          <b><u>FC Pantry</u></b>
        </p>
        <p class="rules__paragraph">
          This slot is for food and pots. Please take as and when you need and not in huge
          bulks (basically ensure that there's enough stuff in there for everyone).
        </p>
        <p class="rules__paragraph">
          1) Old food and pots, including crafting/gathering food. Feel free to take as needed
          if you just need something to help with levelling, etc.
        </p>
        <p class="rules__paragraph">
          2) Current BiS raid food intended for Excel Academy. Please only take 6 prior to
          Excel Academy raid nights. This is paid for by the FC and is intended to help support anyone who raids with
          the FC.
        </p>

        <!-- Slot 5 -->
        <p class="rules__paragraph rules__slot-title">
          <span aria-hidden="true" class="section-badge section-badge--slot slot-5">Slot 5</span>
          <b><u>Dyes & Misc</u></b>
        </p>
        <p class="rules__paragraph">
          If you're looking for a particular dye, we have a huge amount available free to take
          in Slot 5. Feel free to deposit any as well. We also have magicked prisms if you want. No real restriction to
          these.
        </p>
      </div>
    </div>
  </section>
</template>

<style scoped>
/* Layout: let the global .container set the width for consistency */
.rules {
  margin: 0 auto;
}

/* Typography and spacing for paragraphs */
.rules__body {
  line-height: 1.7;
}

.rules__paragraph {
  margin: 0 0 .75rem;
  color: var(--fg);
}

.rules__paragraph:last-child {
  margin-bottom: 0;
}

/* Make heading-like paragraphs stand out slightly */
.rules__paragraph--heading {
  font-weight: 600;
}

.rules__slot-title {
  font-weight: 700;
  margin-top: 1rem;
  padding-top: .25rem;
  border-top: 1px dashed var(--border);
}

/* Section badges (inspired by existing .badge styles) */
.section-badge {
  display: inline-block;
  margin-right: .5rem;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
  vertical-align: middle;
  border: 1px solid var(--card-border);
  background: var(--badge-bg);
  color: var(--badge-fg);
}

/* High-contrast badge variants for light theme (default) */
.section-badge--rule {
  background: #FEF3C7; /* amber-100 */
  color: #7C2D12; /* amber-900 */
  border-color: #F59E0B; /* amber-500 */
}

.section-badge--slot {
  background: #DBEAFE; /* blue-100 */
  color: #1E3A8A; /* blue-900 */
  border-color: #3B82F6; /* blue-500 */
}

/* Slightly differentiate individual slots */
.section-badge.slot-4 {
  background: #D1FAE5; /* emerald-100 */
  color: #065F46; /* emerald-900 */
  border-color: #10B981; /* emerald-500 */
}

.section-badge.slot-5 {
  background: #FEE2E2; /* red-100 */
  color: #7F1D1D; /* red-900 */
  border-color: #EF4444; /* red-500 */
}

/* Dark theme overrides for strong contrast */
/* Fixed: Put entire selector inside :global() to prevent Firefox bug */
:global([data-theme='dark'] .rules .section-badge--rule) {
  background: #78350f; /* amber-900 */
  color: #fde68a; /* amber-200 */
  border-color: #f59e0b; /* amber-500 */
}

:global([data-theme='dark'] .rules .section-badge--slot) {
  background: #1e3a8a; /* blue-900 */
  color: #bfdbfe; /* blue-200 */
  border-color: #60a5fa; /* blue-400 */
}

:global([data-theme='dark'] .rules .section-badge.slot-4) {
  background: #064e3b; /* emerald-900 */
  color: #a7f3d0; /* emerald-200 */
  border-color: #34d399; /* emerald-400 */
}

:global([data-theme='dark'] .rules .section-badge.slot-5) {
  background: #7f1d1d; /* red-900 */
  color: #fca5a5; /* red-300 */
  border-color: #f87171; /* red-400 */
}
</style>
