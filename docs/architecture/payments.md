# Consultazione dei pagamenti

## Scopo e origine dei dati

L'incremento introduce la consultazione in sola lettura della lista e del dettaglio dei
pagamenti demo. La UI carica l'intero `DemoDataset` tramite `IDemoDatasetStore`, quindi
costruisce in memoria una proiezione presentazionale. Non sono presenti backend, API
remote o database server e il seed fittizio esistente non viene modificato.

Ogni `Payment` viene associato tramite `MemberId` a `Member`, tramite l'eventuale
`SubscriptionId` a `MemberSubscription` e tramite `RecordedByUserId` a `DemoUser`. In
caso di identificativi duplicati viene usato deterministicamente il primo elemento
nell'ordine della collezione sorgente. Gli oggetti del dataset non vengono modificati.

## Dati e funzioni di consultazione

La lista mostra data e ora, numero tessera e nome dell'iscritto, importo e valuta,
metodo, stato, riferimento, abbonamento e identificazione minima dell'utente demo
registrante. Le note non sono mostrate né ricercate perché non sono necessarie a questa
consultazione.

La ricerca, senza distinzione tra maiuscole e minuscole e con spazi normalizzati, copre
numero tessera, nome, cognome, nome completo, riferimento e identificazione dell'utente
demo. I filtri coprono tutti i valori di `PaymentMethod` e `PaymentStatus`. Si può
ordinare in entrambe le direzioni per data e ora, numero tessera, cognome e nome,
importo, metodo e stato. L'importo è ordinato come valore numerico originale. Gli ID
forniscono uno spareggio stabile. La paginazione è interamente client-side e conserva il
conteggio totale filtrato; una pagina oltre i risultati restituisce una lista vuota.

## Semantica e formattazione

I metodi sono presentati come **Contanti**, **Carta**, **Bonifico bancario** e **Altro**;
un valore sconosciuto diventa **Metodo non disponibile**. Gli stati sono presentati come
**In attesa**, **Completato**, **Non riuscito** e **Annullato**; un valore sconosciuto
diventa **Stato non disponibile**. Il testo rimane sempre visibile, quindi il significato
non dipende dal colore.

La proiezione conserva `Amount` e `Currency`. L'importo usa esplicitamente la cultura
italiana, due decimali e il codice valuta normalizzato in maiuscolo. Una valuta assente o
non composta da tre lettere viene resa come **Valuta non disponibile** senza interrompere
la pagina. Il riferimento è testo semplice, mai un link: se assente viene mostrato
**Nessun riferimento**.

`OccurredAtUtc` rimane un `DateTimeOffset` e viene ordinato tramite l'istante UTC. La
presentazione converte esplicitamente in UTC e usa il formato italiano
`gg/mm/aaaa HH:mm UTC`; non dipende dal fuso locale della macchina.

## Riferimenti mancanti

Un riferimento non risolvibile non elimina il pagamento e non genera eccezioni. La UI
mostra **Iscritto non disponibile**, **Abbonamento non disponibile** o **Utente demo non
disponibile** e non crea link verso risorse inesistenti. Un `SubscriptionId` nullo è una
condizione diversa e viene mostrato come **Nessun abbonamento associato**. Ricerca,
filtri, ordinamento e paginazione continuano a operare deterministicamente.

## Autorizzazione e stati UI

Le route `/payments` e `/payments/{id}` richiedono i ruoli Administrator, Manager o
Receptionist tramite l'infrastruttura esistente basata su `AuthorizeRouteView`. Il ruolo
Viewer è escluso. Tutti gli utenti autorizzati possono soltanto consultare, filtrare,
ordinare, paginare e seguire i link in sola lettura verso iscritti e abbonamenti esistenti.

La pagina distingue caricamento, errore, dataset senza pagamenti e assenza di risultati
dopo ricerca o filtri. Un ID di dettaglio sconosciuto produce un messaggio controllato.
Controlli etichettati, messaggi con semantica di stato e tabella responsive supportano
navigazione da tastiera e viewport ridotti.

## Limitazioni e funzionalità escluse

I dati sono esclusivamente dimostrativi. Non sono previste persistenza di modifiche,
registrazione, associazione, modifica, cancellazione, annullamento operativo, rimborso,
importazione, esportazione, ricevute, fatture, validazione finanziaria, gateway di
pagamento o gestione di carte e coordinate bancarie. Non viene introdotto alcun CRUD e
non vengono modificate autenticazione, deploy o le altre aree di consultazione.
