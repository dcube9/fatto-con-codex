# Persistenza client-side e gestione del dataset demo

## Scopo e sorgente dei dati

ViteKlub è un'applicazione Blazor WebAssembly senza backend. Tutti i dati sono fittizi e
restano nel profilo del browser corrente: non vengono inviati a servizi remoti né
sincronizzati con altri dispositivi. Il seed distribuito con l'applicazione è
`src/ViteKlub.Web/wwwroot/data/initial-dataset.json`; l'applicazione lo legge come risorsa
statica e non lo modifica durante l'uso o il ripristino.

## Flusso di caricamento

`IDemoDatasetStore` espone alla UI un `DemoDatasetSnapshot`, che contiene il
`DemoDataset` validato, i conteggi delle raccolte e la modalità di storage effettivamente
usata. Contiene inoltre una revisione locale opaca, valida soltanto nell'istanza corrente
dello store. `BrowserDemoDatasetStore` applica questa sequenza:

1. prova a leggere la chiave `current` dal database IndexedDB `viteklub-demo`;
2. se la chiave contiene un dataset, lo deserializza, lo valida e lo restituisce;
3. se IndexedDB è vuoto, scarica il seed, lo deserializza, lo valida e lo salva come
   valore iniziale;
4. se IndexedDB non è disponibile durante il caricamento, carica il seed e conserva il
   dataset in memoria per la durata dell'istanza scoped del servizio.

Un documento JSON malformato o un dataset semanticamente non valido produce un errore
invece di essere accettato.

## Ripristino amministrativo

La route `/demo`, autorizzata esclusivamente per il ruolo `Administrator`, mostra i
metadati, i conteggi e la persistenza effettivi dello snapshot caricato. Il ripristino è
un'operazione distruttiva, preceduta da una conferma esplicita: tutte le modifiche locali
al dataset vengono sostituite e l'operazione non può essere annullata.

`RestoreInitialDatasetAsync` rilegge sempre il seed distribuito, senza riutilizzare il
contenuto eventualmente persistito. Il servizio deserializza il documento ed esegue
`DemoDatasetValidator` **prima** di aggiornare lo storage:

- con IndexedDB disponibile, salva il seed validato sotto la chiave `current` e restituisce
  il nuovo snapshot soltanto dopo il completamento effettivo del salvataggio;
- quando il caricamento ha già rilevato IndexedDB non disponibile, sostituisce il fallback
  in memoria. Questo ripristino è temporaneo e non sopravvive al reload;
- un errore di lettura, deserializzazione, validazione o salvataggio viene propagato alla
  UI. In particolare, un errore di salvataggio IndexedDB non viene trasformato
  indiscriminatamente in fallback e non viene dichiarato come successo: il dataset
  corrente resta invariato.

La pagina aggiorna immediatamente metadati e conteggi usando lo snapshot restituito e
mostra un feedback distinto per IndexedDB e memoria temporanea, senza richiedere un
reload. Il ripristino riguarda soltanto `IDemoDatasetStore`: lo storage della sessione di
autenticazione è separato e la sessione amministrativa corrente rimane attiva.

## Interoperabilità JavaScript

`wwwroot/js/demoStorage.js` pubblica le funzioni `demoStorage.load` e
`demoStorage.save`. Lo script apre IndexedDB alla versione 1 e usa l'object store
`datasets`. Ogni operazione chiude il database al termine.

## Limiti e fallback

- i dati sono esclusivamente locali, dimostrativi e non sincronizzati;
- cancellare i dati del sito elimina anche il dataset persistito;
- in navigazione privata, con IndexedDB disabilitato o in caso di errore JavaScript in
  lettura, il fallback in memoria non sopravvive al ricaricamento della pagina;
- gli errori di scrittura non attivano il fallback, così da non mascherare un mancato
  salvataggio richiesto esplicitamente;
- non sono implementate migrazioni tra versioni schema: un dataset non compatibile viene
  segnalato come errore.
# Persistenza client-side

`IDemoDatasetStore.SaveAsync` valida l’intero dataset, lo serializza esclusivamente con
`DemoDatasetJson` e richiede la revisione dello snapshot su cui il comando è basato. Se
nel frattempo la stessa istanza ha caricato o salvato uno snapshot più recente, il
salvataggio viene rifiutato prima della scrittura. La regola vale anche nel fallback in
memoria e una chiamata priva di una revisione ottenuta da `LoadAsync` non può salvare.
Con
IndexedDB disponibile, lo stato corrente non viene sostituito se la scrittura fallisce;
in fallback viene aggiornata una copia deserializzata mantenuta in memoria. Il round-trip
impedisce allo store di trattenere l’istanza mutabile ricevuta e la cancellazione viene
propagata.

Caricamento e ripristino del seed conservano il comportamento precedente. Il ripristino
sostituisce eventuali modifiche locali con il dataset dimostrativo iniziale. In memoria i
dati non sopravvivono al reload; IndexedDB persiste nello stesso browser. Nessuna delle due
modalità sincronizza dati con server, altri browser, dispositivi o schede concorrenti.
La revisione non sopravvive al reload e non rileva modifiche effettuate da un'altra
scheda dopo l'ultima lettura: è un controllo di consistenza della singola istanza, non un
meccanismo di sincronizzazione tra contesti browser.
