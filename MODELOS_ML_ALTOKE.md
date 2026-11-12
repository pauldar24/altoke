# Modelos ML adicionales para AlToke

## Qué se implementó

AlToke conserva el modelo ML.NET de categoría y agrega tres modelos entrenados adicionales.
Cada uno tiene datos, parámetros aprendidos y archivo de modelo propios. Se ejecutan con
Microsoft.ML 5.0, ya instalado en el proyecto. No requieren otra API, clave ni GPU.

| Modelo nuevo | Problema que resuelve | Entrada y salida | Uso en la web |
|---|---|---|---|
| Coincidencias perdido/hallazgo | Acercar al dueño a publicaciones que podrían corresponder a su objeto | Similitud de título y descripción, categoría, lugar y contradicción de color → candidato compatible | Detalle del aviso, sección Posibles coincidencias |
| Spam y abuso | Facilitar la revisión de mensajes ajenos al objetivo de la plataforma | Texto del contacto en español → posible spam o mensaje sin esa marca | Buzón administrativo; etiqueta Posible spam (ML) |
| Claridad de descripción | Ayudar a redactar avisos reconocibles | Descripción en español → aporta detalles o conviene ampliarla | Publicar objeto, botón Revisar si mi descripción es clara |

Los tres usan clasificadores binarios SDCA Logistic Regression. Compartir el algoritmo no los
convierte en el mismo modelo: se entrenan de manera independiente con etiquetas y datos
distintos. Los dos de texto comparten infraestructura de carga, vectorización y evaluación.

La categoría existente es un cuarto modelo ML.NET multiclase.
Groq sigue proporcionando el modelo de lenguaje del chatbot y la moderación existente.
Las recomendaciones generales basadas en favoritos se conservan como algoritmo y no se
cuentan como otro modelo ML.

## Investigación y decisión

- La clasificación binaria de ML.NET aprende a decidir entre dos etiquetas a partir de ejemplos.
  Permite entrenar el detector de spam y el evaluador de claridad con texto vectorizado.
  https://learn.microsoft.com/dotnet/machine-learning/resources/tasks
  https://learn.microsoft.com/dotnet/machine-learning/tutorials/sentiment-analysis
- Un clasificador de pares puede aprender de características numéricas de dos avisos.
  En AlToke compara candidatos de tipo opuesto que permanecen abiertos y pertenecen a
  autores distintos. La salida sugiere publicaciones; la identidad del objeto se comprueba
  con el autor, nunca se confirma automáticamente por el modelo.
  https://learn.microsoft.com/dotnet/machine-learning/how-to-choose-an-ml-net-algorithm
- Se descartó por ahora la factorización matricial: el sistema aún no tiene un historial de
  valoraciones o interacciones etiquetadas suficiente para un recomendador colaborativo.
  https://learn.microsoft.com/dotnet/machine-learning/tutorials/movie-recommendation
- Agrupar con K-Means sería útil al contar con suficientes publicaciones geolocalizadas.
  En el estado actual aporta menos al flujo de devolución que las tres funciones elegidas.
  https://learn.microsoft.com/dotnet/machine-learning/resources/tasks

## Datos y entrenamiento

Los archivos ML/modelos/*-train.jsonl y *-eval.jsonl se incluyen en la publicación.
Son ejemplos sintéticos redactados para este proyecto, sin mensajes personales de usuarios
ni afirmaciones de que correspondan a devoluciones reales. Las etiquetas iniciales requieren
revisión y ampliación por el equipo.

- Coincidencias: 88 pares de entrenamiento y 24 de evaluación. La evaluación usa objetos y
  redacciones diferentes. Incluye candidatos ajenos y colores incompatibles.
- Spam: 60 textos de entrenamiento y 20 de evaluación, equilibrados por clase.
  Una queja legítima se etiqueta como válida, aunque exprese descontento.
- Claridad: 60 descripciones de entrenamiento y 20 de evaluación, equilibradas por clase.
  Se etiqueta como suficiente cuando contiene características útiles para distinguir el objeto.
  Las frases vagas, aunque sean largas, se etiquetan como mejorables.

El inicio de Program.cs prepara los modelos antes de aceptar peticiones. Cada instancia
PredictionEngine se protege con un bloqueo porque no es segura para uso concurrente.
Los modelos se guardan en /app/data/ml en producción o ML/Models en desarrollo:
- coincidencias-v2.zip
- spam-v1.zip
- calidad-v1.zip

Si no se puede guardar el artefacto, se mantiene el modelo en memoria. Si no se puede cargar
o entrenar, la función queda no disponible y la web permite continuar con revisión manual.
Para cambiar datasets y reentrenar, se debe cambiar la versión del archivo en el servicio
correspondiente y volver a desplegar. El artefacto existente se reutiliza entre arranques.

Los datos de evaluación no se añaden al entrenamiento. Los registros incluyen precisión,
exhaustividad y F1 con los umbrales usados por la aplicación: 0.65 para coincidencias,
0.85 para posible spam y 0.5 para claridad. No se muestran probabilidades como garantías.

## Resultado de la comprobación local

| Modelo | Precisión positiva | Exhaustividad | F1 |
|---|---:|---:|---:|
| Coincidencias, 24 pares reservados | 1.000 | 1.000 | 1.000 |
| Spam, 20 textos reservados | 0.909 | 1.000 | 0.952 |
| Claridad, 20 descripciones reservadas | 1.000 | 1.000 | 1.000 |

Estos resultados solo describen corpus pequeños y sintéticos. No son tasas de éxito reales
de AlToke ni permiten afirmar que el modelo está validado para todos los objetos o usuarios.
En spam hubo un falso positivo entre diez mensajes legítimos. Por eso la marca local
Posible spam (ML) conserva el mensaje en la bandeja para revisión humana; el administrador
decide si lo mueve a spam y puede restaurarlo. La moderación Groq preexistente conserva
su comportamiento separado.

Se verificó además el flujo HTTP local de las tres funciones, con cuentas temporales:
- coincidencia entre mochila perdida y hallazgo de otro usuario;
- exclusión de un paraguas;
- revisión de descripción vaga y detallada;
- clasificación local del conjunto reservado de spam sin llamar a Groq.

## Demostración

1. Publicar desde dos usuarios un aviso de dueño y un hallazgo compatibles. Abrir el detalle:
   aparece Posibles coincidencias. Los casos cerrados y los avisos del mismo autor se excluyen.
2. En Contacto, enviar un texto de prueba comercial. En Administración → Mensajes revisar
   la marca Posible spam (ML). Comparar con una consulta legítima y decidir manualmente.
3. En Publicar objeto, escribir una descripción vaga, pulsar Revisar si mi descripción es clara
   y luego probar con color, marca, rasgos y lugar. La revisión es opcional.
   No dispara nuevas sugerencias de categoría ni bloquea el formulario.

La revisión de claridad no evalúa satisfacción, identidad, veracidad ni propiedad del objeto.

## Ramas acumulativas

1. codex/ml-coincidencias
2. codex/ml-spam
3. codex/ml-calidad-avisos

La tercera contiene los tres modelos y los ajustes finales de evaluación.
Integrar en este orden, o integrar directamente la tercera en main.
Falta la prueba de las funciones sobre el despliegue de Render después de fusionar.

### Consejos de descripción contextuales

El modelo ML.NET mantiene su evaluación binaria de detalle suficiente. El mensaje de ayuda se construye mediante reglas que reconocen colores, algunas marcas, rasgos y ubicación en los campos del formulario. No es texto generado por un modelo de lenguaje ni extracción semántica completa. Si reconoce un dato, no vuelve a solicitarlo. Por ejemplo, «Mochila negra» reconoce el color y propone otros detalles. Cambiar título, descripción o ubicación invalida la ayuda anterior.
