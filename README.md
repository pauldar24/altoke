# AlToke · Grupo 3 (versión ampliada)

Plataforma ASP.NET Core MVC 10 para publicar, encontrar y recuperar objetos de la comunidad USMP. Se adaptaron las funcionalidades relevantes de **Demo20262** al propósito y diseño original de AlToke. Colores, tipografía, tarjetas y flujo de publicación del Grupo 3 permanecen como base de la interfaz.

## Arranque

**Requisitos:** .NET SDK **10**, acceso a NuGet y SQLite (incluido con EF Core). No se necesitan servicios externos para navegar, registrar usuarios, publicar, usar favoritos, enviar solicitudes, administrar mensajes o utilizar el asistente de respaldo.

```bash
dotnet restore
dotnet build
dotnet run
```

Abre la dirección local mostrada por `dotnet run`. La nueva migración `20261028000000_FuncionesDemoAlToke` agrega cuatro tablas sin borrar las anteriores y se aplica al arrancar.

`SeedDemoData=true` carga los objetos de muestra cuando la base no tiene usuarios ni publicaciones. Está habilitado por defecto para que una base SQLite nueva en Render también muestre ejemplos. El seeder se detiene si encuentra usuarios o publicaciones y nunca los borra. Las cuentas demostrativas nuevas reciben contraseñas con hash PBKDF2.

## Configuración por variables de entorno

Las claves no se incluyen en el código. Usa variables de entorno `SECCION__CLAVE`, por ejemplo (elige tus propias credenciales):

| Variable | Función |
|---|---|
| `AdminSettings__Password` | Contraseña de acceso a `/Admin/Login` (obligatoria para administración) |
| `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret` | Inicio de sesión con Google OAuth 2.0 (no solicita acceso al buzón de Gmail) |
| `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` | Cargar imágenes persistentes, opcional |
| `ConnectionStrings__Redis` | Sesiones con caché distribuida; si se omite usa memoria local |
| `Groq__ApiKey`, `Groq__Model`, `Groq__BaseUrl` | Asistente de IA opcional; sin clave funciona la respuesta local basada en publicaciones y FAQ |
| `Algolia__ApplicationId`, `Algolia__SearchApiKey`, `Algolia__AdminApiKey`, `Algolia__IndexName` | Autocompletado externo y sincronización desde el panel, opcional |
| `SeedDemoData` | Habilita datos de muestra en una base vacía; activo por defecto |

El botón **Continuar con Google** solo aparece cuando están definidas ambas credenciales. Google devuelve el perfil y correo para iniciar sesión; AlToke no pide permisos para leer Gmail. Los secretos se guardan como variables privadas en Render, nunca en `appsettings.json`.

## Modelos de Machine Learning

AlToke integra un clasificador de categorías con ML.NET, sin GPU ni un servicio externo de inferencia:

- **Categoría multiclase:** sugiere una categoría a partir del título y la descripción en el formulario de publicación. Se entrena con los avisos de SQLite cuando hay al menos 10 avisos y dos categorías; la persona siempre confirma la opción. El panel administrativo permite reentrenarlo.

Los artefactos se guardan en `/app/data/ml` en Render. Si el servicio no tiene disco persistente, los modelos pueden volver a entrenarse desde los datos de la aplicación después de un reinicio; para conservar SQLite y los modelos, conecta un disco persistente montado en `/app/data`.

## Despliegue de APIs en Render

El proyecto se ejecuta como un único Web Service Docker: la API REST de AlToke y las integraciones externas forman parte de la aplicación. El `Dockerfile` publica .NET 10. En Render, configura `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` para que ASP.NET Core reconozca el HTTPS original del proxy y construya correctamente el callback OAuth.

APIs e integraciones:

- **API REST propia:** `/api/objetos`; los GET son públicos, las escrituras requieren sesión y antifalsificación.
- **Google OAuth 2.0:** inicio de sesión. Callback de producción: `https://<dominio-render>/signin-google`.
- **Groq:** respuestas del chatbot (opcional).
- **Algolia:** búsqueda y sincronización de avisos (opcional).
- **Cloudinary:** almacenamiento de fotos (opcional).

Groq, Algolia y Cloudinary ya tienen rutas de respaldo locales. Su código desplegado no implica que estén conectados: necesitan credenciales válidas en Render. No hay integración con Gmail API para leer correo; Google se usa solamente como proveedor de identidad.

**Importante:** los ZIP originales contenían una contraseña de administración y credenciales de Cloudinary escritas en `appsettings.json`. Se retiraron del proyecto actualizado; **debes revocarlas y sustituirlas en el proveedor correspondiente**, porque haberlas quitado del ZIP no impide que las copias anteriores sigan expuestas. No publiques archivos secretos ni bases de datos con credenciales reales.

## Funcionalidades y rutas

- `/Explorar`: filtros por texto, categoría y estado, búsqueda por ubicación, sugerencias al escribir y paginación.
- `/Explorar/Detalle/{id}`: ficha completa, autor, ubicación, favoritos, solicitud al propietario y avisos relacionados.
- `/Favoritos`: publicaciones guardadas y recomendaciones de objetos similares basadas en categoría, texto, ubicación y preferencias. **No** usa el modelo de factorización de productos de Demo20262, porque ese dataset no representa objetos perdidos.
- `/Recuperaciones`: solicitudes enviadas/recibidas, aprobación o rechazo, respuesta privada de coordinación y aviso de solicitudes pendientes en la navegación.
- `/ObjetosPerdidos/Create`, `/ObjetosPerdidos/Editar/{id}`: publicación con foto, sugerencia de categoría por ML.NET y edición por el propietario. Si Cloudinary no está configurado, la imagen se guarda bajo `wwwroot/uploads` (almacenamiento local **no persistente** en contenedores efímeros).
- `/Home/Contacto`: formulario y registro de mensajes.
- `/Reportes/Contactos` y `/Conocimiento`: reportes administrativos, mantenimiento de respuestas e importación de documentos .txt/.md para el asistente local.
- Asistente flotante: consulta avisos reales y artículos de ayuda. Se conecta a Groq si hay clave; sin clave usa una respuesta local con enlaces a objetos. No inventa hallazgos.
- `/api/objetos`: API REST pública para GET; POST/PUT/DELETE requieren cookie de sesión y token antifalsificación, y respetan la propiedad del aviso. `/swagger` está disponible en Development.
- `/Admin`: tablero original y opciones nuevas de informes, conocimiento y sincronización con Algolia.

## Diferencias respecto a Demo20262

| Función de Demo20262 | Adaptación en AlToke | Alcance |
|---|---|---|
| Catálogo, detalle, búsqueda y filtros | Objetos con detalle, filtros y paginación | Incorporada |
| Guardar productos | Favoritos de objetos | Incorporada |
| Recomendaciones ML.NET por productos | Motor de afinidad según publicaciones y favoritos | Alternativa de dominio; **sin entrenamiento colaborativo ML.NET** |
| Mensajes y reportes | Contacto, moderación de mensajes y respuesta administrativa | Incorporada |
| Asistente Groq/Semantic Kernel | Asistente contextual con Groq opcional y modo local | Adaptada; no usa Semantic Kernel |
| Documentos Qdrant | Base de preguntas/respuestas en SQLite | Alternativa local, **sin Qdrant ni embeddings** |
| Algolia búsqueda/indexación | Búsqueda sugerida local y Algolia opcional | Incorporada con configuración externa |
| API de productos + Swagger | API de objetos + Swagger | Incorporada |
| Pedido + RabbitMQ/CloudAMQP | Solicitud de recuperación persistida en SQLite | Flujo adaptado; **sin broker AMQP** |
| PieSocket en tiempo real | Indicadores de solicitudes con consulta periódica | Alternativa sin WebSocket |
| ASP.NET Identity | Sesiones de AlToke y contraseñas PBKDF2 con actualización de hashes heredados | **No hay migración completa a Identity / roles** |
| Redis de catálogo | Redis ya existente para sesiones | Conservado; no cachea el catálogo |
| DummyJSON / tareas de prueba | No aplica al caso de objetos perdidos | No trasladada |
| Formulario de mascotas | Formulario de reporte existente de AlToke | Conservada |

Esta versión **no replica literalmente todas las dependencias de infraestructura** de Demo20262: integraciones de tienda, broker de pedidos y recomendador entrenado sobre ventas serían inapropiados sin diseñar equivalentes y sus datos reales. Lo que se incluyó es funcional dentro del dominio AlToke; los proveedores opcionales necesitan claves propias y pruebas con los servicios reales.

## Verificación

```bash
python tests/verificar_estructura.py
node --check wwwroot/js/asistente.js
node --check wwwroot/js/busqueda.js
node --check wwwroot/js/recuperaciones.js
# En un equipo con .NET 10:
dotnet build
dotnet run
```

La validación estática, el análisis de sintaxis de JavaScript con Node y la inspección de la base SQLite se ejecutaron. **La compilación C#, la ejecución del servidor, la aplicación real de migraciones por EF, el flujo extremo a extremo y las APIs de terceros no pudieron ejecutarse en el entorno de generación porque no incluye .NET SDK ni credenciales externas.** Revisa esas pruebas antes de desplegar.

## Seguridad / producción

- Crea una contraseña de administración privada; sin ella no se habilita el acceso al panel.
- Las cuentas nuevas usan `PasswordHasher<Usuario>` de ASP.NET Core (PBKDF2). En el siguiente inicio de sesión válido se actualizan contraseñas heredadas SHA-256 o del seeder; planifica una migración/rotación de usuarios reales y no mantengas texto plano.
- Las llamadas que escriben datos tienen protección antifalsificación. La API está pensada para clientes autenticados del mismo origen; Swagger muestra documentación, pero no suministra automáticamente tokens CSRF.
- Persistencia en Render/Docker: monta un volumen para `/app/data` y configura almacenamiento de imágenes externo como Cloudinary si no quieres perder subidas locales tras reinicios.
- Si no hay Redis la sesión se almacena en memoria, por lo que se pierde tras reinicios y no se comparte entre instancias.
- No se comparten correos privados de los autores en el buscador ni en el asistente. La respuesta a una solicitud la ven las personas involucradas.

El análisis de satisfacción se retiró. Las solicitudes aceptadas cuentan con chat privado entre sus dos participantes; al resolver el caso se conserva el historial. El clasificador de categorías se prepara durante el arranque con ejemplos base incluidos y los avisos existentes, antes de atender el formulario.

## Tres modelos adicionales orientados a recuperar objetos
Coincidencias entre pérdida y hallazgo, detección de posible spam y revisión opcional de claridad de descripciones. Son modelos ML.NET entrenados de forma independiente, además del clasificador de categoría existente. Investigación, datasets iniciales, evaluación, límites y pasos de demostración en [MODELOS_ML_ALTOKE.md](MODELOS_ML_ALTOKE.md).
