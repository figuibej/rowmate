# It. 7 — Gate único de QA (armonía QUÉ ↔ CÓMO)

| Campo | Valor |
|---|---|
| Iteración | 7 · `07-remero-palada` |
| Entrada | [01-po-spec](01-po-spec.md), [02-tech-design](02-tech-design.md), [00-arte-remero](00-arte-remero.md) |
| Legacy verificado | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `3ae0f3a`: `scene/stroke_cycle.dart`, `scene/scene_state.dart`, `scene/painters/boat_painter.dart`, `test/scene/{stroke_cycle,scene_state,boat_painter}_test.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar |
| dotnet (línea de base, sin cambios de la 7) | `run-tests.ps1 -ArtifactsPath <scratch>/it7/gate/dotnet`: **3049 tests, 3010 pasan, 39 omitidos, 0 fallan** |
| Modo | Entrega rápida: solo bloqueantes y mayores reales; lo demás en una línea |

## Veredicto: **APROBADO CON OBSERVACIONES**

Se puede empezar por WP-A. Hay que aplicar las 5 mayores antes de cerrar WP-A (QA7-G-02, -03, -05) y antes de WP-B0 (QA7-G-01, -04). Ninguna cambia un número del contrato.

Lo que está bien y no se discute: `StrokeCycle`, `SceneState` (fase, cierre a 12 spm, pausa, charcos con `_bladeWasIn = true`, cabeceo con `p`) y `LegacyBoatGeometry` se portan tal cual, con el oráculo `-Stroke` en `1e-12`. Las cuentas de la spec están bien: tabla de Q-70 (r 0,230 / 0,337 / 0,373 / 0,409 / 0,420), charco del contrato en x ±3,23 y z +7,84, tabla de *surge* 8,6 / 14,5 cm, entrada y salida de la pala en c 0,9945 y 0,381, capacidad de los anillos (16 charcos del contrato hasta 120 spm, 12 visibles hasta 90 spm) y escalón 24 → 30 con error máximo de 0,0125 ciclos. Los 38 AC y LT-88 a LT-98 tienen un componente responsable y una prueba planificada (matriz §6.4). Los documentos no tienen rutas de la PC.

## Issues

| ID | Sev. | Resp. | Problema | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA7-G-01 | mayor (bloquea Y Bot) | po + architect | **Y Bot no pasa la validación del slot**: tiene 55 320 triángulos y el límite es ≤ 30 000. Con el FBX presente igual se usaría el maniquí, así que AC-SCN-08-08 con Y Bot, DEMO-06, CAP-18, CAP-21 y `RowerSlotTests` fallarían. El presupuesto también queda corto: +35 k triángulos y `SceneBudgetTests` con tope de 185 k | Parseo binario del FBX local (herramienta en `<scratch>/it7/gate/fbxcount`): 2 geometrías, 34 480 + 20 840 triángulos, 65 huesos (`LimbNode`). Límite en spec AC-SCN-08-08, diseño §0.3 y §3.2, ASSET_SLOTS:147, :216 | Subir el límite del slot a **≤ 60 000** triángulos (spec AC-SCN-08-08, ASSET_SLOTS §2, `RowerCharacterResolver`). Presupuesto con Y Bot: +56 k, total ≈ 191 k (el tope de 350 k sigue). `SceneBudgetTests` con Y Bot: ≤ 205 k. Los 65 huesos entran en el límite de ≤ 75 |
| QA7-G-02 | mayor | po + designer | **El barrido visible no llega a +42°**: con `−62° + 102°·(α+55°)/90°`, el finish da **+40°**. La spec, la tabla de poses, el arte (charco en x ±2,11, z −1,18) y el test planificado "barrido −62° / +42°" piden +42°, que es un arco de **104°** | spec AC-SCN-05-07; ANIMATION_BIBLE:431 ("arco 104°" con la fórmula de 102°), :395; diseño §2.5 `VisualSweepDeg`; arte §8.3 | Usar `−62° + 104°·(α_legacy + 55°)/90°` en la spec, ANIMATION_BIBLE §3.6 y `OarCurves.VisualSweepDeg` |
| QA7-G-03 | mayor | architect (+ po en AC-SCN-04-04) | **Sin paladas, el remero no queda en el catch.** Cuando `p` llega a 0, `ω_s` cae a 0 con τ 0,5 s y `φ` sigue de largo, porque `φ' ≥ 0` le impide volver. Queda trabado en `φ ≈ 0,025` (`φ = 0,1·(e^{−2t} − e^{−4t})`, máximo a los 0,35 s), que con r 0,23 es `c ≈ 0,043`: piernas al 15 %, pala en el agua. Fallarían el test planificado "spm 0 → c = 0", VID-05 ("queda en el catch; después, respira") y `RowerIdle`, que pide "c en el catch" para respirar | diseño §2.2 (τ 0,5 activo; objetivo 0 con spm 0 y `p = 0`); spec AC-SCN-04-04; ANIMATION_BIBLE §3.9 ("queda en el catch") | En `VisualStrokeClock`: con `!rowing` y `p = 0`, `ω_s` sigue en 12/60 hasta que `φ` cruza 0, y ahí `φ = 0` y `ω_s = 0`. No se ve el corte, porque todas las curvas tienen derivada 0 en el catch. Test: después de 6 s con spm 0, `φ = 0` exacto y `StillSeconds` crece. La pausa no cambia: el adelanto de ≈ 0,028 ciclos sobre `p` es correcto |
| QA7-G-04 | mayor | architect | **El `.meta` versionado del FBX de Mixamo queda huérfano** en cualquier clon sin el FBX (CI `ci-unity`, otra PC). Unity borra los `.meta` sin asset y avisa, y el árbol queda sucio. Además, el bootstrapper escribe `Slots/rower.character.asset` en `Asset` o `Procedural` según la máquina, así que un archivo versionado cambia de un clon a otro. Choca con AC-SCN-08-09 (el CI sin la carpeta) | diseño §1.3 ("el `.meta` de cada FBX de Mixamo", G), §3.2 Import, §10 último punto; `.gitignore:70-72`; hoy no hay nada versionado bajo `Assets/ThirdParty` | No versionar nada bajo `Assets/ThirdParty/Mixamo/` (ni los `.meta`; excepción en `.gitignore`). El `rower.character.asset` se versiona siempre en `Procedural` con su `expectedPath`. El resolver y el build cargan por ruta (el postprocesador ya garantiza el import) y la referencia vive en un asset generado e ignorado (patrón `Bootstrap/Generated`). `RepoHygieneTests`: 0 archivos versionados bajo `ThirdParty/Mixamo` y slot en `Procedural` |
| QA7-G-05 | mayor | po | **La spec no recoge los tres desvíos que el diseño da por aceptados.** Escritos así, fallarían en la verificación: AC-SCN-05-06 (pedalera a 1,15 m y recorrido de 0,62 m; DT7-02 lo muestra imposible), AC-SCN-06-03 ("+ 15 % del cabeceo" entero; DT7-06 lo topa en W1280, TH y L640) y CAP-04 ("dos pares"; DT7-10 / DA7-15 muestran que con `R-4` hay uno) | diseño §0.1 DT7-02, -06, -10, §8.1 DA7-01 ("la spec cambia el texto"); spec AC-SCN-05-06, -06-03, CAP-04 sin cambiar | Cambiar los textos. AC-SCN-05-06: "pedalera y recorrido de `RiggingFit` (≈ −0,17 m y ≈ 0,54 m para 1,85 m)", con los 1,15 / 0,6 solo en LT-96. AC-SCN-06-03: "cabeceo + 15 % con tope suave para no pasar ± 3 dp". CAP-04: "un par por lado; el `manifest` anota los charcos vivos". Anotar DT7-02 como DV-77 |

## Menores (una línea cada una)

- **QA7-G-06** (po): la tabla de *surge* (8,6 / 14,5 cm) supone el factor de esfuerzo 1 (`e = 0,5`); con el split 2:00 real (≈ 202 W, e ≈ 0,59) da 8,9 / 14,9 cm y pasa la tolerancia de ± 0,2 cm. `BoatMotionTests` usa `e = 0,5` y la spec lo dice.
- **QA7-G-07** (architect): "el `r` del cierre es `DriveRatio(12)`" choca con "r se fija en el catch". Se mantiene el `r` fijado hasta el catch y no se cambia a mitad de palada.
- **QA7-G-08** (architect): las citas de líneas del legacy en §2.1 (`scene_state.dart:56-137`, `:82`, `:103-136`, `:136`) no existen, porque el archivo tiene 87 líneas. Las buenas son `:23`, `:31`, `:50-86` y `:85`.
- **QA7-G-09** (architect): el 0 B de `SceneDataIsolationTests` con el contador del hilo sirve en dotnet solo como apoyo. AC-SCN-01-16 se da por bueno únicamente con `FrameAllocations` (con control positivo) y el Profiler del build.
- **QA7-G-10** (po): el tronco visible +28° / −18° no es el +25° / −20° del contrato (que sigue en `StrokeCycle.TorsoLean`) y no tiene DV. Se suma a DV-74.
- **QA7-G-11** (architect): DT7-03 da al maniquí las proporciones de Y Bot. Si B0 mide a Y Bot fuera de 1,80–1,88 m (AC-SCN-05-06), se escala a 1,85 m con las mismas proporciones.
- **QA7-G-12** (architect): `com.unity.animation.rigging` queda sin uso en el manifiesto (ya está en "Para issues" del diseño).

## Matriz AC → test (gate)

Todos los AC tienen dueño y prueba en el diseño §6.4. Solo con captura o Editor quedan AC-SCN-08-02 (visual, aceptable) y AC-SCN-08-07 (LT-97/98 en `RowerSceneTests`, igual que el test de pintura del legacy). Lo que depende de esta gate:

| AC | Estado en el gate |
|---|---|
| AC-SCN-04-01 a -03, AC-SCN-05-01 a -05, AC-SCN-07-01 (LT-88 a -96) | OK: tal cual + oráculo |
| AC-SCN-04-04, -05 | OK; el cierre sin paladas depende de QA7-G-03 |
| AC-SCN-05-06 | depende de QA7-G-05 |
| AC-SCN-05-07, AC-SCN-07-02 (lugar del charco) | dependen de QA7-G-02 |
| AC-SCN-06-03 | depende de QA7-G-05 |
| AC-SCN-08-08 (Y Bot), CAP-18, CAP-21, DEMO-06 | dependen de QA7-G-01 |
| AC-SCN-08-09 | depende de QA7-G-04 |
| AC-SCN-05-10 (reposo después del cierre), VID-05 | dependen de QA7-G-03 |
| El resto | OK |

## Para issues

- `Assets/New Text Document.txt` sin versionar en `Assets/`: que no entre en un commit.
- Medir el alto real de Y Bot en B0 y anotarlo en ASSET_SLOTS §2 junto con los 55 k triángulos.
- Revisar en el 6b si conviene un LOD de Y Bot para móvil (55 k con *skinning*).
