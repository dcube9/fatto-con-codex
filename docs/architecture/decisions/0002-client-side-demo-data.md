# ADR 0002: persistenza locale del dataset demo

- Stato: accettata
- Data: 2026-09-09

## Contesto

La demo deve conservare le modifiche dopo un refresh, pur senza introdurre un backend.

## Decisione

Il dataset verrà caricato da un JSON versionato e memorizzato in IndexedDB. Se lo storage
non è disponibile, l'applicazione potrà continuare in memoria mostrando un avviso. Le
schede dello stesso browser potranno coordinarsi in modalità best-effort tramite
`BroadcastChannel`.

## Conseguenze

- La cancellazione dei dati del sito elimina le modifiche locali.
- Modalità privata e limiti di quota possono ridurre la persistenza.
- Le evoluzioni del modello richiederanno una strategia di migrazione dello schema.
- Importazione ed esportazione potranno essere aggiunte in un incremento successivo.
