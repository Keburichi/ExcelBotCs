<script lang="ts" setup>
import { computed, onMounted, ref, watch } from 'vue'

export interface WheelSegment {
  label: string
  color: string
}

const props = withDefaults(defineProps<{
  segments: WheelSegment[]
  targetIndex: number
  spinning: boolean
  duration?: number
}>(), {
  duration: 4000,
})

const emit = defineEmits<{
  (e: 'spinComplete'): void
}>()

const canvasRef = ref<HTMLCanvasElement | null>(null)
const wheelRef = ref<HTMLDivElement | null>(null)
const currentRotation = ref(0)
const isAnimating = ref(false)
const prefersReducedMotion = ref(false)

const segmentAngle = computed(() => 360 / props.segments.length)

function drawWheel() {
  const canvas = canvasRef.value
  if (!canvas || props.segments.length === 0) return

  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const size = canvas.width
  const center = size / 2
  const radius = center - 4

  ctx.clearRect(0, 0, size, size)

  const anglePerSegment = (2 * Math.PI) / props.segments.length

  props.segments.forEach((segment, i) => {
    const startAngle = i * anglePerSegment - Math.PI / 2
    const endAngle = startAngle + anglePerSegment

    // Draw segment
    ctx.beginPath()
    ctx.moveTo(center, center)
    ctx.arc(center, center, radius, startAngle, endAngle)
    ctx.closePath()
    ctx.fillStyle = segment.color
    ctx.fill()

    // Draw border between segments
    ctx.strokeStyle = 'rgba(0, 0, 0, 0.15)'
    ctx.lineWidth = 1
    ctx.stroke()

    // Draw label
    ctx.save()
    ctx.translate(center, center)
    const midAngle = startAngle + anglePerSegment / 2
    ctx.rotate(midAngle)

    ctx.fillStyle = '#fff'
    ctx.font = `bold ${Math.max(10, Math.min(14, 280 / props.segments.length))}px system-ui, sans-serif`
    ctx.textAlign = 'center'
    ctx.textBaseline = 'middle'

    const labelRadius = radius * 0.65
    const label = truncateLabel(segment.label, props.segments.length)
    ctx.fillText(label, labelRadius, 0)
    ctx.restore()
  })

  // Draw center hub
  ctx.beginPath()
  ctx.arc(center, center, 16, 0, 2 * Math.PI)
  ctx.fillStyle = '#1a1a2e'
  ctx.fill()
  ctx.strokeStyle = '#fff'
  ctx.lineWidth = 2
  ctx.stroke()
}

function truncateLabel(label: string, segmentCount: number): string {
  const maxLen = segmentCount > 20 ? 6 : segmentCount > 10 ? 10 : 16
  return label.length > maxLen ? `${label.slice(0, maxLen - 1)}…` : label
}

function spin() {
  if (props.segments.length === 0) return

  if (prefersReducedMotion.value) {
    emit('spinComplete')
    return
  }

  isAnimating.value = true

  // Calculate target angle: the pointer is at the top (0deg / 12 o'clock).
  // We rotate the wheel clockwise. To land on targetIndex, we need the
  // center of that segment to align with the top.
  const targetSegmentCenter = props.targetIndex * segmentAngle.value + segmentAngle.value / 2
  // Rotate several full turns plus offset to land on target
  const fullRotations = 5 + Math.floor(Math.random() * 3)
  const targetRotation = fullRotations * 360 + (360 - targetSegmentCenter)

  currentRotation.value = targetRotation
}

function onTransitionEnd() {
  if (isAnimating.value) {
    isAnimating.value = false
    emit('spinComplete')
  }
}

watch(() => props.spinning, (spinning) => {
  if (spinning) {
    spin()
  }
})

watch(() => props.segments, () => {
  drawWheel()
}, { deep: true })

onMounted(() => {
  prefersReducedMotion.value = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  drawWheel()
})
</script>

<template>
  <div class="wheel-container">
    <!-- Pointer -->
    <div class="wheel-pointer">
      <svg width="24" height="32" viewBox="0 0 24 32" fill="none">
        <path d="M12 32L2 4L12 8L22 4L12 32Z" fill="var(--wheel-pointer, #e63946)" />
      </svg>
    </div>

    <!-- Spinning wrapper -->
    <div
      ref="wheelRef"
      class="wheel-spinner"
      :class="{ 'wheel-spinner--animating': isAnimating }"
      :style="{
        transform: `rotate(${currentRotation}deg)`,
        transitionDuration: `${duration}ms`,
      }"
      @transitionend="onTransitionEnd"
    >
      <canvas
        ref="canvasRef"
        width="320"
        height="320"
        class="wheel-canvas"
      />
    </div>
  </div>
</template>

<style scoped>
.wheel-container {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 320px;
  height: 320px;
  margin: 0 auto;
}

.wheel-pointer {
  position: absolute;
  top: -4px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 2;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
}

.wheel-spinner {
  width: 320px;
  height: 320px;
  border-radius: 50%;
  box-shadow:
    0 0 0 4px var(--border),
    0 4px 20px rgba(0, 0, 0, 0.2);
  transition-property: transform;
  transition-timing-function: cubic-bezier(0.17, 0.67, 0.12, 0.99);
}

.wheel-spinner--animating {
  transition-property: transform;
}

.wheel-canvas {
  display: block;
  width: 100%;
  height: 100%;
  border-radius: 50%;
}

@media (prefers-reduced-motion: reduce) {
  .wheel-spinner {
    transition: none !important;
  }
}
</style>
