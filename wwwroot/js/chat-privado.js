(() => {
    const chat = document.getElementById('chatPrivado');
    if (!chat) return;
    const id = chat.dataset.solicitud;
    const historial = document.getElementById('historialChat');
    const estado = document.getElementById('estadoChat');
    const anteriores = document.getElementById('mensajesAnteriores');
    const vistos = new Set();
    let ultimo = 0, primero = 0, cargando = false;
    async function cargar(antiguos = false) {
        if (cargando) return;
        cargando = true;
        try {
            const eraInicial = ultimo === 0;
            const query = antiguos && primero ? `antes=${primero}` : `despues=${ultimo}`;
            const respuesta = await fetch(`/ChatPrivado/Mensajes?id=${id}&${query}`, { cache: 'no-store' });
            if (!respuesta.ok) throw new Error('No se pudo cargar el chat. Revisa tu sesión.');
            const mensajes = await respuesta.json();
            const bloque = document.createDocumentFragment();
            for (const mensaje of mensajes) {
                if (vistos.has(mensaje.id)) continue;
                vistos.add(mensaje.id);
                ultimo = Math.max(ultimo, mensaje.id); primero = primero ? Math.min(primero, mensaje.id) : mensaje.id;
                const tarjeta = document.createElement('article');
                tarjeta.className = 'border rounded p-3 mb-2 ' + (String(mensaje.autorId) === chat.dataset.usuario ? 'bg-light' : '');
                const nombre = document.createElement('strong'); nombre.textContent = mensaje.autor;
                const texto = document.createElement('p'); texto.textContent = mensaje.texto; texto.style.whiteSpace = 'pre-wrap';
                const fecha = document.createElement('small'); fecha.textContent = new Date(mensaje.creadoEn + (mensaje.creadoEn.endsWith('Z') ? '' : 'Z')).toLocaleString('es-PE');
                tarjeta.append(nombre, texto, fecha); bloque.append(tarjeta);
            }
            if (antiguos) historial.prepend(bloque); else historial.append(bloque);
            if (antiguos || eraInicial) anteriores.hidden = mensajes.length < 100;
            estado.textContent = vistos.size ? '' : 'Todavía no hay mensajes. Escribe para coordinar.';
        } catch (error) { estado.textContent = error.message; }
        finally { cargando = false; }
    }
    anteriores.addEventListener('click', () => cargar(true));
    const form = document.getElementById('formChatPrivado');
    form?.addEventListener('submit', async evento => {
        evento.preventDefault();
        const boton = form.querySelector('button'); if (boton.disabled) return;
        boton.disabled = true;
        try {
            const respuesta = await fetch(form.action, { method: 'POST', body: new FormData(form) });
            if (!respuesta.ok) throw new Error(respuesta.status === 400 ? await respuesta.text() : 'No se pudo enviar. Revisa tu sesión e inténtalo otra vez.');
            form.elements.texto.value = ''; form.elements.claveEnvio.value = crypto.randomUUID();
            await cargar();
        } catch (error) { estado.textContent = error.message; }
        finally { boton.disabled = false; }
    });
    window.addEventListener('altoke-chat', evento => { if (String(evento.detail) === id) cargar(); });
    window.addEventListener('focus', () => cargar());
    setInterval(() => { if (!document.hidden) cargar(); }, 5000);
    cargar();
})();
