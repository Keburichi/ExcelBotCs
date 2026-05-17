<script lang="ts" setup>
import type { EventTemplate } from '@/features/event-templates/event-templates.types'
import type { GuildEmoji, SignupButtonConfig } from '@/features/events/events.types'
import type { Member } from '@/features/members/members.types'
import { computed, nextTick, onMounted, onUnmounted, reactive, ref } from 'vue'
import BaseButton from '@/components/BaseButton.vue'
import SearchableDropdown from '@/components/SearchableDropdown.vue'
import { useAuth } from '@/composables/useAuth'
import { EventTemplatesApi } from '@/features/event-templates/event-templates.api'
import { DayOfWeek, dayOfWeekToString, formatTimeOfDay } from '@/features/event-templates/event-templates.types'
import { EventsApi } from '@/features/events/events.api'
import { EventType, ROLE } from '@/features/events/events.types'
import { MembersApi } from '@/features/members/members.api'

const { user } = useAuth()

const templates = ref<EventTemplate[]>([])
const loading = ref(false)
const error = ref('')

const showForm = ref(false)
const editingId = ref<string | null>(null)
const saving = ref(false)

const adminMembers = ref<Member[]>([])
const selectedOrganizer = ref<Member | null>(null)
const guildEmojis = ref<GuildEmoji[]>([])

function formatMemberName(member: Member): string {
  return member.PlayerName || member.DiscordName
}

function blankForm(): Omit<EventTemplate, 'Id'> {
  return {
    Name: '',
    Description: '',
    Type: EventType.Other,
    DayOfWeek: DayOfWeek.Wednesday,
    TimeOfDayMinutes: 1200,
    Duration: 120,
    Organizer: '',
    MaxNumberOfParticipants: 8,
    SignupButtonConfigs: undefined,
  }
}

const form = reactive<Omit<EventTemplate, 'Id'>>(blankForm())

// Party presets (reused from CreateEventView)
type PartyPreset = 'light-party' | 'full-party' | 'alliance-raid' | 'any' | 'custom'
const partyPreset = ref<PartyPreset>('full-party')

const partyPresetOptions = [
  { key: 'light-party' as PartyPreset, label: 'Light (4)' },
  { key: 'full-party' as PartyPreset, label: 'Full (8)' },
  { key: 'alliance-raid' as PartyPreset, label: 'Alliance (24)' },
  { key: 'any' as PartyPreset, label: 'Any (99)' },
  { key: 'custom' as PartyPreset, label: 'Custom' },
]

function setPartyPreset(preset: PartyPreset) {
  partyPreset.value = preset
  switch (preset) {
    case 'light-party':
      form.MaxNumberOfParticipants = 4
      break
    case 'full-party':
      form.MaxNumberOfParticipants = 8
      break
    case 'alliance-raid':
      form.MaxNumberOfParticipants = 24
      break
    case 'any':
      form.MaxNumberOfParticipants = 99
      break
    case 'custom':
      if ([4, 8, 24, 99].includes(form.MaxNumberOfParticipants))
        form.MaxNumberOfParticipants = 0
      break
  }
}

function detectPreset(max: number): PartyPreset {
  switch (max) {
    case 4: return 'light-party'
    case 8: return 'full-party'
    case 24: return 'alliance-raid'
    case 99: return 'any'
    default: return 'custom'
  }
}

// Button configuration (reused pattern from CreateEventView)
type ButtonMode = 'standard' | 'roles-helper' | 'custom'
const buttonMode = ref<ButtonMode>('standard')
const signupButtonConfigs = ref<SignupButtonConfig[]>([])
const emojiSearchQuery = ref('')
const emojiDropdownOpenIndex = ref<number | null>(null)
const emojiSearchInputRef = ref<HTMLInputElement | null>(null)

const ROLE_EMOJI_IDS: Record<string, string> = {
  tank: '1380979172423499846',
  healer: '1380979170787721368',
  melee: '873621778214318091',
  ranged: '873621778453368895',
  caster: '873621778566635540',
}

const filteredEmojis = computed(() => {
  const query = emojiSearchQuery.value.trim().toLowerCase()
  if (!query)
    return guildEmojis.value
  return guildEmojis.value.filter(e => e.Name.toLowerCase().includes(query))
})

function getEmojiUrl(id?: string): string | null {
  if (!id)
    return null
  const emoji = guildEmojis.value.find(e => e.Id === id)
  if (emoji)
    return emoji.Url
  return `https://cdn.discordapp.com/emojis/${id}.webp?size=20`
}

function slugify(text: string): string {
  return text.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}

function addButton() {
  signupButtonConfigs.value.push({ Slug: '', Label: '', EmojiId: undefined, IsHelper: false, MappedRole: undefined })
}

function removeButton(index: number) {
  signupButtonConfigs.value.splice(index, 1)
}

function onLabelChange(index: number) {
  signupButtonConfigs.value[index].Slug = slugify(signupButtonConfigs.value[index].Label)
}

function openEmojiDropdown(index: number) {
  emojiSearchQuery.value = ''
  emojiDropdownOpenIndex.value = index
  nextTick(() => emojiSearchInputRef.value?.focus())
}

function selectEmoji(index: number, emoji: GuildEmoji) {
  signupButtonConfigs.value[index].EmojiId = emoji.Id
  emojiDropdownOpenIndex.value = null
}

function clearEmoji(index: number) {
  signupButtonConfigs.value[index].EmojiId = undefined
  emojiDropdownOpenIndex.value = null
}

function closeEmojiDropdown() {
  emojiDropdownOpenIndex.value = null
}

function handleGlobalKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && emojiDropdownOpenIndex.value !== null)
    closeEmojiDropdown()
}

function handleGlobalClick(e: MouseEvent) {
  if (emojiDropdownOpenIndex.value === null)
    return
  if (!(e.target as HTMLElement).closest('.emoji-picker-wrapper'))
    closeEmojiDropdown()
}

function setButtonMode(mode: ButtonMode) {
  buttonMode.value = mode
  if (mode === 'roles-helper') {
    signupButtonConfigs.value = [
      { Slug: 'tank', Label: 'Tank', EmojiId: ROLE_EMOJI_IDS.tank, IsHelper: false, MappedRole: ROLE.Tank },
      { Slug: 'healer', Label: 'Healer', EmojiId: ROLE_EMOJI_IDS.healer, IsHelper: false, MappedRole: ROLE.Healer },
      { Slug: 'melee', Label: 'Melee', EmojiId: ROLE_EMOJI_IDS.melee, IsHelper: false, MappedRole: ROLE.Melee },
      { Slug: 'caster', Label: 'Caster', EmojiId: ROLE_EMOJI_IDS.caster, IsHelper: false, MappedRole: ROLE.Caster },
      { Slug: 'ranged', Label: 'Ranged', EmojiId: ROLE_EMOJI_IDS.ranged, IsHelper: false, MappedRole: ROLE.Ranged },
      { Slug: 'helper', Label: 'Helper', EmojiId: undefined, IsHelper: true, MappedRole: undefined },
    ]
  }
  else if (mode === 'standard') {
    signupButtonConfigs.value = [
      { Slug: 'tank', Label: 'Tank', EmojiId: ROLE_EMOJI_IDS.tank, IsHelper: false, MappedRole: ROLE.Tank },
      { Slug: 'healer', Label: 'Healer', EmojiId: ROLE_EMOJI_IDS.healer, IsHelper: false, MappedRole: ROLE.Healer },
      { Slug: 'melee', Label: 'Melee', EmojiId: ROLE_EMOJI_IDS.melee, IsHelper: false, MappedRole: ROLE.Melee },
      { Slug: 'caster', Label: 'Caster', EmojiId: ROLE_EMOJI_IDS.caster, IsHelper: false, MappedRole: ROLE.Caster },
      { Slug: 'ranged', Label: 'Ranged', EmojiId: ROLE_EMOJI_IDS.ranged, IsHelper: false, MappedRole: ROLE.Ranged },
    ]
  }
}

function applyPreset(preset: 'interested' | 'interested-helper') {
  if (preset === 'interested') {
    signupButtonConfigs.value = [{ Slug: 'interested', Label: 'Interested', IsHelper: false, MappedRole: undefined }]
  }
  else {
    signupButtonConfigs.value = [
      { Slug: 'interested', Label: 'Interested', IsHelper: false, MappedRole: undefined },
      { Slug: 'helper', Label: 'Helper', IsHelper: true, MappedRole: undefined },
    ]
  }
}

// Event type options
const eventTypeOptions = computed(() => {
  return Object.keys(EventType)
    .filter(key => Number.isNaN(Number(key)))
    .map(key => ({ value: EventType[key as keyof typeof EventType], label: key }))
})

// Day of week options
const dayOfWeekOptions = [
  { value: DayOfWeek.Monday, label: 'Monday' },
  { value: DayOfWeek.Tuesday, label: 'Tuesday' },
  { value: DayOfWeek.Wednesday, label: 'Wednesday' },
  { value: DayOfWeek.Thursday, label: 'Thursday' },
  { value: DayOfWeek.Friday, label: 'Friday' },
  { value: DayOfWeek.Saturday, label: 'Saturday' },
  { value: DayOfWeek.Sunday, label: 'Sunday' },
]

// Time input helper
const timeValue = computed({
  get() {
    return formatTimeOfDay(form.TimeOfDayMinutes)
  },
  set(val: string) {
    const [h, m] = val.split(':').map(Number)
    form.TimeOfDayMinutes = (h || 0) * 60 + (m || 0)
  },
})

async function loadTemplates() {
  loading.value = true
  error.value = ''
  try {
    templates.value = await EventTemplatesApi.list()
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to load templates'
  }
  finally {
    loading.value = false
  }
}

function openCreateForm() {
  editingId.value = null
  Object.assign(form, blankForm())
  partyPreset.value = 'full-party'
  buttonMode.value = 'standard'
  signupButtonConfigs.value = []
  selectedOrganizer.value = null

  if (user.value) {
    const me = adminMembers.value.find(m => m.DiscordId === user.value!.DiscordId)
    if (me) {
      selectedOrganizer.value = me
      form.Organizer = formatMemberName(me)
    }
  }

  showForm.value = true
}

function openEditForm(template: EventTemplate) {
  editingId.value = template.Id
  Object.assign(form, {
    Name: template.Name,
    Description: template.Description,
    Type: template.Type,
    DayOfWeek: template.DayOfWeek,
    TimeOfDayMinutes: template.TimeOfDayMinutes,
    Duration: template.Duration,
    Organizer: template.Organizer,
    MaxNumberOfParticipants: template.MaxNumberOfParticipants,
    SignupButtonConfigs: template.SignupButtonConfigs,
  })
  partyPreset.value = detectPreset(template.MaxNumberOfParticipants)

  if (template.Organizer) {
    const match = adminMembers.value.find(m => m.PlayerName === template.Organizer)
    if (match)
      selectedOrganizer.value = match
    else selectedOrganizer.value = null
  }
  else {
    selectedOrganizer.value = null
  }

  if (template.SignupButtonConfigs && template.SignupButtonConfigs.length > 0) {
    const hasHelper = template.SignupButtonConfigs.some(c => c.IsHelper)
    const hasAllRoles = ['tank', 'healer', 'melee', 'caster', 'ranged']
      .every(slug => template.SignupButtonConfigs!.some(c => c.Slug === slug))
    if (hasHelper && hasAllRoles)
      buttonMode.value = 'roles-helper'
    else buttonMode.value = 'custom'
    signupButtonConfigs.value = [...template.SignupButtonConfigs]
  }
  else {
    buttonMode.value = 'standard'
    signupButtonConfigs.value = []
  }

  showForm.value = true
}

function closeForm() {
  showForm.value = false
  editingId.value = null
}

async function saveTemplate() {
  saving.value = true
  error.value = ''
  try {
    form.SignupButtonConfigs = buttonMode.value !== 'standard' ? signupButtonConfigs.value : undefined

    if (editingId.value) {
      await EventTemplatesApi.update(editingId.value, { ...form })
    }
    else {
      await EventTemplatesApi.create({ ...form })
    }
    showForm.value = false
    editingId.value = null
    await loadTemplates()
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to save template'
  }
  finally {
    saving.value = false
  }
}

const deleteConfirmId = ref<string | null>(null)

async function deleteTemplate(id: string) {
  if (deleteConfirmId.value !== id) {
    deleteConfirmId.value = id
    return
  }
  error.value = ''
  try {
    await EventTemplatesApi.delete(id)
    deleteConfirmId.value = null
    await loadTemplates()
  }
  catch (e: any) {
    error.value = e?.message || 'Failed to delete template'
  }
}

function cancelDelete() {
  deleteConfirmId.value = null
}

onMounted(async () => {
  document.addEventListener('keydown', handleGlobalKeydown)
  document.addEventListener('click', handleGlobalClick)

  await loadTemplates()

  try {
    const allMembers = await MembersApi.list()
    adminMembers.value = allMembers.filter(m => m.IsAdmin)
  }
  catch { /* optional */ }

  try {
    guildEmojis.value = await EventsApi.getGuildEmojis()
  }
  catch { /* optional */ }
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleGlobalKeydown)
  document.removeEventListener('click', handleGlobalClick)
})
</script>

<template>
  <section class="templates-view">
    <!-- Header -->
    <div class="templates-header">
      <h3 class="section-heading">
        Event Templates
      </h3>
      <BaseButton
        v-if="!showForm"
        title="New Template"
        size="small"
        @clicked="openCreateForm"
      />
    </div>

    <p v-if="error" class="form-error">
      {{ error }}
    </p>

    <!-- Template Form -->
    <div v-if="showForm" class="template-form-surface">
      <h4 class="form-title">
        {{ editingId ? 'Edit Template' : 'New Template' }}
      </h4>

      <form class="template-form" @submit.prevent="saveTemplate">
        <!-- Basic Info -->
        <div class="form-group">
          <div class="form-field">
            <label for="tpl-name">Name</label>
            <input id="tpl-name" v-model="form.Name" type="text" placeholder="Template name" required>
          </div>

          <div class="form-field">
            <label for="tpl-description">Description</label>
            <textarea id="tpl-description" v-model="form.Description" placeholder="Event description" rows="3" />
          </div>

          <div class="form-field-row">
            <div class="form-field">
              <label for="tpl-type">Type</label>
              <select id="tpl-type" v-model.number="form.Type" required>
                <option v-for="option in eventTypeOptions" :key="option.value" :value="option.value">
                  {{ option.label }}
                </option>
              </select>
            </div>
          </div>
        </div>

        <hr class="form-divider">

        <!-- Schedule -->
        <div class="form-group">
          <h5 class="form-group-label">
            Schedule
          </h5>
          <div class="form-field-row">
            <div class="form-field">
              <label for="tpl-day">Day of Week</label>
              <select id="tpl-day" v-model.number="form.DayOfWeek" required>
                <option v-for="day in dayOfWeekOptions" :key="day.value" :value="day.value">
                  {{ day.label }}
                </option>
              </select>
            </div>
            <div class="form-field">
              <label for="tpl-time">Time of Day</label>
              <input id="tpl-time" v-model="timeValue" type="time" required>
            </div>
          </div>
          <div class="form-field">
            <label>Duration</label>
            <div class="duration-controls">
              <div class="duration-presets">
                <BaseButton
                  v-for="mins in [60, 120, 180]"
                  :key="mins"
                  :state="form.Duration === mins ? 'primary' : 'secondary'"
                  :title="`${mins} min`"
                  :variant="form.Duration === mins ? 'elevated' : 'outlined'"
                  size="small"
                  type="button"
                  @clicked="form.Duration = mins"
                />
              </div>
              <input
                v-model.number="form.Duration"
                class="duration-input"
                inputmode="numeric"
                min="0"
                pattern="[0-9]*"
                placeholder="min"
                required
                type="number"
              >
            </div>
          </div>
        </div>

        <hr class="form-divider">

        <!-- Participants -->
        <div class="form-group">
          <h5 class="form-group-label">
            Participants
          </h5>
          <div class="form-field">
            <label>Party Size</label>
            <div class="party-presets">
              <BaseButton
                v-for="preset in partyPresetOptions"
                :key="preset.key"
                :state="partyPreset === preset.key ? 'primary' : 'secondary'"
                :title="preset.label"
                :variant="partyPreset === preset.key ? 'elevated' : 'outlined'"
                size="small"
                type="button"
                @clicked="setPartyPreset(preset.key)"
              />
            </div>
            <input
              v-if="partyPreset === 'custom'"
              v-model.number="form.MaxNumberOfParticipants"
              inputmode="numeric"
              max="99"
              min="1"
              pattern="[0-9]*"
              placeholder="Custom count"
              required
              type="number"
            >
          </div>
          <div class="form-field">
            <label>Organizer</label>
            <SearchableDropdown
              v-model="selectedOrganizer"
              :format-option="formatMemberName"
              :options="adminMembers"
              placeholder="Select organizer..."
              @update:model-value="form.Organizer = $event ? formatMemberName($event) : ''"
            >
              <template #selected="{ option }">
                <span class="organizer-option">
                  <img
                    v-if="option.DiscordAvatar"
                    :src="option.DiscordAvatar"
                    :alt="formatMemberName(option)"
                    class="organizer-avatar"
                  >
                  <span v-else class="organizer-avatar organizer-avatar--placeholder" />
                  {{ formatMemberName(option) }}
                </span>
              </template>
              <template #option="{ option }">
                <span class="organizer-option">
                  <img
                    v-if="option.DiscordAvatar"
                    :src="option.DiscordAvatar"
                    :alt="formatMemberName(option)"
                    class="organizer-avatar"
                  >
                  <span v-else class="organizer-avatar organizer-avatar--placeholder" />
                  {{ formatMemberName(option) }}
                </span>
              </template>
            </SearchableDropdown>
          </div>
        </div>

        <hr class="form-divider">

        <!-- Signup Buttons -->
        <div class="form-group">
          <h5 class="form-group-label">
            Signup Buttons
          </h5>
          <div class="form-field">
            <label>Button Mode</label>
            <div class="party-presets">
              <BaseButton
                :state="buttonMode === 'standard' ? 'primary' : 'secondary'"
                :variant="buttonMode === 'standard' ? 'elevated' : 'outlined'"
                size="small"
                title="Standard Roles"
                type="button"
                @clicked="setButtonMode('standard')"
              />
              <BaseButton
                :state="buttonMode === 'roles-helper' ? 'primary' : 'secondary'"
                :variant="buttonMode === 'roles-helper' ? 'elevated' : 'outlined'"
                size="small"
                title="Roles + Helper"
                type="button"
                @clicked="setButtonMode('roles-helper')"
              />
              <BaseButton
                :state="buttonMode === 'custom' ? 'primary' : 'secondary'"
                :variant="buttonMode === 'custom' ? 'elevated' : 'outlined'"
                size="small"
                title="Custom Buttons"
                type="button"
                @clicked="setButtonMode('custom')"
              />
            </div>
          </div>

          <!-- Roles + Helper: configure helper button -->
          <template v-if="buttonMode === 'roles-helper'">
            <p class="field-hint">
              Standard role buttons with emotes. Configure the helper button below:
            </p>
            <div v-if="signupButtonConfigs.find(c => c.IsHelper)" class="button-config-row">
              <div class="button-config-fields">
                <input
                  v-model="signupButtonConfigs[signupButtonConfigs.length - 1].Label"
                  class="button-config-input"
                  placeholder="Helper label"
                  type="text"
                  @input="signupButtonConfigs[signupButtonConfigs.length - 1].Slug = slugify(signupButtonConfigs[signupButtonConfigs.length - 1].Label)"
                >
                <span class="button-tag button-tag--helper">helper</span>
                <div class="emoji-picker-wrapper">
                  <button
                    class="emoji-picker-trigger"
                    type="button"
                    @click.stop="openEmojiDropdown(signupButtonConfigs.length - 1)"
                  >
                    <img
                      v-if="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)"
                      :src="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)!"
                      alt=""
                      class="emoji-preview-img"
                    >
                    <span v-else class="emoji-picker-placeholder">Emoji</span>
                  </button>
                  <div v-if="emojiDropdownOpenIndex === signupButtonConfigs.length - 1" class="emoji-dropdown">
                    <input
                      ref="emojiSearchInputRef"
                      v-model="emojiSearchQuery"
                      class="emoji-search-input"
                      placeholder="Search emojis..."
                      type="text"
                    >
                    <button
                      class="emoji-option emoji-option--clear"
                      type="button"
                      @click="clearEmoji(signupButtonConfigs.length - 1)"
                    >
                      No emoji
                    </button>
                    <div class="emoji-grid">
                      <button
                        v-for="emoji in filteredEmojis"
                        :key="emoji.Id"
                        :title="emoji.Name"
                        class="emoji-option-img"
                        type="button"
                        @click="selectEmoji(signupButtonConfigs.length - 1, emoji)"
                      >
                        <img :src="emoji.Url" :alt="emoji.Name" class="emoji-grid-img">
                      </button>
                    </div>
                  </div>
                </div>
              </div>
              <div class="button-preview">
                <span class="button-preview-btn">
                  <img
                    v-if="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)"
                    :src="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)!"
                    alt=""
                    class="button-preview-emoji"
                  >
                  {{ signupButtonConfigs[signupButtonConfigs.length - 1].Label || 'Helper' }}
                </span>
              </div>
            </div>
          </template>

          <!-- Custom buttons -->
          <template v-if="buttonMode === 'custom'">
            <div class="form-field">
              <label>Presets</label>
              <div class="party-presets">
                <BaseButton
                  size="small"
                  state="secondary"
                  title="Interested Only"
                  type="button"
                  variant="outlined"
                  @clicked="applyPreset('interested')"
                />
                <BaseButton
                  size="small"
                  state="secondary"
                  title="Interested + Helper"
                  type="button"
                  variant="outlined"
                  @clicked="applyPreset('interested-helper')"
                />
              </div>
            </div>

            <div v-for="(config, index) in signupButtonConfigs" :key="index" class="button-config-row">
              <div class="button-config-fields">
                <input
                  v-model="config.Label"
                  class="button-config-input"
                  placeholder="Label"
                  type="text"
                  @input="onLabelChange(index)"
                >
                <span v-if="config.IsHelper" class="button-tag button-tag--helper">helper</span>
                <span v-else-if="config.Slug === 'interested'" class="button-tag button-tag--interested">interested</span>
                <div class="emoji-picker-wrapper">
                  <button
                    class="emoji-picker-trigger"
                    type="button"
                    @click.stop="openEmojiDropdown(index)"
                  >
                    <img
                      v-if="getEmojiUrl(config.EmojiId)"
                      :src="getEmojiUrl(config.EmojiId)!"
                      alt=""
                      class="emoji-preview-img"
                    >
                    <span v-else class="emoji-picker-placeholder">Emoji</span>
                  </button>
                  <div v-if="emojiDropdownOpenIndex === index" class="emoji-dropdown">
                    <input
                      ref="emojiSearchInputRef"
                      v-model="emojiSearchQuery"
                      class="emoji-search-input"
                      placeholder="Search emojis..."
                      type="text"
                    >
                    <button type="button" class="emoji-option emoji-option--clear" @click="clearEmoji(index)">
                      No emoji
                    </button>
                    <div class="emoji-grid">
                      <button
                        v-for="emoji in filteredEmojis"
                        :key="emoji.Id"
                        :title="emoji.Name"
                        class="emoji-option-img"
                        type="button"
                        @click="selectEmoji(index, emoji)"
                      >
                        <img :src="emoji.Url" :alt="emoji.Name" class="emoji-grid-img">
                      </button>
                    </div>
                  </div>
                </div>
              </div>
              <div class="button-preview">
                <span class="button-preview-btn">
                  <img
                    v-if="getEmojiUrl(config.EmojiId)"
                    :src="getEmojiUrl(config.EmojiId)!"
                    alt=""
                    class="button-preview-emoji"
                  >
                  {{ config.Label || '...' }}
                </span>
              </div>
              <button
                class="button-remove"
                title="Remove button"
                type="button"
                @click="removeButton(index)"
              >
                <svg fill="none" height="14" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" viewBox="0 0 24 24" width="14">
                  <line x1="18" x2="6" y1="6" y2="18" /><line x1="6" x2="18" y1="6" y2="18" />
                </svg>
              </button>
            </div>

            <BaseButton
              size="small"
              state="secondary"
              title="+ Add Button"
              type="button"
              variant="outlined"
              @clicked="addButton"
            />
          </template>
        </div>

        <!-- Actions -->
        <div class="form-actions">
          <BaseButton
            state="secondary"
            title="Cancel"
            variant="outlined"
            @clicked="closeForm"
          />
          <BaseButton
            :disabled="saving"
            :title="saving ? 'Saving...' : (editingId ? 'Save Changes' : 'Create Template')"
            type="submit"
          />
        </div>
      </form>
    </div>

    <!-- Template List -->
    <div v-if="!showForm" class="templates-list">
      <div v-if="loading" class="placeholder-box">
        <p class="placeholder-box__text">
          Loading templates...
        </p>
      </div>

      <div v-else-if="templates.length === 0" class="placeholder-box">
        <p class="placeholder-box__text">
          No event templates yet.
        </p>
        <p class="placeholder-box__subtext">
          Create a template to quickly set up recurring events.
        </p>
      </div>

      <div v-else class="templates-table-wrap">
        <table class="templates-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Schedule</th>
              <th>Duration</th>
              <th>Party Size</th>
              <th>Organizer</th>
              <th class="actions-col" />
            </tr>
          </thead>
          <tbody>
            <tr v-for="tpl in templates" :key="tpl.Id">
              <td class="name-cell">
                {{ tpl.Name }}
              </td>
              <td>
                <span class="type-badge">{{ EventType[tpl.Type] }}</span>
              </td>
              <td>{{ dayOfWeekToString(tpl.DayOfWeek) }} {{ formatTimeOfDay(tpl.TimeOfDayMinutes) }}</td>
              <td>{{ tpl.Duration }} min</td>
              <td>{{ tpl.MaxNumberOfParticipants }}</td>
              <td>{{ tpl.Organizer || '—' }}</td>
              <td class="actions-cell">
                <BaseButton
                  size="small"
                  state="secondary"
                  title="Edit"
                  variant="text"
                  @clicked="openEditForm(tpl)"
                />
                <template v-if="deleteConfirmId === tpl.Id">
                  <BaseButton
                    size="small"
                    state="danger"
                    title="Confirm"
                    variant="elevated"
                    @clicked="deleteTemplate(tpl.Id)"
                  />
                  <BaseButton
                    size="small"
                    state="secondary"
                    title="Cancel"
                    variant="text"
                    @clicked="cancelDelete"
                  />
                </template>
                <BaseButton
                  v-else
                  size="small"
                  state="danger"
                  title="Delete"
                  variant="text"
                  @clicked="deleteTemplate(tpl.Id)"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>

<style scoped>
.templates-view {
  width: 100%;
}

.templates-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}

.section-heading {
  font-size: 1.5rem;
  font-weight: 600;
  color: var(--fg);
  margin: 0;
}

.form-error {
  padding: 0.75rem 1rem;
  background: var(--alert-error-bg);
  color: var(--alert-error-fg);
  border: 1px solid var(--alert-error-border);
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

/* Template Form */
.template-form-surface {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
  margin-bottom: 1.5rem;
}

:root[data-theme='dark'] .template-form-surface {
  background: rgba(18, 26, 45, 0.7);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .template-form-surface {
    background: rgba(18, 26, 45, 0.7);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.form-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--fg);
  margin: 0 0 1.25rem 0;
}

.template-form {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.form-group { display: flex; flex-direction: column; gap: 0.75rem; }
.form-group-label {
  font-size: 0.8125rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
  margin: 0;
}

.form-divider { border: none; border-top: 1px solid var(--border); margin: 1.25rem 0; }

.form-field { display: flex; flex-direction: column; gap: 0.25rem; }
.form-field label { font-weight: 500; font-size: 0.875rem; color: var(--fg); }

.form-field-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.75rem;
}

.field-hint { font-size: 0.75rem; color: var(--muted); }

.duration-controls { display: flex; align-items: center; gap: 0.5rem; }
.duration-presets { display: flex; gap: 0.25rem; flex-shrink: 0; }
.duration-input { width: 5rem; flex-shrink: 0; }

.party-presets { display: flex; flex-wrap: wrap; gap: 0.25rem; }

.form-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 1.25rem;
}

/* Organizer */
.organizer-option { display: inline-flex; align-items: center; gap: 0.5rem; }
.organizer-avatar { width: 24px; height: 24px; border-radius: 50%; flex-shrink: 0; object-fit: cover; }
.organizer-avatar--placeholder {
  display: inline-block;
  background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 50%, #ec4899 100%);
}

/* Button config rows */
.button-config-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: 8px;
}

.button-config-fields { display: flex; flex-wrap: wrap; align-items: center; gap: 0.375rem; flex: 1; min-width: 0; }

.button-config-input {
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--bg);
  color: var(--fg);
  min-width: 0;
  width: 7rem;
}

.button-tag {
  display: inline-flex;
  align-items: center;
  padding: 0.125rem 0.375rem;
  font-size: 0.6875rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  border-radius: 4px;
  white-space: nowrap;
  text-transform: lowercase;
}

.button-tag--helper { background: rgba(124, 58, 237, 0.12); color: #7c3aed; }
.button-tag--interested { background: rgba(37, 99, 235, 0.12); color: #2563eb; }

:root[data-theme='dark'] .button-tag--helper { background: rgba(167, 139, 250, 0.15); color: #a78bfa; }
:root[data-theme='dark'] .button-tag--interested { background: rgba(96, 165, 250, 0.15); color: #60a5fa; }

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .button-tag--helper { background: rgba(167, 139, 250, 0.15); color: #a78bfa; }
  :root:not([data-theme='light']) .button-tag--interested { background: rgba(96, 165, 250, 0.15); color: #60a5fa; }
}

.button-remove {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 1.5rem;
  height: 1.5rem;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--muted);
  cursor: pointer;
  transition: color 0.15s ease, background 0.15s ease;
}

.button-remove:hover { color: #dc2626; background: rgba(220, 38, 38, 0.08); }

:root[data-theme='dark'] .button-remove:hover { color: #f87171; background: rgba(248, 113, 113, 0.12); }

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .button-remove:hover { color: #f87171; background: rgba(248, 113, 113, 0.12); }
}

/* Emoji picker */
.emoji-picker-wrapper { position: relative; }

.emoji-picker-trigger {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--bg);
  color: var(--fg);
  cursor: pointer;
  min-width: 6rem;
  height: 1.75rem;
}

.emoji-picker-trigger:hover { border-color: var(--link); }
.emoji-preview-img { width: 18px; height: 18px; }
.emoji-picker-placeholder { color: var(--muted); font-size: 0.75rem; }

.emoji-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  z-index: 100;
  width: 240px;
  max-height: 240px;
  overflow-y: auto;
  background: var(--card, #fff);
  border: 1px solid var(--border);
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
  padding: 0.375rem;
  margin-top: 4px;
}

.emoji-search-input {
  width: 100%;
  padding: 0.25rem 0.5rem;
  font-size: 0.8125rem;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--bg);
  color: var(--fg);
  margin-bottom: 0.375rem;
}

.emoji-option {
  display: block;
  width: 100%;
  text-align: left;
  padding: 0.25rem 0.5rem;
  border: none;
  background: none;
  color: var(--fg);
  font-size: 0.75rem;
  cursor: pointer;
  border-radius: 4px;
}

.emoji-option:hover { background: var(--muted-bg); }

.emoji-option--clear {
  color: var(--muted);
  border-bottom: 1px solid var(--border);
  margin-bottom: 0.25rem;
  padding-bottom: 0.375rem;
  border-radius: 0;
}

.emoji-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 2px; }

.emoji-option-img {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border: none;
  background: none;
  border-radius: 4px;
  cursor: pointer;
}

.emoji-option-img:hover { background: var(--muted-bg); }
.emoji-grid-img { width: 24px; height: 24px; }

.button-preview { flex-shrink: 0; }

.button-preview-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.625rem;
  font-size: 0.8125rem;
  font-weight: 600;
  border-radius: 6px;
  background: #5865f2;
  color: #fff;
  white-space: nowrap;
}

.button-preview-emoji { width: 16px; height: 16px; }

/* Placeholder */
.placeholder-box {
  background: var(--muted-bg);
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
}

.placeholder-box__text { color: var(--muted); font-size: 1.125rem; }
.placeholder-box__subtext { color: var(--muted); font-size: 0.875rem; margin-top: 0.5rem; }

/* Templates Table */
.templates-table-wrap {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
}

:root[data-theme='dark'] .templates-table-wrap {
  background: rgba(18, 26, 45, 0.7);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .templates-table-wrap {
    background: rgba(18, 26, 45, 0.7);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.templates-table {
  width: 100%;
  border-collapse: collapse;
}

.templates-table th {
  text-align: left;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
  padding: 0.75rem;
  border-bottom: 1px solid var(--border);
}

.templates-table td {
  padding: 0.75rem;
  font-size: 0.875rem;
  color: var(--fg);
  border-bottom: 1px solid var(--border);
}

.templates-table tbody tr:last-child td {
  border-bottom: none;
}

.templates-table tbody tr:hover {
  background: color-mix(in oklab, var(--card, #fff) 90%, var(--link) 10%);
}

.name-cell {
  font-weight: 500;
}

.type-badge {
  display: inline-block;
  padding: 0.125rem 0.5rem;
  font-size: 0.75rem;
  font-weight: 500;
  border-radius: 999px;
  background: var(--muted-bg);
  color: var(--fg);
}

.actions-col {
  width: 1%;
  white-space: nowrap;
}

.actions-cell {
  display: flex;
  gap: 0.25rem;
  align-items: center;
}

/* Responsive */
@media (max-width: 768px) {
  .template-form-surface { padding: 1rem 1.25rem 1.25rem; }
  .duration-controls { flex-wrap: wrap; }
  .form-field-row { grid-template-columns: 1fr; }

  .templates-table-wrap { overflow-x: auto; }
  .templates-table { min-width: 600px; }
}

@media (max-width: 480px) {
  .template-form-surface { padding: 0.75rem 1rem 1rem; border-radius: 12px; }
  .form-group-label { font-size: 0.75rem; }
  .duration-presets { flex-wrap: wrap; }
  .form-actions { flex-direction: column; }
}
</style>
