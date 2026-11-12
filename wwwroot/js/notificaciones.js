(() => {
    const panel = document.getElementById('campanaNotificaciones');
    if (!panel) return;
    async function cargar() {
        try {
            const respuesta = await fetch('/Notificaciones', { cache: 'no-store' });
            if (!respuesta.ok) return;
            const datos = await respuesta.json();
            document.getElementById('contadorNotificaciones').textContent = datos.noLeidas;
            const lista = document.getElementById('listaNotificaciones');
            lista.replaceChildren();
            for (const aviso of datos.lista) {
                const form = document.createElement('form');
                form.method = 'post'; form.action = '/Notificaciones/Leer';
                const token = panel.querySelector('input[name="__RequestVerificationToken"]').cloneNode();
                const id = document.createElement('input'); id.type = 'hidden'; id.name = 'id'; id.value = aviso.id;
                const boton = document.createElement('button'); boton.className = 'dropdown-item text-wrap';
                boton.textContent = (aviso.leida ? '' : '● ') + aviso.texto;
                form.append(token, id, boton); lista.append(form);
            }
            if (!datos.lista.length) lista.textContent = 'Todavía no tienes notificaciones.';
        } catch { /* Se conserva el contenido y se reintenta al abrir la campana. */ }
    }
    panel.addEventListener('show.bs.dropdown', cargar);
    cargar();
    window.addEventListener('focus', cargar);
    setInterval(() => { if (!document.hidden) cargar(); }, 30000);
    if (window.signalR) {
        const conexion = new signalR.HubConnectionBuilder().withUrl('/hubs/objetos').withAutomaticReconnect().build();
        conexion.on('NotificacionRecibida', cargar);
        conexion.onreconnected(cargar);
        async function conectar() { try { await conexion.start(); await cargar(); } catch { setTimeout(conectar, 10000); } }
        conectar();
    }
})();
