# Architettura di ViteKlub

ViteKlub è un prototipo per la gestione di una palestra distribuito come applicazione
Blazor WebAssembly standalone su hosting statico.

## Perimetro

Le aree applicative sono dashboard operativa, iscritti, abbonamenti, accessi, pagamenti, utenti demo
e gestione demo. Corsi, calendario, istruttori e sale sono esplicitamente esclusi.

## Vincoli

- Non è presente un backend.
- Ogni browser mantiene un dataset indipendente.
- I dati non vengono sincronizzati tra dispositivi.
- Autenticazione, ruoli e autorizzazioni hanno esclusivamente finalità dimostrative.
- Non devono essere utilizzati dati personali, finanziari o credenziali reali.
- Il dataset verrà inizializzato da JSON e conservato in IndexedDB.

## Progetti

- `ViteKlub.Core` contiene modelli e regole applicative indipendenti dalla UI.
- `ViteKlub.Web` contiene la SPA Blazor WebAssembly e i servizi browser.
- `ViteKlub.Core.Tests` contiene i test unitari del nucleo applicativo.

I dettagli della persistenza e delle singole aree saranno introdotti tramite incrementi
separati.
