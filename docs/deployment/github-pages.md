# Deploy su GitHub Pages

## Obiettivo

Il workflow `.github/workflows/ci.yml` pubblica la SPA Blazor WebAssembly su GitHub Pages
dopo ogni push su `main`. Le pull request eseguono soltanto build e test: non producono
deploy e non possono modificare il sito pubblico.

Il deploy non introduce backend, database remoto o segreti applicativi. Dataset e
sessione demo continuano a risiedere esclusivamente nel browser del visitatore.

## Configurazione iniziale del repository

Prima della prima esecuzione, un amministratore del repository deve aprire **Settings →
Pages**, nella sezione **Build and deployment**, e impostare **Source** su **GitHub
Actions**. La configurazione deve essere salvata prima di eseguire nuovamente il workflow:
il workflow non abilita Pages in autonomia. Non sono necessari branch di pubblicazione,
personal access token o credenziali aggiuntive.

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

Il job di build dispone dei permessi di lettura dei contenuti e della configurazione
Pages. Il job finale riceve esclusivamente `pages: write` e `id-token: write`, necessari
al deploy tramite GitHub Actions.

## Risoluzione dei problemi

Se `Configure GitHub Pages` restituisce `Get Pages site failed` con risposta `Not Found`,
verificare prima di tutto che **Settings → Pages → Build and deployment → Source** sia
impostato su **GitHub Actions**. La risposta indica che il Pages site non è ancora stato
abilitato o non è leggibile dal workflow. Il job `build-pages` dichiara esplicitamente
`pages: read` per consentire questa verifica.

Il workflow usa versioni delle action basate sul runtime Node corrente dei runner GitHub.
Non impostare `ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION`: la variabile riabiliterebbe
temporaneamente un runtime deprecato invece di correggere la causa dell'avviso.

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
workflow su GitHub. Nella pagina **Actions** tutti e tre i job devono risultare verdi; il
job `deploy-pages` espone l'URL pubblicato nell'environment `github-pages`.

Controllare almeno:

- apertura dell'URL principale;
- caricamento di CSS, JavaScript e file `_framework` senza errori 404;
- login con un profilo demo;
- refresh di una route interna, per esempio `/members`;
- persistenza della sessione dopo il refresh.

## Limitazioni

- GitHub Pages ospita esclusivamente file statici e non rende sicura l'autenticazione
  simulata;
- ogni browser mantiene il proprio dataset e la propria sessione;
- un dominio personalizzato pubblicato alla radice richiederebbe di configurare
  esplicitamente il base path `/` nello step di preparazione;
- il workflow distribuisce solo `main` e non crea preview per le pull request.
