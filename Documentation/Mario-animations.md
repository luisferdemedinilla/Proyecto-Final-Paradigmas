# Animaciones iniciales de Mario

## Archivos

`Assets/_Project/Art/Characters/Mario/Animations/` contiene `Idle.anim`, `Walk.anim`, `Run.anim`, `Jump.anim`, `Fall.anim` y `Mario.controller`.

El Animator está añadido a la raíz del prefab existente `Assets/_Project/Prefabs/Characters/Mario.prefab`. Se conserva su GUID, jerarquía, referencias a modelos/materiales y pose de reposo. `Apply Root Motion` está desactivado. No se han añadido componentes de físicas ni scripts de movimiento.

## Inspección del rig

El cuerpo utiliza Generic. Antes de crear los clips no había AnimationClip reutilizables en Assets ni Animator en el prefab. El cuerpo tiene 14 huesos con pesos; cada una de las cuatro mallas de cabeza está vinculada a un hueso. La cabeza completa sigue al hueso `face_1` del cuerpo.

Se verificaron pesos normalizados, índices válidos y correspondencia entre matrices de bind y pose de reposo (error máximo inferior a 0,00001). No se modificó ningún DAE original ni ninguna geometría importada.

Cada clip anima 14 huesos mediante 56 curvas de cuaterniones. No hay curvas de traslación, escala ni desplazamiento de la raíz. Las rotaciones de reposo se utilizan como referencia; no se convierten a cero ni se reemplaza el esqueleto.

| Clip | Duración | Bucle | Comportamiento inicial |
|---|---:|---|---|
| Idle | 2 s | Sí | Brazos relajados y respiración suave |
| Walk | 1 s | Sí | Balanceo alterno de brazos y piernas |
| Run | 0,64 s | Sí | Zancada mayor, codos flexionados y ligera inclinación |
| Jump | 0,55 s | No | Pose de impulso con puño levantado; mantiene la pose final |
| Fall | 1,2 s | Sí | Brazos abiertos y piernas preparadas para aterrizar |

Son animaciones procedurales iniciales en el sitio, no capturas originales del juego. Jump/Fall solo representan la pose: no elevan ni desplazan al personaje. Todavía no hay ajuste de pies al terreno ni sincronización con una velocidad física.

## Parámetros del Animator

| Parámetro | Tipo | Valor inicial | Uso |
|---|---|---|---|
| Speed | Float | 0 | Velocidad normalizada: 0 reposo, 0,35 andar, 1 correr |
| Grounded | Bool | true | Si el personaje está apoyado |
| VerticalSpeed | Float | 0 | Positivo al subir; cero o negativo al caer |

Idle/Walk/Run cambian con Speed mientras Grounded es true. Los umbrales son 0,1 y 0,65. Any State entra en Jump con Grounded=false y VerticalSpeed>0,05; entra en Fall con Grounded=false y VerticalSpeed<0,05. Al aterrizar se pasa por Idle y después al estado de locomoción correspondiente. Transiciones sin exit time, de 0,09 a 0,12 segundos. Los valores exactamente iguales a los umbrales mantienen el estado previo hasta cruzarlos.

## Previsualización

Abre **Tools > Mario Animations > Preview Animations**.

1. Selecciona Idle, Walk, Run, Jump o Fall.
2. Activa Reproducir, o arrastra Tiempo para revisar una pose.
3. Usa Camara para revisar el personaje desde otros ángulos.
4. Seleccionar clip en Project muestra el archivo para editarlo con las herramientas de Unity.

La ventana usa una instancia en una escena de previsualización aislada y no modifica la escena de trabajo. Para revisar Jump cómodamente, la ventana lo reproduce repetidamente; el archivo Jump.anim sigue teniendo Loop Time desactivado.

También puedes abrir Mario.controller en la ventana Animator. Los scripts de creación, verificación y previsualización están en `Assets/_Project/Editor/` y no se incluyen como comportamiento del personaje en una build.

## Verificación realizada

- 61 muestras por clip: bindings válidos, huesos con posiciones y escalas locales constantes, vértices finitos y malla que responde a las rotaciones.
- Los cuatro clips en bucle cierran sin diferencia angular entre inicio y final.
- Revisión de 20 renders, cuatro fases por clip, sin deformaciones anómalas observadas.
- 13 casos evaluados con AnimatorControllerPlayable y PlayableGraph en el propio Editor: locomoción, salto, caída, caída desde un borde y aterrizajes andando/corriendo. Se comprueba el estado efectivo, movimiento de los huesos y raíz inmóvil.
- Previsualización comprobada mediante avance del contador de fotogramas e imágenes renderizadas diferentes.
- Originales comprobados mediante SHA-256. SampleScene continúa activa. En la revisión final contiene cinco objetos raíz y cambios pendientes; no se ha guardado ni revertido la escena. Las pruebas y la previsualización utilizan escenas aisladas.

Las pruebas son de animación en el Editor. No se ha probado un personaje jugable en Play Mode, colisiones, desplazamiento o contacto con el terreno: todavía no se han implementado.

Resultados detallados: `Temp/MarioAnimationAudit/` (diagnósticos locales ignorados por Git). Resumen visual: `Mario-animation-poses.jpg`.
