import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://127.0.0.1:64563';
const qaOfferDirectory = path.resolve('.playwright/offer-letters');
const localDb = String.raw`Server=(localdb)\MSSQLLocalDB;Database=RecruitmentTracker_Playwright;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`;

export default defineConfig({
  testDir: './qa/tests',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  reporter: [
    ['list'],
    ['html', { open: 'never' }],
  ],
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{
    name: 'chromium',
    use: {
      ...devices['Desktop Chrome'],
      ...(process.env.PLAYWRIGHT_USE_EDGE === '1' ? { channel: 'msedge' } : {}),
    },
  }],
  webServer: process.env.PLAYWRIGHT_EXTERNAL_SERVER === '1' ? undefined : {
    command: `dotnet run --no-launch-profile --urls ${baseURL}`,
    url: `${baseURL}/Account/Login`,
    timeout: 240_000,
    reuseExistingServer: false,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__DefaultConnection: process.env.PLAYWRIGHT_DB_CONNECTION ?? localDb,
      OFFER_LETTER_DIRECTORY: qaOfferDirectory,
    },
  },
});
