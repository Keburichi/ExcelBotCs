<script setup lang="ts">
import type { ChartConfiguration } from 'chart.js'
import type { Member } from '@/features/members/members.types'
import { Chart, registerables } from 'chart.js'
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { FightType } from '@/features/fights/fights.types'

const props = defineProps<{
  members: Member[]
}>()

// Register Chart.js components
Chart.register(...registerables)

const subscriptionChartCanvas = ref<HTMLCanvasElement>()
const contentChartCanvas = ref<HTMLCanvasElement>()
let subscriptionChart: Chart | null = null
let contentChart: Chart | null = null

// Calculate subscription statistics
const subscriptionStats = computed(() => {
  if (!props.members || props.members.length === 0) {
    return { subscribed: 0, notSubscribed: 0 }
  }
  const subscribed = props.members.filter(m => m?.Subbed === true).length
  const notSubscribed = props.members.length - subscribed
  return { subscribed, notSubscribed }
})

// Calculate content cleared statistics
const contentStats = computed(() => {
  const stats = {
    Extreme: new Set<string>(),
    Savage: new Set<string>(),
    LegacySavage: new Set<string>(),
    Ultimate: new Set<string>(),
    Chaotic: new Set<string>(),
  }

  if (!props.members || props.members.length === 0) {
    return {
      Extreme: 0,
      Savage: 0,
      LegacySavage: 0,
      Ultimate: 0,
      Chaotic: 0,
    }
  }

  props.members.forEach((member) => {
    if (member?.Experience && Array.isArray(member.Experience) && member.Experience.length > 0) {
      member.Experience.forEach((fight) => {
        if (fight && typeof fight.Type !== 'undefined') {
          const typeKey = FightType[fight.Type] as keyof typeof stats
          if (stats[typeKey] && member.Id) {
            stats[typeKey].add(member.Id)
          }
        }
      })
    }
  })

  return {
    Extreme: stats.Extreme.size,
    Savage: stats.Savage.size,
    LegacySavage: stats.LegacySavage.size,
    Ultimate: stats.Ultimate.size,
    Chaotic: stats.Chaotic.size,
  }
})

function cssVar(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim()
}

function initCharts() {
  const fg = cssVar('--fg')

  // Subscription Chart
  if (subscriptionChartCanvas.value) {
    const subCtx = subscriptionChartCanvas.value.getContext('2d')
    if (subCtx) {
      const subConfig: ChartConfiguration = {
        type: 'pie',
        data: {
          labels: ['Subscribed', 'Not Subscribed'],
          datasets: [{
            data: [subscriptionStats.value.subscribed, subscriptionStats.value.notSubscribed],
            backgroundColor: [cssVar('--exp-extreme'), cssVar('--exp-savage')],
            borderColor: [cssVar('--exp-extreme-border'), cssVar('--exp-savage-border')],
            borderWidth: 2,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              position: 'bottom',
              labels: {
                color: fg,
                padding: 15,
                font: {
                  size: 12,
                },
              },
            },
            title: {
              display: true,
              text: 'Subscription Status',
              color: fg,
              font: {
                size: 16,
                weight: 'bold',
              },
              padding: {
                top: 10,
                bottom: 20,
              },
            },
            tooltip: {
              callbacks: {
                label: (context) => {
                  const label = context.label || ''
                  const value = context.parsed || 0
                  const total = subscriptionStats.value.subscribed + subscriptionStats.value.notSubscribed
                  const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0'
                  return `${label}: ${value} (${percentage}%)`
                },
              },
            },
          },
        },
      }
      subscriptionChart = new Chart(subCtx, subConfig)
    }
  }

  // Content Cleared Chart
  if (contentChartCanvas.value) {
    const contentCtx = contentChartCanvas.value.getContext('2d')
    if (contentCtx) {
      const contentConfig: ChartConfiguration = {
        type: 'pie',
        data: {
          labels: ['Extreme', 'Savage', 'Legacy Savage', 'Ultimate', 'Chaotic'],
          datasets: [{
            data: [
              contentStats.value.Extreme,
              contentStats.value.Savage,
              contentStats.value.LegacySavage,
              contentStats.value.Ultimate,
              contentStats.value.Chaotic,
            ],
            backgroundColor: [
              cssVar('--exp-extreme'),
              cssVar('--exp-savage'),
              cssVar('--exp-legacy-savage'),
              cssVar('--exp-chaotic'),
              cssVar('--exp-chaotic-border'),
            ],
            borderColor: [
              cssVar('--exp-extreme-border'),
              cssVar('--exp-savage-border'),
              cssVar('--exp-legacy-savage-border'),
              cssVar('--exp-chaotic-border'),
              cssVar('--exp-chaotic'),
            ],
            borderWidth: 2,
          }],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              position: 'bottom',
              labels: {
                color: fg,
                padding: 15,
                font: {
                  size: 12,
                },
              },
            },
            title: {
              display: true,
              text: 'Members with Cleared Content by Type',
              color: fg,
              font: {
                size: 16,
                weight: 'bold',
              },
              padding: {
                top: 10,
                bottom: 20,
              },
            },
            tooltip: {
              callbacks: {
                label: (context) => {
                  const label = context.label || ''
                  const value = context.parsed || 0
                  const total = props.members.length
                  const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : '0.0'
                  return `${label}: ${value} members (${percentage}%)`
                },
              },
            },
          },
        },
      }
      contentChart = new Chart(contentCtx, contentConfig)
    }
  }
}

function updateCharts() {
  // Update subscription chart
  if (subscriptionChart && subscriptionChart.data.datasets[0]) {
    subscriptionChart.data.datasets[0].data = [
      subscriptionStats.value.subscribed,
      subscriptionStats.value.notSubscribed,
    ]
    subscriptionChart.update()
  }

  // Update content chart
  if (contentChart && contentChart.data.datasets[0]) {
    contentChart.data.datasets[0].data = [
      contentStats.value.Extreme,
      contentStats.value.Savage,
      contentStats.value.LegacySavage,
      contentStats.value.Ultimate,
      contentStats.value.Chaotic,
    ]
    contentChart.update()
  }
}

onMounted(async () => {
  await nextTick()
  initCharts()
})

onUnmounted(() => {
  if (subscriptionChart) {
    subscriptionChart.destroy()
  }
  if (contentChart) {
    contentChart.destroy()
  }
})

// Watch for data changes and update charts
watch(() => props.members, () => {
  updateCharts()
}, { deep: true })
</script>

<template>
  <div class="statistics-container">
    <div class="chart-card">
      <canvas ref="subscriptionChartCanvas" />
    </div>
    <div class="chart-card">
      <canvas ref="contentChartCanvas" />
    </div>
  </div>
</template>

<style scoped>
.statistics-container {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.chart-card {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(20px);
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 16px;
  padding: 1.5rem;
  height: 400px;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08),
  inset 0 1px 0 rgba(255, 255, 255, 0.5);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

:root[data-theme='dark'] .chart-card {
  background: rgba(18, 26, 45, 0.7);
  border: 2px solid rgba(255, 255, 255, 0.15);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.08);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .chart-card {
    background: rgba(18, 26, 45, 0.7);
    border: 2px solid rgba(255, 255, 255, 0.15);
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3),
    inset 0 1px 0 rgba(255, 255, 255, 0.08);
  }
}

.chart-card:hover {
  backdrop-filter: blur(24px);
  border-color: rgba(59, 130, 246, 0.4);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.15),
  0 4px 16px rgba(0, 0, 0, 0.1),
  inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

:root[data-theme='dark'] .chart-card:hover {
  border-color: rgba(59, 130, 246, 0.5);
  box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
  0 4px 16px rgba(0, 0, 0, 0.4),
  inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .chart-card:hover {
    border-color: rgba(59, 130, 246, 0.5);
    box-shadow: 0 8px 32px rgba(59, 130, 246, 0.25),
    0 4px 16px rgba(0, 0, 0, 0.4),
    inset 0 1px 0 rgba(255, 255, 255, 0.12);
  }
}

.chart-card canvas {
  flex: 1;
}
</style>
