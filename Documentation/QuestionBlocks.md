# BlockQuestion y BlockEmpty

Prefabs jugables en `Assets/_Project/Prefabs/Items/Gameplay/`:

- `BlockEmpty.prefab`: modelo vacío y un BoxCollider sólido. No tiene comportamiento de rotura ni Rigidbody.
- `BlockQuestion.prefab`: entrega una vez el contenido al recibir un salto por debajo o un golpe hacia abajo por encima. Cambia al modelo vacío conservando el collider. Los contactos laterales y los aterrizajes normales no lo activan.

En el componente QuestionBlock, el campo **Content Prefab** permite elegir el objeto que aparece encima del bloque; por defecto es GoldCoin. **Content Clearance** controla la separación vertical respecto a la cara superior. La moneda se recoge con su comportamiento habitual, sin finalizar el nivel. Los efectos de setas o estrellas no están implementados: para usarlos, el prefab asignado deberá incluir su propio comportamiento.

`ResetBlock()` restaura el estado inicial y elimina el contenido generado por ese bloque. No se ha conectado al reinicio de MarioFirstLevel ni se han añadido instancias a la escena. Los personajes, los otros bloques y las escenas existentes permanecen sin cambios.

Menú de pruebas: Tools > Blocks > Validate Question And Empty. Las pruebas usan objetos temporales en Play Mode. Informe: `QuestionBlocks-validation.json`.
