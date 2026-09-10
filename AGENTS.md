# Repository instructions

## Git workflow

Il branch `main` è protetto e rappresenta il codice stabile del progetto.

Non lavorare direttamente su `main`.

Per una nuova attività di implementazione:

1. Parti dalla versione aggiornata di `main`.
2. Crea un branch dedicato all'attività.
3. Esegui tutte le modifiche esclusivamente nel branch dedicato.
4. Esegui i test e le verifiche disponibili.
5. Crea commit chiari e logicamente separati.

Per proseguire un'attività esistente, continua sul branch già associato all'attività senza
crearne uno nuovo a ogni messaggio.

Nel flusso Codex Cloud di questo progetto, Codex prepara le modifiche, i commit, le
verifiche e il titolo e la descrizione della Pull Request. La pubblicazione del lavoro e
la creazione della Pull Request vengono avviate dall'utente tramite **Create PR**
nell'interfaccia.

L'agente:

* non deve eseguire `git push` dal terminale né usare altri comandi o API per sostituire
  il passaggio **Create PR**;
* non deve configurare token, password, chiavi SSH o credential helper per aggirare
  errori di autenticazione;
* deve distinguere chiaramente il lavoro presente nel container da quello effettivamente
  pubblicato su GitHub;
* non deve dichiarare che una Pull Request esiste senza averne conferma;
* non deve eseguire push direttamente su `main`, effettuare merge, attivare auto-merge o
  modificare branch protection, ruleset o altri controlli GitHub;
* non deve utilizzare force push su `main`.

## Branch naming

Utilizza questi prefissi:

* `feature/<nome>` per nuove funzionalità
* `fix/<nome>` per correzioni
* `refactor/<nome>` per refactoring
* `docs/<nome>` per modifiche alla documentazione
* `test/<nome>` per attività principalmente relative ai test
* `chore/<nome>` per manutenzione tecnica

Usa nomi brevi, descrittivi e in inglese.

Esempi:

`feature/initial-application`
`feature/user-login`
`fix/form-validation`
`docs/update-readme`

## Pull Requests

Ogni modifica destinata a `main` deve essere presentata tramite Pull Request.

Codex prepara titolo e descrizione della Pull Request; nel flusso Codex Cloud la
pubblicazione e la creazione effettiva della Pull Request spettano all'utente tramite
**Create PR**. Finché questo passaggio non è confermato, la Pull Request è soltanto
proposta e non deve essere presentata come esistente su GitHub.

La Pull Request deve indicare almeno:

* scopo dell'attività;
* principali modifiche effettuate;
* file o componenti interessati;
* test eseguiti;
* risultato dei test;
* eventuali test non eseguiti e relativo motivo;
* limitazioni note;
* problemi ancora aperti.

Non dichiarare un test come superato se non è stato realmente eseguito.

## Safety

Non inserire nel repository:

* password;
* API key;
* access token;
* private key;
* credenziali;
* connection string contenenti segreti;
* dati personali o sensibili non necessari.

Non modificare le protezioni GitHub del repository.

Non eliminare branch remoti senza una richiesta esplicita.

Non riscrivere la cronologia Git esistente senza una richiesta esplicita.

Se un'attività richiede di aggirare queste regole, fermati e segnala il problema invece di procedere.

## Development

Prima di considerare completata un'attività:

1. controlla le modifiche prodotte;
2. esegui i test pertinenti disponibili;
3. esegui la build, se applicabile;
4. verifica che non siano stati aggiunti accidentalmente segreti o file temporanei;
5. aggiorna la documentazione quando necessario.

Mantieni le modifiche strettamente pertinenti all'attività richiesta.

Evita refactoring non richiesti durante l'implementazione di una funzionalità o di una correzione.
