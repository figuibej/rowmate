# It. 4a — Deuda de la It. 3 (Architect)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-10-28 |
| Iteración | 4a · `04a-motor-hud-funcional` ([ROADMAP §It. 4a](../../ROADMAP.md)), al empezar y antes del diseño técnico |
| Entrada | [REPORT de la It. 3 §7-§9](../03-rutinas/REPORT.md); [05-qa-verification de la It. 3](../03-rutinas/05-qa-verification.md), rondas 1 a 3 (QA-V3-02 a -08, -10 y -12); [04-design-notes de la It. 3 §5 y §16.6](../03-rutinas/04-design-notes.md); [STATUS](../../STATUS.md): el usuario aceptó los valores por defecto del gate de la It. 3 (DV-20 incluida) |
| En paralelo | El Designer escribió [00-deuda-diseno](00-deuda-diseno.md), con el visto bueno de QA-V3-02 y dos ajustes (DD4a-05), que este trabajo aplica (§1.1). El PO escribe [01-po-spec](01-po-spec.md). Este documento no toca esos dos ni las biblias que actualizó el Designer |
| Legacy (solo lectura) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) en `3ae0f3a`. No hizo falta leer código del legacy: todo lo de esta deuda es de la app nueva, del arnés o de las herramientas. No se leyó `lib/core/strava/strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| Repo | Sin commits (los hace el coordinador). `ProjectSettings/ProjectSettings.asset` sin `ROWMATE_SIMULATOR` después de todas las corridas; sin `Assets/Resources`; los `FontAsset` generados y los assets de URP, sin cambios |

---

## 0. Resumen

**Los nueve puntos del Architect quedan resueltos**, más lo pendiente de CAP-27 y del CI. Todo está en verde: dotnet, las cuatro suites de Unity, el arnés `it3` e `it2` y los dos builds de Windows (§13). Las capturas se regeneraron **una sola vez**. Después, dos corridas seguidas de `it3` y de `it2`, una detrás de la otra, dieron **los mismos bytes que el repo** en las 47 + 43 tomas. Una corrida con `-Filter` también (§5).

Al resolver QA-V3-06 apareció **un defecto real de la app**: el ancho del chip `×N` dependía de lo que se había dibujado antes. Quedó corregido (§5.3). También quedó a la vista una diferencia con el legacy en el foco de los botones, que se deja para el diseño de la 4a (§11.1).

| ID | Estado | Qué cambió | Prueba / evidencia |
|---|---|---|---|
| QA-V3-02 | **Cerrado** (visto bueno del Designer con DD4a-05, aplicado) | Lo último borrado se desvanece y colapsa y **recién ahí** entra el vacío, en el editor y en Rutinas. El primer paso funde el vacío en su lugar. Las hojas de la serie crecen y funden. Los colores del tipo cruzan. La línea de error se abre y se cierra (§1) | PlayMode `RoutineMotionDebtTests` (10 de sus 14 pruebas, cuadro a cuadro). VID-01: el bloque funde y colapsa entre los 8,0 y los 8,3 s, y el vacío entra a los 300 ms (§1.6) |
| QA-V3-03 | Cerrado | DEMO-05 baja hasta las progresiones antes de escribir la de SPM, y la toma comprueba que se ven (§2) | Arnés: DEMO-05 con las tres progresiones a la vista y la vista previa fija |
| QA-V3-04 | Cerrado | `UiTestHost.Type` deja el cursor al final. `Tap` mueve el foco como un clic de verdad (§3) | PlayMode `TypeLeavesTheCaretAtTheEnd`, `TapMovesTheFocusLikeAClick` y `PointerDownFocusesAButton` (el fundamento medido). CAP-16 `500|`; DEMO-04 y DEMO-06 sin el nombre seleccionado |
| QA-V3-05 | Cerrado | Los tres motivos de `[Ignore]` dicen dónde se verifica cada cosa, y una prueba vigila todos los motivos (§4) | dotnet `ItThreeDebtTests.IgnoreReasonsNameWhereTheyAreVerified` |
| QA-V3-06 | Cerrado | El arnés precarga los atlas de las fuentes antes de la primera toma (`FontAtlasWarmup`). `TabularNumber` mide cada carácter con su avance real (§5) | Dos corridas seguidas (`it3` → `it2`, dos veces) iguales al repo byte a byte; `-Filter` igual a la corrida completa; registro `0 glifos antes`; dotnet `AtlasCharactersCoverEveryText`; PlayMode `TabularNumberMeasuresEveryCharacterItDraws` |
| QA-V3-07 | Cerrado | El comentario de `RmTextField` dice la regla del legacy (`deny('\n')`) y que quitar `\r` y `\t` es DV-20 (§7) | dotnet `SingleLineCommentTellsTheLegacyRuleAndTheDifference` |
| QA-V3-08 | Cerrado | Los builds de desarrollo van siempre a la misma carpeta. Con otra, `build.ps1` sale con 5 sin abrir Unity. `tools/unity/README.md` explica el aviso del Firewall y cómo evitarlo (§8) | dotnet `DevelopmentBuildsAlwaysUseTheSameFolder` y `DevelopmentBuildRefusesANewFolderForReal` (de verdad, con Windows PowerShell 5.1). En la PC: 12 reglas del Firewall antes y después de los builds de esta deuda, ningún aviso nuevo |
| QA-V3-10 | Cerrado | `Guardar`, `Eliminar` y `Reintentar`, y las acciones de Dispositivo y del pulsómetro, usan `Forget(logger, tag)`: ninguna excepción queda sin observar (§9) | dotnet `PresentationNeverDiscardsATask` (ningún `_ =` en Presentation) |
| QA-V3-12 | Cerrado (lo pendiente de CAP-27) | `capture-window.ps1` recorta al marco visible de la ventana. `capture-cap27.ps1` deja la foto en una carpeta temporal y nunca la escribe directo en el repo (§10) | dotnet `WindowCaptureUsesTheVisibleFrame` (de verdad, sobre la barra de tareas) y `Cap27PhotoNeverGoesStraightToTheRepo` (de verdad, sin tocar nada) |
| CI | Visto | `ci-core` del commit de cierre de la It. 3 terminó en verde en `ubuntu-latest` y en `windows-latest`, con `ITT.WindowCaptureStartsTheExeForReal` (§10.3) | API pública de GitHub Actions |
| Hallazgo | **Corregido** | `TabularNumber` medía `×` con 0,5 em si el glifo todavía no estaba en el atlas (§5.3) | CAP-10 de la It. 3 tenía el `×3` más ancho que CAP-11 |
| Observación | Para el diseño de la 4a | Con un clic, UI Toolkit le da el foco al botón; en Flutter de escritorio, no (§11.1) | PlayMode `PointerDownFocusesAButton` |

---

## 1. QA-V3-02 · Lo que se va, las hojas, los colores y la línea de error

Son los cuatro puntos de [04-design-notes de la It. 3 §16.6](../03-rutinas/04-design-notes.md), con la coreografía exacta de [00-deuda-diseno §2](00-deuda-diseno.md). Todo va por código y avanza con el reloj de `MotionDriver`, como el resto del motion: una transición de USS correría con el reloj real y la regla de toma la frenaría. Las animaciones nuevas son finitas y están registradas, así que `UiTestHost.Settle` y la regla de toma del arnés las esperan sin cambios.

### 1.1 Lo último que se va y el vacío (punto 1)

**Causa.** `RoutineEditorPage.Render` pasaba `#editor-steps` a `display: none` en el mismo cuadro en que arrancaba el `CollapseMotion`. El colapso corría, pero sobre algo que ya no se veía. Rutinas tenía el mismo defecto con la última tarjeta: `List.style.display = None` en el acto.

**Arreglo.**

- `EditorStepsView` y `RoutineListView` cuentan los colapsos en curso (`IsCollapsing`) y avisan con `Collapsed(overrunMs)` cuando termina el último.
- Con 0 pasos (o 0 rutinas) y algo colapsando, la lista **sigue a la vista** y el vacío queda pendiente. Cuando llega `Collapsed`, la lista pasa a `display: none` y el vacío entra (240 ms, y +8, `ease.enter`).
- `overrunMs` es lo que el colapso se pasó de su final en ese cuadro (`TimedMotion.OverrunMs`, nuevo). El vacío arranca en el instante exacto en que terminó el colapso y no en el cuadro siguiente: `EnterMotion` acepta una espera negativa. Así coincide con la tabla de 00-deuda-diseno §2.1: 0,231 de opacidad a los 300 ms.
- Con el **primer** paso, el vacío funde → 0 en 120 (`ease.exit`) en su lugar: sale del flujo con `position: absolute` en su misma caja (`ExitMotion`, nuevo), así que la fila nueva crece donde va, sin esperar. `MotionSpecs.EmptyOutMs` = 120.
- **DD4a-05 a** (Designer): si se agrega algo mientras el vacío entra, sale desde la opacidad y la `y` que tiene, sin pasar por 1. `ExitMotion.Play` recibe la opacidad y el desplazamiento actuales.
- **DD4a-05 b**: con reducir movimiento, el vacío entra en 160 sin subir. `EnterMotion` ya lo hacía: con reducir movimiento usa `dur.fast` sin desplazamiento.
- Interrupciones (00-deuda-diseno §2.3):
  - si se agrega algo mientras colapsa lo último, lo pendiente se cancela;
  - con dos borrados seguidos, el vacío espera al último colapso;
  - si se cambia de pestaña, el colapso termina con su tiempo y el vacío entra sin animar, porque la pestaña ya no está a la vista.

### 1.2 Las hojas de la serie (punto 2)

`SeriesBlockView.SetRepeat` cambiaba el `display` de las dos hojas en el acto. Ahora cada hoja tiene su `LayerMotion`: alto 0 ↔ 3 desde su borde de arriba (cuelga del pie) y opacidad 0 ↔ 1, en `StackLayerMs` (160) con `ease.move`.

- La primera vez, cuando el bloque nace, las hojas llegan con el bloque, como antes: la escalera de `GrowMotion` las funde.
- Con reducir movimiento: solo el fundido, sin cambio de alto. Es la decisión del Designer (00-deuda-diseno §2.5).
- `VisibleLayers` cuenta las hojas que quedan al terminar, así que las comprobaciones de las tomas y de las pruebas no cambian.

### 1.3 Los colores del tipo (punto 3)

`ColorFade` (nuevo) cruza colores en `dur.fast` (160) con `ease.move`, con la mezcla en sRGB de `TokenColors.Lerp`. Un extremo transparente toma el tono del otro, así que el cruce no pasa por el negro. Un cruce nuevo en medio de otro arranca desde el color que se ve. Lo usan:

- **`StepRowView`**, al cambiar de tipo: el borde izquierdo, la insignia, el ícono y el título. Lo usan la vista previa del diálogo y la fila del editor después del lápiz. Lo que pone el USS por clase (el borde, el título y el ícono) se anima en línea y al terminar se suelta: la clase nueva ya da el color final, así que no salta. La insignia es del código y queda en su color.
- **`SegmentedControl`**, con un `MotionDriver`. En los mosaicos, el elegido cruza fondo, borde y etiqueta, y el anterior hace lo mismo al revés. En el selector de modo, el segmento cruza su fondo (04-design-notes §5.7).

Con reducir movimiento cruza igual, porque es un fundido (§5.10). Sin armar o fuera del panel, el color cambia en el acto: la primera vez, el color llega con la fila.

### 1.4 La línea de error (punto 4)

`ErrorLine` (nuevo, `Presentation/Controls`) envuelve el `Label` de contrato (`#<campo>-error`, `.rm-field__error`) en un slot sin estilo, `.rm-field__error-slot`, con `flex-shrink: 0`. `RevealMotion` (nuevo) lo abre y lo cierra como `GrowMotion` y `CollapseMotion`:

- mientras se mueve, el slot recorta y su alto va de 0 al del texto **más** su margen de 4;
- el texto va en `position: absolute` con su alto natural, así que no se aplasta;
- la opacidad va de 0 a 1;
- todo en 160 con `ease.move`.

Para abrir, espera el layout del cuadro siguiente, que da el alto (`ILayoutBoundMotion`, como `GrowMotion`). Al corregir el campo, se cierra al revés con el texto viejo a la vista, y recién después queda en `display: none`. Lo usan `RmTextField` y las dos líneas de los pares del diálogo de paso (`#step-time-error`, `#step-split-error`).

- El **error lógico** (`ErrorText`, `HasError`) y la clase `--error` de la caja cambian **en el acto**, como pide §5.2. Las pruebas y las comprobaciones que leen `ErrorText` no cambian.
- Con reducir movimiento: en el acto.
- La línea del par Min/Seg se oculta con su fila al pasar a "Por distancia", también en el acto (`ErrorLine.SetVisible`, que termina lo que esté en curso).

### 1.5 Pruebas (PlayMode, `Tests/PlayMode/RoutineMotionDebtTests.cs`)

Todas con el reloj de prueba, cuadro a cuadro. Con el código de antes fallan:

- `LastBlockCollapsesBeforeTheEmptyText`, `LastLooseStepCollapsesBeforeTheEmptyText` y `LastRoutineCollapsesBeforeTheEmptyState` comprueban:
  - que mientras lo borrado se ve, la lista sigue en `flex` y el vacío en `none`;
  - que el alto del slot solo baja, sin ningún paso de más del 35 % (el más grande de la curva es ≈ 25 %);
  - que el colapso dura más de 10 cuadros;
  - que después el vacío entra fundiendo y queda sin nada en línea.
- `FirstStepFadesTheEmptyTextOutInPlace`: el vacío sale del flujo en su lugar (± 0,5 px), funde y a los 120 ms ya no está.
- `EmptyTextLeavesFromWhereItWas` (DD4a-05 a): el vacío que entraba nunca vuelve a 1.
- `SeriesSheetsGrowAndFade` (×3 → ×1 → ×2): a los 60 ms, opacidad y alto a mitad; en reposo, 3 dp del USS y nada en línea.
- `TypeColorsCrossFade` y `TypeColorsCrossFadeAlsoWithReducedMotion`: el borde de la vista previa y el fondo del mosaico a mitad del cruce, y en reposo los colores de los tokens, con lo del USS suelto.
- `ErrorLineOpensAndClosesInPlace` y `ErrorLineIsInstantWithReducedMotion`.

### 1.6 VID-01, cuadro a cuadro

Cuadros extraídos del MP4 regenerado, con PyAV en `<scratch>/it4a/vid/`. Cada cuadro del video se toma después de avanzar 1/30 s. El tacho está a los 8,000 s, así que el cuadro 240 muestra +33 ms:

| Cuadro | Tiempo desde el toque | Qué se ve |
|---|---|---|
| 237 | antes | La serie ×3 entera, `Pasos (2)` |
| 240-241 | +33 / +67 ms | `Pasos (0)` en el acto; el bloque casi opaco (la curva da 0,97 y 0,83) |
| 242 | +100 ms | El bloque tenue (la curva da 0,42), con su alto entero |
| 243-247 | +133 a +267 ms | El bloque ya no se ve; su slot colapsa |
| 248 | +300 ms | El vacío asoma, tenue y un poco más abajo (la curva da 0,23 y +6 dp) |
| 258 | +633 ms | El vacío entero, en su lugar |

Comparado con el VID-01 de `HEAD`, los cuadros cambian **solo entre el 240 y el 252**. En VID-02 y VID-03 ningún cuadro tiene más de 50 píxeles con más de 24 de diferencia: son los bordes de los glifos (§5) y la compresión.

---

## 2. QA-V3-03 · DEMO-05 con las progresiones

En `RoutinesCatalog.DemoScript`, después de escribir los minutos y el SPM objetivo, la secuencia baja hasta el final del contenido del diálogo, como haría el usuario, y recién ahí escribe el incremento de SPM. En W1280 la vista previa queda fija arriba (04-design-notes §7.3). La toma suma dos comprobaciones: "vista previa fija" y "las tres progresiones a la vista", con los tres campos enteros dentro de la zona desplazable. La descripción del INDEX dice "y, desplazado al final, Incremento SPM 2".

---

## 3. QA-V3-04 · `Type` y `Tap` del arnés

- **`UiTestHost.Type`** asigna el valor y después deja el cursor al final (`Select(largo, largo)`), como al teclear. Si el campo filtró algo, el cursor queda al final de lo que quedó. Ejemplo: `A\nB` en una línea da `AB|`.
- **`Tap`** le da el foco a lo que toca si es enfocable, antes del `NavigationSubmitEvent`, como el `FocusController` de UI Toolkit al presionar. Sin teclado de por medio no prende el anillo (`.rm-keyboard-nav`), y `FocusScroller` solo desplaza con el teclado, así que nada se mueve. Lo que **no** es enfocable no cambia el foco: los ítems del menú de texto, por ejemplo, para que no le saquen el foco al campo.
- **Medido, no supuesto**: `PointerDownFocusesAButton` manda un `PointerDownEvent` de verdad a `+ Paso` con el nombre enfocado, y el foco pasa al botón. Si eso dejara de pasar en otra versión de Unity, `Tap` tendría que dejar de mover el foco.

Consecuencias en las tomas, todas como en la app real:

- CAP-16 `500|`, DEMO-02 `Escalera SPM|`, DEMO-03 `5|` y DEMO-05 `2|`.
- En DEMO-04, DEMO-06 y DEMO-08 el nombre ya no sale seleccionado ni con foco.
- En CAP-18, CAP-19 y CAP-28 el campo del nombre ya no tiene foco: el clic en `Guardar` se lo lleva.

Una prueba de la It. 3 dependía del comportamiento viejo: `RoutineEditorTests.EnterDuringTheExitDoesNothing`. Abría el editor con un `Tap` en `+`, que antes no se llevaba el foco. Ahora `+` tiene el foco, `ExitLock` se lo devuelve al salir y el Enter le llega a él, que abre un editor **nuevo y vacío**. La prueba sigue comprobando lo que pide: Enter durante la salida no guarda otra vez y no le llega a la página que sale. Dejó de exigir que no haya ningún editor arriba. La diferencia con el legacy queda para la 4a (§11.1).

---

## 4. QA-V3-05 · Los motivos de `[Ignore]`

| Prueba | Motivo nuevo |
|---|---|
| `StepLabelsTests.EditorSubtitleOnScreen` | Se verifica en PlayMode: `RoutineEditorTests.BorderSubtitlesOnScreen` (las 11 filas de AC-RUT-12-06) y en CAP-12a y CAP-12b |
| `StepTypeTests.IconsInEditor` | Se verifica en PlayMode: `RoutineEditorTests.StepRowIcons` (descanso sin relleno, P-07) y en CAP-10 y CAP-11 |
| `LanguageStateTests.SystemTextsFollowSystemLanguage` | Se verifica en PlayMode: `TextEditingTests.ContextMenuInTheAppLanguage` (P-06) y en CAP-26. [Device-And]: el teclado del sistema, cuando estén el módulo y el teléfono (AC-RUT-11-10) |

`ItThreeDebtTests.IgnoreReasonsNameWhereTheyAreVerified` revisa todos los `[Ignore]` de `Tests/`. Falla si un motivo manda a una iteración cerrada ("→ It. 1", "→ It. 2" o "→ It. 3") o si nombra una prueba (`Clase.Método`) que no existe. Hoy revisa los 17 motivos y todas las pruebas que nombran existen. Sigue "→ It. 4a: el diálogo de inicio de rutina" (`RoutineSummaryTests`), que es de esta iteración.

---

## 5. QA-V3-06 · Capturas deterministas

### 5.1 Causa

Los 9 `FontAsset` de la UI son dinámicos y se versionan vacíos (QA-V2-07). TextCore agrega cada glifo al atlas la primera vez que se dibuja, en el primer lugar libre. El lugar depende del orden, y con él el muestreo del SDF en el borde (± 1-2 por canal, 04-design-notes de la It. 3 §16.5). Ese orden cambia con:

- lo que dibujaron las tomas anteriores en el mismo proceso;
- una corrida con `-Filter`;
- el orden en que UI Toolkit genera el texto de un cuadro, que lo hace en paralelo.

La diferencia de la ronda 1 de QA (13 PNG de `it2` con ± 2 en bordes de cifras) es de esta familia. **Los atlas no pasan de un proceso del Editor al siguiente**: el registro nuevo lo muestra, con `0 glifos antes` en cada corrida. Lo que cambiaba era el orden dentro de cada proceso.

### 5.2 Arreglo: los atlas, precargados antes de la primera toma

- `Snapshots/FontAtlasWarmup` corre en `SnapshotRunner.Start`, antes de registrar la máquina de estados.
- Recorre las fuentes en el orden fijo de `FontAssetPaths.All` y llama a `TryAddCharacters(…, includeFontFeatures: false)`. Sin las características de la fuente, el kerning es el de la tabla versionada (DN3-24) y no se toca.
- Los caracteres salen de `Core/Design/AtlasCharacters`, una clase pura que se prueba en dotnet:
  - en las 7 caras de texto, ASCII imprimible, Latin-1 imprimible y la puntuación de los textos (– — ‘ ’ “ ” • … ← → − ≈ ≤ ≥);
  - en las 2 de íconos, los glifos de `Icons.All`;
  - Noto Emoji no: ninguna toma dibuja emojis.
- `GeneratedAssetGuard` sigue frenando que se guarden: el registro dice `[ASSETS] no se guarda …` por cada archivo y `git status` no los muestra.
- El registro de cada corrida empieza con `[SNAPSHOT] atlas de fuentes precargados (QA-V3-06): 9 FontAsset, 0 glifos antes, 1476 después (6 caracteres sin glifo en su fuente)`. Los 6 son caracteres del juego que alguna de las 9 fuentes no trae. Si un texto los usara en esa fuente, TextCore buscaría el respaldo, como antes de la precarga.
- `ItThreeDebtTests.AtlasCharactersCoverEveryText` falla si un texto de `strings.es.json` o `strings.en.json`, lo que escribe una toma (`Type`) o el nombre o la descripción de una semilla usan un carácter que no se precarga.

### 5.3 Hallazgo: el ancho del chip `×N` dependía de lo dibujado antes

Al comparar la regeneración con `HEAD`, CAP-10 cambió más que en los bordes. En la It. 3, el `×3` de CAP-10 tenía la `×` más separada que el de CAP-11, la misma rutina a lo ancho.

`TabularNumber.UpdateMetrics` precargaba `0123456789:.-m`, pero no la `×`. `Advance('×')` sin el glifo en el atlas cae a 0,5 em. En CAP-10 era la primera `×` del proceso, así que se medía con 0,5 em. En CAP-11 ya estaba dibujada y se medía con su avance real.

En la app pasaba lo mismo con el primer chip de la sesión. Su ancho dependía de si otro texto ya había dibujado una `×`, y se corregía solo la próxima vez que se medía.

**Arreglo (app):** `TabularNumber.EnsureGlyphs`, antes de medir, agrega al atlas los caracteres del texto que no son cifras ni `:`/`.` y que todavía no están. Pasa una sola vez por carácter en la sesión y después no asigna nada. La tabla de cadenas de un carácter pasa a 256 entradas (Latin-1): la `×` ya no asigna en cada dibujo.

`TabularNumberMeasuresEveryCharacterItDraws` (PlayMode) lo prueba con un carácter que ningún otro texto de las pruebas dibuja: la celda mide su avance real y no 0,5 em. Es lo que pide DN-08 ("el resto, con su avance").

### 5.4 Resultado

| Corrida | Resultado |
|---|---|
| Regeneración `it3` (en el repo) | 47 tomas, 0 con problemas, 0 sondas de aspecto que fallan |
| Regeneración `it2` (en el repo), justo después | 41 tomas, 0 con problemas, 0 sondas que fallan |
| A: `it3` y después `it2`, con `-Out` en `<scratch>` | `it3`: 44 PNG, 3 MP4 e `INDEX.md` iguales byte a byte al repo. `it2`: 41 tomas, `INDEX.md` y `rt1-measurement.json` iguales. En `manifest.json` solo cambia `durationMs` |
| B: lo mismo otra vez | Igual que A, y A = B en todos los archivos |
| `-Filter "CAP-04,CAP-10,CAP-17,CAP-26"` | Las 4 tomas, iguales a la corrida completa. Antes de la precarga, CAP-10 difería en 5 píxeles (04-design-notes §16.5) |
| `-Filter "CAP-17"` justo después de una PlayMode con simulador | Igual a A |

**Lo que queda (menor, para QA).** En la regeneración, CAP-17 salió con 3 píxeles distintos (de hasta 75) en la fila de arriba del cursor del campo `Cantidad`. Las otras cuatro corridas (A, B y los dos `-Filter`) dieron los mismos bytes entre sí. Se versiona esa versión, la de las cuatro: es la misma toma, del mismo código.

Es un borde del cursor, no un glifo: la precarga no lo toca. La hipótesis es que el cursor queda en una posición que se calcula en un layout cuyo cuadro depende del bucle real del Editor, durante la entrada escalada del diálogo. Para verlo hay que repetir: si vuelve a aparecer, la regla para esa toma pasaría a tener tolerancia de ± 1 fila en el cursor.

La regla para versionar, ahora en `tools/unity/README.md`: **dos corridas seguidas tienen que dar los mismos bytes**, y una con `-Filter` da los mismos que la completa.

---

## 6. Capturas regeneradas: qué cambió y por qué

Se regeneraron una vez los dos conjuntos. Contra `HEAD`, píxel por píxel, con un programa propio sobre `System.Drawing` en `<scratch>/it4a/tools/`:

- **`it2` (41 tomas)**: CAP-20a, CAP-20c, CAP-20d y CAP-20e, iguales. En las otras, como mucho 2 por canal y **ningún** píxel con más de 3. Son bordes de glifos, por la precarga de §5.2. `INDEX.md` y `rt1-measurement.json` no cambian. En los 3 videos cambia la compresión de esos bordes.
- **`it3` (47 tomas)**: CAP-06, CAP-11, CAP-22, CAP-23 y CAP-26, iguales. En las demás, ± 1 en bordes de glifos, salvo lo que cambió a propósito:

| Toma | Qué cambió | Por qué |
|---|---|---|
| CAP-10 | El `×3` con su avance real (más angosto) | §5.3 |
| CAP-16, DEMO-02, DEMO-03, DEMO-05 | El cursor al final (`500|`, `Escalera SPM|`, `5|`, `2|`) | QA-V3-04 |
| CAP-18, CAP-19, CAP-28 | El nombre sin foco: el clic en `Guardar` se lo lleva | QA-V3-04, como en la app |
| DEMO-04, DEMO-06, DEMO-08 | El nombre sin seleccionar y sin foco (en DEMO-08, tampoco la descripción) | QA-V3-04 |
| DEMO-05 | Desplazado al final, con las progresiones | QA-V3-03 |
| CAP-20a | El cursor al final del `10000`, y `Confirmar` 1 px más abajo, por el redondeo del slot nuevo de la línea de error | QA-V3-04 y §1.4 |
| VID-01 | Del cuadro 240 al 252: lo último que se va y el vacío | QA-V3-02 (§1.6) |

---

## 7. QA-V3-07 · El comentario de `RmTextField`

Decía "singleLineFormatter del legacy". Ahora dice:

- que el legacy solo quita `\n` (`FilteringTextInputFormatter.singleLineFormatter = deny('\n')`, `<flutter-sdk>/packages/flutter/lib/src/services/text_formatter.dart:425`);
- que quitar también `\r` y `\t` es **DV-20** (01-po-spec de la It. 3 §11.1);
- que en la descripción solo se quita `\t`, porque Tab nunca lo escribe y tampoco entra pegado (QA-G3-10).

El código no cambió.

---

## 8. QA-V3-08 · Los builds de desarrollo y el Firewall de Windows

### 8.1 Medido en la PC

- Abrí el `RowMate.exe` de desarrollo de `Builds/Windows/simulator` con `-batchmode -nographics` y `ROWMATE_DATA_DIR` en `<scratch>`, y miré sus sockets. Escucha en **TCP 55000 sobre `0.0.0.0`**: es el *PlayerConnection* de Unity, el del Profiler. Tiene además un UDP efímero para anunciarse. Un `listen` en todas las interfaces es lo que dispara el aviso del Firewall la primera vez para cada ruta.
- Probé `player-connection-mode=Connect` con `player-connection-ip=127.0.0.1` en el `boot.config` de ese mismo build, y restauré el archivo verificado con SHA-256. **Igual escucha** en 55000: el registro dice `Direct connection to host time out.` y anuncia igual. No hay un ajuste del `boot.config` que lo apague. La salida es la carpeta fija.
- Las reglas de entrada de `rowmate.exe` son 12: dos por cada una de las 6 carpetas donde se abrió alguna vez un build de desarrollo. Están `Builds/Windows/simulator`, `Builds/Windows/dev-smoke` y `Builds/Windows/simulator-it3`, y tres carpetas de QA en el scratchpad. Hoy todas son de permiso, y no hay ningún aviso del Firewall en pantalla. Con los dos builds y el humo de esta deuda siguen siendo 12, y no apareció ningún aviso.
- Los dos `RowMate.exe` que abrí los cerré yo. No quedó ningún proceso.

### 8.2 Arreglo

- **`tools/build/build.ps1`**. Un build de desarrollo es todo `-Simulator`, y los normales con `-SmokeTest` o `-DbSmoke`.
  - Va siempre a `Builds/Windows/simulator`, para el simulador, o a `Builds/Windows/dev-smoke`, para el normal de desarrollo, que antes pisaba al de release en `normal`.
  - Con otro `-Out` sale con **5** antes de buscar Unity: no abre nada ni crea la carpeta.
  - `-AllowNewFolder` fuerza otra carpeta, sabiendo que esa ruta va a preguntar una vez.
  - El build de release (`Builds/Windows/normal`) no abre el puerto, así que su `-Out` es libre.
- **`BuildRequest.DefaultOutput`**: cuando `BuildCli` se llama sin `-rowmateOutput`, usa las mismas carpetas.
- **`tools/unity/README.md`**, sección "It. 4a: builds de desarrollo y el Firewall de Windows", explica:
  - qué abre el puerto y por qué Windows pregunta una vez por ruta;
  - las carpetas fijas;
  - que los players se abren desde ahí y nunca desde una copia;
  - que para mostrarle la app al usuario se le señala `Builds/Windows/simulator/RowMate.exe`, o una copia siempre en la **misma** carpeta, sin el número de iteración;
  - que si el aviso aparece igual se contesta **`Cancelar`**, porque la app no usa la red;
  - que para perfilar se usa ese mismo build.

### 8.3 Pruebas

- `DevelopmentBuildsAlwaysUseTheSameFolder` es estática: revisa la regla, que la negativa va antes de `Resolve-Unity`, las carpetas de `BuildRequest` y lo que explica el README.
- `DevelopmentBuildRefusesANewFolderForReal` corre `build.ps1` de verdad con Windows PowerShell 5.1 y un `Unity.exe` que no existe:
  - `-Simulator` y `-DbSmoke` con otra carpeta salen con 5, con su mensaje, y la carpeta no se crea;
  - con `-AllowNewFolder`, con la carpeta fija, sin `-Out` y en el build de release pasan la negativa y salen con 12 (no encuentra Unity);
  - nunca abre Unity.

---

## 9. QA-V3-10 · Ninguna tarea sin observar

`Guardar` (`RoutineEditorPage`), `Eliminar` y `Reintentar` (`RoutinesScreenView`) lanzan su tarea con `.Forget(_ui.Logger, RoutinesStore.Tag)` en lugar de `_ =`. Una excepción que no venga de la base, que `RoutinesStore` ya registra como `Warning`, queda en el registro como `Error` con el tag `DB`. `RoutineEditorStore.SaveAsync` la relanza a propósito para que el editor no quede trabado. Lo mismo para las acciones de Dispositivo (`Desconectar` y `Buscar`, con el tag `BLE`, nuevo como `AppContext.BleTag`) y de la tarjeta del pulsómetro (tag `HRM`).

`PresentationNeverDiscardsATask` falla si aparece un `_ =` en `Presentation` o `SimulatorPresentation`, sin contar comentarios ni literales. El comportamiento de `Forget` ya lo probaba `TestMainThreadContextTests.ForgetLogsFaultsThroughTheLogger`.

---

## 10. QA-V3-12 y lo pendiente de CAP-27

### 10.1 El marco de la foto

`capture-window.ps1` captura el marco **visible** (`DwmGetWindowAttribute` con `DWMWA_EXTENDED_FRAME_BOUNDS` = 9, en `Get-VisibleFrame`). `GetWindowRect` queda solo de respaldo, si DWM no responde: en Windows 10 y 11 suma bordes invisibles de unos 7-8 px y traía franjas del escritorio de atrás. Las 9 comprobaciones de "nadie la tapa" usan el mismo marco. El registro dice de dónde salió (`marco dwm`).

`WindowCaptureUsesTheVisibleFrame` corre de verdad el `Add-Type` y `Get-VisibleFrame` del script, sacados del propio archivo, con Windows PowerShell 5.1 y sobre la barra de tareas. Es una ventana que ya existe: no abre nada, no captura ni toca el mouse. Comprueba que el marco sale de DWM, que tiene ancho y alto y que queda dentro de `GetWindowRect`. Sin barra de tareas, como en un runner sin escritorio, queda ignorada con el motivo.

### 10.2 La foto nunca va directo al repo

- En `capture-cap27.ps1`, `-Out` queda vacío por defecto y la foto va a `<temp>/RowMate.Cap27-foto/`.
- Un `-Out` dentro del repo sale con **5** antes de todo: antes del tema, de Unity, de la semilla y de cualquier copia.
- Al terminar, el script dice que hay que mirarla antes de pasarla, porque el repo es público, y da el `Copy-Item` a `03-rutinas/snapshots/`.

`Cap27PhotoNeverGoesStraightToTheRepo` lo prueba de verdad con `-Rehearsal` y un `Unity.exe` que no existe. Así, aunque la negativa faltara, no se toca nada del usuario y sin Unity se sale con 12. Con `-Out` en `docs/` da 5 y no deja archivo; sin `-Out`, pasa la negativa y da 12.

CAP-27 sigue siendo solo a pedido (REPORT de la It. 3 §8.3). Con esto ya no hay nada de la herramienta que frene la toma.

### 10.3 El CI

El *push* del cierre de la It. 3 (`dcdfe13`) corrió `ci-core`, que terminó en verde en sus dos jobs, `Core (ubuntu-latest)` y `Core (windows-latest)`. Lo leí en la API pública de GitHub Actions del repositorio. En Windows corre de verdad `ITT.WindowCaptureStartsTheExeForReal`, y en Ubuntu queda ignorada con su motivo. `ci-unity` sigue esperando la licencia (P-08).

---

## 11. Observaciones y pedidos para el diseño de la 4a

### 11.1 El foco de los botones con el clic (nueva, menor)

UI Toolkit le da el foco a un botón enfocable cuando se presiona: lo midió `PointerDownFocusesAButton`. Flutter de escritorio, no. En el legacy, un clic en un botón no lo enfoca, y un clic afuera le saca el foco al campo de texto (QA de la It. 3, R2.3).

Consecuencia en la app: después de abrir el editor con un clic en `+` y guardarlo, el foco vuelve al `+` (`ExitLock`), y un Enter abre otro editor. En el legacy no pasa nada. Lo mismo pasa con cualquier botón: Enter o Espacio después de un clic lo vuelven a activar. No falla ningún AC, y para quien usa teclado es lo correcto: el foco vuelve a quien abrió.

**Propuesta para el diseño de la 4a** (decide el PO, con el Designer), con dos caminos:

- **(a)** que `ExitLock` devuelva el foco solo si se estaba navegando con teclado (`.rm-keyboard-nav`), como Flutter, donde el clic no enfocó nada;
- **(b)** que los botones no tomen el foco con el puntero.

Es relevante para el HUD: la 4a suma `Pausa` y `Terminar`, y un Enter que reactive `Terminar` sería peor.

### 11.2 Pedidos del Designer para el diseño de la 4a

En [00-deuda-diseno](00-deuda-diseno.md) hay pedidos que no son de esta deuda: cambian la regla de la llave, que decide el PO, y la base de las capturas.

- **DD4a-01 a DD4a-04**: la regla pura de la llave (QA-V3-09), los menús que la esquivan, el panel anclado y la auditoría del arnés.
- **DD4a-06**: las tablas de kerning con K2.
- **DD4a-07**: RowMate Numeric SC.

Van al 02-tech-design de la 4a. DD4a-05 ya está aplicado (§1.1). Una nota para DD4a-06 b: la precarga del arnés usa `includeFontFeatures: false` a propósito y no interfiere con una tabla generada en el bootstrapper.

### 11.3 Lo que sigue de la It. 3 para la 4a (sin cambios)

R3-08, comprobar los topes antes de expandir una rutina, y reusar la pila, los diálogos y los avisos para `¿Iniciar entrenamiento?`. Las dos van en el diseño de la 4a (REPORT de la It. 3 §9.2).

---

## 12. Corridas (evidencia)

Carpetas propias en `<scratch>/it4a/`. Una sola instancia de Unity por vez, en serie. Al empezar no había ningún Editor abierto, solo el `unity.exe` de la Hub, que no se tocó.

| Corrida | Resultado | Antes (ronda 3 de QA de la It. 3) |
|---|---|---|
| dotnet (`run-tests.ps1 -Matrix -UnityResults <scratch>/it4a/unity-final -ArtifactsPath <scratch>/it4a/debt`) | `Build succeeded. 0 Warning(s) 0 Error(s)`. Simulator **151/151**. EditMode **1457: 1440 pasan, 0 fallan, 17 ignoradas**. exit 0. Matriz: **PASA 377, FALLA 0, IGNORADO 0, PENDIENTE UNITY 0, SIN TEST 0** | 1599 / 1582 / 0 / 17 → **1608 / 1591 / 0 / 17** (+9: `ItThreeDebtTests`) |
| Unity EditMode | **1488 / 1472 / 0 / 16**, exit 0 | 1479 / 1463 / 0 / 16 (+9) |
| Unity EditMode + simulador | **1639 / 1623 / 0 / 16**, 0 inconclusos. Enable → 0, Restore → 0 | 1630 / 1614 / 0 / 16 (+9) |
| Unity PlayMode | **202 / 202**, exit 0 (1 min 48 s) | 188 / 188 (+14: las de `RoutineMotionDebtTests`) |
| Unity PlayMode + simulador | **249 / 249**, exit 0 (2 min 19 s) | 235 / 235 (+14) |
| Rendimiento (QA-G3-21) | `[PRF] 54 rutinas a la vista en 182 ms` (180 con simulador); en el hilo principal, lista de 54 en 3,6 ms (3,7), editor de 20 pasos en 0,8 ms (0,7) y `Tick` durante el guardado en 0,00 ms | 197 ms; 6,6 / 1,2 / 0,00 ms |
| Arnés `it3` e `it2` | La regeneración y las corridas A, B y `-Filter` de §5.4. Todas con exit 0, 0 problemas y 0 sondas que fallan | — |
| `build.ps1 -Target Windows -Simulator -DbSmoke` | `[BUILD] Windows simulator Mono2x Development → …/Builds/Windows/simulator/RowMate.exe`, `PerformanceTestRunCleaner: 4 entradas`, `Result: Success`, `[DB] humo: ok` (27 s) | — |
| `build.ps1 -Target Windows` | `Builds/Windows/normal`, `Success`, `BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests.` (29 s) | — |
| Repo | `ProjectSettings.asset` sin `ROWMATE_SIMULATOR`. Sin `Assets/Resources`. `UI/Fonts/Generated`, `UI/Icons/Generated` y `Settings/` sin cambios. Lo nuevo y lo regenerado no tiene rutas de la PC, nombres de usuario ni direcciones de red | — |
| Legacy | `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar | — |

La regla de una sola instancia se cumplió. Las PlayMode con simulador y el arnés activan y restauran el define en su propia secuencia, y cada uno termina con el define restaurado y verificado.

---

## 13. Archivos

| Archivo | Qué |
|---|---|
| `Assets/RowMate/Presentation/Motion/ColorFade.cs` (nuevo) | Cruce de colores (§1.3) |
| `Assets/RowMate/Presentation/Motion/RevealMotion.cs` (nuevo) | Abrir y cerrar en su lugar (§1.4) |
| `Assets/RowMate/Presentation/Motion/ExitMotion.cs` (nuevo) | Fundirse en su lugar, fuera del flujo, desde lo que se ve (§1.1) |
| `Assets/RowMate/Presentation/Controls/ErrorLine.cs` (nuevo) | La línea de error con su slot (§1.4) |
| `Assets/RowMate/Presentation/Motion/TimedMotion.cs`, `GrowMotion.cs` (`EnterMotion`) | `OverrunMs`; espera negativa (§1.1) |
| `Assets/RowMate/Presentation/Routines/RoutineEditorPage.cs`, `EditorStepsView.cs`, `RoutineListView.cs`, `RoutinesScreenView.cs` | Lo último que se va y el vacío (§1.1); `Forget` (§9) |
| `Assets/RowMate/Presentation/Routines/SeriesBlockView.cs` | Las hojas (§1.2) |
| `Assets/RowMate/Presentation/Routines/StepRowView.cs`, `Controls/SegmentedControl.cs`, `Routines/StepDialog.cs` | Colores del tipo y del modo (§1.3); las líneas de los pares (§1.4) |
| `Assets/RowMate/Presentation/Controls/RmTextField.cs` | `ErrorLine` (§1.4); el comentario (§7) |
| `Assets/RowMate/Presentation/Controls/TabularNumber.cs` | `EnsureGlyphs` y cadenas Latin-1 (§5.3) |
| `Assets/RowMate/Presentation/Device/DeviceScreenView.cs`, `HeartRateCardView.cs`, `App/Composition/AppContext.cs` | `Forget` con `BLE` y `HRM`; `AppContext.BleTag` (§9) |
| `Assets/RowMate/Presentation/Styles/Base.uss` | `.rm-field__error-slot` (estructura, sin aspecto) |
| `Assets/RowMate/Core/Design/MotionSpecs.cs`, `Core/Design/AtlasCharacters.cs` (nuevo) | `EmptyOutMs`; los caracteres de la precarga (§5.2) |
| `Assets/RowMate/Snapshots/FontAtlasWarmup.cs` (nuevo), `SnapshotRunner.cs`, `RoutinesCatalog.cs` | La precarga (§5.2); DEMO-05 (§2) |
| `Assets/RowMate/Tests/UiSupport/UiTestHost.cs` | `Type` y `Tap` (§3) |
| `Assets/RowMate/Editor/Build/BuildRequest.cs`, `tools/build/build.ps1` | Las carpetas fijas (§8) |
| `tools/unity/capture-window.ps1`, `tools/unity/capture-cap27.ps1` | El marco visible y la foto fuera del repo (§10) |
| `tools/unity/README.md` | El Firewall (§8), el determinismo (§5) y la foto de CAP-27 (§10) |
| `Assets/RowMate/Tests/EditMode/Project/ItThreeDebtTests.cs` (nuevo, 9 pruebas, dotnet y Unity) | QA-V3-05, -06, -07, -08, -10 y -12 |
| `Assets/RowMate/Tests/PlayMode/RoutineMotionDebtTests.cs` (nuevo, 14 pruebas) | QA-V3-02, -04 y -06 |
| `Assets/RowMate/Tests/PlayMode/RoutineEditorTests.cs`, `EditMode/Design/MotionSpecsTests.cs`, `EditMode/Models/StepLabelsTests.cs`, `StepTypeTests.cs`, `EditMode/Localization/LanguageStateTests.cs` | §3, `EmptyOutMs` y los motivos de §4 |
| `docs/migration/iterations/03-rutinas/snapshots/*` y `02-dispositivo-simulador/snapshots/*` | Regenerados una vez (§6) |

Los `.meta` de los 9 archivos nuevos los creó Unity. Van con su archivo.
