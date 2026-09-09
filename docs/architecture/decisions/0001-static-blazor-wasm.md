# ADR 0001: applicazione Blazor WebAssembly interamente statica

- Stato: accettata
- Data: 2026-09-09

## Contesto

ViteKlub deve essere pubblicata con un unico deploy statico gratuito. Uno stato condiviso
tra browser richiederebbe un runtime o un servizio dati separato.

## Decisione

L'applicazione sarà una Blazor WebAssembly standalone senza backend. Il dataset iniziale
verrà distribuito come asset JSON e ogni browser ne conserverà una copia indipendente in
IndexedDB. L'autenticazione e i ruoli saranno simulati esclusivamente nel client.

## Conseguenze

- Non esiste sincronizzazione tra browser o dispositivi.
- Le autorizzazioni non costituiscono un confine di sicurezza.
- Il reset riguarda soltanto il browser corrente.
- L'applicazione non può essere utilizzata con dati reali o sensibili.
- Build e pubblicazione producono esclusivamente asset statici.
