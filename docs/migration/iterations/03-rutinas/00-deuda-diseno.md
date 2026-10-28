# It. 3 — Deuda de diseño de la It. 2

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-10-27 |
| Iteración | 3 · `03-rutinas` ([ROADMAP §It. 3](../../ROADMAP.md)), antes de la spec del PO |
| Entrada | [04-design-notes de la It. 2 §11 y §15.6](../02-dispositivo-simulador/04-design-notes.md) (y §2-§5, §15.3 como fuente), [05-qa-verification §6](../02-dispositivo-simulador/05-qa-verification.md) (QA-V2-12, QA-V2-13), [REPORT §7.2 y §9.2](../02-dispositivo-simulador/REPORT.md), `snapshots/rt1-measurement.json` de la It. 2, `Assets/RowMate/Core/Design/Tokens.cs` (manda), `Core/Design/MotionSpecs.cs`, `Core/Design/Contrast.cs` |
| Entrega | Este documento; ART_BIBLE y ANIMATION_BIBLE alineadas (secciones "Cambios al iniciar la It. 3"); 8 tokens del HUD en `Tokens.cs` y `Tokens.uss` regenerado; un test nuevo y tres tests con valores fijos actualizados (§2.7) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `lib/features/workout/immersive_workout_screen.dart:351-353`, `:760-800` y `:915-940` (tarjeta del HUD y botones de pausa: en pausa el legacy no baja la opacidad de las tarjetas). No se leyó `strava_secrets.dart`. `git -C ../rowmate-legacy status --porcelain` vacío antes y después |
| Verificado | Contrastes y derivación OKLCH con un programa propio en `<scratch>` (mismo método que `Contrast.cs`); `tools/tokens --check` con 0 (127 tokens); `tools/dotnet/run-tests.ps1` en verde. **Unity no se abrió** (lo estaba usando el Architect): nada de esta entrega necesita Unity para verificarse (§2.7) |

Convenciones: las de las 04-design-notes (hex `#RRGGBB`, transparencia `@NN %`, contraste WCAG 2.x con mezcla sRGB sin redondear). Peor vidrio: `glass.fallback` (`#070D16 @86 %`) sobre blanco. **W0** = ese compuesto sin redondear, `(41,72; 46,88; 54,62)`; **W1** = el mismo en 8 bits, `#2A2F37` (lo que se ve y lo que midió R-T1); **W2** = W1 + 1 por canal, `#2B3038`.

---

## 0. Resumen

1. **Biblias alineadas** con lo que se decidió e implementó en la It. 2 (§1): tokens nuevos (`bodyM`, `bodyXs`, `button`, `accent.tonalBg`, `accent.onTonal`), tablero con acento al 60 % y brillo de 14 %, botones (tonal AA, `button` 14/600, una línea con "…", *hover* 4 %, presionado `:hover:active`), barra de vidrio con brillo, cinta 10/700, reglas de UI Toolkit (`letter-spacing` en 1/100 em, cajas de línea, centrado óptico), y en motion el rodillo por celda, el latido doble con compresión, las coreografías de Dispositivo y la regla "código o USS".
2. **QA-V2-12, causa:** las variantes del HUD se habían ajustado al umbral justo **contra el cálculo sin redondear** (W0). En pantalla el vidrio se cuantiza hacia arriba (W1) y el contraste baja 0,24 %: el pulso da **4,497** (QA midió "4,50") y el Split **6,987**, los dos por debajo de su umbral en el píxel real. El Split no lo marcó nadie, pero está en el mismo `rt1-measurement.json` (`6.99`).
3. **QA-V2-12, ajuste (hecho):** criterio nuevo, el umbral se cumple contra **W2** (`#2B3038`, ≈ 1,7 % de margen). Cambian **8 variantes del HUD** con el mismo matiz (§2.3): `metric.hr.hud` y `step.work.hud` `#FF5A7E` → **`#FF5D7F`**, `metric.split.hud` → `#FFAA47`, `metric.spm.hud` → `#3ACCF1`, `metric.distance.hud` → `#4ECBF2`, `state.offTarget.hud` → `#FF625B`, `step.cooldown.hud` → `#37A2CB`, `step.unknown.hud` → `#7D9BA9`. Diferencia visual nula (ΔE OK ≤ 0,0062). **Ningún color de contrato cambia.** Lo protege un test nuevo.
4. **Cuatro cosas más que se comían el margen del HUD** y que el token no puede arreglar solo (§2.6), resueltas en la spec: el reflejo interior del vidrio pasa a los 8 dp superiores (bajo la etiqueta bajaba el pulso a 4,1:1); en pausa las tarjetas ya no bajan al 85 % (el pulso daba 3,2:1; el legacy tampoco las atenúa); el glow de objetivo va solo por fuera del borde (adentro, el valor fuera de objetivo caía a 3,1-3,7:1); y la fórmula del vidrio adaptativo de la 6b tenía un peor caso que no era el blanco (un celeste de cielo: el pulso nuevo bajaba a 4,496): pasa a `smoothstep(0,10; 0,70)`.
5. **QA-V2-13:** posición del Designer (§3): en teléfono, barras de desplazamiento ocultas como el legacy; en escritorio, automáticas. Lo implementa el Architect.

---

## 1. Alineación de las biblias

Fuente: 04-design-notes de la It. 2 §11 (lo que había que pasar a las biblias) y §15.6 (lo que la implementación cambió o no adoptó). Manda el código; ningún número de esta tabla cambia lo implementado.

| 04-design-notes | Biblia | Qué quedó |
|---|---|---|
| §11 · ART_BIBLE §3.1 | ART_BIBLE §3.1 | Acento `@60 %` en reposo (legacy); brillo de 24 dp con textura teñida, 0,084 en reposo y 0,14 con dato nuevo (era `@18 %`); valor sin tope inferior (D-10) con centrado óptico de Barlow; marcadores `--:--` con guiones de avance real; destello solo si cambió el texto; etiqueta del pulso y unidad `bpm` en `metric.hr.hud` (DV-03, DV-04, QA-G2-08); tarjeta del pulsómetro (acento fijo, corazón relleno, error sin caja); cabecera que ajusta. Las 4 columnas en tablet quedan como propuesta (P-02) |
| §11 · ART_BIBLE §3.6 | ART_BIBLE §3.6 | `button` 14/600; tonal `accent.tonalBg` + `accent.onTonal`; tabla de variantes con `min-height` y relleno; *hover* 4 % (era 6 %), presionado 8 % y escala; foco con halo; chip, interruptor y spinner |
| §11 · ART_BIBLE §3.11 | ART_BIBLE §3.11 | Brillo superior de la barra (5 %, 32 dp) y sus medidas; cinta 10/700 sin tracking con la geometría del `Banner` (era 11/800 con 0,1 em); AppBar; llave y panel del simulador; padding inferior dentro del contenido |
| §11 · ART_BIBLE §4.3 | ART_BIBLE §4.3 | `bodyM`, `bodyXs`, `button`; `letter-spacing: 8px` = 0,08 em; cajas de línea; centrado óptico; sin mayúsculas por USS |
| §11 · ART_BIBLE §2.4 | ART_BIBLE §2.4 | `ConstantPixelSize` con la densidad (D-03) |
| §11 · ART_BIBLE §3.10 | ART_BIBLE §3.10 | Vacío de Dispositivo (ícono 48, D-11; título `body` `text.secondary`, DV-05; ayuda `bodyXs`); la fila "Vacío" genérica queda para las listas de la It. 3; pantalla provisoria; spinner en botones |
| §11 · 00-deuda-diseno de la It. 2 §3.5 | ART_BIBLE §3.11 y §3.7 | Llave en `accent.primary` sobre `surface.overlay` y *chevron* en `text.tertiary` (04-design-notes §1), insignia 12 y vacío 48 (D-11). El documento de la It. 2 no se toca: está cerrado y 04-design-notes §1 ya lo corrige |
| §11 · ANIMATION_BIBLE §2.1 | ANIMATION_BIBLE §2.1 | Color de la pestaña por código (DN-11); pastilla desde su posición y velocidad; escala del ícono 0-90-260; reducir movimiento |
| §11 · ANIMATION_BIBLE §2.4 | ANIMATION_BIBLE §2.4 | Curvas por celda del rodillo, ancho interpolado, "el número que se ve es el real", destello `k(t)` |
| §11 · ANIMATION_BIBLE §2.9 | ANIMATION_BIBLE §2.9 | Suma de los dos golpes, compresión con `T < 420 ms`, parada en 120 ms, opacidad por código. Corrige un error: "sin pulso, quieto `@40 %`" era solo sin conexión; conectado con `--`, pleno y quieto |
| §11 · ANIMATION_BIBLE §2.12 | ANIMATION_BIBLE §2.12 | Presión 0,97 (0,94 en íconos, pestaña y llave), ida 90 y vuelta 240 `ease-out-cubic`; interruptor; spinner |
| §5.5-§5.10 (no estaba en §11) | ANIMATION_BIBLE §2.13 (nueva) | "Apagar" y "encender" Dispositivo, alto del cuerpo del pulsómetro, llave y panel, `FitBox`, PULSO, resumen de reducir movimiento |
| §2.2 y §15.3 | ANIMATION_BIBLE §0.1 (principios 7 y 8), §1.2 | Coreografías y lo que sale en capturas, por código; USS solo para *hover*, presionado e interruptor, siempre bajo `:hover`; `var()` en transiciones; nada antes del primer cuadro |
| §15.6 · DN-20 no adoptado | ART_BIBLE §3.6 | Etiquetas de botón en una línea con "…" (no dos líneas a 1,3×) |
| §15.6 · presionado `:hover:active` | ART_BIBLE §3.6, ANIMATION_BIBLE §2.12 | Con su motivo (clic simulado de 100 ms de reloj real) y lo que queda para Android ([Device-And]) |
| §15.6 · respaldo de §8.3 | — | No hace falta: S-B1 eligió `GammaRt`. Las biblias no lo mencionan |
| §15.6 · pedidos al Architect y a WP-B | — | No son de las biblias (sondas al catálogo, regla de toma, `Rower` en VID-03, CAP-17): siguen en REPORT §7 |

Agregados que encontré al alinear (no estaban en §11): `state.connected` sobre la insignia en `bg.app` (8,8:1, ART_BIBLE §2.2); la fila de dispositivo encontrado con las medidas reales (§3.7); las barras de desplazamiento (§3.11, con QA-V2-13); la grilla `> 600 → 3` en lugar de "tablet ≥ 840: 4"; los resortes como constantes de `CriticalSpring` y el resto en `MotionSpecs` (ANIMATION_BIBLE §1.2).

---

## 2. QA-V2-12: margen de contraste del HUD

### 2.1 Qué pasaba

El criterio de la It. 1 (ART_BIBLE §2.3, 02-tech-design §4.4 de la It. 1, respuesta a P-05) es "umbral contra W0, sin redondear". Las variantes del HUD se derivaron justo hasta el umbral, sin margen. Pero en pantalla el vidrio no es W0: la mezcla se escribe en 8 bits y W0 redondea **hacia arriba** en los tres canales (41,72 → 42, 46,88 → 47, 54,62 → 55). La luminancia sube de 0,02782 a 0,02801 y todo contraste sobre el vidrio baja 0,24 %.

| Token | Umbral | W0 (test) | W1 (píxel real) | R-T1 medido |
|---|---|---|---|---|
| `metric.hr.hud` `#FF5A7E` | 4,5 | 4,508 | **4,497** | `4.50` |
| `metric.split.hud` `#FEA845` | 7 | 7,004 | **6,987** | `6.99` |
| `metric.distance.hud` `#4CC9F0` | 7 | 7,016 | **6,999** | — |
| `metric.spm.hud` `#38CBEF` | 7 | 7,040 | 7,023 | — |
| `state.offTarget.hud` `#FF5F59` | 4,5 | 4,519 | 4,508 | — |
| `step.cooldown.hud` `#35A0C9` | 4,5 | 4,512 | 4,501 | — |
| `step.unknown.hud` `#7C9AA8` | 4,5 | 4,526 | 4,515 | — |

Tres quedan por debajo del umbral en el píxel real y cuatro a menos de un 0,4 %. QA marcó el pulso; el Split está en el mismo archivo (`snapshots/rt1-measurement.json` de la It. 2: `"metric.split.hud/glass": 6.99`) y no llega al 7:1 de R-10 en pantalla. Todos se usan en el HUD de la 4a (SPM, Split, distancia, BPM, semáforo y paso en curso).

### 2.2 Criterio nuevo

**Cada variante del HUD llega a su umbral contra W2** (`#2B3038`): el vidrio ya en 8 bits más 1 por canal, que cubre la tolerancia de la GPU, del *dithering* y de la sonda (±1, la misma de las capturas). Contra W0 equivale a un margen de ≈ 1,7 % (`(L(W0) + 0,05) / (L(W2) + 0,05)` = 0,9834). Sigue siendo el mismo umbral de R-10 y D-ART-01: solo cambia contra qué fondo se mide.

Por qué W2 y no un porcentaje fijo: es físico (cuantización + 1 LSB), se prueba con un hex y no hay que justificar un número inventado. Por qué no oscurecer el vidrio (`glass.fallback` `@88 %` daría margen a todos con un solo token): es un valor cerrado (P-08, QA-P-17), lo citan ARCHITECTURE §7.4, §20.1 y RA-07, y R-T1 se cerró midiéndolo.

### 2.3 Tokens que cambian

Misma derivación que la It. 1 (ART_BIBLE §2.3): desde la base, OKLCH con el matiz fijo, se sube la claridad de a 0,0005 y se baja el croma solo si sale de gamut, hasta el primer hex que pasa. Con el criterio viejo esa misma rutina reproduce exactos `#38CBEF`, `#FF5A7E`, `#FF5F59` y `#7C9AA8` (y `#FFA841` / `#34A0C8` contra `#FEA845` / `#35A0C9`, a 1-4 LSB): es el mismo método. Con W2 da:

| Token | Antes → ahora | W0 | W1 | W2 | Sobre `surface.card` | OKLCH (L · C · h) | ΔE OK |
|---|---|---|---|---|---|---|---|
| `metric.split.hud` | `#FEA845` → `#FFAA47` | 7,004 → 7,127 | 6,987 → 7,110 | 6,888 → 7,009 | 7,169 → 7,295 | 0,805 · 0,149 · 66,5° | 0,0051 |
| `metric.spm.hud` | `#38CBEF` → `#3ACCF1` | 7,040 → 7,124 | 7,023 → 7,106 | 6,923 → 7,006 | 7,205 → 7,291 | 0,786 · 0,129 · 219,2° | 0,0038 |
| `metric.distance.hud` | `#4CC9F0` → `#4ECBF2` | 7,016 → 7,163 | 6,999 → 7,145 | 6,900 → 7,044 | 7,181 → 7,331 | 0,788 · 0,121 · 222,4° | 0,0060 |
| `metric.hr.hud` | `#FF5A7E` → **`#FF5D7F`** | 4,508 → 4,577 | 4,497 → 4,566 | 4,433 → 4,502 | 4,614 → 4,685 | 0,699 · 0,197 · 11,2° | 0,0049 |
| `step.work.hud` | `#FF5A7E` → `#FF5D7F` | igual que `metric.hr.hud` (misma base `#EF476F`, mismo umbral) | | | | | |
| `state.offTarget.hud` | `#FF5F59` → `#FF625B` | 4,519 → 4,594 | 4,508 → 4,583 | 4,444 → 4,518 | 4,625 → 4,702 | 0,698 · 0,193 · 25,9° | 0,0051 |
| `step.cooldown.hud` | `#35A0C9` → `#37A2CB` | 4,512 → 4,622 | 4,501 → 4,610 | 4,438 → 4,545 | 4,618 → 4,730 | 0,669 · 0,112 · 227,9° | 0,0062 |
| `step.unknown.hud` | `#7C9AA8` → `#7D9BA9` | 4,526 → 4,583 | 4,515 → 4,572 | 4,451 → 4,507 | 4,633 → 4,691 | 0,672 · 0,039 · 227,9° | 0,0033 |

Los matices quedan a menos de 0,5° de la base (`step.unknown.hud`, a 1,1°, igual que antes). Una diferencia de ΔE OK de 0,005 está muy por debajo de lo que se distingue a simple vista (≈ 0,02).

### 2.4 Qué no cambia

- **Ningún color de contrato** (los 21 de `TokensContractTests.LegacyContractHex`): `metric.hr` sigue `#EF476F`, `state.offTarget` `#FF4444`, etc. `metric.distance.hud` deja de ser igual a su base: es una variante de luminancia del mismo matiz, lo que R-10 permite.
- Pasan W2 sin tocarlos: `metric.watts.hud` 9,399, `metric.time.hud` 10,052, `metric.kcal.hud` y `step.rest.hud` 7,035, `step.warmup.hud` 9,203, `text.hudPrimary` 13,269.
- `glass.fallback`, `#2A2F37` como peor caso y la evidencia de R-T1.
- `accent.onTonal` (`#38CBEF`): coincidía con el `metric.spm.hud` viejo, pero es un token independiente (04-design-notes §2.1, "decisión independiente"); el botón tonal no cambia.

### 2.5 Efecto en lo que ya existe (Dispositivo, It. 2)

`metric.hr.hud` ya se usa en Dispositivo: etiqueta `PULSO`, título `PULSÓMETRO` y unidad `bpm`. Pasan a `#FF5D7F` (ΔE 0,0049: no se ve la diferencia) y suben de contraste: N2 (con el brillo de la tarjeta) 4,514 → 4,583; N13 (sobre la tarjeta) 4,614 → 4,685.

- Las sondas del arnés (`SnapshotCatalog`) y el test PlayMode de la etiqueta comparan contra `Tokens.MetricHrHud`, así que siguen al token. La próxima corrida del arnés espera `#FF5D7F`.
- Las capturas y el `manifest.json` de la It. 2 quedan como evidencia de su momento (`#FF5A7E`). `SnapshotEvidenceTests` no mira las sondas de `metric.hr.hud`: ningún test falla por eso.
- Fuera de la familia del pulso, nada de la It. 2 usa los tokens que cambian.

### 2.6 Lo que también se comía el margen (reglas nuevas en la spec del HUD)

Al revisar el vidrio del HUD de la 4a encontré cuatro situaciones que bajan el contraste más que la cuantización, y que un token con un poco más de luz no arregla:

**a) Reflejo interior superior** (ART_BIBLE §3.2: `#FFFFFF @6 % → 0` en el 40 % superior). Las etiquetas del HUD van arriba (relleno 10 + caja de 16 dp), donde el reflejo vale ≈ 3 %. Blanco al `k` % sobre W1:

| `k` | Pulso `#FF5A7E` | Pulso con `#FF6284` (+0,01 L) | Split `#FEA845` | SPM base (etiqueta) | Blanco |
|---|---|---|---|---|---|
| 0 % | 4,497 | 4,700 | 6,987 | 5,461 | 13,460 |
| 1 % | 4,365 | 4,562 | 6,782 | 5,300 | 13,064 |
| 3 % | **4,106** | **4,291** | 6,380 | 4,987 | 12,291 |
| 6 % | 3,736 | 3,905 | 5,805 | 4,537 | 11,184 |

Para pasar con el 3 % el pulso tendría que ser bastante más claro (y menos rojo). **Regla**: el reflejo va solo en los **8 dp superiores**, dentro del relleno de toda pieza de vidrio (10 en las tarjetas, 8 en la chica), nunca debajo de un texto. Se sigue viendo como el filo de luz del vidrio (es lo que hace la barra de pestañas, con 32 dp y la etiqueta debajo). ART_BIBLE §3.2.

**b) Pausa** (ART_BIBLE §3.2 y ANIMATION_BIBLE §2.11: "tarjetas del HUD a 85 %"). Bajar la opacidad de la tarjeta mezcla el texto con la escena: sobre una nube blanca el pulso da **3,2:1**, el Split 4,8:1 y el blanco 8,4:1 (con la escena oscurecida 15 %, 3,6 · 5,3 · 9,4). Los valores siguen actualizándose en pausa (contrato) y se leen. El legacy no atenúa las tarjetas (`immersive_workout_screen.dart:915-940` solo cambia el botón). **Regla**: en pausa, tarjetas y texto al 100 %; la pausa la dicen el velo de la escena (que además oscurece el fondo y **sube** el contraste), el botón Reanudar y el chip de fase.

**c) Vidrio adaptativo de la 6b** (ART_BIBLE §3.2: `a = lerp(0,72; 0,86; smoothstep(0,15; 0,70; L))`, "garantiza el peor caso de §2.3"). No lo garantiza: en los grises medios el alfa sube tarde. Recorriendo el cubo RGB de a 5, el compuesto más claro **no** es el blanco sino un celeste de cielo, `(120; 160; 220)` con alfa 0,760: luminancia 0,02921 contra 0,02782 de W0 (factor de contraste 0,9824). Con los tokens nuevos el pulso bajaría a 4,496 y el Split a 7,002. Con `smoothstep(0,10; 0,70; L)` el compuesto más claro vuelve a ser el blanco en todo el cubo (también con 0,08/0,60 y 0,05/0,55; elijo el cambio mínimo). **Regla**: ART_BIBLE §3.2 con el borde inferior en 0,10, y un test para cuando la fórmula exista en Core (6b): "el compuesto del vidrio adaptativo nunca es más claro que W0", recorriendo el cubo.

**d) Glow de objetivo activo** (ART_BIBLE §3.2: color de la métrica `@20 %` por SDF, del `BoxShadow` del legacy). En Flutter la sombra se pinta debajo de la caja y, con la tarjeta translúcida, tiñe también el interior. Si el SDF hiciera lo mismo, el valor fuera de objetivo (`state.offTarget.hud`) sobre el vidrio teñido daría 3,7:1 con glow rojo, 3,3:1 con el cian de SPM y 3,1:1 con el ámbar de Split. **Regla**: el glow va solo por fuera del borde.

Los cuatro son especificación de pantallas que todavía no existen (4a, 4b y 6b): no hay código que cambiar hoy.

### 2.7 Verificación

| Qué | Resultado |
|---|---|
| `Tokens.cs` | 8 valores; comentario del bloque con el criterio W2 |
| `Tokens.uss` | regenerado con `tools/tokens` (el `.dll` compilado; ver nota): 8 variables cambian, `Typography.uss` igual. `--check` sale con 0 (127 tokens) |
| Test nuevo `ContrastTests.HudKeepsItsThresholdOnTheRenderedWorstGlass` | los 14 tokens con rol de HUD contra W2: pasan; el menor, `metric.hr.hud` y `step.work.hud` con 4,502 |
| Tests con valores fijos actualizados | `TokensContractTests.MandatoryTokensHaveTheirFieldValueAndRole` (8 hex), `ContrastTests.ControlValuesMatchTheDesignNotes` (6 valores), `MetricPaletteTests.LabelAndIconColors` (`#FF5D7F`) |
| `tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it3/designer-debt` | compilación con 0 advertencias (incluye `RowMate.Tests.UnityApiCheck`, el perfil de Unity); 1 265 tests: 1 248 pasan, 0 fallan, 17 omitidos (los NO VERIFICABLE de equipo y GitHub de siempre). N2 4,583 y N13 4,685 en la salida |
| Unity | no se abrió. Los cambios son datos (`Tokens.cs`, `Tokens.uss`) y tests de EditMode que también compila dotnet en el perfil de Unity; las sondas y el test PlayMode siguen al token |

Nota para el Architect: el comando del encabezado de `tools/tokens/Program.cs` (`dotnet run --project tools/dotnet/RowMate.Tokens --artifacts-path …`) compila pero después busca el ejecutable en `bin/Debug/net8.0` y falla ("cannot find the file"). La forma de `tools/dotnet/README.md` (correr el `.dll` de la carpeta de artefactos) funciona. Es menor; conviene corregir el comentario.

---

## 3. QA-V2-13: barras de desplazamiento en teléfono

QA: la barra vertical queda siempre visible en los perfiles de teléfono (CAP-05) y el legacy en Android no la muestra. Posición del Designer:

- **Teléfono (sin puntero): ocultas** (`verticalScrollerVisibility = Hidden`). Es paridad: el `ScrollBehavior` de Material en Android no dibuja barras, y el desplazamiento se nota por el contenido que corre por debajo de la barra de pestañas translúcida. Una barra "que aparece al desplazar" sería una mejora, pero UI Toolkit no la trae y no vale el código propio ahora.
- **Escritorio:** como hoy, visibilidad automática, *dragger* de 4 dp `border.strong`, sin flechas.

Lo implementa el Architect, y coincide con lo que tiene en curso al escribir esto (`Presentation/Controls/ScrollPolicy.cs`: `Auto` en escritorio y `Hidden` en teléfono, sin barra horizontal), que tendría que valer también para las listas y el editor de la It. 3. Quedó en ART_BIBLE §3.11.

---

## 4. Para otros

| Para | Qué |
|---|---|
| Coordinador / PO | Tres textos citan los hex viejos como ejemplo de D-ART-01: ROADMAP §It. 4a, demo (b) (`#FF5F59`), ROADMAP §6, fila D-ART-01 (`#FF5A7E`, `#FF5F59`) y FEATURE_INVENTORY Q-60 (`#FF5F59`). La decisión no cambia (familia roja ≥ 4,5:1 con una variante del mismo matiz); solo el valor: `#FF625B` y `#FF5D7F`. Conviene actualizarlos o remitir a ART_BIBLE §2.3 |
| QA | En la 4a, medir el HUD contra el criterio W2 (§2.2). La próxima `rt1-measurement.json` tendría que dar ≈ 4,57 en el pulso y ≈ 7,11 en el Split |
| Architect | QA-V2-13 (§3): de acuerdo con `ScrollPolicy`. En la 6b, la fórmula del vidrio adaptativo de ART_BIBLE §3.2 y su test (§2.6 c). El comentario de `tools/tokens/Program.cs` (§2.7) |
| Designer (4a) | Las 04-design-notes de la 4a parten de ART_BIBLE §3.2 con las cuatro reglas de §2.6 |

No se editaron documentos de iteraciones cerradas (01-fundaciones, 02-dispositivo-simulador) ni LEGACY_VISUAL_AUDIT o QA_PLAN_REVIEW: sus hex son los de su momento.
