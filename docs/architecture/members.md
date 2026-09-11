# Area Iscritti in sola lettura

## Scopo e origine dei dati

Questo incremento sostituisce il placeholder `/members` con una directory consultabile e
un dettaglio in sola lettura. La pagina carica lo snapshot tramite `IDemoDatasetStore`:
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
sono autorizzate per Administrator, Manager, Receptionist e Viewer. Il Viewer vede gli
stessi dati in sola lettura degli altri ruoli: nessun ruolo dispone qui di comandi di
creazione, modifica, archiviazione, eliminazione, importazione o esportazione.

La lista comunica in modo accessibile il caricamento, un errore dello storage, il dataset
senza iscritti e l’assenza di corrispondenze ai filtri. I controlli hanno etichette e sono
utilizzabili da tastiera; la tabella adotta il layout responsive di MudBlazor sui viewport
piccoli.

## Limitazioni e funzionalità escluse

L’area è dimostrativa, interamente client-side e non persiste alcuna modifica. Non include
CRUD degli iscritti, archiviazione operativa, import/export, backend, né CRUD di
abbonamenti, accessi o pagamenti. Non modifica seed, autenticazione, matrice globale dei
ruoli, deploy o gli altri placeholder. Non sono presenti azioni amministrative o problemi
aperti noti nell’ambito di questo incremento.
