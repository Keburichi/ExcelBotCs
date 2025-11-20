<script lang="ts" setup>
// Placeholder for statistics functionality
</script>

<template>
  <section class="statistics-view">
    <h2 class="text-3xl font-bold text-gray-900 dark:text-white mb-6">
      Statistics
    </h2>

    <div class="stats-grid">
      <!-- Subscription Status Placeholder -->
      <div class="stat-card">
        <h3 class="text-xl font-semibold text-gray-900 dark:text-white mb-3">
          Subscription Status
        </h3>
        <p class="text-gray-600 dark:text-gray-400">
          Member subscription statistics will be displayed here.
        </p>
      </div>

      <!-- Clear Percentages Placeholder -->
      <div class="stat-card">
        <h3 class="text-xl font-semibold text-gray-900 dark:text-white mb-3">
          Clear Percentages by Content Type
        </h3>
        <p class="text-gray-600 dark:text-gray-400">
          Content clear rates (Extreme, Savage, Ultimate, etc.) will be displayed here.
        </p>
      </div>

      <!-- Event Participation Placeholder -->
      <div class="stat-card">
        <h3 class="text-xl font-semibold text-gray-900 dark:text-white mb-3">
          Event Participation
        </h3>
        <p class="text-gray-600 dark:text-gray-400">
          Event attendance and participation metrics will be displayed here.
        </p>
      </div>

      <!-- Additional Stats Placeholder -->
      <div class="stat-card">
        <h3 class="text-xl font-semibold text-gray-900 dark:text-white mb-3">
          Additional Metrics
        </h3>
        <p class="text-gray-600 dark:text-gray-400">
          Other relevant statistics and insights will be displayed here.
        </p>
      </div>
    </div>

    <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 mt-6">
      <p class="text-blue-800 dark:text-blue-200 text-sm">
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

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 1.5rem;
}

.stat-card {
  background: var(--card, #fff);
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 0.75rem;
  padding: 1.5rem;
  transition: box-shadow 0.2s;
}

.stat-card:hover {
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}
</style>
