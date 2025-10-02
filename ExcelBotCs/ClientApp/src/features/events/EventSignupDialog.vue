<script setup lang="ts">
import BaseModal from "@/components/BaseModal.vue";
import {EventSignup, FCEvent, ROLE, type Role} from "@/features/events/events.types";
import {computed, onMounted, ref} from "vue";
import DiscordMessageRenderer from "@/components/DiscordMessageRenderer.vue";
import {useAuth} from "@/composables/useAuth";
import {useMembers} from "@/composables/useMembers";
import {useEvents} from "@/composables/useEvents";

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>();

const {user} = useAuth();
const members = useMembers();
const fcEvent = ref<FCEvent | null>(props.event)

// Load members data when component mounts
onMounted(() => {
  if (members.members.value.length === 0) {
    members.load();
  }
});

// Check if current user is signed up for a specific role
function isSignedUpForRole(role: Role): boolean {
  if (!user.value?.DiscordId) 
    return false;

  return fcEvent.value?.Signups?.some(signup =>
      signup.DiscordUserId === user.value?.DiscordId && signup.Roles.some(signupRole => signupRole === role)
  ) ?? false;
}

// Get signup count for a specific role
function getSignupCountForRole(role: Role): number {
  if (!fcEvent.value?.Signups) 
    return 0;
  
  return fcEvent.value.Signups.filter(signup => 
    signup.Roles.some(signupRole => signupRole === role)
  ).length;
}

// Get list of user display names signed up for a specific role
function getSignedUpUsersForRole(role: Role): string[] {
  if (!fcEvent.value?.Signups) 
    return [];
  
  return fcEvent.value.Signups
    .filter(signup => signup.Roles.some(signupRole => signupRole === role))
    .map(signup => {
      const member = members.members.value.find(m => m.DiscordId === signup.DiscordUserId);
      
      if (!member) return signup.DiscordUserId; // Fallback to ID if member not found
      
      // Return PlayerName if present, otherwise DiscordName
      return member.PlayerName || member.DiscordName;
    });
}

// Computed properties for each role
const isSignedUpTank = computed(() => isSignedUpForRole(ROLE.Tank));
const isSignedUpHealer = computed(() => isSignedUpForRole(ROLE.Healer));
const isSignedUpMelee = computed(() => isSignedUpForRole(ROLE.Melee));
const isSignedUpCaster = computed(() => isSignedUpForRole(ROLE.Caster));
const isSignedUpRanged = computed(() => isSignedUpForRole(ROLE.Ranged));

// Signup counts for each role
const tankCount = computed(() => getSignupCountForRole(ROLE.Tank));
const healerCount = computed(() => getSignupCountForRole(ROLE.Healer));
const meleeCount = computed(() => getSignupCountForRole(ROLE.Melee));
const casterCount = computed(() => getSignupCountForRole(ROLE.Caster));
const rangedCount = computed(() => getSignupCountForRole(ROLE.Ranged));

// Signed up users for tooltips
const tankUsers = computed(() => getSignedUpUsersForRole(ROLE.Tank));
const healerUsers = computed(() => getSignedUpUsersForRole(ROLE.Healer));
const meleeUsers = computed(() => getSignedUpUsersForRole(ROLE.Melee));
const casterUsers = computed(() => getSignedUpUsersForRole(ROLE.Caster));
const rangedUsers = computed(() => getSignedUpUsersForRole(ROLE.Ranged));

async function signUp(signupEvent: FCEvent, role: Role) {
  await useEvents().signup(signupEvent, role)
      .then(async () => {
        // emit('update:modelValue', false)
        // Reload event after signup and update button states
         fcEvent.value = await useEvents().getEvent(signupEvent.Id)
      })
      .catch(error => {
            console.error('Error signing up:', error)
            alert('Error signing up. Please try again.')
          }
      )
}

</script>

<template>
  <BaseModal :modelValue="props.modelValue" @update:modelValue="emit('update:modelValue', $event)"
             :title="event.Name + ' - signup'" :description="event.Description">
    <template #body>
      <DiscordMessageRenderer :content="event.Description"/>
    </template>
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>

    <template #actions>
      <button 
        :class="['btn', { 'success': isSignedUpTank }]" 
        @click="signUp(event, ROLE.Tank)"
        :data-tooltip="tankUsers.length > 0 ? tankUsers.join(', ') : 'No signups yet'"
      >
        Tank ({{ tankCount }})
      </button>
      <button 
        :class="['btn', { 'success': isSignedUpHealer }]" 
        @click="signUp(event, ROLE.Healer)"
        :data-tooltip="healerUsers.length > 0 ? healerUsers.join(', ') : 'No signups yet'"
      >
        Healer ({{ healerCount }})
      </button>
      <button 
        :class="['btn', { 'success': isSignedUpMelee }]" 
        @click="signUp(event, ROLE.Melee)"
        :data-tooltip="meleeUsers.length > 0 ? meleeUsers.join(', ') : 'No signups yet'"
      >
        Melee ({{ meleeCount }})
      </button>
      <button 
        :class="['btn', { 'success': isSignedUpCaster }]" 
        @click="signUp(event, ROLE.Caster)"
        :data-tooltip="casterUsers.length > 0 ? casterUsers.join(', ') : 'No signups yet'"
      >
        Caster ({{ casterCount }})
      </button>
      <button 
        :class="['btn', { 'success': isSignedUpRanged }]" 
        @click="signUp(event, ROLE.Ranged)"
        :data-tooltip="rangedUsers.length > 0 ? rangedUsers.join(', ') : 'No signups yet'"
      >
        Ranged ({{ rangedCount }})
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>

</style>