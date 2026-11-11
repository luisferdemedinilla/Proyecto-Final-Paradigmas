# Integración de Hello Mario Assets

Se han organizado los 5.850 recursos originales (aproximadamente 1 GB) mediante AssetDatabase.MoveAsset, conservando sus GUID y archivos .meta. No se han descartado modelos, variantes, texturas ni animaciones. El inventario completo y la correspondencia de rutas están en HelloMario-inventory.json y HelloMario-migration.json.

## Contenido revisado

| Categoría | Prefabs | FBX (modelos y animaciones) | Materiales | Texturas |
|---|---:|---:|---:|---:|
| Boss | 15 | 172 | 34 | 77 |
| Decor | 181 | 174 | 188 | 396 |
| Enemy | 98 | 758 | 169 | 349 |
| Item | 92 | 220 | 137 | 331 |
| Materials | 0 | 0 | 200 | 475 |
| NPC | 39 | 426 | 74 | 120 |
| Player | 11 | 1021 | 33 | 58 |

Player incluye Mario, Luigi y Toad, variantes de manos, expresiones y trajes, colas y bumeranes. Hay animaciones generales y variantes Luigi/Kinopio, además de movimientos para Peach/Rosalina. NPC contiene, entre otros, Peach, Rosalina, Yoshi, Captain Toad, Plessie y Sprixies. Los esqueletos de estos NPC no se han convertido en personajes jugables.

Enemy y Boss aportan enemigos y jefes para futuras fases. Item incluye bloques, monedas, potenciadores, metas, checkpoint, puertas, interruptores y trampolines. Decor aporta tuberías, plataformas, puentes, vallas, árboles y decoración. Materials contiene superficies de suelo, hierba, piedra, madera, metal, interiores y dulces.

## Organización

- `Assets/_Project/Prefabs/Characters/Playable/`: MarioPlayer, LuigiPlayer y ToadPlayer, con física, controles y Animator.
- `Assets/_Project/Prefabs/Characters/HelloMario/`: prefabs visuales originales de los jugadores y accesorios.
- `Assets/_Project/Prefabs/{Enemies,Bosses,NPC,Items,Environment}/HelloMario/`: biblioteca de prefabs del paquete.
- `Assets/_Project/Art/Characters/{Mario,Luigi,Toad}/HelloMario/`: Models, Textures, Materials y Animations/Gameplay.
- `Assets/_Project/Art/Characters/Shared/HelloMario/Animations/{Source,Tail}/`: animaciones FBX originales.
- `Assets/_Project/Art/Characters/{Enemies,Bosses,NPC}/HelloMario/`: arte de otros personajes.
- `Assets/_Project/Art/{Environment,Items}/HelloMario/`: escenarios, superficies y objetos.
- `Assets/_Project/Documentation/HelloMario/`: Readme e imagen de presentación originales.

Las carpetas originales vacías conservan sus .meta; no quedan recursos de contenido en HelloMarioAssets.

## Personajes y animaciones activas

MarioTest usa el nuevo Mario. El anterior está conservado como `Actors/MarioLegacy`, inactivo, y su prefab y animaciones originales siguen intactos. No actives ambos jugadores simultáneamente.

En modo edición, usa **Tools > Hello Mario > Use Mario / Luigi / Toad In Test Scene**. El cambio mantiene las teclas y ajustes de movimiento de la instancia, actualiza el objetivo de cámara y usa la cápsula y animaciones del personaje seleccionado. No se ha creado todavía un menú de selección dentro del juego.

| Estado | Animación original |
|---|---|
| Idle | Wait |
| Walk | Walk |
| Run | Dash |
| Jump | Jump; LuigiJump para Luigi |
| Fall | FallLoop |
| Crouch | SquatWait |
| AirSpin | SpinJumpL |

Los 21 clips derivados conservan las rotaciones animadas del paquete, adaptan la altura de pelvis y conservan las longitudes de brazos y piernas de cada personaje. Se corrige el apoyo en clips de suelo y se elimina el desplazamiento de la raíz: el Rigidbody sigue controlando el movimiento. El giro de 360° ajusta su reproducción a Air Spin Duration.

## Materiales y limitaciones

Los 835 materiales originales referenciaban un shader ausente. Se han adaptado a URP/Lit, conservando albedo, color, tiling, normales y emisión cuando existen. Sus datos originales y .meta están respaldados en `HelloMario-original-materials.zip`. Cuatro materiales son de color plano y no tenían textura base. Las referencias secundarias ausentes del shader anterior están registradas en el inventario; no se han inventado texturas.

La adaptación ofrece un aspecto básico compatible con URP. No reproduce efectos exclusivos del shader ausente, como agua especializada, contornos o iluminación toon. Esos efectos requieren trabajo posterior por material. El Readme del autor se conserva, incluyendo procedencia y herramientas utilizadas.

## Qué no se ha activado

- No se han colocado enemigos, jefes, monedas, bloques ni decorados nuevos en MarioTest, ni se han implementado sus comportamientos. Sus prefabs son recursos visuales; no son mecánicas completas.
- Las animaciones ajenas a los siete estados actuales se conservan como biblioteca, sin añadir acciones de natación, trepar, lanzar, ataques, etc.
- Peach, Rosalina, Yoshi y otros NPC se mantienen como recursos; no se han sustituido por error los rigs de Mario/Luigi/Toad. El Readme advierte que faltan animaciones de varios personajes.
- La imagen promocional y el Readme están en Documentation, no en el contenido jugable.
- No se han eliminado las animaciones anteriores ni el Mario original.

## Verificación

**Tools > Hello Mario > Validate Library And Animations** revisa los 5.850 GUID, los materiales/esqueletos de 436 prefabs (1.787 renderers) y muestrea los 21 clips derivados: bindings, límites de malla, longitudes de huesos, apoyo y giro completo. Informe: `Temp/HelloMarioAudit/validation.json`.

Las pruebas de movilidad y giro se ejecutan en Play Mode con teclado virtual desde **Tools > Mario Test**. Los informes están en Temp; los resultados consolidados de esta integración se conservan en Documentation/HelloMario-validation.json.

Resultado final: validación de biblioteca correcta y seis pruebas de Play Mode superadas (movilidad y giro para Mario, Luigi y Toad). MarioTest queda con Mario seleccionado y fuera de Play Mode.
