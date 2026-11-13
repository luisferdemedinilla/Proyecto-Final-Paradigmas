# Creator

Abre `Assets/Scenes/creator.unity` y pulsa Play.

- Cambia «juego1» arriba a la izquierda y pulsa Guardar.
- Selecciona una imagen y coloca piezas con clic o arrastrando.
- En BLOQUES, el cuadrado vacío borra casillas. También funciona el clic derecho.
- Un único personaje de inicio y un único final entre palo, bandera y torre.
- Mario, Luigi y Toad; Brick, Brick Hard y Block Question; Goomba; palo, bandera, torre y moneda. No hay otras opciones.

El archivo se guarda como `Creator-<nombre>.json` en `Application.persistentDataPath`, con el nombre dentro del diseño. Guardar otra vez con el mismo nombre actualiza ese archivo. Ctrl+Z permite deshacer al editar la cuadrícula.

Las miniaturas son renders frontales de los prefabs originales. `CreatorScreen.TryGetSpawn` devuelve el prefab elegido y su posición para la futura ejecución del nivel. Esta pantalla todavía es el editor, no el nivel jugable.

Todo lo añadido o modificado está dentro de Creator y de su escena. `Creator.Editor.CreatorSetup.ValidateRules()` valida las reglas de colocación. `Creator.Editor.CreatorSimplify.Apply()` ajusta la interfaz existente y genera las miniaturas desde los modelos; solo funciona en creator.unity fuera de Play.
