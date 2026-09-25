import { fileURLToPath } from 'node:url'
import { mergeConfig, defineConfig, configDefaults } from 'vitest/config'
import viteConfig from './vite.config'

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      name: 'integration',
      environment: 'jsdom',
      exclude: [...configDefaults.exclude, 'e2e/**'],
      include: ['tests/integration/**/*.spec.ts'],
      root: fileURLToPath(new URL('./', import.meta.url)),
      testTimeout: 30000, // 30 seconds for API calls
      hookTimeout: 10000, // 10 seconds for setup/teardown
      globals: true,
      setupFiles: ['./tests/integration/setup.ts'],
      coverage: {
        provider: 'v8',
        reporter: ['text', 'json', 'html'],
        include: ['src/modules/**/services/**/*.ts'],
        exclude: [
          'node_modules/**',
          'tests/**',
          '**/*.spec.ts',
          '**/*.d.ts',
        ],
      },
    },
  })
)
