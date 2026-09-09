# Autenticazione e ruoli demo

ViteKlub simula autenticazione e autorizzazione interamente nel browser. Questo meccanismo
serve a dimostrare la navigazione e i permessi dell'interfaccia, ma non costituisce un
confine di sicurezza.

## Accesso

La pagina di login presenta i profili attivi contenuti nel dataset. La selezione non
richiede password. L'identificativo scelto viene conservato in `sessionStorage`, quindi la
sessione sopravvive ai refresh ma termina con la chiusura della scheda.

Se `sessionStorage` non è disponibile, il profilo rimane valido soltanto nella memoria
dell'applicazione corrente.

## Ruoli

- **Administrator**: accesso completo, inclusi utenti e gestione demo.
- **Manager**: gestione operativa e pagamenti.
- **Receptionist**: iscritti, abbonamenti, accessi e pagamenti.
- **Viewer**: consultazione delle aree non riservate; nessun accesso ai pagamenti.

Le route applicano gli stessi vincoli mostrati dal menu. Un accesso diretto non autorizzato
mostra un errore, mentre un visitatore non autenticato viene rimandato al login.

## Limiti di sicurezza

Il codice, i profili e i dati sono disponibili nel browser e possono essere alterati con
gli strumenti di sviluppo. L'applicazione non deve essere utilizzata con dati personali,
credenziali o informazioni finanziarie reali.
