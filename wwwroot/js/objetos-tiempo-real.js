(() => {
  if (!window.signalR) return;

  const normalizar = (valor) => String(valor ?? "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("es");

  const fechaVisible = (valor) => {
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? "" : new Intl.DateTimeFormat("es-PE").format(fecha);
  };

  function crearTarjeta(objeto, inicio) {
    const columna = document.createElement("div");
    columna.className = inicio ? "col-md-4 mb-4" : "col-lg-4 col-md-6 mb-4";
    columna.dataset.objetoId = String(objeto.id);

    const tarjeta = document.createElement("div");
    tarjeta.className = inicio ? "card card-objeto h-100 shadow-sm" : "card card-objeto h-100 border-0";
    columna.appendChild(tarjeta);

    if (objeto.fotoUrl) {
      const imagen = document.createElement("img");
      imagen.src = objeto.fotoUrl;
      imagen.alt = objeto.titulo || "Objeto publicado";
      imagen.className = "card-img-top";
      tarjeta.appendChild(imagen);
    } else {
      const imagenVacia = document.createElement("div");
      imagenVacia.className = "bg-light d-flex align-items-center justify-content-center";
      imagenVacia.style.height = "200px";
      imagenVacia.style.borderRadius = "12px 12px 0 0";
      imagenVacia.setAttribute("aria-hidden", "true");
      const icono = document.createElement("i");
      icono.className = "bi bi-image text-muted";
      icono.style.fontSize = "3rem";
      imagenVacia.appendChild(icono);
      tarjeta.appendChild(imagenVacia);
    }

    if (!inicio) {
      const estadoSobreImagen = document.createElement("div");
      estadoSobreImagen.className = "position-absolute top-0 end-0 m-2";
      const insigniaEstado = document.createElement("span");
      insigniaEstado.className = "badge badge-estado-perdido shadow-sm";
      insigniaEstado.textContent = objeto.estado || "Abierto";
      estadoSobreImagen.appendChild(insigniaEstado);
      tarjeta.appendChild(estadoSobreImagen);
    }

    const cuerpo = document.createElement("div");
    cuerpo.className = "card-body d-flex flex-column";
    const categoria = document.createElement("span");
    categoria.className = "badge badge-categoria mb-2 align-self-start";
    categoria.textContent = objeto.categoria || "Otros";
    cuerpo.appendChild(categoria);
    const tipo = document.createElement("p"); tipo.className = "small fw-bold"; tipo.textContent = objeto.tipoPublicacion || "Publicado por el dueño · Busca su objeto"; cuerpo.appendChild(tipo);

    const titulo = document.createElement("h5");
    titulo.className = inicio ? "card-title fw-bold text-dark mb-2" : "card-title fw-bold";
    const enlace = document.createElement("a");
    enlace.href = `/Explorar/Detalle/${encodeURIComponent(objeto.id)}`;
    enlace.className = inicio ? "stretched-link text-decoration-none text-dark" : "text-decoration-none text-dark stretched-link";
    enlace.textContent = objeto.titulo || "Objeto publicado";
    titulo.appendChild(enlace);
    cuerpo.appendChild(titulo);

    const descripcion = document.createElement("p");
    descripcion.className = "card-text text-muted flex-grow-1";
    const limite = inicio ? 100 : 120;
    const textoDescripcion = String(objeto.descripcion || "");
    descripcion.textContent = textoDescripcion.length > limite ? `${textoDescripcion.slice(0, limite)}...` : textoDescripcion;
    cuerpo.appendChild(descripcion);
    tarjeta.appendChild(cuerpo);

    const pie = document.createElement("div");
    pie.className = "card-footer bg-transparent border-0 pt-0 pb-3 px-3 d-flex justify-content-between align-items-center";
    const fecha = document.createElement("small");
    fecha.className = "text-muted";
    fecha.textContent = fechaVisible(objeto.fecha);
    pie.appendChild(fecha);
    if (inicio) {
      const estado = document.createElement("span");
      estado.className = "badge badge-estado-perdido";
      estado.textContent = objeto.estado || "Abierto";
      pie.appendChild(estado);
    } else {
      const autor = document.createElement("small");
      autor.className = "text-muted";
      autor.textContent = objeto.autor || "Anónimo";
      pie.appendChild(autor);
    }
    tarjeta.appendChild(pie);
    return columna;
  }

  function mostrarAviso(objeto) {
    const anfitrion = document.getElementById("notificacionesEnVivo");
    if (!anfitrion) return;
    const aviso = document.createElement("div");
    aviso.className = "alert alert-success shadow mb-2";
    aviso.setAttribute("role", "status");
    aviso.textContent = `Nuevo objeto publicado: ${objeto.titulo || "Ver avisos"}`;
    anfitrion.appendChild(aviso);
    window.setTimeout(() => aviso.remove(), 6000);
  }

  function filtrosCoinciden(objeto, parametros) {
    const buscar = normalizar(parametros.get("buscar"));
    const textoObjeto = normalizar(`${objeto.titulo || ""} ${objeto.descripcion || ""} ${objeto.ubicacion || ""}`);
    const categoria = parametros.get("categoria");
    const estado = parametros.get("estado");
    const pagina = parametros.get("pagina") || "1";
    return pagina === "1" &&
      (!buscar || textoObjeto.includes(buscar)) &&
      (!categoria || normalizar(objeto.categoria) === normalizar(categoria)) &&
      (!estado || normalizar(objeto.estado) === normalizar(estado));
  }

  function agregarEnInicio(objeto) {
    const vacio = document.getElementById("inicioSinObjetos");
    let lista = document.getElementById("objetosRecientes");
    if (!lista && vacio) {
      lista = document.createElement("div");
      lista.id = "objetosRecientes";
      lista.className = "row";
      vacio.before(lista);
    }
    if (!lista || lista.querySelector(`[data-objeto-id="${Number(objeto.id)}"]`)) return;
    vacio?.remove();
    lista.prepend(crearTarjeta(objeto, true));
    while (lista.children.length > 6) lista.lastElementChild.remove();
    const total = document.getElementById("totalObjetos");
    if (total) total.textContent = String((Number.parseInt(total.textContent, 10) || 0) + 1);
  }

  function agregarEnExplorar(objeto) {
    const parametros = new URLSearchParams(window.location.search);
    if (!filtrosCoinciden(objeto, parametros)) return;
    const vacio = document.getElementById("explorarSinResultados");
    let lista = document.getElementById("objetosExplorar");
    if (!lista && vacio) {
      lista = document.createElement("div");
      lista.id = "objetosExplorar";
      lista.className = "row";
      vacio.before(lista);
    }
    if (!lista || lista.querySelector(`[data-objeto-id="${Number(objeto.id)}"]`)) return;
    vacio?.remove();
    lista.prepend(crearTarjeta(objeto, false));
    while (lista.children.length > 9) lista.lastElementChild.remove();
    const total = document.getElementById("totalResultados");
    if (total) total.textContent = String((Number.parseInt(total.textContent, 10) || 0) + 1);
  }

  const conexion = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/objetos")
    .withAutomaticReconnect()
    .build();

  conexion.on("ObjetoPublicado", (objeto) => {
    if (!objeto || objeto.id == null) return;
    const ruta = window.location.pathname.toLowerCase();
    if (ruta === "/") agregarEnInicio(objeto);
    if (ruta === "/explorar" || ruta.startsWith("/explorar/")) agregarEnExplorar(objeto);
    mostrarAviso(objeto);
  });

  async function iniciar() {
    try {
      await conexion.start();
    } catch {
      window.setTimeout(iniciar, 5000);
    }
  }

  conexion.onclose(iniciar);
  iniciar();
})();
