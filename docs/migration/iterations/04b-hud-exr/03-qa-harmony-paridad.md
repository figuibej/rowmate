# It. 4b — Gate de armonía, lente "paridad" (QA)

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-10-29 |
| Iteración | 4b · `04b-hud-exr` ([ROADMAP §It. 4b](../../ROADMAP.md)) |
| Lente | **Paridad**: el QUÉ ([01-po-spec](01-po-spec.md)) y el CÓMO ([02-tech-design](02-tech-design.md), [04-design-notes](04-design-notes.md)) contra el legacy, verificados en su código y no en los documentos. También se leyeron [00-deuda-4a](00-deuda-4a.md) y [00-deuda-diseno](00-deuda-diseno.md) |
| Legacy (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy). Enteros: `lib/features/workout/step_pill.dart`, `test/step_pill_test.dart`, `lib/features/workout/immersive_workout_screen.dart`, `workout_provider.dart`, `step_target_tracker.dart`, `lib/shared/theme.dart`, `docs/superpowers/specs/2026-10-02-step-timeline-states-design.md` y el diff de `3ae0f3a`. En parte: `series_panels.dart:1-140` (`WallClock`), `workout_screen.dart:150-200`, `lib/core/models/interval_step.dart:79-110`, `lib/core/database/database_service.dart:254-261`, `lib/main.dart:50-125`, `lib/l10n/app_localizations.dart` (los *getters* de Workout) y las claves de Workout de `lib/l10n/app_es.arb` y `app_en.arb`. Se miró `docs/image-03.png`. No se leyó `lib/core/strava/strava_secrets.dart` |
| Herramientas | Unity **no** se abrió y dotnet no hizo falta. Sondas propias en `<scratch>/it4b/gate-paridad/` (§1.2): un proyecto de Flutter que importa el legacy en solo lectura y un script de anchos con fontTools. Nada de eso escribe en el repo ni en el legacy |
| Veredicto | **APROBADO CON OBSERVACIONES**: 0 bloqueantes, 1 mayor y 7 menores (§2) |

---

## 0. Veredicto

**APROBADO CON OBSERVACIONES.**

La 4b es un puerto fiel de lo que el legacy decide:

- **Las reglas están bien leídas.** Los cuatro estados de la píldora, sus opacidades, el relleno, el último paso en curso (Q-52), el diálogo de completado una vez por sesión y modal, sus textos y su botón, el chip de fase, el reloj de pared y las barras del sistema coinciden línea por línea con el legacy. En la muestra de **22 casos** (§4), 15 salen sin observaciones; los otros 7 dan los issues de §2, y ninguno es sobre una regla del motor.
- **El caso legacy está entero.** LT-46 tiene sus 12 afirmaciones mapeadas (§5), con un riesgo de que el port quede tautológico (P08).
- **La cobertura cierra.** Los 100 IDs de la spec están en la matriz de 02-tech-design §6.4, ni uno más ni uno menos (§9).
- **Las DV que ya existen son correctas**, salvo DV-43, que describe una conducta del legacy que no pasa (P05), y DV-44, que quedó vieja frente a las notas de diseño (P06).

Lo que encontré está casi todo en la **maquetación del legacy**, que nadie había medido. Lo medí con el código real del legacy y su letra de Windows (§1.2):

- **QA-G4b-P01 (mayor) · la pastilla de objetivo.** La regla nueva de las 04-design-notes (corte por fragmentos, pastilla oculta y aires compactos) contradice AC-WKT-14-15, CAP-19 y CAP-20. Tampoco coincide con el legacy, que nunca la oculta, la corta por carácter y le da a lo sumo la mitad del espacio libre. Falta la decisión del PO antes de WP-B0, que es donde se toca `StepInfoBar`.
- **P02 · la fila de información no está pegada al borde en el legacy.** Con pastilla, el `Flexible` y el `Spacer` se reparten el espacio libre, y la cuenta y el reloj quedan corridos: 427 dp del borde en W1280 y 244 en TH. Además saltan. Falta la DV.
- **P03 · el halo del legacy también tiñe la pista** de la píldora en curso. La app lo deja "solo por fuera", sin decirlo.
- **P04 · la barra libre en TV.** En una ventana de 411 el legacy muestra `Entrenamiento Libre` entero, y la app lo recorta.

Los demás son precisiones de documentos: P05 y P06 (DV), P07 (la excepción a Q-59) y P08 (el port de LT-46).

**Orden sugerido:**

1. **Antes de WP-B0:** P01, porque DN4b-16 toca `StepInfoBar`.
2. **Antes de WP-B1:** P07, porque `PillRowGeometry` lo implementa WP-A, y P04.
3. **Antes de WP-A:** P06 (el token y `PhaseChip`) y P08.
4. **Al cerrar la iteración:** P02, P03 y P05, que solo cambian documentos.

---

## 1. Método

### 1.1 Lectura

Todo contra el legacy. La spec y los dos diseños se usaron solo para saber qué buscar. Abrí en su línea las citas `archivo:línea` de esta lente: las de la spec (§4 y las 53 filas de §9) y las de 02-tech-design (§2, §3.4 y §4). Están bien todas las que revisé, más de 60.

### 1.2 Sondas (evidencia propia)

Las sondas viven en `<scratch>/it4b/gate-paridad/` y no se versionan.

**`legacy_probe/`** es un proyecto de Flutter que depende del legacy por ruta relativa, como el oráculo del motor de la 4a.

- Importa solo `step_pill.dart`, `series_panels.dart` e `interval_step.dart`. Sus `import` no llegan al módulo de Strava.
- Se corre con `<flutter-sdk>/bin/flutter pub get --offline` y después `flutter test test/<archivo>.dart`, en tres archivos:
  - **`row_test.dart`** replica la fila de información del legacy (`immersive_workout_screen.dart:382-476`) con el `WallClock` **real**, en anchos de fila de 361, 864 y 1230 dp: las ventanas de 411, 914 y 1280 menos el margen, el borde y el relleno.
  - **`bars_test.dart`** replica la barra libre (`:497-550`) y el título del diálogo de completado (`:213-219`), en ventanas de 360, 390, 411 y 640.
  - **`probe_test.dart`** tiene dos pruebas:
    - el halo de la `StepPill` **real**, con la lectura de los píxeles;
    - una réplica del diálogo (`:196-229`, `:959-996`) que cuenta cuántas veces se vuelve a leer el resumen.
- Las dos primeras cargan Segoe UI (regular, semibold, bold y black) desde `%WINDIR%/Fonts`. Es la letra que usa el legacy en Windows; en Android usa Roboto, con anchos parecidos.

**`legacy_widths.py`** (fontTools, `uv run --offline`, Segoe UI, sin kerning) es una cota independiente de los mismos anchos. Toma el peso 800 como bold y como black.

| Sonda | Resultado |
|---|---|
| `row_test.dart` | **12 / 12 pasan**. Los números, en §3 (P01, P02) |
| `bars_test.dart` | **9 / 9 pasan**. Los números, en §3 (P04, P05) |
| `probe_test.dart` (halo, diálogo) | **1 / 1 y 1 / 1 pasan**. Los números, en §3 (P03, P05). Una primera versión de la fila, con la letra de prueba de Flutter (un em por glifo), desbordaba y no sirvió para medir: la reemplaza `row_test.dart` |
| `legacy_widths.py` | Coincide con `row_test.dart` y con `bars_test.dart` en ± 2 dp |

Entre una corrida y otra los anchos cambian en ± 2 dp, porque el `WallClock` muestra la hora real y Segoe UI no tiene cifras tabulares de verdad. Ninguna conclusión depende de eso.

### 1.3 Guardas

- `git -C ../rowmate-legacy status --porcelain` quedó **vacío** antes de las sondas, entre cada corrida y al final.
- Los metadatos de `.dart_tool/package_config.json` y `.flutter-plugins-dependencies` del legacy no cambiaron.
- Al terminar borré del scratch lo que genera `pub get` (`.dart_tool/`, `pubspec.lock`), que trae rutas de la PC.
- Este documento no tiene rutas de la PC ni datos del equipo.

---

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección sugerida |
|---|---|---|---|---|---|
| **QA-G4b-P01** | **mayor** | po | La regla de la fila de información de las 04-design-notes contradice la spec, y ninguna de las dos es la del legacy. La regla tiene modos compactos (aires de 8 y 10 → 6; `Próximo` de 7 × 3 → 4 × 3), corta la pastilla por fragmentos enteros y la oculta si no entra el primero (DN4b-05, DN4b-16). La spec, en cambio, pide que la pastilla "se recorta con `…`" (AC-WKT-14-15, CAP-19, CAP-20) y fija los aires de AC-WKT-14-04 (8) y AC-WKT-14-14 (10); además, con la regla CAP-01 queda en compacto 1. El legacy nunca oculta la pastilla ni cambia los aires: es un `Flexible` con `TextOverflow.ellipsis` (corte por carácter) que recibe **a lo sumo la mitad** del espacio libre, porque lo comparte con el `Spacer` | `immersive_workout_screen.dart:394-416` (el `Flexible` y el `Spacer`), `:452` (8), `:473` (10); 04-design-notes §3.3 y §10; spec AC-WKT-14-15, CAP-19 y CAP-20. `row_test.dart` en 361 dp: en el estado de CAP-01, `18 spm` queda en **40,3 de 48,2** dp (recortada) y el `Spacer` mide lo mismo (40,3); en `Texto largo`, la pastilla mide **7,9 dp**, una franja teñida sin texto | **El PO decide antes de WP-B0** entre dos caminos: **(a)** la regla del Designer, con AC-WKT-14-15 reescrito (fragmentos, cuándo se oculta, qué aires se achican y hasta cuánto), con las excepciones de modo compacto en AC-WKT-14-04 y -14 y con lo esperado de CAP-01, -19 y -20 corregido; **(b)** la spec (el recorte con `…` sin ocultar), con DN4b-05 retirado. En los dos casos va una DV nueva que describa el legacy: la mitad del espacio libre, el corte por carácter y que nunca se oculta. El Architect alinea DT4b-12 con lo que se decida |
| **QA-G4b-P02** | menor | po | Falta una DV sobre la alineación de la fila. En el legacy, con pastilla, el grupo de la derecha (`Up next`, la cuenta y el reloj) **no queda pegado al borde**: lo que la pastilla no usa de su mitad queda vacío al final de la fila. El grupo, además, **salta**: al entrar `Up next`, y al pasar a un paso sin pastilla o al revés. La app lo alinea siempre al borde desde la 4a: un espaciador que crece (`.rm-flex-spacer`, `flex-grow: 1`), que con M5 pasa a `margin-left: auto`. Además le da a la pastilla todo el espacio libre. Por eso CAP-24, "comparable con `docs/image-03.png`", compara con una toma sin pastilla, que no es lo que el legacy muestra con `Prueba 4a` | `immersive_workout_screen.dart:397-416`. `row_test.dart`, hueco entre el reloj y el borde: **426,7 dp** en 1230 y **243,7** en 864 en el estado de CAP-01; **42,7** en 361 en un trabajo con `18 spm` todavía sin `Up next`; **0** sin pastilla (como `image-03.png`). En 361, el reloj se corre 42,7 dp a la derecha cuando entra `Up next` | El PO suma la DV. El legacy deja el grupo corrido y con saltos según la pastilla y `Próximo`; la app lo pone siempre en el borde, sin saltos (mejora EXR "nada salta"). También precisa la "paridad" de AC-WKT-14-14 y de CAP-24. El diseño no cambia |
| **QA-G4b-P03** | menor | po | AC-WKT-14-11 pide el halo "solo por fuera" y dice que mide lo del legacy, pero el `BoxShadow` del legacy se pinta **debajo de toda la píldora**. La parte sin relleno de la pista (al 30 %) se tiñe y late con el halo. La app la deja en 30 % (04-design-notes §3.2: "la pista no se tiñe"). La lista informativa del Designer (§10) solo dice que el halo del legacy pasa debajo de la fila de información | `step_pill.dart:82-101` (el `BoxShadow` se pinta detrás del hijo; por defecto, con `BlurStyle.normal`). `probe_test.dart`, canal rojo de la pista sin relleno de una píldora de trabajo: **72** sin pulso, **122** con el pulso en 0 y **189** con el pulso en 1; el relleno, 239. `flutter_tester` no aplica el desenfoque: en el equipo, con sigma 5,12, debajo del centro de la píldora queda ≈ 0,76·α (estimado), o sea la pista entre ≈ 40 % y ≈ 67 % de cobertura | Dos caminos: **(a)** el PO lo acepta como cambio del sistema visual, lo suma a la lista informativa (04-design-notes §10) o a §11.1 y AC-WKT-14-11 dice "solo por fuera (el legacy además tiñe la pista)"; **(b)** el Designer reproduce el tinte debajo de la en curso. Por defecto, (a) |
| **QA-G4b-P04** | menor | po | Barra libre en TV: en una ventana de 411 el legacy muestra `Entrenamiento Libre` **entero**, y la app lo recorta (`Entrenamient…`, DN4b-06). Lo recortan la escala del HUD (1,054), el reloj de 20, el chip de ancho fijo y el lugar del punto. Es una pérdida visible frente al legacy en el perfil de referencia (CAP-13). En 390 y 360 el legacy desborda: empuja el reloj y el chip fuera de la barra, y ahí la app está mejor | `immersive_workout_screen.dart:505-519` (el título sin `Flexible`). `bars_test.dart`: en 411 entra con ≈ 3-6 dp libres (`EN MARCHA` o `FINALIZADO`); en 390 y 360, `DESBORDA`. `legacy_widths.py` da lo mismo: +6 a +13 dp en 411, −8 a −15 en 390. 04-design-notes §3.5 y DN4b-27 | El PO tiene dos caminos: **(a)** acepta DN4b-27 (achicar el título hasta 0,8 antes del `…`; entero necesita 0,818), y CAP-13 lo muestra entero; **(b)** registra la DV ("en un teléfono de 411 el legacy lo muestra entero; la app lo recorta") y CAP-13 lo espera recortado a propósito |
| **QA-G4b-P05** | menor | po | §11.1 tiene dos problemas. **DV-43 describe una conducta del legacy que no pasa**: rotar, cambiar la ventana o redibujar la página no vuelve a leer el resumen del diálogo, porque `_CompletionStats` se arma una sola vez dentro del *builder* de `showDialog` y no depende de nada que lo reconstruya. Además **falta la corrección del título del diálogo**: en el legacy el título es una `Row` sin `Flexible` y en un teléfono se sale del diálogo | DV-43 y AC-WKT-21-04. `probe_test.dart` (réplica de `:196-229` y `:959-996`): el resumen se lee **1** vez al abrir y sigue en 1 después de redibujar la página, de rotar y de cambiar la ventana; sigue mostrando `1120 m` con la lectura en 1200. Título (`:213-219`): `bars_test.dart` da que, en 411 y en 360, termina en **406,4 dp** con el diálogo hasta 371 o 320 (desborda); entra en 640. `legacy_widths.py` da +59 dp de más en 411 y +110 en 360 | El PO reescribe DV-43: el legacy lee los totales una sola vez, al armar el diálogo un cuadro después del fin, y no los cambia (solo los volvería a leer algo que reconstruya el `Navigator`, cosa que no medí); la app usa los del punto final, que en la práctica son los mismos. O baja DV-43 a una nota. También suma una DV, o una fila de §15, para el título que el legacy desborda y la app parte en dos líneas (04-design-notes §3.8). AC-WKT-21-04 y -12 no cambian |
| **QA-G4b-P06** | menor | po | El chip no coincide entre la spec y las notas. La spec (DV-44, AC-WKT-16-03 y CAP-15) pide `FINALIZADO` en `#FF5D7F` y el texto en 11/800. Las 04-design-notes miden `#FF5D7F` en 3,942:1, piden `#FF7A92` (`phase.finished.hud`, un token nuevo) y usan 11/700, porque el repo no tiene Inter 800. 02-tech-design §2.7 todavía nombra `step.work.hud`, y su CAP-15 espera `#FF5D7F` | Spec AC-WKT-16-03, §11.1 DV-44 y CAP-15; 04-design-notes §2.1, §3.5 y DN4b-15; 02-tech-design §2.7 y §5.3. El legacy, `:530-547`: 11/800, *tracking* 0,8 y el texto en el color de la fase | El PO actualiza DV-44, AC-WKT-16-03 y CAP-15 a `#FF7A92` y deja dicho que el 700 es del sistema visual (con 700 y el *tracking* se ve como el 800 del legacy). El Architect cambia `PhaseChipSpec.Text` de `FINALIZADO` a `phase.finished.hud` y la comprobación de CAP-15. Va antes de WP-A |
| **QA-G4b-P07** | menor | po | Q-59 (todas del mismo ancho) tiene una excepción nueva que nadie confirmó y que el diseño técnico no tiene. Las 04-design-notes dibujan la en curso como un **pulgar** de 4·s × 10·s y la hueca con **dos rieles** en **toda** píldora de menos de 6 base, también con separación 1 (por ejemplo, 100 pasos en TV, de 2,45 base). 02-tech-design §2.2 solo prevé un mínimo de 2 base y solo en la tira. En el legacy todas son `Expanded` iguales, aunque midan 0 | 04-design-notes §3.2, DN4b-10, DN4b-18 y §10 ("PO · Q-59"); 02-tech-design §2.2 (`StripCurrentMin` 2, "en la tira") y §3.4; spec AC-WKT-14-08 y -16, DV-40; `step_pill.dart:103` | El PO confirma la excepción (menos de 6 base, en cualquier régimen) y la escribe en AC-WKT-14-08, en AC-WKT-14-16 y en DV-40. El Architect actualiza §2.2, §3.4 y `PillRowGeometryTests` con el pulgar y los rieles. Va antes de WP-A, porque `PillRowGeometry` es de WP-A |
| **QA-G4b-P08** | menor | architect | El port de LT-46 "sobre `StepPills.Paint`" puede quedar tautológico. En el legacy, cada píldora entra como (estado, resultado), y la regla "hecha sin resultado = cumplida" vive en el `switch` de `step_pill.dart:43-55`. Si el test le pasa `StepPillLook.Solid` a las dos, `expect(noTarget, done)` no prueba nada | `test/step_pill_test.dart:20-26` y `:34-36`; 02-tech-design §2.1 (`LookOf`, `Paint`) y §6.3 | Que `StepPillsTests.PendingActiveDoneAndMissedPaintDifferently` arme los cinco aspectos con `LookOf`: índices antes, en y después del paso en curso, con `Met`, `Missed` y `null`. Después, que compare el color final (el base del tipo por el alfa) y el borde, con las 12 afirmaciones de §5 una por una y con el mapeo escrito en el test. `PillRowTests` hace lo mismo con la fila montada |

---

## 3. Detalle de las sondas

### 3.1 La fila de información del legacy (P01, P02)

`row_test.dart`. La fila replica `:382-476`: el tipo 13/700; 6; el `Flexible` con la pastilla 11/600 y relleno 6 × 2; el `Spacer`; `Up next: ` 10 y el tipo 10 en negrita, con relleno 7 × 3; 8; la cuenta 18/800; 10; y el `WallClock(framed: false)` real.

| Ancho de la fila | Caso | Pastilla (natural) | `Spacer` | Hueco después del reloj |
|---|---|---|---|---|
| 361 (ventana de 411) | CAP-01: `Trabajo`, `18 spm`, `Up next: Descanso`, `0:10` | **40,3** (48,2): recortada | 40,3 | 0 |
| 361 | trabajo con `18 spm`, lejos del fin (sin `Up next`, `0:45`) | 48,2 (48,2) | 90,8 | **42,7** |
| 361 | sin pastilla (como `docs/image-03.png`) | — | — | 0 |
| 361 | `Texto largo` (`Calentamiento`, `>150W · 24 spm · 02:05/500m`, `Up next: Enfriamiento`) | **7,9** (167,3): una franja | 7,9 | 0 |
| 864 (TH) | CAP-01 · lejos del fin · sin pastilla · `Texto largo` | 48,2 · 48,2 · — · 167,3 | 291,8 · 342,3 · — · 259,4 | **243,7** · **294,2** · 0 · **92,1** |
| 1230 (W1280) | CAP-01 · lejos del fin · sin pastilla · `Texto largo` | 48,2 · 48,2 · — · 167,3 | 474,8 · 525,3 · — · 442,4 | **426,7** · **477,2** · 0 · **275,1** |

Así se lee la tabla:

- **El `Spacer` y la pastilla se reparten el espacio libre por mitades.** Es como trabaja el `Flex` de Flutter: un `Flexible` (ajuste suelto) no le devuelve al `Spacer` lo que no usa, y lo que sobra va al final de la fila.
- **En un teléfono de 411**, `18 spm` ya se recorta a la mitad del espacio, aunque al lado hay 40 dp vacíos. Con `Texto largo` la pastilla es una franja de 8 dp sin texto.
- **En TH y en W1280**, cuando hay pastilla, la cuenta y el reloj quedan a 240-480 dp del borde. Sin pastilla, pegados al borde.

### 3.2 El halo del legacy (P03)

`probe_test.dart`. Es la `StepPill` real, en curso con progreso 0,5, sobre negro. Los valores son del canal rojo.

| Pulso | Pista sin relleno (al 75 % del ancho) | 4 dp arriba de la píldora | Relleno |
|---|---|---|---|
| sin halo | 72 (= 0,30 × 239) | 0 | 239 |
| halo, pulso 0 (α 0,30) | **122** (= (1 − 0,7 × 0,7) × 239) | 0 | 239 |
| halo, pulso 1 (α 0,70) | **189** (= (1 − 0,3 × 0,7) × 239) | 0 | 239 |

**La sombra se pinta debajo de la píldora y se ve a través de la pista al 30 %.** `flutter_tester` no aplica el desenfoque (`MaskFilter.blur`): por eso afuera solo quedan los 2 dp de la expansión, y adentro la sombra llega con α entera. En el equipo el desenfoque la baja a ≈ 0,76·α en el centro de la píldora, pero el tinte sigue ahí.

### 3.3 El diálogo de completado del legacy (P05)

`probe_test.dart`. La réplica abre el diálogo como el legacy: `showDialog` con `barrierDismissible: false`, un `PopScope(canPop: false)` y un `AlertDialog` cuyo `content` tiene un `StatelessWidget` que lee el modelo al dibujarse, como `_CompletionStats`. La página de atrás se redibuja con un `ListenableBuilder`, como el `context.watch` del legacy.

| Momento | Veces que se leyó el resumen | Muestra |
|---|---|---|
| Al abrir | 1 | `1120 m` |
| Llega una lectura (1200) y la página de atrás se redibuja | 1 | `1120 m` |
| Rotar (411 × 914 → 914 × 411) | 1 | `1120 m` |
| Cambiar la ventana (→ 1280 × 720) | 1 | `1120 m` |

### 3.4 La barra libre y el título del diálogo (P04, P05)

`bars_test.dart`, con Segoe UI.

| Ventana | Barra libre con `EN MARCHA` · `FINALIZADO` (título 151,9 dp; chip 93,9 · 94,8) | Título del diálogo |
|---|---|---|
| 360 | desborda · desborda | termina en 406,4 dp, con el diálogo hasta 320: desborda |
| 390 | desborda · desborda | — |
| 411 | **entra**, con ≈ 3-6 dp libres | termina en 406,4 dp, con el diálogo hasta 371: desborda |
| 640 | — | entra |

---

## 4. Muestreo: 22 casos del legacy contra la spec y los diseños

"OK" = la spec y los dos diseños dicen lo mismo que el legacy, o el cambio ya está declarado (DV o mejora EXR).

| # | Legacy | Qué hace (verificado) | Spec | 02 / 04 | Resultado |
|---|---|---|---|---|---|
| 1 | `step_pill.dart:30-32`, `:41-55` | Opacidades 0,18 / 0,30 / 0,85. La no cumplida tiene el interior al 0,18 y un borde de 1,5 al 0,85; la hecha sin resultado es igual a la cumplida | AC-WKT-15-02 a -05 | §2.1 (tabla de `Paint`); 04 §3.2 | OK |
| 2 | `:57-60`, `:71-76` | Alto 8 y radio 4 (`ClipRRect`). Relleno solo si está en curso **y** el progreso es > 0, con `widthFactor` acotado. El borde derecho del relleno es recto. El borde es interno (`BoxDecoration`) | AC-WKT-15-01, -03 | §3.4 (`FillPolygon`, borde recto); 04 §3.2 ("por dentro") | OK |
| 3 | `:82-101`; `immersive_workout_screen.dart:372` | El halo es un `BoxShadow` con el color a 0,3 + 0,4·pulso, desenfoque 8, expansión 2 y radio 5. Va solo en la en curso y "por terminar", y sigue en pausa | AC-WKT-14-11, -06 | §3.4; 04 §3.2 y §4.4 (0,65·α en el borde) | OK en cuándo y cuánto. **P03** (tiñe la pista). Que se extienda ≈ 14 dp está en la lista informativa del Designer |
| 4 | `immersive_workout_screen.dart:359-364`; `step_pill.dart:103` | Relleno 10 / 8 / 10 / 4, separación 3 y `Expanded` iguales. Con más de ≈ 122 pasos en 411 (365 / 3), cada píldora mide 0 | AC-WKT-14-07, -08, -16; DV-40 | §2.2; 04 §3.2 | OK (DV-40). **P07** (el pulgar) |
| 5 | `:367-371`; `workout_provider.dart:31-36`, `:203-216` | El estado sale del índice. El último paso queda en curso (progreso 1 por tiempo, 0 por distancia); su resultado se calcula (`:204`) y no se ve | AC-WKT-14-08, -09, -13 (Q-52) | §2.1 `LookOf` y `Fills`; CAP-06, CAP-12 | OK |
| 6 | `workout_provider.dart:39-41`; `immersive_workout_screen.dart:343-344` | "Por terminar" con 1 a 5 s; "a continuación" con 0 a 15 s y un paso siguiente. Con la rutina terminada, ninguno de los dos | AC-WKT-14-04, -06, -11 (y AC-WKT-04-04 y -05 de la 4a) | §2.11 `EndingSoon` | OK |
| 7 | `:380-476` | Fila de información: tipo, 6, `Flexible` (pastilla con `…`), `Spacer`, `Up next`, 8, cuenta, 10, `WallClock` | AC-WKT-14-03, -04, -14, -15 | §3.4 (`margin-left: auto`); 04 §3.3 (modos, fragmentos) | **P01, P02** |
| 8 | `:319-322`, `:419-470` | Un solo `AnimationController` de 600 + 600 ms mueve el halo, el color de la cuenta y `Up next`, también en pausa | AC-WKT-14-04, -06, -11 (el reloj común) | §3.4 (`StepPulse.PulseAt`); 04 §4.4 | OK. El origen y la curva del reloj común son de la 4a (`StepPulse`) |
| 9 | `series_panels.dart:57-110` | `WallClock`: `HH:mm` (24 h) con su propio `Timer` de 1 s, que redibuja solo al cambiar la hora o el minuto. Ícono `schedule` 16 en `white70`, 5 de aire, 18/800 con cifras tabulares. La variante con marco no se usa: solo `framed: false`, en `:474` y `:515` | AC-WKT-19-01 a -05 | §2.8 (mira cada 250 ms; `Instant`); 04 §3.4 (20/700 = la cuenta, P-07) | OK |
| 10 | `immersive_workout_screen.dart:497-519` | Barra libre: `rowing` 20 `#00B4D8`, 8, `Entrenamiento Libre` 16/600 (fijo en español, sin `Flexible`), `Spacer`, reloj, 10 y chip | AC-WKT-16-01, -02, -06 | §3.7; 04 §3.5 | OK salvo **P04** |
| 11 | `:524-550` | Chip: `EN MARCHA` `#06D6A0`, `PAUSADO` `#FFD166`, `FINALIZADO` `#EF476F` y, en otra fase, `—` en `white38`. Tinte 15 % y borde 50 %, radio 8, relleno 8 × 3, 11/800 con *tracking* 0,8 y el texto en el color de la fase. Su ancho cambia con la etiqueta, así que el reloj se corre al pausar | AC-WKT-16-03, -04 (DV-41, DV-44) | §2.7 (ancho fijo, DT4b-11); 04 §3.5 (`#FF7A92`, 700) | **P06**. El ancho fijo es la mejora "nada salta" (informativo, §8) |
| 12 | `:49-55`, `:29`, `:61` | El diálogo sale una vez por página: después del cuadro en que la página se dibuja con la fase terminada | AC-WKT-21-01, AC-WKT-13-11 | §2.6 `CompletionDialogRule` (300 ms, por `Session`) | OK |
| 13 | `:125-147`; `workout_provider.dart:252-303` | Terminar a mano es `await finish(); reset()`. `finish()` avisa recién al final, y `reset()` corre en la microtarea anterior al cuadro siguiente: el diálogo **solo asoma** si llega una lectura o un bpm durante el guardado. Vale también en libre, porque el diálogo no mira si hay rutina. Cuando asoma, el `pop` del reposo cierra el diálogo y no la página, que sale con la lectura siguiente | AC-WKT-21-07 (Q-53) | §2.6: `ShouldOpen` exige `EndedBy == Completed` | OK (Q-53 lo corrige) |
| 14 | `:207-212` | `barrierDismissible: false` y `PopScope(canPop: false)`: no lo cierran ni un toque afuera, ni "Atrás", ni Escape. Aunque Escape llegara a pedir el `pop`, el `PopScope` lo bloquea | AC-WKT-21-02, AC-APP-03-10 | §3.9 (`Dismissible = false`; `CancelTop` consume) | OK |
| 15 | `:213-229`, `:959-1019` | Título: `check_circle` `#06D6A0` 28, 12 y `workoutCompleted`, en una `Row` sin `Flexible`. Luego `workoutCompletedMessage`, 16 y la caja (blanco α 10 ≈ 4 %, radio 8, relleno 12). Las filas, separadas 8, llevan ícono 20 `#00B4D8`, 12, etiqueta en `white70` (`Expanded`) y valor en negrita de 16. Los valores van en `H:MM:SS` o `MM:SS`, `{n} m` y kcal sin unidad | AC-WKT-21-03 | §3.9; 04 §3.8 (check 32, `num.s` 20/700, `bodyM` 14) | OK. Los tamaños son del sistema visual. **P05** (el título) |
| 16 | `:965-991` | `_CompletionStats` lee `w.totalElapsedSeconds` y `w.data` al dibujarse | AC-WKT-21-04, DV-43 | §2.6 `CompletionSummary` (el punto final) | La regla de la app está bien; la descripción de DV-43 no (**P05**) |
| 17 | `:230-292` | Sin Strava hay un solo `FilledButton(workoutFinish)`, que hace `pop` y `reset()` sin esperar. Si el guardado no terminó, `finish()` falla en `startedAt: _startedAt!` (ya nulo): la sesión queda sin fin ni totales, pero con los puntos guardados. `insertDataPoints` arma el lote enseguida (`database_service.dart:254-261`), antes de que un toque pueda vaciar la lista: su primer `await` es el de la base ya abierta, que vuelve en una microtarea | AC-WKT-21-06, -10 (DV-42) | §2.6 `AcknowledgeCompletion` | OK (DV-42 bien descrita) |
| 18 | `:32-41` | `immersiveSticky` en `initState`; `edgeToEdge` en `dispose`, al terminar la transición de salida. En escritorio no tiene efecto | AC-APP-15-01, -02, -04 | §4.1, §4.2 | OK |
| 19 | `:57-65`, `:94-96`, `:534`, `:569-577` | Con la sesión en reposo, la página se saca y se redibuja en reposo mientras sale: barra libre, `—` y `00:00` | AC-WKT-13-08, -16-04 (DV-41) | §3.3 (`Freeze`) | OK |
| 20 | `app_es.arb:61`, `:74-75`, `:105`; `app_localizations.dart:375`, `:423`, `:429` | `workoutFinish` `Terminar` / `Finish`; `workoutCompleted` y `workoutCompletedMessage` coinciden. Los *fallback* `'Finalizar'` del código son código muerto, porque los *getters* no son nulables. `Tiempo`, `Distancia` y `Calorías` están fijos en español (`:981-990`). `historyStatCalories` es `kcal` | §11.2, AC-APP-10-11 | §2.12 | OK. El catálogo de hoy tiene 202 claves en es y en (contadas en los JSON), y ninguna de las 12 nuevas existe todavía |
| 21 | `workout_provider.dart:147`, `:247` | El reloj cuenta disparos de un `Timer.periodic` de 1 s. Dart no recupera los disparos perdidos, así que con el equipo suspendido el lapso no cuenta (a lo sumo 1 s) | AC-WKT-02-11 (P-17 de la 4a) | §2.9 | OK en la regla. El equipo lo verifica el usuario (demo (c)) |
| 22 | `step_target_tracker.dart:23-61` | Una lectura cuenta si SPM > 0 **o** W > 0; el split, solo si es > 0. Cumple solo si cumple todos los objetivos definidos; los vatios máx. no son objetivo. Sin split informado, ese objetivo no se evalúa; sin remar, no cumplido | AC-WKT-09-09 (el promedio del sello) | §2.5 `StepOutcome` | OK. El sello puede decir `Objetivo cumplido · --:-- / 02:05/500m` en un paso solo de split sin dato: la spec lo prevé ("con un split nunca informado, `--:--`") |

---

## 5. El caso legacy (LT-46)

`test/step_pill_test.dart:19` 'pendiente, en curso, hecho y no cumplido se pintan distinto'. Son cinco píldoras de un trabajo de 60 s (`:7`) en una fila de 400 (`:11`).

| # | Afirmación del legacy | Línea | En el port (02-tech-design §6.3) |
|---|---|---|---|
| 1 | la hecha cumplida con alfa > 0,8 | `:35` | sí |
| 2 | la hecha sin resultado **igual** a la cumplida (el color entero) | `:36` | sí, con la condición de **P08**: que pase por `LookOf` |
| 3 | la pendiente con alfa < 0,3 | `:37` | sí |
| 4 | el interior de la no cumplida con el alfa de la pendiente | `:40` | sí |
| 5 | el borde de la no cumplida con alfa > 0,8 | `:41-42` | sí |
| 6 | la pendiente sin borde | `:43` | sí |
| 7 | la hecha sin borde | `:44` | sí |
| 8 | la en curso con alfa mayor que la pendiente | `:47` | sí |
| 9 | la en curso con alfa menor que la hecha | `:48` | sí |
| 10 | el relleno con `widthFactor` 0,5 | `:49-50` | sí (en PlayMode, la mitad del ancho ± 1 px) |
| 11 | la en curso sin borde (desde `3ae0f3a`, "sin marco") | `:51` | sí; además, `PRT.NoFrameOnTheCurrentPill` |
| 12 | la pendiente sin relleno | `:52` | sí |

Son **12 de 12**. El nombre, la línea y los AC (AC-WKT-15-02 a -05) coinciden en la spec §6 y en 02-tech-design §6.3. El diff de `3ae0f3a` confirma que el marco blanco salió del código y del test (`a-frame` → `a-bg` sin borde).

---

## 6. Q-52, Q-53 y Q-59

| Decisión | Legacy | Spec y diseño | Resultado |
|---|---|---|---|
| **Q-52** (el último paso queda en curso, sin resultado) | El índice no avanza en el último (`workout_provider.dart:205-216`), el tiempo del paso queda en la duración (progreso 1) y `resultOf` existe pero no se pinta (`immersive_workout_screen.dart:367-371`) | AC-WKT-14-13, AC-WKT-09-08 (sin sello), §2.1 `LookOf`, CAP-06, CAP-12 | OK. `Corta` con `R-4` da la 0 y la 2 sólidas rosas, la 1 sólida verde y la 3 llena, como en el legacy |
| **Q-53** (a mano, nunca el diálogo) | Asoma solo con una lectura durante el guardado (caso 13) | AC-WKT-21-07; `ShouldOpen(EndedBy == Completed)`; CHT, CDR y CDT | OK. La corrección cubre también el libre, donde el legacy no mira si hay rutina |
| **Q-59** (mismo ancho) | `Expanded` iguales con 3 de separación; con más de ≈ 122 pasos, ancho 0 | AC-WKT-14-08 (± 1 px), -16 (DV-40), §2.2 | OK hasta la excepción del pulgar (**P07**) |

---

## 7. Textos (Q-01)

- **Las 12 claves nuevas** (§11.2) no chocan con ninguna de las 202 de hoy.
- **El español es el del legacy donde el legacy lo tiene fijo:**
  - `EN MARCHA`, `PAUSADO` y `FINALIZADO` (`:531-533`);
  - `Tiempo`, `Distancia` y `Calorías` (`:981-990`).
- **Las que se reusan** dicen en el catálogo de hoy lo que cita la spec:
  - `workoutCompleted`, `workoutCompletedMessage`, `workoutFinish` y `stepType*`;
  - `hudFreeTitle` (`Entrenamiento Libre` / `Free Workout`) y `hudUpNext` (`Próximo:` / `Up next:`, DV-24);
  - `workoutSaveError`, `stepDurationMeters` (`{meters}m`) y `stepDurationNone` (`?`).
- **No reusar `historyStat*` es correcto**: `historyStatCalories` es `kcal`, no `Calorías`.
- **Los textos sin legacy** son nuevos y no tienen paridad que cuidar: `hudPausedLabel`, `hudSeal*` y los ingleses del chip.

---

## 8. Huecos del spec y notas informativas

**Huecos**, con su issue: son comportamientos del legacy que la spec no registra.

1. La mitad del espacio para la pastilla y el corte por carácter (**P01**).
2. La fila no pegada al borde, con sus saltos (**P02**).
3. El tinte de la pista bajo el halo (**P03**).
4. La barra libre entera en 411 (**P04**).
5. El título del diálogo que desborda (**P05**).

**Notas** (sin issue; ya están cubiertas o son de la "nada salta"):

- **El chip cambia de ancho con la fase y corre el reloj.** `PAUSADO` es más angosto que `EN MARCHA` en el legacy. La app lo fija (DT4b-11, 04 §3.5).
- **En el legacy el halo se dibuja encima de la píldora anterior y debajo de la siguiente**, por el orden de la `Row`. La app lo dibuja debajo de todas (04 §3.2). Es un detalle de 1-2 dp, del sistema visual.
- **Cuando el diálogo asoma a mano** (caso 13), el `pop` del reposo cierra el diálogo y la página queda en reposo hasta la lectura siguiente. Q-53 lo saca entero.
- **Al volver del segundo plano en Android**, el legacy abre el diálogo con su entrada de 150 ms, porque el `addPostFrameCallback` corre recién con el primer cuadro. La app lo muestra ya abierto (AC-WKT-21-09), y está declarado como mejora.
- **Entre el fin y el diálogo** (un cuadro en el legacy, 300 ms en la app), el legacy deja los controles habilitados: un `Pausa` haría `_phase = paused` sobre una sesión terminada (`workout_provider.dart:239-243`). La app los deja al 0,38 desde `Finished` (DN4a-20 de la 4a): es más seguro, y queda detrás del velo.

---

## 9. Cobertura, desde la paridad

- **Spec ↔ matriz.** Extraje los IDs de §4 y §5 de la spec (83 en viñetas y 17 en la tabla de §5.3) y los de 02-tech-design §6.4: **100 = 100**, sin faltantes ni sobrantes. LT-46 tiene su fila (`d p`).
- **Lo que queda sin test automatizado** (la lista del Architect) no tiene observaciones de paridad:
  - **"Atrás" de Android** con el diálogo y en la entrada: en la app pasa por el mismo `DialogLayer.CancelTop()` que Escape, y Escape tiene prueba de PlayMode (CDT, HCT). Solo el equipo queda NO VERIFICABLE.
  - **Las barras de Android**: la regla está en SBR y SBU, y el legacy no tiene más que el `setEnabledSystemUIMode` de `:34` y `:39`.
  - **P-17 en el equipo**: lo verifica el usuario.
  - **Las partes [Device-Win]**: QA las mira a mano en la verificación.

---

## 10. Riesgos (paridad)

| ID | Riesgo | Mitigación |
|---|---|---|
| R-P1 | Las medidas de §3 son con Segoe UI, la letra del legacy en Windows. En un teléfono, con Roboto, cambian algunos dp | Las conclusiones de P01 y P02 no dependen de la letra: el reparto por mitades es del `Flex`. La de P04 vale para 411 con Segoe UI; con Roboto conviene que el PO lo decida igual (DN4b-27) |
| R-P2 | `flutter_tester` no aplica el desenfoque del `BoxShadow`, así que el tinte de P03 sale exagerado en la sonda | Lo que se prueba es el orden de pintura, que no depende del desenfoque. La magnitud en el equipo es una estimación (≈ 0,76·α en el centro) |
| R-P3 | CAP-24 y la demo en W1280 se comparan "con `docs/image-03.png`", una toma sin pastilla | P02: precisar el texto de CAP-24, o compararla con un paso sin objetivos |
| R-P4 | DN4b-05 cambia lo esperado de CAP-01, -19 y -20 si el PO no decide antes de B0/B3 | P01, primero en el orden |

---

## 11. Verificación hecha

1. **Legacy en solo lectura**:
   - `git -C ../rowmate-legacy status --porcelain` quedó vacío antes de empezar, entre las corridas de las sondas y al terminar;
   - los metadatos de su `.dart_tool/` no cambiaron;
   - no se leyó ni se importó `lib/core/strava/strava_secrets.dart`.
2. **Las sondas** (§1.2), en `<scratch>/it4b/gate-paridad/`:
   - `flutter pub get --offline` resolvió sin red;
   - `row_test.dart` dio 12 / 12, `bars_test.dart` 9 / 9 y las dos pruebas de `probe_test.dart` 1 / 1 cada una;
   - `uv run --offline --with fonttools python legacy_widths.py` coincide en ± 2 dp;
   - lo generado por `pub get` se borró al final.
3. **La matriz**: el cruce de IDs de §9 se hizo con `grep` y `comm` en el scratch.
4. **El catálogo**: 202 claves en es y 202 en en, contadas con `ConvertFrom-Json`, sin las entradas `@`.
5. **Unity y dotnet**: Unity no se abrió. dotnet no hizo falta para esta lente, porque nada de lo que se afirma depende del código construido de la 4b, que todavía no existe.
6. **Repo público**: solo se escribió este documento, con rutas relativas, el legacy citado por su URL o `../rowmate-legacy`, el scratch como `<scratch>` y el SDK como `<flutter-sdk>`. Sin rutas de la PC ni datos del equipo. Sin commits.
