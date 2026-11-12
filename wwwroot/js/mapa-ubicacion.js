(() => {
    const contenedor = document.getElementById('mapaUbicacion');
    if (!contenedor || !window.L) return;
    const latitud = document.getElementById('Latitud');
    const longitud = document.getElementById('Longitud');
    const lat = Number(latitud?.value || contenedor.dataset.lat);
    const lng = Number(longitud?.value || contenedor.dataset.lng);
    const tienePunto = Number.isFinite(lat) && Number.isFinite(lng) && (latitud?.value || contenedor.dataset.lat);
    const mapa = L.map(contenedor).setView(tienePunto ? [lat, lng] : [-12.0464, -77.0428], tienePunto ? 16 : 12);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>' }).addTo(mapa);
    let marcador = tienePunto ? L.marker([lat, lng]).addTo(mapa) : null;
    if (latitud && longitud) {
        mapa.on('click', evento => {
            if (marcador) marcador.remove();
            marcador = L.marker(evento.latlng).addTo(mapa);
            latitud.value = evento.latlng.lat.toFixed(6); longitud.value = evento.latlng.lng.toFixed(6);
        });
        document.getElementById('limpiarUbicacion')?.addEventListener('click', () => {
            marcador?.remove(); marcador = null; latitud.value = ''; longitud.value = '';
        });
    }
})();
