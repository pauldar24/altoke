(() => {
    const boton = document.getElementById('revisarDescripcion');
    const salida = document.getElementById('resultadoDescripcion');
    const descripcion = document.getElementById('descripcionObjeto');
    if (!boton || !salida || !descripcion) return;
    let version = 0;
    descripcion.addEventListener('input', () => { version++; salida.hidden = true; });
    boton.addEventListener('click', async () => {
        if (boton.disabled) return;
        const texto = descripcion.value.trim();
        salida.hidden = false;
        if (texto.length < 10) { salida.textContent = 'Añade una descripción de al menos 10 caracteres para revisarla.'; return; }
        const revision = version;
        const datos = new FormData();
        datos.set('descripcion', texto);
        datos.set('__RequestVerificationToken', boton.closest('form').querySelector('[name="__RequestVerificationToken"]').value);
        boton.disabled = true; salida.textContent = 'Revisando los detalles…';
        try {
            const respuesta = await fetch('/ObjetosPerdidos/EvaluarDescripcion', { method: 'POST', body: datos, signal: AbortSignal.timeout(15000) });
            if (!respuesta.ok) throw new Error();
            const resultado = await respuesta.json();
            if (revision !== version) return;
            salida.textContent = resultado.recomendacion;
        } catch {
            if (revision === version) salida.textContent = 'La revisión no está disponible. Puedes continuar revisando la descripción manualmente.';
        } finally { boton.disabled = false; }
    });
})();
