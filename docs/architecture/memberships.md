# Consultazione degli abbonamenti

## Scopo e origine dei dati

L'incremento introduce la lista `/memberships` e il dettaglio `/memberships/{id}` in sola
lettura. Entrambe le pagine caricano lo snapshot demo tramite `IDemoDatasetStore`; il
browser continua quindi a usare IndexedDB e il fallback in memoria già esistenti. Non
sono presenti backend, API remote o persistenza di modifiche. Tutti i nomi e gli importi
mostrati appartengono esclusivamente al seed fittizio distribuito con l'applicazione.

## Proiezione presentazionale

`MembershipDirectory.Project` associa in memoria ogni `MemberSubscription` al `Member`
tramite `MemberId` e al `MembershipPlan` tramite `MembershipPlanId`. Produce nuovi record
presentazionali senza modificare gli oggetti del dataset. La vista mostra numero tessera,
nome e cognome, piano e tipo, stato, date di inizio e fine, ingressi residui e prezzo di
acquisto con valuta; non espone altri dati personali.

Un riferimento non risolvibile non elimina l'abbonamento e non interrompe la pagina:
vengono mostrati «Iscritto non disponibile» o «Piano non disponibile». Il collegamento al
dettaglio iscritto compare solo quando l'iscritto esiste. Ricerca, filtri, ordinamento e
paginazione rimangono deterministici anche per questi record.

## Consultazione

La ricerca, senza distinzione tra maiuscole e minuscole e con spazi normalizzati, considera
numero tessera, nome, cognome, nome completo (in entrambi gli ordini) e nome del piano.
I filtri comprendono tutti gli stati (`Scheduled`, `Active`, `Suspended`, `Expired`,
`Cancelled`) e i tipi di piano (`TimeBased`, `EntryBased`), oltre all'opzione «Tutti».

L'ordinamento crescente o decrescente è disponibile per tessera, cognome e nome, piano,
inizio, fine e stato. Usa confronti ordinali indipendenti dalla cultura e l'identificativo
come spareggio stabile. La paginazione è client-side e il conteggio indica il totale dopo
ricerca e filtri.

Le date usano il formato italiano `gg/mm/aaaa`; gli importi usano separatori italiani e
mostrano esplicitamente il codice valuta. Stati e tipi hanno etichette italiane testuali.
Per un piano a ingressi si mostra il residuo oppure «Dato non disponibile»; per un piano a
tempo si mostra sempre «Non applicabile».

## Autorizzazione, stati e accessibilità

Le route mantengono l'infrastruttura `AuthorizeRouteView` e sono autorizzate per
Administrator, Manager, Receptionist e Viewer. Tutti, Viewer incluso, possono soltanto
consultare, cercare, filtrare, ordinare, cambiare pagina, aprire il dettaglio e seguire il
link all'iscritto. Non sono esposte azioni di modifica.

La UI distingue con testo accessibile caricamento, errore, dataset senza abbonamenti e
nessun risultato per i filtri. I controlli hanno etichette, gli stati non dipendono dal
solo colore e la tabella passa alla presentazione responsive di MudBlazor sugli schermi
piccoli. Un identificativo di dettaglio inesistente produce un avviso gestito.

## Limitazioni e funzionalità escluse

Questa è una consultazione dimostrativa locale. Sono esclusi creazione, rinnovo, modifica,
sospensione operativa, annullamento, eliminazione, importazione, esportazione, pagamenti e
qualsiasi CRUD di piani, accessi o utenti e non aggiunge comandi sugli iscritti all'area
abbonamenti; la gestione iscritti resta disponibile nelle route dedicate. Non vengono
apportate modifiche al seed, all'autenticazione o al deploy.
