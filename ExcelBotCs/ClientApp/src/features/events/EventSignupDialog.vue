<script setup lang="ts">
import BaseModal from "@/components/BaseModal.vue";
import {EventSignup, FCEvent, Role} from "@/features/events/events.types";
import {useAuth} from "@/features/auth/useAuth";
import {computed, onMounted} from "vue";
import {useMembers} from "@/features/members/useMembers";

const props = defineProps<{
  modelValue: boolean
  event: FCEvent
}>();

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
}>();

const {user} = useAuth();
const members = useMembers();

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

  return props.event.Signups?.some(signup =>
      signup.DiscordUserId === user.value?.DiscordId && signup.Roles.some(signupRole => signupRole === role)
  ) ?? false;
}

// Get signup count for a specific role
function getSignupCountForRole(role: Role): number {
  if (!props.event.Signups) 
    return 0;
  
  return props.event.Signups.filter(signup => 
    signup.Roles.some(signupRole => signupRole === role)
  ).length;
}

// Get list of user display names signed up for a specific role
function getSignedUpUsersForRole(role: Role): string[] {
  if (!props.event.Signups) 
    return [];
  
  return props.event.Signups
    .filter(signup => signup.Roles.some(signupRole => signupRole === role))
    .map(signup => {
      const member = members.members.value.find(m => m.DiscordId === signup.DiscordUserId);
      
      if (!member) return signup.DiscordUserId; // Fallback to ID if member not found
      
      // Return PlayerName if present, otherwise DiscordName
      return member.PlayerName || member.DiscordName;
    });
}

// Computed properties for each role
const isSignedUpTank = computed(() => isSignedUpForRole(Role.Tank));
const isSignedUpHealer = computed(() => isSignedUpForRole(Role.Healer));
const isSignedUpMelee = computed(() => isSignedUpForRole(Role.Melee));
const isSignedUpCaster = computed(() => isSignedUpForRole(Role.Caster));
const isSignedUpRanged = computed(() => isSignedUpForRole(Role.Ranged));

// Signup counts for each role
const tankCount = computed(() => getSignupCountForRole(Role.Tank));
const healerCount = computed(() => getSignupCountForRole(Role.Healer));
const meleeCount = computed(() => getSignupCountForRole(Role.Melee));
const casterCount = computed(() => getSignupCountForRole(Role.Caster));
const rangedCount = computed(() => getSignupCountForRole(Role.Ranged));

// Signed up users for tooltips
const tankUsers = computed(() => getSignedUpUsersForRole(Role.Tank));
const healerUsers = computed(() => getSignedUpUsersForRole(Role.Healer));
const meleeUsers = computed(() => getSignedUpUsersForRole(Role.Melee));
const casterUsers = computed(() => getSignedUpUsersForRole(Role.Caster));
const rangedUsers = computed(() => getSignedUpUsersForRole(Role.Ranged));

function signUp(fcEvent: FCEvent, role: Role) {
  alert('Signed up for: ' + fcEvent.Name + ' - ' + role)
}

</script>

<template>
  <BaseModal :modelValue="props.modelValue" @update:modelValue="emit('update:modelValue', $event)"
             :title="event.Name + ' - signup'" :description="event.Description">
    <template #image>
      <img v-if="event.PictureUrl" :src="event.PictureUrl" alt="avatar" class="card__image">
    </template>

    <template #actions>
      <button 
        :class="['btn', { 'danger': isSignedUpTank }]" 
        @click="signUp(event, Role.Tank)"
        :data-tooltip="tankUsers.length > 0 ? tankUsers.join(', ') : 'No signups yet'"
      >
        Tank ({{ tankCount }})
      </button>
      <button 
        :class="['btn', { 'danger': isSignedUpHealer }]" 
        @click="signUp(event, Role.Healer)"
        :data-tooltip="healerUsers.length > 0 ? healerUsers.join(', ') : 'No signups yet'"
      >
        Healer ({{ healerCount }})
      </button>
      <button 
        :class="['btn', { 'danger': isSignedUpMelee }]" 
        @click="signUp(event, Role.Melee)"
        :data-tooltip="meleeUsers.length > 0 ? meleeUsers.join(', ') : 'No signups yet'"
      >
        Melee ({{ meleeCount }})
      </button>
      <button 
        :class="['btn', { 'danger': isSignedUpCaster }]" 
        @click="signUp(event, Role.Caster)"
        :data-tooltip="casterUsers.length > 0 ? casterUsers.join(', ') : 'No signups yet'"
      >
        Caster ({{ casterCount }})
      </button>
      <button 
        :class="['btn', { 'danger': isSignedUpRanged }]" 
        @click="signUp(event, Role.Ranged)"
        :data-tooltip="rangedUsers.length > 0 ? rangedUsers.join(', ') : 'No signups yet'"
      >
        Ranged ({{ rangedCount }})
      </button>
    </template>
  </BaseModal>
</template>

<style scoped>

</style>