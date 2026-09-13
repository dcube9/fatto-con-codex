import { test, expect } from '@playwright/test';

const viewer = 'f536acbd-0a9d-51fe-8446-6d76838ce13a';
const accessId = '58a3f95e-2d5e-52ef-9b48-0d4ddb06c0d7';

test.use({ timezoneId: 'America/New_York' });

test('lista e dettaglio usano Europe/Rome invece del fuso browser', async ({ page }) => {
  await page.addInitScript(id => localStorage.setItem('viteklub.demo.current-user', id), viewer);
  await page.goto('/accesses');
  const row = page.getByRole('row').filter({ has: page.getByRole('link', { name: 'Visualizza' }) })
    .filter({ hasText: '31/08/2026 23:00 (UTC+02:00)' }).first();
  await expect(row).toBeVisible();
  await expect(row).not.toContainText('21:00 UTC');
  await row.getByRole('link', { name: 'Visualizza' }).click();
  await expect(page).toHaveURL(new RegExp(`/accesses/${accessId}$`));
  await expect(page.getByText('31/08/2026 23:00 (UTC+02:00)', { exact: true })).toBeVisible();
  await expect(page.getByText(/mostrate in UTC/)).toHaveCount(0);
});
