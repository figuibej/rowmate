# It. 8 — Notas de diseño (WP-D: shaders, materiales, selector y pulido)

| Campo | Valor |
|---|---|
| Autor | rowmate-designer |
| Fecha | 2026-11-07 |
| Entrada | [02-tech-design](02-tech-design.md) §1.3, §8.3, §8.4, "Cambios por gate" y "Cambios por implementación de WP-B" (B8-01 a B8-09); [01-po-spec](01-po-spec.md); [00-arte-entornos](00-arte-entornos.md) §1 a §9; las tomas de `it8` ([snapshots/](snapshots/INDEX.md)) y una corrida parcial propia en `<scratch>` |
| Modo | Entrega rápida: cada decisión en una línea; lo menor va a "Para issues" |

WP-D llegó después de WP-B (B8-01): los objetos de río, costa y regata ya van **horneados por segmento** con `Prop` (no instanciados con `float4`), las cifras del cartel en 7 segmentos y el público en cajas. Este paquete hace D1 + D2 sobre eso, sin tocar la lógica ni el armado de los kits: todo lo nuevo entra por globales y propiedades de material, y lo que costaría rehacer el armado de B queda en "Para issues". Cada número nuevo vive en `Core/Scene/Art/{RiverArt,CoastArt,RegattaArt,WaterArt,ShaderArt}.cs` y el shader lo copia como `kRm*` (lo comprueban `SceneShaderContractTests` y `EnvironmentShaderContractTests`).

---

## 1. Qué se hizo contra el contrato de §8.4

| Pieza del contrato | Estado | Nota (una línea) |
|---|---|---|
| `RowMateLighting.hlsl` · `_RmUnderBridge` | Hecho | `RmUnderBridgeGamma2`: la fórmula de `BridgeShade` (rampa centrada en el borde, × (1 − 0,4·o)) en un salto por el uniforme: sin puente el factor es 1 exacto y no cuesta nada |
| `WaterDegraded` · ola de la costa | Hecho, **sin palabra clave** | `_RmSwell = (A; s; φ₁; φ₂)` global, en un salto por `A > 0` en vez de `RM_WATER_SWELL`: sin variantes nuevas en la colección ni recompilar al cambiar de escenario (DT8-01). Se apaga de 120 a 300 m (§2) |
| `WaterDegraded` · bajo el puente | Hecho | por fragmento con la posición de cinta |
| `Terrain` · adoquín, arena húmeda, césped con franjas | Hecho | `_RmTerrainKind` (0 lago … 3 regata) por material, que pone `SceneWorld.SetEnvironment` (§3) |
| `Horizon` · ventanas del skyline | Hecho | `_RmSkylineWindows` (1 solo en el río, `HorizonRenderer.SetEnvironment`): la grilla del legacy en px de TV (§4) |
| `Glow` (nuevo) | Hecho | halos de faroles, sala de la torre y linterna del faro con destello, y el haz de 2 × 60 m que gira cada 12 s; `GlowRenderer` (un dibujo) y `Glow.mat` en el *bootstrapper* (§5) |
| Hull, Rower, Oar | Hecho | bajo el puente por vértice (0 ALU de fragmento) |
| `Facade`, `Crowd`, `MarkerText` | **No** | B8-01 / B8-05 hornean fachadas, cifras y público dentro de la malla del segmento con `Prop`: no hay por instancia ni UV con qué manejarlos. Rehacer el armado es de WP-B → Para issues |
| `Foam` | Hecho (§10) | `FoamRenderer`: una tira por segmento, instanciada; no depende del armado de los kits |
| `Prop` · tela y cabeceo de boyas | Hecho (§10), **sin palabra clave** | marca en el uv de `KitSegmentMesh` en vez de `RM_PROP_CLOTH`: con una malla por segmento la palabra clave no separa la bandera del mástil |
| `EnvironmentCardMotion` | Hecho | §7. Sin `EnvironmentPicker.uss` (ver §7.2: una hoja más en el tema rompía `HudBudgetTests`) |
| `RiverArt`, `CoastArt`, `RegattaArt`, `ShaderArt` | Hecho | dueño desde D1; constantes nuevas abajo |

## 2. La ola de la costa en el agua (AC-SCN-12-01; DT8-05, DT8-06)

- `c × (1 + h·A·0,25)` con `h = sin(relZ/s + φ₁)·0,6 + sin((0,7·relZ + 1,3x)/s + φ₂)·0,4`, la de `water.frag` sin ruido; φ en `double` desde Core (`CoastSwell.Phases`), así a 42 km no tiembla.
- **Fundido de 120 a 300 m** (`CoastArt.SwellFadeNear/Far`): con `s = 4` la cresta mide 25 m y más allá de 300 m ocupa menos de un píxel en TV: titilaba. El legacy no la apaga porque dibuja en 2D sin perspectiva.
- Fuera de la costa `SceneLighting` pone `_RmSwell = 0` y el salto no corre: el lago, el río y la regata dan los mismos bytes que antes en el agua (la paridad de la 6a y la 7).

## 3. Los suelos (`Terrain`, `_RmTerrainKind`)

| Escenario | Zonas del lago reusadas | Patrón |
|---|---|---|
| Río (1) | playa y pradera → adoquín `#8A8378`; pendiente > 35° → piedra del muro `#9E9E9E`; sin bosque | aparejo trabado de **1,3/6 = 0,217 m** con la fase de cinta (`_RmZonePhase`, no nada): juntas al 30 % y ± 6 % de tono por piedra |
| Costa (2) | abajo → arena `#D8C3A0` (franja húmeda = arena × 0,62); desde 3 m → pasto de la meseta `#556B2F`; empinado → roca `#5D4A42` | — |
| Regata (3) | pradera → césped `#5E8C3E` | franjas de 3 m a lo largo del canal, ± 4 % (sin fase: corren con el canal) |

- El píxel sale de la distancia y lo rasante (sin derivadas: el bloque es un salto por el uniforme y el lago no lo paga). El contraste se apaga cuando la piedra mide menos de 4 píxeles.
- El adoquín usa 0,217 m (y no los 0,20 del arte §1.2) para que entren 6 piedras exactas en el período del ruido fino de Core: con otro tamaño el patrón nadaría al avanzar.

## 4. Las ventanas del skyline (`Horizon`; AC-SCN-20-04)

- La grilla del legacy (`river_environment.dart:58-66`) en px de TV: ventanas de 3 × 4 cada 8 × 7, desde 4 px del borde del bloque y 6 del techo, hasta 4 del horizonte; `#FFE08A` al 85 %; de noche (ambiente < 0,6, fundido de 0,05).
- Cada bloque es un cuadrilátero con el techo a la misma `p`: el fragmento recupera el alto del bloque como `p / v` (v = la fracción de la columna, que ya viaja para la nieve). La mitad de las celdas, con un *hash* fijo por (columna, fila, celda): **no** es la máscara exacta del legacy (el `math.Random` de Dart no entra en un shader) → Para issues si QA la pide.

## 5. Las luces (`Glow`, `GlowRenderer`)

| Luz | Dónde | Tamaño y alfa (× nivel de `NightLights`) |
|---|---|---|
| Farol (río) | globo, `LampGlobeY` 4,1 m sobre el malecón | halo r 1,5, `#FFE082` 35 % (legacy) |
| Sala de la torre (regata) | 8,4 m | halo r 2,2, 25 % |
| Linterna del faro (costa) | 2,5 + 9,6 m | halo r 3, `#FFF59D` 35 %; **destello** × 2,2 cuando el haz apunta a la cámara (± 6°) |
| Haz del faro | desde la linterna, si el faro está a < 400 m | 2 medios haces opuestos de 60 m, 4° de apertura, 0,4 m en la raíz, 18 % → 0 en la punta; giran 360° cada 12 s de escena |

- Un dibujo instanciado (≤ 96), aditivo, cola 3010, sin profundidad; el halo se adelanta 0,6 × r hacia la cámara para que el globo o la linterna no lo corten; todo con la niebla extendida y × `_RmEmissiveGain`.
- De día no hay instancias (nivel 0) y el lago no tiene: 0 dibujos.
- El haz se ajustó en D2 de 35 % a **18 %** (el del arte §3.4): en CAP-20 tapaba la linterna y se leía como una cuña sólida.
- Sin columnas de luz en el agua (DT8-11: son de la 6b).

## 6. Costo (D3D11, nivel Medio, medido por `SceneShaderTests`)

| Shader | ALU (tope) | En saltos por uniforme (tope) | Antes (It. 7) |
|---|---|---|---|
| `WaterDegraded` | 27 (27) | 35 (40): ola + puente | 24 |
| `Terrain` | 33 (35) | 73 (76): suelos + puente; solo río, costa y regata | 33 |
| `Prop` | 22 (30) | 29 (32): puente | 22 |
| `Horizon` | 15 (16) | 53 (56): ventanas, solo el río de noche | 15 |
| `Glow` (nuevo) | 14 (14) | — | — |
| Hull, Rower, Oar | sin cambio en el fragmento | (bajo el puente por vértice) | 21 / 26 / 19 |

- El agua sube 3 ALU en todos los escenarios (las dos comparaciones de los saltos y su producto): `WaterArt.MaxAlu` 24 → 27 y los tests que fijaban 24 (`SceneShaderContractTests`, `RowerShaderContractTests`) dicen 27 con su motivo.
- Lo de los saltos solo corre donde hace falta (costa, puente cerca, suelos de los kits, skyline de noche). `SetPass`: + 1 (`Glow`) en río, costa y regata de noche; 0 en el lago.

## 7. El selector (`EnvironmentCardMotion`; arte §8.2)

### 7.1 Movimiento

| Canal | La nueva | La que se deja |
|---|---|---|
| Anillo de color (elemento aparte, 2,5 `accent.primary`) | opacidad 0 → 1 en 160 `ease.enter`; escala 1,04 → 1 en 260 `ease.enter` | 1 → 0 en 160 `ease.exit` |
| Check | desde 60: escala 0,6 → 1 en 200 `ease.emphasis`, opacidad en 120 | 1 → 0 y 1 → 0,6 en 120 |
| Velo 15 % | → 0 en 160 | → 0,15 en 160 |
| Miniatura | 1 → 1,03 en 240 `ease.enter` | → 1 en 240 `ease.move` |
| Nombre | 400 → 700 por fundido cruzado en 120 | al revés |

- Reducir movimiento: sin escalas, opacidades en 120 lineal. Un toque nuevo arranca desde lo que se ve. `MotionDriver` pide 60 fps mientras corre.
- **La miniatura se acerca por las uv de la imagen**, no con `scale`: con `scale` su caja crecía 3 % y la auditoría de CAP-37 la daba "fuera de `#unity-content-container`".

### 7.2 Sin hoja nueva ni estilos en línea en reposo

- Probado en PlayMode: sumar `EnvironmentPicker.uss` al tema, o las mismas reglas en `Base.uss`, o dejar los estilos de la animación en línea en reposo, hace que el panel del inmersivo asigne en más cuadros (`HudBudgetTests.LongSeriesNoAllocationsPerFrame`: 3 cuadros en el tramo quieto contra un tope de 2; con la versión final, 1). El estilo se vuelve a resolver en todo el panel y cada regla o estilo en línea de más le cuesta.
- Por eso: el movimiento pone sus estilos en línea **solo mientras corre** (el anillo de color armado, el velo y el check en `display: flex`, el anillo fino en 1 `border.default`) y al terminar los suelta todos; en reposo manda la hoja de B tal cual (`Base.uss`, sección It. 8), que ya es el final de cada canal. `RowMateTheme.tss` queda sin cambios.
- Se sacó el *hover* 1,02 del arte §8.2 (escritorio): necesitaba una regla `:hover` en el tema → Para issues.

## 8. Lo que se vio en las tomas y qué se cambió (D2)

| Toma | Visto | Cambio |
|---|---|---|
| CAP-10 / CAP-43 (río, 12 h, puente) | El agua y el casco bajo el tablero, igual de claros que afuera | `_RmUnderBridge`: la franja bajo el tablero baja 40 % con su rampa |
| CAP-12 / CAP-16 (río, 22 h) | Skyline liso de noche; faroles sin halo | Ventanas del skyline y halos |
| CAP-20 / CAP-24 (costa, 22 h) | El faro apagado salvo la linterna | Halo con destello y haz; haz a 18 % (§5) |
| CAP-42 (costa, ola) | El agua de la costa idéntica a la del canal | Bandas de mar de fondo que viajan con las fases de Core |
| CAP-10, CAP-26 (suelos) | Malecón y césped con el canto rodado y el pasto del lago | Adoquín, piedra del muro, arena, césped con franjas |
| CAP-37 (selector) | Falla de auditoría al agrandar la miniatura | Acercamiento por uv (§7.1) |
| Miniaturas | La huella vieja (shaders y Art cambiaron) | Re-horneadas con `tools/unity/bake-thumbnails.ps1` (dos tomas iguales); `thumbnails.json` al día |

## 9. Constantes nuevas (Art)

`RiverArt`: `CobbleSize` (1,3/6), `CobbleJoint` 0,3, `CobbleTone` 0,12, `GlowPull` 0,6. `CoastArt`: `SwellFadeNear/Far` 120 / 300, `PlateauGrassFrom` 3, `WetSandShade` 0,62, `BeamApertureDeg` 4, `BeamRootWidth` 0,4, `BeamFlash` 2,2, `BeamFlashDeg` 6, `BeamAlpha` 0,18. `RegattaArt`: `LawnStripeWidth` 3, `LawnStripeContrast` 0,04. `WaterArt`: `MaxAlu` 27, `MaxBranchAlu` 40. `ShaderArt`: `Environment` (Glow), globales `_RmSwell` y `_RmUnderBridge`, presupuestos de los saltos.

## 10. Lo que suma el pulido del worktree (reconciliación)

La segunda corrida de WP-D (en el worktree local, ver [02-tech-design](02-tech-design.md) §15) hizo además estas piezas, que se sumaron sobre lo de arriba sin cambiar lo que ya estaba:

| Pieza | Qué hace |
|---|---|
| `Foam` + `FoamRenderer` (costa) | La espuma: tira de 1,6 m por segmento armado dentro de los 400 m; al pie del acantilado (x −11,4 a −13,0) densa contra las rocas con alfa 0,5 ↔ 0,9 cada 6,5 s; en la playa (k mod 5 = 2, x −8,0 a −9,6) una línea que sube y baja con la misma onda; los cambios de orilla se apagan en 6 m. Rota por el fBm del ruido de la escena a 2,5 m. Cola 3020, 1 dibujo, ≤ 16 instancias, levantada 3 cm (`CoastArt.FoamLift`) |
| La vida en el vértice (`Prop.PropMotion` + `KitSegmentMesh`) | `KitSegmentMesh` marca en el uv lo que se mueve (`uv.x = −(código + peso)`, `uv.y` = fase): banderas `z += u²·(0,2·sin(4t + φ − 2,5u) + 0,06·sin(9t + 2,1u + φ))`, tela de las sombrillas `y += 0,03·w·sin(5t + φ + 6(x + z))`, boyas con cabeceo 0,08 rad y arfada 0,02 m. En las tres pasadas de `Prop`; las mallas del lago traen uv en [0; 1] y no se mueven |
| Mar abierto en la costa | `_RmWaterExtras.w = 1` (lo pone `WaterSurface` al cambiar de escenario): sin la franja somera del lado del mar; se veía una cuña gris hasta el horizonte |
| Haz del faro en Bajo | `GlowRenderer.Beam`: en Bajo, sin el haz (los halos quedan) |
| Boyas bajo el puente | `Buoy` también se oscurece bajo el tablero (`RmUnderBridgeGamma2` por vértice) |

Con esto salen de "Para issues" `Foam` y la tela de banderas y sombrillas con el cabeceo de las boyas.

## 11. Lo que suma la corrección de la verificación 1 (QA-V8-01)

| Pieza | Qué hace |
|---|---|
| Gaviotas (`SeagullFlock` + `SeagullRenderer`, costa) | Las 5 del legacy (`SeagullFlight`) a 90 m de la cámara: la envergadura de 1,1 m da 5 px de TV, × (5 + i)/5; silueta `#2B2B2B` por las dos caras; aleteo `sin(6t + i)` con la punta ± 0,25 m; opacidad `0,5 + 0,4·ambiente` por el tramado. Un dibujo con `Prop`, sin sombras |
| Público (`Prop`, código 7) | Cada persona se mece 0,04 m de costado y se levanta hasta 0,06 m con su período (1,5 a 4 s) y su fase; los pies quietos, la cabeza lo que más se mueve. Sigue en cajas (el atlas de poses queda para issues) |
| Banderas lejanas de la regata (`Horizon`, z > 0) | Grupos de 4 (rojo, ámbar, azul, verde, profundidad 0,5) con mástil del color de la torre, en la tira lejana; la punta flamea `sin(3t + i)·2 px` |


## Para issues

- `Facade` (ladrillo, sillares, vidrio con cielo), `Crowd` (atlas de 9 cuadros con poses), `MarkerText` (SDF de Barlow): piden volver al instanciado con `float4` de §3.3 o UV y datos por vértice en `KitSegmentMesh` (WP-B).
- La sombra del paso de las gaviotas sobre el agua (las gaviotas ya están, §11).
- Máscara exacta del legacy en las ventanas del skyline (hoy un *hash* fijo con la misma proporción).
- *Hover* 1,02 de la miniatura en escritorio sin regla `:hover` en el tema.
- Sillares del muro y línea de mojado 0,15 m (el muro es `Prop` liso).
- Columnas de luz de los faroles en el agua (6b, issue #6).
- `SceneBudgetTests.MemoryAndSceneCpuPerTierWithinTheBudget(High)` da p95 1,7–1,8 ms (tope 1,5) también en las corridas de WP-B, con otro Unity corriendo pruebas en paralelo en la PC: medirlo con la máquina sola.
