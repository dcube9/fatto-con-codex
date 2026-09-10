# Persistenza nel browser

ViteKlub conserva un'unica copia attiva del dataset demo in IndexedDB. Il servizio
`BrowserDemoDatasetStore` nasconde l'interoperabilità JavaScript e offre operazioni di
lettura, salvataggio e reset tipizzate.

## Inizializzazione

1. Il servizio verifica che IndexedDB sia disponibile.
2. Se esiste un dataset locale, lo deserializza e lo valida prima di esporlo.
3. Al primo utilizzo scarica `data/initial-dataset.json`, lo valida e lo salva.
4. Lo snapshot viene mantenuto anche in memoria per evitare letture ripetute.

Il dataset non valido non viene accettato né scritto nello storage.

## Fallback

Se l'API IndexedDB non è presente o una sua operazione fallisce, il servizio passa alla
modalità in memoria. L'applicazione resta utilizzabile, ma le modifiche vengono perse alla
chiusura della scheda. La dashboard mostra chiaramente quale modalità è attiva.

## Limiti

- Lo storage appartiene al browser e all'origine del sito.
- Browser e dispositivi diversi non condividono i dati.
- La cancellazione dei dati del sito elimina il dataset locale.
- Il coordinamento tra più schede e le migrazioni saranno aggiunti in incrementi dedicati.
