export type DropdownPosition =
  | 'top-left'
  | 'top-center'
  | 'top-right'
  | 'bottom-left'
  | 'bottom-center'
  | 'bottom-right'

export type DropdownWidth = 'auto' | 'trigger' | 'full' | number | string

export interface DropdownProps {
  modelValue?: boolean
  label?: string
  position?: DropdownPosition
  offset?: number
  closeOnSelect?: boolean
  disabled?: boolean
  width?: DropdownWidth
}

export interface DropdownEmits {
  (event: 'update:modelValue', value: boolean): void
  (event: 'open'): void
  (event: 'close'): void
}
