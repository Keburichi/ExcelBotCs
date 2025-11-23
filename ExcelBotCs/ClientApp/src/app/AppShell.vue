<script setup lang="ts">
// Global app shell
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import UserMenu from '@/components/auth/UserMenu.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { useAuth } from '@/composables/useAuth'

const route = useRoute()
const { isMember, isAdmin } = useAuth()

// Hide header/footer on login page for full immersive experience
const isLoginPage = computed(() => route.path === '/login')
</script>

<template>
  <div :class="{ 'login-layout': isLoginPage }" class="app-shell">
    <!-- Animated gradient background for all pages -->
    <div v-if="!isLoginPage" class="app-background">
      <div class="gradient-orb orb-1" />
      <div class="gradient-orb orb-2" />
      <div class="gradient-orb orb-3" />
    </div>

    <div v-if="!isLoginPage" class="container">
      <header class="app-header">
        <h1 class="brand">
          Excelsior FC
        </h1>
        <template v-if="isMember">
          <nav class="nav">
            <RouterLink to="/home">
              Home
            </RouterLink>
            <RouterLink to="/events">
              Events
            </RouterLink>
            <RouterLink to="/members">
              Members
            </RouterLink>
            <RouterLink to="/fights">
              Fights
            </RouterLink>
            <RouterLink to="/lottery">
              Lottery
            </RouterLink>
            <RouterLink v-if="isAdmin" to="/admin">
              Admin
            </RouterLink>
          </nav>
        </template>
        <template v-else>
          <nav class="nav">
            <RouterLink to="/home">
              Home
            </RouterLink>
            <RouterLink to="/members">
              Members
            </RouterLink>
          </nav>
        </template>
        <div class="header-actions">
          <ThemeToggle />
          <UserMenu />
        </div>
      </header>

      <main class="app-content">
        <RouterView />
      </main>

      <footer class="app-footer">
        <small>© {{ new Date().getFullYear() }} Excelsior FC</small>
      </footer>
    </div>

    <!-- Login page gets full viewport without container wrapper -->
    <RouterView v-else />
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  position: relative;
}

.app-shell.login-layout {
  /* Remove any padding/margin constraints for login page */
  padding: 0;
  margin: 0;
}

/* Animated gradient background - Light theme */
.app-background {
  position: fixed;
  inset: 0;
  z-index: 0;
  background: linear-gradient(135deg,
  rgba(59, 130, 246, 0.15) 0%,
  rgba(147, 51, 234, 0.15) 50%,
  rgba(236, 72, 153, 0.15) 100%
  );
  pointer-events: none;
}

/* Dark theme background */
:root[data-theme='dark'] .app-background {
  background: linear-gradient(135deg,
  rgba(59, 130, 246, 0.18) 0%,
  rgba(147, 51, 234, 0.18) 50%,
  rgba(236, 72, 153, 0.18) 100%
  );
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .app-background {
    background: linear-gradient(135deg,
    rgba(59, 130, 246, 0.18) 0%,
    rgba(147, 51, 234, 0.18) 50%,
    rgba(236, 72, 153, 0.18) 100%
    );
  }
}

/* Floating gradient orbs */
.gradient-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.7;
  animation: float 20s ease-in-out infinite;
  will-change: transform;
  pointer-events: none;
}

.orb-1 {
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.5) 0%, rgba(59, 130, 246, 0.1) 50%, transparent 70%);
  top: 10%;
  left: 5%;
  animation-name: float1;
}

.orb-2 {
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(147, 51, 234, 0.4) 0%, rgba(147, 51, 234, 0.1) 50%, transparent 70%);
  bottom: 10%;
  right: 5%;
  animation-name: float2;
  animation-delay: -7s;
}

.orb-3 {
  width: 350px;
  height: 350px;
  background: radial-gradient(circle, rgba(236, 72, 153, 0.4) 0%, rgba(236, 72, 153, 0.1) 50%, transparent 70%);
  top: 50%;
  right: 30%;
  animation-name: float3;
  animation-delay: -14s;
}

@keyframes float1 {
  0%, 100% {
    transform: translate(0, 0) scale(1);
  }
  33% {
    transform: translate(50px, -40px) scale(1.1);
  }
  66% {
    transform: translate(-30px, 40px) scale(0.95);
  }
}

@keyframes float2 {
  0%, 100% {
    transform: translate(0, 0) scale(1);
  }
  33% {
    transform: translate(-60px, 30px) scale(1.05);
  }
  66% {
    transform: translate(40px, -50px) scale(0.9);
  }
}

@keyframes float3 {
  0%, 100% {
    transform: translate(0, 0) scale(1);
  }
  33% {
    transform: translate(30px, 60px) scale(1.15);
  }
  66% {
    transform: translate(-50px, -30px) scale(0.92);
  }
}

.container {
  position: relative;
  z-index: 1;
  max-width: 1100px;
  margin: 0 auto;
  padding: 1rem;
}

/* Glass morphism header */
.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 1.5rem;
  margin-bottom: 1.5rem;
  background: rgba(255, 255, 255, 0.6);
  backdrop-filter: blur(20px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  gap: 1.5rem;
  flex-wrap: wrap;
}

:root[data-theme='dark'] .app-header {
  background: rgba(18, 26, 45, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .app-header {
    background: rgba(18, 26, 45, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

/* Brand with gradient text */
.brand {
  font-size: 1.5rem;
  font-weight: 800;
  margin: 0;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.01em;
  white-space: nowrap;
}

/* Modern navigation */
.nav {
  display: flex;
  gap: 0.375rem;
  flex: 1;
  justify-content: center;
}

.nav :deep(a) {
  padding: 0.5rem 1rem;
  border-radius: 10px;
  color: var(--fg);
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
  position: relative;
  background: transparent;
}

.nav :deep(a:hover) {
  background: rgba(59, 130, 246, 0.1);
}

:root[data-theme='dark'] .nav :deep(a:hover) {
  background: rgba(59, 130, 246, 0.15);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .nav :deep(a:hover) {
    background: rgba(59, 130, 246, 0.15);
  }
}

.nav :deep(a.router-link-active) {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.15) 0%, rgba(147, 51, 234, 0.15) 100%);
  color: var(--fg);
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.2);
}

:root[data-theme='dark'] .nav :deep(a.router-link-active) {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.25) 0%, rgba(147, 51, 234, 0.25) 100%);
  box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .nav :deep(a.router-link-active) {
    background: linear-gradient(135deg, rgba(59, 130, 246, 0.25) 0%, rgba(147, 51, 234, 0.25) 100%);
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
  }
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.app-content {
  position: relative;
  z-index: 1;
}

/* Glass morphism footer */
.app-footer {
  position: relative;
  z-index: 1;
  margin-top: 3rem;
  padding: 1.25rem;
  text-align: center;
  background: rgba(255, 255, 255, 0.4);
  backdrop-filter: blur(20px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  color: var(--muted);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.06);
}

:root[data-theme='dark'] .app-footer {
  background: rgba(18, 26, 45, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .app-footer {
    background: rgba(18, 26, 45, 0.4);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
  }
}

/* Responsive design */
@media (max-width: 768px) {
  .app-header {
    flex-direction: column;
    gap: 1rem;
    padding: 1rem;
  }

  .nav {
    width: 100%;
    justify-content: flex-start;
    flex-wrap: wrap;
  }

  .brand {
    font-size: 1.25rem;
  }

  .gradient-orb {
    filter: blur(60px);
    opacity: 0.5;
  }

  .orb-1, .orb-2, .orb-3 {
    width: 300px;
    height: 300px;
  }
}

/* Respect reduced motion */
@media (prefers-reduced-motion: reduce) {
  .gradient-orb {
    animation: none !important;
  }
}
</style>
