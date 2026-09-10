# Autenticazione dimostrativa e ruoli

## Ambito e limiti di sicurezza

ViteKlub simula l'autenticazione interamente nel browser per consentire di esplorare la
demo statica. Non esistono backend, password, token firmati o credenziali reali. L'utente
sceglie un profilo fittizio già presente nel dataset e l'identificativo selezionato viene
salvato in `localStorage`.

Questo meccanismo è intenzionalmente **non sicuro e non adatto alla produzione**: chi usa
il browser può leggere o alterare sia il dataset sia la sessione e tutti i controlli di
autorizzazione sono eseguiti nel client. Non deve essere usato per proteggere dati o
funzioni reali.

## Componenti

- `IDemoAuthenticationService` espone l'elenco dei profili attivi e le operazioni di
  login e logout.
- `DemoAuthenticationStateProvider` implementa il servizio e
  `AuthenticationStateProvider`. Carica gli utenti tramite `IDemoDatasetStore`, crea le
  claims e notifica Blazor quando lo stato cambia.
- `IDemoSessionStore` separa la persistenza della sessione; l'implementazione browser usa
  la chiave `viteklub.demo.current-user` in `localStorage`.
- `AuthorizeRouteView` protegge le pagine. Un visitatore anonimo viene inviato a
  `/login`; un profilo autenticato senza ruolo sufficiente viene inviato a
  `/access-denied`.
- `DemoNavigation` costituisce l'unica matrice condivisa da menu e controllo delle route
  predisposte, evitando che siano mostrate voci non accessibili al ruolo corrente.

## Ciclo della sessione

Al primo accesso il provider legge l'identificativo da `localStorage` e cerca il profilo
nel dataset corrente. La sessione viene ripristinata solo se l'identificativo è valido e
l'utente esiste ed è attivo. Valori malformati, utenti rimossi e utenti disattivati
causano la cancellazione della sessione e restituiscono un'identità anonima. Il logout
cancella sempre la chiave e aggiorna immediatamente lo stato dell'interfaccia.

Le claims generate sono:

- `nameidentifier`: identificativo del profilo demo;
- `name`: nome visualizzato fittizio;
- `role`: uno fra i quattro ruoli approvati;
- `preferred_username`: username dimostrativo.

## Matrice dei ruoli

| Area | Administrator | Manager | Receptionist | Viewer |
| --- | :---: | :---: | :---: | :---: |
| Dashboard | ✓ | ✓ | ✓ | ✓ |
| Iscritti | ✓ | ✓ | ✓ | ✓ (sola lettura) |
| Abbonamenti | ✓ | ✓ | ✓ | ✓ (sola lettura) |
| Accessi | ✓ | ✓ | ✓ | ✓ (sola lettura) |
| Pagamenti | ✓ | ✓ | ✓ | — |
| Utenti demo | ✓ | — | — | — |
| Gestione demo | ✓ | — | — | — |

In questo incremento le aree funzionali restano segnaposto: non vengono introdotte
operazioni CRUD. La distinzione di sola lettura del Viewer sarà applicata alle azioni
quando tali funzioni verranno implementate; per ora limita l'insieme delle aree visibili.

`Administrator` ha accesso completo. `Manager` e `Receptionist` accedono alle aree
operative, dashboard e pagamenti. `Viewer` accede soltanto alle viste informative
consentite e non vede pagamenti o amministrazione della demo.
