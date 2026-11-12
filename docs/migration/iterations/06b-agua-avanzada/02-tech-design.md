# It. 6b — Agua avanzada, reflejos, estela, niebla, vidrio con desenfoque y rendimiento (diseño técnico)

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-10 |
| Iteración | 6b · `06b-agua-avanzada` ([ROADMAP §It. 6b](../../ROADMAP.md)), sobre los 4 escenarios (Lago, Río, Costa y Regata) con el remero y el bote de la 7 |
| Entrada | [01-po-spec](01-po-spec.md) (AC-6B-01 a -38, DV-149 a -153, set `it6b`) y [00-arte-agua](00-arte-agua.md) (DA6b-01 a -05, PA6b-01 a -21). **Si el arte contradice la spec, manda la spec** (§1) |
| Issues leídos | #1, #2, #3, #6, #12, #13, #22, #31 y #85 (API pública) |
| Legacy | `shaders/water.frag`, `lib/features/workout/scene/water_shader.dart` y lo citado por la spec, de [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura). No se tocó el módulo de credenciales de Strava |
| Pantalla | Al escribir este diseño, la pantalla principal está en su modo normal (no en el de respaldo de 1024 × 768). Cada corrida `[PERF]` igual registra la resolución y la frecuencia reales (§6) |
| Estado | Diseño compacto, **sin código**. Orden: **WP-A ∥ WP-D (shaders)** → **WP-B** → ajuste de WP-D (§9). Corregido por el gate ([03-qa-gate](03-qa-gate.md), G6b-01 a -05): §14 |

Convenciones: las rutas son relativas a `Assets/RowMate/`, salvo `tools/`, `docs/` y `ProjectSettings/`. **N** = nuevo, **C** = cambia. Mundo del legacy: `x` lateral, `y` altura, `z` hacia adelante, cámara del contrato a 2,2 m, popa en `relZ` 5,3. `D` = distancia de escena (double). `S(a; b; v)` = smoothstep. En cada arreglo, el test se escribe **primero** y falla contra el código de hoy.

---

## 0. Resumen y decisiones

1. **Deuda primero** (§2). La entrada temprana tironea porque la precarga corre unidades de "primera vez del proceso" (URP, el JIT de Core, el dibujo de calentamiento) en cuadros visibles del inmersivo, y porque el toque arma la sesión de escena. Arreglo: un **preámbulo** de primera vez al principio de la precarga, unidades **pesadas** que solo corren en cuadros quietos (con el negro de la entrada como último recurso) y el dibujo de calentamiento partido. Las 10 líneas, las montañas y Dispositivo son arreglos chicos.
2. **Core puro** (§3): la regla de elección del agua, el modelo del contrato (`water.frag`), los perfiles por escenario, las fases en double (sin salto a 4096 m), la estela con historia, los micro-anillos, las columnas de luz, el alfa del vidrio y la adaptación térmica con dos pasos nuevos (reflejo y agua degradada).
3. **Render** (§4): un shader `WaterAdvanced` con tres variantes (Alto, Medio y Lite), sobre una malla en anillos. Reflejo planar en Alto. Estela como cinta con historia y columnas de luz como **tipos de `WaterMarks`** (el mismo material: 0 SetPass nuevos, DT6b-23). Vidrio desenfocado calculado **sobre la RT de la escena** después de dibujarla y mostrado como fondo de las piezas de vidrio que ya existen (0 elementos nuevos).
4. **Caída en caliente** (§5): el agua avanzada sabe dibujar la degradada adentro (un salto por uniforme), así el fundido de 0,6 s es un solo dibujo, en los dos sentidos.
5. **Medición** (§6): topes de ARCHITECTURE §20 sin relajar; `[PERF]` en el build con la pantalla y la carga externa registradas.

### 0.1 Decisiones (una línea cada una)

| ID | Decisión |
|---|---|
| DT6b-01 | El agua elegida es estado **de la sesión de la app** (`WaterAvailability`, en `SceneHost`): un fallo o el escalón térmico la dejan degradada hasta reiniciar, sin reintento (`water_shader.dart:12-22`, DV-152) |
| DT6b-02 | La escena queda `Ready` con el agua degradada. Las unidades del agua avanzada van **después** de `Ready` (fase 10). En reposo terminan ≈ 0,3 s después; si el usuario ya entró, la avanzada aparece con su fundido (AC-6B-07) |
| DT6b-03 | El Fresnel es **el peso del legacy** sobre la normal plana, `F = 0,6·(1 − |V·ŷ|)³`. A la altura del contrato es `0,6·(1 − 2,2/√(z² + 2,2²))³`. La normal perturbada solo mueve el reflejo y el brillo. Se rechaza el Schlick del arte (spec §7) |
| DT6b-04 | La columna de brillo del legacy (`exp(−(Δx/w)²)·0,4`, `w = 25 + 4z` dp, × cresta × luz) es el **piso** en todos los niveles avanzados. Su `sparkle` sale de la máscara de facetas de B3 (en Lite, de B2). Alto y Medio suman el GGX del astro, recortado a 1 |
| DT6b-05 | Medio **no** tiene planar del bote (spec #13 y §9). Bajo refleja **solo el cielo** (AC-6B-12): sin banda de orilla |
| DT6b-06 | La espuma de crestas de la costa topa en **0,35** (AC-6B-19), no en el 0,45 del arte |
| DT6b-07 | El tope de SetPass de **Alto sigue en 80** (cambio de la 6a en ARCHITECTURE §20.1). Medio y Bajo, 50. El "≤ 50" de AC-6B-29 se lee por nivel (§12, desvío D-01) |
| DT6b-08 | El desenfoque **no es un renderer feature de URP**. La escena ya se dibuja a pedido en su propia RT (ADR 022): `HudBlurPass` corre un `CommandBuffer` armado una vez por tamaño, con `Graphics.ExecuteCommandBuffer` después del dibujo de la escena. Sin Render Graph ni pasos en la cámara de la UI |
| DT6b-09 | El tinte del vidrio va **por píxel** en la última pasada (`α(L)` con L = la luminancia del fondo desenfocado). La UI muestra un compuesto opaco `_RmHudGlass`: con L = 1 el compuesto es el respaldo `@86 %` sobre blanco, y es monótono en L (§4.4). El contraste peor es el de hoy |
| DT6b-10 | Cada pieza de vidrio usa `_RmHudGlass` como `background-image`, con `background-size` y `background-position` = el rectángulo de `scene-surface` en coordenadas locales de la pieza. Se recalcula en `GeometryChangedEvent`, nunca por cuadro. Durante la cascada (que traslada con *transform*) el desenfoque se corre con la tarjeta: es imperceptible con un radio de ≈ 10 dp |
| DT6b-11 | Resolución del desenfoque: el "1/4 de resolución" de la spec = ¼ de los píxeles de la RT en escritorio (½ por eje, densidad < 2) y ¼ por eje en teléfono (densidad ≥ 2), para que el radio en dp sea parecido (arte §8). 3 pasadas Kawase |
| DT6b-12 | El fundido entre avanzada y degradada se hace en `WaterAdvanced` con `_RmDegradedBlend` (salto por uniforme, `RowMateWater.hlsl` compartido con `WaterDegraded`). El corte es instantáneo **solo** si el *shader* avanzado no puede dibujar (no soportado o excepción al dibujar). Eso queda en el registro |
| DT6b-13 | La línea del registro usa el texto del legacy con su etiqueta: `[Scene] Shader de agua no disponible, usando fallback: <motivo>`, **una** vez por sesión de la app (`WaterFallbackLog`) |
| DT6b-14 | `-rowmate-water auto|advanced|degraded`, `-rowmate-water-fail <s>`, `-rowmate-perf-early <s>` y `-rowmate-perf-gpu-load <ms>` solo existen con `UNITY_EDITOR || DEVELOPMENT_BUILD` (el build de simulador fuerza Development). El build normal los ignora. "advanced" en un equipo sin soporte da la degradada con su línea |
| DT6b-15 | Costa: la boya de 500 m (P-06, la única que hay ahí) sube y baja con la ola del casco: `y = CoastSwell.HullScale · h(4,5; relZ)`. No se agregan boyas a la costa. El vaivén de 0,02 m de las boyas de carril de río y regata (PA6b-20) va a issues: están horneadas por segmento |
| DT6b-16 | CAP-10 y VID-05 corren con `Series 600` en un paso de trabajo, con la boya a `relZ` ≈ 30 m, así la boya está en el cuadro (AC-6B-19) |
| DT6b-17 | Mapa de normales `water.normalDetail` (el *slot* que ya existe): 512² RGBA, BC7 en escritorio y ASTC 4×4 en Android, ≈ 0,33 MB con mips. Así entra en el tope de 1 MB de "contenido sin el pack" de Medio (`SceneBudgetTests`). Si no entra: 256² |
| DT6b-18 | La huella del mapa de normales va en `Art/Textures/Water/water.json` (semilla y SHA-256), con su prueba de dotnet. `thumbnails.json` no cambia |
| DT6b-19 | Las miniaturas del selector siguen con la degradada: `ThumbnailRule` fuerza `WaterForce.Degraded`. `thumbnails.json` no cambia (prueba) |
| DT6b-20 | Charcos "calmos" en Alto y Medio: el quad del charco (`WaterMarks`) dibuja el reflejo con la **normal plana** (cielo analítico; en Alto, el planar sin distorsión) con alfa de disco, más su borde de espuma. Sin textura de interacción (DA6b-02) |
| DT6b-21 | Lo que se apoya en el agua de la costa (charcos, anillos, estela y boya) sube con el mar de fondo B0 por la misma fórmula: `RmSwellHeight` en el vértice, o `CoastSwell.HeightAt` en la CPU |
| DT6b-22 | (G6b-01) `WaterAdvanced` **no tiene `Fallback`** y sus SubShaders llevan el tag `RmWater`=`Advanced`. Hay soporte solo si `Shader.isSupported` **y** `material.GetTag("RmWater", false) == "Advanced"`. La degradada no vive adentro del avanzado: la dibuja `WaterSurfaceController` con `WaterDegraded` (§4.1, §5) |
| DT6b-23 | (G6b-02) La estela y las columnas de luz son **tipos de `WaterMarks`** (4 = columna y 5 = estela; el 3 son los micro-anillos). Usan el mismo material, la variante `INSTANCING_ON` y la cola 3050, con mezcla premultiplicada (`Blend One OneMinusSrcAlpha`). Así no suman SetPass en ningún nivel ni escenario (§6.1). No hay `WaterWake.shader` ni `Glow` tipo 3, y `Glow` no cambia |
| DT6b-24 | (G6b-03) El agua de lo viejo **no cambia sola**: `UiHostOptions.SceneWater` vale `WaterForce.Degraded` por defecto, y `SnapshotCli` fija `Degraded` en los sets `it2` a `it15` y `thumbs`. Solo eligen otra cosa el set `it6b`, la CAP-04 de `it7` y las pruebas de la 6b que la piden (§8) |
| DT6b-25 | (G6b-05) Una `Heavy` corre solo si el cuadro actual **y** el anterior fueron quietos **y** el Input System no muestra ningún puntero, toque o tecla apretados, ni apretados o soltados en este cuadro. El orden del *player loop* está en §2.1 (6) y la regla no depende de él |
| DT6b-26 | (G6b-04) La niebla a 400 m se prueba numérica en [dotnet]. En captura se mide el **peso** de la niebla en la fila de z ≈ 300 m en W1280, con una pasada de prueba. La costura se mide solo donde el agua toca el cielo o la franja de bruma (arte §2.8, §8) |

---

## 1. Pedidos y decisiones del arte

| Pedido | Decisión |
|---|---|
| DA6b-01 reflejo por nivel | **Se adopta en parte**: planar ¼ en Alto. En Medio, cielo analítico + perfil del horizonte + banda de orilla, **sin** el planar del bote (DT6b-05). En Bajo, solo el cielo analítico. Sin SSR ni sondas |
| DA6b-02 sin textura de interacción | **Se adopta**. La calma del charco va por DT6b-20 |
| DA6b-03 Lite en Bajo | **Se adopta** (≤ 35 ALU / 2 tex). La degradada queda como respaldo |
| DA6b-04 la ola del legacy por escenario | **Se adopta** (es la de `CoastSwell`, generalizada) |
| DA6b-05 niebla después del reflejo | **Se adopta** (`water.frag:96`) |
| §4.1 Fresnel de Schlick y "Fresnel máximo" por escenario | **Se rechaza**: manda el peso del legacy (DT6b-03, spec §7). El "más espejo" de la regata sale de la distorsión `k_d` y de la rugosidad |
| §11 Medio con planar del bote / Bajo con banda | **Se rechaza** (DT6b-05) |
| §12 set de tomas del arte | **Se reemplaza** por el set de la spec §5 (32 + 5). Las sondas del arte que coinciden (costura, popa ≤ 0,82, estela) quedan como sondas informativas |
| PA6b-01 `WaterAdvanced` (Alto, Medio y Lite) con las capas de §2 | **Se adopta**, con DT6b-03/-04/-05. Topes en `ShaderArt`: 110/6, 70/5 y 35/2 |
| PA6b-02 malla en anillos | **Se adopta**. Es pura: `Core/Scene/Geometry/WaterRingMesh` (WP-A), ≤ 20 k triángulos, paso de 1 m en Bajo |
| PA6b-03 `WaterProfile` y `WavePhases` | **Se adopta** (§3) |
| PA6b-04 mapa de normales procedural | **Se adopta** con DT6b-17/-18. El generador es puro (`WaterNormalMap`, WP-A) y el Editor lo hornea |
| PA6b-05 perfil del horizonte 256 × 1 | **Se adopta**, solo en Medio. Se rehace por filas (unidad `TextureRow`) cuando cambia el escenario o cada 50 m |
| PA6b-06 banda de orilla | **Se adopta en Medio** (en Bajo no, DT6b-05) |
| PA6b-07 planar de Alto | **Se adopta**, con tope de SetPass 80 y el recorte de §4.2 del arte si no entra |
| PA6b-08 planar chico del bote en Medio | **Se rechaza** (spec #13 y §9: va a issues) |
| PA6b-09 estela | **Se adopta**: historia en Core. La cinta es una malla fija de 3 × 48 segmentos que el vértice expande desde `_RmWake[48]` (sin subir mallas por cuadro). Se dibuja con el material de `WaterMarks`, tipo 5, y no con un `WaterWake` propio (DT6b-23, G6b-02) |
| PA6b-10 micro-anillos y #12 | **Se adopta**. La capacidad de `WaterInteractionRenderer` pasa de 16 a 40 |
| PA6b-11 columnas de luz `Glow` tipo 3 | **Se adopta en parte**: las columnas van como `WaterMarks` tipo 4, en el lote de marcas (capacidad 40 + 32 = 72), también en el lago (la cabaña). En Lite, las 8 más cercanas. No van en `Glow`, que queda en 96 instancias: un lote `Glow` en el lago de noche sumaría 1 SetPass en Medio, que ya está en el tope (DT6b-23, G6b-02) |
| PA6b-12 espuma de crestas y `Foam` ≤ 30 | **Se adopta** con tope 0,35 (DT6b-06) |
| PA6b-13 `WaterModeSelector` | **Se adopta** (§3.2), con la tabla de la spec |
| PA6b-14 `HudBlurFeature` | **Se adopta como `HudBlurPass`** (DT6b-08 a -11) |
| PA6b-15 / -16 deuda #2 y #3 | **Se adoptan** (§2.2 y §2.3) |
| PA6b-17 deuda #1 | **Se adopta** con el diseño de §2.1 |
| PA6b-18 / -19 popa y línea de contacto | **Se adoptan** (§4.5) |
| PA6b-20 boyas | **Se adopta en parte** (DT6b-15) |
| PA6b-21 set `it6b` | **Se adopta** con la lista de la spec (§8) |

---

## 2. Deuda de la 6a: causa, arreglo y test

### 2.1 #1 · QA-V6a-R3-01 y R2-04: la entrada antes de que termine la precarga (AC-6B-01, -02)

**Causa** (06a/05-qa-verification §R3.6 y la lectura de `Scene/SceneHost.cs` y `Bootstrap/SceneBridge.cs`):

- (a) `SceneBridge.Tick` da un tramo de 2 ms con el inmersivo abierto si no hay movimiento finito, y los rodillos de cada lectura no cuentan (B6a-19). `SceneWarmupPolicy.RunAnother` deja correr **al menos una unidad por cuadro**, cueste lo que cueste. Así, en cuadros visibles caen unidades que la primera vez del proceso tardan 11-22 ms: el post de URP en la RT chica (el código de URP por primera vez), el primer `Tick` sintético (el JIT de Core y de `SceneDirector.Apply`) y el dibujo de calentamiento (los estados de *pipeline*). Son los 20,7-20,8 ms "cuando llega la escena".
- (b) `_nextSession` solo se arma después de `Ready` (`SceneHost.cs:515-518`). Con la precarga sin terminar, `Open` construye `SceneSession` en el cuadro del toque, y ese código además corre por primera vez. Son los 19,7-20,0 ms del toque y el margen variable de R2-04.

**Arreglo:**

1. **Preámbulo de primera vez** (fases 0a-0c, antes del mundo, con `SceneWarmupPolicy.UnitClass.Heavy`): (0a) la cámara, la luz y la RT (la unidad 1 de hoy, adelantada); (0b) `FirstUrpRequest`, el primer pedido de URP a una RT de 64² con la máscara en 0; (0c) `CoreJitPreamble`, una `SceneSession` de usar y tirar sin mundo con `Open` y `Tick` a las 12 h, a las 22 h con velo, un paso del vuelo, uno de la salida y uno de la toma aérea, en 5 unidades. Después viene `NextSession`: arma `_nextSession` con `SelectedEnvironment` (liviana, ≤ 1 ms). Si el escenario cambia en reposo, se rearma como hoy.
2. **Clase de unidad** en `SceneWarmupPolicy` (Core): `Heavy` = preámbulo, `PostAtSessionSize` y cualquier unidad cuya cota pase de `VisibleUnitMaxMs` = **4 ms**. Una `Heavy` solo corre en un cuadro **quieto**: Workout en reposo sin movimiento finito, o negro y quieto. Con el inmersivo a la vista, solo corren unidades `Light`. Además, el cuadro anterior tiene que haber sido quieto y no puede haber entrada en curso (DT6b-25, G6b-05). `RunAnother(slice, elapsed, units, nextIsHeavy, in WarmupFrame frame)`, con `WarmupFrame { Quiet, PreviousQuiet, InputActive }`. `InputActive` lo arma `SceneBridge` desde el Input System: `Pointer.current` y `Touchscreen.current` (`press.isPressed`, `wasPressedThisFrame` y `wasReleasedThisFrame`) y `Keyboard.current.anyKey` (`isPressed` y `wasPressedThisFrame`, que cubre Enter y Espacio sobre un botón con foco). El Input System actualiza ese estado en `PreUpdate`, antes de cualquier despacho de la UI, y se lee sin asignar.
3. **El negro de la entrada**: el cuadro del armado del HUD ya es negro y quieto (`ImmersiveWorkoutPage`, QA-V6a-REN-04). Ahí `IImmersiveScene.RunPendingHeavyWork()` corre **todas** las `Heavy` pendientes que se puedan correr. El reloj de la entrada (fondo 220-400 y cascada desde 400) se corre lo que duró ese trabajo: el negro dura un poco más, sin saltos. Si el toque llega antes que el preámbulo (menos de ≈ 0,3 s de Workout quieto), se paga ahí, en negro.
4. **El dibujo de calentamiento partido** en sub-dibujos por grupo de *renderers* (cielo y horizonte; terreno y orilla; objetos e instancias; bote, remero y remos; agua; post), cada uno `Light` y ≤ 4 ms. El grupo de agua crea los estados de *pipeline* de la degradada y, en la fase 10, los de la avanzada. `PostAtSessionSize` queda `Heavy`: si al tocar sigue pendiente, va al negro.
5. **Medida que entra antes de `Ready`**: `-rowmate-perf-early <s>`. Un arranque en frío entra a `Entrenamiento libre` a los `s` segundos de que aparece Workout, sin esperar `Ready`. Registra `[PERF] entrada temprana: precarga en <fase>, toque X ms, peor con movimiento Y ms (a los N ms, unidad <fase>), negro quieto Z ms, carga externa W %`. `build.ps1 -PerfEarly`: 5 arranques con el lago y 1 con cada uno de los otros escenarios (con `-rowmate-perf-env`, elegido en reposo). `perf-log-check.ps1` controla 16,7 ms y guarda el mínimo y el máximo del toque (AC-6B-02).
6. **Orden del *player loop* y el cuadro del toque** (G6b-05). Unity 6.3, sin `EventSystem`: el despacho de UI Toolkit (`DefaultEventSystem.Update`, desde el *callback* `UpdatePanels` del sistema nativo `PreLateUpdate.UIElementsUpdatePanels`) corre **antes** de `PreLateUpdate.ScriptRunBehaviourLateUpdate`, que es donde está `AppRoot.LateUpdate` → `SceneBridge.Tick`. Así, el `PointerDown` del cuadro ya marcó `MotionDriver.UserInputActive` (`ShellHost`, QA-V6a-REN-06) cuando se decide el tramo. Esta es la lectura de la referencia de Unity y de los símbolos de `UnityEngine.UIElementsModule` / `CoreModule` de 6000.3.27f1, y se **comprueba** en el primer paso de (1) con `EarlyEntryTests.PlayerLoopDispatchesUiBeforeLateUpdate`: lee `PlayerLoop.GetCurrentPlayerLoop()` (los índices de los dos sistemas) y, con un clic sintético del Input System, compara el `Time.frameCount` del `ClickEvent` con el del `LateUpdate` que lo ve. Resultado esperado: el mismo cuadro, con el despacho antes. La regla de DT6b-25 **no depende** del orden. Si el despacho fuera después, o si bajar y soltar caen en la misma actualización de entrada, el cuadro igual tiene `InputActive` y la `Heavy` espera. El resultado real se anota en "Cambios por implementación".

**Tests** (primero, en rojo): `SceneWarmupPolicyTests.{HeavyOnlyWhenQuiet, HeavyNeedsPreviousQuietFrameAndNoInput, PreambleBeforeWorld, VisibleUnitsUnderFourMs}` [dotnet]; `EarlyEntryTests.{HeavyUnitsNeverRunOnVisibleImmersiveFrames, TapFrameReusesPreparedSession, BlackFrameRunsPendingHeavyAndShiftsTheClock, EverySubDrawUnderFourMs, PlayerLoopDispatchesUiBeforeLateUpdate, HeavyNeverSharesTheTapFrame}` [PlayMode]. `HeavyNeverSharesTheTapFrame` usa `InputTestFixture` (como `It15GapTests`) con un mouse y un teclado de prueba. Con Workout en reposo, el cuadro anterior quieto y una `Heavy` pendiente, pone en el mismo cuadro (a) bajar y soltar el mouse sobre `Entrenamiento libre` y (b) Enter sobre el botón con foco. Afirma que `SceneHost.LastHeavyFrame` nunca es igual al cuadro del toque ni al siguiente, y que el `Open` usa la sesión preparada o va al negro. La medida real es la de `-PerfEarly` [Device-Win]. El JIT de "primera vez del proceso" no se reproduce en el Editor.

### 2.2 #2 · QA-V6a-PAR-01: la 11.ª línea (AC-6B-03)

- **Causa**: `Art/Shaders/Scene/WaterDegraded.shader:176`. El índice de la línea no tiene tope: en la fila de abajo, `t` llega a 1 y se dibuja una línea más.
- **Arreglo** (WP-D): `n = clamp(round(...); 0; 9)` en `RowMateWater.hlsl`, compartido por `WaterDegraded` y el salto degradado de `WaterAdvanced`. `WaterArt.MaxAlu` pasa de 27 a 28 (WP-A).
- **Test**: `WaterShaderContractTests.DegradedClampsToTenLines` [dotnet, lectura del `.shader`]. Sonda `lines` en el manifiesto: recorre la columna del astro y cuenta los máximos locales sobre el agua. Se aplica a `it6a` (CAP de las 6, 12 y 18 h), que sale con la degradada fija (DT6b-24), y a `it6b` CAP-21. `it6a` se regenera dos veces y da lo mismo. Si la sonda no encuentra agua degradada en la toma (el manifiesto registra `water: Degraded`), la toma **falla**: nunca cuenta 0 líneas.

### 2.3 #3 · QA-V6a-12: montañas y halo (AC-6B-04)

- **Arreglo** (los números del arte §10, sin tocar DV-65 ni los colores del contrato): `LakeArt.FlankToSun` 0,12 → 0,18, `FlankAway` −0,10 → −0,16, `BaseFog` 0,15 → 0,10 y `LeftEnvelopeSlope` 0,20 → 0,14. `SkyArt.GlowStrength` 0,35 → 0,45 cuando `g` > 0,5.
- **Tests**: `LakeArtTests` y `SkyArtTests` con los valores nuevos [dotnet]. Sondas en CAP-03 (halo a 2 y a 6 diámetros, ≥ 15 %) y en CAP-17 (contraste de la silueta ≥ 1,3:1; la ladera tapa ≤ 30 % del ancho de la cordillera) [Editor]. Si una sonda no llega, el Designer ajusta solo esas constantes (DV-153) y lo anota.

### 2.4 `DeviceScreenView.Render` debajo del inmersivo (AC-6B-05)

- **Causa**: `Presentation/Device/DeviceScreenView.cs:139-160` hace `Render()` con cada lectura (`OnDevice`) aunque el inmersivo tape todo (≈ 20 asignaciones por lectura).
- **Arreglo**: `UiContext.ImmersiveCover` (`IReadOnlyObservable<bool>`). `ImmersiveWorkoutPage` lo pone en `true` cuando el negro de la entrada cubre (220 ms) y en `false` en `RevealStarted` de la salida (400 ms). `DeviceScreenView`: con la tapa puesta, solo guarda `_dirty = true`; al sacarla, si quedó sucia, hace `Render()` en ese mismo cuadro, antes de que se vea.
- **Test**: `DeviceCoverTests.{NoRenderUnderImmersive (60 s a 1 Hz → 0), FirstVisibleFrameShowsLastReading}` [PlayMode].

---

## 3. Core puro (WP-A, `noEngineReferences`, C# 9)

Carpeta nueva `Core/Scene/Water/`. Todo sin asignar por cuadro: los arreglos se reservan una vez.

### 3.1 Contrato y perfiles

```csharp
public static class WaterModel                       // water.frag:55-98; water_painter.dart:32-60
{
    public const double CamHeight = 2.2, SternRelZ = 5.3, WakeFoamMax = 0.35, ColumnPeak = 0.4;
    public static SceneColor BaseColor(in SceneColor near, in SceneColor far, double z, double h, double amplitude); // lerp(near; far; S(2;120;z))·(1 + h·A·0,25)
    public static double ReflectionWeight(double z);  // 0,6·(1 − 2,2/√(z² + 2,2²))³: 0,128 @5; 0,290 @10; 0,424 @20; 0,561 @100
    public static double FogWeight(double z);         // S(80; 400; z)
    public static double WakeIntensity(double speedMps);            // clamp(v/4; 0; 1)
    public static double WakeHalfWidth(double backM);               // 0,3 + 0,35·back
    public static double ColumnWidthDp(double z);                   // 25 + 4z
    public static double ColumnWeight(double dxDp, double z, double lightStrength); // exp(−(dx/w)²)·0,4·luz
    public static bool IsTransparentRow(double rowDp, double horizonDp);            // en el horizonte o medio dp debajo
}

public enum WaterLook { Mirror, Current, Sea, Track }   // lago, río, costa, regata (DV-150)
public readonly struct WaterProfile                     // ART_BIBLE §7.7 + arte §3.2 (los números visuales en WaterLookArt)
{
    public EnvironmentId Environment { get; } public WaterLook Look { get; }
    public double Amplitude { get; } public double Scale { get; }       // de ShoreEnvironment (legacy): 0,25/2,5; 0,35/2,0; 0,8/4,0; 0,3/1,5
    public double CurrentMps { get; }                                   // río 0,25 (hacia la cámara); 0 los demás
    public bool Swell { get; }                                          // B0 solo en la costa
    public WaterBand B2 { get; } public WaterBand B3 { get; }          // tile, deriva, θ, fuerza
    public double Roughness { get; } public double Distortion { get; } // α GGX y k_d
    public ShoreBand Left { get; } public ShoreBand Right { get; }      // x_s, H_s, tono (Medio)
}
public static class WaterProfiles { public static WaterProfile For(EnvironmentId id); }
```

`WaterLookArt` (N, `Core/Scene/Art/`) tiene la tabla del arte §3.2 y §4.2 con sus números. La crea WP-A; desde que cierra, sus valores son del Designer (como `WaterArt` en la 6a).

### 3.2 La regla de elección y la caída (F-SCN-13; AC-6B-06 a -08)

```csharp
public enum WaterKind { Degraded, Advanced }
public enum WaterForce { Auto, Advanced, Degraded }
public readonly struct WaterChoice { public WaterKind Kind { get; } public QualityTier Variant { get; } public WaterKind Showing { get; } public string? Reason { get; } }
public readonly struct WaterSelectorInputs                // todo lo que decide, sin Unity
{ public QualityTier Tier; public bool Supported; public bool Failed; public bool ThermalDegraded; public WaterForce Force;
  public bool ForceAllowed; public bool Thumbnail; public bool AdvancedReady; }
public static class WaterModeSelector { public static WaterChoice Choose(in WaterSelectorInputs inputs); }

public sealed class WaterAvailability                     // pegajoso por sesión de la app (DT6b-01)
{
    public bool IsDegradedForApp { get; } public string? Reason { get; }
    public bool ReportUnsupported(string reason);         // true solo la primera vez (→ una línea, DT6b-13)
    public bool ReportFailure(string reason);
    public bool ReportThermal();                          // DV-152
}
public struct WaterFade { public void Start(double now, bool toAdvanced, bool reduceMotion); public double DegradedBlend(double now); public bool Running(double now); } // 0,6 s; con reducir movimiento, en el acto
public static class WaterFallbackLog { public const string Tag = "Scene"; public static string Line(string reason); } // "Shader de agua no disponible, usando fallback: " + motivo
```

| Fila de la spec | `Choose` |
|---|---|
| Sin soporte o falla de carga | `Degraded` (y `Failed` queda pegado) |
| Alto / Medio / Bajo | `Advanced` con `Variant` = el nivel (Bajo = Lite); `Showing = Degraded` mientras `!AdvancedReady` |
| Térmico agotado | `Degraded` (pegado) |
| Forzada (solo con `ForceAllowed`) | La forzada. `Advanced` sin soporte → `Degraded` con motivo |
| Miniatura | `Degraded` |

**Adaptación** (`ThermalAdaptation`, C): `QualityActionKind` suma `LowerReflection` (después de que la resolución llega a 0,75) y `DegradeWater` (después de `LowerTier`, el último). El orden queda resolución → reflejo → sombras → vegetación → nivel → agua degradada (AC-6B-31). Los umbrales no cambian (13 ms de GPU o 15 de CPU, 3 s).

### 3.3 Fases, estela, anillos y columnas

```csharp
public struct WaterPhaseSet { public double Phase1, Phase2; public double B2U, B2V, B3U, B3V, B4U, B4V, NoiseU, NoiseV; public double GridAnchorZ; }
public static class WavePhases                           // Q-68: todo reducido en double; el shader nunca ve D
{
    public static void Compute(double distance, double sceneTime, in WaterProfile p, ref WaterPhaseSet into);
    // φ₁ = Mod(D/s + 0,6t; 2π), φ₂ = Mod(0,7·D/s + 0,9t; 2π) (CoastSwell.Phases generalizado); o = Mod((D·ẑ + v·t·θ̂)/T; 1); ruido Mod(D/256; 1); ancla Mod(D; 16)
}
public sealed class WakeTrail                             // DV-151: 48 puntos cada 0,25 s (12 s)
{
    public const int Capacity = 48; public const double Interval = 0.25, LifeSeconds = 12;
    public void Step(double now, double distance, double hullSpeedMps);   // la velocidad con el surge de la 7 (BoatSurge)
    public void Prime(double now, double distance, double steadySpeedMps); // 48 puntos a velocidad estable (tomas con distancia forzada, G6b-15)
    public void CopyTo(float[] packed);   // por punto: relZ, I₀, edad y recorrido (back): lo lee WakeRenderer → _RmWake
}
public sealed class MicroRingField { public const int Capacity = 24; public void Step(double now, StrokeSpray spray, in StrokeEvents ev, double distance); public int Count { get; } }
public sealed class LightColumns  { public const int Capacity = 32; public void Fill(in SceneFrame frame, NightLights lights, FacadeWindows windows, QualityTier tier); } // faroles, 16 ventanas a < 60 m, cabaña, torre y faro a ±20°; Lite: 8
public static class CoastBuoySwell { public static double Heave(double relZ, in WaterPhaseSet ph, in WaterProfile p); } // 0,15·h(4,5; relZ)
public static class CrestFoam     { public static double Amount(double h, double noise, double z); }   // S(0,55;0,85;h)·S(0,45;0,75;n)·(1 − S(15;150;z)), ≤ 0,35
public static class SwirlOrientation { public static double AngleDeg(in ScullingPose finishPose); }  // ≈ 40° respecto de x (#12)
```

`SceneFrame` suma `WaterFrame Water` (fases, `WaterChoice`, `DegradedBlend`, astro, banda de orilla, columnas y estela empaquetada). `SceneSession.Tick` la llena sin asignar.

### 3.4 Vidrio, malla, mapa de normales, popa y precarga

```csharp
public static class GlassTint                             // ART_BIBLE §3.2; P-04
{
    public static double Alpha(double backgroundL);      // lerp(0,72; 0,86; S(0,10; 0,70; L)): 0,72 @0,10; 0,79 @0,40; 0,86 @0,70
    public static double CompositeL(double backgroundL); // (1 − α)·L + α·L(#070D16); monótona en L
    public static double ContrastOver(double backgroundL, in SceneColor text);
}
public static class WaterRingMesh   { public static void Build(QualityTier tier, MeshData into); }        // 40 × 40 m a 0,5 m (1 m en Bajo), anillos ×2, borde 1,5 km, ≤ 20 k triángulos
public static class WaterNormalMap  { public static void Fill(int size, uint seed, byte[] rgba); }         // RG chop 12 senos, BA rizo 16 senos, sin costura
public static class HorizonProfile  { public static void Fill(EnvironmentId id, double distance, byte[] r8x256); }
```

- `HullMesh` (C): degradé por color de vértice de `#F5F5F5` a `#C9D1D6` en los últimos 2,5 m de la cubierta. Los últimos 0,6 m se afinan de 0,33 a 0,06 m de manga, con la cubierta 0,03 m más baja. Casco, borda y *cockpit* sin cambios (AC-SCN-01-08).
- `SceneWarmupPolicy` (C): `WarmupUnit` suma `FirstUrpRequest`, `CoreJitPreamble`, `NextSession`, `WarmupSubDraw`, `WaterVariant`, `WaterResource` y `WaterSubDraw`. Se agregan `UnitClass Classify(WarmupUnit)` y `VisibleUnitMaxMs = 4`.
- `ShaderArt` (C): `WaterAdvanced` 70/5 (Medio, con `MaxBranchAlu` para el salto degradado y la espuma de la costa), `WaterAdvancedHigh` 110/6, `WaterAdvancedLite` 35/2, `Foam` 64 → **30**, `WaterMarks` con `maxBranchAlu` 32 → 40 (la calma, la columna y la estela, cada una en su rama por tipo; DT6b-23). `Glow` sin cambios (14). No hay `WaterWake`. Globales nuevos `_RmWaterPhases`, `_RmWaterOffsets`, `_RmWaterReflectionTex`, `_RmDegradedBlend`, `_RmWake`.
- `Tests/EditMode/Traceability/It06bIds.cs` (N): los 38 AC con su test.

---

## 4. Render por nivel (WP-B integra, WP-D escribe los shaders)

### 4.1 Tabla por nivel

| | Alto | Medio | Bajo (Lite) | Degradada |
|---|---|---|---|---|
| Malla | Anillos 0,5 m | Anillos 0,5 m | Anillos 1 m | La grilla de la 6a |
| Bandas | B0-B4 | B0-B3 | B0 + B1 + B2 | — |
| Reflejo | Planar ½ × ½ de la RT, con mips (orilla, puente, fachadas, bote y remero) | Cielo analítico + perfil + banda de orilla | Cielo analítico | `skyHorizon` leve |
| Peso | DT6b-03 | DT6b-03 | DT6b-03 | El de la 6a |
| Brillo | Columna del legacy + GGX + destellos + B4 | Columna + GGX + destellos | Columna | Las 10 líneas |
| Estela / anillos / charcos calmos | Sí / sí / sí | Sí / sí / sí | Sí / no / no (charcos de la 7) | No / no / charcos de la 7 |
| Espuma de crestas (costa) | Sí | Sí | No | No |
| Columnas de luz | Sí | Sí | 8 más cercanas | Sí (son `WaterMarks` tipo 4) |
| Vidrio | Desenfoque | Desenfoque | Plano `glass.fallback` | Según el nivel (no depende del agua) |
| Fragmento | ≤ 110 ALU / 6 tex | ≤ 70 / 5 | ≤ 35 / 2 | ≤ 28 / 0 |

Orden de las capas en el fragmento: el del arte §2, con DT6b-03/-04. Cola 2450 (opaca), como la degradada, con un solo pase `UniversalForward`, como `WaterDegraded`. **Sin `Fallback`** y con `Tags { "RmWater" = "Advanced" }` en cada SubShader (DT6b-22). Con un `Fallback` a la degradada, `Shader.isSupported` daría `true` en un equipo sin soporte, que dibujaría la degradada sin línea en el registro (G6b-01). R-04 (ARCHITECTURE §14.2) se cumple con el par de *renderers* de `WaterSurfaceController`: la degradada sigue siendo obligatoria, pero ya no es un SubShader del avanzado. Se anota en "Cambios de la It. 6b" de ARCHITECTURE.

### 4.2 Pases de un cuadro (`SceneHost.DrawFrame`)

1. **Reflejo** (Alto, agua avanzada visible, reflejo no bajado por la adaptación): `PlanarReflection.Render` → `_RmWaterReflectionTex` (½ × ½ de la RT de la escena, mips automáticos). Usa una cámara espejo deshabilitada, con matriz de reflexión en y = 0 y proyección oblicua, por `RenderPipeline.SubmitRenderRequest` con `GL.invertCulling = true` alrededor. Va sin post, sin sombras ni MSAA, con recorte lejano en 600 m. El agua, `ScreenOverlay` y las partículas se apagan durante ese pedido. Las instancias (`InstanceBatch.Draw`) son por cámara: el pasto, los juncos y las rocas chicas no se encolan para el espejo, y los objetos van en LOD1. Marcador CPU `RowMate.Scene.Reflection`.
2. **Escena** (como hoy) → la RT de la escena.
3. **Desenfoque** (§4.4) si hay vidrio a la vista y el nivel lo permite → `_RmHudGlass`.

### 4.3 Estela, marcas, columnas, espuma y boyas

- `WakeRenderer` (N): una malla fija de 3 cintas × 48 segmentos (≈ 300 triángulos). Se dibuja con **el mismo material de `WaterMarks`** (el `Material` de `WaterInteractionRenderer`), con `Graphics.RenderMeshInstanced` y **1 instancia**, para que use la misma variante `INSTANCING_ON` que el lote de marcas. La malla lleva el tipo 5 en `uv2.x`. Va en la cola 3050 del material: es lo único de esa cola junto con las marcas, así los dos dibujos quedan seguidos y no hay SetPass nuevo (DT6b-23). El vértice expande desde `_RmWake[48]` (`SetVectorArray` global con un `Vector4[]` reservado) y suma `RmSwellHeight` en la costa. Al caer a la degradada se apaga en 0,6 s (`_RmWakeFade`).
- `WaterInteractionRenderer` (C): capacidad 40 + 32 = **72** en un solo lote. Tipo 3 = micro-anillos de `MicroRingField` y **tipo 4 = la columna de luz** (un quad acostado sobre el agua desde el pie de la luz hacia la cámara, con franjas con la fase de B2), desde `LightColumns`, también en el lago (la cabaña). Radio del charco × 1,3 con alfa mínimo 0,35 hasta los 2 s, par de remolinos girado con `SwirlOrientation` y hervidero + 25 %. Charco calmo en Alto y Medio (DT6b-20). En la costa, `y` + la ola (DT6b-21). `WaterMarks.shader` pasa a la mezcla premultiplicada (WP-D): los tipos de hoy devuelven `(col·a; a)` y se ven igual (las pruebas de la 7 no cambian), y la columna devuelve `(col·w; 0)`, que es aditiva como el `Glow`.
- `GlowRenderer` **sin cambios** (96 instancias; halos, globos, linterna y haz).
- `MarkerBuoy` (C): en la costa, `y = CoastBuoySwell.Heave`.
- `FoamRenderer` (C solo en lo que necesite pasar al vértice para #22).

### 4.4 Vidrio con desenfoque (F-WKT-17; AC-6B-23 a -27)

- `HudBlurPass` (N, `RowMate.Scene`): `RenderTexture`s propias (`_RmHudBlurA/B` y `_RmHudGlass`) en el formato y el espacio de la RT de la escena. Se reservan al cambiar de tamaño, nunca por cuadro. El `CommandBuffer` se arma una vez por tamaño: bajada a ½ (4 lecturas), bajada a ¼ (solo en teléfono, DT6b-11), 3 Kawase (1,5, 2,5 y 3,5 texeles) y **composición** (`α(L)` por píxel, tramado Bayer ± 0,5/255, `#070D16`). Se ejecuta con `Graphics.ExecuteCommandBuffer` dentro de `BeginSample/EndSample("RowMate.HudBlur")` para medir la GPU.
- Shader `RowMate/UI/HudBlur` (WP-D), en `Art/Shaders/UI/`, con 3 pasadas (bajada, Kawase y composición).
- `IImmersiveScene` (C) suma `Texture? GlassTexture`, `double GlassWeight` (0-1: la escena a la vista × el nivel con desenfoque, con su fundido de 200 ms) y `event Action GlassChanged`.
- `GlassSurface` (N, `Presentation/Workout/`): un helper sin elementos. `Attach(VisualElement piece, SceneSurface surface)` pone el fondo (DT6b-10), registra en el `generateVisualContent` de la pieza el reflejo interior (`#FFFFFF @6 % → 0` en los 8 dp de arriba, debajo de los hijos) y anima `unityBackgroundImageTintColor.a` con `GlassWeight`. El fundido de 200 ms es un movimiento finito: puede rehacer el estilo, y fuera de él no se toca nada. Lo usan `HudCard`, `HudBand`, `FreeTopBar`, `StepInfoBar`, `HudControls`, `CenterCallouts` (las placas), `SeriesPanelView` (su vidrio enmascarado pasa a `MeshGenerationContext.Allocate(..., texture)` con UV de pantalla) y `CompletionDialog` (`.rm-dialog--glass`, P-19; el velo del modal no cambia).
- Debajo de la imagen, el `background-color` sigue en `glass.fallback`. Con `GlassWeight` = 0 (Bajo, escena sin mostrar, sin soporte de RT) se ve el respaldo plano de hoy, sin el reflejo interior.
- Contraste (AC-6B-24): `GlassTint.CompositeL` es monótona y vale lo del respaldo con L = 1, así que el peor caso es el `@86 %` de hoy. La prueba de dotnet lo recorre en L ∈ [0; 1] con paso 1/255.

### 4.5 Popa y línea de contacto (#85, #13)

`HullMesh` (WP-A, §3.4) y `Hull.shader` con el Fresnel de la cubierta (`N.y > 0,8`) recortado a 0,15 (WP-D). La línea de contacto (`col × 0,75`, caída de 0,06 m dentro de la huella del casco) va en `WaterAdvanced` con `_RmHullFootprint`, que da `BoatSpec`.

---

## 5. Disponibilidad y caída en caliente

`SceneHost` (C) tiene `WaterAvailability`, `WaterFade` y el `WaterChoice` del cuadro. `WaterSurfaceController` (N, `Scene/Water/`) tiene dos *renderers*: el de la 6a (degradada, la grilla) y el avanzado (anillos y material del nivel).

| Momento | Qué pasa |
|---|---|
| Precarga, fase 10 (después de `Ready`) | Sus unidades son `Light`. (a) Soporte (DT6b-22): `Shader.isSupported` **y** `material.GetTag("RmWater", false) == "Advanced"` del material del nivel, después de fijar sus palabras clave. Si falla una de las dos: `ReportUnsupported("sin SubShader avanzado")` y una línea. (b) Las variantes del nivel con `WarmUpProgressively(1)`; las de los niveles de abajo, solo en reposo. (c) El perfil del horizonte por filas (Medio). (d) La RT del reflejo (Alto): si `Create()` falla, `ReportFailure`. (e) Sub-dibujos: el espejo con máscara 0 a 64² y el agua avanzada en una RT de 64². (f) `AdvancedReady` |
| Se pone lista con el inmersivo abierto | `WaterFade.Start(toAdvanced)`: se prende el *renderer* avanzado con `_RmDegradedBlend` 1 → 0 en 0,6 s y el degradado se apaga **en el mismo cuadro** (el avanzado con blend 1 ya dibuja lo mismo). Nunca hay dos opacos coplanares en y = 0 ni un SetPass de más durante el fundido (G6b-14). La distancia, el tiempo y la velocidad no cambian (es otro material del mismo cuadro) |
| Falla en medio de la sesión (excepción en `Apply`/`Render` del agua o del espejo, la RT del reflejo perdida, `-rowmate-water-fail <s>`) | `ReportFailure(motivo)` → una línea → `WaterFade.Start(toDegraded)`: la avanzada sigue dibujando con el blend 0 → 1 y la estela se funde. Al terminar, se apaga y queda la grilla. Si no puede dibujar (excepción en el dibujo), el corte es en el acto (DT6b-12). El reloj, el HUD, el guardado y la sesión de escena no se tocan |
| `DegradeWater` de la adaptación | `ReportThermal()` → igual que una falla (fundido y una línea con el motivo "adaptación térmica") |
| `LowerReflection` | Alto: el peso del planar baja a 0 en 0,6 s (`_RmReflectionMix`) y queda la de Medio. Medio: la banda y el perfil bajan a 0 (queda el cielo) |
| `LowerTier` | El material avanzado cambia de variante (palabras clave del material), el vidrio pasa al respaldo en 200 ms si llega a Bajo y la malla de Bajo se usa en la próxima sesión de escena (no se rehace a la vista) |

Ningún cuadro del cambio pasa de 33 ms: el fundido no reserva ni compila nada (las variantes ya están calentadas, salvo un `LowerTier` antes del calentamiento en reposo, que queda en el registro).

---

## 6. Presupuestos y medición

| Recurso | Tope | Cómo se mide |
|---|---|---|
| Cuadro (build, W1280, Alto y Medio, 4 escenarios × 12 y 22 h libre con `Fuerte`; regata `E-180`; diálogo) | p99 ≤ 16,7 ms; ningún cuadro > 33 ms; 0 B/cuadro | `build.ps1 -PerfWater` (N) con `-rowmate-perf-scene`, `-rowmate-perf-env`, `-rowmate-perf-hour` y `-rowmate-quality`; `FrameAllocations` con su control positivo; Profiler del build de desarrollo (`-rowmate-perf-profile`) |
| GPU del desenfoque (Medio, W1280) | ≤ 0,6 ms | `ProfilerRecorder` con `GpuRecorder` sobre `RowMate.HudBlur` → `[PERF] vidrio: GPU p50/p95`. Sin `SystemInfo.supportsGpuRecorder`: NO VERIFICABLE en esa parte |
| GPU del cuadro y de la adaptación | 13 ms (disparo) | `FrameTimingManager` (el de `ThermalAdapter`). `-rowmate-perf-gpu-load <ms>` suma un pase de carga (`RowMate/Dev/GpuLoad`, solo en desarrollo) para ver los pasos en el registro (AC-6B-31) |
| Draw calls / SetPass / triángulos (Editor, 4 escenarios × 3 niveles × 12 y 22 h, avanzada, con el maniquí) | ≤ 150 / 50 (Alto 80) / 600 k, 350 k, 180 k | `SceneBudgetTests.AdvancedWaterFitsByTierAndEnvironment` (C, como el de hoy, con el espejo incluido), `SceneAdaptationTests.TheRowerFitsTheMediumBudget` y `EnvironmentBudgetTests` (C), todos con `SceneWater = Auto`. Antes de medir esperan `AdvancedReady` y afirman `Showing == Advanced` (G6b-16), y con la estela y una columna en el cuadro (`Fuerte`; a las 22 h). Cuenta y mitigación en §6.1 |
| SetPass del vidrio | ≤ 3 (bajada, Kawase y composición), aparte del tope de la escena | `HudGlassTests.BlurPassCount`. La escena se mide con el panel escondido (B6a-23), así que el desenfoque no corre ahí, como pasa con la UI |
| CPU de la escena | ≤ 1,5 ms p95 (`RowMate.Scene.Update`) | Ídem. El espejo se informa aparte (`RowMate.Scene.Reflection`) |
| Malla del agua | ≤ 20 k triángulos | `WaterRingMeshTests` [dotnet] y la malla viva [PlayMode] |
| Fragmento | §4.1; `Foam` ≤ 30; degradada ≤ 28 | `SceneShaderTests` (D3D11, por palabra clave de nivel) |
| Memoria | ± 5 % después de 10 vueltas; ≤ 1,5 GB en escritorio; contenido ≤ 1 MB (Medio); RT ≤ 64/48/180 MB | `SceneMemoryTests.TenEnvironmentRoundsReturnWithinFivePercent` (N) y `SceneBudgetTests` (topes de hoy); `[PERF] memoria` en el build |
| HUD | ≤ 60 elementos (hoy 56-57), 0 nuevos | `HudBudgetTests` sin cambios |

**Cómo se mide en esta PC.** Cada línea `[PERF]` empieza con `pantalla <ancho>×<alto> @ <Hz>`. Sale de `Screen.currentResolution` (`refreshRateRatio`) y del tamaño de la ventana. Si la pantalla está en el modo de respaldo, se mide igual y se anota. `build.ps1 -PerfEarly/-PerfWater` y `run-unity-tests.ps1 -LogLoad` (N) toman `Get-Counter '\Processor(_Total)\% Processor Time'` y el porcentaje de los otros procesos antes y durante la corrida. Lo guardan en `<scratch>/it6b/perf/`, y en los documentos va solo el porcentaje, sin nombres de procesos, de equipo ni de usuario. Ningún script toca, pausa ni mata otros procesos. Un tope de CPU que falla con carga externa anotada se informa como "no concluyente por carga", **sin** subir el tope, y se repite cuando baje la carga. Android: NO VERIFICABLE (AC-6B-32, #7).

### 6.1 Cuenta de SetPass por nivel, escenario y hora (G6b-02)

"Hoy" es el máximo más frecuente de `[SBT]`/`[EBT]` en las corridas de PlayMode de la It. 15 (Editor, maniquí, agua degradada; lago `E-920`, río `E-250`, costa `E-500`, regata `E-180`). En 2 de ≈ 220 líneas `[SBT]` de registros guardados, el lago en Medio midió 52-54. En el primer paso de (2) se busca reproducirlo y se explica; no se tolera.

| Escenario · hora | Bajo: hoy → 6b | Medio: hoy → 6b (tope 50) | Alto: hoy → 6b (tope 80) |
|---|---|---|---|
| Lago 12 h | 35 → 35 | **50 → 50** | 65 → 65 + espejo |
| Lago 22 h | 37 → 37 | **50 → 50** | 66 → 66 + espejo |
| Río 12 / 22 h | 16 / 20 → igual | 23 / 25 → igual | 24 / 27 → + espejo |
| Costa 12 / 22 h | 18 / 22 → igual | 25 / 27 → igual | 26 / 29 → + espejo |
| Regata 12 / 22 h | 16 / 18 → igual | 23 / 23 → igual | 24 / 25 → + espejo |

De dónde sale "→ igual", por cada cosa que suma la 6b:

| Qué | SetPass | Por qué |
|---|---|---|
| `WaterAdvanced` en lugar de `WaterDegraded` | 0 | Es un pase `UniversalForward`, igual que la degradada, y la reemplaza (no se dibujan las dos, tampoco en el fundido: §5) |
| Estela | 0 | `WaterMarks` tipo 5, el mismo material y la misma variante, en la cola 3050 junto al lote de marcas (DT6b-23). Sin el cambio, sería +1 en todos los niveles |
| Columnas de luz (la cabaña del lago a las 22 h) | 0 | `WaterMarks` tipo 4 en el mismo lote. Con el `Glow` tipo 3 del arte, el lago a las 22 h sumaría 1 SetPass, porque hoy no tiene lote `Glow` (08: "0 en el lago") |
| Micro-anillos y charco calmo | 0 | Son ramas por tipo de `WaterMarks` |
| Perfil, banda de orilla, mapa de normales y popa | 0 | Son texturas y uniformes del mismo material, más el `Hull` de hoy |
| Espejo (solo Alto) | **a medir** | Un pedido de URP sin post, sombras, agua, partículas ni instancias chicas. Se estima en ≈ 15-20 SetPass, o sea 80-86 en el lago (estimado, **sin medir**). Si pasa de 80, se aplica el recorte del arte §4.2 (§11). Río, costa y regata quedan muy por debajo |
| Vidrio | aparte | Fila "SetPass del vidrio" de §6 |

Medio en el lago queda en **50 con 0 de margen**. Por eso el **primer paso de (2)** es medirlo, antes de seguir con la avanzada: Medio, lago, 12 y 22 h, con el maniquí y con el *slot*, y la estela, una columna y tres charcos en el cuadro, con `SceneAdaptationTests` y `EnvironmentBudgetTests` (y dos corridas, con la carga anotada). Si da más de 50, se sigue este orden **sin subir el tope**: (i) Frame Debugger del Editor, la lista de cambios de pase, y juntar dos dibujos del mismo *shader* separados por el orden de cola (darles la misma cola o el mismo material). (ii) Si no alcanza, el pase extra que aparezca se lleva a un material que ya se dibuje, como se hizo con la estela. (iii) Si tampoco alcanza, AC-6B-29 queda FAIL documentado en Medio-lago, con su issue, y no se relaja la prueba. Cada paso se anota en "Cambios por implementación".

---

## 7. Árbol de archivos por paquete (disjuntos)

**WP-A — puro (dotnet, sin Unity).**
`Core/Scene/Water/{WaterModel, WaterProfile, WaterProfiles, WaterModeSelector, WaterAvailability, WaterFade, WaterFallbackLog, WavePhases, WakeTrail, MicroRingField, LightColumns, CoastBuoySwell, CrestFoam, SwirlOrientation, WaterFrame}.cs` N · `Core/Scene/Geometry/{WaterRingMesh, WaterNormalMap}.cs` N · `Core/Scene/HorizonProfile.cs` N · `Core/Scene/Geometry/PropMeshes.cs` C (`HullMesh`) · `Core/Scene/{SceneFrame, SceneSession, ThermalAdaptation, SceneWarmupPolicy}.cs` C · `Core/Scene/Art/{WaterArt, ShaderArt, LakeArt, SkyArt}.cs` C · `Core/Scene/Art/WaterLookArt.cs` N · `Core/Design/GlassTint.cs` N · `Tests/EditMode/Scene/Water/*Tests.cs` N · `Tests/EditMode/Scene/{SceneWarmupPolicyTests, ThermalAdaptationTests, LakeArtTests, SkyArtTests, WaterShaderContractTests}.cs` C/N · `Tests/EditMode/Design/GlassTintTests.cs` N · `Tests/EditMode/Traceability/It06bIds.cs` N · `Tests/EditMode/Project/ItSixBToolingTests.cs` N (argumentos solo de desarrollo, `capture-snapshots.ps1` escribe en la 6b, la huella `water.json` y, por texto, `SnapshotCli` con la degradada fija en `it2` a `it15` y `thumbs`, DT6b-24).

**WP-B — Unity.**
`Scene/Water/{WaterSurfaceController, AdvancedWaterSurface, PlanarReflection, WakeRenderer}.cs` N · `Scene/Water/{WaterInteractionRenderer, MarkerBuoy}.cs` C · `Scene/HudBlurPass.cs` N · `Scene/{SceneHost, SceneWorldBuilder, SceneDirector, SceneShaderIds, SceneShaderWarmup, SceneRenderer, HorizonRenderer (WaterSurface queda como la degradada)}.cs` C · `Scene/Environments/FoamRenderer.cs` C (`GlowRenderer` sin cambios, DT6b-23) · `Scene/Quality/{QualityTierProfile, SceneQualityApplier}.cs` C (reflejo, desenfoque y variante de agua por nivel) · `Presentation/Workout/GlassSurface.cs` N · `Presentation/Workout/{IImmersiveScene, ImmersiveWorkoutPage, SceneSurface, HudCard, HudBand, FreeTopBar, StepInfoBar, HudControls, CenterCallouts, SeriesPanelView, CompletionDialog}.cs` C · `Presentation/Device/DeviceScreenView.cs` C · `Presentation/UiContext.cs` C · `Bootstrap/{AppRoot, SceneBridge}.cs` C · `Art/Shaders/Dev/GpuLoad.shader` N (solo desarrollo; fuera de la colección de la app) · `Editor/Bootstrap/SceneMaterialsBuilder.cs` C (materiales `WaterAdvanced` × nivel y `HudBlur`; colección de variantes) · `Editor/Bootstrap/WaterNormalBaker.cs` N (hornea `Art/Textures/Water/water-normal.png` + `water.json` y asigna el slot) · `Snapshots/WaterCatalog.cs` N · `Snapshots/{SnapshotCatalog, SnapshotCli, SnapshotScenario, SnapshotManifest, SceneMarks, EnvironmentMarks, RowerCatalog}.cs` C (sondas de §8; `SnapshotScenario.Water` y el agua fija por set, DT6b-24; CAP-04 de la 7) · `Tests/UiSupport/UiHostOptions.cs` C (`SceneWater`, por defecto `Degraded`) · `tools/unity/{capture-snapshots.ps1, run-unity-tests.ps1}` C · `tools/build/{build.ps1, perf-log-check.ps1}` C (`-PerfEarly`, `-PerfWater`; `-BleSmoke` sin tocar) · `Tests/PlayMode/Scene/{EarlyEntryTests, AdvancedWaterTests, WaterFallbackTests, ReflectionTierTests, WakeRendererTests, SceneMemoryTests}.cs` N · `Tests/PlayMode/{HudGlassTests, CompletionGlassTests, DeviceCoverTests}.cs` N · `Tests/PlayMode/Scene/{SceneBudgetTests (también `SceneAdaptationTests`), SceneLifecycleTests, EnvironmentBudgetTests}.cs` C (`SceneWater = Auto`, `AdvancedReady` y `Showing == Advanced`) · `Tests/PlayMode/Scene/Shaders/{WaterNoTagTest, WaterUnsupportedTest}.shader` N (solo de prueba, G6b-01) · `Tests/UnityEditMode/SceneShaderTests.cs` C · `docs/migration/architecture/adr/027-agua-avanzada-y-vidrio.md` N y `ARCHITECTURE.md` C ("Cambios de la It. 6b") · capturas (§8).

**WP-D — Designer.**
`Art/Shaders/Include/RowMateWater.hlsl` N (la ola, `RmSwellHeight`, el degradado compartido con `n ≤ 9`) · `Art/Shaders/Scene/WaterAdvanced.shader` N (sin `Fallback`, tag `RmWater`, DT6b-22) · `Art/Shaders/Scene/{WaterDegraded, WaterMarks, Foam, Hull}.shader` C (`WaterMarks`: mezcla premultiplicada y tipos 4 y 5, DT6b-23) · `Art/Shaders/UI/HudBlur.shader` N · `Presentation/Styles/{Base, Skin}.uss` C (`.rm-dialog--glass`, el borde del vidrio y el reflejo interior por clase, sin elementos) · después de WP-A: los valores de `Core/Scene/Art/{WaterLookArt, LakeArt, SkyArt}.cs` (ronda de ajuste) · `docs/migration/design/{ART_BIBLE, ASSET_SLOTS}.md` C · `06b-agua-avanzada/04-design-notes.md` N y `snapshots/INDEX.md`.

`ProjectSettings/` no cambia y el define del simulador nunca queda ahí. Si el *bootstrapper* reescribe `HorizonFar.mat` o `HorizonNear.mat`, se revierten. `THIRD_PARTY_NOTICES.md` no cambia (todo es propio).

---

## 8. Capturas (set `it6b`)

`WaterCatalog` → `docs/migration/iterations/06b-agua-avanzada/snapshots`. `capture-snapshots.ps1` suma `it6b` al `ValidateSet` y a `Get-SetFolder` (el valor por defecto pasa a `it6b`), y `ItSixBToolingTests` comprueba la carpeta. Datos con `ROWMATE_DATA_DIR` temporal y `F-5`. Escena con el HUD salvo "sola"; `Fuerte`; distancias de referencia (lago `E-920`, río `E-250`, costa `E-500`, regata `E-180`); Alto salvo donde se indica. Las sondas de escena se leen sobre la RT de la escena del mismo cuadro, sin la UI. **Dos corridas**: las mismas sondas y la misma lista de archivos.

**El agua de cada toma** (DT6b-24, G6b-03): `SnapshotScenario.Water` (`WaterForce?`). Si es `null`, vale lo del set: `SnapshotCli.WaterFor(set)` da `Degraded` en `it2` a `it15` y `thumbs`, y `Auto` solo en `it6b`. Lo explícito gana: CAP-21 a -24 de `it6b` con `Degraded`, y la CAP-04 de `it7` con `Auto` (AC-6B-17). Con `Auto`, la toma espera `AdvancedReady` y afirma `Showing == Advanced`. El manifiesto anota `water: <Showing>` en cada toma, y una toma que no tiene el agua pedida **falla**. Las tomas con distancia forzada precargan la estela (12 s a la velocidad estable, `WakeTrail.Prime`, G6b-15).

**Niebla y costura** (AC-6B-15 corregido, DT6b-26, G6b-04):

- `fogRow` (en W1280: CAP-28, -30 y -31). La fila de z ≈ 300 m (720·2,2/300 = 5,28 dp debajo del horizonte; fuera de la costura de 3 dp) se mide sobre dos pasadas de prueba del mismo cuadro, sin post y con la bruma baja apagada (`_RmWaterHazeScale` = 0): `c` con la niebla y `c₀` con `_RmWaterFogScale` = 0 (globales solo de prueba, en `UNITY_EDITOR`). En cada columna, `f = |c − c₀| / |fog − c₀|` en el canal con mayor `|fog − c₀|`. Se usan las columnas con ese valor ≥ 20/255. La mediana tiene que dar `WaterModel.FogWeight(z de la fila)` ± 0,05 (0,77 a 300 m). La z de la fila sale de `SceneCamera` para W1280. Los 400 m (peso 1, el agua igual a `fog` antes de la costura) se prueban en [dotnet] con `WaterModelTests.FogReachesFogColorAt400`.
- `seam` (CAP-01 a -16). Las filas y las columnas son las del arte §2.8: la última fila entera de agua y la primera entera de cielo, solo en las columnas donde `HorizonProfile` da 0 (ninguna silueta a menos de 1 dp sobre el horizonte), y nunca sobre una silueta. Tope: ≤ 6/255 por canal en el p95 de esas columnas. Si una toma tiene menos de 32 columnas libres, la costura se mide además en una pasada de prueba con `HorizonRenderer` y la orilla lejana apagados, que deja el agua contra el cielo y la bruma en todo el ancho.

| Toma | Qué | Sondas (manifiesto) |
|---|---|---|
| CAP-01 a -04 | Lago TV 6/12/18/22 h | `seam` ≤ 6/255; columna más brillante (20-80 m) a ± 40 dp del astro (6, 12 y 18 h); 22 h más tenue que 12 h; CAP-02: orilla reflejada ≥ 10 % más oscura; CAP-03: halo ≥ 15 %; CAP-04: columna de la cabaña |
| CAP-05 a -08 | Río TV, ídem | Ídem; CAP-06: fachadas reflejadas ≥ 10 %; CAP-08: columnas de faroles y ventanas |
| CAP-09 a -12 | Costa TV, ídem (con `Series 600`, DT6b-16) | Ídem; CAP-10: espuma ≤ 35 % solo en crestas, boya sobre la ola (± 0,12 m), casco sobre la ola |
| CAP-13 a -16 | Regata TV, ídem | Ídem |
| CAP-17 a -20 | 4 escenarios TH 12 h | CAP-17: silueta ≥ 1,3:1 y ladera ≤ 30 % |
| CAP-21 a -24 | 4 a 12 h, TV, `-rowmate-water degraded` | CAP-21: 10 líneas; CAP-23: franjas de la costa (AC-SCN-12-01); charcos visibles |
| CAP-25 / -26 | Lago TV 12 h Medio / Bajo | Banda de orilla (Medio); solo cielo (Bajo) |
| CAP-27 | TV `Series 600`, dos paneles, vidrio sobre la orilla | Variación dentro de la tarjeta ≤ 25 % de la de sin tarjeta; contraste; reflejo interior fuera de los textos |
| CAP-28 | W1280 río 18 h libre | Vidrio sobre el atardecer; contraste; `fogRow` |
| CAP-29 | TV `Corta`, diálogo a los 36,3 s, toma aérea | Popa proyectada ≥ 10 % más clara que el agua de la misma fila; contraste del texto |
| CAP-30 | W1280 lago `E-42K` 12 h | Estadística del agua (media y desvío por franja) igual a la de CAP-02 en W1280 dentro del ruido de dos tomas; `fogRow` |
| CAP-31 | W1280 lago 12 h, el cuadro de CAP-10 de la 15 | Popa con ≥ 8 % de variación de punta a punta (y ≤ 0,82 de media, informativa); reflejo del casco debajo; `fogRow` |
| CAP-32 | TH lago, escena sola, fase forzada, charcos de 1,0 s | Hervidero, charcos y salpicadura ≥ 1,15:1; remolinos a 40° ± 5° (proyectados a la pantalla) |
| VID-01 | W1280 lago cruzando 4096 m | Pico de diferencia entre cuadros ≤ 1,5 × la mediana |
| VID-02 | TV avanzada → degradada (`-rowmate-water-fail`) y la avanzada al volver a arrancar | Ningún cuadro > 33 ms; una línea en el registro; fundido de 0,6 s |
| VID-03 | TV estela: arrancar, `Fuerte` y pausa | Sin estela quieto; a pleno ≥ 4 m/s; se apaga con la inercia |
| VID-04 | TV `Hora escena` 18 → 22 h en el río | Columnas que se encienden sin saltos |
| VID-05 | TH costa con `Series 600` | Mar de fondo, casco y boya en fase |

También se regenera `it6a` entero, **con la degradada fija**, con la sonda `lines` (AC-6B-03, dos corridas), y la CAP-04 de `it7` con la avanzada y el charco proyectado (`-Set it7 -Filter CAP-04`, AC-6B-17). Las demás tomas de `it6a` cambian solo por los arreglos de la deuda (la fila de abajo de #2, las montañas y el halo de #3, la popa de #85), nunca por el agua avanzada. Así la evidencia de la 6a, la 7 y la 8 no cambia de agua (AC-6B-35). Ninguna corrida escribe en otra carpeta: se mira con `git status`.

---

## 9. Paquetes, orden y comandos

| Paquete | Depende de | Cierre |
|---|---|---|
| **WP-A** | — | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it6b` en verde (también `-Matrix`). `WaterNormalMap` da los mismos bytes dos veces |
| **WP-D (shaders)** | Los nombres de `ShaderArt` de §3.4 (este diseño) | Los `.shader` y el `.hlsl` compilan en el Editor (lo corre WP-B). `04-design-notes` con el reparto de ALU |
| **WP-B** | WP-A y WP-D | El orden de abajo. EditMode y PlayMode, normal y `-Simulator`. Builds normal, simulador y desarrollo con `-PerfEarly` y `-PerfWater`. Los sets `it6b` (×2), `it6a` (×2) y la CAP-04 de `it7` |
| **WP-D (ajuste)** | WP-B (las capturas) | Los valores de `WaterLookArt`, `LakeArt` y `SkyArt` hasta que pasen las sondas, sin tocar topes. `INDEX.md` con la comparación contra LEG de la 6a y de la 8 |

**Orden dentro de WP-B**: (1) **deuda**: los tests de §2 en rojo, después el preámbulo, la clase de unidad, el negro, `DeviceScreenView` y las 10 líneas. `-PerfEarly` se corre **antes** de seguir; si una unidad `Light` mide más de 4 ms, se parte y se anota. En (1) también va `PlayerLoopDispatchesUiBeforeLateUpdate` (§2.1, punto 6). (2) El agua avanzada en los 4 escenarios: **primero la cuenta de SetPass de Medio en el lago** (§6.1, con la estela y la columna como tipos de `WaterMarks`), después Medio en los demás, Lite y Alto con el espejo. (3) La disponibilidad y la caída, empezando por `UnsupportedSubShaderIsDetected`. (4) El vidrio. (5) Los presupuestos en el Editor. (6) Los builds y `[PERF]`. (7) Las capturas (con el agua fija por set, DT6b-24), siempre al final, con una sola instancia de Unity y sin editar fuentes mientras corre.

Comandos: `tools/unity/run-unity-tests.ps1 [-Platform PlayMode] [-Simulator] [-Filter <regex>] [-LogLoad]`; `tools/unity/capture-snapshots.ps1 -Set it6b`; `tools/build/build.ps1 -Target Windows [-Simulator] [-PerfEarly] [-PerfWater]`. Builds solo bajo `Builds/Windows/` (si la carpeta está en uso, se reintenta cada 2 min, hasta 20 veces). Si Unity sale con 198, se abre el Hub y se reintenta.

---

## 10. Matriz AC → test

| AC | Test (paquete) | Marca |
|---|---|---|
| 01 | `SceneWarmupPolicyTests.{HeavyOnlyWhenQuiet, HeavyNeedsPreviousQuietFrameAndNoInput, PreambleBeforeWorld, VisibleUnitsUnderFourMs}` (A) · `EarlyEntryTests.*` (B) · `build.ps1 -PerfEarly`: 5 lago + 1 × 3 escenarios | dotnet · Editor · Device-Win |
| 02 | `EarlyEntryTests.{PlayerLoopDispatchesUiBeforeLateUpdate, HeavyNeverSharesTheTapFrame}` (B) · `-PerfEarly`: el toque en 5/5 ≤ 16,7 ms, mínimo y máximo y la carga en `perf-log-check.ps1` | Editor · Device-Win |
| 03 | `WaterShaderContractTests.DegradedClampsToTenLines` (A) · `ItSixBToolingTests.OldSetsDrawDegradedWater` (A) · `SceneShaderTests` degradada ≤ 28 (B) · sonda `lines` en `it6a` (con la degradada fija) y CAP-21 | dotnet · Editor |
| 04 | `LakeArtTests`, `SkyArtTests` (A) · sondas CAP-03, CAP-17 (B) | dotnet · Editor |
| 05 | `DeviceCoverTests.{NoRenderUnderImmersive, FirstVisibleFrameShowsLastReading}` (B) | Editor |
| 06 | `WaterModeSelectorTests` (una por fila) (A) · `ItSixBToolingTests.WaterArgumentsAreDevelopmentOnly` (A) · `WaterFallbackTests.UnsupportedSubShaderIsDetected` (B: `WaterNoTagTest` con `Fallback` a la degradada y sin el tag, y `WaterUnsupportedTest` con un SubShader que no corre en URP; los dos caen a la degradada con una línea) · `SceneShaderTests.WaterAdvancedHasNoFallbackAndIsTagged` (B) | dotnet · Editor |
| 07 | `WaterFadeTests` (A) · `WaterFallbackTests.AdvancedArrivesWithFadeKeepingTheScene` (B, con `DelayAdvancedWaterForTest`) · VID-02 | dotnet · Editor |
| 08 | `WaterAvailabilityTests.{StickyNoRetry, OneLogLine}` (A) · `WaterFallbackTests.{UnsupportedSubShaderIsDetected, FailureFadesToDegraded, NoFrameOver33, SavedSessionIdentical, NoUserVisibleError}` (B) · `-rowmate-water-fail` en el build | dotnet · Editor · Device-Win |
| 09 | CAP-21 a -24 con sus sondas · las pruebas de AC-SCN-13-02 a -04 y AC-SCN-12-01 (`EnvironmentSceneTests`, `RowerSceneTests` y las demás de antes) con `UiHostOptions.SceneWater = Degraded`, el valor por defecto (DT6b-24), y su afirmación `Showing == Degraded` (B) | Editor |
| 10 | `WaterModelTests` (los números de la spec) (A) · `WaterInputsTests` (LT-124, sin cambios) | dotnet |
| 11 | `WaterProfilesTests` (A) · sonda de variación cercana en CAP-02/-06/-10/-14 (B) · `CoastSwellTests` (sin cambios) | dotnet · Editor |
| 12 | `ReflectionTierTests.{HighDrawsBoatAndRower, MediumBand, LowSkyOnly}` (B) · sondas CAP-02/-06, CAP-25/-26, CAP-31 | Editor |
| 13 | `WaterModelTests.ColumnFollowsAstroAndFades` (A) · sonda de la columna en CAP-01 a -16 | dotnet · Editor |
| 14 | `WakeTrailTests.{IntensityFromSpeed, PersistsTwelveSecondsAndOpens, SurgeBeats, FadesWithInertia}` (A) · `WakeRendererTests` (B) · VID-03 | dotnet · Editor |
| 15 | `WaterModelTests.{FogWeightCurve, FogReachesFogColorAt400}` (A) · sonda `seam` en CAP-01 a -16 y `fogRow` en CAP-28, -30 y -31 (§8) (B) | dotnet · Editor |
| 16 | `MicroRingFieldTests`, `PuddleFieldTests` (A) · `AdvancedWaterTests.MarksByTier` (B) · CAP-32 | dotnet · Editor |
| 17 | `SwirlOrientationTests` (A) · sondas CAP-32 y CAP-04 de `it7` (B) | dotnet · Editor |
| 18 | `LightColumnsTests.{NightOnly, FollowsLightLevel, NearestWindows}` (A) · sondas CAP-04/-08/-12/-16 · VID-04 | dotnet · Editor |
| 19 | `CrestFoamTests`, `CoastBuoySwellTests` (A) · CAP-10 · VID-05 | dotnet · Editor |
| 20 | `WavePhasesTests.{NoJumpAt4096, TapeMatchesWorld}` (A) · VID-01 · CAP-30 contra CAP-02 (B) | dotnet · Editor |
| 21 | `HullMeshTests.{SternTaperAndGradient, ContractUnchanged}` (A) · sonda CAP-31 (B) | dotnet · Editor |
| 22 | `SceneLifecycleTests` (los 16 casos con la avanzada y el cierre mientras se prepara) (B) | Editor |
| 23 | `GlassTintTests.Alpha` (A) · `HudGlassTests.{EveryGlassPieceShowsTheBlur, ReflexNeverUnderText}` (B) · CAP-27/-28 | dotnet · Editor |
| 24 | `GlassTintTests.{CompositeIsMonotonic, ContrastWorstCase}` (A) · sonda de contraste en las tomas con vidrio (B) | dotnet · Editor |
| 25 | `HudGlassTests.{LowUsesFallback, TierChangeCrossfades200Ms, BlurIndependentOfWater}` (B) | Editor |
| 26 | `-PerfWater` con `-rowmate-quality medium` (marcador de GPU) · `HudBudgetTests` (sin cambios) · `HudGlassTests.NoAllocationsPerFrame` (B) | Device-Win · Editor |
| 27 | `CompletionGlassTests` (B) · CAP-29 | Editor |
| 28 | `-PerfWater` (la matriz) + `FrameAllocations` (B) | Device-Win |
| 29 | `SceneBudgetTests.AdvancedWaterFitsByTierAndEnvironment`, `SceneAdaptationTests.TheRowerFitsTheMediumBudget` y `EnvironmentBudgetTests` con la avanzada (§6.1), `SceneShaderTests.{WaterAdvancedFitsItsBudget, WaterMarksTypesFitTheBranchBudget, FoamBackToThirty}` (B) · `WaterRingMeshTests` (A) | Editor · dotnet |
| 30 | `SceneMemoryTests.TenEnvironmentRoundsReturnWithinFivePercent` (B) · `[PERF] memoria` | Editor · Device-Win |
| 31 | `ThermalAdaptationTests.OrderEndsInDegradedWater` (A) · `-rowmate-perf-gpu-load` en el build | dotnet · Device-Win |
| 32 | — | Device-Android **NO VERIFICABLE** (#7) |
| 33 | Set `it6b` ×2 con el manifiesto (B) · `INDEX.md` (D) | Editor |
| 34 | `AdvancedWaterTests.HourChangeWithoutJumps` (B) · VID-04 | Editor |
| 35 | dotnet (`-Matrix`), EditMode y PlayMode, normal y `-Simulator` · los sets viejos con la degradada fija (DT6b-24) | dotnet · Editor |
| 36 | Lo [dotnet] de arriba también corre en EditMode (mismo ensamblado de pruebas) | dotnet · Editor |
| 37 | `SceneSlotsTests` (los *slots* `water.*` en Procedural, sin `[SLOT]`) · `ItSixBToolingTests.WaterNormalFootprint` (A) · `THIRD_PARTY_NOTICES.md` sin cambios | Editor · dotnet |
| 38 | `tools/unity/ProjectSettingsGuard.ps1`, `git status` (incluidos los dos `.mat`), `git -C ../rowmate-legacy status --porcelain` vacío y búsqueda de rutas de la PC (B y QA) | Editor |

---

## 11. Riesgos

| Riesgo | Mitigación |
|---|---|
| `GL.invertCulling` no se respeta dentro de `SubmitRenderRequest` | Plan B: cámara espejada (posición `(x, −y, z)` y cabeceo invertido, sin matriz de reflexión) y el shader lee la RT con `v` invertida. Lo prueba `ReflectionTierTests` en el primer paso de (2) |
| Medio en el lago queda en 50 con 0 de margen (G6b-02) | La estela y las columnas no suman pase (DT6b-23). Se mide primero en (2) y, si pasa, se sigue el orden de §6.1. El tope no se sube |
| El SetPass de Alto con el espejo pasa de 80 | El recorte del arte §4.2: primero los pinos a más de 150 m fuera del espejo, después los objetos LOD1 a más de 300 m. El tope no se sube |
| El mapa de normales no entra en 1 MB de contenido (Medio) | DT6b-17: 512² comprimido; si no, 256² |
| El JIT y la primera vez no se reproducen en el Editor | La medida real es `-PerfEarly` en el build. El Editor solo prueba la política (qué corre dónde) y la cota de 4 ms |
| Una unidad `Light` mide más de 4 ms en el build | Se parte (los sub-dibujos son por grupo) y se anota en "Cambios por implementación". Si no se puede partir, pasa a `Heavy` (y va al negro o al reposo) |
| La GPU no informa tiempos (D3D11 en batchmode) | Se mide en el build de desarrollo. Si `supportsGpuRecorder` es falso, esa parte queda NO VERIFICABLE, sin inventar números |
| Carga externa (la VM del usuario) en las pruebas de CPU | Se anota con `-LogLoad`. Sin carga, se repite. El tope no se relaja y no se toca ningún proceso |
| `background-image` con RT en UI Toolkit: la RT se recrea al cambiar el tamaño | `GlassChanged` vuelve a asignar el fondo una vez. Sin `MarkDirtyRepaint` por cuadro (el panel muestrea la RT viva, como `scene-surface`) |
| `SeriesPanelView` con vidrio enmascarado | `Allocate` con textura y UV de pantalla, en el mismo `generateVisualContent`. 0 elementos |
| `LowerTier` antes de calentar las variantes de abajo | Un tirón único en un evento térmico, que queda en el registro. Se calientan en reposo apenas termina la fase 10 |
| B0 desplaza el agua de la costa ± 0,15 m | DT6b-21: todo lo apoyado sube con la misma fórmula; sonda en CAP-10 |
| El color del panel gamma | `_RmHudGlass` en el mismo formato y espacio que la RT de la escena (como `UiColorPipeline` la muestrea hoy) |
| El *bootstrapper* reescribe los `.mat` del horizonte | Se revierten antes de cerrar (AC-6B-38) |
| Sin módulo de Android: no se compilan los shaders de GLES ni de Vulkan acá | Los ALU se miden en D3D11 (la regla de `ShaderArt`). Android queda NO VERIFICABLE |

---

## 12. Desvíos propuestos

| ID | Spec | Propuesta | Si se rechaza |
|---|---|---|---|
| D-01 | AC-6B-29: "SetPass ≤ 50" | Por nivel, como hoy: Alto ≤ 80 (ARCHITECTURE §20.1 desde la 6a, medido 61 sin espejo); Medio y Bajo ≤ 50 | Alto sin espejo (el planar pasa a issues) |
| D-02 | AC-6B-23: "a 1/4 de resolución" | DT6b-11 (½ por eje en escritorio, ¼ por eje en teléfono) | ¼ por eje en todos (radio de ≈ 20 dp en W1280) |
| D-03 | AC-6B-01: el negro de la entrada | Puede durar lo que tarde el trabajo pesado pendiente (solo en una entrada antes de que termine el preámbulo), con el reloj de la entrada corrido | Esas unidades esperan al reposo y la escena llega más tarde |

---

## 13. Para issues

- Vaivén de 0,02 m de las boyas de carril de río y regata (PA6b-20; van horneadas por segmento).
- Reflejo del bote y del remero en Medio (PA6b-08; spec §9).
- Volver al agua avanzada sin reiniciar cuando el equipo se enfría (spec §9).
- Fresnel de Schlick como opción de arte, con medida (spec §9).
- Reflejo de nubes y estrellas en Medio; ventanas de fachadas reflejadas en Medio; textura de interacción; mar con FFT en Alto de escritorio (arte, "Para issues").
- Ola de proa de 0,05 m cuando haya cámaras alternativas (spec §9).

---

## 14. Cambios por gate

Gate: [03-qa-gate](03-qa-gate.md) (aprobado con observaciones). Se corrigen los 5 mayores. De paso se resuelven 4 menores que tocan lo mismo: G6b-10 (`Glow` no cambia y `WaterMarks` tiene tope), G6b-14, G6b-15 y G6b-16. Los demás menores se resuelven en la implementación o los resuelve su responsable.

| Issue | Qué cambió | Dónde |
|---|---|---|
| G6b-01 | `WaterAdvanced` sin `Fallback` y con el tag `RmWater`=`Advanced`. El soporte es `Shader.isSupported` **y** el tag del SubShader activo, así que un equipo sin soporte cae a la degradada con su línea (AC-6B-06, -08). Hay 2 *shaders* de prueba y `WaterFallbackTests.UnsupportedSubShaderIsDetected`. R-04 se cumple con el par de *renderers* y se anota en ARCHITECTURE | DT6b-22, §4.1, §5, §7, §10 (06, 08) |
| G6b-02 | Cuenta de SetPass por nivel × escenario × hora, con los números de la It. 15. La estela y las columnas pasan a ser tipos de `WaterMarks` (el mismo material y variante, cola 3050, mezcla premultiplicada): 0 pases nuevos y Medio-lago queda en 50. No hay `WaterWake.shader` y `Glow` no cambia. Se mide primero en (2), con un orden de mitigación que nunca sube el tope | DT6b-23, §1 (PA6b-09, -11), §3.4, §4.3, §6, §6.1, §7, §9, §11 |
| G6b-03 | Lo viejo se queda con la degradada: `UiHostOptions.SceneWater` por defecto `Degraded`, `SnapshotCli.WaterFor(set)` da `Degraded` en `it2` a `it15` y `thumbs`, y `SnapshotScenario.Water` para lo explícito (`it6b` y la CAP-04 de `it7`). El manifiesto anota el agua y una toma con otra agua falla. La prueba es `ItSixBToolingTests.OldSetsDrawDegradedWater` | DT6b-24, §2.2, §7, §8, §10 (03, 09, 35) |
| G6b-04 (PO; el diseño acompaña) | La niebla a 400 m va en [dotnet]. En captura, el peso de la niebla en la fila de z ≈ 300 m en W1280 (0,77 ± 0,05, con dos pasadas de prueba). La costura se mide solo en las columnas sin silueta, con las filas del arte §2.8 y, si faltan columnas, una pasada sin siluetas. Spec AC-6B-15 y arte §2.8/§12 corregidos, con su motivo | DT6b-26, §8, §10 (15); [01-po-spec](01-po-spec.md) AC-6B-15; [00-arte-agua](00-arte-agua.md) §2.8 y §12 |
| G6b-05 | Se escribe el orden del *player loop*: el despacho de UI Toolkit está en `PreLateUpdate.UIElementsUpdatePanels`, antes de `LateUpdate`, y se comprueba con una prueba. La regla no depende de él: una `Heavy` exige el cuadro anterior quieto y ninguna entrada del Input System en curso (`WarmupFrame`). Se suman `HeavyNeedsPreviousQuietFrameAndNoInput` y `EarlyEntryTests.HeavyNeverSharesTheTapFrame` | DT6b-25, §2.1 (2) y (6), §9, §10 (01, 02) |
| G6b-14 | El degradado se apaga en el mismo cuadro en que se prende el avanzado | §5 |
| G6b-15 | `WakeTrail.Prime` para las tomas con distancia forzada | §3.3, §8 |
| G6b-16 | Las pruebas de presupuesto esperan `AdvancedReady` y afirman `Showing == Advanced` | §6 |

---

## 15. Cambios por implementación (WP-A)

WP-A cerrado con dotnet (`run-tests.ps1`, ver el resultado en la respuesta del paquete). Lo que cambió respecto de §3 y lo que WP-B y WP-D tienen que saber:

| ID | Qué | Por qué / quién sigue |
|---|---|---|
| IA6b-01 | `GlassTint`: L es la luminancia del fondo **como la ve el panel** (el valor sRGB 0-1) y el compuesto se mezcla en sRGB, como `Contrast.CompositeSrgb`. Con la mezcla en lineal el respaldo sobre blanco daría 5,4:1 (no 13,5) y en sRGB con L lineal el compuesto no es monótono | WP-D: `HudBlur` calcula L y mezcla en el espacio del panel (la RT gamma) |
| IA6b-02 | El respaldo de hoy sobre blanco da **13,493:1**; el "≥ 13,5" de AC-6B-24 es ese número redondeado. `GlassTintTests.ContrastWorstCase` exige "nunca peor que hoy" y ≥ 13,49 | Desvío menor (QA/PO) |
| IA6b-03 | `LightColumns.Fill(in SceneFrame, QualityTier)`: `NightLights` y `FacadeWindows` son estáticas (no van como parámetros). Las ventanas de un edificio van por columna de ventanas de la cara del río (5), repartidas a lo largo de `BuildingDepth` y centradas en su z (aprox.). La torre lleva 0,5 × 8 m al 20 % (el arte no da números) | WP-D ajusta los números en `WaterLookArt` |
| IA6b-04 | `MicroRingField.Step(dt, StrokeSpray, StrokeEvents)` (sin la distancia: los anillos van en mundo). Se llama antes de `StrokeSpray.Step` y predice qué gotas tocan el agua en el paso; el ataque suma 2 por pala en la corona recién nacida | — |
| IA6b-05 | `WakeTrail.Step(now, distance, hullSpeed, sternOffset)`: la popa que se dibuja con el surge; la velocidad del casco es `v̄·(1 + A·g(φ))` (la derivada del surge de la 7). `SceneSession.PrimeWakeForTest(v)` para las tomas con distancia forzada (G6b-15) | WP-B: `WakeRenderer` lee `frame.Water.WakePacked` |
| IA6b-06 | `HorizonProfile` es una instancia (la forma del escenario se arma una vez; `Fill(distance, r8)` sin asignar) más la firma estática del diseño. Codificación: byte = 255·mín(v/2; 1), v en fracciones de tan Θ × k(d) | WP-D decodifica × 2 × tan Θ |
| IA6b-07 | `WaterNormalMap` en enteros (tabla de senos en punto fijo): los bytes salen iguales en .NET 8 y en Mono. `water.json` lleva `seed`, `size` y `sha256` de los bytes RGBA | WP-B: `WaterNormalBaker` escribe esos tres campos |
| IA6b-08 | `WaterRingMesh`: anillos de **4 celdas** de ancho que duplican el paso (Alto y Medio 18 120 triángulos en 9 anillos; Bajo 6 888 en 8). Con anillos que duplican también el tamaño no entra en 20 k. Juntas en T entre anillos (agua plana) | — |
| IA6b-09 | `SceneWarmupPolicy`: además de lo de §2.1, `WarmupFrame.Next(quiet, input)` arma el cuadro siguiente (el anterior cuenta como quieto solo sin entrada: ni el toque ni el siguiente corren pesadas), `IsQuiet(...)`, `PhaseOf(unit)` y `FirstTimeUnitCostMs` = 22. La firma vieja de `RunAnother` sigue (la usa `SceneHost` hasta WP-B) | WP-B: `SceneBridge` arma el `WarmupFrame` con `Next` |
| IA6b-10 | `ThermalAdaptation` emite `LowerReflection` y `DegradeWater`; `AppRoot.ApplyQuality` todavía no los aplica (solo los registra) | WP-B |
| IA6b-11 | `ShaderArt`: `WaterShaders` (`WaterAdvanced`, 70/5, cola 2450, propiedades `_RmWaterNormal`, `_RmHorizonProfile`, `_RmHullFootprint`, `_RmReflectionMix`), `WaterTiers` (110/6, 70/5, 35/2 con `RM_WATER_HIGH` / sin palabra / `RM_WATER_LITE`), `WaterGlobals`, `WaterConstants()` (los `kRm*` del agua con su fuente en Core, entre ellos `kRmDegradedLineMax` = 9), `FoamMaxAlu` = 30 y `WaterMarks` con ramas ≤ 40. No van en `Scene`/`Globals`/`Includes`: las pruebas de la 6a no dependen de los archivos de WP-D | WP-D usa esos nombres; `WaterShaderContractTests` los revisa cuando existe `RowMateWater.hlsl` |
| IA6b-12 | #3: `SkyArt.GlowStrengthAt(g)` = 0,35 + 0,10·clamp((g − 0,5)/0,5): sin saltos. Va en el vértice de `Sky.shader` con `_RmGlowParams.w` = 0,10 y `kRmGlowGoldenFrom`; `Horizon.shader` toma los flancos y la bruma nuevos (`Sky.mat` también) | WP-D ajusta con las sondas (DV-153) |
| IA6b-13 | `HullMesh`: la cubierta de la punta queda 0,03 m más baja; las pruebas de la 6a reconocen la cubierta por la normal (no por la altura) | — |
| IA6b-14 | La huella de las miniaturas (`thumbnails.json`) **cambia**: hashea `ShaderArt`, `LakeArt`, `SkyArt` y el texto de `Art/Shaders/**`, que la 6b cambia a propósito (y las montañas de #3 y la línea de #2 cambian la imagen). DT6b-19 se cumple en el agua (la miniatura sigue degradada), no en la huella | WP-B vuelve a hornear con `tools/unity/bake-thumbnails.ps1` **después** de WP-D; hasta entonces `TheVersionedThumbnailsAreUpToDate` queda roja |
| IA6b-15 | Las pruebas de texto de archivos de WP-B/WP-D (`ItSixBToolingTests`, 4 de `WaterShaderContractTests`) quedan ignoradas con "pendiente WP-B/WP-D" hasta que existan; el set por defecto de `capture-snapshots.ps1` se acepta `it15` o `it6b` en las pruebas de la 6a y la 15 | WP-B y WP-D quitan las guardas al cerrar |
| IA6b-16 | `run-tests.ps1 -Matrix` y `TraceabilityTests` reconocen `AC-6B-nn` (`It06bIds`, 38 AC) | — |

---

## 16. Cambios por implementación (WP-B)

WP-B sobre WP-A, sin WP-D: los *shaders* de WP-D (`RowMateWater.hlsl`, `WaterAdvanced`, los cambios de `WaterDegraded` y `WaterMarks`, `UI/HudBlur`) van en una versión **provisoria** de WP-B que cumple el contrato y los topes (WP-D los reemplaza en su ronda). El resultado de las corridas está en la respuesta del paquete.

| ID | Qué | Por qué / quién sigue |
|---|---|---|
| IB6b-01 | Las fases de `SceneHost` son 14: 0-2 el preámbulo (la cámara y la luz son las dos primeras unidades de `SceneWorldBuilder`; el primer pedido de URP a 64² con la cámara del mundo a medio armar; la sesión de Core de usar y tirar en 5 unidades), 3 la sesión preparada (con el escenario elegido, antes del mundo), 4 el resto del mundo, 5-8 como en la 6a, 9 el post al tamaño de la sesión (pesado; si ya corrió en el negro, se salta), 10 el cuadro y los 5 sub-dibujos, 11 la orilla, 12 Ready, 13 el agua avanzada. El post en la RT chica de la 6a pasó a ser el (0b) | — |
| IB6b-02 | Los sub-dibujos se arman apagando los *renderers* de los otros grupos (`SceneDirector.WarmupGroupOf`) y pidiendo solo los dibujos instanciados del grupo; `WarmupDraws` sigue contando el dibujo entero (1) y `SubDraws` los sub-dibujos. Son **6** y no 5: en la corrida completa del Editor el grupo "terreno y orilla" midió 4,37 ms, así que la orilla y el relleno instanciados van en su propio sub-dibujo. Editor, segunda precarga: peor sub-dibujo 1,5 ms; peor unidad liviana 3,2 ms (Tick sintético) | La medida real es `-PerfEarly` en el build |
| IB6b-03 | El orden del *player loop* medido (`EarlyEntryInputTests`): `UIElementsUpdatePanels` en el índice 6 y `ScriptRunBehaviourLateUpdate` en el 12 de `PreLateUpdate`: el despacho de la UI va antes. `HeavyNeverSharesTheTapFrame` va en `UnityEditMode` (con `InputTestFixture` sobre `SceneBridge.InputActive` y `WarmupFrame.Next`), no en PlayMode con la escena: el ensamblado de PlayMode no tiene el *test framework* del Input System | Desvío menor |
| IB6b-04 | `RunPendingHeavyWork` corre en `BuildViewNow` justo después de `CatchUp`; el reloj se corre con `ImmersiveCascadeMotion.Delay` y el vuelo con `_entryShift` | — |
| IB6b-05 | `-rowmate-perf-early` toca la tecla de Workout con `ShellStore.Select(1)` (si no, no hay precarga), espera que se pueda empezar, espera los `s` y llama `StartFree` (el mismo camino que el toque) | — |
| IB6b-06 | `WaterMarks`: los tipos 3 (anillo), 4 (columna) y 5 (estela) los pasa el **vértice** a los dibujos de los tipos 2 (el anillo de la boya) y 1 (la espuma batida), con el color de la luz y el alfa de salida en 0 para la columna (aditiva). Así el fragmento no suma ramas: 13 ALU + 28 en ramas (tope 20 + 40). La estela empaqueta en `_RmWake[96]` lo que el vértice necesita (z, semiancho, ancho del brazo, alfa; ancho y alfa del centro): sin constantes nuevas | WP-D puede volver a dar a la columna sus franjas si le entra en el tope |
| IB6b-07 | `WaterAdvanced` provisorio: lo suave (color base, peso del reflejo sobre la normal plana, niebla, luz bajo el puente, la columna con la cresta, el tramo de la degradada y su reflejo leve) va en el vértice; el fragmento pone la normal del mapa, el cielo reflejado (planar en Alto, silueta en Medio), los destellos y, en ramas, la espuma de las crestas, la línea de contacto y las líneas de la degradada. D3D11: Alto 51 ALU / 4 tex, Medio 40 / 3, Lite 27 / 1, ramas 32 (topes 110/6, 70/5, 35/2 y 40). El salto degradado del fundido omite la franja somera y la bruma baja de la degradada (solo se ve durante los 0,6 s) | WP-D: el reparto final y el GGX en Medio |
| IB6b-08 | El slot `water.normalDetail` queda en Procedural (AC-6B-37): el PNG horneado lo lleva el material `WaterAdvanced.mat` (`SceneMaterialsBuilder`); un asset asignado al slot gana; sin ninguno de los dos, uno procedural de 64². `WaterNormalBaker.ApplyFromCommandLine` hornea y rehace `scene.materials` sin pasar por el *bootstrapper* (no reescribe `ProjectSettings/RowMateBootstrap.json`) | — |
| IB6b-09 | El soporte (DT6b-22) se comprueba con `Shader.isSupported` y `material.GetTag`. Unity no mira el tag `RenderPipeline` en ninguno de los dos: el *shader* de prueba "que no corre en URP" es uno con `#pragma only_renderers vulkan` (en el Editor de Direct3D 11 no tiene SubShader) | — |
| IB6b-10 | `-rowmate-perf-gpu-load` no usa un `GpuLoad.shader` propio: repite el `CommandBuffer` del vidrio (`GpuLoadPassesPerMs` = 4 por ms pedido). No hace falta un material solo de desarrollo (G6b-12) | Desvío menor |
| IB6b-11 | `HoldAdvancedWaterForTest` (además de `DelayAdvancedWaterForTest`) para la prueba de la avanzada que llega tarde | — |
| IB6b-12 | La placa del conteo de `CenterCallouts` (dibujada con el *painter*) y el vidrio enmascarado de `SeriesPanelView` siguen con el respaldo plano; el vidrio va en las piezas `rm-hud-glass`, la placa de pausa y el diálogo de completado | Para issues |
| IB6b-13 | Las columnas de luz y la estela no cambian `Glow` (DT6b-23); los micro-anillos se prenden con la avanzada en Alto y Medio (`WaterInteractionRenderer.RingsEnabled`) | — |
| IB6b-14 | Medio usa la malla en anillos de 1 m de Bajo (`SceneWorldBuilder.WaterMeshTier`), y todas sin los triángulos enteros detrás de la cámara (z < −10 m: la cámara del contrato, el vuelo y la toma aérea miran siempre hacia adelante). Con la de 0,5 m, `SceneAdaptationTests.TheRowerFitsTheMediumBudget` daba 181 928 triángulos con el maniquí (tope 170 k) y 285 176 con el *slot* (tope 280 k). Ahora Alto 12 816, Medio y Bajo 4 536 triángulos de agua | Desvío de §4.1 (Medio a 0,5 m); el detalle de cerca lo pone el mapa de normales |
| IB6b-15 | La estela dibujada aparte (1 instancia del mismo material) sumaba 1 SetPass: Medio en el lago daba **51**. La cinta va ahora en la MISMA malla que el quad de las marcas (`WakeRenderer.BuildCombined`, UV2 distingue las partes y el vértice degenera la que no corresponde) y la estela es una instancia más del lote de `WaterInteractionRenderer` (capacidad 73). Medio-lago: **50** a las 12 y a las 22 h | §6.1 (ii): "el pase extra se lleva a un material que ya se dibuja" |
| IB6b-16 | El espejo (`PlanarReflection`) se crea en la fase 13, solo en Alto y con la avanzada en camino: con la degradada fija no hay una segunda cámara de escena (`SceneProjectionTests` cuenta una) | — |
| IB6b-17 | `Foam` provisorio a 23 ALU (tope 30, #22): el latido, las puntas, el color con la niebla y las coordenadas del ruido van en el vértice (un vértice por metro), las rampas del ancho son lineales y la playa y el acantilado se eligen sin rama | WP-D revisa el aspecto |
| IB6b-18 | Las pruebas de presupuesto (`SceneBudgetTests.DrawCountsPerTierWithinTheBudget`, `SceneAdaptationTests`, `EnvironmentBudgetTests`) miden con `SceneWater = Auto`, esperan la avanzada (`AdvancedWaterTests.UntilAdvanced`) y afirman que se dibuja. Editor: Alto 71/72 SetPass con el espejo (tope 80), Medio 50/50, Bajo 36/37 | — |
| IB6b-19 | Los cambios de #12 en los charcos (× 1,3, alfa mínimo 0,35 hasta los 2 s, el par girado 40° y el hervidero + 25 %) y la instancia de la estela van **solo con el agua avanzada** (`RingsEnabled`): con la degradada fija, los charcos de la 7 no cambian (DT6b-24). La estela usa uno de cada dos puntos de la historia (24 tramos, 138 triángulos por instancia del lote) y su alfa va × 0,45 (provisorio) | WP-D ajusta |
| IB6b-20 | La prueba vieja `SceneLongSessionTests.AtTheMarathonNothingShakes` espera 20 cuadros antes de comparar: la placa de pausa ahora es de vidrio y su fundido de 200 ms es un movimiento finito (no se relajó la tolerancia: siguen 0 píxeles) | — |
| IB6b-21 | `-rowmate-perf-early` mide el tiempo de CPU del hilo principal (`FrameTimingManager`, como `FrameTimeProbe`): el intervalo entre cuadros incluía la espera del `FramePacer` en reposo (≈ 80 ms a 12,5 cuadros por segundo) | — |
| IB6b-22 | Medidas en el build de desarrollo del simulador (Mono). La pantalla del player estaba en el modo de **respaldo, 1024 × 768 a 60 Hz** (la ventana quedó en 1024 × 720 y no en W1280). La carga de los otros procesos fue del 9 al 24 % en cada corrida. **`-PerfEarly`** (`-EarlySeconds 1`, 8 corridas): el toque costó entre 2,2 y 3,1 ms de CPU y el negro de la entrada 33 ms con el trabajo pesado. El peor cuadro con movimiento fue de **44-47 ms**, a los ≈ 540 ms del toque, con la precarga ya liviana (la primera vez de la cascada en un arranque en frío): **FAIL** de AC-6B-02 en ese punto. Con `-EarlySeconds 0,3` el toque llegó a 722-744 ms (Mono, sin el precalentado del HUD). **`-PerfWater`** (16 corridas): p99 de CPU del hilo principal de 14,3 a 17,4 ms en Alto (lago y río a las 12 h por encima de 16,7) y de 12,3 a 15,2 ms en Medio; máximo de 22 a 26 ms (por debajo de 33); la avanzada se dibujó en todas, con el espejo en Alto. La GPU del vidrio (`GpuRecorder`) no se midió | Para issues (QA) |
| IB6b-23 | En dos tomas de `it6a` (CAP-12 a 42 km, `it6a-low`) aparece un temblor de 4 a 10 píxeles: diferencias de 1 nivel en la piel del remero entre dos dibujos quietos. No viene del agua ni del vidrio (pasa también en Bajo, sin vidrio). La causa no se encontró | Para issues (QA) |

---

## 17. Cambios por verificación (ronda 1, architect)

Contra [05-qa-verification](05-qa-verification.md): QA-V6b-01, -06 y -11. Corridas en `<scratch>/it6b/v1/`, con `ROWMATE_DATA_DIR` temporal, una sola instancia de Unity y sin tocar procesos ajenos. **Pantalla: modo de respaldo, 1024 × 768 a 60 Hz** (la ventana del player, 1024 × 720; el player va a ≈ 13 cuadros/s). Carga de los otros procesos: 10-25 % en las corridas del build y 17-22 % de media en las de Unity (picos de 87-89 % en PlayMode).

| Ítem | Qué se hizo y por qué (una línea) |
|---|---|
| QA-V6b-01 · causa | **La medida, no la escena**: `FrameTimingManager` entrega el tiempo de un cuadro 1 a 4 cuadros tarde, y `-rowmate-perf-early` le ponía la hora y la fase del cuadro en que lo **leía**. El "peor con movimiento" de QA era el cuadro negro de la entrada: en las 8 corridas valía el negro quieto + 11,8-13,2 ms (el armado del HUD), a los ≈ 540 ms con 4 cuadros de atraso y a los ≈ 310 ms con 1. La fase "sesión preparada" era la del cuadro de la lectura. Las unidades de la escena nunca pasaron de 3,9 ms |
| QA-V6b-01 · arreglo de la medida | N `Core/Scene/EarlyEntryFrames.cs`: cada medida va al cuadro que la produjo por `frameStartTimestamp` (el primero cuyo `LateUpdate` es posterior). `AppRoot` anota cada cuadro desde antes del toque con `QueryPerformanceCounter` (el reloj de `FrameTimingManager` en Windows; el `Stopwatch` de Mono cuenta desde el arranque del proceso y no coincide), lee las 4 últimas medidas por cuadro y suma las esperas (`Gfx.WaitForPresentOnGfxThread`, `Gfx.WaitForRenderThread`, `WaitForTargetFPS`), que **no** entran en el CPU medido. La línea `[PERF]` suma la espera, el negro entero (con su trabajo pesado aparte), los cuadros atribuidos y la línea de tiempo; sin atribución escribe "sin medida" y `perf-log-check.ps1` falla (antes un 0 pasaba) |
| QA-V6b-01 · resultado AC-6B-01 | `build.ps1 -Simulator -PerfEarly -EarlySeconds 1` (5 lago + río, costa y regata, en frío): **peor cuadro con movimiento 4,8-5,8 ms (16/16 en dos corridas de 8, ≤ 16,7)**, todos los cuadros desde el toque atribuidos (35/35). **PASS** |
| QA-V6b-01 · lo que destapó (AC-6B-02) | Con la atribución bien hecha, **el toque en frío costaba 41,3-43,1 ms**. El "1,4-3,1 ms" de QA y de IB6b-22 eran cuadros de antes del toque leídos tarde. Desglose del cuadro: `StartFree` 14,4-15,2 ms (el `Open` de la escena 4,5-4,8), el `Tick` de la escena 11,7-12,0 y ≈ 11 de Unity (el panel y el dibujo de la página nueva) |
| QA-V6b-01 · arreglo del toque | `SceneHost.Open`: si el toque llega **antes del preámbulo y sin sesión preparada** (`_phase ≤ PhaseCoreJit`), solo anota la hora (`OpenPending`). La sesión, su `Open` con la hora del toque y su primer `Tick` se arman en el negro de la entrada (`RunPendingHeavyWork`, después del preámbulo y con el JIT de Core hecho), al día con el toque. En el arnés (tramo sin tope) se arma en el primer `Tick` en que el preámbulo terminó. `Close` descarta el pendiente. Prueba: `EarlyEntryTests.AColdTapLeavesTheSessionToTheBlackFrame` (sin sesión en el cuadro del toque ni en el siguiente; llega en el negro, después del preámbulo y al día con el toque) |
| QA-V6b-01 · resultado AC-6B-02 | Toque en frío **25,3-28,8 ms** en 16/16 (antes 41,3-43,1): `Open` 0,1 ms y `Tick` 0,0 ms. El negro quieto pasa a 59-72 ms (D-03: lo que dure el trabajo pesado, con el reloj corrido). **Sigue FAIL**: queda `StartFree` sin la escena (≈ 10 ms: el motor y la página del inmersivo por primera vez) y ≈ 12 ms del panel. Con el toque a los 4 s (precarga en "mundo") el cuadro medía 23-24,6 ms antes de este arreglo: no es solo la precarga. Va a "Para issues" con el dato |
| QA-V6b-06 · AC-6B-22 | N `Tests/PlayMode/Scene/AdvancedLifecycleTests.cs`: los 16 casos de `scene_view_test.dart:11` (4 escenarios × 390 × 844 y 844 × 390 × 12 y 23 h) con `SceneWater = Auto` y Alto (el espejo incluido), la avanzada a la vista en cada uno de los 6 cuadros de 100 ms, el desmontaje sin excepciones ni avisos y ninguna RT del agua viva; y `ClosingWhileTheAdvancedWaterIsPreparing` (8 casos: Alto y Medio × "preparando" / "retenida" × terminar / desmontar). "Preparando" = `SceneHost.StopWaterWarmupAtForTest(4)` (nuevo, internal: el espejo y la RT del sub-dibujo creados, sin "lista"); "retenida" = `HoldAdvancedWaterForTest`. **24/24** |
| QA-V6b-11 · AC-6B-08 | `WaterFallbackTests.NoFrameOver33` (fundido y corte): el trabajo de la app en cada cuadro del cambio (el `Tick` del shell y el de la escena, que dibuja) **3,7 ms** en el fundido y **3,6 ms** en el corte (tope 33). `SavedSessionIdentical`: dos libres de 20 s con el reloj y el remo de prueba, uno con la falla a los 10 s: la misma fila (sin el id) y los mismos 5 puntos (sin sus ids). **3/3** |
| Pruebas | dotnet `-Matrix`: **4173: 4134 pasan, 0 fallan, 39 ignorados** (+194/194); matriz PASA 919, FALLA 0. EditMode **4316: 4261 pasan, 0 fallan, 55 ignorados**; `-Simulator` **4510: 4455, 0, 55**. PlayMode **795: 795 pasan**. PlayMode `-Simulator` **876: 876 pasan**. `ProjectSettings` sin cambios en todas |
| Capturas | Ningún cambio toca lo que se dibuja (la medida, una prueba nueva y el orden del `Open` en frío, que en las tomas no pasa: entran con la escena lista). No se regeneraron |
| Builds | `simulator` (desarrollo, con `-PerfEarly`: código 15 por el toque, AC-6B-02) y `normal` (release: 0, sin rastros del simulador, `RowMateBle.dll`, VERSIONINFO 1.0.0) regenerados; `ProjectSettings` sin cambios |

**Para issues** (de esta ronda):

- AC-6B-02 en frío: el cuadro del toque sigue en 25-29 ms sin la escena (≈ 10 ms de `StartFree` con la página del inmersivo por primera vez y ≈ 12 ms del panel). Propuesta: precalentar en el arranque la página del inmersivo y el camino de `StartFree`, como `HudWarmup` hace con el HUD (QA-V4b-06). Medir con la pantalla en su modo normal.
- La adaptación y todas las medidas de GPU siguen con la pantalla en el modo de respaldo (QA-V6b-10): el hilo principal espera ≈ 75 ms por cuadro a la GPU.

---

## 18. Cambios por la ronda del salto (AC-6B-20 b, QA-V6b-R2-03, #91)

Lo único que se corrige es el salto del agua cada 16 m. Ningún otro FAIL se toca. Corridas en `<scratch>/it6b/r3/`, con `ROWMATE_DATA_DIR` temporal, una sola instancia de Unity y sin tocar procesos ajenos.

**Causa exacta.** La malla en anillos se movía con `−Mod(D; 16)` (`WavePhases.GridAnchorZ`). Cada 16 m saltaba 16 m en cinta, y un mismo punto de la pantalla pasaba de golpe a otro anillo: por ejemplo, de paso 8 m a paso 4 m a 40 m de la cámara. Lo que el **vértice** calcula y el rasterizador interpola cambiaba de golpe: el color con la ola, la pendiente de B1, la cresta de la columna, la espuma de las crestas y los parches de calma. En los anillos de 4 a 16 m, B1 (λ de 6 a 25 m) quedaba con menos de 3 vértices por λ: el muestreo era casi aleatorio y cambiaba en cada salto. Las UV de B2/B3/B4, la niebla, la absorción, el reflejo y la huella son afines o dependen solo de la z de cinta: con la malla en otro lugar no cambian y **no** eran la causa.

| ID | Decisión (una línea) |
|---|---|
| DT6b-27 | **La malla queda quieta en cinta** (`WaterVertexField.MeshOffsetZ` = 0; `AdvancedWaterSurface` ya no la mueve). El paso en cada punto de la pantalla ya no cambia, y todo lo del vértice es continuo en D (las fases y los corrimientos siguen en double: Q-68 sin cambios) |
| DT6b-27 · banda | Lo que el paso no alcanza se **apaga hacia su media** en vez de arrastrarse (aliasing con la malla quieta). Cada onda va × `Band(k·paso)` = 1 − S(π/3; 2π/3; k·paso): entera hasta 6 vértices por λ y nada desde 3. El paso es `WaterRingMesh.MaxStep` = mín(256; máx(s₀; (d − 20 + 8·s₀)/4)) con d = máx(\|x\|; \|z\|). Es lineal en d, conservador (nunca menor que el paso del triángulo) e igual en los vértices que comparten dos anillos. Va en el color **b** de la malla, porque Medio y Bajo usan la malla de Bajo y un cambio de nivel no rehace la malla |
| DT6b-27 · qué se apaga | El color (h·A·0,25), la pendiente de B1 y la cresta de la columna van con la ola limitada; la cresta tiende a su media de 0,3754. La espuma de las crestas se apaga una octava antes (2·k₂, porque es un umbral angosto) hacia su media de 0,1069. La calma usa sus dos ondas (k 0,196 y 0,342 rad/m). La **geometría** de la costa (`_RmWaterLift·h`) sigue con la ola entera, así lo apoyado (la boya) sube igual |
| Referencia | N `Core/Scene/Water/WaterVertexField.cs` (`Evaluate`, `Band`, las medias y las constantes) es la referencia del vértice de `WaterAdvanced.shader`. Las 9 `kRm*` nuevas están en `ShaderArt.WaterConstants` (las comprueba `TheWaterConstantsAreTheCoreOnes`). `WaterPhaseSet.GridAnchorZ` y `WavePhases.GridStep` se quitan; `_RmWaterOffsets2.w` = 0 |
| Fragmento | Sin cambios: Alto 83/110, **Medio 70/70**, Lite 33/35, ramas 40/40 (`SceneShaderTests`). El vértice suma 2 senos y 5 `SmoothStep` (sin tope) |
| Prueba que reproduce | N `Tests/EditMode/Scene/Water/WaterMeshShiftTests.cs` (dotnet y Unity). Interpola los campos del vértice sobre la malla de verdad (Alto y Bajo), en la posición de `MeshOffsetZ`, como VID-01 sin GPU: R-4 a 3,8 m/s desde 4090 m durante 6 s, cruzando 4096 y 4112 m, con 60 fps, z de 16 a 60 m y \|x\| ≤ 6. Por cuadro mide la diferencia media contra la mediana de los ±0,25 s. **Antes: pico de 5,7 a 34,8 × la mediana** en todos los escenarios y campos. **Después: 1,01 a 1,27 ×** (tope 1,5). También prueba que el color b cubre el paso de cada triángulo y las dos medias |
| Desvío | PA6b-02 / 00-arte-agua §3.3 ("la malla se ancla con `Mod(D; 16)`") deja de regir. Más allá de ≈ 25-50 m (según λ del escenario) la ola del legacy ya no pinta su dibujo en el color, la pendiente ni la cresta: queda su media. Antes ahí había ruido de muestreo que saltaba cada 16 m |

**Resultados.**

| Corrida | Resultado | Carga de otros procesos (sin Unity) |
|---|---|---|
| `run-tests.ps1` (dotnet) | Antes de hornear: 4182, 1 falla (`TheVersionedThumbnailsAreUpToDate`). **Al final: 4182: 4143 pasan, 0 fallan, 39 ignorados** (+194/194) | — |
| `bake-thumbnails.ps1` ×2 | 4/4 (la huella cambia por `ShaderArt` y el `.shader`) | 23 % de media, 73 % máx |
| Unity EditMode | **4325: 4270 pasan, 0 fallan, 55 ignorados** | 13 % antes; 16 % de media, 25 % máx |
| Unity EditMode `-Simulator` | **4519: 4464 pasan, 0 fallan, 55 ignorados** | 14 % antes; 17 % de media, 62 % máx |
| Unity PlayMode | **796: 796 pasan** | 13 % antes; 15 % de media, 83 % máx |
| Unity PlayMode `-Simulator` | **877: 875 pasan, 2 fallan**: `BleAllocationTests.HrmConnectedZeroBytesPerFrame` (1 asignación en 120 cuadros) y `TheSonarDoesNotAllocatePerFrame` (2 en 120 dibujos), en pantallas sin escena. Sola, la clase da **4/4**. Son intermitentes y ajenos al agua; no se tocó la tolerancia. `SceneBudgetTests(High)` pasó | 15 % antes; 15 % de media, 65 % máx (la VM de WSL, ≈ 11 %) |
| `capture-snapshots.ps1 -Set it6b` | 38 tomas. **VID-01: mediana 0,0062, pico 0,0087 a los 2,67 s (1,41 ×; tope 1,5); en el cruce de 4096 m (0,77 s) no hay pico. Pasa.** Sondas: las mismas 16 de aspecto que fallan, **0 de contrato**, y ninguna sonda cambia de estado. Cambian valores: AC-6B-12 lago 36,5 → 20,4 % y río 10,6 → 26,6 % (≥ 10 %), AC-6B-13 a ± 40 dp en todos los casos, la espuma de crestas 15,3 → 15,7 % (informativa) | 22 % de media, 84 % máx |

Segunda corrida de `it6b` a `<scratch>`: **38/38 PNG y MP4 iguales byte a byte**, y el `manifest.json` y el `INDEX.md` iguales salvo `durationMs`. Solo escribe en la carpeta de la 6b. Builds regenerados: `normal` (release, 0: sin rastros del simulador, con `RowMateBle.dll` y VERSIONINFO 1.0.0) y `simulator` (0). `ProjectSettings` sin cambios en todas las corridas (la guardia); `HorizonFar.mat` y `HorizonNear.mat` sin cambios; el legacy sin cambios.

**Para issues** (de esta ronda):

- Llevar B1 al fragmento en Alto (sobran 27 ALU) recuperaría el dibujo de la ola más allá de ≈ 30 m. En Medio no entra (70/70).
- Las juntas en T entre anillos siguen (ahora quietas en la pantalla). Con la costa se abren a lo sumo 0,15·Δh.
