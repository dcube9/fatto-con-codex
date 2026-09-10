# Deploy su GitHub Pages

## Obiettivo

Il workflow `.github/workflows/ci.yml` pubblica la SPA Blazor WebAssembly su GitHub Pages
dopo ogni push su `main`. Le pull request eseguono soltanto build e test: non producono
deploy e non possono modificare il sito pubblico.

Il deploy non introduce backend, database remoto o segreti applicativi. Dataset e
sessione demo continuano a risiedere esclusivamente nel browser del visitatore.

## Configurazione iniziale del repository

Prima della prima esecuzione funzionante, un amministratore del repository deve aprire
**Settings → Pages → Build and deployment → Source** e selezionare **GitHub Actions**.
Questa operazione abilita Pages e permette a `actions/configure-pages` di leggere la
configurazione del sito. Il workflow non tenta di cambiare le impostazioni del repository:
non sono necessari branch di pubblicazione, personal access token, repository secret o
altre credenziali privilegiate.

L'environment `github-pages` viene usato dal job di deploy. Eventuali regole di protezione
dell'environment configurate su GitHub restano applicabili e non vengono modificate dal
workflow.

## Pipeline

La pipeline è composta da tre job:

1. `build-and-test` ripristina le dipendenze, verifica la formattazione, compila ed esegue
   i test;
2. `build-pages`, eseguito solo per un push su `main` e dopo il successo dei controlli,
   pubblica `ViteKlub.Web` e carica l'artifact Pages;
3. `deploy-pages` usa l'artifact per aggiornare l'environment `github-pages`.

I permessi sono dichiarati separatamente e negati per impostazione predefinita. Il job di
controllo dispone soltanto di `contents: read`; il job che prepara Pages dispone di
`contents: read` e `pages: read`, necessario a `actions/configure-pages`; il job finale
riceve esclusivamente `pages: write` e `id-token: write`, necessari al deploy tramite
GitHub Actions. Non viene usato `enablement: true`, perché l'abilitazione resta
un'operazione amministrativa esplicita e non richiede al workflow un token privilegiato.

Il workflow usa le major correnti delle action ufficiali, tutte compatibili con il
runtime Node 24: `actions/checkout@v7`, `actions/setup-dotnet@v6`,
`actions/upload-artifact@v7`, `actions/configure-pages@v6`,
`actions/upload-pages-artifact@v5` e `actions/deploy-pages@v5`. Non imposta
`ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION`.

## Base path e navigazione SPA

Un project site viene normalmente pubblicato sotto `/<nome-repository>/`, mentre un
repository chiamato `<account>.github.io` viene pubblicato alla radice. Lo script
`scripts/prepare-github-pages.sh` determina il base path dal nome del repository e
aggiorna l'elemento `<base>` dell'output pubblicato.

Lo script crea inoltre:

- `.nojekyll`, affinché GitHub Pages serva anche le directory che iniziano con `_`, come
  `_framework` e `_content`;
- `404.html`, copia del bootstrap della SPA, per consentire il caricamento diretto e il
  refresh delle route Blazor.

Queste trasformazioni interessano soltanto l'artifact: il file sorgente
`wwwroot/index.html` conserva il base path `/` necessario allo sviluppo locale.

## Verifica del deploy

Il deploy effettivo può essere verificato soltanto dopo il merge e l'esecuzione del
workflow su GitHub. Nella pagina **Actions** tutti e tre i job devono risultare verdi. Il
job `deploy-pages` associa `steps.deployment.outputs.page_url` all'URL dell'environment
`github-pages`: aprire il riepilogo del job o il link dell'environment e verificare che
l'URL prodotto corrisponda al sito atteso.

Controllare almeno:

- apertura dell'URL principale;
- caricamento di CSS, JavaScript e file `_framework` senza errori 404;
- login con un profilo demo;
- refresh di una route interna, per esempio `/members`;
- persistenza della sessione dopo il refresh.

## Troubleshooting

### `Get Pages site failed` o risposta `Not Found`

Questi messaggi nello step **Configure GitHub Pages** indicano normalmente che il sito
Pages non è ancora abilitato, che la sorgente non è impostata su GitHub Actions oppure
che il job non può leggere la configurazione Pages.

1. un amministratore verifichi **Settings → Pages → Build and deployment → Source →
   GitHub Actions**;
2. verificare nel file del workflow che il job `build-pages` dichiari `pages: read`;
3. dopo aver salvato la configurazione, rieseguire il workflow fallito dalla pagina
   **Actions**, oppure attendere un nuovo push su `main`;
4. non aggiungere `enablement: true`, PAT o secret per aggirare l'errore: il normale
   `GITHUB_TOKEN`, con i permessi minimi dichiarati, è sufficiente dopo l'abilitazione
   amministrativa.

Una risposta `Not Found` può quindi riferirsi alla configurazione Pages assente, non a un
artifact mancante. Se **Configure GitHub Pages** riesce ma il deploy fallisce, controllare
separatamente i log di `upload-pages-artifact` e `deploy-pages`.

### Avviso di deprecazione Node 20

Controllare che il run utilizzi la revisione del workflow contenente le major elencate
nella sezione **Pipeline**. Le action JavaScript referenziate direttamente usano Node 24;
non impostare `ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION`. Se l'avviso rimane, identificare
nei log lo step che lo genera e verificare che non provenga da un workflow riutilizzabile
o da un'action diversa da quelle dichiarate in questo repository.

### URL del deploy

Lo step **Deploy GitHub Pages**, identificato come `deployment`, restituisce `page_url`.
Il valore è esposto come URL dell'environment `github-pages`; verificarlo nel riepilogo
del job e poi controllare base path e asset nel browser. Per un project site l'URL deve
servire l'applicazione sotto `/<nome-repository>/`; per `<account>.github.io` deve servirla
alla radice `/`.

## Limitazioni

- GitHub Pages ospita esclusivamente file statici e non rende sicura l'autenticazione
  simulata;
- ogni browser mantiene il proprio dataset e la propria sessione;
- un dominio personalizzato pubblicato alla radice richiederebbe di configurare
  esplicitamente il base path `/` nello step di preparazione;
- il workflow distribuisce solo `main` e non crea preview per le pull request.
