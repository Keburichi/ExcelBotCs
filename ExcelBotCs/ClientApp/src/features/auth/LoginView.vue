<script setup lang="ts">
import {onMounted, ref} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import {useAuth} from "@/composables/useAuth";

const route = useRoute()
const router = useRouter()
const {authorized, ensureAuth, login} = useAuth()

const checking = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    await ensureAuth()
  } catch (e: any) {
    error.value = e?.message || 'Failed to check auth status'
  } finally {
    checking.value = false
  }
})

function targetAfterLogin(): string {
  // use ?redirect= if present, else current path, else fallback
  const q = route.query?.redirect
  if (typeof q === 'string' && q) return q
  return route.fullPath || '/home'
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
  <section class="login-view">
    <h2>Login</h2>

    <p v-if="checking">Checking your session…</p>

    <template v-else>
      <p v-if="error" class="error">{{ error }}</p>

      <div v-if="authorized">
        <p>You are already signed in.</p>
        <button class="btn" @click="proceed">Continue</button>
      </div>

      <div v-else>
        <p>Please sign in to continue.</p>
        <button class="btn" @click="doLogin">Login with Discord</button>
      </div>
    </template>
  </section>
</template>

<style scoped>
.login-view {
  max-width: 480px;
  margin: 2rem auto;
}

.error {
  color: #b00020;
}

.btn {
  padding: 0.5rem 1rem;
}
</style>