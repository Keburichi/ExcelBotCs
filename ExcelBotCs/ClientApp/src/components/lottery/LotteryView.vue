<script setup lang="ts">
import { onMounted } from 'vue'
import { useLottery } from '@/composables/useLottery'

const lottery = useLottery()

onMounted(lottery.load)
</script>

<template>
  <section class="home">
    <div>
      <p v-if="lottery.error" class="error">
        {{ lottery.error }}
      </p>
      <p>{{ lottery.view }}</p>

      <input v-model.number="lottery.input" type="number" min="1" max="100" placeholder="Enter your number">
      <button class="btn" @click="lottery.guess(lottery.input)">
        Guess your number
      </button>
      <p v-if="lottery.response">
        {{ lottery.response }}
      </p>
    </div>
  </section>
</template>

<style scoped>

</style>
