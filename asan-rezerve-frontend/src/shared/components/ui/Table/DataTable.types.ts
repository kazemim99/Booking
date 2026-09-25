// src/shared/components/ui/Table/DataTable.types.ts
export interface TableColumn<T = unknown> {
  key: string
  label: string
  sortable?: boolean
  width?: string
  align?: 'left' | 'center' | 'right'
  formatter?: (value: unknown, row: T) => string | number
  component?: unknown // Vue component for custom rendering
  hidden?: boolean
}

export interface TableAction<T = unknown> {
  label: string
  icon?: string
  variant?: 'primary' | 'secondary' | 'danger' | 'success'
  onClick: (row: T) => void
  visible?: (row: T) => boolean
  disabled?: (row: T) => boolean
}

export interface TableProps<T = unknown> {
  columns: TableColumn<T>[]
  data: T[]
  loading?: boolean
  sortable?: boolean
  selectable?: boolean
  actions?: TableAction<T>[]
  emptyMessage?: string
  rowKey?: string
  striped?: boolean
  hoverable?: boolean
  bordered?: boolean
}

export interface SortState {
  column: string | null
  direction: 'asc' | 'desc' | null
}
