# Cambios de AlToke — ramas y configuración

Las ramas son acumulativas y se integran en este orden:
1. codex/publicaciones-validacion
2. codex/contactos-moderacion-correo
3. codex/notificaciones-avistamientos
4. codex/mapa-turnstile

La cuarta contiene todos los cambios. No fusionar las ramas fuera de orden.
Cada migración se aplica al inicio mediante Database.Migrate(), como en el proyecto existente.
Conservar la base SQLite en un disco persistente antes del despliegue.

## Configuración del servidor
- Groq__ApiKey y Groq__Model: reutilizan la integración existente.
  La moderación envía únicamente el título y descripción, o asunto y texto de contacto.
  Usa JSON validado. Si Groq falla, la publicación aplica un filtro local conservador
  y los contactos quedan "Sin clasificar" para revisión manual.
- Resend__ApiKey: clave de envío de Resend.
- Resend__From: remitente de un dominio verificado en Resend.
  Sin configuración válida, Responder muestra que el correo no se pudo enviar.
  La respuesta se conserva para reintentar el mismo texto con clave de idempotencia.
  Un avistamiento se guarda aunque el correo de respaldo falle.
- Turnstile__SiteKey y Turnstile__SecretKey: widget de Cloudflare para el dominio publicado.
- Turnstile__Hostname: hostname exacto, sin https ni ruta.
  En Render se toma RENDER_EXTERNAL_HOSTNAME si no se define.
  Acciones verificadas: registro y contacto.
  En producción, registro y contacto de visitantes requieren Turnstile configurado.
  En desarrollo se omite la verificación si no existe SecretKey.
  Contacto con sesión toma nombre y correo de la base de datos y omite el captcha.

El mapa usa Leaflet 1.9.4 y teselas de OpenStreetMap, con atribución visible.
No requiere clave. Las coordenadas son opcionales, públicas y validadas en el servidor.
Los avisos anteriores sin coordenadas siguen funcionando.

## Comportamiento
- Fecha de pérdida o hallazgo con máximo hoy (hora de Perú), validada en creación,
  edición y API. Publicación Perdí/Encontré con foto opcional y texto aclaratorio.
- Contactos: Consulta, Queja, Sugerencia, Spam/abuso o Sin clasificar.
  El buzón muestra la clasificación de moderación y las acciones spam, responder, archivar, restaurar.
  Los contactos históricos se mantienen disponibles para revisión.
- Campana con contador de no leídas, lista de 50 recientes y lectura individual.
- Lo vi: requiere sesión, aviso perdido y usuario distinto del dueño.
  Guarda ubicación y hora, notifica al dueño por SignalR y prueba correo de respaldo.
  Un reenvío idéntico evita duplicar el registro.
- SignalR usa un grupo privado asignado por el servidor a partir de la sesión.
  Las notificaciones persistidas se recuperan al abrir la campana o reconectar.

## Verificación
Compilación .NET 10 y migraciones sobre una base SQLite de comprobación.
Comprobación HTTP local con dos cuentas temporales: identidad del contacto,
rechazo de carro, aceptación de llaves de carro, fecha futura, coordenadas y
aislamiento de las notificaciones.
Groq, Resend y Turnstile requieren verificación con credenciales reales en el despliegue.
La entrega del evento SignalR en dos navegadores y las teselas externas deben
comprobarse en la demo del despliegue.

Documentación oficial:
https://console.groq.com/docs/structured-outputs
https://developers.cloudflare.com/turnstile/get-started/server-side-validation/
https://resend.com/docs/api-reference/emails/send-email
https://leafletjs.com/examples/quick-start/

## Corrección de tipos de publicación
Desde codex/tipos-publicacion, se retiró Lo vi y su endpoint. Tipo de publicación separa Dueño y Hallazgo. Ambos se crean abiertos; Resuelto significa entrega o recuperación completada. Las filas históricas quedan SinEspecificar, para que su autor o administrador revise el tipo en Editar sin deducirlo de un estado ambiguo. Los ejemplos nuevos del seeder de hallazgos se crean abiertos.

## Chat privado y categorías
El chat se abre desde solicitudes aceptadas y solo admite a sus dos participantes. Al cerrar el caso queda en modo de lectura. Los mensajes generan una notificación que abre el chat. La entrega usa SignalR y recuperación periódica si se interrumpe la conexión.
La categoría se sugiere una vez al salir del título (mínimo tres caracteres). El modelo v3 se prepara antes de atender peticiones y se entrena con ML/categorias_base.csv y los títulos válidos existentes. El tiempo de preparación se traslada al arranque del servicio.
Se retiraron el servicio de sentimiento y su dataset. El campo histórico se conserva en la base para mantener compatibilidad de datos, pero ya no se calcula ni se muestra como satisfacción.
