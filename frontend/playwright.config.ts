import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL: process.env.PLAYWRIGHT_TEST_BASE_URL || 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    actionTimeout: 10000,
    contextOptions: {
      logger: {
        isEnabled: (name, severity) => true,
        log: (name, severity, message, args) => console.log(`${name} ${message}`)
      }
    },
    extraHTTPHeaders: {
      'Remote-User': 'admin',
      'Remote-Groups': 'full_admin,house_member',
      'Remote-Name': 'Administrator',
      'Remote-User-Sid': 'S-1-5-32-544'
    }
  },
  webServer: {
    command: 'npx vite --port 5173',
    port: 5173,
    reuseExistingServer: !process.env.CI,
    timeout: 30000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
