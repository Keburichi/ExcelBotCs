import type { Ref } from 'vue'
import type { FilterDef, FilterSelection } from '@/utils/filters.types'
import { computed, ref } from 'vue'

export function useFilters<T>(
  items: Ref<T[]>,
  filters: FilterDef<T>[],
  initial?: FilterSelection,
) {
  const selected = ref<FilterSelection>({ ...(initial ?? {}) })

  const filtered = computed(() => {
    const arr = items.value ?? []

    const active = filters
      .map((f) => {
        const sel = selected.value[f.id]
        const empty
          = sel == null
            || (Array.isArray(sel) && sel.length === 0)
            || (sel as any) === ''
        return empty ? null : { def: f, sel }
      })
      .filter(Boolean) as { def: FilterDef<T>, sel: unknown | unknown[] }[]

    if (active.length === 0)
      return arr
    return arr.filter(item => active.every(f => f.def.predicate(item, f.sel)))
  })

  function clear() {
    for (const f of Object.keys(selected)) {
      delete (selected as any)[f]
    }
  }

  return { selected, filtered, clear }
}
