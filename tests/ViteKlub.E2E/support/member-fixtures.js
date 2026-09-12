import { expect } from '@playwright/test';

export const users = {
  administrator: '1e716ceb-f355-57af-a13e-25012c245a95',
  manager: 'fcdcfdf5-5443-5370-bac6-c9210995310a',
  receptionist: '957b5aa3-3100-59ac-bcfe-ae518c8cb6da',
  viewer: 'f536acbd-0a9d-51fe-8446-6d76838ce13a'
};

export const seedMemberId = '696534be-0212-5a49-8393-a7ec2c84f854';
export const viewports = {
  desktop: { width: 1440, height: 900 },
  tablet: { width: 768, height: 1024 },
  mobile: { width: 390, height: 844 }
};

export async function authenticate(page, role = 'administrator') {
  await page.addInitScript(({ id }) => localStorage.setItem('viteklub.demo.current-user', id), { id: users[role] });
}

export async function waitForBlazor(page) {
  await expect(page.locator('#blazor-error-ui')).toBeHidden();
  await expect(page.locator('[aria-label^="Caricamento"], [role="status"]', { hasText: /Caricamento/ })).toHaveCount(0);
}

export async function clearDatabase(page) {
  await page.goto('/login');
  await page.evaluate(async () => {
    await new Promise((resolve, reject) => {
      const request = indexedDB.deleteDatabase('viteklub-demo');
      request.onsuccess = resolve;
      request.onerror = () => reject(request.error);
      request.onblocked = resolve;
    });
  });
}

export async function readSnapshot(page) {
  return page.evaluate(async () => JSON.parse(await window.demoStorage.load()));
}

export async function writeSnapshot(page, transform) {
  const snapshot = await readSnapshot(page);
  const changed = transform(structuredClone(snapshot));
  await page.evaluate(value => window.demoStorage.save(JSON.stringify(value)), changed);
  return changed;
}

export async function installStorageInterceptor(page) {
  await page.addInitScript(() => {
    const state = { failSave: false, saves: 0, loads: 0 };
    window.__e2eStorage = state;
    let storage;
    Object.defineProperty(window, 'demoStorage', {
      configurable: true,
      get: () => storage,
      set: value => {
        const load = value.load.bind(value);
        const save = value.save.bind(value);
        storage = {
          load: async (...args) => { state.loads++; return load(...args); },
          save: async (...args) => {
            state.saves++;
            if (state.failSave) throw new Error('E2E controlled write failure');
            return save(...args);
          }
        };
      }
    });
  });
}

export async function failWrites(page, enabled) {
  await page.evaluate(value => { window.__e2eStorage.failSave = value; }, enabled);
}

export async function fillMemberForm(page, member) {
  for (const [label, value] of Object.entries({
    Nome: member.firstName,
    Cognome: member.lastName,
    Email: member.email,
    Telefono: member.phone,
    'Contatto di emergenza (facoltativo)': member.emergencyContact,
    'Note (facoltative)': member.notes
  })) await page.getByLabel(label, { exact: true }).fill(value);
  await setDate(page, 'Data di nascita', member.dateOfBirth);
  await setDate(page, 'Data di iscrizione', member.joinedOn);
  const privacy = page.getByLabel('Consenso privacy', { exact: true });
  if (member.privacyConsent && !await privacy.isChecked()) await privacy.check();
}

export async function setDate(page, label, value) {
  const input = page.getByLabel(label, { exact: true });
  await input.evaluate(element => element.removeAttribute('readonly'));
  await input.fill(value);
  await input.press('Tab');
  await expect(input).toHaveValue(value);
}

export async function dashboardCounts(page) {
  await page.goto('/');
  await waitForBlazor(page);
  const result = {};
  for (const label of ['Iscritti totali', 'Iscritti attivi', 'Iscritti sospesi', 'Iscritti archiviati']) {
    const card = page.getByText(label, { exact: true }).locator('..');
    result[label] = Number(await card.locator('.mud-typography-h5').textContent());
  }
  return result;
}

export async function assertNoHorizontalOverflow(page) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
}
