import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  testMatch: '**/*.spec.ts',
  timeout: 60000,
  retries: 0,
  use: {
    headless: true,
    viewport: { width: 1600, height: 900 },
    screenshot: 'on',
    video: 'on',
    trace: 'on',
  },
  reporter: [
    ['html', { open: 'never', outputFolder: 'test-results/html-report' }],
    ['list'],
  ],
  outputDir: 'test-results',
});
