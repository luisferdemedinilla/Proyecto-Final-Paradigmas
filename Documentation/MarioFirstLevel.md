# Primer nivel jugable

Escena: `Assets/_Project/Scenes/MarioFirstLevel.unity`. Pulsa Play para jugar.

- A/D: desplazamiento con aceleración. W o Espacio: salto de altura variable.
- Pulsa salto otra vez en el aire para girar. Pulsa S después de saltar para hacer el golpe hacia abajo. En tierra, S agacha al personaje.
- El Goomba patrulla y persigue al personaje cercano. Tocarlo de lado causa derrota; caer sobre su cabeza o golpearlo desde arriba lo elimina y produce un rebote.
- Caer por el hueco también causa derrota. R o el botón Reintentar reinician tras ganar o perder.
- Recoger la moneda dorada designada en esta escena causa victoria.

## Recursos y configuración

Los prefabs jugables MarioPlayer, LuigiPlayer y ToadPlayer, en `_Project/Prefabs/Characters/Playable`, tienen el golpe y sus tres animaciones: GroundPoundStart, GroundPoundFall y GroundPoundLand. Las teclas y los tiempos/velocidad del golpe se editan en MarioPlayerController. La escena instancia a Mario; no se ha añadido un selector de personajes.

Los prefabs reutilizables del enemigo y la moneda están en `Prefabs/Enemies/Gameplay/Goomba.prefab` y `Prefabs/Items/Gameplay/GoldCoin.prefab`. GoombaEnemy permite ajustar velocidad, alcance de persecución y límites de patrulla. CollectibleCoin controla la recogida y el giro, pero no decide victorias. DemoLevelController se suscribe únicamente a la moneda asignada como Goal Coin en este nivel; las otras monedas no terminan niveles.

Se conservan los modelos originales y MarioTest. No se usa Root Motion; la física mantiene el movimiento en XY.

## Corrección del movimiento, 11 de noviembre de 2026

Se mantienen walkSpeed 3.5, runSpeed 6, acceleration 8, braking 18 y airAcceleration 6. Los prefabs HelloMario vuelven a usar el material físico anterior MarioTest (fricción 0.15 y combinación Minimum), que faltaba y dejaba actuar la fricción predeterminada de 0.6.

La carrera utiliza Run.fbx en lugar de Dash.fbx. Las transiciones a Jump exigen un salto o rebote real, además de estar ascendiendo; un impulso de contacto no se interpreta como salto. Las transiciones normales quedan bloqueadas durante el golpe y sus parámetros se actualizan antes de seleccionar la fase. La voltereta conserva su centro horizontal sobre el collider, evitando que la rotación alrededor de los pies desplace visualmente el modelo en Z.

## Verificación

Las pruebas en Play Mode comprueban la carrera y las tres fases del golpe con Mario, Luigi y Toad. El informe `Mario-motion-validation.json` recoge los resultados. Las pruebas del nivel cubren aparición, agacharse, golpe, persecución y animación del Goomba, derrota lateral, pisotón, rebote, reinicio, moneda dorada giratoria, victoria por la moneda designada y recogida de otra moneda sin victoria. Resultados en `MarioFirstLevel-validation.json`.

Los menús de validación están en Tools > Mario Demo. La prueba Trace Motion usa una plataforma temporal durante Play Mode; no modifica el escenario guardado. Las pruebas verifican situaciones concretas mediante teclado virtual y algunas recolocaciones del personaje; no equivalen a una partida manual completa.

### Uniones de bloques y límites del Goomba

La prueba anterior sobre una plataforma continua no cubría las uniones entre colliders del nivel. Se reprodujeron impulsos verticales pequeños al cruzarlas: Grounded descartaba el apoyo por superar 0.15 de velocidad ascendente y activaba Fall. Ahora ese descarte solo se aplica durante un salto real; además de la esfera, se comprueba el apoyo bajo tres puntos del pie. No se mantiene Grounded mediante un temporizador, ni se extiende el apoyo sobre huecos. Trace Block Motion repite el recorrido sobre los bloques originales con los tres personajes; resultados antes/después en `Mario-block-seams-validation.json`.

Los límites de patrulla del Goomba ya no fuerzan velocidad cero durante la persecución. Puede salir de esa zona para acercarse al jugador. Detecta suelo delante y paredes físicas; en patrulla gira ante obstáculos, y en persecución se detiene ante un hueco o pared sin reproducir una carrera inmóvil. No salta ni sube escalones. La prueba del nivel comprueba persecución más allá de X=17 y parada ante el hueco real.
