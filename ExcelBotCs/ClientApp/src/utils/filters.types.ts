export type Option<V = any> = { label: string; value: V }

export type FilterSelection = Record<string, unknown | unknown[]>

export type FilterDef<T> = {
    id: string
    label: string
    multiple?: boolean
    options?: Option[]
    predicate: (item: T, selected: unknown | unknown[]) => boolean
}