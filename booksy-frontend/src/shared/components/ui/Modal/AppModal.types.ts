export type ModalSize = 'small' | 'medium' | 'large' | 'full'

export interface ModalProps {
  modelValue: boolean
  title?: string
  size?: ModalSize
  showClose?: boolean
  closeOnOverlay?: boolean
  closeOnEsc?: boolean
  centered?: boolean
}

export interface ModalEmits {
  (event: 'update:modelValue', value: boolean): void
  (event: 'close'): void
}
