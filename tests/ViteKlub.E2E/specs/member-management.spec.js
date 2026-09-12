import { test, expect } from '@playwright/test';

const users = {
  administrator: '1e716ceb-f355-57af-a13e-25012c245a95',
  manager: 'fcdcfdf5-5443-5370-bac6-c9210995310a',
  receptionist: '957b5aa3-3100-59ac-bcfe-ae518c8cb6da',
  viewer: 'f536acbd-0a9d-51fe-8446-6d76838ce13a'
};
const memberId = '696534be-0212-5a49-8393-a7ec2c84f854';

async function authenticate(page, role) {
  await page.addInitScript(({ id }) => localStorage.setItem('viteklub.demo.current-user', id), { id: users[role] });
}

test('le route anonime preservano la destinazione dopo il login', async ({ page }) => {
  await page.goto(`/members/${memberId}`);
  await expect(page).toHaveURL(new RegExp(`/login\\?returnUrl=.*members`));
  await expect(page.getByRole('heading', { name: 'Accesso dimostrativo' })).toBeVisible();
  await page.getByRole('button', { name: 'Entra nella demo' }).click();
  await expect(page).toHaveURL(new RegExp(`/members/${memberId}$`));
});

for (const role of ['administrator', 'manager', 'receptionist']) {
  test(`${role} accede alle route operative renderizzate`, async ({ page }) => {
    await authenticate(page, role);
    await page.goto('/members');
    await expect(page.getByRole('heading', { name: 'Iscritti', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Nuovo iscritto' })).toBeVisible();
    await page.goto(`/members/${memberId}`);
    await expect(page.getByLabel('Modifica Alessia Bianchi')).toBeVisible();
    await expect(page.getByRole('button', { name: /Sospendi Alessia Bianchi/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Archivia Alessia Bianchi/ })).toBeVisible();
    await page.goto('/members/new');
    await expect(page.getByRole('heading', { name: 'Nuovo iscritto' })).toBeVisible();
    await page.goto(`/members/${memberId}/edit`);
    await expect(page.getByRole('heading', { name: 'Modifica iscritto' })).toBeVisible();
  });
}

test('Viewer consulta gli iscritti ma non accede alle operazioni', async ({ page }) => {
  await authenticate(page, 'viewer');
  await page.goto('/members');
  await expect(page.getByRole('heading', { name: 'Iscritti', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Nuovo iscritto' })).toHaveCount(0);
  await page.goto(`/members/${memberId}`);
  await expect(page.getByRole('heading', { name: 'Alessia Bianchi' })).toBeVisible();
  await expect(page.getByLabel(/Modifica|Sospendi|Riattiva|Archivia/)).toHaveCount(0);
  for (const route of ['/members/new', `/members/${memberId}/edit`]) {
    await page.goto(route);
    await expect(page).toHaveURL(/access-denied/);
    await expect(page.getByRole('heading', { name: 'Accesso negato' })).toBeVisible();
    await expect(page.getByText(/non dispone del ruolo richiesto/i)).toBeVisible();
  }
});

test('directory: ricerca, paginazione, dettaglio e responsive', async ({ page }) => {
  await authenticate(page, 'administrator');
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/members');
  await expect(page.getByRole('status').filter({ hasText: /iscritti trovati/ })).toContainText('75 iscritti trovati');
  await expect(page.getByLabel('Paginazione iscritti')).toBeVisible();
  await page.getByLabel('Cerca iscritti').fill('Alessia Bianchi');
  await expect(page.getByRole('status').filter({ hasText: /iscritti trovati/ })).toHaveText('4 iscritti trovati');
  await page.getByLabel('Apri il dettaglio di Alessia Bianchi').first().click();
  await expect(page.getByRole('heading', { name: 'Alessia Bianchi' })).toBeVisible();
  await page.goto('/members/00000000-0000-0000-0000-000000000001');
  await expect(page.getByText(/non esiste nel dataset demo/)).toBeVisible();
  await page.goto('/members');
  await expect(page.getByRole('heading', { name: 'Iscritti', exact: true })).toBeVisible();
  await page.screenshot({ path: 'test-results/documentation/members-directory-desktop.png', fullPage: true });
  for (const viewport of [{ width: 768, height: 1024 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(viewport);
    await page.goto('/members');
    await expect(page.getByRole('heading', { name: 'Iscritti', exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
  }
});

test('form: validazione accessibile, focus e layout mobile', async ({ page }) => {
  await authenticate(page, 'administrator');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/members/new');
  for (const label of ['Nome', 'Cognome', 'Data di nascita', 'Data di iscrizione', 'Email', 'Telefono', 'Consenso privacy'])
    await expect(page.getByText(label, { exact: true }).first()).toBeVisible();
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  const summaryAlert = page.getByRole('alert').filter({ hasText: 'Correggi i dati indicati.' });
  const summary = summaryAlert.locator('..');
  await expect(summary).toBeFocused();
  await expect(summaryAlert).toHaveCount(1);
  await expect(summary).toContainText('Il nome è obbligatorio.');
  await expect(summary).toContainText('Il consenso privacy è obbligatorio.');
  await expect(page.locator('#privacy-error')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
  await page.screenshot({ path: 'test-results/documentation/member-form-mobile.png', fullPage: true });
});

test('IndexedDB persiste e il dialogo archivia senza eliminare il record', async ({ page }) => {
  await authenticate(page, 'administrator');
  await page.goto(`/members/${memberId}`);
  await expect(page.getByText('Modalità: IndexedDB')).toHaveCount(0);
  await page.getByRole('button', { name: /Sospendi Alessia Bianchi/ }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Iscritto sospeso.' })).toBeVisible();
  await page.reload();
  await expect(page.getByText('Sospeso', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: /Riattiva Alessia Bianchi/ }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Iscritto riattivato.' })).toBeVisible();
  const archive = page.getByRole('button', { name: /Archivia Alessia Bianchi/ });
  await archive.focus();
  await page.keyboard.press('Enter');
  const dialog = page.getByRole('dialog');
  await expect(dialog).toContainText('Alessia Bianchi');
  await expect(dialog).toContainText(/resterà nello storico e non potrà essere riattivato/);
  await dialog.getByRole('button', { name: 'Annulla' }).click();
  await expect(page.getByText('Attivo', { exact: true })).toBeVisible();
  await archive.click();
  await dialog.getByRole('button', { name: 'Archivia' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Iscritto archiviato.' })).toBeVisible();
  await page.reload();
  await expect(page.getByText('Archiviato', { exact: true })).toBeVisible();
  await expect(page.getByLabel(/Modifica|Sospendi|Riattiva|Archivia/)).toHaveCount(0);
  await page.goto('/');
  await expect(page.getByText('Modalità: IndexedDB')).toBeVisible();
});

test('fallback in memoria è dichiarato e si perde al reload', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(window, 'indexedDB', { value: undefined });
  });
  await authenticate(page, 'administrator');
  await page.goto('/');
  await expect(page.getByText('Modalità: memoria temporanea')).toBeVisible();
  await expect(page.getByText(/non sopravvivranno al reload/)).toBeVisible();
  await page.goto(`/members/${memberId}`);
  await page.getByRole('button', { name: /Sospendi Alessia Bianchi/ }).click();
  await expect(page.getByText('Sospeso', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText('Attivo', { exact: true })).toBeVisible();
});
