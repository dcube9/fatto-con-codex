import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  expect: { timeout: 20_000 },
  timeout: 90_000,
  testDir: './specs',
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  outputDir: 'test-results',
  use: {
    baseURL: 'http://127.0.0.1:4173',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project ../../src/ViteKlub.Web/ViteKlub.Web.csproj --no-launch-profile --urls http://127.0.0.1:4173',
    url: 'http://127.0.0.1:4173/login',
    reuseExistingServer: false,
    timeout: 120_000,
    stdout: 'pipe',
    stderr: 'pipe'
  }
});
