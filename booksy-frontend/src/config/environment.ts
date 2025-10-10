export const environment = {
  userManagementApiUrl: import.meta.env.VITE_USER_MANAGEMENT_API_URL || 'https://localhost:5000/api',
  serviceCategoryApiUrl: import.meta.env.VITE_SERVICE_CATEGORY_API_URL || 'https://localhost:5001/api',
  appTitle: import.meta.env.VITE_APP_TITLE || 'Booksy',
  enableLogging: import.meta.env.VITE_ENABLE_LOGGING === 'true',
  isDevelopment: import.meta.env.MODE === 'development',
  isProduction: import.meta.env.MODE === 'production',
  mode: import.meta.env.MODE,
}

export default environment
