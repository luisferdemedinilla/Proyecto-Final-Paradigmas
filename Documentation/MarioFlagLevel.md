# Nivel de la bandera

Escena: `Assets/_Project/Scenes/MarioFlagLevel.unity`.

Hay cuatro plataformas de ocho bloques: BlockEmpty, Brick, BrickHard y BlockQuestion. Sus colliders se ajustan a una cuadrícula de una unidad mediante la escala de las instancias; los prefabs originales no cambian. Las plataformas están separadas por huecos que se pueden saltar con los controles habituales.

La moneda de la primera plataforma es opcional: recogerla no finaliza el nivel. El Goomba persigue al jugador; el contacto lateral causa derrota y un pisotón lo elimina. Caer por un hueco también causa derrota.

## Meta

El prefab `Assets/_Project/Prefabs/Items/Gameplay/FlagGoal.prefab` contiene los modelos Goal Pole y Goal Flag. Al tocar el mástil, FlagGoal bloquea los controles, reproduce las poses de agarre y deslizamiento del personaje y baja la bandera. Al terminar el descenso se declara la victoria y se reproduce la celebración.

Se reutilizan GoalLow y el ondeo Wait de Goal, además de GoalPoleWait, GoalPoleFall y GoalPose de los personajes. Las copias adaptadas están en `Art/Items/HelloMario/Goal/Animations/Gameplay/` y en la carpeta `HelloMario/Animations/Goal/` de Mario, Luigi y Toad. El prefab selecciona los clips según el controlador del personaje. Las animaciones se reproducen mediante un PlayableGraph temporal; el controlador normal se restaura al reiniciar. No se usa Root Motion para desplazar al jugador.

## Reinicio

R o Reintentar después de ganar/perder restauran al jugador, el Goomba, la moneda, la bandera, los Brick destruidos y los BlockQuestion utilizados. También se eliminan los contenidos que hayan generado esos bloques.

La condición de victoria pertenece al nuevo FlagLevelController. No se modificaron el controlador del nivel anterior, sus escenas, los scripts de movimiento ni los prefabs jugables existentes.

## Comprobación

Tools > Flag Goal > Validate Level ejecuta pruebas en Play Mode. Incluyen un recorrido continuo con teclado por todas las plataformas hasta el mástil, recogida de moneda sin victoria, derrota y pisotón del Goomba, descenso de la bandera, victoria única y reinicio de los recursos del nivel. El Goomba se elimina en la prueba de pisotón antes de iniciar el recorrido continuo. Las pruebas de interacciones aisladas usan recolocaciones controladas del personaje. Informe: `MarioFlagLevel-validation.json`.
