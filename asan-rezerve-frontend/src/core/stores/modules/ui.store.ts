import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Component } from 'vue'

export interface Modal {
  id: string
  isOpen: boolean
  component?: Component
  props?: Record<string, unknown>
}

export const useUiStore = defineStore('ui', () => {
  // State
  const isLoading = ref(false)
  const loadingMessage = ref('')
  const isSidebarOpen = ref(true)
  const modals = ref<Modal[]>([])
  const theme = ref<'light' | 'dark'>('light')

  // Actions
  function startLoading(message = 'Loading...'): void {
    isLoading.value = true
    loadingMessage.value = message
  }

  function stopLoading(): void {
    isLoading.value = false
    loadingMessage.value = ''
  }

  function toggleSidebar(): void {
    isSidebarOpen.value = !isSidebarOpen.value
  }

  function openSidebar(): void {
    isSidebarOpen.value = true
  }

  function closeSidebar(): void {
    isSidebarOpen.value = false
  }

  function openModal(id: string, component?: Component, props?: Record<string, unknown>): void {
    const existingModal = modals.value.find((m) => m.id === id)
    if (existingModal) {
      existingModal.isOpen = true
      existingModal.component = component
      existingModal.props = props
    } else {
      modals.value.push({ id, isOpen: true, component, props })
    }
  }

  function closeModal(id: string): void {
    const modal = modals.value.find((m) => m.id === id)
    if (modal) {
      modal.isOpen = false
    }
  }

  function closeAllModals(): void {
    modals.value.forEach((modal) => {
      modal.isOpen = false
    })
  }

  function toggleTheme(): void {
    theme.value = theme.value === 'light' ? 'dark' : 'light'
    localStorage.setItem('theme', theme.value)
    document.documentElement.setAttribute('data-theme', theme.value)
  }

  function initializeTheme(): void {
    const storedTheme = localStorage.getItem('theme') as 'light' | 'dark' | null
    if (storedTheme) {
      theme.value = storedTheme
    } else {
      // Check system preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      theme.value = prefersDark ? 'dark' : 'light'
    }
    document.documentElement.setAttribute('data-theme', theme.value)
  }

  // Initialize theme on store creation
  initializeTheme()

  return {
    // State
    isLoading,
    loadingMessage,
    isSidebarOpen,
    modals,
    theme,
    // Actions
    startLoading,
    stopLoading,
    toggleSidebar,
    openSidebar,
    closeSidebar,
    openModal,
    closeModal,
    closeAllModals,
    toggleTheme,
  }
})
