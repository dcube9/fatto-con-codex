import { test, expect } from '@playwright/test';
import {
  assertNoHorizontalOverflow, authenticate, dashboardCounts, failWrites, fillMemberForm,
  installStorageInterceptor, readSnapshot, seedMemberId, setDate, viewports, waitForBlazor, writeSnapshot
} from '../support/member-fixtures.js';

const newMember = {
  firstName: 'Giulia', lastName: 'Collaudo', email: 'giulia.collaudo@example.invalid',
  phone: '+39 320 000 4242', dateOfBirth: '01/01/1990', joinedOn: '01/09/2026',
  emergencyContact: 'Referente fittizio +39 320 000 4343', notes: 'Profilo creato dal test browser.', privacyConsent: true
};

test.beforeEach(async ({ page }) => authenticate(page));

test('dataset vuoto persistito e autorizzazioni di creazione', async ({ page }) => {
  await page.addInitScript(() => {
    let storage;
    Object.defineProperty(window, 'demoStorage', {
      configurable: true,
      get: () => storage,
      set: value => {
        const originalLoad = value.load.bind(value);
        storage = { ...value, load: async () => {
          const current = await originalLoad();
          if (current) return current;
          const dataset = await (await fetch('/data/initial-dataset.json')).json();
          const empty = { ...dataset, members: [], subscriptions: [], accesses: [], payments: [] };
          const json = JSON.stringify(empty);
          await value.save(json);
          return json;
        } };
      }
    });
  });
  await page.goto('/members');
  await expect(page.getByText('Il dataset demo non contiene iscritti.')).toBeVisible();
  await expect(page.getByRole('table')).toHaveCount(0);
  await expect(page.getByLabel('Paginazione iscritti')).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Nuovo iscritto' })).toBeVisible();
  await page.reload();
  await expect(page.getByText('Il dataset demo non contiene iscritti.')).toBeVisible();
  await page.evaluate(id => localStorage.setItem('viteklub.demo.current-user', id), 'f536acbd-0a9d-51fe-8446-6d76838ce13a');
  await page.reload();
  await expect(page.getByRole('link', { name: 'Nuovo iscritto' })).toHaveCount(0);
});

test('creazione e modifica complete persistono senza duplicati', async ({ page }) => {
  const before = await dashboardCounts(page);
  await page.goto('/members/new');
  await fillMemberForm(page, newMember);
  const save = page.getByRole('button', { name: 'Salva iscritto' });
  await save.dblclick();
  await expect(page).toHaveURL(/\/members\/[0-9a-f-]+\?saved=1$/);
  await expect(page.getByRole('status').filter({ hasText: 'Iscritto salvato correttamente.' })).toHaveCount(1);
  const id = page.url().match(/members\/([0-9a-f-]+)/)[1];
  const number = (await page.getByText('Numero tessera').locator('..').textContent()).replace('Numero tessera', '').trim();
  expect(number).toMatch(/^VK-\d+$/);
  await page.screenshot({ path: 'test-results/documentation/member-created-desktop.png', fullPage: true });
  await page.reload();
  await expect(page.getByText(number, { exact: true })).toBeVisible();
  await page.getByLabel(`Modifica ${newMember.firstName} ${newMember.lastName}`).click();
  await expect(page.getByLabel('Consenso privacy')).toBeChecked();
  const changed = { ...newMember, firstName: 'Giuliana', lastName: 'Verifica', email: 'giuliana.verifica@example.invalid', phone: '+39 320 000 4444', emergencyContact: 'Nuovo referente fittizio', notes: 'Dati aggiornati dal browser.' };
  await fillMemberForm(page, changed);
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page.getByRole('heading', { name: 'Giuliana Verifica' })).toBeVisible();
  await page.reload();
  await expect(page.getByText(number, { exact: true })).toBeVisible();
  await page.goto('/members');
  await page.getByLabel('Cerca iscritti').fill(number);
  await expect(page.getByRole('status').filter({ hasText: /iscritto trovato/ })).toHaveText('1 iscritto trovato');
  await expect(page.getByText('Giuliana Verifica', { exact: true })).toBeVisible();
  const snapshot = await readSnapshot(page);
  expect(snapshot.members.filter(member => member.id === id)).toHaveLength(1);
  expect(snapshot.members.find(member => member.id === id).memberNumber).toBe(number);
  const after = await dashboardCounts(page);
  expect(after['Iscritti totali']).toBe(before['Iscritti totali'] + 1);
  expect(after['Iscritti attivi']).toBe(before['Iscritti attivi'] + 1);
});

test('validazione DOM completa, date e responsive', async ({ page }) => {
  await page.setViewportSize(viewports.mobile);
  await page.goto('/members/new');
  await page.getByLabel('Nome', { exact: true }).fill('N'.repeat(101));
  await page.getByLabel('Cognome', { exact: true }).fill('C'.repeat(101));
  await page.getByLabel('Email', { exact: true }).fill(`${'e'.repeat(250)}@x.invalid`);
  await page.getByLabel('Telefono', { exact: true }).fill('1'.repeat(51));
  await page.getByLabel('Contatto di emergenza (facoltativo)').fill('E'.repeat(1001));
  await page.getByLabel('Note (facoltative)').fill('N'.repeat(1001));
  await setDate(page, 'Data di nascita', '01/09/2026');
  await setDate(page, 'Data di iscrizione', '31/08/2026');
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  const summaries = page.getByRole('alert').filter({ hasText: 'Correggi i dati indicati.' });
  await expect(summaries).toHaveCount(1);
  await expect(summaries).toContainText('non può superare');
  await expect(summaries).toContainText(/data di iscrizione/i);
  await expect(page.getByLabel('Consenso privacy')).toHaveAttribute('aria-describedby', 'privacy-error');
  await expect(page.locator('#privacy-error')).toBeVisible();
  await assertNoHorizontalOverflow(page);
  await page.screenshot({ path: 'test-results/documentation/member-form-errors-mobile.png', fullPage: true });
});

test('errore di scrittura in creazione conserva pagina, form e snapshot', async ({ page }) => {
  await installStorageInterceptor(page);
  await page.goto('/members/new');
  await fillMemberForm(page, newMember);
  const before = await readSnapshot(page);
  await failWrites(page, true);
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  const alert = page.getByRole('alert').filter({ hasText: 'Non è stato possibile salvare l’iscritto.' });
  await expect(alert).toBeVisible();
  await expect(alert).not.toContainText(/IndexedDB|JavaScript|stack/i);
  await expect(page).toHaveURL(/members\/new$/);
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue(newMember.firstName);
  await expect(page.getByRole('button', { name: 'Salva iscritto' })).toBeEnabled();
  await failWrites(page, false);
  expect(await readSnapshot(page)).toEqual(before);
});

test('conflitto di versione e record rimosso non sovrascrivono lo snapshot', async ({ context, page }) => {
  const second = await context.newPage();
  await authenticate(second);
  await page.goto(`/members/${seedMemberId}/edit`);
  await second.goto(`/members/${seedMemberId}/edit`);
  await second.getByLabel('Nome', { exact: true }).fill('Prima');
  await second.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(second.getByRole('heading', { name: /Prima Bianchi/ })).toBeVisible();
  await page.getByLabel('Nome', { exact: true }).fill('Obsoleta');
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page.getByRole('alert')).toContainText(/Ricarica e riprova/);
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue('Obsoleta');
  expect((await readSnapshot(page)).members.find(member => member.id === seedMemberId).firstName).toBe('Prima');
  await page.reload();
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue('Prima');

  await writeSnapshot(second, dataset => ({ ...dataset, members: dataset.members.filter(member => member.id !== seedMemberId) }));
  await page.getByLabel('Nome', { exact: true }).fill('Non ricreare');
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page.getByRole('alert')).toContainText(/non esiste più|Ricarica|Non è stato possibile salvare/);
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue('Non ricreare');
  expect((await readSnapshot(page)).members.some(member => member.id === seedMemberId)).toBe(false);
});

test('ciclo di vita integrato e dialogo accessibile nei tre viewport', async ({ page }) => {
  const initial = await dashboardCounts(page);
  for (const viewport of Object.values(viewports)) {
    await page.setViewportSize(viewport);
    await page.goto(`/members/${seedMemberId}`);
    await assertNoHorizontalOverflow(page);
  }
  await page.setViewportSize(viewports.tablet);
  await page.goto(`/members/${seedMemberId}`);
  await page.getByRole('button', { name: /Sospendi Alessia Bianchi/ }).click();
  await page.reload();
  await expect(page.getByText('Sospeso', { exact: true })).toBeVisible();
  const suspended = await dashboardCounts(page);
  expect(suspended['Iscritti attivi']).toBe(initial['Iscritti attivi'] - 1);
  expect(suspended['Iscritti sospesi']).toBe(initial['Iscritti sospesi'] + 1);
  await page.goto(`/members/${seedMemberId}`);
  await page.getByRole('button', { name: /Riattiva/ }).click();
  const archive = page.getByRole('button', { name: 'Archivia Alessia Bianchi' });
  await archive.focus();
  await archive.press('Enter');
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByRole('button', { name: 'Archivia' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(dialog.getByRole('button', { name: 'Annulla' })).toBeFocused();
  await page.screenshot({ path: 'test-results/documentation/member-archive-dialog-tablet.png', fullPage: true });
  await page.keyboard.press('Escape');
  await expect(archive).toBeVisible();
  await archive.press('Enter');
  await dialog.getByRole('button', { name: 'Archivia' }).press('Enter');
  await page.reload();
  await expect(page.getByText('Archiviato', { exact: true })).toBeVisible();
  await expect(page.getByLabel(/Modifica|Sospendi|Riattiva|Archivia/)).toHaveCount(0);
  await page.screenshot({ path: 'test-results/documentation/member-archived-final.png', fullPage: true });
  const final = await dashboardCounts(page);
  expect(final['Iscritti archiviati']).toBe(initial['Iscritti archiviati'] + 1);
});
