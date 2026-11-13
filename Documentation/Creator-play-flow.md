# Inicio y niveles creados

Abre `Assets/Scenes/Inicio.unity` y pulsa Play. Es la primera escena de Build Settings.

- Crear abre un tablero vacío y propone un nombre que no exista todavía.
- Guarda el diseño antes de pulsar Inicio: salir del creador descarta las ediciones no guardadas.
- Jugar enumera `Creator-*.json` de `Application.persistentDataPath`, con seis niveles por página.
- Pulsa un nivel para construir el escenario con las piezas del catálogo. Los huecos se conservan.
- Cada nivel necesita un personaje. Una bandera, palo de meta o torre permite completarlo al tocarla. Sin meta se puede explorar el nivel.
- A/D mueven; W/Espacio saltan; S agacha o ejecuta el golpe descendente; Esc vuelve a la lista. Solo se puede reintentar desde la pantalla de derrota o victoria; R no reinicia durante la partida.
- Los bloques tienen colisiones de una casilla; Goombas y monedas reutilizan los controladores existentes.
- Los archivos incompatibles o ilegibles muestran un aviso y no se ejecutan.

## Validación en Unity 6000.6.7f1

Compilación del editor sin errores. Comprobados los botones Crear / Inicio / Jugar / prueba, tablero vacío, listado del archivo existente, un personaje y un Goomba, apoyo físico sobre los bloques, movimiento y salto con teclado virtual, derrota, reinicio y victoria mediante contacto con un trigger de meta temporal. Revisadas capturas de Inicio, creador, lista y escenario. El archivo prueba no se modificó. No se ha generado un ejecutable.
