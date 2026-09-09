# Modello dati demo

Il dataset iniziale di ViteKlub è un asset JSON versionato caricato dal browser. Tutti i
dati sono fittizi e appartengono esclusivamente alla demo.

## Radice del dataset

`DemoDataset` contiene:

- `schemaVersion`, usato per verificare la compatibilità strutturale;
- `datasetVersion`, identificativo leggibile della revisione del seed;
- `referenceDate`, data rispetto alla quale verranno riallineate le date della demo;
- utenti demo;
- iscritti;
- piani di abbonamento;
- abbonamenti sottoscritti;
- accessi;
- pagamenti;
- eventi di audit dimostrativi.

Il seed `initial-v1` usa il primo settembre 2026 come data di riferimento deterministica.
Una futura fase di inizializzazione in IndexedDB riallineerà le date alla data corrente
senza modificare le durate relative.

## Entità

Ogni entità possiede un identificativo GUID, timestamp UTC di creazione e aggiornamento e
una versione positiva. Gli identificativi sono univoci nell'intero dataset.

### Iscritto

Contiene numero tessera, anagrafica minima, contatti fittizi, stato, data di iscrizione,
scadenza del certificato medico simulato, contatto di emergenza e consenso privacy demo.

### Piano e abbonamento

Il piano descrive durata, prezzo e accesso temporale oppure a ingressi. L'abbonamento
collega un iscritto a un piano e conserva intervallo, stato, prezzo di acquisto ed
eventuali ingressi residui.

### Accesso

Registra il check-in, il relativo esito, l'eventuale causa di rifiuto, la sorgente e
l'utente demo che ha registrato l'operazione.

### Pagamento

Registra esclusivamente importi e metodi fittizi. Non contiene numeri di carta, coordinate
bancarie o dati utilizzabili per un pagamento reale.

### Utente e audit

Gli utenti rappresentano i quattro ruoli dimostrativi. Gli eventi di audit migliorano la
credibilità dell'interfaccia, ma non costituiscono una traccia affidabile perché vivono nel
browser e possono essere modificati.

## Validazione

Il loader rifiuta proprietà JSON sconosciute ed enum non riconosciuti. Il validatore
controlla almeno:

- versione schema;
- metadati obbligatori;
- identificativi vuoti o duplicati;
- timestamp e date incoerenti;
- numero tessera e username duplicati;
- assenza di un Administrator attivo;
- riferimenti a entità inesistenti;
- prezzi, importi e ingressi negativi;
- coerenza tra tipo del piano e numero di ingressi;
- valuta diversa da EUR;
- esiti di accesso incompatibili con la causa di rifiuto.

## Dimensioni iniziali

Il file contiene 4 utenti, 75 iscritti, 8 piani, 90 abbonamenti, 350 accessi, 150
pagamenti e 25 eventi di audit. Le dimensioni sono sufficienti per alimentare elenchi,
filtri e dashboard senza introdurre dati personali reali.
