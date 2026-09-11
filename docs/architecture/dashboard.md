# Dashboard operativa

La route `/` presenta una sintesi operativa costruita interamente nel browser dallo stesso
`DemoDatasetSnapshot` usato dalle directory. La pagina carica lo snapshot tramite
`IDemoDatasetStore` e delega tutti i conteggi, i filtri e gli ordinamenti indipendenti
dall'interfaccia a `DashboardProjection` in `ViteKlub.Core`. Non usa API remote, un database
server o l'orologio del dispositivo.

## Data di riferimento e indicatori

Ogni regola temporale usa esclusivamente `DemoDataset.ReferenceDate`. Gli intervalli sono
espressi in giorni UTC e hanno estremi inclusi:

* un abbonamento è attivo quando ha stato `Active` e la data di riferimento è compresa tra
  `StartsOn` ed `EndsOn`; gli abbonamenti terminati prima di tale data sono esclusi;
* "in scadenza entro 30 giorni" indica una scadenza compresa tra la data di riferimento e
  `ReferenceDate + 30 giorni`, inclusi entrambi gli estremi, e si applica agli abbonamenti
  attivi;
* un certificato è già scaduto se la sua scadenza precede la data di riferimento; è in
  scadenza se termina tra la data di riferimento e il limite di 30 giorni, inclusi;
* gli accessi del giorno hanno timestamp UTC la cui data coincide con la data di
  riferimento. `Granted` è consentito e `Denied` è rifiutato; `Cancelled` concorre al
  numero registrato ma non ai due sotto-conteggi;
* soltanto i pagamenti con stato `Completed` concorrono al conteggio e alla somma. Il
  totale è presentato in euro con la cultura italiana.

Gli indicatori riportano inoltre iscritti totali e iscritti nei tre stati `Active`,
`Suspended` e `Archived`. Raccolte vuote producono conteggi a zero e stati vuoti espliciti,
non valori dimostrativi.

## Riepiloghi

Gli abbonamenti in scadenza sono ordinati per data, nome dell'iscritto e identificativo;
i certificati per data, nome e identificativo. Gli accessi recenti includono soltanto
eventi non successivi alla data di riferimento e sono ordinati per timestamp decrescente
e identificativo. Ciascun elenco mostra al massimo cinque elementi. Le etichette degli
esiti degli accessi riusano `AccessDirectory`.

## Persistenza e autorizzazione

La dashboard mostra la versione del dataset e la modalità effettiva dello snapshot.
Con IndexedDB i dati locali possono sopravvivere al reload; nel fallback in memoria la
pagina avverte esplicitamente che non lo faranno. Le azioni di ripristino restano nella
gestione demo e non sono replicate.

La route richiede uno dei quattro ruoli demo autenticati e il redirect degli anonimi resta
affidato a `AuthorizeRouteView` e `RouteAuthorizationRedirect`. I collegamenti visibili
seguono la matrice centralizzata in `DemoNavigation`/`DemoRoles`:

| Collegamento | Administrator | Manager | Receptionist | Viewer |
| --- | --- | --- | --- | --- |
| Iscritti, abbonamenti, accessi | sì | sì | sì | sì |
| Pagamenti | sì | sì | sì | no |
| Gestione demo | sì | no | no | no |

## Limiti

I dati sono esclusivamente locali, fittizi e non sincronizzati. La dashboard non aggiorna
automaticamente lo snapshot dopo il caricamento e non offre grafici, notifiche remote o
azioni di modifica. Il fallback in memoria perde ogni modifica al reload.
