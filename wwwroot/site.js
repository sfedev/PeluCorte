window.PeluCorteMapa = {
    mapa: null,
    capa: null,
    init(elementId, centroLat, centroLng) {
        const el = document.getElementById(elementId);
        if (!el) return;
        if (this.mapa) { this.mapa.remove(); this.mapa = null; }
        this.mapa = L.map(elementId).setView([centroLat || 40.4168, centroLng || -3.7038], 12);
        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(this.mapa);
        this.capa = L.layerGroup().addTo(this.mapa);
    },
    setPins(pins) {
        if (!this.mapa || !this.capa) return;
        this.capa.clearLayers();
        const markers = [];
        for (const p of pins) {
            const m = L.marker([p.lat, p.lng]);
            const safeName = (p.nombre || '').replace(/</g, '&lt;');
            const safeAddr = (p.direccion || '').replace(/</g, '&lt;');
            m.bindPopup(`<strong>${safeName}</strong><br/>${safeAddr}<br/><a href="${p.url}">Reservar →</a>`);
            m.addTo(this.capa);
            markers.push(m);
        }
        if (markers.length > 0) {
            const grupo = L.featureGroup(markers);
            this.mapa.fitBounds(grupo.getBounds(), { padding: [40, 40], maxZoom: 14 });
        }
    }
};

window.PeluCorteUI = {
    initTema() {
        // El servidor ya renderiza data-tema desde la cookie. Solo refrescamos icono.
        this.refrescarIconoTema();
    },
    toggleTema() {
        const actual = document.documentElement.getAttribute('data-tema') || 'claro';
        const nuevo = actual === 'claro' ? 'oscuro' : 'claro';
        document.documentElement.setAttribute('data-tema', nuevo);
        // Guardamos en cookie (para que el servidor lo lea en cada render) y en localStorage (backup).
        try { localStorage.setItem('pc-tema', nuevo); } catch (e) {}
        document.cookie = 'pc-tema=' + nuevo + '; path=/; max-age=31536000; samesite=lax';
        this.refrescarIconoTema();
        return nuevo;
    },
    refrescarIconoTema() {
        const icono = document.getElementById('pc-tema-icono');
        if (!icono) return;
        const tema = document.documentElement.getAttribute('data-tema') || 'claro';
        icono.classList.remove('bi-sun', 'bi-moon');
        icono.classList.add(tema === 'oscuro' ? 'bi-sun' : 'bi-moon');
    },
    temaActual() {
        return document.documentElement.getAttribute('data-tema') || 'claro';
    },
    aceptarCookies() {
        localStorage.setItem('pc-cookies', '1');
    },
    cookiesAceptadas() {
        return localStorage.getItem('pc-cookies') === '1';
    },
    initCookieBanner() {
        if (this.cookiesAceptadas()) return;
        const banner = document.getElementById('pc-cookie-banner');
        if (banner) banner.style.display = '';
    }
};

function pcInicializar() {
    window.PeluCorteUI.initTema();
    window.PeluCorteUI.initCookieBanner();
}

document.addEventListener('DOMContentLoaded', pcInicializar);
document.addEventListener('enhancedload', pcInicializar);
