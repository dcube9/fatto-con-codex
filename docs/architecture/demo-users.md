# Directory degli utenti demo

La directory **Utenti demo** espone una consultazione esclusivamente dimostrativa e in
sola lettura degli utenti inclusi nel dataset client-side. Non interroga backend, database
o servizi remoti e non introduce credenziali reali. I dati provengono dalla proprietà
`Users` del `DemoDataset` caricato tramite lo stesso storage browser usato dalle altre
directory.

## Consultazione

La logica pura in `DemoUserDirectory`:

- ordina per nome visualizzato, poi per username, senza distinzione tra maiuscole e
  minuscole, e usa l'identificativo come criterio finale deterministico;
- cerca senza distinzione tra maiuscole e minuscole in nome visualizzato e username,
  normalizzando gli spazi inseriti nella ricerca;
- filtra per l'unico ruolo assegnato previsto dal modello corrente;
- permette di combinare ricerca e ruolo e gestisce collezioni vuote, valori testuali vuoti
  e identificativi assenti senza eccezioni.

L'elenco e il dettaglio mostrano soltanto i campi non sensibili del modello: identificativo,
nome visualizzato, username, ruolo, stato, versione e date tecniche del record. Password,
token, segreti e altri dati di autenticazione non sono visualizzati. Il dataset corrente
non contiene questi campi e la directory non deve essere estesa automaticamente a eventuali
campi sensibili aggiunti in futuro.

## Autorizzazione e stati

Le route `/demo-users` e `/demo-users/{id}` richiedono il ruolo `Administrator`, riusando
la matrice dei ruoli e il reindirizzamento alla pagina di accesso negato dell'applicazione.
Anche la voce di navigazione è visibile soltanto agli amministratori. Un identificativo
non presente produce lo stato esplicito **Utente non trovato**, senza generare eccezioni.

Questa funzionalità resta interamente locale al browser, usa esclusivamente dati fittizi e
non costituisce un sistema reale di amministrazione degli utenti.
