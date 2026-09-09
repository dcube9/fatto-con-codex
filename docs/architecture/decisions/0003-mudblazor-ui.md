# ADR 0003: MudBlazor come libreria UI

- Stato: accettata
- Data: 2026-09-09

## Contesto

Il prototipo richiede un'interfaccia gestionale completa, responsive e coerente, con
tempi di implementazione contenuti.

## Decisione

ViteKlub utilizzerà MudBlazor per layout, navigazione, form, tabelle, dialoghi e feedback.
L'interfaccia sarà in italiano e adotterà un'identità moderna e sportiva.

## Conseguenze

- La versione della libreria viene centralizzata in `Directory.Packages.props`.
- I componenti applicativi devono mantenere accessibilità e navigazione da tastiera.
- Gli stili personalizzati devono integrare MudBlazor senza duplicarne i componenti.
