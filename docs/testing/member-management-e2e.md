# Verifica E2E della gestione iscritti

La suite usa Playwright come dipendenza esclusivamente di test e Chromium. Verifica il DOM
prodotto da Blazor e MudBlazor, le route autorizzate, la tastiera, i viewport e IndexedDB;
non viene aggiunta alcuna dipendenza al progetto applicativo.

## Esecuzione locale

```bash
cd tests/ViteKlub.E2E
npm ci
npx playwright install chromium
npm test
```

Playwright avvia ViteKlub su `http://127.0.0.1:4173`, attende il login e arresta il
processo a fine suite. Profili, report, screenshot e trace sono ignorati da Git. I retry
sono disabilitati.

## Copertura automatizzata

- redirect anonimo e route di lettura/operative per tutti i ruoli demo;
- directory, ricerca, paginazione, dettaglio, identificativo inesistente e dataset vuoto;
- autorizzazione alla creazione distinta fra Administrator e Viewer;
- creazione e modifica complete con identificativo e tessera generati dall'applicazione,
  persistenza, assenza di duplicati e conteggi dashboard;
- limiti dei campi, regole sulle date, record inesistente o archiviato e messaggi italiani;
- ciclo sospensione, riattivazione e archiviazione fra dashboard, directory e dettaglio;
- fallimento controllato di `demoStorage.save`, conservazione di pagina, form e snapshot;
- conflitto di versione fra due pagine e rimozione del record durante una modifica, senza
  sovrascrittura dei dati correnti;
- riepilogo e annunci unici, associazione `aria-describedby` degli errori, nomi
  accessibili, ciclo del focus e comandi da tastiera del dialogo, intestazioni e assenza
  di overflow a 1440×900, 768×1024 e 390×844;
- fallback in memoria e perdita documentata delle modifiche al reload.

Gli helper in `support/member-fixtures.js` centralizzano autenticazione, attesa del
rendering Blazor, lettura e scrittura della fixture IndexedDB del singolo context,
compilazione tramite label, conteggi dashboard e intercettazione ripristinabile di
`demoStorage.load`/`demoStorage.save`. Ogni test usa il context isolato creato da
Playwright. La fixture senza iscritti elimina anche i record dipendenti per restare valida;
il seed distribuito non viene modificato.

L'errore di persistenza è simulato sostituendo prima del bootstrap soltanto la funzione
pubblica `demoStorage.save` del context. Non esistono flag diagnostici di produzione. La
concorrenza usa due pagine dello stesso context, quindi lo stesso IndexedDB, e riproduce il
controllo ottimistico della versione dell'iscritto e il record scomparso.

## Artefatti locali

Gli screenshot documentali sono scritti esclusivamente in
`tests/ViteKlub.E2E/test-results/documentation`:

- `members-directory-desktop.png`;
- `member-form-mobile.png`;
- `member-created-desktop.png`;
- `member-form-errors-mobile.png`;
- `member-archive-dialog-tablet.png`;
- `member-archived-final.png`.

Questi file restano ignorati perché il flusso Create PR non supporta file binari. I trace
sono prodotti soltanto in caso di fallimento (`retain-on-failure`).

## Limiti e verifiche manuali

Non è stato aggiunto axe: l'alternativa è una serie mirata di asserzioni sul DOM e sul
focus reali, che non costituisce un audit WCAG completo. Restano manuali le verifiche con
tecnologie assistive reali. La collisione della revisione in memoria del medesimo store
resta coperta dai test .NET: la revisione non è persistita e non è un'API browser
pilotabile. Browser: Chromium; viewport: 1440×900, 768×1024 e 390×844.

## Registro di validazione del 12 settembre 2026

La consolidazione è stata verificata a partire dalla baseline immutabile
`a468ed8ec8639a5524934ea0b84376b6db051495`, con seed
`d5190e7bbadbb4604b75d0c666ab53225bea10aa39036d1782c2d3eedfab2630`. L'ambiente era
Ubuntu 24.04, .NET SDK 10.0.111, Node.js 20.20.2, npm 11.4.2 e Playwright 1.55.0 con
Chromium 140.0.7339.16 (build Playwright 1187). La configurazione effettiva ha usato un
worker, progetto `chromium`, retry zero, trace conservato solo al fallimento e screenshot
al fallimento.

Il comando di accettazione è `npx playwright test --retries=0 --reporter=line`, eseguito
dalla directory `tests/ViteKlub.E2E`: il run completo con retry zero ha concluso 40 test
superati, zero falliti e un test ignorato in 16,7 minuti. Il caso ignorato è marcato
`fixme`: il comando membro ammette l'iscrizione nella stessa data della nascita, mentre il
validatore dell'intero dataset la rifiuta. La discrepanza è lasciata esplicita perché la
presente attività non modifica il codice applicativo. I browser diversi da Chromium, gli
screen reader reali e un audit WCAG completo non sono stati eseguiti.
