# Recursos de Mario

## Organización y conservación

Los originales permanecen intactos en `Mario/`, fuera de Assets. Se han importado copias en `Assets/_Project/Art/Characters/Mario/Models` y `Textures`, con materiales URP Lit en `Materials`. Los originales no tenían `.meta`; Unity ha generado GUID nuevos para las copias. Ningún recurso preexistente se ha movido, borrado ni sobrescrito.

En las copias DAE únicamente se han cambiado las rutas de imágenes a `../Textures/<nombre>.png`. Se ha verificado que el resto del XML coincide con el original y que cada PNG es idéntico byte a byte. Todos los PNG conservan su nombre.

## Identificación

Las identificaciones se basan en geometría, dimensiones relativas, esqueleto, materiales y aspecto de los atlas. No se dispone de documentación del autor ni confirmación de procedencia. Los nombres abreviados no se interpretan como modelos completos: B es cuerpo, H cabeza y C gorra separada en las familias observadas. La familia SM tiene un cuerpo de aproximadamente 12 unidades frente a 22 en M.

| Original | Copia organizada / nombre propuesto | Identificación | Confianza | Mallas | Triángulos | Nodos JOINT |
|---|---|---|---|---:|---:|---:|
| `MB_model.dae` | `Mario_Super_Body.dae` | Cuerpo Super Mario | Alta | 1 | 1126 | 17 |
| `MC_model.dae` | `Mario_Super_Cap.dae` | Gorra independiente de Super Mario | Alta | 1 | 182 | 1 |
| `MH_model.dae` | `Mario_Super_Head.dae` | Cabeza Super Mario; gorra y pelo alternativos | Alta | 4 | 903 | 1 |
| `MMB_model.dae` | `MMB_model.dae` | Probable cuerpo de ardilla voladora; identificación provisional | Media; se conserva el nombre | 1 | 1740 | 24 |
| `MMH_model.dae` | `MMH_model.dae` | Probable cabeza de ardilla voladora / variante P; provisional | Media; se conserva el nombre | 3 | 966 | 1 |
| `PLMB_model.dae` | `Mario_Propeller_Body.dae` | Cuerpo Mario hélice | Alta | 1 | 1264 | 17 |
| `PLMH_model.dae` | `Mario_Propeller_Head.dae` | Cabeza y hélice del casco | Alta | 4 | 887 | 2 |
| `PMB_model.dae` | `Mario_Penguin_Body.dae` | Cuerpo Mario pingüino | Alta | 1 | 1048 | 17 |
| `PMH_model.dae` | `Mario_Penguin_Head.dae` | Cabeza Mario pingüino | Alta | 2 | 538 | 1 |
| `SMB_model.dae` | `Mario_Small_Body.dae` | Cuerpo Mario pequeño | Alta | 1 | 1074 | 17 |
| `SMC_model.dae` | `Mario_Small_Cap.dae` | Gorra independiente de Mario pequeño | Alta | 1 | 186 | 1 |
| `SMH_model.dae` | `Mario_Small_Head.dae` | Cabeza Mario pequeño; gorra y pelo alternativos | Alta | 4 | 927 | 1 |

Los nodos JOINT no equivalen al número de huesos con pesos: Unity conserva únicamente los bindposes utilizados por cada malla. Todos los modelos tienen skinning y UV. No contienen clips de animación.

## Texturas y variantes

- `player_body.0.png` y `player_head.0.png`: Mario normal, camiseta/gorra rojas y peto azul.
- `player_body.1.png` y `player_head.1.png`: fuego, camiseta/gorra blancas y peto rojo.
- `player_body.2.png` y `player_head.2.png`: hielo, camiseta/gorra azul claro y peto rojo.
- Estas tres parejas se declaran tanto en los DAE de Mario grande como en los de Mario pequeño; el material difuso original usa la variante `.0`. Fuego/hielo no son otros DAE independientes.
- `player_eye.0.png` a `.6.png`: siete atlas de expresiones/ojos. `.0` es el predeterminado. No se ha añadido animación de expresiones.
- `player_mbody.0/.1.png` y `player_mhead.0/.1.png`: dos paletas para la familia MM, posiblemente ardilla normal/P. No se les asigna un nombre definitivo sin más evidencia.
- `player_pbody.png` y `player_phead.png`: traje de pingüino.
- `player_plbody.png`, `player_plface.png`, `player_plhead.png`: traje y casco de hélice.
- `P.png`: emblema P usado por `MMH_model.dae`; material con recorte alfa. `P_mask.png` figura como recurso auxiliar, sin enlace difuso.
- `lm_01p.png`, `lm_02p.png`, `star_env.png`, `p_env.png` y `puropera_env.png`: mapas auxiliares declarados por los DAE, sin conexión activa en sus efectos COLLADA exportados. Se conservan, sin inventar conexiones de normal/metallic/emisión en URP.
- `player_mbody_spc.png` y `player_mhead_spc.png`: mapas con apariencia especular que no están referenciados por ningún DAE. Conservados; no se aplican automáticamente como metallic/smoothness porque no se conoce su empaquetado original.

Se crearon 23 materiales URP Lit: texturas de color, variantes, expresiones y emblema P. Los materiales de cada modelo se remapean explícitamente en su ModelImporter; nombres internos compartidos como `mat_player_body` no provocan mezclas entre trajes. Se respeta el muestreo Mirror/Clamp/Repeat declarado en los efectos. Para las variantes de color sin efecto propio se hereda el muestreo de la variante predeterminada.

## Relaciones exactas por modelo

### MB_model.dae

- Materiales difusos activos: `mat_player_body` → `player_body.0.png`.
- Imágenes declaradas en el DAE: `player_body.0.png`, `player_body.1.png`, `player_body.2.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`.

### MC_model.dae

- Materiales difusos activos: `mat_player_hat` → `player_head.0.png`.
- Imágenes declaradas en el DAE: `player_head.0.png`, `player_head.1.png`, `player_head.2.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`.

### MH_model.dae

- Materiales difusos activos: `mat_player_eye` → `player_eye.0.png`; `mat_player_face` → `player_head.0.png`; `mat_player_hair` → `player_head.0.png`; `mat_player_hat` → `player_head.0.png`.
- Imágenes declaradas en el DAE: `player_eye.0.png`, `player_eye.1.png`, `player_eye.2.png`, `player_eye.3.png`, `player_eye.4.png`, `player_eye.5.png`, `player_eye.6.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`, `player_head.0.png`, `player_head.1.png`, `player_head.2.png`.

### MMB_model.dae

- Materiales difusos activos: `mat_player_body` → `player_mbody.0.png`.
- Imágenes declaradas en el DAE: `player_mbody.0.png`, `player_mbody.1.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`, `p_env.png`.

### MMH_model.dae

- Materiales difusos activos: `mat_P` → `P.png`; `mat_player_eye` → `player_eye.0.png`; `mat_player_face` → `player_mhead.0.png`.
- Imágenes declaradas en el DAE: `P.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`, `p_env.png`, `P_mask.png`, `player_eye.0.png`, `player_eye.1.png`, `player_eye.2.png`, `player_eye.3.png`, `player_eye.4.png`, `player_eye.5.png`, `player_eye.6.png`, `player_mhead.0.png`, `player_mhead.1.png`.

### PLMB_model.dae

- Materiales difusos activos: `mat_player_body` → `player_plbody.png`.
- Imágenes declaradas en el DAE: `player_plbody.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`.

### PLMH_model.dae

- Materiales difusos activos: `mat_player_eye` → `player_eye.0.png`; `mat_player_face` → `player_plface.png`; `mat_player_hat` → `player_plhead.png`.
- Imágenes declaradas en el DAE: `player_eye.0.png`, `player_eye.1.png`, `player_eye.2.png`, `player_eye.3.png`, `player_eye.4.png`, `player_eye.5.png`, `player_eye.6.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`, `player_plface.png`, `player_plhead.png`, `puropera_env.png`.

### PMB_model.dae

- Materiales difusos activos: `mat_player_body` → `player_pbody.png`.
- Imágenes declaradas en el DAE: `player_pbody.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`.

### PMH_model.dae

- Materiales difusos activos: `mat_player_eye` → `player_eye.0.png`; `mat_player_face` → `player_phead.png`.
- Imágenes declaradas en el DAE: `player_eye.0.png`, `player_eye.1.png`, `player_eye.2.png`, `player_eye.3.png`, `player_eye.4.png`, `player_eye.5.png`, `player_eye.6.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`, `player_phead.png`.

### SMB_model.dae

- Materiales difusos activos: `mat_player_body` → `player_body.0.png`.
- Imágenes declaradas en el DAE: `player_body.0.png`, `player_body.1.png`, `player_body.2.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`.

### SMC_model.dae

- Materiales difusos activos: `mat_player_hat` → `player_head.0.png`.
- Imágenes declaradas en el DAE: `player_head.0.png`, `player_head.1.png`, `player_head.2.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`.

### SMH_model.dae

- Materiales difusos activos: `mat_player_eye` → `player_eye.0.png`; `mat_player_face` → `player_head.0.png`; `mat_player_hair` → `player_head.0.png`; `mat_player_hat` → `player_head.0.png`.
- Imágenes declaradas en el DAE: `player_eye.0.png`, `player_eye.1.png`, `player_eye.2.png`, `player_eye.3.png`, `player_eye.4.png`, `player_eye.5.png`, `player_eye.6.png`, `lm_01p.png`, `lm_02p.png`, `star_env.png`, `player_head.0.png`, `player_head.1.png`, `player_head.2.png`.

## Prefab y validación

`Assets/_Project/Prefabs/Characters/Mario.prefab` representa Super Mario normal en pose T. Usa instancias anidadas de `Mario_Super_Body.dae` y `Mario_Super_Head.dae`; la cabeza está vinculada al hueso `face_1`. No se añade la gorra independiente porque la cabeza ya contiene una.

La malla `Head/polygon0` (pelo de la alternativa sin gorra) está desactivada en el prefab para evitar que atraviese la gorra; permanece intacta en el modelo. Para una futura versión sin gorra habrá que alternarla con `Head/polygon3`.

La raíz Mario tiene escala 1. Su hijo Visual aplica escala 0,05 a las unidades del recurso: altura aproximada de 1,69 unidades de Unity. No se han reescrito las mallas ni los esqueletos. El prefab conserva cinco SkinnedMeshRenderer, cuatro activos, y usa los atlas de cuerpo, cabeza y ojos normales. No tiene Rigidbody, Collider ni scripts de comportamiento. No está colocado en la escena.

Comprobaciones: 12 DAE cargados como GameObject; recuentos de triángulos coincidentes; índices geométricos válidos; UV y bindposes presentes; todas las rutas de imágenes resueltas; texturas originales intactas; materiales URP con BaseMap enlazado; huesos del prefab sin referencias nulas; captura visual del ensamblaje revisada. SampleScene permanece activa, con sus tres objetos originales y sin cambios pendientes.

La utilidad `Assets/_Project/Editor/MarioAssetSetup.cs` solo se ejecuta en el editor; no forma parte del comportamiento del personaje. Se usó porque el evaluador C# de Pipeline fallaba por codificación. Los comandos de importación/creación rechazan destinos existentes. Las salidas de diagnóstico están en Temp/MarioAudit (ignoradas por Git).

## Corrección del mapeado de ojos

Los atlas `player_eye.0` a `.6` contienen media cara. Sus materiales necesitan tiling horizontal 2 y vertical 1, con offset 0 y Mirror en U; el tiling 1 anterior dibujaba un único ojo y ceja a lo ancho de la cara. Se han corregido los siete materiales conservando sus GUID, texturas y mallas. La captura frontal `Mario-preview.png` confirma dos ojos y dos cejas. El prefab y el resto de materiales no se han modificado en esta corrección.
