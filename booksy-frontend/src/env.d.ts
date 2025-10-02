/// <reference types="vite/client" />

/**
 * Environment variable type definitions for Booksy Frontend
 * Provides type safety for import.meta.env variables
 */

interface ImportMetaEnv {
  // API Configuration
  readonly VITE_API_BASE_URL: string
  readonly VITE_API_TIMEOUT: string
  readonly VITE_API_VERSION: string
  
  // Application Configuration
  readonly VITE_APP_TITLE: string
  readonly VITE_APP_DESCRIPTION: string
  readonly VITE_APP_VERSION: string
  
  // Feature Flags
  readonly VITE_ENABLE_LOGGING: string
  readonly VITE_ENABLE_ANALYTICS: string
  readonly VITE_ENABLE_DEBUG_MODE: string
  readonly VITE_ENABLE_RTL: string
  
  // Authentication & Security
  readonly VITE_JWT_ISSUER: string
  readonly VITE_JWT_AUDIENCE: string
  readonly VITE_REFRESH_TOKEN_ENABLED: string
  readonly VITE_SESSION_TIMEOUT: string
  
  // External Services
  readonly VITE_GOOGLE_MAPS_API_KEY: string
  readonly VITE_STRIPE_PUBLIC_KEY: string
  readonly VITE_ANALYTICS_ID: string
  readonly VITE_SENTRY_DSN: string
  
  // Microservices Endpoints (if using API Gateway)
  readonly VITE_USER_MANAGEMENT_API: string
  readonly VITE_BOOKING_API: string
  readonly VITE_SERVICE_CATALOG_API: string
  readonly VITE_NOTIFICATIONS_API: string
  readonly VITE_REVIEWS_API: string
  
  // Localization
  readonly VITE_DEFAULT_LOCALE: string
  readonly VITE_SUPPORTED_LOCALES: string
  
  // Environment Metadata
  readonly MODE: 'development' | 'staging' | 'production' | 'test'
  readonly BASE_URL: string
  readonly PROD: boolean
  readonly DEV: boolean
  readonly SSR: boolean
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

// Extend window object for runtime environment access (if needed)
declare global {
  interface Window {
    /**
     * Runtime environment configuration
     * Can be populated by index.html or server-side rendering
     */
    __ENV__?: Partial<ImportMetaEnv>
  }
}

export {}