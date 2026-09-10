(function () {
    "use strict";

    const databaseName = "viteklub-demo";
    const storeName = "datasets";
    const datasetKey = "current";

    function openDatabase() {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(databaseName, 1);
            request.onupgradeneeded = () => {
                const database = request.result;
                if (!database.objectStoreNames.contains(storeName)) {
                    database.createObjectStore(storeName);
                }
            };
            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    }

    async function execute(mode, operation) {
        const database = await openDatabase();
        try {
            return await new Promise((resolve, reject) => {
                const transaction = database.transaction(storeName, mode);
                const request = operation(transaction.objectStore(storeName));
                request.onsuccess = () => resolve(request.result ?? null);
                request.onerror = () => reject(request.error);
                transaction.onabort = () => reject(transaction.error);
            });
        } finally {
            database.close();
        }
    }

    window.demoStorage = {
        load: () => execute("readonly", store => store.get(datasetKey)),
        save: json => execute("readwrite", store => store.put(json, datasetKey))
    };
}());
