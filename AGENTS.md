# Repository instructions

## Git workflow

Il branch `main` è protetto e rappresenta il codice stabile del progetto.

Non lavorare direttamente su `main`.

Per una nuova attività:

1. Parti dalla versione aggiornata di `main`.
2. Crea un branch dedicato all'attività.
3. Esegui tutte le modifiche esclusivamente nel branch dedicato.
4. Esegui i test e le verifiche disponibili.
5. Crea commit chiari e logicamente separati.
6. Prepara un titolo e una descrizione per la Pull Request verso `main`.

Quando un'attività esistente viene ripresa, continua a lavorare sul branch già dedicato
all'attività senza crearne un altro e senza ripartire da branch locali appartenenti ad
altri task.

Codex prepara le modifiche, i commit, le verifiche e il titolo e la descrizione proposti
per la Pull Request. La pubblicazione del branch e la creazione della Pull Request sono
invece avviate dall'utente esclusivamente tramite **Create PR**.

Codex non deve:

* eseguire `git push` dal terminale;
* usare API, CLI GitHub o comandi alternativi per pubblicare il branch o creare la Pull
  Request al posto di **Create PR**;
* configurare access token, password, chiavi SSH o credential helper;
* eseguire push direttamente su `main` o utilizzare force push;
* effettuare merge o auto-merge di una Pull Request;
* modificare o aggirare branch protection, ruleset o altri controlli GitHub.

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

Nel riepilogo finale distingui chiaramente il lavoro completato localmente, come commit e
verifiche, da ciò che è stato effettivamente pubblicato su GitHub. Non dichiarare che un
branch è stato pubblicato o che una Pull Request esiste senza una conferma esplicita del
relativo esito.

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
