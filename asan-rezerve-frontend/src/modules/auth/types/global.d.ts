// src/types/global.d.ts

// Augment HTMLElement with custom properties
declare global {
  interface HTMLElement {
    clickOutsideEvent?: (event: Event) => void
  }
}

// Augment Vue Router meta
declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    isPublic?: boolean
    roles?: string[]
    title?: string
    showSidebar?: boolean
    transition?: string
    showBreadcrumb?: boolean
    showFooter?: boolean
    breadcrumbs?: Array<{ label: string; path: string }>
  }
}

// Augment Window interface for custom properties
declare global {
  interface Window {
    fs?: {
      readFile: (path: string, options?: { encoding?: string }) => Promise<Uint8Array | string>
    }
  }
}

export { }
