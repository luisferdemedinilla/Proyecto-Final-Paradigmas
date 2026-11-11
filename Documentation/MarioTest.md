# MarioTest

Escena: `Assets/_Project/Scenes/MarioTest.unity`.

Actualización HelloMario: la escena usa ahora el nuevo modelo y animaciones. Mario, Luigi y Toad pueden seleccionarse desde **Tools > Hello Mario > Use … In Test Scene**. Véase [la integración y sus limitaciones](HelloMario-integration.md). Las dimensiones de las cápsulas se ajustan al personaje; los datos de la prueba inicial que siguen documentan la versión original.

## Escenario

Nivel 2.5D de cubos con tres tramos de suelo, cuatro plataformas elevadas, cuatro escalones, tres paredes y dos huecos (x=9–11 y x=18–20). Los 14 bloques sólidos tienen BoxCollider; las franjas de hierba y la marca de inicio son decorativas y no duplican colisiones.

Jerarquía:

- Environment: Ground, Platforms, Steps, Walls y Visual Details.
- Actors: instancia vinculada de Mario.prefab.
- Markers: Mario Spawn y posiciones de los dos huecos.
- Cameras: Main Camera lateral ortográfica.
- Lighting: Directional Light.

La cámara lateral sigue suavemente a Mario, con desplazamiento (2; 1,8; -25) y Orthographic Size 4,5. Mario comienza en (-1,5; 0,15; 0), orientado hacia +X.

## Física de la instancia

El prefab original solo tenía Transform, Animator y SkinnedMeshRenderer. Se verificó el controlador y sus cinco estados antes de crear la escena.

La instancia de prueba añade exactamente un Rigidbody 3D y un CapsuleCollider; no se ha aplicado ningún override al prefab original.

- Rigidbody: masa 1, gravedad activada, dinámico, interpolación, Continuous Dynamic.
- Restricciones: Freeze Position Z y Freeze Rotation X/Y/Z. X e Y quedan libres.
- Cápsula vertical: radio 0,32, altura 1,68 y centro (0; 0,84; 0).
- Material físico con fricción 0,15 y sin rebote.
- Animator y controlador existentes; Apply Root Motion desactivado.

Se ha comprobado por SHA-256 que Mario.prefab, su .meta, el Animator Controller y todos los clips y sus .meta permanecen intactos.

## Prueba inicial del escenario

Se ejecutó realmente Play Mode con MarioTest como escena de inicio temporal. Se comprobó que avanzaban los fotogramas y que:

1. Mario cae y se apoya en el suelo, con la raíz en y≈0.
2. Se apoya sobre un escalón, en y≈0,7.
3. Se apoya sobre una plataforma, en y≈2,2.
4. La pared intermedia frena el movimiento en X, mientras una velocidad en Z y un impulso de giro no sacan al personaje del plano ni cambian su orientación.
5. El hueco deja caer a Mario por debajo del nivel.

## Controles y movilidad

Pulsa Play en MarioTest. Selecciona Actors/Mario y su componente MarioPlayerController para cambiar teclas y parámetros en el Inspector.

- A/D: izquierda/derecha. La velocidad aumenta progresivamente hasta Run Speed (6), con Acceleration (8). Braking (18) frena al soltar y Air Acceleration (6) controla la respuesta en el aire.
- W o espacio: salto variable. Un toque da un salto corto; mantener la tecla añade impulso durante Max Jump Hold Time (0,3 s). Minimum Jump Speed (4) fija el impulso inicial, Jump Hold Acceleration (18) la asistencia y Jump Speed (8,5) el límite de velocidad vertical. Soltar cancela la asistencia; volver a pulsar en el aire no la reinicia.
- Vuelve a pulsar W o espacio después de soltar el salto: giro aéreo de 360°, una vez por salto. Air Spin Duration (0,45 s) controla su duración y Air Spin Fall Speed (1,5) limita temporalmente la caída. Funciona durante la subida o bajada tras saltar, sin añadir un segundo impulso ascendente. Aterrizar permite usarlo otra vez; caer de un borde sin saltar no lo activa.
- S: agacharse. La pose dobla las piernas y ajusta la altura visual según los tobillos, también durante las transiciones, para conservar el apoyo. La cápsula reduce su altura a 1,4 manteniendo la base. Un techo impide levantarse.
- R: volver al inicio. También se reinicia al caer por debajo de Y=-10.
- Require Run Key está desactivado: A/D alcanzan la velocidad máxima sin Shift. Si se activa, Run Key (Shift izquierdo) permite superar Walk Speed.

El movimiento queda restringido al plano XY y Root Motion permanece desactivado. La instancia usa MarioGameplay.controller; el prefab original, su controlador y los cinco clips originales se conservan.

## Validación de movilidad

**Tools > Mario Test > Validate Mobility Refinements** ejecuta pruebas en Play Mode con teclado virtual: aceleración, velocidad máxima, frenado, apoyo de la malla al agacharse y cuatro duraciones de salto. Crea un suelo temporal que desaparece al finalizar y restaura la configuración previa de inicio de Play Mode. Resultados en `Temp/MarioMobilityAudit/results.json` y captura en `Temp/MarioMobilityAudit/crouch.png`.

La antigua prueba Run Physics Validation corresponde a la fase anterior a los controles. Para comprobar los cambios actuales utiliza Validate Mobility Refinements.

**Tools > Mario Test > Validate Air Spin** comprueba en Play Mode la activación mediante teclado, la reducción de caída, el giro del esqueleto, la recuperación de gravedad, el límite de un uso, el reinicio al aterrizar y la cancelación al reaparecer. Resultados en `Temp/MarioAirSpinAudit/results.json`. El clip `AirSpin.anim` y el estado AirSpin están en el controlador MarioGameplay.
