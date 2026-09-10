# ViteKlub

Prototipo statico Blazor WebAssembly per la gestione di una palestra.

## Development status

Il bootstrap della soluzione è in corso. Il perimetro comprende dashboard, iscritti,
abbonamenti, accessi, pagamenti, utenti demo e gestione demo.

## Architettura

ViteKlub viene eseguita interamente nel browser e non utilizza un backend. I dati demo
saranno conservati localmente e non saranno condivisi tra browser o dispositivi. Consulta
[`docs/architecture/overview.md`](docs/architecture/overview.md) per i dettagli e i limiti.
La strategia di persistenza nel browser è descritta in
[`docs/architecture/client-storage.md`](docs/architecture/client-storage.md).

## Prerequisiti

- .NET SDK 10.0.100 o una feature band successiva di .NET 10;
- un browser moderno.

## Avvio locale

```bash
dotnet restore ViteKlub.slnx
dotnet run --project src/ViteKlub.Web/ViteKlub.Web.csproj
```

## Build e test

```bash
dotnet build ViteKlub.slnx --configuration Release --no-restore
dotnet test ViteKlub.slnx --configuration Release --no-build
dotnet format ViteKlub.slnx --verify-no-changes --no-restore
```

> ViteKlub è esclusivamente una demo: autenticazione e ruoli sono simulati e non devono
> essere utilizzati dati personali, credenziali o informazioni finanziarie reali.
