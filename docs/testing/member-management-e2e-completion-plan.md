# Piano di completamento E2E della gestione iscritti

## Scopo, fonti e fotografia iniziale

Questo documento pianifica il consolidamento della copertura browser della gestione
iscritti. Non autorizza modifiche al seed o al codice applicativo: ogni eventuale difetto
di produzione emerso durante l'esecuzione deve essere isolato e corretto in un'attività
separata, prima di riprendere il piano.

La fotografia è ricavata da `member-management-e2e.md`, dalle due specifiche Playwright,
da `support/member-fixtures.js`, da `playwright.config.js` e dal commit più recente
`3b72450` (`test: expand member management browser scenarios (#29)`). Nel clone esaminato
non esiste un riferimento Git `main` né un remoto configurato: non è quindi stato
possibile eseguire `git diff main...HEAD`. Il diff del commit più recente rispetto al suo
genitore mostra l'aggiunta della specifica degli scenari e degli helper e l'aggiornamento
del documento esistente. Questa limitazione va risolta prima della consegna finale, senza
creare arbitrariamente un riferimento `main`.

Non sono presenti nel repository risultati macchina che dimostrino un'esecuzione completa
verde del commit corrente. Di conseguenza, **nessuno scenario nuovo viene qui dichiarato
superato**. La classificazione iniziale è:

| Stato | Evidenza nel codice corrente |
| --- | --- |
| Implementato e verificato | Nessuno classificabile come tale sulla sola base dei file o del messaggio di commit; serve un report di un'esecuzione completa con retry zero. |
| Implementato, non ancora validato da una suite completa verde | Route e ruoli; directory e dataset vuoto; creazione/modifica; validazioni parziali; errore `save` in creazione; conflitto di versione e record rimosso; ciclo sospensione/riattivazione/archiviazione; parte di tastiera, focus, responsive e screenshot; fallback in memoria. |
| Parzialmente coperto | Attese Blazor (helper esistente ma non usato dopo ogni transizione); doppio submit (un `dblclick` non prova stato disabilitato e numero di chiamate); tutti i limiti e le regole di data; accessibilità di tutti i campi; dialogo e focus trap; viste e conteggi dell'intero ciclo; conservazione delle entità collegate; ripristino degli interceptor. |
| Assente | Errori `save` in modifica e cambio stato; conflitto di revisione dello store pilotato dal browser; isolamento esplicito fra due context; nascita successiva alla data operativa e iscrizione uguale alla nascita come casi distinti; flusso completo senza mouse; verifica completa di intestazioni e nomi accessibili. |

Ogni risultato futuro deve riportare commit, data/ora UTC, ambiente e conteggi; la mera
presenza di un'asserzione non equivale a copertura verificata.

## Step 1 — Stabilizzazione della suite

| Voce | Piano |
| --- | --- |
| **Obiettivo** | Ottenere una baseline ripetibile dell'intera suite Playwright, eliminando sincronizzazioni fragili e dimostrando l'isolamento dei test. |
| **Stato iniziale** | La configurazione usa Chromium, un worker, `fullyParallel: false`, `retries: 0`, trace conservato al fallimento e screenshot al fallimento. Esiste `waitForBlazor`, ma il suo uso è limitato; non risultano attese temporali arbitrarie nelle specifiche esaminate. Non esiste un report committato dell'ultima suite completa. |
| **Attività dettagliate** | 1. Eseguire tutta la suite senza filtri e confermare dalla configurazione effettiva che i retry siano zero. 2. Registrare passed/failed/skipped/flaky e durata, senza trasformare un retry in un successo. 3. Ripetere localmente i test falliti per diagnosi, non come criterio di accettazione. 4. Cercare `waitForTimeout`, `setTimeout`, sleep e polling manuale; sostituire eventuali attese arbitrarie con locator auto-waiting, `expect`, attesa della route, del rendering Blazor e dello stato DOM osservabile. 5. Applicare `waitForBlazor` nei punti in cui caricamento e navigazione possono competere con le asserzioni, rafforzandolo solo su segnali pubblici della UI. 6. Verificare che ogni test riceva un browser context nuovo e IndexedDB/localStorage non trapelino; aggiungere una prova sentinella se l'isolamento non è dimostrabile. 7. Registrare nome/versione browser e Playwright, sistema, viewport, worker, retry, politiche trace/screenshot e percorsi artefatti. 8. Rieseguire l'intera suite una volta dopo le correzioni. |
| **File probabilmente interessati** | `tests/ViteKlub.E2E/specs/member-management.spec.js`, `tests/ViteKlub.E2E/specs/member-management-scenarios.spec.js`, `tests/ViteKlub.E2E/support/member-fixtures.js`, eventualmente `tests/ViteKlub.E2E/playwright.config.js`; nessun file applicativo. |
| **Test o comandi** | `cd tests/ViteKlub.E2E && npx playwright --version`; `cd tests/ViteKlub.E2E && npx playwright test --retries=0`; ricerche statiche con `rg`; eventuali ripetizioni diagnostiche con `--repeat-each`, sempre separate dal run di accettazione. |
| **Artefatti attesi** | Registro testuale con commit, ambiente, configurazione e conteggi numerici; report/trace/screenshot soltanto locali e ignorati; elenco delle instabilità con causa e correzione. |
| **Dipendenze** | Nessuna; è il gate per gli step successivi. |
| **Rischi** | Bootstrap Blazor, inizializzazione IndexedDB o selettori testuali possono introdurre race; un solo worker può nascondere interferenze; un report HTML residuo può finire nel diff. |
| **Limitazioni** | Un singolo run verde non dimostra assenza assoluta di flakiness; Chromium non rappresenta tutti i browser. Nessun test è dichiarato verde finché il run non termina realmente con exit code zero. |
| **Criteri di accettazione** | Intera suite Playwright verde in almeno un'esecuzione completa, non filtrata, con retry impostato a zero; zero failed e conteggio esplicito di passed/skipped; nessuna attesa arbitraria; context isolati dimostrati; metadati e policy artefatti registrati. |
| **Risultato da registrare** | Comando esatto, SHA, timestamp UTC, browser/versione, viewport/progetto, retry, worker, trace, screenshot, durata, passed/failed/skipped/flaky e link/percorso degli artefatti locali. |

## Step 2 — Persistenza e concorrenza

| Voce | Piano |
| --- | --- |
| **Obiettivo** | Provare dal browser atomicità, gestione degli errori e concorrenza ottimistica senza allentare le regole di revisione/versione. |
| **Stato iniziale** | L'intercettazione della funzione pubblica `demoStorage.save` e il confronto snapshot esistono per la creazione. Due pagine nello stesso context coprono in parte il conflitto di versione e la rimozione del record. Mancano errori in modifica/cambio stato, conteggio puntuale delle scritture, cleanup esplicito e una prova browser del conflitto di revisione in memoria. |
| **Attività dettagliate** | Aggiungere casi indipendenti per errore `save` durante creazione, modifica e ciascun cambio stato; acquisire snapshot prima/dopo e verificare uguaglianza, feedback italiano, dati compilati conservati, azione nuovamente disponibile e nessuna navigazione/sovrascrittura. Contare le chiamate a `save`. Con due pagine dello stesso context, caricare la stessa versione, far vincere la prima modifica e verificare che la seconda conservi i valori inseriti, segnali il conflitto e non alteri lo snapshot vincente; ripetere rimuovendo il record e ricaricando lo snapshot. Ripristinare sempre l'interceptor in fixture/teardown e provarne il pass-through. Usare due context per dimostrare isolamento dei database e impedire che una mutazione o un interceptor attraversino il confine; non usarli per simulare concorrenza sul medesimo IndexedDB, perché i context non condividono lo storage. Documentare separatamente il conflitto di revisione non pilotabile. |
| **File probabilmente interessati** | Specifica scenari e helper E2E; configurazione solo se necessaria al teardown. Se emerge un difetto, file di produzione esclusivamente in una successiva attività di correzione. |
| **Test o comandi** | Test Playwright mirati per tag/titolo durante lo sviluppo; poi obbligatoriamente `npx playwright test --retries=0` completo. Confronti deep-equal dello snapshot e contatori dell'interceptor. |
| **Artefatti attesi** | Tabella caso/esito con snapshot pre/post, numero di `load`/`save`, pagina/context usati e messaggio UI osservato; trace locale dei soli fallimenti. |
| **Dipendenze** | Step 1 verde e fixture di sincronizzazione stabile. |
| **Rischi** | Sovrascrivere `demoStorage` troppo tardi; confondere versione dell'iscritto con revisione dello store; una pagina può mantenere uno store .NET in memoria non condiviso; cleanup incompleto può falsare casi successivi. |
| **Limitazioni** | La revisione dello store è solo in memoria e non è esposta da un'API browser: il suo conflitto non è oggi pilotabile E2E in modo affidabile. Rimane coperto a livello .NET finché non esiste un comportamento pubblico osservabile; non si deve aggiungere un bypass diagnostico di produzione né allentare il controllo. |
| **Criteri di accettazione** | Tutti i tre errori `save`, conflitto versione e record assente producono feedback e lasciano lo snapshot corretto; input conservati; zero sovrascritture; interceptor ripristinato dopo ogni test; isolamento a due context provato; limite sulla revisione dichiarato. |
| **Risultato da registrare** | Per ogni scenario: meccanismo pubblico, topologia pagine/context, revisioni/versioni osservabili, snapshot prima/dopo, chiamate, testo UI, esito e issue di regressione se applicabile. |

### Mappa del pilotaggio

| Scenario | Meccanismo corretto |
| --- | --- |
| Fallimento `demoStorage.save` in creazione/modifica/cambio stato | API browser pubblica sostituita prima del bootstrap; una pagina e un context, con ripristino garantito. |
| Conflitto di versione iscritto | Due pagine nello stesso context, quindi stesso IndexedDB: entrambe caricano la versione iniziale, una salva e l'altra tenta il comando obsoleto. |
| Record eliminato/assente dopo reload snapshot | Due pagine nello stesso context oppure una mutazione controllata tramite `demoStorage.load/save`, poi reload e tentativo con form già compilato. |
| Isolamento | Due context separati; le modifiche non devono propagarsi. Non simula un conflitto sul medesimo database. |
| Conflitto di revisione store | Non pilotabile oggi dal browser: la revisione vive nell'istanza .NET in memoria e non in IndexedDB. Verifica nei test .NET e decisione tecnica aperta, senza API diagnostiche ad hoc. |

Un test può riprodurre regressioni di produzione quali salvataggio parziale prima
dell'eccezione, perdita degli input, doppia scrittura, sovrascrittura dell'aggiornamento
vincente, ricreazione involontaria di un record eliminato, stato UI aggiornato senza
persistenza o interceptor rimasto attivo. In tal caso si applica il processo descritto in
“Regressioni di produzione”.

## Step 3 — Creazione e modifica complete

| Voce | Piano |
| --- | --- |
| **Obiettivo** | Rendere completa e leggibile la prova browser di creazione, persistenza, unicità, modifica e validazione del form. |
| **Stato iniziale** | Esistono compilazione tramite label, consenso, doppio click, navigazione al dettaglio, ID dalla URL, tessera dal DOM, reload, ricerca univoca, confronto snapshot e modifica di molti campi. Esiste un caso aggregato di limiti/date, ma non prova separatamente ogni confine. Non sono provati stato disabilitato, singolo annuncio “Salvataggio in corso”, singola chiamata, tutte le date limite, tutti i dati modificabili né distinzione completa inesistente/archiviato. |
| **Attività dettagliate** | Usare esclusivamente label e ruoli accessibili per compilare/azionare. Derivare la data operativa dal dataset/UI e costruire date relative, senza data “oggi” o valori temporali fragili. In creazione verificare consenso obbligatorio, pulsante disabilitato mentre la Promise `save` è controllatamente sospesa, una sola regione/occorrenza “Salvataggio in corso”, impossibilità del secondo submit e una sola chiamata `save`; poi feedback italiano, route dettaglio, ID letto dalla route/DOM e tessera letta dal DOM (mai attesi hard-coded), reload, singolo record in directory/snapshot. In modifica cambiare nome, cognome, nascita, iscrizione, email, telefono, scadenza certificato se presente, contatto e note; verificare ID, tessera e consenso invariati prima/dopo reload. Creare casi tabellari separati per limite e oltre-limite di nome, cognome, email, telefono, contatto emergenza e note. Creare casi distinti per nascita uguale e successiva alla data operativa, iscrizione precedente e uguale alla nascita. Verificare messaggi italiani precisi. Verificare separatamente route di ID inesistente e form/dettaglio archiviato, incluse azioni assenti e feedback specifico. |
| **File probabilmente interessati** | `member-management-scenarios.spec.js` e `member-fixtures.js`; eventualmente la specifica base per eliminare duplicazioni mirate. Nessun seed o produzione. |
| **Test o comandi** | Esecuzioni mirate dei casi create/edit/validation con retry zero; suite completa a fine step; confronto del record DOM e snapshot prima/dopo reload. |
| **Artefatti attesi** | Evidenza testuale dei valori generati dall'app, matrice limite/valore/messaggio, screenshot locale della creazione completata e registro di una sola scrittura/annuncio. |
| **Dipendenze** | Step 1; interceptor robusto dello Step 2 per sospendere e contare `save`. |
| **Rischi** | `dblclick` può completarsi dopo la prima navigazione senza dimostrare protezione; leggere l'ID solo dalla URL non prova che sia esposto nel DOM se questo è requisito UI; selettori per testo possono confondere label e valore; date fisse diventano incoerenti col dataset. |
| **Limitazioni** | ID e tessera possono essere letti solo da elementi realmente presentati dalla UI; se l'ID non è mostrato nel DOM, registrare la lacuna e aprire una decisione/prodotto invece di fissarlo nel test. |
| **Criteri di accettazione** | Creazione e modifica persistono una sola entità; doppio submit genera una sola scrittura; stato/annuncio di salvataggio sono unici; tutti i campi e confini richiesti sono verificati; date relative al dataset; ID/tessera non hard-coded; consenso e identificativi conservati; inesistente e archiviato sono distinguibili. |
| **Risultato da registrare** | Valori DOM usati, data operativa, ID/tessera osservati, conteggio save/record/annunci, matrice validazioni e risultato dopo ogni reload. |

### Asserzioni esistenti e rafforzamenti

| Area | Esiste già | Da rafforzare o aggiungere |
| --- | --- | --- |
| Compilazione | Helper usa label; consenso selezionato. | Confermare ruoli accessibili, campo certificato e tutti i campi modificabili. |
| Salvataggio | `dblclick`, feedback singolo, URL dettaglio. | Promise controllata, pulsante disabilitato, singolo annuncio in corso e singola chiamata `save`. |
| Identità/unicità | ID estratto dalla URL; tessera dal testo DOM; record unico nello snapshot e ricerca a risultato singolo. | Leggere anche l'identificativo dal DOM se disponibile e confrontarlo; unicità prima/dopo reload e dopo modifica. |
| Persistenza | Reload dopo creazione e modifica; tessera visibile. | Asserire tutti i valori, ID, tessera, consenso e un solo record. |
| Validazioni | Sei valori oltre limite in un test; iscrizione precedente alla nascita; privacy `aria-describedby`. | Casi limite indipendenti, data operativa, nascita uguale/successiva, iscrizione uguale, messaggio per ogni campo. |
| Inesistente/archiviato | Dettaglio inesistente e azioni assenti dopo archiviazione sono presenti in test distinti. | Verificare route edit, feedback specifici e non confondere i due stati. |

## Step 4 — Ciclo di vita integrato

| Voce | Piano |
| --- | --- |
| **Obiettivo** | Realizzare un solo flusso seriale, isolato e leggibile che segua un nuovo iscritto in tutte le viste fino all'archiviazione. |
| **Stato iniziale** | Il test corrente opera su un record seed: misura alcuni conteggi, sospende, riattiva, archivia e controlla il dettaglio finale. Non include creazione, directory a ogni fase, tutti i conteggi, reload dopo ogni comando né conservazione delle entità collegate. |
| **Attività dettagliate** | Marcare il flusso seriale e garantirgli un context/database esclusivo. 1. Leggere dalla dashboard i conteggi iniziali visibili. 2. Creare l'iscritto. 3. Ricaricare. 4. Trovarlo una sola volta in directory e aprirne il dettaglio. 5. Rileggere la dashboard. 6. Sospendere. 7. Ricaricare e verificare directory, dettaglio e dashboard. 8. Riattivare. 9. Ricaricare e verificare di nuovo le tre viste. 10. Archiviare tramite dialogo. 11. Ricaricare. 12. Verificare stato archiviato in directory/dettaglio. 13. Confrontare i conteggi finali mostrati con quelli iniziali e con gli effetti documentati dei comandi. 14. Verificare assenza di modifica/sospensione/riattivazione/archiviazione. 15. Fotografare prima e dopo gli abbonamenti, accessi e pagamenti collegati al record scelto e dimostrarne la conservazione; se un nuovo iscritto non ha collegamenti, usare una fixture controllata o separare una verifica su record seed senza mutare il seed distribuito. |
| **File probabilmente interessati** | Specifica scenari e helper E2E. Nessuna copia di `DashboardProjection` nel JavaScript. |
| **Test o comandi** | Test seriale mirato durante sviluppo; intera suite retry zero. Asserzioni solo su card, righe, badge, detail e azioni della UI. |
| **Artefatti attesi** | Registro per ogni fase con route, stato nelle tre viste, conteggi UI e cardinalità delle entità collegate; screenshot finale archiviato locale. |
| **Dipendenze** | Step 1; creazione robusta dello Step 3; semantica degli errori dello Step 2. |
| **Rischi** | Un test lungo rende difficile localizzare failure; navigazioni possono usare DOM obsoleto; il nuovo record potrebbe non avere relazioni e produrre una verifica vacua; ricostruire la proiezione nel test duplicherebbe produzione. |
| **Limitazioni** | Si confrontano esclusivamente valori mostrati dalla UI e gli effetti documentati dei comandi; il test non replica né importa l'algoritmo di `DashboardProjection`. La strategia non vacua per le entità collegate è una decisione tecnica. |
| **Criteri di accettazione** | Tutti i 15 passaggi completati nello stesso flusso isolato; reload dopo le transizioni; stato coerente nelle tre viste; conteggi UI coerenti con gli effetti documentati; azioni finali assenti; relazioni preesistenti conservate con cardinalità e identità confrontate. |
| **Risultato da registrare** | Sequenza delle fasi, valori UI iniziali/intermedi/finali, identificativo letto dal DOM, relazioni prima/dopo, screenshot e conteggi del run. |

## Step 5 — Accessibilità, tastiera e responsive

| Voce | Piano |
| --- | --- |
| **Obiettivo** | Consolidare requisiti DOM/accessibility tree, tastiera e layout nei tre viewport, senza presentare le verifiche mirate come audit WCAG completo. |
| **Stato iniziale** | Sono verificati un riepilogo errori, focus sul riepilogo, `aria-describedby` e messaggio privacy, parte del dialogo con Enter/Tab/Escape, azioni principali e overflow in alcune pagine. Screenshot locali sono già richiesti da test. Mancano copertura campo-per-campo, focus trap completo, ripristino focus asserito, flusso senza mouse e struttura intestazioni completa. |
| **Attività dettagliate** | Per ogni campo invalido leggere `aria-describedby`, risolvere ogni ID e asserire elemento esistente, visibile e col messaggio pertinente. Contare una sola regione di riepilogo errori e una sola regione/occorrenza di salvataggio. Inventariare tutte le azioni per ruolo/stato e richiedere nomi accessibili specifici, non una regex ombrello. Verificare un solo `h1`, gerarchia senza salti ingiustificati e nomi coerenti. Nel dialogo verificare focus iniziale sull'azione documentata, ciclo Tab/Shift+Tab contenuto, nessun focus sul contenuto retrostante, chiusura con Annulla ed Escape (e altri comandi realmente supportati), quindi focus restituito esattamente al pulsante di apertura. Eseguire creazione e archiviazione usando solo `focus`, Tab/Shift+Tab, Space/Enter/Escape. A ogni focus verificare bounding box entro viewport e non occultato da overflow/contenitori. Ripetere i controlli rilevanti a 1440×900, 768×1024 e 390×844. Produrre localmente i quattro screenshot richiesti. |
| **File probabilmente interessati** | Specifiche e helper E2E; `package.json`/lockfile soltanto se viene approvato axe. Nessun asset binario committato. |
| **Test o comandi** | Test Playwright mirati nei tre viewport e suite completa; controllo locale dei quattro PNG e successiva conferma che siano ignorati/non nel diff. Se approvato, comando axe dedicato documentato e suite completa. |
| **Artefatti attesi** | `member-created-desktop.png`, `member-form-errors-mobile.png`, `member-archive-dialog-tablet.png`, `member-archived-final.png` sotto `test-results/documentation`, soltanto locali; matrice campo/error-id; matrice azione/nome/ruolo/stato; log focus. |
| **Dipendenze** | Step 1, flussi degli Step 3 e 4. |
| **Rischi** | Screenshot o report accidentalmente tracciati; controlli bounding-box non rilevano ogni occlusione; MudBlazor può usare portali e ordine focus diverso; axe può introdurre rumore e manutenzione. |
| **Limitazioni** | Asserzioni Playwright e axe non sostituiscono test con screen reader, ingranditori o altre tecnologie assistive reali. Senza axe e tecnologie reali non si dichiara un audit WCAG completo; anche con axe resta necessaria verifica manuale. |
| **Criteri di accettazione** | Associazioni errori complete; regioni e annunci unici; azioni con nomi specifici; intestazioni coerenti; dialogo con focus iniziale, trap, chiusure e restore; creazione/archiviazione senza mouse; focus visibile e niente overflow nei tre viewport; quattro screenshot prodotti localmente e assenti dal diff. |
| **Risultato da registrare** | Viewport, sequenza tasti, elemento focalizzato a ogni passo, conteggi regioni/annunci, matrice ARIA, overflow, percorsi screenshot, esito axe se eseguito e verifiche manuali ancora dovute. |

### Valutazione di axe

| Aspetto | Valutazione |
| --- | --- |
| Motivazione | Individuare automaticamente classi di problemi (nomi, contrasto, struttura, ARIA) che le sole asserzioni funzionali possono omettere. |
| Impatto | Nuova dipendenza esclusivamente dev/test, aggiornamento lockfile, tempi e manutenzione delle esclusioni; nessun pacchetto applicativo deve cambiare. |
| Alternativa | Asserzioni Playwright mirate su accessibility tree, ruoli, nomi, relazioni, regioni, focus, tastiera, intestazioni e overflow, più verifica manuale con tecnologie assistive. |
| Decisione proposta | Introdurre `@axe-core/playwright` in un'attività futura dedicata dopo la baseline, con zero esclusioni generiche e violazioni motivate; se non approvato, completare la matrice Playwright e mantenere esplicito il limite. Non aggiungerlo nell'attività di pianificazione corrente. |

## Step 6 — Validazione finale e consegna

| Voce | Piano |
| --- | --- |
| **Obiettivo** | Eseguire realmente tutti i gate .NET, browser e repository e consegnare modifiche riproducibili senza artefatti o violazioni architetturali. |
| **Stato iniziale** | Questo documento non attribuisce esito a comandi non eseguiti. Il riferimento `main` manca nel clone corrente e va reso disponibile dal normale workflow prima dei controlli `main...HEAD`. |
| **Attività dettagliate** | Eseguire nell'ordine i comandi sotto, registrando exit code e conteggi. Dopo i test ispezionare status, diff e file ignorati. Confrontare hash del seed prima/dopo. Cercare credenziali e file generati nel diff. Verificare una sola definizione `VersionPrefix` in `Directory.Build.props`; nessuna lettura diretta dell'orologio nel Core; nessuna generazione di timestamp/ID nei Razor; Viewer senza azioni e senza accesso diretto alle route operative; policy/route operative realmente autorizzate; snapshot immutato nei casi di conflitto. Distinguere fallimento di codice da limite ambientale. |
| **File probabilmente interessati** | Nessuno oltre ai test/documenti prodotti negli step precedenti; eventuali correzioni devono rispettare il perimetro della relativa attività. |
| **Test o comandi** | I comandi elencati nella sezione seguente, senza omissioni silenziose. |
| **Artefatti attesi** | Registro finale comandi/esiti, conteggi .NET/Playwright, numstat, hash seed, inventario diff e attestazione dei controlli statici; report binari soltanto locali. |
| **Dipendenze** | Step 1–5 completati; riferimento `main` autentico e aggiornato disponibile. |
| **Rischi** | Dipendenze/browser mancanti; file generati nel diff; controlli statici troppo generici; confronto contro un `main` non aggiornato; dichiarare verde un run parziale. |
| **Limitazioni** | Un comando non eseguito o bloccato dall'ambiente resta “non eseguito/bloccato”, mai “superato”. Il controllo segreti automatico non sostituisce l'ispezione del diff. |
| **Criteri di accettazione** | Tutti i comandi obbligatori eseguiti con esito registrato; suite completa verde con retry zero; diff atteso e pulito; nessun artefatto/segreto; tutti i guardrail statici e funzionali verificati; nessuna issue aperta bloccante. |
| **Risultato da registrare** | Tabella comando, directory, timestamp, exit code, passed/failed/skipped, limitazioni; hash seed; file nel diff; risultati di ogni controllo finale; SHA del commit e bozza PR aggiornata. |

### Comandi obbligatori

```bash
dotnet restore ViteKlub.slnx
dotnet build ViteKlub.slnx --configuration Release --no-restore
dotnet test ViteKlub.slnx --configuration Release --no-build
dotnet format ViteKlub.slnx --verify-no-changes --no-restore
cd tests/ViteKlub.E2E && npm ci
cd tests/ViteKlub.E2E && npm test
git diff --check
git diff --numstat main...HEAD
```

`npm test` deve continuare a risolvere a una suite completa con `retries: 0`; il log deve
riportare passed, failed e skipped. Prima di `npm ci` va registrato l'hash del seed e dopo
la suite va confrontato. I controlli finali comprendono:

- seed `src/ViteKlub.Web/wwwroot/data/initial-dataset.json` invariato;
- nessun PNG, ZIP, trace, report HTML, profilo browser, log, `node_modules`, `bin` o `obj`
  nel diff (controllare file tracciati, non solo `.gitignore`);
- nessun segreto o credenziale nel diff;
- `VersionPrefix` presente esclusivamente in `Directory.Build.props`;
- nessuna lettura diretta dell'orologio nel Core;
- nessuna generazione di timestamp o identificativi nei componenti `.razor`;
- Viewer completamente in sola lettura sia nella UI sia tramite navigazione diretta;
- route operative protette da autorizzazione reale, non solo da pulsanti nascosti;
- snapshot byte/strutturalmente invariato dopo ogni conflitto o errore, salvo la modifica
  vincente esplicitamente attesa nel caso concorrente.

## Matrice di tracciabilità

Legenda: **P** = responsabilità primaria; **V** = validazione/gate; **D** = dipendenza.

| Requisito E2E | 1 | 2 | 3 | 4 | 5 | 6 |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Suite completa, retry zero, conteggi e ambiente | P | D | D | D | D | V |
| Stabilità, attese Blazor/DOM, niente attese arbitrarie | P | D | D | D | D | V |
| Isolamento browser context | P | P | D | P | D | V |
| `save` fallisce in creazione/modifica/cambio stato | D | P | D |  |  | V |
| Conflitto revisione/versione, record assente | D | P | D |  |  | V |
| Input e snapshot conservati, nessuna sovrascrittura | D | P | V |  |  | V |
| Interceptor ripristinato | D | P |  |  |  | V |
| Form via label/ruoli, data operativa, privacy | D | D | P | D | V | V |
| Stato salvataggio, annuncio unico, doppio submit | D | D | P |  | V | V |
| Feedback, dettaglio, ID/tessera dal DOM | D |  | P | V |  | V |
| Reload, persistenza, unicità | D | V | P | P |  | V |
| Modifica completa e identità/consenso conservati | D | V | P |  |  | V |
| Tutti i limiti e regole data | D |  | P |  | V | V |
| Inesistente distinto da archiviato | D | V | P | V |  | V |
| Ciclo integrato nelle tre viste e conteggi | D | D | D | P | D | V |
| Assenza azioni e relazioni conservate | D | V |  | P | V | V |
| Errori, riepilogo e `aria-describedby` | D |  | D |  | P | V |
| Nomi accessibili e intestazioni | D |  | D | D | P | V |
| Dialogo: focus, trap, chiusura e restore | D |  |  | D | P | V |
| Creazione/archiviazione senza mouse | D | D | D | D | P | V |
| Focus visibile, overflow e tre viewport | D |  | D | D | P | V |
| Quattro screenshot locali |  |  | D | D | P | V |
| Valutazione axe e limiti WCAG |  |  |  |  | P | V |
| Gate .NET/npm/Git e guardrail repository |  |  |  |  |  | P |

## Ordine di esecuzione consigliato

1. Stabilire baseline e sincronizzazione con lo Step 1.
2. Rendere sicuri interceptor, snapshot e topologie di concorrenza con lo Step 2.
3. Completare create/edit e la matrice di validazione con lo Step 3.
4. Comporre il flusso seriale dello Step 4 usando primitive già stabilizzate.
5. Eseguire la matrice accessibilità/tastiera/responsive dello Step 5 e decidere axe.
6. Eseguire senza filtri tutti i gate dello Step 6 e preparare la consegna.

Ogni step deve poter essere una successiva attività indipendente: parte dal commit verde
precedente, aggiorna il registro di evidenza e non anticipa refactoring non necessari.

## Scenari ancora manuali

- prova con screen reader reali (almeno combinazioni browser/tecnologia concordate),
  lettura del riepilogo, messaggi dinamici e dialogo;
- ingrandimento, contrasto forzato, modalità high-contrast e preferenze utente non
  emulate dalla sola matrice viewport;
- valutazione cognitiva e qualità linguistica dei messaggi italiani;
- audit WCAG completo: resta esplicitamente non dichiarabile senza metodologia, axe e
  tecnologie assistive reali;
- conflitto di revisione dello store dal browser, finché la revisione resta solo nello
  stato .NET in memoria e non esiste un comportamento pubblico pilotabile;
- browser diversi da Chromium, finché non vengono configurati ed eseguiti realmente;
- deploy pubblico, finché non è disponibile un URL/ambiente concordato.

## Decisioni aperte

1. **Riferimento `main`:** rendere disponibile il riferimento autentico e aggiornato per
   il diff finale; non sintetizzarlo dal branch corrente.
2. **ID nel DOM:** verificare se il dettaglio espone l'identificativo oltre alla route; se
   non lo espone, decidere se il requisito richiede una modifica UI separata.
3. **Revisione store:** confermare che resti intenzionalmente interna e coperta dai test
   .NET; evitare hook diagnostici di produzione creati solo per E2E.
4. **Relazioni nel ciclo:** scegliere una fixture non vacua, isolata e creata dal browser
   o un record seed con legami, senza modificare il seed distribuito.
5. **Axe:** approvare o rifiutare la dipendenza di solo test secondo impatto e proposta
   dello Step 5.
6. **Comandi del dialogo:** censire quelli realmente supportati dal componente e non
   pretendere scorciatoie non documentate.
7. **Browser matrix:** stabilire se Chromium è sufficiente al gate o se pianificare in
   seguito Firefox/WebKit.

## Regressioni di produzione

Quando un test riproduce un comportamento contrario ai requisiti:

1. conservare trace, screenshot e log solo localmente e annotare SHA, ambiente e passi;
2. riprodurre almeno una volta con test mirato a retry zero e verificare che non sia un
   errore di fixture o sincronizzazione;
3. ridurre il caso senza indebolire le asserzioni o la concorrenza;
4. aprire una issue/attività di correzione distinta con atteso, osservato, impatto e
   riproduzione; non modificare produzione nello stesso commit di solo test se il perimetro
   non lo consente;
5. aggiungere prima una prova che fallisce per il difetto, correggere produzione nel
   branch appropriato e rieseguire test mirato, suite completa e gate .NET;
6. non saltare, disabilitare, rendere flaky-tolerant o allentare controlli di revisione e
   versione per ottenere il verde;
7. riprendere questo piano solo dopo integrazione della correzione, registrando issue/PR e
   risultato del run completo.

## Proposta di suddivisione dei commit futuri

1. `test: stabilize member management e2e suite` — attese, isolamento e baseline.
2. `test: cover member persistence failures` — interceptor con cleanup e tre errori save.
3. `test: cover member concurrency conflicts` — due pagine, due context e snapshot.
4. `test: complete member create and edit coverage` — submit, persistenza e identità.
5. `test: complete member validation boundaries` — limiti e date tabellari.
6. `test: add integrated member lifecycle coverage` — unico flusso seriale e relazioni.
7. `test: strengthen member accessibility coverage` — ARIA, focus, tastiera e viewport.
8. `test: add axe checks` — soltanto se la decisione è approvata, includendo lockfile.
9. `docs: record member e2e validation results` — evidenze reali, limiti e consegna.

Ogni commit deve essere autonomo, senza binari, seed, versione o refactoring estraneo; un
eventuale fix applicativo segue un branch/commit separato e viene referenziato dal test.

## Checklist conclusiva

- [ ] Baseline completa verde con retry zero e conteggi numerici registrati.
- [ ] Browser/versione, viewport, worker, retry, trace e screenshot registrati.
- [ ] Nessuna attesa arbitraria; rendering Blazor e DOM attesi esplicitamente.
- [ ] Context, pagine e interceptor isolati e ripristinati.
- [ ] Tre errori `save`, conflitti e record assente non alterano lo snapshot.
- [ ] Create/edit completi, singolo submit, ID/tessera DOM e reload verificati.
- [ ] Tutti i limiti, casi data, inesistente e archiviato verificati separatamente.
- [ ] Flusso seriale di 15 passaggi e relazioni non vacue completati.
- [ ] ARIA, annunci, intestazioni, dialogo, tastiera e focus verificati.
- [ ] Tre viewport senza overflow/focus occulto e quattro screenshot solo locali.
- [ ] Decisione axe registrata; nessuna dichiarazione impropria di audit WCAG.
- [ ] Tutti i comandi dello Step 6 realmente eseguiti e relativi esiti registrati.
- [ ] Seed, versione e guardrail architetturali invariati.
- [ ] Nessun segreto, binario o artefatto generato nel diff.
- [ ] Diff completo riesaminato e limitato ai file previsti dall'attività.
- [ ] Limitazioni, scenari manuali, regressioni e problemi aperti documentati.

## Bozza della futura Pull Request (da non pubblicare in questa attività)

**Titolo:** `test: complete member management E2E consolidation`

**Descrizione:**

> ## Scopo
> Completare e stabilizzare la copertura E2E browser della gestione iscritti, incluse
> persistenza, concorrenza, ciclo di vita, accessibilità e responsive.
>
> ## Modifiche principali
> - stabilizzazione delle attese Blazor/DOM e dell'isolamento;
> - copertura di errori di salvataggio, conflitti e snapshot invariati;
> - completamento di creazione, modifica, validazioni e doppio submit;
> - flusso seriale sospensione/riattivazione/archiviazione nelle tre viste;
> - verifiche ARIA, tastiera, focus e viewport concordati;
> - aggiornamento della documentazione con risultati effettivi.
>
> ## File e componenti
> Specifiche e helper sotto `tests/ViteKlub.E2E/` e documentazione sotto
> `docs/testing/`; indicare separatamente ogni eventuale fix di produzione.
>
> ## Test eseguiti e risultati
> Inserire i comandi dello Step 6, exit code, conteggi passed/failed/skipped e ambiente.
> Non compilare questa sezione con esiti presunti.
>
> ## Test non eseguiti
> Elencare esplicitamente ogni comando omesso e il motivo.
>
> ## Limitazioni note
> Revisione store non pilotabile dal browser; browser effettivamente eseguiti; verifiche
> manuali con tecnologie assistive; decisione ed esito axe.
>
> ## Problemi aperti
> Collegare regressioni e decisioni tecniche residue oppure dichiarare “nessuno” solo
> dopo la validazione finale.

La bozza è destinata a una futura attività di implementazione: pubblicazione del branch e
creazione della Pull Request restano azioni esplicite dell'utente.
