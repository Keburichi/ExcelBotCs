<script setup lang="ts">
import type { MemberNote } from '@/features/members/members.types'
import BaseButton from '@/components/BaseButton.vue'
import BaseModal from '@/components/BaseModal.vue'
import { useAuth } from '@/composables/useAuth'

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>()
const modelValue = defineModel<boolean>('isOpen', { required: true })
const memberNote = defineModel<MemberNote>('memberNote', { required: true })

const { user } = useAuth()
</script>

<template>
  <BaseModal
    v-model="modelValue" title="Edit Note" size="medium"
  >
    <template #body>
      <input type="text" :value="memberNote.Note">
    </template>
    <template #footer>
      <p>Written by: {{ memberNote.Author.DiscordName }}</p>
    </template>
    <template #actions>
      <BaseButton title="Save" size="small" state="primary" />
      <BaseButton title="Cancel" size="small" state="secondary" />
      <BaseButton title="Delete" size="small" state="danger" />
    </template>
  </BaseModal>
</template>

<style scoped />
