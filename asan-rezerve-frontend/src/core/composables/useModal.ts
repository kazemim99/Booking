import { useUiStore } from '@/core/stores/modules/ui.store'
import type { Component } from 'vue'

export function useModal() {
  const uiStore = useUiStore()

  function open(id: string, component?: Component, props?: Record<string, unknown>): void {
    uiStore.openModal(id, component, props)
  }

  function close(id: string): void {
    uiStore.closeModal(id)
  }

  function closeAll(): void {
    uiStore.closeAllModals()
  }

  return {
    open,
    close,
    closeAll,
  }
}
