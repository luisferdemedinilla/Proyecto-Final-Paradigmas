# MarioTest

Escena: `Assets/_Project/Scenes/MarioTest.unity`.

## Escenario

Nivel 2.5D de cubos con tres tramos de suelo, cuatro plataformas elevadas, cuatro escalones, tres paredes y dos huecos (x=9–11 y x=18–20). Los 14 bloques sólidos tienen BoxCollider; las franjas de hierba y la marca de inicio son decorativas y no duplican colisiones.

Jerarquía:

- Environment: Ground, Platforms, Steps, Walls y Visual Details.
- Actors: instancia vinculada de Mario.prefab.
- Markers: Mario Spawn y posiciones de los dos huecos.
- Cameras: Main Camera lateral ortográfica.
- Lighting: Directional Light.

La cámara está en (10,5; 2,8; -25), mira hacia +Z y tiene Orthographic Size 8,3. El encuadre completo se ha comprobado a 1600×900 (16:9). Mario comienza en (-1,5; 0,15; 0), orientado hacia +X.

## Física de la instancia

El prefab original solo tenía Transform, Animator y SkinnedMeshRenderer. Se verificó el controlador y sus cinco estados antes de crear la escena.

La instancia de prueba añade exactamente un Rigidbody 3D y un CapsuleCollider; no se ha aplicado ningún override al prefab original.

- Rigidbody: masa 1, gravedad activada, dinámico, interpolación, Continuous Dynamic.
- Restricciones: Freeze Position Z y Freeze Rotation X/Y/Z. X e Y quedan libres.
- Cápsula vertical: radio 0,32, altura 1,68 y centro (0; 0,84; 0).
- Material físico con fricción 0,15 y sin rebote.
- Animator y controlador existentes; Apply Root Motion desactivado.

Se ha comprobado por SHA-256 que Mario.prefab, su .meta, el Animator Controller y todos los clips y sus .meta permanecen intactos.

## Prueba realizada

Se ejecutó realmente Play Mode con MarioTest como escena de inicio temporal. Se comprobó que avanzaban los fotogramas y que:

1. Mario cae y se apoya en el suelo, con la raíz en y≈0.
2. Se apoya sobre un escalón, en y≈0,7.
3. Se apoya sobre una plataforma, en y≈2,2.
4. La pared intermedia frena el movimiento en X, mientras una velocidad en Z y un impulso de giro no sacan al personaje del plano ni cambian su orientación.
5. El hueco deja caer a Mario por debajo del nivel.

Las posiciones y fuerzas de comprobación se aplican únicamente desde una utilidad del Editor durante la prueba; no hay scripts de control ni físicas adicionales en el personaje. Se ha revisado también la captura de la cámara lateral.

## Cómo abrirlo

Guarda tus cambios pendientes en SampleScene cuando quieras conservarlos y abre MarioTest desde Project. Al pulsar Play, Mario activa Idle y se apoya en el suelo. **Todavía no hay controles de teclado, caminar ni saltar mediante input**, ni un script que sincronice Grounded/VerticalSpeed con la física. Esto se añadirá en la siguiente fase.

Para repetir las pruebas automáticas: **Tools > Mario Test > Run Physics Validation**. La utilidad restaura la escena de edición y la configuración anterior de inicio de Play Mode al terminar. Los resultados locales están en `Temp/MarioTestAudit/physics-validation.json` y la vista en `Documentation/MarioTest.png`.

SampleScene y sus cinco objetos raíz con cambios pendientes se conservaron sin guardar ni descartar. No se han añadido enemigos, monedas, bloques especiales ni un editor de niveles.
