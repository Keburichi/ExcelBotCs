<script lang="ts" setup>
// Placeholder for future settings functionality
</script>

<template>
  <section class="settings-view">
    <h2 class="section-heading">
      Settings
    </h2>
    <div class="placeholder-box">
      <p class="placeholder-box__text">
        Settings configuration will be available here soon.
      </p>
      <p class="placeholder-box__subtext">
        This section is under development.
      </p>
    </div>
  </section>
</template>

<style scoped>
.settings-view {
  width: 100%;
}
.section-heading { font-size: 1.875rem; font-weight: 700; color: var(--fg); margin-bottom: 1rem; }
.placeholder-box {
  background: var(--muted-bg); border-radius: 8px; padding: 2rem; text-align: center;
}
.placeholder-box__text { color: var(--muted); font-size: 1.125rem; }
.placeholder-box__subtext { color: var(--muted); font-size: 0.875rem; margin-top: 0.5rem; }
</style>
