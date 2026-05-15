<script lang="ts" setup>
import type { FCEvent, GuildEmoji, SignupButtonConfig } from '@/features/events/events.types'
import type { Fight } from '@/features/fights/fights.types'
import type { RecurrenceConfig } from '@/utils/ical'
import { computed, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import DateTimePicker from '@/components/DateTimePicker.vue'
import EventCard from '@/components/events/EventCard.vue'
import RecurrenceOptions from '@/components/events/RecurrenceOptions.vue'
import SearchableDropdown from '@/components/SearchableDropdown.vue'
import { useAuth } from '@/composables/useAuth'
import { EventsApi } from '@/features/events/events.api'
import { EventType, OccurrenceStatus, ROLE } from '@/features/events/events.types'
import { FightsApi } from '@/features/fights/fights.api'
import { fightTypeToString } from '@/features/fights/fights.types'
import mapsPlaceholder from '@/static/img/maps-placeholder.png'
import { generateICalString, parseICalString } from '@/utils/ical'

const router = useRouter()
const route = useRoute()
const { user, isAdmin, loadMe } = useAuth()

function toAbsoluteUrl(url: string): string {
  if (!url)
    return url
  if (/^(?:[a-z]+:)?\/\//i.test(url))
    return url
  const withSlash = url.startsWith('/') ? url : `/${url}`
  return `${window.location.origin}${withSlash}`
}

const mapsPlaceholderAbsolute = toAbsoluteUrl(mapsPlaceholder)

const loading = ref(false)
const error = ref('')
const isEditMode = computed(() => !!route.params.id)

const fights = ref<Fight[]>([])
const selectedFight = ref<Fight | null>(null)

type PartyPreset = 'light-party' | 'full-party' | 'alliance-raid' | 'any' | 'custom'
const partyPreset = ref<PartyPreset>('full-party')

const form = reactive<FCEvent>({
  Id: '',
  Name: '',
  Description: '',
  DiscordMessageId: '',
  PictureUrl: '',
  Type: EventType.Other,
  FightId: undefined,
  Organizer: '',
  StartDate: new Date(),
  EndDate: new Date(),
  Duration: 30,
  ICalString: '',
  SignupType: 0,
  MaxNumberOfParticipants: 8,
  SignupButtonConfigs: undefined,
  Signups: [],
  Groups: [],
  Occurrences: [],
  AvailableForSignup: false,
  IsArchived: false,
  CanBeArchived: false,
})

const recurrence = ref<RecurrenceConfig>({
  enabled: false,
  frequency: 'WEEKLY',
  interval: 1,
  endType: 'never',
  byWeekday: [],
})

// Signup button configuration
type ButtonMode = 'standard' | 'roles-helper' | 'custom'
const buttonMode = ref<ButtonMode>('standard')
const signupButtonConfigs = ref<SignupButtonConfig[]>([])
const guildEmojis = ref<GuildEmoji[]>([])
const emojiSearchQuery = ref('')
const emojiDropdownOpenIndex = ref<number | null>(null)
const emojiSearchInputRef = ref<HTMLInputElement | null>(null)

// Known role emoji IDs (used for "Roles + Helper" preset)
const ROLE_EMOJI_IDS: Record<string, string> = {
  tank: '1380979172423499846',
  healer: '1380979170787721368',
  melee: '873621778214318091',
  ranged: '873621778453368895',
  caster: '873621778566635540',
}

const filteredEmojis = computed(() => {
  const query = emojiSearchQuery.value.trim().toLowerCase()
  if (!query) return guildEmojis.value
  return guildEmojis.value.filter(e => e.Name.toLowerCase().includes(query))
})

function getEmojiById(id?: string): GuildEmoji | undefined {
  if (!id) return undefined
  return guildEmojis.value.find(e => e.Id === id)
}

function getEmojiUrl(id?: string): string | null {
  if (!id) return null
  const emoji = getEmojiById(id)
  if (emoji) return emoji.Url
  return `https://cdn.discordapp.com/emojis/${id}.webp?size=20`
}

function slugify(text: string): string {
  return text.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}

function addButton() {
  signupButtonConfigs.value.push({
    Slug: '',
    Label: '',
    EmojiId: undefined,
    IsHelper: false,
    MappedRole: undefined,
  })
}

function removeButton(index: number) {
  signupButtonConfigs.value.splice(index, 1)
}

function onLabelChange(index: number) {
  const config = signupButtonConfigs.value[index]
  config.Slug = slugify(config.Label)
}

function openEmojiDropdown(index: number) {
  emojiSearchQuery.value = ''
  emojiDropdownOpenIndex.value = index
  nextTick(() => {
    emojiSearchInputRef.value?.focus()
  })
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
  if (e.key === 'Escape' && emojiDropdownOpenIndex.value !== null) {
    closeEmojiDropdown()
  }
}

function handleGlobalClick(e: MouseEvent) {
  if (emojiDropdownOpenIndex.value === null) return
  const target = e.target as HTMLElement
  if (!target.closest('.emoji-picker-wrapper')) {
    closeEmojiDropdown()
  }
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
    signupButtonConfigs.value = []
  }
}

function applyPreset(preset: 'interested' | 'interested-helper') {
  switch (preset) {
    case 'interested':
      signupButtonConfigs.value = [
        { Slug: 'interested', Label: 'Interested', IsHelper: false, MappedRole: undefined },
      ]
      break
    case 'interested-helper':
      signupButtonConfigs.value = [
        { Slug: 'interested', Label: 'Interested', IsHelper: false, MappedRole: undefined },
        { Slug: 'helper', Label: 'Helper', IsHelper: true, MappedRole: undefined },
      ]
      break
  }
}

const previewCollapsed = ref(true)

const previewEvent = computed<FCEvent>(() => {
  const icalStr = generateICalString(form, recurrence.value)
  return {
    ...form,
    Organizer: form.Organizer || user.value?.PlayerName || 'You',
    ICalString: icalStr,
    AvailableForSignup: true,
    Occurrences: [{
      Id: 'preview',
      OccurrenceDate: form.StartDate,
      Status: OccurrenceStatus.Scheduled,
    }],
    IsArchived: false,
    CanBeArchived: false,
  }
})

const previewEventRef = ref<FCEvent>(previewEvent.value)
watch(previewEvent, (val) => { previewEventRef.value = { ...val } }, { deep: true })

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
      if (form.MaxNumberOfParticipants === 4 || form.MaxNumberOfParticipants === 8
        || form.MaxNumberOfParticipants === 24 || form.MaxNumberOfParticipants === 99) {
        form.MaxNumberOfParticipants = 0
      }
      break
  }
}

function setDuration(minutes: number) {
  form.Duration = minutes
}

function detectPreset(maxParticipants: number): PartyPreset {
  switch (maxParticipants) {
    case 4: return 'light-party'
    case 8: return 'full-party'
    case 24: return 'alliance-raid'
    case 99: return 'any'
    default: return 'custom'
  }
}

const isInputDisabled = computed(() => partyPreset.value !== 'custom')

const partyPresetOptions = [
  { key: 'light-party' as PartyPreset, label: 'Light (4)' },
  { key: 'full-party' as PartyPreset, label: 'Full (8)' },
  { key: 'alliance-raid' as PartyPreset, label: 'Alliance (24)' },
  { key: 'any' as PartyPreset, label: 'Any (99)' },
  { key: 'custom' as PartyPreset, label: 'Custom' },
]

const eventTypeOptions = computed(() => {
  return Object.keys(EventType)
    .filter(key => Number.isNaN(Number(key)))
    .map(key => ({
      value: EventType[key as keyof typeof EventType],
      label: key,
    }))
})

const showFightSelection = computed(() => {
  const fightCompatibleTypes = [
    EventType.Academy, EventType.Downsynced, EventType.BLU,
    EventType.Farming, EventType.Raid, EventType.MinIlvl, EventType.Other,
  ]
  return fightCompatibleTypes.includes(form.Type)
})

function formatFight(fight: Fight): string {
  return `${fight.Name} (${fightTypeToString(fight.Type)})`
}

onMounted(async () => {
  document.addEventListener('keydown', handleGlobalKeydown)
  document.addEventListener('click', handleGlobalClick)

  try {
    fights.value = await FightsApi.list()
  }
  catch {
    // Fights are optional
  }

  try {
    guildEmojis.value = await EventsApi.getGuildEmojis()
  }
  catch {
    // Emojis are optional
  }

  if (isEditMode.value) {
    loading.value = true
    try {
      const eventData = await EventsApi.get(route.params.id as string)
      if (eventData) {
        Object.assign(form, eventData)
        if (form.PictureUrl) {
          form.PictureUrl = toAbsoluteUrl(form.PictureUrl)
        }
        partyPreset.value = detectPreset(eventData.MaxNumberOfParticipants)
        if (eventData.FightId) {
          selectedFight.value = fights.value.find(f => f.Id === eventData.FightId) || null
        }
        if (eventData.ICalString) {
          const parsedRecurrence = parseICalString(eventData.ICalString)
          if (parsedRecurrence) {
            recurrence.value = parsedRecurrence
          }
        }
        if (eventData.SignupButtonConfigs && eventData.SignupButtonConfigs.length > 0) {
          // Detect if it's the "roles + helper" preset
          const hasHelper = eventData.SignupButtonConfigs.some(c => c.IsHelper)
          const hasAllRoles = ['tank', 'healer', 'melee', 'caster', 'ranged']
            .every(slug => eventData.SignupButtonConfigs!.some(c => c.Slug === slug))
          if (hasHelper && hasAllRoles) {
            buttonMode.value = 'roles-helper'
          }
          else {
            buttonMode.value = 'custom'
          }
          signupButtonConfigs.value = eventData.SignupButtonConfigs
        }
      }
    }
    catch (e: any) {
      error.value = e?.message || 'Failed to load event'
    }
    finally {
      loading.value = false
    }
  }
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleGlobalKeydown)
  document.removeEventListener('click', handleGlobalClick)
})

watch(selectedFight, (newFight) => {
  if (newFight) {
    if (newFight.ImageUrl) {
      form.PictureUrl = toAbsoluteUrl(newFight.ImageUrl)
    }
    form.FightId = newFight.Id
  }
  else {
    form.FightId = undefined
  }
})

watch(
  () => form.Type,
  (newType) => {
    if (newType === EventType.Maps) {
      if (!form.PictureUrl || form.PictureUrl === mapsPlaceholder || form.PictureUrl === mapsPlaceholderAbsolute) {
        form.PictureUrl = mapsPlaceholderAbsolute
      }
    }
    else {
      if ((form.PictureUrl === mapsPlaceholder || form.PictureUrl === mapsPlaceholderAbsolute) && !selectedFight.value) {
        form.PictureUrl = ''
      }
    }
    if (!showFightSelection.value) {
      selectedFight.value = null
      form.FightId = undefined
    }
  },
  { immediate: true },
)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await loadMe()
    if (!isAdmin.value) {
      error.value = 'You do not have permission to create/edit events.'
      return
    }
    form.Organizer = user.value?.PlayerName ?? ''
    if (form.PictureUrl) {
      form.PictureUrl = toAbsoluteUrl(form.PictureUrl)
    }
    form.ICalString = generateICalString(form, recurrence.value)
    form.SignupButtonConfigs = buttonMode.value !== 'standard' ? signupButtonConfigs.value : undefined

    if (isEditMode.value) {
      await EventsApi.update(form.Id, form)
    }
    else {
      await EventsApi.create(form)
    }
    await router.push({ name: 'events' })
  }
  catch (e: any) {
    error.value = e?.message || `Failed to ${isEditMode.value ? 'update' : 'create'} event`
  }
  finally {
    loading.value = false
  }
}

function cancel() {
  router.push({ name: 'events' })
}
</script>

<template>
  <div class="create-event-page">
    <div class="page-header">
      <h2 class="page-title">
        {{ isEditMode ? 'Edit Event' : 'New Event' }}
      </h2>
    </div>

    <p v-if="error" class="form-error">
      {{ error }}
    </p>

    <div class="create-event-layout">
      <!-- Form Column -->
      <form class="event-form" @submit.prevent="submit">
        <div class="form-surface">
          <!-- Basic Information -->
          <div class="form-group">
            <div class="form-field">
              <label for="event-name">Name</label>
              <input id="event-name" v-model="form.Name" placeholder="Event name" required type="text">
            </div>

            <div class="form-field">
              <label for="event-description">Description</label>
              <textarea id="event-description" v-model="form.Description" placeholder="Describe the event (supports Discord formatting)" rows="4" />
            </div>

            <div class="form-field-row">
              <div class="form-field">
                <label for="event-type">Type</label>
                <select id="event-type" v-model.number="form.Type" required>
                  <option v-for="option in eventTypeOptions" :key="option.value" :value="option.value">
                    {{ option.label }}
                  </option>
                </select>
              </div>
              <div v-if="form.DiscordMessageId" class="form-field">
                <label for="discord-id">Discord Message ID</label>
                <input id="discord-id" v-model="form.DiscordMessageId" placeholder="Discord message ID" type="text">
              </div>
            </div>
          </div>

          <hr class="form-divider">

          <!-- Fight & Media -->
          <div class="form-group">
            <h3 class="form-group-label">
              {{ showFightSelection ? 'Fight & Media' : 'Media' }}
            </h3>
            <div v-if="showFightSelection" class="form-field">
              <label>Fight (optional)</label>
              <SearchableDropdown
                v-model="selectedFight"
                :format-option="formatFight"
                :options="fights"
                placeholder="Search fights..."
              />
              <small class="field-hint">Auto-fills the event image</small>
            </div>
            <div class="form-field">
              <label for="picture-url">Image URL</label>
              <input
                id="picture-url"
                v-model="form.PictureUrl"
                :placeholder="showFightSelection ? 'Auto-filled from fight' : 'https://...'"
                type="url"
              >
            </div>
          </div>

          <hr class="form-divider">

          <!-- Schedule -->
          <div class="form-group">
            <h3 class="form-group-label">
              Schedule
            </h3>
            <div class="form-field">
              <DateTimePicker
                v-model="form.StartDate"
                :required="true"
                label="Start Date & Time"
              />
            </div>
            <div class="form-field">
              <label>Duration</label>
              <div class="duration-controls">
                <div class="duration-presets">
                  <BaseButton
                    v-for="mins in [60, 120, 180]"
                    :key="mins"
                    :state="form.Duration === mins ? 'primary' : 'secondary'"
                    :variant="form.Duration === mins ? 'elevated' : 'outlined'"
                    :title="`${mins} min`"
                    size="small"
                    type="button"
                    @clicked="setDuration(mins)"
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
            <div class="form-field">
              <RecurrenceOptions v-model="recurrence" />
            </div>
          </div>

          <hr class="form-divider">

          <!-- Participants -->
          <div class="form-group">
            <h3 class="form-group-label">
              Participants
            </h3>
            <div class="form-field">
              <label>Party Size</label>
              <div class="party-presets">
                <BaseButton
                  v-for="preset in partyPresetOptions"
                  :key="preset.key"
                  :state="partyPreset === preset.key ? 'primary' : 'secondary'"
                  :variant="partyPreset === preset.key ? 'elevated' : 'outlined'"
                  :title="preset.label"
                  size="small"
                  type="button"
                  @clicked="setPartyPreset(preset.key)"
                />
              </div>
              <input
                v-if="partyPreset === 'custom'"
                v-model.number="form.MaxNumberOfParticipants"
                :disabled="isInputDisabled"
                inputmode="numeric"
                max="99"
                min="1"
                pattern="[0-9]*"
                placeholder="Custom count"
                required
                type="number"
              >
            </div>
            <div v-if="isEditMode" class="form-field">
              <label>Organizer</label>
              <input :value="user?.PlayerName || ''" disabled type="text">
            </div>
          </div>

          <hr class="form-divider">

          <!-- Signup Buttons -->
          <div class="form-group">
            <h3 class="form-group-label">
              Signup Buttons
            </h3>
            <div class="form-field">
              <label>Button Mode</label>
              <div class="party-presets">
                <BaseButton
                  :state="buttonMode === 'standard' ? 'primary' : 'secondary'"
                  :variant="buttonMode === 'standard' ? 'elevated' : 'outlined'"
                  title="Standard Roles"
                  size="small"
                  type="button"
                  @clicked="setButtonMode('standard')"
                />
                <BaseButton
                  :state="buttonMode === 'roles-helper' ? 'primary' : 'secondary'"
                  :variant="buttonMode === 'roles-helper' ? 'elevated' : 'outlined'"
                  title="Roles + Helper"
                  size="small"
                  type="button"
                  @clicked="setButtonMode('roles-helper')"
                />
                <BaseButton
                  :state="buttonMode === 'custom' ? 'primary' : 'secondary'"
                  :variant="buttonMode === 'custom' ? 'elevated' : 'outlined'"
                  title="Custom Buttons"
                  size="small"
                  type="button"
                  @clicked="setButtonMode('custom')"
                />
              </div>
            </div>

            <!-- Roles + Helper: only configure the helper button -->
            <template v-if="buttonMode === 'roles-helper'">
              <p class="field-hint">Standard role buttons (Tank, Healer, Melee, Caster, Ranged) with emotes. Configure the helper button below:</p>
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
                      type="button"
                      class="emoji-picker-trigger"
                      @click.stop="openEmojiDropdown(signupButtonConfigs.length - 1)"
                    >
                      <img v-if="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)" :src="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)!" alt="" class="emoji-preview-img">
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
                      <button type="button" class="emoji-option emoji-option--clear" @click="clearEmoji(signupButtonConfigs.length - 1)">
                        No emoji
                      </button>
                      <div class="emoji-grid">
                        <button
                          v-for="emoji in filteredEmojis"
                          :key="emoji.Id"
                          type="button"
                          class="emoji-option-img"
                          :title="emoji.Name"
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
                    <img v-if="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)" :src="getEmojiUrl(signupButtonConfigs[signupButtonConfigs.length - 1].EmojiId)!" alt="" class="button-preview-emoji">
                    {{ signupButtonConfigs[signupButtonConfigs.length - 1].Label || 'Helper' }}
                  </span>
                </div>
              </div>
            </template>

            <!-- Custom buttons: full config -->
            <template v-if="buttonMode === 'custom'">
              <div class="form-field">
                <label>Presets</label>
                <div class="party-presets">
                  <BaseButton
                    title="Interested Only"
                    size="small"
                    state="secondary"
                    variant="outlined"
                    type="button"
                    @clicked="applyPreset('interested')"
                  />
                  <BaseButton
                    title="Interested + Helper"
                    size="small"
                    state="secondary"
                    variant="outlined"
                    type="button"
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
                      type="button"
                      class="emoji-picker-trigger"
                      @click.stop="openEmojiDropdown(index)"
                    >
                      <img v-if="getEmojiUrl(config.EmojiId)" :src="getEmojiUrl(config.EmojiId)!" alt="" class="emoji-preview-img">
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
                          type="button"
                          class="emoji-option-img"
                          :title="emoji.Name"
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
                    <img v-if="getEmojiUrl(config.EmojiId)" :src="getEmojiUrl(config.EmojiId)!" alt="" class="button-preview-emoji">
                    {{ config.Label || '...' }}
                  </span>
                </div>
                <button
                  type="button"
                  class="button-remove"
                  title="Remove button"
                  @click="removeButton(index)"
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></svg>
                </button>
              </div>

              <BaseButton
                size="small"
                state="secondary"
                title="+ Add Button"
                variant="outlined"
                type="button"
                @clicked="addButton"
              />
            </template>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="form-actions">
          <BaseButton :disabled="loading" state="secondary" title="Cancel" variant="outlined" @clicked="cancel" />
          <BaseButton
            :disabled="loading"
            :title="loading ? (isEditMode ? 'Saving...' : 'Creating...') : (isEditMode ? 'Save Changes' : 'Create Event')"
            type="submit"
          />
        </div>
      </form>

      <!-- Preview Column (desktop) -->
      <aside class="preview-column">
        <div class="preview-sticky">
          <span class="preview-label">Live Preview</span>
          <div class="preview-card-wrap">
            <EventCard
              v-model:fc-event="previewEventRef"
              :is-member="true"
            />
          </div>
        </div>
      </aside>
    </div>

    <!-- Preview Toggle (mobile) -->
    <div class="preview-mobile">
      <button class="preview-toggle" type="button" @click="previewCollapsed = !previewCollapsed">
        <span>{{ previewCollapsed ? 'Show Preview' : 'Hide Preview' }}</span>
        <svg
          :class="{ 'chevron--open': !previewCollapsed }"
          class="chevron"
          fill="none"
          height="16"
          stroke="currentColor"
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          viewBox="0 0 24 24"
          width="16"
        >
          <polyline points="6 9 12 15 18 9" />
        </svg>
      </button>
      <div v-if="!previewCollapsed" class="preview-mobile-card">
        <EventCard
          v-model:fc-event="previewEventRef"
          :is-member="true"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.create-event-page {
  max-width: 1120px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 2rem;
}

.page-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0;
  color: var(--fg);
  letter-spacing: -0.02em;
}

.form-error {
  padding: 0.75rem 1rem;
  background: var(--alert-error-bg);
  color: var(--alert-error-fg);
  border: 1px solid var(--alert-error-border);
  border-radius: 12px;
  margin-bottom: 1.5rem;
}

/* Two-column layout */
.create-event-layout {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2rem;
}

@media (min-width: 960px) {
  .create-event-layout {
    grid-template-columns: 1fr 340px;
  }
}

/* Form */
.event-form {
  min-width: 0;
}

.form-surface {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  padding: 1.5rem;
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.08),
    inset 0 1px 0 rgba(255, 255, 255, 0.5);
}

:root[data-theme='dark'] .form-surface {
  background: rgba(18, 26, 45, 0.7);
  border-color: rgba(255, 255, 255, 0.15);
  box-shadow:
    0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .form-surface {
    background: rgba(18, 26, 45, 0.7);
    border-color: rgba(255, 255, 255, 0.15);
    box-shadow:
      0 4px 16px rgba(0, 0, 0, 0.3),
      inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.form-group-label {
  font-size: 0.8125rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
  margin: 0;
}

.form-divider {
  border: none;
  border-top: 1px solid var(--border);
  margin: 1.25rem 0;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.form-field label {
  font-weight: 500;
  font-size: 0.875rem;
  color: var(--fg);
}

.form-field-row {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.75rem;
}

.field-hint {
  font-size: 0.75rem;
  color: var(--muted);
}

/* Duration */
.duration-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.duration-presets {
  display: flex;
  gap: 0.25rem;
  flex-shrink: 0;
}

.duration-input {
  width: 5rem;
  flex-shrink: 0;
}

/* Party presets */
.party-presets {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}

/* Button config */
.button-config-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: 8px;
}

.button-config-fields {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.375rem;
  flex: 1;
  min-width: 0;
}

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

/* Button tags */
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

.button-tag--helper {
  background: rgba(124, 58, 237, 0.12);
  color: #7c3aed;
}

.button-tag--interested {
  background: rgba(37, 99, 235, 0.12);
  color: #2563eb;
}

:root[data-theme='dark'] .button-tag--helper {
  background: rgba(167, 139, 250, 0.15);
  color: #a78bfa;
}

:root[data-theme='dark'] .button-tag--interested {
  background: rgba(96, 165, 250, 0.15);
  color: #60a5fa;
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .button-tag--helper {
    background: rgba(167, 139, 250, 0.15);
    color: #a78bfa;
  }

  :root:not([data-theme='light']) .button-tag--interested {
    background: rgba(96, 165, 250, 0.15);
    color: #60a5fa;
  }
}

/* Remove button */
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

.button-remove:hover {
  color: #dc2626;
  background: rgba(220, 38, 38, 0.08);
}

:root[data-theme='dark'] .button-remove:hover {
  color: #f87171;
  background: rgba(248, 113, 113, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .button-remove:hover {
    color: #f87171;
    background: rgba(248, 113, 113, 0.12);
  }
}

/* Emoji picker */
.emoji-picker-wrapper {
  position: relative;
}

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

.emoji-picker-trigger:hover {
  border-color: var(--link);
}

.emoji-preview-img {
  width: 18px;
  height: 18px;
}

.emoji-picker-placeholder {
  color: var(--muted);
  font-size: 0.75rem;
}

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
  box-shadow: 0 4px 16px rgba(0,0,0,0.15);
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

.emoji-option:hover {
  background: var(--muted-bg);
}

.emoji-option--clear {
  color: var(--muted);
  border-bottom: 1px solid var(--border);
  margin-bottom: 0.25rem;
  padding-bottom: 0.375rem;
  border-radius: 0;
}

.emoji-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 2px;
}

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

.emoji-option-img:hover {
  background: var(--muted-bg);
}

.emoji-grid-img {
  width: 24px;
  height: 24px;
}

/* Button preview */
.button-preview {
  flex-shrink: 0;
}

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

.button-preview-emoji {
  width: 16px;
  height: 16px;
}

/* Form actions */
.form-actions {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 1.25rem;
}

/* Preview column (desktop) */
.preview-column {
  display: none;
}

@media (min-width: 960px) {
  .preview-column {
    display: block;
  }
}

.preview-sticky {
  position: sticky;
  top: 5rem;
}

.preview-label {
  display: block;
  font-size: 0.6875rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--muted);
  margin-bottom: 0.5rem;
}

.preview-card-wrap {
  pointer-events: none;
}

.preview-card-wrap :deep(.event-card__actions) {
  display: none;
}

/* Preview mobile */
.preview-mobile {
  margin-top: 1.5rem;
}

@media (min-width: 960px) {
  .preview-mobile {
    display: none;
  }
}

.preview-toggle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.75rem 1rem;
  background: var(--muted-bg);
  border: 1px solid var(--border);
  border-radius: 12px;
  color: var(--fg);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.2s ease;
}

.preview-toggle:hover {
  background: color-mix(in oklab, var(--muted-bg) 85%, var(--link) 15%);
}

.chevron {
  margin-left: auto;
  transition: transform 0.2s ease;
}

.chevron--open {
  transform: rotate(180deg);
}

.preview-mobile-card {
  margin-top: 0.75rem;
  pointer-events: none;
}

.preview-mobile-card :deep(.event-card__actions) {
  display: none;
}

/* Responsive */
@media (max-width: 768px) {
  .form-surface {
    padding: 1rem 1.25rem 1.25rem;
  }

  .duration-controls {
    flex-wrap: wrap;
  }

  .form-field-row {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 480px) {
  .page-header {
    margin-bottom: 1.5rem;
  }

  .form-surface {
    padding: 0.75rem 1rem 1rem;
    border-radius: 12px;
  }

  .form-group-label {
    font-size: 0.75rem;
  }

  .duration-presets {
    flex-wrap: wrap;
  }

  .form-actions {
    flex-direction: column;
  }
}
</style>
