# Persistenza client-side del dataset demo

## Scopo

ViteKlub è un'applicazione Blazor WebAssembly senza backend. Il dataset della demo resta
nel browser e non viene trasmesso a un servizio remoto. Questo incremento introduce il
solo caricamento persistente del dataset; autenticazione e operazioni di modifica sono
fuori ambito.

## Flusso di caricamento

`IDemoDatasetStore` espone alla UI uno snapshot composto dal `DemoDataset` validato e
dalla modalità di storage effettivamente usata. `BrowserDemoDatasetStore` applica questa
sequenza:

1. prova a leggere la chiave `current` dal database IndexedDB `viteklub-demo`;
2. se la chiave contiene un dataset, lo deserializza, lo valida e lo restituisce;
3. se IndexedDB è vuoto, scarica `wwwroot/data/initial-dataset.json`, lo valida e lo
   salva come valore iniziale;
4. se IndexedDB non è disponibile, carica il seed e conserva il dataset in memoria per
   la durata dell'istanza scoped del servizio.

Un dataset malformato o non valido produce un errore invece di essere accettato. La
dashboard rende espliciti gli stati di caricamento ed errore e, a caricamento completato,
mostra la versione del dataset, il numero di iscritti e la modalità di persistenza.

## Interoperabilità JavaScript

`wwwroot/js/demoStorage.js` pubblica le funzioni `demoStorage.load` e
`demoStorage.save`. Lo script apre IndexedDB alla versione 1 e usa l'object store
`datasets`. Ogni operazione chiude il database al termine.

## Limiti e fallback

- i dati sono locali al profilo e al browser correnti e non vengono sincronizzati;
- cancellare i dati del sito elimina anche il dataset persistito;
- in navigazione privata, con IndexedDB disabilitato o in caso di errore JavaScript, il
  fallback in memoria non sopravvive al ricaricamento della pagina;
- questo incremento non implementa migrazioni tra versioni schema: un dataset non
  compatibile viene segnalato come errore.
