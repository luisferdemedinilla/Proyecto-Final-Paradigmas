# Brick rompible

Arrastra `Assets/_Project/Prefabs/Items/Gameplay/Brick.prefab` a la escena. Incluye collider y el componente `BreakableBrick`; no necesita Rigidbody ni cambios en los personajes.

- Se rompe al recibir un impacto ascendente por debajo o un golpe hacia abajo por encima.
- Caminar contra él y aterrizar normalmente no lo rompen.
- Al romperse desaparece la colisión inmediatamente y se reproduce la destrucción durante aproximadamente 1.47 segundos. Los fragmentos son visuales y no bloquean al jugador.
- `ResetBlock()` permite restaurarlo si posteriormente se integra en un sistema de reinicio. El reinicio actual de MarioFirstLevel no está conectado a este nuevo prefab.

La destrucción reutiliza `Brick Break.prefab` y las curvas de `Break.fbx`, adaptadas a la escala y pose del modelo importado, con reducción final de los fragmentos. Los recursos generados están en `Art/Items/HelloMario/Block/Animations/Gameplay/BrickBreak.anim` y `BrickBreak.controller`. El modelo intacto no contiene los siete huesos que necesita la animación de destrucción; por eso se intercambia por el modelo fragmentado al romperse.

No se han sustituido los bloques del suelo o escalones, ni se han cambiado personajes, modelos originales, BlockEmpty, BlockQuestion o BrickHard. Las pruebas de colisión usan objetos temporales en Play Mode sin guardar cambios en la escena.

Menú de comprobación: Tools > Brick > Validate Collisions. Informe: `Brick-validation.json`.
