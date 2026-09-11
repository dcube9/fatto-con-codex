# Consultazione degli accessi

## Scopo e origine dei dati

L'incremento rende disponibili la lista `/accesses` e il dettaglio
`/accesses/{id}` esclusivamente in lettura. Le pagine caricano lo snapshot demo tramite
`IDemoDatasetStore`: non contattano API remote e non introducono un database server. I
dati sono quelli interamente fittizi del seed browser esistente.

La proiezione in memoria `AccessDirectory` associa ogni `GymAccess` al `Member` indicato
da `MemberId`, al `MemberSubscription` facoltativo indicato da `SubscriptionId` e al
`DemoUser` indicato da `RecordedByUserId`. Gli oggetti del dataset non vengono modificati.
La UI mostra soltanto data e ora, tessera e nome demo dell'iscritto, esito, origine,
motivo del rifiuto, riferimento all'abbonamento e identificazione minima dell'utente demo
registrante. Il dettaglio offre collegamenti alle viste in sola lettura di iscritto e
abbonamento soltanto quando il riferimento è risolvibile.

## Consultazione della lista

La ricerca, senza distinzione tra maiuscole e minuscole e con gli spazi normalizzati,
copre numero tessera, nome, cognome, nome completo e identificazione dell'utente demo.
I filtri coprono tutti i valori di `AccessOutcome`, `AccessSource` e
`AccessDenialReason`, anche in combinazione con la ricerca. È possibile ordinare in
entrambe le direzioni per istante, numero tessera, cognome e nome, esito e origine.
L'identificativo dell'accesso è lo spareggio stabile e deterministico. I risultati sono
paginati lato client, riportano il conteggio filtrato e tutti i criteri possono essere
azzerati insieme.

## Semantica presentazionale

Gli esiti sono presentati in italiano come **Consentito**, **Negato** e **Annullato**,
sempre con testo e non soltanto tramite colore. Le origini sono **Reception** e
**Simulatore**. Per un accesso consentito o annullato il motivo del rifiuto è **Non
applicabile**. Per un accesso negato viene tradotto il motivo specifico; `None`, un valore
sconosciuto o una combinazione incoerente producono l'esplicito **Motivo del rifiuto non
disponibile**. Il motivo di annullamento non viene esposto perché non è necessario alla
consultazione richiesta.

La proiezione conserva il `DateTimeOffset` originale. La formattazione converte
esplicitamente l'istante in UTC e usa il formato italiano `gg/mm/aaaa HH:mm UTC`; sia la
lista sia il dettaglio dichiarano il fuso mostrato. Ordinamento e test usano l'istante UTC
e non dipendono dal fuso locale o dall'orologio della macchina.

## Riferimenti mancanti

Un riferimento non risolvibile non elimina l'accesso e non genera collegamenti errati:
la proiezione usa **Iscritto non disponibile**, **Abbonamento non disponibile** o
**Utente demo non disponibile**. Un `SubscriptionId` assente è invece indicato con
**Nessun abbonamento associato**, distinguendolo da un identificativo presente ma non
risolvibile. Ricerca, filtri, ordinamento, paginazione e dettaglio rimangono deterministici
anche in questi casi. Un identificativo di dettaglio inesistente genera uno stato
informativo gestito, non un'eccezione non gestita.

## Autorizzazione, stati e accessibilità

Le route mantengono `AuthorizeRouteView` e richiedono uno dei ruoli Administrator,
Manager, Receptionist o Viewer. Tutti, incluso Viewer, possono consultare, cercare,
filtrare, ordinare, paginare, aprire il dettaglio e seguire i collegamenti in sola lettura;
nessun ruolo dispone qui di azioni operative.

La pagina distingue caricamento accessibile, errore, dataset privo di accessi e nessun
risultato per i criteri scelti. I controlli hanno label testuali e la tabella usa il
comportamento responsive di MudBlazor, che trasforma le colonne in righe etichettate sui
viewport ridotti mantenendo la navigazione da tastiera.

## Limitazioni e funzionalità escluse

L'area è dimostrativa, usa esclusivamente dati fittizi ed esegue proiezione e query in
memoria. Non registra, simula, modifica, annulla operativamente, elimina, importa o
esporta accessi; non persiste cambiamenti e non applica regole operative d'ingresso o
decrementi. Sono esclusi anche CRUD di iscritti, abbonamenti, piani, pagamenti e utenti,
nonché modifiche ad autenticazione, seed e deploy. Non risultano problemi aperti noti
nello scope della consultazione.
