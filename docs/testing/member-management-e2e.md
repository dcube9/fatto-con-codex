# Verifica E2E della gestione iscritti

La suite usa Playwright come dipendenza **esclusivamente di test** e Chromium. La scelta
permette di verificare il DOM realmente prodotto da Blazor e MudBlazor, le route
autorizzate, tastiera, viewport e IndexedDB; i test .NET e l'ispezione statica non sono
alternative equivalenti per questi scenari. Non viene aggiunta alcuna dipendenza al
progetto applicativo.

## Esecuzione locale

```bash
cd tests/ViteKlub.E2E
npm ci
npx playwright install chromium
npm test
```

Playwright avvia ViteKlub su `http://127.0.0.1:4173`, attende la pagina di login e
arresta il processo a fine suite. I profili browser, report e trace diagnostici sono
ignorati da Git. Non sono previsti retry.

## Copertura automatizzata

- redirect anonimo con ritorno alla route richiesta;
- route di lettura e operative per Administrator, Manager, Receptionist e Viewer;
- rendering di directory e dettaglio, ricerca, paginazione e identificativo inesistente;
- errori del form, riepilogo annunciato, focus dopo submit e associazione del consenso;
- matrice delle azioni, tastiera e conferma/annullamento del dialogo di archiviazione;
- persistenza dei cambi di stato in IndexedDB dopo reload e modalità amministrativa;
- fallback quando IndexedDB non è disponibile;
- assenza di overflow a 1440×900, 768×1024 e 390×844.

La suite acquisisce una directory desktop e un form mobile, con soli dati fittizi del
seed, in `tests/ViteKlub.E2E/test-results/documentation`. Gli screenshot restano
artefatti locali intenzionalmente ignorati da Git: il sistema di creazione delle Pull
Request non supporta file binari.

## Limiti e verifiche non automatizzate

La suite non forza dall'esterno conflitti simultanei di revisione/versione né errori a
metà transazione: questi casi restano coperti dai test .NET dello store e dei comandi.
Non sono ancora automatizzati nel browser il dataset vuoto, la creazione/modifica
completa e il confronto dei conteggi dashboard lungo l'intero ciclo di vita; la suite
browser verifica invece il ciclo sospensione/riattivazione/archiviazione sul seed
isolato. Questi limiti sono intenzionalmente dichiarati e non sono presentati come
copertura browser.
Non viene eseguito un audit automatico WCAG (per esempio axe); nomi, ruoli, associazioni,
focus e annunci principali sono verificati tramite l'accessibility tree di Playwright.
Il deploy pubblico non è verificato perché il repository non configura un URL di deploy.
