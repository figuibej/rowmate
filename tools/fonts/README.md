# tools/fonts — la derivada tabular y las tablas de kerning (It. 4a, WP-D0)

Dos programas de Python con **fontTools 4.68.0 fijado**, sin red (la caché de `uv`), que dejan en el repo dos cosas que UI Toolkit no sabe sacar de una fuente:

| Programa | Qué escribe | Pedido |
|---|---|---|
| `freeze_tnum.py` | `Assets/RowMate/UI/Fonts/RowMateNumericSC/RowMateNumericSC-{SemiBold,Bold,ExtraBold}.ttf` y su `OFL.txt` | DD4a-07 |
| `kern_pairs.py` | `Assets/RowMate/UI/Fonts/Kerning/<cara>.pairs.json` para Inter 400, 500, 600 y 700 y RowMate Numeric SC 700 | DD4a-06 (K2), DN4a-22 |

Fuentes del diseño: [00-deuda-diseno §3](../../docs/migration/iterations/04a-motor-hud-funcional/00-deuda-diseno.md) (método, reglas K1 a K6 y la receta), [04-design-notes §4.9](../../docs/migration/iterations/04a-motor-hud-funcional/04-design-notes.md) y [02-tech-design §6.14](../../docs/migration/iterations/04a-motor-hud-funcional/02-tech-design.md) (cómo los integra el *bootstrapper*). Slot: [ASSET_SLOTS §3.7](../../docs/migration/design/ASSET_SLOTS.md).

## Los comandos

Desde la raíz del repositorio, **en este orden** (la lista de la derivada 700 se calcula sobre la derivada):

```
uvx --offline --from fonttools==4.68.0 python tools/fonts/freeze_tnum.py
uvx --offline --from fonttools==4.68.0 python tools/fonts/kern_pairs.py
```

- Los dos son reproducibles: dos corridas seguidas dan los mismos bytes (la derivada conserva la fecha de Barlow, `recalcTimestamp=False`; las listas van ordenadas y sin datos de la corrida).
- `freeze_tnum.py` sale con 1 si las diez cifras de alguna cara no quedan en los `*.tf` con un solo avance (507, 521 y 535 unidades) o si algún nombre sigue diciendo Barlow.
- Sin la caché de `uv` (otra PC, sin red), `uv` necesita bajar fontTools una vez: sacar `--offline`.

## RowMate Numeric SC (DD4a-07)

Barlow Semi Condensed trae cifras tabulares **dibujadas para eso** (`zero.tf` a `nine.tf`, en la feature `tnum`), pero UI Toolkit no aplica features de OpenType. La derivada las congela en el `cmap`:

- las diez cifras apuntan a los `*.tf`: un solo avance por peso (0,507 em en 600, 0,521 en 700, 0,535 en 800), el `1` con pie y sin pares de kerning entre cifras;
- los nombres 1, 3, 4, 6, 16 y 21 pasan de "Barlow Semi Condensed" a "RowMate Numeric SC": la SIL OFL 1.1 pide otro nombre para una versión modificada. El `OFL.txt` es el de Barlow con una nota de la modificación;
- ninguna otra tabla cambia. Equivale a `pyftfeatfreeze -f tnum -R "Barlow Semi Condensed/RowMate Numeric SC"` (opentype-feature-freezer, que no está en la caché sin red).

Los `.ttf` de Barlow quedan versionados como entrada de la receta.

## Las listas de kerning K2 (DD4a-06)

Con `getFontFeatures = false` (K1) TextCore aplica solo los pares de la tabla versionada de cada `FontAsset`. K2 fija esa tabla: **todos** los pares GPOS `kern` distintos de 0 entre los caracteres del **juego fijo**, y solo esos.

- **Juego:** los textos de `Assets/RowMate/Localization/strings.{es,en}.json`, el ASCII imprimible (U+0020 a U+007E), `áéíóúüñÁÉÍÓÚÜÑ¿¡` y `×±·°`. Hoy son 116 caracteres, 115 con glifo (el `📈` del catálogo no está en Inter). Si cambia el catálogo, se vuelve a correr `kern_pairs.py` (K3).
- **Caras:** las que dibujan texto. Inter 400, 500, 600 y 700, y la derivada 700 (los valores `num.stat` del panel del simulador; DN4a-22, en lugar de Barlow 700). La derivada 600 y 800 no llevan tabla: solo las dibuja `TabularNumber`, celda por celda (K4).
- **Método:** la feature `kern` de GPOS, `PairPos` de formatos 1 y 2 (también dentro de `Extension`); dentro de cada *lookup* gana el primer *subtable* que cubre el primer glifo y los *lookups* se suman. Valor = `XAdvance` del primero + `XPlacement` del segundo. Es el método de 03-rutinas/04-design-notes §14.7: da exactamente los valores que TextCore había escrito en las tablas parciales de las It. 2 y 3 (0 diferencias en los 2 882 pares en común; ninguno de esos falta en la lista).

Formato de `<cara>.pairs.json`:

| Campo | Qué es |
|---|---|
| `face`, `source`, `sourceSha256` | la cara, su `.ttf` (ruta del repo) y el SHA-256 del archivo con el que se calculó: si cambia la fuente, la lista queda vieja y se nota |
| `unitsPerEm`, `glyphs` | unidades por em de la fuente; glifos del juego que tiene la cara |
| `characters` | el juego, como puntos de código ordenados |
| `pairs` | `[primero, segundo, unidades]`: índices de glifo de la fuente (los de TextCore) y el ajuste en unidades de la fuente, ordenados por (primero, segundo) |

El *bootstrapper* (`FontAssetBuilder`, WP-B0) escribe cada par en `m_FontFeatureTable.m_GlyphPairAdjustmentRecords` con `XAdvance` del primero = unidades / `unitsPerEm` × tamaño de muestreo (48 en Inter, 90 en la derivada) y lo demás en 0, dentro de `GeneratedAssetGuard.AllowWrites()`.

| Cara | Pares |
|---|---|
| Inter 400 | 1 445 |
| Inter 500 | 1 695 |
| Inter 600 | 1 695 |
| Inter 700 | 1 695 |
| RowMate Numeric SC 700 | 1 272 |

## Reglas

- Ningún archivo de esta carpeta lleva rutas de la PC: el repo se ubica desde el propio script (`Path(__file__).parents[2]`).
- La entrada y la salida están versionadas; los programas no leen nada fuera del repo.
- Unity importa las salidas como `Font` (los `.ttf`) y `TextAsset` (los `.json`); sus `.meta` los crea el Editor la primera vez que abre el proyecto.
