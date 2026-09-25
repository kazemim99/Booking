import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    port: 3000,
    host: 'localhost',
    // allow importing the sibling packages/design-tokens (shared @booksy/tokens) in dev
    fs: { allow: ['..'] },
    proxy: {
      // Proxy all /api requests to the Booksy monolith host (gateway retired)
      // NOTE: local dev runs the host on :5050 (:5000 is taken by the CoRide backend).
      '/api': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        secure: false,
        ws: true, // WebSocket support if needed
        configure: (proxy, _options) => {
          proxy.on('error', (err, _req, _res) => {
            console.log('proxy error', err);
          });
          proxy.on('proxyReq', (proxyReq, req, _res) => {
            console.log('Sending Request to Gateway:', req.method, req.url);
          });
          proxy.on('proxyRes', (proxyRes, req, _res) => {
            console.log('Received from Gateway:', proxyRes.statusCode, req.url);
          });
        },
      }
    }
  },
})
