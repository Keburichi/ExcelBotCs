<script lang="ts" setup>
import { RRule } from 'rrule'
import { computed, onMounted, ref } from 'vue'
import VueCal from 'vue-cal'
import { useRouter } from 'vue-router'
import BaseButton from '@/components/BaseButton.vue'
import CardList from '@/components/CardList.vue'
import EventCard from '@/components/events/EventCard.vue'
import { useAuth } from '@/composables/useAuth'
import { useEvents } from '@/composables/useEvents'
import { useTheme } from '@/composables/useTheme'
import { eventTypeToString } from '@/features/events/events.types'
import { parseICalString } from '@/utils/ical'

const e = useEvents()
const { isAdmin, isMember, isDeveloper } = useAuth()
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

const freqMap = {
  DAILY: RRule.DAILY,
  WEEKLY: RRule.WEEKLY,
  MONTHLY: RRule.MONTHLY,
  YEARLY: RRule.YEARLY,
}

const calendarEvents = computed(() => {
  if (!e.events.value)
    return []
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
          // Compute occurrences within the current year using `between` (all() doesn't accept range args)
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
        catch (e) {
          console.error('Could not parse rrule string', e)
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
      // Use minute precision for tighter vertical bounds.
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

      // Start one hour earlier than the earliest event, clamp to 00:00.
      timeFrom.value = Math.max(0, earliestStartMin - 60)
      // Keep an extra hour after last event for readability, clamp to 24:00.
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
  <section class="home">
    <template v-if="isDeveloper">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-2xl font-bold">
          Upcoming FC Events
        </h2>
        <div class="flex gap-2">
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

      <div class="mb-8" style="height: 600px">
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
    <template v-else>
      <div class="mb-8 p-6 rounded-lg border border-dashed border-gray-400 text-gray-600 dark:text-gray-300">
        <h3 class="text-xl font-semibold mb-2">
          Calendar coming soon
        </h3>
        <p>We’re working on the calendar experience. It’s currently visible only to developers.</p>
      </div>
    </template>

    <h2>Events List ({{ e.events.value.length }})</h2>
    <p v-if="e.error" class="error">
      {{ e.error }}
    </p>

    <div v-if="isAdmin" class="container">
      <BaseButton v-if="isAdmin" size="medium" state="primary" title="Create Event" @clicked="goCreate" />
    </div>

    <CardList :columns="2" :items="e.events.value" item-key="Id">
      <template #item="{ item }">
        <EventCard
          :fc-event="item"
          :is-admin="isAdmin?.valueOf()"
          :is-member="isMember?.valueOf()"
          @start-edit="goEdit"
          @cancel-edit="e.cancelEdit"
          @save-edit="e.save"
          @delete-event="e.deleteEvent"
        />
      </template>
    </CardList>
  </section>
</template>

<style>
.vuecal__event {
  cursor: pointer;
}

.event-type--raid {
  background-color: #e3f2fd;
  border: 1px solid #1565c0;
}

.event-type--social {
  background-color: #f3e5f5;
  border: 1px solid #7b1fa2;
}

.event-type--farming {
  background-color: #e8f5e9;
  border: 1px solid #2e7d32;
}

.event-type--maps {
  background-color: #fff3e0;
  border: 1px solid #e65100;
}

.event-type--blu {
  background-color: #e0f2f1;
  border: 1px solid #00695c;
}

.event-type--academy {
  background-color: #fce4ec;
  border: 1px solid #c2185b;
}

.event-type--minilvl {
  background-color: #fff9c4;
  border: 1px solid #f57f17;
}

.event-type--downsynced {
  background-color: #ede7f6;
  border: 1px solid #4527a0;
}

.event-type--other {
  background-color: #eceff1;
  border: 1px solid #455a64;
}

.vuecal--dark .event-type--raid {
  background-color: #1565c0;
}

.vuecal--dark .event-type--social {
  background-color: #7b1fa2;
}

.vuecal--dark .event-type--farming {
  background-color: #2e7d32;
}

.vuecal--dark .event-type--maps {
  background-color: #e65100;
}

.vuecal--dark .event-type--blu {
  background-color: #00695c;
}

.vuecal--dark .event-type--academy {
  background-color: #c2185b;
}

.vuecal--dark .event-type--minilvl {
  background-color: #f57f17;
}

.vuecal--dark .event-type--downsynced {
  background-color: #4527a0;
}

.vuecal--dark .event-type--other {
  background-color: #455a64;
}
</style>
