# Gestione degli iscritti

## Scopo e origine dei dati

La directory `/members` e il dettaglio caricano lo snapshot tramite `IDemoDatasetStore`:
usa quindi lo stesso dataset browser già disponibile in IndexedDB, con il fallback in
memoria esistente. Non contatta API remote e non introduce un database server.

Tutti i nominativi e i recapiti provengono esclusivamente dal seed fittizio. Sono dati
dimostrativi, non rappresentano persone reali e non devono essere usati in produzione.

## Lista, ricerca e filtri

La lista responsive mostra numero tessera, nome e cognome, stato, data di iscrizione,
scadenza e classificazione del certificato medico. La ricerca confronta numero tessera,
nome, cognome e nome completo senza distinguere maiuscole e minuscole e normalizza gli
spazi. Il filtro di stato offre tutti, attivo, sospeso e archiviato.

L’ordinamento client-side è disponibile, in entrambe le direzioni, per numero tessera,
cognome e nome, data di iscrizione e scadenza del certificato. L’identificativo stabile
dell’iscritto risolve deterministicamente i valori uguali. La paginazione è interamente
client-side, usa pagine da dieci elementi e mostra il conteggio filtrato.

La logica pura in `ViteKlub.Core.Members.MemberDirectory` opera su collezioni in memoria,
non modifica gli iscritti e non dipende da Razor, JavaScript o IndexedDB. Gestisce anche
la ricerca del dettaglio per identificativo; un identificativo assente produce uno stato
"non trovato" anziché un’eccezione non gestita.

## Certificato medico

La classificazione presentazionale usa la data locale corrente come riferimento nella UI
e accetta una data esplicita nei test:

- **non presente**: la scadenza manca;
- **scaduto**: la scadenza precede la data di riferimento;
- **in scadenza entro 30 giorni**: la scadenza è compresa tra il giorno di riferimento e
  il trentesimo giorno successivo, estremi inclusi;
- **valido**: la scadenza supera i trenta giorni.

Lista e dettaglio presentano sempre un testo esplicito, senza affidare l’informazione al
solo colore. Le date sono formattate nel formato italiano `gg/mm/aaaa`.

## Autorizzazione e stati della UI

`/members` e `/members/{id}` usano l’infrastruttura esistente con `AuthorizeRouteView` e
sono autorizzate per Administrator, Manager, Receptionist e Viewer. Administrator,
Manager e Receptionist possono usare le route realmente protette `/members/new` e
`/members/{id}/edit` e le azioni di stato; il Viewer resta in sola lettura e non vede tali
comandi.

La lista comunica in modo accessibile il caricamento, un errore dello storage, il dataset
senza iscritti e l’assenza di corrispondenze ai filtri. I controlli hanno etichette e sono
utilizzabili da tastiera; la tabella adotta il layout responsive di MudBlazor sui viewport
piccoli.

## Regole e flusso operativo

`MemberManagement` riceve sempre dataset, identificativi, utente demo e timestamp dal
chiamante e produce un nuovo snapshot senza mutare quello sorgente. Il form normalizza
tramite Core gli spazi iniziali, finali e multipli e valida obbligatorietà, formato email,
lunghezze e coerenza delle date. Alla creazione il consenso privacy è obbligatorio e lo
stato iniziale è `Active`; in modifica identificativo, numero tessera, timestamp di
creazione e relazioni restano invariati. La versione aumenta a ogni modifica o transizione
e impedisce di sovrascrivere silenziosamente una versione non più corrente.

Il numero tessera usa il formato `VK-00000`: viene scelto il più piccolo intero positivo
non già presente. La strategia è deterministica, indipendente dall’orologio, funziona con
dataset vuoti e riutilizza il primo intervallo libero senza rinumerare record esistenti.
I valori preesistenti che non corrispondono a `VK-` seguito soltanto da cifre sono
conservati ma ignorati nel calcolo; gli interi conformi sono elaborati senza limite a 32
bit. Prima della creazione il Core controlla comunque il candidato esplicito e rifiuta
un duplicato, anche senza distinzione tra maiuscole e minuscole.

Le sole transizioni ammesse sono `Active → Suspended`, `Suspended → Active`, `Active →
Archived` e `Suspended → Archived`. L’archiviazione è logica e irreversibile dalla UI:
l’iscritto e abbonamenti, accessi e pagamenti collegati non vengono eliminati. Ogni
operazione riuscita aggiunge un evento audit con azione, identificativo entità, attore e
timestamp, senza email, telefono, note o altri dati personali.

## Consistenza e limiti

Il salvataggio riguarda sempre l’intero `DemoDataset`; directory, dettaglio e dashboard lo
rileggono dallo store e la dashboard continua a usare `DashboardProjection`. Ogni snapshot
porta una revisione opaca dell’istanza scoped dello store: un comando ricarica lo snapshot,
applica l’operazione alla versione corrente e deve presentare la stessa revisione durante
il salvataggio. Una revisione superata o una versione dell’iscritto diversa viene rifiutata
con l’invito a ricaricare, senza scrivere il dataset.

La route di modifica distingue un identificativo inesistente da un iscritto archiviato,
che resta consultabile ma non è più modificabile. I comandi si disabilitano durante il
salvataggio. Un errore di persistenza mostra un messaggio italiano non tecnico e conserva
dataset e dettaglio correnti.

La revisione protegge operazioni concorrenti nella **stessa istanza dello store**, quindi
nella stessa sessione applicativa. Non coordina schede concorrenti, reload, browser o
dispositivi differenti; IndexedDB non offre qui sincronizzazione applicativa e non
esistono backend, lock remoti o cache parallele degli iscritti.

## Strategia di verifica della UI

Il progetto non include un framework di rendering dei componenti Razor e il vincolo di
non aggiungere dipendenze impedisce di introdurne uno per questa attività. Le regole pure,
la matrice di autorizzazione, lo storage e `MemberCommandFactory` (incluso l'uso esatto di
timestamp e identificativi iniettati) restano verificabili automaticamente. Rendering,
focus, dialogo, navigazione e comportamento responsive richiedono invece una verifica
manuale nel browser con i quattro ruoli demo e da utente anonimo.
