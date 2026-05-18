/// <reference types="vite/client" />

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const component: DefineComponent<Record<string, never>, Record<string, never>, any>
  export default component
}

declare module 'vue-cal' {
  import type { DefineComponent } from 'vue'
  const VueCal: DefineComponent<any, any, any>
  export default VueCal
}
