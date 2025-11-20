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
  // use ?redirect= if present, else current path, else fallback
  const q = route.query?.redirect
  if (typeof q === 'string' && q)
    return q
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

    <p v-if="checking">
      Checking your session…
    </p>

    <template v-else>
      <p v-if="error" class="error">
        {{ error }}
      </p>

      <div v-if="authorized">
        <p>You are already signed in.</p>
        <BaseButton title="Continue" @clicked="proceed" />
      </div>

      <div v-else>
        <p>Please sign in to continue.</p>
        <BaseButton title="Login with Discord" @clicked="doLogin" />
      </div>
    </template>
  </section>
</template>

<style scoped>
@utility login-view {
  max-width: 30rem; /* max-w-md */
  margin-left: auto;
  margin-right: auto;
  margin-top: 2rem;
  margin-bottom: 2rem;
}

.error {
  color: rgb(var(--color-danger));
}
</style>
