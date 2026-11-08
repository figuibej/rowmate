# It. 10a — Notas de diseño (WP-D): arte y movimiento del Bluetooth real

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-08 |
| Entrada | [00-arte-bluetooth](00-arte-bluetooth.md) (D10-01 a -13), [02-tech-design §1.4, §6.4, §10](02-tech-design.md) (A10-03, -05, -07, -10 a -13; DT10-13), [01-po-spec](01-po-spec.md) |
| Modo | Entrega rápida: cada duda se resolvió con el arte, el legacy o un valor por defecto, en una línea (§6). Lo menor va a "Para issues" (§7) |

## 1. Qué quedó hecho

| Pieza | Dónde | Notas |
|---|---|---|
| Constantes y curvas | `Core/Design/BleMotionSpecs.cs` + `BleMotionSpecsTests` (dotnet) | Sonar, tarjetas, pasos, check, vista de estado, "se despierta", avisos, diagnóstico. Las funciones son puras y no asignan; CAP-05 queda fijado por test (radios 67/60/37, alfas 0,11/0,26/0,41) |
| Token `mono` | `Tokens.Mono` (JetBrains Mono 400, 12/18) → `tools/tokens` → `Typography.uss` (`.rm-type-mono` y escalas de letra) | `FontAssetPaths` suma la cara Mono 400 (sin tabla de kerning: monoespaciada). `JetBrainsMono-Regular-SDF.asset` lo creó el bootstrapper (`ReapplyFromCommandLine`, 48/5, 1024², dinámico y vacío); `GeneratedAssetGuard` lo protege porque está en `FontAssetPaths.All` (11) |
| Sonar | `Presentation/Device/SonarView.cs` | 2 elementos (la banda con malla y el ícono). Banda de 152 entre la ayuda y la lista; sin banda con alto compacto (< 560 dp: L640, TH). Entra con la regla del empuje y sale con la búsqueda. Eco con cada remo nuevo. Marcador `RowMate.Device.Sonar` |
| Tarjeta de remo | `ScanResultCard.cs`, `ScanResultList.cs` | Disco de 40 con `rowing` 24, nombre en `titleS`, r.card 16. Entrada: crece (0-160) y baja de −8 (60-260), escalonada cada 35 ms. Pasos: el renglón 2 cruza (0-80 / 80-200) con un texto fantasma. "Listo": check dibujado (círculo 0-160, tilde 80-240) en `state.connected`. Las otras tarjetas funden a 0,38 en 160 |
| Vista de estado | `BluetoothIllustration.cs`, `BluetoothStateView.cs` | Anillos de 64 y 84 al 8 % y 5 %, disco de 112 en `surface.card`, ícono de 64 (rojo; `text.tertiary` sin Bluetooth LE). Respira 1 → 0,72 en 3 200 ms (apagado y sin permiso). Alto compacto: 112 / 72 / 48 |
| Transiciones | `Presentation/Motion/BleMotion.cs` (`Fade`, `Grow`, `Wake`, `DiscIn`) + `DeviceScreenView` | "Se despierta" 0-720 (AUX-10a-02); búsqueda → apagado con el disco 0,94 → 1; un cambio en medio salta al final (regla de la It. 2) |
| Indicador del enlace | `Presentation/Controls/LinkIndicator.cs` | Un elemento: spinner, arco de espera (pista en `border.strong`), check dibujado (finito, 240) y `link_off` dibujado. En el aviso del HUD mide con el marcador `RowMate.Toast.LinkNotice` |
| Caja del enlace | `LinkBanner.cs` + Skin | Crece al aparecer (0-160); borde sutil (naranja al 15 % en "monitor apagado") |
| Aviso del HUD | `ToastLayer` (clase `.rm-toast--link`) + Skin | Cápsula de vidrio plano (`glass.fallback`, sin desenfoque), radio 22, contenido centrado, texto en `titleXs` `text.hudPrimary`, check verde y `link_off` naranja |
| Diagnóstico | `BleDebugPanel.cs` + Skin | Valores y hex en `mono`; insignia en píldora con el punto que late (1 → 0,35 → 1 en 300) con cada paquete; hex con 2 líneas de alto mínimo; `DEBUG BLE` con su tracking de 0,08 em |
| Mantener presionada la cabecera | `ConnectedView.cs` | La capa sube 0 → 8 % lineal en 500 ms y vuelve en 160 al soltar o al abrir |

## 2. Reducir movimiento (A10-12)

Sonar: anillos quietos (u = 0,25, 0,55 y 0,85), sin respiración ni eco, banda solo con opacidad. Tarjetas: entran con un fundido de 160 sin cambio de alto; textos con fundido de 80; las otras a 0,38 en el acto; check entero con un fundido de 160. Vista de estado: sin respiración; "se despierta" en 0-160 (glifo y color) y un cruce de opacidad 120 + 120; el disco entra sin escala. Punto del diagnóstico: fijo. Arco de espera y spinner: siguen (indican espera). Test: `DBA.WakingUpWithReducedMotionEndsInTheSameState`.

## 3. Presupuesto (§8 del arte)

| Qué | Resultado |
|---|---|
| Sonar con 3 remos, 120 cuadros (TV) | **0 B** en dibujos, pasos y reloj (`BleAllocationTests.TheSonarDoesNotAllocatePerFrame`) |
| Caja del enlace con el arco de espera | **0 B** (el test de WP-B, con el arco nuevo) |
| Aviso del HUD con el spinner (W1280) | **0 B** en dibujos y pasos |
| HUD | Sin cambios: el aviso es el `#toast` del `ToastLayer`, fuera de `#immersive` (59 elementos, 02-tech-design §6.3) |
| Dispositivo | Tarjeta: 10 elementos (caja, capa, disco, ícono, columna, nombre, renglón, fantasma, flecha, spinner) + el check al conectar. Sonar: 2. Ilustración: 3. Diagnóstico: ≈ 24 |

Los anillos y discos usan `Painter2D` (borde con antialias) en lugar de `MeshGenerationContext.Allocate`: el arte pedía búferes propios para no asignar, y la medición da 0 B igual, con mejor borde a 1,5 dp.

## 4. Capturas revisadas

Se volvieron a tomar las del conjunto `it10a` con el arte (TV, W1280, L640, TH; adaptador falso). CAP-05 (sonar a 1 200 ms), CAP-01 a -04 (ilustración en reposo), CAP-06/-09/-14/-17 (tarjetas y cajas), CAP-15/-16/-18 (cápsula sobre el HUD) y CAP-19/-20 (diagnóstico en mono) se ven como pide el arte. Dos guiones de `BleCatalog` cambiaron de instante para que la toma caiga en un estado quieto: CAP-10 a 1,25 s (el cruce de 200 ms ya terminó, antes de los 300 del `0x07`) y CAP-11 a t₀ + 235 ms (check casi entero; `MotionMayRun`). La comparación de dos corridas idénticas sigue siendo de WP-B2.

## 5. Tests

`BleMotionSpecsTests` (dotnet, 9), `TokenCatalogTests.ItTenAMonoToken`, conteos de fuentes (`ItFourAFontTests`, `TypographyUssSyncTests`, `ItTwoDebtTests`, `ItThreeToolingTests`, `FontAndIconAssetsTests`: 10 → 11 FontAsset, 7 → 8 caras); PlayMode `DeviceBleArtTests` (sonar, alto compacto, check, "se despierta" y su versión reducida, mono en el diagnóstico) y `BleAllocationTests.TheSonarDoesNotAllocatePerFrame`. `DeviceBleTests.TheTappedCardShowsTheStepsAndHoldsReady` toca sin `Settle` (el cruce y el fundido nuevos son finitos y gastaban 200 ms de reloj antes de medir la retención).

## 6. Decisiones (una línea cada una)

- **DD10-01** Título de la vista de estado en 18 negrita (DT10-13 manda sobre D10-06).
- **DD10-02** El aviso del HUD es el `#toast` existente (A10-08 del Architect): cápsula de vidrio en el lugar del aviso común, sin franja A/B propia.
- **DD10-03** `Painter2D` para anillos y discos (antialias; 0 B medido).
- **DD10-04** Monitor apagado sin glifo nuevo: `link_off` dibujado en el `LinkIndicator` (sigue siendo un elemento).
- **DD10-05** El check de "Listo" se infiere en la vista con `IsActive && !ShowSpinner` (la app ya está `Connected`); no se tocó `ScanItemSpec`.
- **DD10-06** Las cajas del enlace crecen al aparecer; el colapso es en el acto (diferir `display: none` rompía los contratos de los tests de WP-B).
- **DD10-07** La lista anterior desaparece en el acto al buscar otra vez (sin el colapso conjunto).
- **DD10-08** La capa de la cabecera usa `text.primary` (la capa de estado de la app), de −12 a +12 dp del contenido.

## 7. Para issues

- Colapso animado de la caja del enlace, del banner de error y de la lista anterior (verbos de la It. 3), con `display: none` diferido y los tests de WP-B ajustados.
- Ícono de error 1 200 ms en la tarjeta que falló (§2.2): hace falta saber en la vista qué tarjeta falló (dato de App).
- Spinner que nace donde termina el arco lleno (§4.1, "el arco se vuelve la cabeza del spinner").
- Aviso del HUD: entrada con y ±8 y ancho que sigue al texto en 240 (§4.2); hoy usa la transición común del `ToastLayer`.
- Barras de RSSI y ícono por tipo de máquina (P2, A10-02, ya listado por el Architect).
- La tarjeta crece hasta 64 + 8: con letra 1,3× su alto natural es mayor y al final del crecimiento salta unos dp.
- Los `.mat` `HorizonFar`/`HorizonNear` cambian al reaplicar el bootstrapper (paso `scene.materials`): se revirtieron; revisar la deriva.
