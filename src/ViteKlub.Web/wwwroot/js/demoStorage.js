const databaseName = "viteklub-demo";
const databaseVersion = 1;
const storeName = "datasets";
const activeDatasetKey = "active";

export function isSupported() {
    return typeof globalThis.indexedDB !== "undefined";
}

export async function readDataset() {
    const database = await openDatabase();

    try {
        return await executeRequest(
            database.transaction(storeName, "readonly").objectStore(storeName).get(activeDatasetKey));
    } finally {
        database.close();
    }
}

export async function writeDataset(json) {
    const database = await openDatabase();

    try {
        const transaction = database.transaction(storeName, "readwrite");
        transaction.objectStore(storeName).put(json, activeDatasetKey);
        await completeTransaction(transaction);
    } finally {
        database.close();
    }
}

function openDatabase() {
    return new Promise((resolve, reject) => {
        const request = globalThis.indexedDB.open(databaseName, databaseVersion);

        request.onupgradeneeded = () => {
            if (!request.result.objectStoreNames.contains(storeName)) {
                request.result.createObjectStore(storeName);
            }
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error ?? new Error("Impossibile aprire IndexedDB."));
        request.onblocked = () => reject(new Error("L'apertura di IndexedDB è bloccata da un'altra scheda."));
    });
}

function executeRequest(request) {
    return new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result ?? null);
        request.onerror = () => reject(request.error ?? new Error("Operazione IndexedDB non riuscita."));
    });
}

function completeTransaction(transaction) {
    return new Promise((resolve, reject) => {
        transaction.oncomplete = () => resolve();
        transaction.onerror = () => reject(transaction.error ?? new Error("Transazione IndexedDB non riuscita."));
        transaction.onabort = () => reject(transaction.error ?? new Error("Transazione IndexedDB annullata."));
    });
}
