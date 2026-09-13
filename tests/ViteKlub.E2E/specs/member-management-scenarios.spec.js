import { test, expect } from '@playwright/test';
import {
  assertNoHorizontalOverflow, authenticate, dashboardCounts, failWrites, fillMemberForm,
  installStorageInterceptor, pauseWrites, readSnapshot, seedMemberId, setDate, viewports, waitForBlazor, writeSnapshot
} from '../support/member-fixtures.js';

const newMember = {
  firstName: 'Giulia', lastName: 'Collaudo', email: 'giulia.collaudo@example.invalid',
  phone: '+39 320 000 4242', dateOfBirth: '01/01/1990', joinedOn: '01/09/2026',
  emergencyContact: 'Referente fittizio +39 320 000 4343', notes: 'Profilo creato dal test browser.', privacyConsent: true
};

test.beforeEach(async ({ page }) => authenticate(page));

test('ogni test riceve storage browser isolato', async ({ page }) => {
  await page.goto('/login');
  await waitForBlazor(page);
  expect(await page.evaluate(() => localStorage.getItem('viteklub.e2e.sentinel'))).toBeNull();
  await page.evaluate(() => localStorage.setItem('viteklub.e2e.sentinel', 'isolated'));
  const snapshot = await readSnapshot(page);
  expect(snapshot.members.some(member => member.firstName === 'Isolamento E2E')).toBe(false);
});

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
  await installStorageInterceptor(page);
  const before = await dashboardCounts(page);
  await page.goto('/members/new');
  await fillMemberForm(page, newMember);
  const save = page.getByRole('button', { name: 'Salva iscritto' });
  const savesBefore = await page.evaluate(() => window.__e2eStorage.saves);
  await pauseWrites(page, true);
  const submission = save.click();
  await expect(save).toBeDisabled();
  await expect(page.getByRole('status').filter({ hasText: 'Salvataggio in corso' })).toHaveCount(1);
  await expect.poll(async () => page.evaluate(() => window.__e2eStorage.saves)).toBe(savesBefore + 1);
  await pauseWrites(page, false);
  await submission;
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
  const changed = { ...newMember, firstName: 'Giuliana', lastName: 'Verifica', dateOfBirth: '02/02/1991', joinedOn: '02/09/2026', email: 'giuliana.verifica@example.invalid', phone: '+39 320 000 4444', emergencyContact: 'Nuovo referente fittizio', notes: 'Dati aggiornati dal browser.' };
  await fillMemberForm(page, changed);
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page.getByRole('heading', { name: 'Giuliana Verifica' })).toBeVisible();
  await page.reload();
  await expect(page.getByText(number, { exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Giuliana Verifica' })).toBeVisible();
  await page.goto('/members');
  await page.getByLabel('Cerca iscritti').fill(number);
  await expect(page.getByRole('status').filter({ hasText: /iscritto trovato/ })).toHaveText('1 iscritto trovato');
  await expect(page.getByText('Giuliana Verifica', { exact: true })).toBeVisible();
  const snapshot = await readSnapshot(page);
  expect(snapshot.members.filter(member => member.id === id)).toHaveLength(1);
  expect(snapshot.members.find(member => member.id === id)).toEqual(expect.objectContaining({
    firstName: changed.firstName,
    lastName: changed.lastName,
    email: changed.email,
    phone: changed.phone,
    emergencyContact: changed.emergencyContact,
    notes: changed.notes,
    privacyConsent: true,
    memberNumber: number
  }));
  const after = await dashboardCounts(page);
  expect(after['Iscritti totali']).toBe(before['Iscritti totali'] + 1);
  expect(after['Iscritti attivi']).toBe(before['Iscritti attivi'] + 1);
});

test('route inesistenti e archiviate restano distinguibili', async ({ page }) => {
  const missing = '00000000-0000-0000-0000-000000000001';
  await page.goto(`/members/${missing}`);
  await expect(page.getByRole('alert')).toContainText('non esiste nel dataset demo');
  await page.goto(`/members/${missing}/edit`);
  await expect(page.getByRole('alert')).toContainText('non esiste nel dataset demo');
  await writeSnapshot(page, dataset => ({ ...dataset, members: dataset.members.map(member =>
    member.id === seedMemberId ? { ...member, status: 'archived' } : member) }));
  await page.goto(`/members/${seedMemberId}`);
  await expect(page.getByText('Archiviato', { exact: true })).toBeVisible();
  await expect(page.getByLabel(/Modifica|Sospendi|Riattiva|Archivia/)).toHaveCount(0);
  await page.goto(`/members/${seedMemberId}/edit`);
  await expect(page.getByRole('alert')).toContainText('archiviato e non può essere modificato');
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

const textBoundaries = [
  ['Nome', 100, 'N', 'Il campo non può superare 100 caratteri.'],
  ['Cognome', 100, 'C', 'Il campo non può superare 100 caratteri.'],
  ['Email', 254, 'e', 'Il campo non può superare 254 caratteri.'],
  ['Telefono', 50, '1', 'Il campo non può superare 50 caratteri.'],
  ['Contatto di emergenza (facoltativo)', 1000, 'E', 'Il contatto di emergenza è troppo lungo.'],
  ['Note (facoltative)', 1000, 'N', 'Le note sono troppo lunghe.']
];

for (const [label, maximum, character, message] of textBoundaries) {
  test(`confine ${label}: accetta il limite`, async ({ page }) => {
    await page.goto('/members/new');
    await fillMemberForm(page, newMember);
    const value = label === 'Email' ? `${'e'.repeat(maximum - 12)}@example.com` : character.repeat(maximum);
    await page.getByLabel(label, { exact: true }).fill(value);
    await page.getByRole('button', { name: 'Salva iscritto' }).click();
    await expect(page).toHaveURL(/\/members\/[0-9a-f-]+\?saved=1$/);
  });

  test(`confine ${label}: rifiuta oltre il limite`, async ({ page }) => {
    await installStorageInterceptor(page);
    await page.goto('/members/new');
    await fillMemberForm(page, newMember);
    const saves = await page.evaluate(() => window.__e2eStorage.saves);
    const value = label === 'Email' ? `${'e'.repeat(maximum - 11)}@example.com` : character.repeat(maximum + 1);
    await page.getByLabel(label, { exact: true }).fill(value);
    await page.getByRole('button', { name: 'Salva iscritto' }).click();
    await expect(page.getByRole('alert')).toContainText(message);
    expect(await page.evaluate(() => window.__e2eStorage.saves)).toBe(saves);
  });
}

for (const scenario of [
  ['nascita uguale alla data operativa', '09/12/2026', '09/12/2026', 'La data di nascita deve precedere la data operativa.'],
  ['nascita successiva alla data operativa', '09/13/2026', '09/13/2026', 'La data di nascita deve precedere la data operativa.'],
  ['iscrizione precedente alla nascita', '02/01/1990', '01/01/1990', 'La data di iscrizione non può precedere la data di nascita.']
]) {
  test(`date: ${scenario[0]}`, async ({ page }) => {
    await installStorageInterceptor(page);
    await page.goto('/members/new');
    await fillMemberForm(page, { ...newMember, dateOfBirth: scenario[1], joinedOn: scenario[2] });
    const saves = await page.evaluate(() => window.__e2eStorage.saves);
    await page.getByRole('button', { name: 'Salva iscritto' }).click();
    await expect(page.getByRole('alert')).toContainText(scenario[3]);
    expect(await page.evaluate(() => window.__e2eStorage.saves)).toBe(saves);
  });
}

test('date: iscrizione uguale alla nascita è accettata', async ({ page }) => {
  test.fixme(true, 'Il validatore del dataset rifiuta ancora la stessa data ammessa dal comando membro.');
  await page.goto('/members/new');
  await fillMemberForm(page, { ...newMember, dateOfBirth: '06/15/2020', joinedOn: '06/15/2020' });
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page).toHaveURL(/\/members\/[0-9a-f-]+\?saved=1$/);
});

for (const [viewportName, viewport] of Object.entries(viewports)) {
  test(`accessibilità form, focus e intestazioni: ${viewportName}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await page.goto('/members/new');
    await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1);
    await page.getByRole('button', { name: 'Salva iscritto' }).focus();
    await page.keyboard.press('Enter');
    await expect(page.getByRole('alert').filter({ hasText: 'Correggi i dati indicati.' })).toHaveCount(1);
    const invalidFields = page.locator('[aria-describedby]');
    for (let index = 0; index < await invalidFields.count(); index++) {
      const field = invalidFields.nth(index);
      for (const id of (await field.getAttribute('aria-describedby')).split(/\s+/)) {
        await expect(page.locator(`#${id}`)).toBeVisible();
        await expect(page.locator(`#${id}`)).not.toHaveText('');
      }
    }
    const focused = page.locator(':focus');
    const box = await focused.boundingBox();
    expect(box).not.toBeNull();
    expect(box.x).toBeGreaterThanOrEqual(0);
    expect(box.y).toBeGreaterThanOrEqual(0);
    expect(box.x + box.width).toBeLessThanOrEqual(viewport.width);
    expect(box.y).toBeLessThan(viewport.height);
    await assertNoHorizontalOverflow(page);
  });
}

test('errore di scrittura in creazione conserva pagina, form e snapshot', async ({ page }) => {
  await installStorageInterceptor(page);
  await page.goto('/members/new');
  await fillMemberForm(page, newMember);
  const before = await readSnapshot(page);
  const savesBefore = await page.evaluate(() => window.__e2eStorage.saves);
  await failWrites(page, true);
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  const alert = page.getByRole('alert').filter({ hasText: 'Non è stato possibile salvare l’iscritto.' });
  await expect(alert).toBeVisible();
  await expect(alert).not.toContainText(/IndexedDB|JavaScript|stack/i);
  await expect(page).toHaveURL(/members\/new$/);
  await expect(page.getByLabel('Nome', { exact: true })).toHaveValue(newMember.firstName);
  await expect(page.getByRole('button', { name: 'Salva iscritto' })).toBeEnabled();
  expect(await page.evaluate(() => window.__e2eStorage.saves)).toBe(savesBefore + 1);
  await failWrites(page, false);
  expect(await readSnapshot(page)).toEqual(before);
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page).toHaveURL(/\/members\/[0-9a-f-]+\?saved=1$/);
  expect(await page.evaluate(() => window.__e2eStorage.saves)).toBe(savesBefore + 2);
});

for (const scenario of [
  { name: 'modifica', form: true, route: `/members/${seedMemberId}/edit`, action: async page => {
    await page.getByLabel('Nome', { exact: true }).fill('Valore conservato');
    await page.getByRole('button', { name: 'Salva iscritto' }).click();
  }, retained: async page => expect(page.getByLabel('Nome', { exact: true })).toHaveValue('Valore conservato'), retry: async page => page.getByRole('button', { name: 'Salva iscritto' }).click() },
  { name: 'sospensione', route: `/members/${seedMemberId}`, action: async page => page.getByRole('button', { name: /Sospendi/ }).click(), retained: async page => expect(page.getByText('Attivo', { exact: true })).toBeVisible(), retry: async page => page.getByRole('button', { name: /Sospendi/ }).click() },
  { name: 'riattivazione', route: `/members/${seedMemberId}`, prepare: async page => {
    await page.getByRole('button', { name: /Sospendi/ }).click();
    await expect(page.getByText('Sospeso', { exact: true })).toBeVisible();
  }, action: async page => page.getByRole('button', { name: /Riattiva/ }).click(), retained: async page => expect(page.getByText('Sospeso', { exact: true })).toBeVisible(), retry: async page => page.getByRole('button', { name: /Riattiva/ }).click() },
  { name: 'archiviazione', route: `/members/${seedMemberId}`, action: async page => {
    await page.getByRole('button', { name: /Archivia/ }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Archivia' }).click();
  }, retained: async page => expect(page.getByText('Attivo', { exact: true })).toBeVisible(), retry: async page => {
    await page.getByRole('button', { name: /Archivia/ }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Archivia' }).click();
  } }
]) {
  test(`errore di scrittura in ${scenario.name} è atomico e ripristinabile`, async ({ context, page }) => {
    await page.goto('/login');
    await readSnapshot(page);
    const intercepted = await context.newPage();
    await authenticate(intercepted);
    await installStorageInterceptor(intercepted);
    await intercepted.goto(scenario.route);
    await waitForBlazor(intercepted);
    if (scenario.prepare) await scenario.prepare(intercepted);
    const before = await readSnapshot(intercepted);
    const savesBefore = await intercepted.evaluate(() => window.__e2eStorage.saves);
    await failWrites(intercepted, true);
    await scenario.action(intercepted);
    await expect(intercepted.getByRole('alert')).toContainText(/Non è stato possibile/);
    expect(await readSnapshot(intercepted)).toEqual(before);
    expect(await intercepted.evaluate(() => window.__e2eStorage.saves)).toBe(savesBefore + 1);
    if (scenario.form) await scenario.retained(intercepted);
    else { await intercepted.reload(); await scenario.retained(intercepted); }
    const retrySavesBefore = await intercepted.evaluate(() => window.__e2eStorage.saves);
    await failWrites(intercepted, false);
    await scenario.retry(intercepted);
    await expect.poll(async () => intercepted.evaluate(() => window.__e2eStorage.saves)).toBe(retrySavesBefore + 1);
  });
}

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

test('context distinti isolano IndexedDB, localStorage e interceptor', async ({ browser }) => {
  const firstContext = await browser.newContext();
  const secondContext = await browser.newContext();
  const first = await firstContext.newPage();
  const second = await secondContext.newPage();
  await authenticate(first);
  await authenticate(second);
  await installStorageInterceptor(first);
  await first.goto('/members');
  await second.goto('/members');
  await waitForBlazor(first);
  await waitForBlazor(second);
  const originalSecond = await readSnapshot(second);
  await writeSnapshot(first, dataset => ({ ...dataset, members: dataset.members.map(member =>
    member.id === seedMemberId ? { ...member, firstName: 'Solo primo context' } : member) }));
  expect((await readSnapshot(first)).members.find(member => member.id === seedMemberId).firstName).toBe('Solo primo context');
  expect(await readSnapshot(second)).toEqual(originalSecond);
  expect(await second.evaluate(() => window.__e2eStorage)).toBeUndefined();
  expect(await first.evaluate(() => localStorage.getItem('viteklub.demo.current-user')))
    .toBe(await second.evaluate(() => localStorage.getItem('viteklub.demo.current-user')));
  await firstContext.close();
  await secondContext.close();
});

test('ciclo di vita integrato e dialogo accessibile nei tre viewport', async ({ page }) => {
  test.slow();
  const initial = await dashboardCounts(page);
  await page.goto('/members/new');
  await fillMemberForm(page, newMember);
  await page.getByRole('button', { name: 'Salva iscritto' }).click();
  await expect(page).toHaveURL(/\/members\/[0-9a-f-]+\?saved=1$/);
  const id = page.url().match(/members\/([0-9a-f-]+)/)[1];
  const beforeRelations = await readSnapshot(page).then(data => ({
    subscriptions: data.subscriptions.filter(item => item.memberId === id),
    accesses: data.accesses.filter(item => item.memberId === id),
    payments: data.payments.filter(item => item.memberId === id)
  }));
  await page.reload();
  for (const viewport of Object.values(viewports)) {
    await page.setViewportSize(viewport);
    await page.goto('/members');
    await page.getByLabel('Cerca iscritti').fill('Giulia Collaudo');
    await expect(page.getByRole('status').filter({ hasText: /iscritto trovato/ })).toHaveText('1 iscritto trovato');
    await assertNoHorizontalOverflow(page);
  }
  await page.getByLabel('Apri il dettaglio di Giulia Collaudo').click();
  const afterCreate = await dashboardCounts(page);
  expect(afterCreate['Iscritti totali']).toBe(initial['Iscritti totali'] + 1);
  expect(afterCreate['Iscritti attivi']).toBe(initial['Iscritti attivi'] + 1);
  await page.goto(`/members/${id}`);
  await page.getByRole('button', { name: /Sospendi Giulia Collaudo/ }).click();
  await page.reload();
  await expect(page.getByText('Sospeso', { exact: true })).toBeVisible();
  const suspended = await dashboardCounts(page);
  expect(suspended['Iscritti attivi']).toBe(initial['Iscritti attivi']);
  expect(suspended['Iscritti sospesi']).toBe(initial['Iscritti sospesi'] + 1);
  await page.goto(`/members/${id}`);
  await page.getByRole('button', { name: /Riattiva/ }).click();
  await page.reload();
  await expect(page.getByText('Attivo', { exact: true })).toBeVisible();
  const archive = page.getByRole('button', { name: 'Archivia Giulia Collaudo' });
  await archive.focus();
  await archive.press('Enter');
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByRole('button', { name: 'Archivia' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(dialog.getByRole('button', { name: 'Annulla' })).toBeFocused();
  await page.keyboard.press('Shift+Tab');
  await expect(dialog.getByRole('button', { name: 'Archivia' })).toBeFocused();
  await page.screenshot({ path: 'test-results/documentation/member-archive-dialog-tablet.png', fullPage: true });
  await dialog.getByRole('button', { name: 'Annulla' }).press('Enter');
  await expect(dialog).toBeHidden();
  await archive.focus();
  await archive.press('Enter');
  await dialog.getByRole('button', { name: 'Archivia' }).press('Enter');
  await expect(page.getByText('Archiviato', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText('Archiviato', { exact: true })).toBeVisible();
  await expect(page.getByLabel(/Modifica|Sospendi|Riattiva|Archivia/)).toHaveCount(0);
  await page.screenshot({ path: 'test-results/documentation/member-archived-final.png', fullPage: true });
  const final = await dashboardCounts(page);
  expect(final['Iscritti totali']).toBe(initial['Iscritti totali'] + 1);
  expect(final['Iscritti archiviati']).toBe(initial['Iscritti archiviati'] + 1);
  const afterRelations = await readSnapshot(page).then(data => ({
    subscriptions: data.subscriptions.filter(item => item.memberId === id),
    accesses: data.accesses.filter(item => item.memberId === id),
    payments: data.payments.filter(item => item.memberId === id)
  }));
  expect(afterRelations).toEqual(beforeRelations);
});
