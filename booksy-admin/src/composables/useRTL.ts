/**
 * useRTL Composable - Provides RTL/LTR directional helpers
 */
import { computed, type CSSProperties } from 'vue'
import { useLocaleStore } from '../stores/locale.store'
import { Direction } from '../types/locale.types'

export function useRTL() {
  const localeStore = useLocaleStore()

  const direction = computed(() => localeStore.direction)
  const isRTL = computed(() => direction.value === Direction.RTL)

  /**
   * Get text alignment based on direction
   * @param position - 'start' or 'end'
   */
  function getTextAlign(position: 'start' | 'end' = 'start'): 'left' | 'right' {
    if (position === 'start') {
      return isRTL.value ? 'right' : 'left'
    }
    return isRTL.value ? 'left' : 'right'
  }

  /**
   * Get float direction based on position
   * @param position - 'start' or 'end'
   */
  function getFloat(position: 'start' | 'end'): 'left' | 'right' {
    if (position === 'start') {
      return isRTL.value ? 'right' : 'left'
    }
    return isRTL.value ? 'left' : 'right'
  }

  /**
   * Get margin-start CSS property
   */
  function getMarginStart(value: string): CSSProperties {
    return isRTL.value
      ? { marginRight: value }
      : { marginLeft: value }
  }

  /**
   * Get margin-end CSS property
   */
  function getMarginEnd(value: string): CSSProperties {
    return isRTL.value
      ? { marginLeft: value }
      : { marginRight: value }
  }

  /**
   * Get padding-start CSS property
   */
  function getPaddingStart(value: string): CSSProperties {
    return isRTL.value
      ? { paddingRight: value }
      : { paddingLeft: value }
  }

  /**
   * Get padding-end CSS property
   */
  function getPaddingEnd(value: string): CSSProperties {
    return isRTL.value
      ? { paddingLeft: value }
      : { paddingRight: value }
  }

  /**
   * Get border-start CSS property
   */
  function getBorderStart(value: string): CSSProperties {
    return isRTL.value
      ? { borderRight: value }
      : { borderLeft: value }
  }

  /**
   * Get border-end CSS property
   */
  function getBorderEnd(value: string): CSSProperties {
    return isRTL.value
      ? { borderLeft: value }
      : { borderRight: value }
  }

  /**
   * Get start position property (left/right)
   */
  function getPositionStart(value: string): CSSProperties {
    return isRTL.value
      ? { right: value }
      : { left: value }
  }

  /**
   * Get end position property (left/right)
   */
  function getPositionEnd(value: string): CSSProperties {
    return isRTL.value
      ? { left: value }
      : { right: value }
  }

  /**
   * Flip value for RTL (useful for transforms)
   * @param value - Numeric value to flip
   */
  function flipValue(value: number): number {
    return isRTL.value ? -value : value
  }

  return {
    direction,
    isRTL,
    getTextAlign,
    getFloat,
    getMarginStart,
    getMarginEnd,
    getPaddingStart,
    getPaddingEnd,
    getBorderStart,
    getBorderEnd,
    getPositionStart,
    getPositionEnd,
    flipValue,
  }
}
