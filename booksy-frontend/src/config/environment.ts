export const environment = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'https://localhost:5000/api',
  appTitle: import.meta.env.VITE_APP_TITLE || 'Booksy',
  enableLogging: import.meta.env.VITE_ENABLE_LOGGING === 'true',
  isDevelopment: import.meta.env.MODE === 'development',
  isProduction: import.meta.env.MODE === 'production',
  mode: import.meta.env.MODE,
}

export default environment
