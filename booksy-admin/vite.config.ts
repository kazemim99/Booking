import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue({
      template: {
        compilerOptions: {
          // Enable Vue DevTools in development
          isCustomElement: () => false,
        },
      },
    }),
    vueDevTools(),
  ],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    // allow importing the sibling packages/design-tokens (shared @booksy/tokens) in dev
    fs: { allow: ['..'] },
  },
  // Explicitly enable source maps for better debugging
  build: {
    sourcemap: true,
  },
})
