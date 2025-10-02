<script setup lang="ts">

import {onMounted} from "vue";
import {useLottery} from "@/composables/useLottery";

const lottery = useLottery();

onMounted(lottery.load);
</script>

<template>
  <section class="home">
    <div>
      <p v-if="lottery.error" class="error">{{lottery.error}}</p>
      <p>{{ lottery.view }}</p>
      
      <input type="number" min="1" max="100" v-model.number="lottery.input" placeholder="Enter your number">
      <button @click="lottery.guess(lottery.input)" class="btn">Guess your number</button>
      <p v-if="lottery.response">{{lottery.response}}</p>
    </div>
  </section>
</template>

<style scoped>

</style>