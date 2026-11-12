(() => {
    const widget = document.querySelector('.cf-turnstile');
    const form = widget?.closest('form');
    if (!form) return;
    const boton = form.querySelector('button[type="submit"]');
    const original = boton.textContent;
    let verificado = false;
    let enviando = false;
    const estado = document.createElement('p');
    estado.className = 'small text-muted';
    estado.setAttribute('role', 'status');
    widget.after(estado);
    function actualizar() {
        boton.disabled = !verificado || enviando;
        estado.textContent = enviando ? 'Creando o enviando tu solicitud…' : verificado
            ? 'Verificación completada. Ya puedes continuar.' : 'Espera a que termine la verificación de Cloudflare.';
    }
    window.altokeVerificado = token => { verificado = Boolean(token); actualizar(); };
    window.altokeVerificacionVencida = () => { verificado = false; actualizar(); };
    form.addEventListener('submit', evento => {
        if (evento.defaultPrevented || !form.checkValidity()) return;
        if (!verificado || enviando) { evento.preventDefault(); return; }
        enviando = true;
        boton.textContent = 'Enviando…';
        actualizar();
    });
    window.addEventListener('pageshow', () => {
        enviando = false;
        boton.textContent = original;
        verificado = Boolean(form.querySelector('input[name="cf-turnstile-response"]')?.value);
        actualizar();
    });
    actualizar();
})();
