# It. 6b — Gate único de QA (spec ↔ diseño ↔ arte)

| Campo | Valor |
|---|---|
| Entrada | [01-po-spec](01-po-spec.md) (AC-6B-01 a -38), [02-tech-design](02-tech-design.md) (DT6b-01 a -21, D-01 a -03) y [00-arte-agua](00-arte-agua.md) |
| Contra | [ROADMAP §It. 6b](../../ROADMAP.md), [ARCHITECTURE §20](../../architecture/ARCHITECTURE.md), la deuda de la 6a ([05-qa-verification R3](../06a-escena-lago/05-qa-verification.md)), el legacy [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`shaders/water.frag`, `water_painter.dart`, `water_shader.dart`, `scene_view.dart`, `scene_camera.dart`, `time_of_day.dart`) y el código de hoy |
| dotnet (base) | `run-tests.ps1 -ArtifactsPath <scratch>/it6b/gate`: **4084 en total, 4045 pasan, 39 omitidos, 0 fallan** (54,6 s) |
| Legacy | `git status --porcelain` vacío (sin tocar el módulo de credenciales de Strava) |
| Repo público | Los 3 documentos: 0 rutas de la PC, sin nombres de usuario ni datos del equipo |

## Veredicto: **APROBADO CON OBSERVACIONES**

0 bloqueantes, **5 mayores** y 15 menores. Se puede implementar. Los mayores se resuelven en el diseño o en la spec **antes del paso de WP-B que tocan** (§9 del diseño): G6b-01 antes de (3), G6b-02 antes de (2), G6b-03 antes de (7), G6b-04 antes de (7) y G6b-05 antes de cerrar (1).

Lo que está bien: los 38 AC tienen responsable y prueba (§10 del diseño). Los números del contrato coinciden con `water.frag:57-96` y `water_painter.dart:57-58`: peso del reflejo 0,128/0,290/0,424/0,561, niebla `S(80;400)`, estela `clamp(v/4)`, popa en 5,3 m, semiancho `0,3+0,35·back` y espuma al 35 %, y amplitud y escala por escenario (`*_environment.dart`). La regla de elección y la caída sin reintento siguen a `water_shader.dart:12-22`. La deuda de la 6a va primero, con causa y prueba. Las técnicas son viables en URP 17 / Unity 6.3: `SubmitRenderRequest` ya se usa en `SceneRenderer`, el planar tiene plan B, y el vidrio usa `Background.FromRenderTexture` con 0 elementos. Los topes no se relajan y la medición registra la pantalla y la carga externa. Los assets son propios. D-01 (SetPass de Alto en 80) es la regla vigente de ARCHITECTURE §20.1 (línea "Cambios de la It. 6a") y de `SceneBudgetTests:185`.

## Issues

| ID | Sev. | Resp. | Qué | Evidencia | Corrección |
|---|---|---|---|---|---|
| G6b-01 | mayor | architect | **La falta de soporte del agua avanzada no se detecta nunca.** `Shader.isSupported` devuelve `true` si el *shader* **o su `Fallback`** funcionan. Con `Fallback "RowMate/Scene/WaterDegraded"` (§4.1), un equipo sin soporte dibuja en silencio la degradada con la malla de anillos y el material avanzado. Así no hay línea de registro (AC-6B-08), `_RmDegradedBlend` no tiene efecto, la estela sigue y AC-6B-06 (fila 1) no se cumple | Diseño §4.1 y §5 (a); ARCHITECTURE línea 808 | Detectar el SubShader avanzado: un tag `RmWater`=`Advanced` en sus SubShaders y `material.GetTag("RmWater", false) == "Advanced"`, o sacar el `Fallback` (la degradada ya la dibuja `WaterSurfaceController`). Agregar la prueba `WaterFallbackTests.UnsupportedSubShaderIsDetected` (un *shader* de prueba sin el tag cae con una línea) |
| G6b-02 | mayor | architect | **El SetPass de Medio en el lago ya está en el tope (50)**, y el diseño solo cuenta el de Alto. La 6b suma `WaterWake` (+1), el lote `Glow` del lago para la cabaña (+1, hoy "0 en el lago") y posiblemente `WaterMarks` con la calma. AC-6B-29 (Medio ≤ 50) falla sin una mitigación escrita | 07/05-qa-verification: `[SBT]` Medio 12 h con el maniquí, **82 / 50 SetPass**; 08/02-tech-design: "+1 (Glow) en río, costa y regata de noche; 0 en el lago"; diseño §6 y §11 (solo Alto) | En §6, la cuenta de SetPass por nivel × escenario × hora. Mitigación sin subir el tope: la estela como un tipo más de `WaterMarks` (la misma variante, sin pase nuevo), o la columna de la cabaña en un material que ya se dibuje. Medirlo en el primer paso de (2) con `SceneAdaptationTests` (con el maniquí) y `EnvironmentBudgetTests` |
| G6b-03 | mayor | architect | **Los sets viejos de capturas pasan a dibujar el agua avanzada.** Con la 6b, el agua por defecto en Alto y Medio es la avanzada. Al regenerar `it6a` (lo pide AC-6B-03) no hay líneas que contar: la sonda `lines` falla o mide la nada, y las tomas que prueban AC de la 6a, la 7 y la 8 cambian de agua sin que nadie lo decida (AC-6B-35) | Diseño §2.2 y §8 ("se regenera `it6a` entero con la sonda `lines`"); solo `ThumbnailRule` fuerza la degradada (DT6b-19) | Que `SnapshotCli` fije `WaterForce.Degraded` en los catálogos de `it2` a `it15`, salvo lo que la 6b pide explícitamente (la CAP-04 de `it7` con la avanzada). Una prueba en `ItSixBToolingTests`. Lo mismo en las pruebas PlayMode que verifican AC de la degradada (AC-SCN-13-02 a -04, AC-SCN-12-01) |
| G6b-04 | mayor | po | **Las sondas de AC-6B-15 no se pueden medir así.** (a) En TV (focal 411 dp, `scene_camera.dart:9`) los 400 m caen a 2,26 dp del horizonte, dentro de la costura de 3 dp del arte (§2.8, `lerp(col; skyHorizon; …)`). A las 12 h `skyHorizon` `#8FD3FF` y `fog` `#CFE9FF` difieren en 64/255 en R: da unos 16/255 en lugar de ± 2/255. (b) "≤ 6/255 entre la última fila de agua y la primera de cielo **o silueta**" contradice las siluetas oscuras del horizonte (cordilleras, fachadas y acantilado), que tienen que leerse (AC-6B-04 pide contraste ≥ 1,3:1) | `time_of_day.dart:80-83`; 00-arte §2.8; DV-66 (franja de bruma de 1,5°) | (a) La niebla a 400 m, numérica en [dotnet] (`WaterModel.FogWeight`, ya en AC-6B-10). En captura, una fila a z ≈ 300 m en W1280 con su valor esperado. (b) La costura se mide solo en las columnas donde el agua toca el cielo o la franja de bruma, nunca una silueta, y el Designer define las filas de la costura |
| G6b-05 | mayor | architect | **Una unidad `Heavy` puede caer en el cuadro del toque.** Hoy el tramo se decide en `AppRoot.LateUpdate` (`SceneBridge.Tick`). Si UI Toolkit (sin `EventSystem`) despacha el toque **después** de esa decisión, el cuadro del toque paga la unidad pesada (11-22 ms, R3-01) más el toque. AC-6B-02 pide 5 de 5 y no hay prueba de ese caso | `AppRoot.cs:427-439`; diseño §2.1 (2) y sus pruebas | Comprobar el orden real del *player loop* y escribirlo. Si el despacho va después, una `Heavy` corre solo cuando el cuadro anterior ya fue quieto y no hay un puntero apretado. Agregar la prueba `EarlyEntryTests.HeavyNeverSharesTheTapFrame` (el toque sintético en el mismo cuadro que una `Heavy` pendiente) |
| G6b-06 | menor | po | AC-6B-29 dice "SetPass ≤ 50": hay que escribirlo por nivel (Alto ≤ 80, Medio y Bajo ≤ 50). Se adopta D-01 | ARCHITECTURE §20.1 | Corregir el texto del AC |
| G6b-07 | menor | po | Se adoptan D-02 (½ por eje en escritorio) y D-03 (el negro puede durar lo que tarde el trabajo pesado, con el reloj de la entrada corrido). Falta anotarlos en la spec | Diseño §12 | Una línea en §7 de la spec |
| G6b-08 | menor | po | AC-6B-18 dice "cada farol y ventana", pero el diseño pone tope de 16 ventanas a menos de 60 m y 8 columnas en Lite. Además, las columnas también salen en la degradada, mientras AC-6B-09 dice "la degradada sigue igual" | DT6b, `LightColumns`; §4.1 | Escribir los topes en AC-6B-18 y decir si la degradada lleva columnas de noche |
| G6b-09 | menor | po | La sonda de AC-6B-23 (variación ≤ 25 %) también pasa con el respaldo plano o con el tinte solo: no prueba que haya desenfoque | AC-6B-23 | Sumar una medida de alta frecuencia: el gradiente dentro de la tarjeta contra el del fondo × (1 − α) |
| G6b-10 | menor | architect | `Glow` tipo 3 y `WaterMarks` con la calma suman ALU sin un tope nuevo en `ShaderArt`: hoy `Glow` está en 14 | `ShaderArt.cs:126,136` | Escribir los topes nuevos |
| G6b-11 | menor | architect | La GPU del desenfoque necesita `CommandBuffer.BeginSample` adentro del *buffer* (no `Profiler.BeginSample`) para que `GpuRecorder` la vea | Diseño §4.4 y §6 | Aclararlo |
| G6b-12 | menor | architect | `GpuLoad.shader` está "fuera de la colección": en el build de desarrollo `Shader.Find` no lo encuentra si nada lo referencia | Diseño §7 | Referenciarlo con un material solo de desarrollo |
| G6b-13 | menor | architect | El CPU del espejo (`RowMate.Scene.Reflection`) no tiene tope (queda fuera de los 1,5 ms) | Diseño §6 | Un tope (≤ 1 ms p95) o el hilo principal ≤ 8 ms (§20.1) en `-PerfWater` |
| G6b-14 | menor | architect | En el fundido hacia la avanzada hay que apagar la grilla degradada **en el mismo cuadro**. Si no, quedan dos opacos coplanares en y = 0 que se pisan (y se cruzan con el mar de fondo de la costa) | Diseño §5 | Aclararlo |
| G6b-15 | menor | architect | Las tomas con distancia forzada necesitan la historia de la estela precargada (12 s a velocidad estable) para que la estela salga a pleno y sea determinista | Diseño §8 | Una precarga de `WakeTrail` en el arnés |
| G6b-16 | menor | architect | Las pruebas de presupuesto con la avanzada tienen que esperar `AdvancedReady` y afirmar `Showing == Advanced`. Si no, miden la degradada | Diseño §6 | Sumar la afirmación |
| G6b-17 | menor | architect | `SavedSessionIdentical` (AC-6B-08) necesita el reloj de prueba y el simulador determinista | Diseño §10 | Escribirlo en la prueba |
| G6b-18 | menor | architect | El Standalone es IL2CPP (`scriptingBackend` 1): no hay "JIT de Core". Lo que cuesta la primera vez es la inicialización de metadatos y de estáticos, y el preámbulo sirve igual. `PostAtSessionSize` en el negro, fuera del orden de las fases, no tiene que depender del mundo | ProjectSettings; diseño §2.1 | Renombrarlo y escribir la independencia |
| G6b-19 | menor | designer | `00-arte-agua` sigue diciendo lo que el diseño rechazó: Schlick, el planar del bote en Medio, la banda en Bajo, la espuma de crestas en 0,45, el *slot* `water.normal` y 32 tomas en Medio | Diseño §1 | Marcar esas partes como reemplazadas, en `04-design-notes` |
| G6b-20 | menor | po | CAP-09 a -12 se toman con `Series 600` (DT6b-16) y el resto de la matriz con `Libre`: el HUD no es comparable | DT6b-16 | Aceptarlo o tomar la boya en una toma aparte |

## Matriz AC → test (resumen)

Los 38 tienen test en el §10 del diseño. Con este gate se suman:

| AC | Prueba que se suma |
|---|---|
| 06, 08 | `WaterFallbackTests.UnsupportedSubShaderIsDetected` (G6b-01) |
| 02 | `EarlyEntryTests.HeavyNeverSharesTheTapFrame` (G6b-05) |
| 03, 09, 35 | `ItSixBToolingTests`: los catálogos viejos con la degradada (G6b-03) |
| 15 | La niebla en [dotnet] y la sonda redefinida (G6b-04) |
| 29 | La cuenta de SetPass de Medio con el maniquí (G6b-02) |
| 32 | NO VERIFICABLE (Android, #7) |

## Riesgos

- Hay riesgos que el Editor no muestra: lo que cuesta la primera vez en IL2CPP y la GPU sin `GpuRecorder`. Para eso están `-PerfEarly` y `-PerfWater` en el build de desarrollo, con la pantalla y la carga anotadas.
- `GL.invertCulling` dentro de `SubmitRenderRequest`: tiene plan B (cámara espejada con la `v` invertida), que es correcto.
- La carga externa de CPU hace "no concluyentes" los topes de CPU, sin relajarlos.

## Para issues

- Las boyas de carril que se mecen y el planar del bote en Medio (ya en §13 del diseño).
