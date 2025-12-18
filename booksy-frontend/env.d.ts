/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_USER_MANAGEMENT_API_URL: string
  readonly VITE_SERVICE_CATALOG_API_URL: string
  readonly VITE_APP_TITLE: string
  readonly VITE_ENABLE_LOGGING: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
