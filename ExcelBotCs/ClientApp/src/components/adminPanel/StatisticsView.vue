<script lang="ts" setup>
// Placeholder for statistics functionality
</script>

<template>
  <section class="statistics-view">
    <h2 class="section-heading">
      Statistics
    </h2>

    <div class="stats-grid">
      <!-- Subscription Status Placeholder -->
      <div class="stat-card">
        <h3 class="stat-card__title">
          Subscription Status
        </h3>
        <p class="stat-card__text">
          Member subscription statistics will be displayed here.
        </p>
      </div>

      <!-- Clear Percentages Placeholder -->
      <div class="stat-card">
        <h3 class="stat-card__title">
          Clear Percentages by Content Type
        </h3>
        <p class="stat-card__text">
          Content clear rates (Extreme, Savage, Ultimate, etc.) will be displayed here.
        </p>
      </div>

      <!-- Event Participation Placeholder -->
      <div class="stat-card">
        <h3 class="stat-card__title">
          Event Participation
        </h3>
        <p class="stat-card__text">
          Event attendance and participation metrics will be displayed here.
        </p>
      </div>

      <!-- Additional Stats Placeholder -->
      <div class="stat-card">
        <h3 class="stat-card__title">
          Additional Metrics
        </h3>
        <p class="stat-card__text">
          Other relevant statistics and insights will be displayed here.
        </p>
      </div>
    </div>

    <div class="info-callout">
      <p class="info-callout__text">
        <strong>Coming Soon:</strong> This section will provide comprehensive analytics including subscription tracking,
        content progression, event participation rates, and member activity metrics.
      </p>
    </div>
  </section>
</template>

<style scoped>
.statistics-view {
  width: 100%;
}

.section-heading { font-size: 1.875rem; font-weight: 700; color: var(--fg); margin-bottom: 1.5rem; }
.stat-card__title { font-size: 1.25rem; font-weight: 600; color: var(--fg); margin-bottom: 0.75rem; }
.stat-card__text { color: var(--muted); }

.info-callout {
  background: rgba(59,130,246,0.08); border: 1px solid rgba(59,130,246,0.25);
  border-radius: 8px; padding: 1rem; margin-top: 1.5rem;
}
.info-callout__text { color: #1e40af; font-size: 0.875rem; }

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 1.5rem;
}

.stat-card {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 1.5rem;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.stat-card:hover {
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.3);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15),
  0 4px 16px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.6);
}
</style>

<style>
:root[data-theme='dark'] .stat-card {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .stat-card {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

:root[data-theme='dark'] .stat-card:hover {
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
  0 4px 16px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .stat-card:hover {
    border-color: rgba(59, 130, 246, 0.4);
    box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

:root[data-theme='dark'] .info-callout { background: rgba(59,130,246,0.12); border-color: rgba(59,130,246,0.3); }
:root[data-theme='dark'] .info-callout__text { color: #93c5fd; }

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .info-callout { background: rgba(59,130,246,0.12); border-color: rgba(59,130,246,0.3); }
  :root:not([data-theme='light']) .info-callout__text { color: #93c5fd; }
}
</style>
