# Repository instructions

## Git workflow

Il branch `main` è protetto e rappresenta il codice stabile del progetto.

Non lavorare direttamente su `main`.

Per ogni attività:

1. Parti dalla versione aggiornata di `main`.
2. Crea un branch dedicato all'attività.
3. Esegui tutte le modifiche esclusivamente nel branch dedicato.
4. Esegui i test e le verifiche disponibili.
5. Crea commit chiari e logicamente separati.
6. Pubblica il branch su GitHub.
7. Crea una Pull Request verso `main`.
8. Non effettuare autonomamente il merge della Pull Request.
9. Non eseguire push direttamente su `main`.
10. Non utilizzare `force push` su `main`.
11. Non aggirare branch protection, ruleset o altri controlli GitHub.

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
