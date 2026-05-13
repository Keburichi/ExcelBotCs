<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import { useAuth } from '@/composables/useAuth'

const route = useRoute()
const router = useRouter()
const { authorized, ensureAuth, login } = useAuth()

const checking = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    await ensureAuth()
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to check auth status'
  }
  finally {
    checking.value = false
  }
})

function targetAfterLogin(): string {
  // use ?redirect= if present, else fallback to home
  const q = route.query?.redirect
  if (typeof q === 'string' && q)
    return q
  return '/home'
}

function doLogin() {
  const redirect = targetAfterLogin()
  // Delegates to server-side OAuth; server should redirect back to the app afterwards
  login(redirect)
}

async function proceed() {
  // If already authorized, go where the user intended to go
  const dest = targetAfterLogin()
  await router.replace(dest)
}
</script>

<template>
  <div class="login-container">
    <div class="login-background">
      <div class="gradient-orb orb-1" />
      <div class="gradient-orb orb-2" />
      <div class="gradient-orb orb-3" />
    </div>

    <div class="login-wrapper">
      <div class="site-branding">
        <h1 class="site-name">
          Excelsior FC
        </h1>
      </div>

      <div class="login-card">
        <div class="login-header">
          <h2 class="login-title">
            Welcome
          </h2>
          <p class="login-subtitle">
            Sign in to continue
          </p>
        </div>

        <div class="login-content">
          <p v-if="checking" class="status-message">
            <span class="spinner" />
            Checking your session…
          </p>

          <template v-else>
            <p v-if="error" class="error-message">
              {{ error }}
            </p>

            <div v-if="authorized" class="auth-state">
              <div class="success-icon">
                ✓
              </div>
              <p class="auth-message">
                You are already signed in
              </p>
              <BaseButton title="Continue" @clicked="proceed" />
            </div>

            <div v-else class="auth-state">
              <p class="auth-message">
                Sign in with your Discord account to access all features
              </p>
              <button class="discord-button" @click="doLogin">
                <svg class="discord-icon" fill="currentColor" viewBox="0 0 24 24">
                  <path
                    d="M20.317 4.37a19.791 19.791 0 0 0-4.885-1.515a.074.074 0 0 0-.079.037c-.21.375-.444.864-.608 1.25a18.27 18.27 0 0 0-5.487 0a12.64 12.64 0 0 0-.617-1.25a.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.37a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057a19.9 19.9 0 0 0 5.993 3.03a.078.078 0 0 0 .084-.028a14.09 14.09 0 0 0 1.226-1.994a.076.076 0 0 0-.041-.106a13.107 13.107 0 0 1-1.872-.892a.077.077 0 0 1-.008-.128a10.2 10.2 0 0 0 .372-.292a.074.074 0 0 1 .077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.01c.12.098.246.198.373.292a.077.077 0 0 1-.006.127a12.299 12.299 0 0 1-1.873.892a.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028a19.839 19.839 0 0 0 6.002-3.03a.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.33c-1.183 0-2.157-1.085-2.157-2.419c0-1.333.956-2.419 2.157-2.419c1.21 0 2.176 1.096 2.157 2.42c0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419c0-1.333.955-2.419 2.157-2.419c1.21 0 2.176 1.096 2.157 2.42c0 1.333-.946 2.418-2.157 2.418z"
                  />
                </svg>
                Login with Discord
              </button>
            </div>
          </template>
        </div>
      </div>

      <footer class="login-footer">
        <span class="copyright-text">© {{ new Date().getFullYear() }} Excelsior FC. All rights reserved.</span>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  overflow: hidden;
}

.login-wrapper {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 440px;
  padding: 2rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* Site branding */
.site-branding {
  text-align: center;
}

.site-name {
  font-size: 2.75rem;
  font-weight: 800;
  margin: 0;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.02em;
}

/* Footer */
.login-footer {
  text-align: center;
}

.login-footer .copyright-text {
  font-size: 0.875rem;
  font-weight: 500;
  letter-spacing: 0.5px;
  opacity: 0.75;
  color: rgba(0, 0, 0, 0.65);
  line-height: 1.6;
}

:root[data-theme='dark'] .login-footer .copyright-text {
  color: rgba(255, 255, 255, 0.75);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .login-footer .copyright-text {
    color: rgba(255, 255, 255, 0.75);
  }
}

/* Animated gradient background */
.login-background {
  position: absolute;
  inset: 0;
  z-index: 0;
  background: linear-gradient(135deg,
  rgba(59, 130, 246, 0.1) 0%,
  rgba(147, 51, 234, 0.1) 50%,
  rgba(236, 72, 153, 0.1) 100%
  );
}

:root[data-theme='dark'] .login-background {
  background: linear-gradient(135deg,
  rgba(59, 130, 246, 0.15) 0%,
  rgba(147, 51, 234, 0.15) 50%,
  rgba(236, 72, 153, 0.15) 100%
  );
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .login-background {
    background: linear-gradient(135deg,
    rgba(59, 130, 246, 0.15) 0%,
    rgba(147, 51, 234, 0.15) 50%,
    rgba(236, 72, 153, 0.15) 100%
    );
  }
}

/* Floating gradient orbs */
.gradient-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.6;
  animation: float 20s ease-in-out infinite;
}

.orb-1 {
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.4) 0%, transparent 70%);
  top: -10%;
  left: -5%;
  animation-delay: 0s;
}

.orb-2 {
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(147, 51, 234, 0.3) 0%, transparent 70%);
  bottom: -15%;
  right: -10%;
  animation-delay: -7s;
}

.orb-3 {
  width: 350px;
  height: 350px;
  background: radial-gradient(circle, rgba(236, 72, 153, 0.3) 0%, transparent 70%);
  top: 40%;
  right: 10%;
  animation-delay: -14s;
}

@keyframes float {
  0%, 100% {
    transform: translate(0, 0) scale(1);
  }
  33% {
    transform: translate(30px, -50px) scale(1.1);
  }
  66% {
    transform: translate(-20px, 20px) scale(0.9);
  }
}

/* Glass morphism card */
.login-card {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 440px;
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border-radius: 16px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  padding: 3rem 2.5rem;
  animation: cardAppear 0.6s ease-out;
}

:root[data-theme='dark'] .login-card {
  background: rgba(18, 26, 45, 0.7);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.1);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .login-card {
    background: rgba(18, 26, 45, 0.7);
    border: 1px solid rgba(255, 255, 255, 0.1);
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.1);
  }
}

@keyframes cardAppear {
  from {
    opacity: 0;
    transform: translateY(20px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

/* Header */
.login-header {
  text-align: center;
  margin-bottom: 2.5rem;
}

.login-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: var(--fg);
}

.login-subtitle {
  font-size: 1rem;
  color: var(--muted);
  margin: 0;
}

/* Content */
.login-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.auth-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
}

.auth-message {
  text-align: center;
  color: var(--fg);
  margin: 0;
  font-size: 0.95rem;
  line-height: 1.6;
}

.status-message {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  color: var(--muted);
  font-size: 0.95rem;
  margin: 0;
}

/* Spinner */
.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid rgba(59, 130, 246, 0.2);
  border-top-color: var(--link);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* Success icon */
.success-icon {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--exp-extreme) 0%, var(--exp-extreme-border) 100%);
  color: var(--bg);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2rem;
  font-weight: bold;
  box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
}

/* Error message */
.error-message {
  padding: 1rem;
  background: var(--alert-error-bg);
  color: var(--alert-error-fg);
  border: 1px solid var(--alert-error-border);
  border-radius: 12px;
  text-align: center;
  margin: 0;
  font-size: 0.9rem;
}

/* Discord button */
.discord-button {
  width: 100%;
  padding: 1rem 2rem;
  background: linear-gradient(135deg, #5865f2 0%, #4752c4 100%);
  color: var(--bg);
  border: none;
  border-radius: 12px;
  font-size: 1.05rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(88, 101, 242, 0.3);
  position: relative;
  overflow: hidden;
}

.discord-button::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(255, 255, 255, 0) 0%, rgba(255, 255, 255, 0.1) 100%);
  opacity: 0;
  transition: opacity 0.2s ease;
}

.discord-button:hover {
  box-shadow: 0 6px 20px rgba(88, 101, 242, 0.4);
}

.discord-button:hover::before {
  opacity: 1;
}

.discord-icon {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
}

/* Responsive design */
@media (max-width: 500px) {
  .login-wrapper {
    padding: 1.5rem 1rem;
    gap: 1.5rem;
  }

  .site-name {
    font-size: 2rem;
  }

  .login-card {
    padding: 2rem 1.5rem;
  }

  .login-title {
    font-size: 1.75rem;
  }

  .gradient-orb {
    filter: blur(60px);
  }
}

/* Respect reduced motion */
@media (prefers-reduced-motion: reduce) {
  .gradient-orb,
  .login-card,
  .discord-button,
  .spinner {
    animation: none !important;
  }

  .discord-button {
    transition: none !important;
  }
}
</style>
