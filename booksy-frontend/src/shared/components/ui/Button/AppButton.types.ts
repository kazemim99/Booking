import type { Component } from 'vue'

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'danger'
  | 'warning'
  | 'info'
  | 'outline'
  | 'ghost'
  | 'link'

export type ButtonSize = 'small' | 'medium' | 'large'

export type ButtonType = 'button' | 'submit' | 'reset'

export type ButtonTag = 'button' | 'a' | 'router-link'

export interface ButtonProps {
  variant?: ButtonVariant
  size?: ButtonSize
  tag?: ButtonTag
  nativeType?: ButtonType
  icon?: Component | string
  iconPosition?: 'left' | 'right'
  loading?: boolean
  disabled?: boolean
  block?: boolean
  rounded?: boolean
  to?: string | object
  href?: string
}

export interface ButtonEmits {
  (event: 'click', e: MouseEvent): void
}
