const CACHE_NAME = "dulce-antojo-v1";

const STATIC_ASSETS = [
    "/",
    "/css/site.css",
    "/js/site.js",
    "/manifest.json",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/offline.html"
];

self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames
                    .filter(cacheName => cacheName !== CACHE_NAME)
                    .map(cacheName => caches.delete(cacheName))
            );
        })
    );

    self.clients.claim();
});

self.addEventListener("fetch", event => {

    if (event.request.method !== "GET") {
        return;
    }

    const requestURL = new URL(event.request.url);

    // Solo manejar recursos de nuestro propio sitio.
    if (requestURL.origin !== self.location.origin) {
        return;
    }

    // Para páginas HTML:
    // primero intenta internet y, si no hay conexión,
    // muestra la página offline.
    if (event.request.mode === "navigate") {

        event.respondWith(
            fetch(event.request)
                .catch(() => caches.match("/offline.html"))
        );

        return;
    }

    // Para CSS, JS, imágenes locales y otros recursos:
    // intenta red y utiliza caché como respaldo.
    event.respondWith(
        fetch(event.request)
            .then(response => {

                if (response && response.status === 200) {

                    const responseClone = response.clone();

                    caches.open(CACHE_NAME)
                        .then(cache => {
                            cache.put(event.request, responseClone);
                        });
                }

                return response;
            })
            .catch(() => caches.match(event.request))
    );
});