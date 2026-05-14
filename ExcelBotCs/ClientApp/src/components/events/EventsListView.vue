<script lang="ts" setup>
import { RRule } from 'rrule'
import { computed, onMounted, ref } from 'vue'
import VueCal from 'vue-cal'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import EventCard from '@/components/events/EventCard.vue'
import { useAuth } from '@/composables/useAuth'
import { useEvents } from '@/composables/useEvents'
import { useTheme } from '@/composables/useTheme'
import { eventTypeToString } from '@/features/events/events.types'
import { parseICalString } from '@/utils/ical'

const e = useEvents()
const { isAdmin, isMember, isDeveloper, user } = useAuth()
const router = useRouter()
const { isDark } = useTheme()

const activeView = ref<'month' | 'week'>('week')
const timeFrom = ref(8 * 60)
const timeTo = ref(24 * 60)

function goCreate() {
  router.push({ name: 'event-create' })
}

function goEdit(event: any) {
  router.push({ name: 'event-edit', params: { id: event.id || event.Id } })
}

function goArchived() {
  router.push({ name: 'events-archived' })
}

function handleEventArchived(event: any) {
  e.events.value = e.events.value.filter(ev => ev.Id !== event.Id)
}

const calendarUrl = computed(() => {
  const discordId = user.value?.DiscordId
  if (!discordId) return ''
  const origin = typeof window !== 'undefined' ? window.location.origin : ''
  return `${origin}/api/Events/retrieve/${discordId}.ics`
})

function subscribeCalendar() {
  if (!calendarUrl.value) return
  window.open(calendarUrl.value, '_blank')
}

const freqMap = {
  DAILY: RRule.DAILY,
  WEEKLY: RRule.WEEKLY,
  MONTHLY: RRule.MONTHLY,
  YEARLY: RRule.YEARLY,
}

const calendarEvents = computed(() => {
  if (!e.events.value) return []
  const events: any[] = []
  e.events.value.forEach((event) => {
    if (event.ICalString && event.ICalString.includes('RRULE:')) {
      const config = parseICalString(event.ICalString)
      const dtstartMatch = event.ICalString.match(/DTSTART:(\d{8}T\d{6}Z)/)

      if (config && dtstartMatch) {
        const dtstartString = dtstartMatch[1]
        const year = Number.parseInt(dtstartString.substring(0, 4))
        const month = Number.parseInt(dtstartString.substring(4, 6)) - 1
        const day = Number.parseInt(dtstartString.substring(6, 8))
        const hour = Number.parseInt(dtstartString.substring(9, 11))
        const minute = Number.parseInt(dtstartString.substring(11, 13))
        const second = Number.parseInt(dtstartString.substring(13, 15))
        const dtstart = new Date(Date.UTC(year, month, day, hour, minute, second))

        try {
          const options = {
            freq: freqMap[config.frequency],
            dtstart,
            interval: config.interval,
            count: config.count,
            until: config.until,
            byweekday: config.byWeekday,
            bymonthday: config.byMonthDay,
          }

          const cleanedOptions = Object.fromEntries(Object.entries(options).filter(([, v]) => v != null))
          const rule = new RRule(cleanedOptions)
          const rangeStart = new Date(new Date().getFullYear(), 0, 1)
          const rangeEnd = new Date(new Date().getFullYear() + 1, 0, 1)
          const occurrences = rule.between(rangeStart, rangeEnd, true)

          occurrences.forEach((occurrence) => {
            events.push({
              id: event.Id,
              start: occurrence,
              end: new Date(occurrence.getTime() + event.Duration * 60000),
              title: event.Name,
              class: `event-type--${eventTypeToString(event.Type).toLowerCase()}`,
              background: true,
            })
          })
        }
        catch (err) {
          console.error('Could not parse rrule string', err)
        }
      }
    }
    else {
      events.push({
        id: event.Id,
        start: new Date(event.StartDate),
        end: new Date(new Date(event.StartDate).getTime() + event.Duration * 60000),
        title: event.Name,
        class: `event-type--${eventTypeToString(event.Type).toLowerCase()}`,
        background: true,
      })
    }
  })
  return events
})

function onViewChange({ view, startDate, endDate }: { view: string, startDate: Date, endDate: Date }) {
  if (view === 'week') {
    const eventsInView = calendarEvents.value.filter((event) => {
      const eventStart = new Date(event.start)
      return eventStart >= startDate && eventStart <= endDate
    })

    if (eventsInView.length > 0) {
      const startMinutes = eventsInView.map((event) => {
        const d = new Date(event.start)
        return d.getHours() * 60 + d.getMinutes()
      })

      const endMinutes = eventsInView.map((event) => {
        const d = new Date(event.end)
        return d.getHours() * 60 + d.getMinutes()
      })

      const earliestStartMin = Math.min(...startMinutes)
      const latestEndMin = Math.max(...endMinutes)

      timeFrom.value = Math.max(0, earliestStartMin - 60)
      timeTo.value = Math.min(24 * 60, latestEndMin + 60)
    }
    else {
      timeFrom.value = 8 * 60
      timeTo.value = 24 * 60
    }
  }
}

onMounted(e.load)
</script>

<template>
  <section>
    <p v-if="e.error" class="error">
      {{ e.error }}
    </p>

    <!-- Calendar (developer-only) -->
    <template v-if="isDeveloper">
      <div class="calendar-header">
        <h3 class="calendar-header__title">
          Calendar
        </h3>
        <div class="calendar-header__controls">
          <BaseButton
            :state="activeView === 'month' ? 'primary' : 'secondary'" title="Month"
            @clicked="activeView = 'month'"
          />
          <BaseButton
            :state="activeView === 'week' ? 'primary' : 'secondary'" title="Week"
            @clicked="activeView = 'week'"
          />
        </div>
      </div>

      <div class="calendar-container" style="height: 600px">
        <VueCal
          :active-view="activeView"
          :dark="isDark"
          :disable-views="['years', 'year', 'day']"
          :events="calendarEvents"
          :events-on-month-view="true"
          :time-from="timeFrom"
          :time-step="60"
          :time-to="timeTo"
          class="vuecal--full-height-delete"
          hide-view-selector
          @event-click="goEdit"
          @view-change="onViewChange"
        />
      </div>
    </template>

    <div class="events-toolbar">
      <div class="events-toolbar__actions">
        <BaseButton
          v-if="isMember"
          :disabled="!calendarUrl"
          :tooltip="calendarUrl ? 'Download or copy URL to subscribe in your calendar app' : 'Sign in first'"
          state="pressed"
          title="Subscribe to Calendar"
          variant="outlined"
          size="small"
          @clicked="subscribeCalendar"
        />
        <BaseButton
          v-if="isAdmin"
          size="small"
          state="secondary"
          title="View Archive"
          tooltip="View archived events"
          @clicked="goArchived"
        />
        <BaseButton
          v-if="isAdmin"
          size="small"
          state="primary"
          title="Create Event"
          @clicked="goCreate"
        />
      </div>
    </div>

    <div v-if="!e.events.value.length && !e.error" class="events-empty muted">
      No upcoming events scheduled.
    </div>

    <div class="events-grid">
      <EventCard
        v-for="(item, idx) in e.events.value"
        :key="item.Id"
        v-model:fc-event="e.events.value[idx]"
        :is-admin="isAdmin?.valueOf()"
        :is-developer="isDeveloper?.valueOf()"
        :is-member="isMember?.valueOf()"
        @archived="handleEventArchived"
        @start-edit="goEdit"
        @cancel-edit="e.cancelEdit"
        @save-edit="e.save"
        @delete-event="e.deleteEvent"
      />
    </div>
  </section>
</template>

<style scoped>
.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.calendar-header__title {
  font-size: 1.125rem;
  font-weight: 600;
}

.calendar-header__controls {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.calendar-container {
  margin-bottom: 2rem;
}

.events-toolbar {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  margin-bottom: 1rem;
}

.events-toolbar__actions {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.events-empty {
  text-align: center;
  padding: 3rem 1rem;
}

.events-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(420px, 1fr));
  gap: 1rem;
}

@media (max-width: 520px) {
  .events-grid {
    grid-template-columns: 1fr;
  }

  .events-toolbar {
    justify-content: flex-start;
  }

  .events-toolbar__actions {
    flex-wrap: wrap;
  }
}
</style>

<!-- Unscoped: calendar event type colors must reach into VueCal -->
<style>
.vuecal__event { cursor: pointer; }
.event-type--raid { background-color: var(--cat-blue-bg); border: 1px solid var(--cat-blue-fg); }
.event-type--social { background-color: var(--cat-purple-bg); border: 1px solid var(--cat-purple-fg); }
.event-type--farming { background-color: var(--cat-green-bg); border: 1px solid var(--cat-green-fg); }
.event-type--maps { background-color: var(--cat-orange-bg); border: 1px solid var(--cat-orange-fg); }
.event-type--blu { background-color: var(--cat-teal-bg); border: 1px solid var(--cat-teal-fg); }
.event-type--academy { background-color: var(--cat-rose-bg); border: 1px solid var(--cat-rose-fg); }
.event-type--minilvl { background-color: var(--cat-amber-bg); border: 1px solid var(--cat-amber-fg); }
.event-type--downsynced { background-color: var(--cat-indigo-bg); border: 1px solid var(--cat-indigo-fg); }
.event-type--other { background-color: var(--cat-slate-bg); border: 1px solid var(--cat-slate-fg); }
.vuecal--dark .event-type--raid { background-color: var(--cat-blue-fg); }
.vuecal--dark .event-type--social { background-color: var(--cat-purple-fg); }
.vuecal--dark .event-type--farming { background-color: var(--cat-green-fg); }
.vuecal--dark .event-type--maps { background-color: var(--cat-orange-fg); }
.vuecal--dark .event-type--blu { background-color: var(--cat-teal-fg); }
.vuecal--dark .event-type--academy { background-color: var(--cat-rose-fg); }
.vuecal--dark .event-type--minilvl { background-color: var(--cat-amber-fg); }
.vuecal--dark .event-type--downsynced { background-color: var(--cat-indigo-fg); }
.vuecal--dark .event-type--other { background-color: var(--cat-slate-fg); }
</style>
