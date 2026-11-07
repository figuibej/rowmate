# It. 7 — Remero, bote y palada biomecánica: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-06 |
| Iteración | 7 · `07-remero-palada` ([ROADMAP §It. 7](../../ROADMAP.md)), sobre la escena de la 6a con el **agua degradada** (la 6b quedó postergada, issue #6) |
| Entrada | [01-po-spec](01-po-spec.md) (38 AC, LT-88 a LT-98, CAP-01 a -21, VID-01 a -07, DEMO-01 a -06, DV-68 a -77); [00-arte-remero](00-arte-remero.md) (D7-01 a -10, DA7-01 a -15); [06a/02-tech-design](../06a-escena-lago/02-tech-design.md) (§2.15 `SceneSession`, §3.1 a §3.7 `SceneHost`, cámara y slots, §4 rendimiento, §5 arnés `it6a`); [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §3, §4, §5.2, §7; [ASSET_SLOTS](../../design/ASSET_SLOTS.md) §1, §2 |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy): `scene/stroke_cycle.dart`, `scene/scene_state.dart`, `scene/painters/boat_painter.dart`, `test/scene/{stroke_cycle,scene_state,boat_painter}_test.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales |
| Código leído (este repo) | `Core/Scene/{SceneState,SceneSession,SceneFrame,SceneInputs,SlotIds,ChaseCameraMotion,SceneMath,SceneQuaternion}.cs`, `Core/Scene/Geometry/{MeshData,PropMeshes}.cs`, `Core/Scene/Art/LakeArt.cs`, `Core/Workout/{WorkoutSnapshot,SeriesTracker}.cs`, `Core/Design/{HudGeometry,SeriesPanelGeometry}.cs`, `Scene/{SceneHost,SceneDirector,HorizonRenderer,SceneWorldBuilder}.cs`, `Snapshots/{SnapshotCatalog,SceneCatalog}.cs`, `Tests/Support/{RowerScripts,RoutineSeeds}.cs`, `Packages/manifest.json`, `.gitignore` |
| Estado | Diseño compacto, **sin código**. Modo "entrega rápida": cada duda se resolvió con el legacy, la spec o el valor por defecto, en una línea (§8). Orden: **WP-A** → **WP-B0** → **WP-D1** → **WP-B1** → **WP-B2** → **WP-D2** (§7) |

Convenciones: las de la 6a. Rutas relativas a `Assets/RowMate/` salvo `tools/`, `docs/`, `Packages/`, `ProjectSettings/`, `Assets/ThirdParty/`. Marcas: **N** nuevo, **C** cambia, **G** generado. Mundo: `x` lateral (+ derecha de la pantalla), `y` altura sobre el agua, `z` adelante desde la cámara; pernos en `z` 9,5. **Marco del bote**: origen en el punto medio entre pernos al nivel del agua (`z` mundo 9,5), `+z` proa, `+y` arriba; el remero mira a `−z` (la cámara). `p` = fase del contrato, `φ` = fase visual, `c` = fase canónica, `e` = esfuerzo (spec §0).

---

## 0. Resumen

1. **Toda la palada es Core puro** (`Core/Scene/Stroke/`): el ciclo del legacy (`StrokeCycle`), la fase del contrato con sus charcos y el cabeceo dentro de `SceneState` (LT-88 a LT-91 sin cambiar un número), la fase visual (`VisualStrokeClock`), el ritmo por SPM (`StrokeRhythm`), la pose 3D del remero y de los remos (`ScullingKinematics`, que resuelve también el IK de dos huesos), el ajuste al cuerpo (`RiggingFit`), el bote (`BoatMotion`, `BoatSurge`), los charcos que se ven (`PuddleField`), la salpicadura y las gotas (`StrokeSpray`), la boya (`BuoyTracker`), la sombra (`BoatShadowRule`) y la huella del remero en pantalla (`RowerFootprint`). `SceneSession.Tick` lo avanza todo sin asignar y lo deja en `SceneFrame.Rower`.
2. **El remero se mueve sin Animator ni Animation Rigging** (DT7-01): Core da las posiciones de las articulaciones y los ejes laterales de cada segmento; `RowerRigDriver` orienta cada hueso hacia su hijo desde la pose de reposo medida al armar. Con eso: los contactos salen exactos por construcción, el arnés en modo edición es determinista, es el mismo código para el maniquí y para Y Bot, y se prueba casi todo con dotnet.
3. **Maniquí por defecto, Y Bot por slot** (`rower.character`): `ProceduralRower` arma el esqueleto `mixamorig:*` del cuerpo y una malla con *skinning* desde Core (≤ 6 k triángulos). Si `Assets/ThirdParty/Mixamo/RowerRealistic/RowerRealistic.fbx` o `.../YBot/YBot.fbx` está y valida (Humanoid, dedos, alto 1,55–2,05 m, ≤ 60 k triángulos, ≤ 75 huesos; Y Bot tiene 55,3 k y 65 huesos, QA7-G-01), se usa ese, con la misma pose. Si falta o no valida: el maniquí y **un** aviso `[SLOT]`. Un `AssetPostprocessor` importa los FBX de Mixamo como Humanoid sin que el usuario toque nada (DT7-04). **Y Bot ya está en la máquina de desarrollo.** De Mixamo no se versiona nada, ni los `.meta`: el slot versionado queda en `Procedural` y la referencia al FBX vive en un *binding* generado e ignorado (DT7-13, QA7-G-04).
4. **Medidas reales** (D-ART-04) con la pedalera y el recorrido del carro de `RiggingFit` (DA7-01): los 1,15 m y 0,6 m del legacy quedan solo en `LegacyBoatGeometry` (LT-96), porque con remos reales son imposibles (DT7-02).
5. **El bote es una jerarquía** `boat-root` (cinta, `z` 9,5 + *surge*) → `boat-pitch` (cabeceo, arfada, rolido sobre el perno) → casco, herrajes, remos, remero (D-ANI-03). El óvalo, los charcos y la boya son del agua y no cabecean.
6. **Nada toca los datos** (AC-SCN-08-11): todo sale de la foto del motor (`SceneInputs`, que suma vatios y el estado de las series); el *surge* es un desfase de dibujo y `SceneState.Distance` no lo ve.
7. **Efectos instanciados y deterministas**: charcos, hervideros y el anillo de la boya en 1 *draw call*; salpicadura y gotas en un *pool* propio de Core dibujado en 1 *draw call* (DT7-05, en lugar de `ParticleSystem`).
8. **La cámara suma los términos del bote** con el tope de ± 3 dp como regla dura (DT7-06: en W1280, TH y L640 la respiración de la 6a ya usa 2,93 de 3 dp, así que el 15 % del cabeceo pasa por un tope suave).
9. **Presupuesto**: ≈ +9 *draw calls*, +11 k triángulos con el maniquí (+56 k con Y Bot, QA7-G-01), ≈ +0,7 ms de GPU y ≈ +0,15 ms de CPU sobre la 6a; 0 B por cuadro (§4).
10. **Arnés `it7`** (`RowerCatalog`, por defecto desde la 7): 21 capturas, 7 videos y 6 tomas de demo, dos corridas iguales byte a byte, con la cámara lateral de prueba y los controles de fase y personaje (§5).

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| **DT7-01** | Solver propio (Core: posiciones e IK de 2 huesos; Unity: orientar huesos) en lugar de Animation Rigging o el IK del `Animator` (ANIMATION_BIBLE §0.3 y §7.3). Los números de la biblia no cambian. El paquete `com.unity.animation.rigging` queda en el manifiesto sin uso (Para issues) |
| **DT7-02** | `AC-SCN-05-06` dice "pedalera a 1,15 m, recorrido 0,62": es imposible con remos reales (la cadera quedaría en −0,79, detrás del puño en −0,72). Manda `RiggingFit`: talones ≈ −0,17, recorrido ≈ 0,54 para 1,85 m; rieles de 0,72 m |
| **DT7-03** | Proporciones del maniquí: la spec (AC-SCN-08-08) pide "las de Y Bot" y el arte (D7-03) una tabla de 1,85 m. **Manda la spec**: WP-B0 mide los huesos de Y Bot (ya está en la máquina) y WP-A los fija en `RowerSpec.Mannequin`, redondeados al cm. Hasta entonces, la tabla del arte §2.1. Si Y Bot mide fuera de 1,80–1,88 m (AC-SCN-05-06), el maniquí se escala a 1,85 m con las mismas proporciones (QA7-G-11) |
| **DT7-04** | `MixamoImportPostprocessor` (Editor) fuerza para `Assets/ThirdParty/Mixamo/**/*.fbx`: Humanoid, *Create From This Model*, *Optimize Game Objects* apagado, sin animación ni cámaras ni luces, **Read/Write encendido** (lo necesita `RowerOutfitBaker`; contradice ASSET_SLOTS §2.5, que se corrige) y sin materiales importados. El usuario solo copia el FBX |
| **DT7-05** | Salpicadura y gotas: `StrokeSpray` (Core, ≤ 128 partículas balísticas con semilla fija) dibujado con `RenderMeshInstanced`, no `ParticleSystem` (DA7-09): deterministas en el arnés, probadas con dotnet, 0 B |
| **DT7-06** | Cámara: los términos del bote (arfada 25 %, *surge* 30 %, cabeceo 15 %) entran con sus τ; el **cabeceo** pasa por un tope suave `B·tanh(x/B)` con `B` = lo que le queda al cuadro hasta 2,95 dp. En TV, V390 y P360 entra entero; en W1280, TH y L640 queda casi en 0. Las traslaciones no mueven el horizonte |
| **DT7-07** | Agarre de Y Bot: una tabla de ángulos por falange en Core (`GripPose`) aplicada como rotación local de los dedos al armar; sin `RowerGripPose.anim` ni `RowerPoseClipBuilder` (DA7-12) |
| **DT7-08** | Sombra: `BoatShadowRule.Mode` = **mapa** si el nivel tiene sombras y la fuerza de sombra de la luz del cuadro es ≥ 0,05; si no, **el óvalo del legacy**. Nunca los dos. En Bajo, siempre el óvalo (AC-SCN-08-06); en Medio de noche la fuerza es 0 (6a §2.7), así que también el óvalo |
| **DT7-09** | El maniquí lleva **pelo** `#3E2723` (el del legacy) y anteojos; la gorra (`rower.cap`) queda apagada por defecto (AC-SCN-08-02 dice "piel y pelo"; el arte la encendía) |
| **DT7-10** | CAP-04: con `R-4` (4 m/s, 10 m por palada) el par anterior ya pasó la cámara (DA7-15): la toma muestra **un** par por lado y el `manifest` anota los charcos vivos. Se toma en la primera salida de pala después de los 59,2 s, + 1 s |
| **DT7-11** | La boya pasa ± 1 s (la spec), no ± 0,5 m (el arte): con `R-4` son ± 4 m |
| **DT7-12** | Un solo arreglo de efectos de agua instanciados de **16** (12 charcos + 2 hervideros + 1 anillo de boya + 1 libre), no 12 (DA7-08) |
| **DT7-13** | Mixamo fuera del repo, `.meta` incluidos (QA7-G-04): `.gitignore` ignora `Assets/ThirdParty/Mixamo/` y `Assets/ThirdParty/Mixamo.meta`; `Slots/rower.character.asset` se versiona **siempre** en `Procedural` con sus `expectedPaths`. En el Editor, `RowerCharacterBindingBuilder` busca esas rutas con `AssetDatabase` (en el *bootstrapper* y antes de cada build) y escribe `Bootstrap/Generated/Resources/RowerCharacterBinding.asset` (ignorado, como `StravaSecrets.g.cs`) con la referencia al FBX encontrado, o lo borra si no hay ninguno. En ejecución, el resolver lo carga con `Resources.Load`; "asignado" = hay *binding* |

---

## 1. Árbol de archivos

### 1.1 Core (`RowMate.Core`, puro) — WP-A

| Archivo | Marca | Contenido (fuente legacy) |
|---|---|---|
| `Core/Scene/StrokeCycle.cs` | N [A] | `StrokeCycle` (`stroke_cycle.dart:5-51`) |
| `Core/Scene/SceneState.cs` | C [A] | + `StrokePhase`, `Pitch`, `Puddles`, `IdleSpm`, `PuddleLife`, `OarLength`, `RiggerX` (`scene_state.dart:18-31`, `:50-86`) (§2.1) |
| `Core/Scene/SceneInputs.cs` | C [A] | + `PowerWatts`, `Buoy` (`BuoyInputs` desde `snapshot.Series`) |
| `Core/Scene/SceneSession.cs`, `SceneFrame.cs` | C [A] | avanza la palada y deja `SceneFrame.Rower`; controles de prueba nuevos (§2.12) |
| `Core/Scene/ChaseCameraMotion.cs` | C [A] | `AddBoatTerms` y el tope de DT7-06 (§2.6) |
| `Core/Scene/Stroke/SceneVec3.cs` | N [A] | vector `double` mínimo (suma, resta, escala, punto, cruz, normalizar, rotar con `SceneQuaternion`) |
| `Core/Scene/Stroke/LegacyBoatGeometry.cs` | N [A] | `SeatZ`, `Handle`, `BladeTip` (`boat_painter.dart:25-56`) |
| `Core/Scene/Stroke/StrokeRhythm.cs`, `VisualStrokeClock.cs`, `StrokeEvents.cs` | N [A] | §2.2 |
| `Core/Scene/Stroke/EffortModel.cs`, `RowerIdle.cs` | N [A] | §2.3 |
| `Core/Scene/Stroke/BoatSpec.cs`, `RowerSpec.cs`, `RiggingFit.cs` | N [A] | §2.4 |
| `Core/Scene/Stroke/ScullingKinematics.cs`, `ScullingPose.cs`, `OarCurves.cs`, `TwoBoneIk.cs`, `GripPose.cs` | N [A] | §2.5 |
| `Core/Scene/Stroke/BoatMotion.cs`, `BoatSurge.cs` | N [A] | §2.6 |
| `Core/Scene/Stroke/PuddleField.cs`, `StrokeSpray.cs` | N [A] | §2.7, §2.8 |
| `Core/Scene/Stroke/BuoyTracker.cs`, `BoatShadowRule.cs`, `RowerFootprint.cs` | N [A] | §2.9 a §2.11 |
| `Core/Scene/Stroke/RowerFrame.cs` | N [A] | lo que se dibuja del remero, el bote y el agua en un cuadro (§2.12) |
| `Core/Scene/Geometry/RowerMeshes.cs` | N [A] | `MannequinSkeleton` (huesos `mixamorig:*`, padres, posiciones de reposo) y `MannequinMesh` (*loft* de superelipse con pesos de 2 huesos, regiones por color de vértice) en `SkinnedMeshData` |
| `Core/Scene/Geometry/SkinnedMeshData.cs` | N [A] | `MeshData` + índices y pesos de hueso (2 por vértice) |
| `Core/Scene/Geometry/BoatMeshes.cs` | N [A] | `OarMesh` (≤ 1,2 k), `BoatFittingsMesh` (riggers, carro, rieles, pedalera; ≤ 2 k), `BuoyMesh` (≤ 400) |
| `Core/Scene/Geometry/PropMeshes.cs` | C [A] | `HullMesh`: el *cockpit* de `z` 9,05 a 10,45 (DA7-02) |
| `Core/Scene/Art/RowerArt.cs` | N [A crea, D dueño] | colores y suavidades por región, proporciones del maniquí, tamaños mínimos en pantalla, constantes de efectos (arte §2, §5, §8, §9) |
| `Core/Scene/Art/LakeArt.cs` | C [A] | constantes del *cockpit* |
| `Core/Scene/SlotIds.cs` | C [A] | + `rower.character`, `rower.outfit`, `rower.cap`, `fx.splash`, `fx.drips`, `fx.ripple`, `prop.buoy500` |
| `Core/Design/MotionSpecs.cs` | C [A] | el bloque de la 7: τ de `φ`, de `r`, de `e`, de la respiración en reposo y de los términos de la cámara; fundido de la boya (0,6 s) y del lado del óvalo (1,5 s) |

### 1.2 Scene (`RowMate.Scene`, Unity) — WP-B (shaders de WP-D)

| Archivo | Marca | Contenido |
|---|---|---|
| `Scene/Rower/RowerRigDriver.cs` | N [B1] | mide la pose de reposo, orienta los huesos desde `ScullingPose`, aplica el agarre (§3.2) |
| `Scene/Rower/RowerBones.cs` | N [B1] | las 24 referencias de hueso (cuerpo) + dedos, desde nombres `mixamorig:*` o desde `HumanDescription` |
| `Scene/Rower/ProceduralRower.cs` | N [B1] | crea el maniquí desde `MannequinSkeleton` y `MannequinMesh` (`SkinnedMeshRenderer`, 1 material) |
| `Scene/Rower/RowerCharacterResolver.cs` | N [B1] | resuelve el slot, valida, instancia, cae al maniquí con un aviso (§3.2) |
| `Scene/Rower/RowerOutfitBaker.cs` | N [B1] | Y Bot: copia de la malla con la región por hueso dominante y las franjas por `|normal.x|` (DA7-05) |
| `Scene/Slots/RowerCharacterSlot.cs` | N [B0] | `AssetSlot<GameObject>` con `expectedPaths` (realista, Y Bot); el versionado queda en `Procedural` (DT7-13) |
| `Scene/Slots/RowerCharacterBinding.cs` | N [B0] | `ScriptableObject` con `GameObject Model` y `string SourcePath`; lo carga el resolver con `Resources.Load` (DT7-13) |
| `Scene/Boat/BoatVisual.cs` | N [B1] (sale de `HorizonRenderer.cs`) | la jerarquía del bote, el *surge*, el cabeceo, la arfada, el rolido; el óvalo con su lado |
| `Scene/Boat/OarRig.cs` | N [B1] | los dos remos (barrido, ángulo vertical, plumeo, flexión `_RmOarLoad`) |
| `Scene/Water/WaterInteractionRenderer.cs` | N [B1] | charcos, hervideros y el anillo de la boya: 16 instancias, 1 *draw call* |
| `Scene/Water/StrokeVfx.cs` | N [B1] | dibuja `StrokeSpray` (≤ 128 instancias, 1 *draw call*) |
| `Scene/Water/MarkerBuoy.cs` | N [B1] | la boya, su vaivén y su fundido |
| `Scene/HorizonRenderer.cs` | C [B1] | sin `BoatVisual` |
| `Scene/SceneDirector.cs`, `SceneHost.cs`, `SceneWorldBuilder.cs` | C [B1] | el remero en el armado (paso nuevo, en tramos), en los `Tick` sintéticos y en el dibujo de calentamiento; los controles de prueba (§3.6) |
| `Scene/SceneShaderIds.cs` | C [B0] | los nombres nuevos (§7.4) |
| `Scene/SceneLighting.cs` | C [B1] | expone la fuerza de sombra del cuadro para `BoatShadowRule` |

### 1.3 Editor, Bootstrap, arnés, pruebas y herramientas

| Archivo | Marca | Contenido |
|---|---|---|
| `Editor/Import/MixamoImportPostprocessor.cs` | N [B0] | DT7-04 |
| `Editor/Bootstrap/SceneAssetsBuilder.cs` | C [B0] | crea `Slots/rower.character.asset` **siempre** en `Procedural` con sus `expectedPaths` (igual en todas las máquinas), reimporta los FBX de Mixamo con el postprocesador y llama a `RowerCharacterBindingBuilder` |
| `Editor/Slots/RowerCharacterBindingBuilder.cs` | N [B0] | DT7-13: escribe o borra el *binding*; también como `IPreprocessBuildWithReport` (el build lleva Y Bot solo si está en esa máquina) |
| `Editor/Bootstrap/SceneMaterialsBuilder.cs` | C [B1] | materiales `Rower`, `Oar`, `Spray`, `WaterMarks`, `Buoy`, `BoatFittings` y sus variantes en la colección |
| `Editor/Slots/SlotValidator.cs` | C [B1] | lista `rower.character` con su resultado |
| `Slots/rower.character.asset` | G [B0] | versionado, en `Procedural`, sin referencia a ningún GUID de Mixamo (AC-SCN-08-09) |
| `Bootstrap/Generated/Resources/RowerCharacterBinding.asset` | G [B0] | **ignorado** (carpeta `Bootstrap/Generated/` ya en `.gitignore`); solo existe si la máquina tiene un FBX de Mixamo |
| `.gitignore` | C [A] | `/Assets/ThirdParty/Mixamo/` y `/Assets/ThirdParty/Mixamo.meta` después de las excepciones de `.meta` (DT7-13) |
| `Art/Shaders/Scene/{Rower,Oar,Spray,WaterMarks,Buoy}.shader` | N [D1] | §7.4 |
| `Art/Shaders/Scene/{WaterDegraded,Hull,BoatShadow}.shader`, `Art/Shaders/Include/RowMateLighting.hlsl` | C [D1] | el agua recibe la sombra (DA7-04); los herrajes por color de vértice; el relleno del remero |
| `Snapshots/RowerCatalog.cs`, `Snapshots/RowerMarks.cs` | N [B2] | el conjunto `it7`; la regla de 1 m y la hoja de poses |
| `Snapshots/{SnapshotCatalog,SnapshotCli,SnapshotRunner,ScenarioScript}.cs` | C [B2] | `-snapshotSet it7` (por defecto), la cámara lateral, la fase y el personaje forzados |
| `tools/unity/capture-snapshots.ps1`, `tools/unity/README.md` | C [B2] | `-Set it7` |
| `tools/oracle/engine/test/stroke_vectors_test.dart`, `boat_legacy_images_test.dart`, `tools/oracle/run-oracle.ps1` | N/C [A] | `-Stroke`: vectores de `StrokeCycle`, `SceneState` y `BoatPainter` y LEG-01/LEG-02 (§6.2) |
| `Tests/Golden/oracle/scene_stroke.json` | G [A] | sus vectores |
| `Tests/EditMode/Scene/Stroke/**` | N [A] | §6.3 |
| `Tests/EditMode/Traceability/It07Ids.cs` | N [A] | 38 ID + LT-88 a LT-98 |
| `Tests/EditMode/Project/{PureCodeRulesTests,RepoHygieneTests}.cs` | C [A] | `Core/Scene/Stroke` sin `UnityEngine` ni reloj; nada de `Assets/ThirdParty/**` versionado salvo `.meta`, y **nada** bajo `Assets/ThirdParty/Mixamo` (ni `.meta`); `rower.character.asset` en `Procedural`; ningún asset versionado referencia un GUID de Mixamo (DT7-13) |
| `Tests/UnityEditMode/{MixamoImportTests,RowerMaterialsTests}.cs` | N [B0, B1] | §6.3 |
| `Tests/PlayMode/Scene/{RowerRigTests,RowerSlotTests,RowerSceneTests,RowerOcclusionTests}.cs` | N [B1] | §6.3 |
| `Tests/PlayMode/Scene/SceneBudgetTests.cs`, `Tests/PlayMode/HudBudgetTests.cs`, `Tests/UiSupport/FrameAllocations.cs` | C [B1] | con el remero |
| `SimulatorPresentation/FrameTimeProbe.cs`, `Bootstrap/AppRoot.cs` | C [B2] | etiquetas `[PERF]` de la 7 (libre `Fuerte`, `Series 600` con los dos paneles) |
| `docs/migration/design/{ASSET_SLOTS,ANIMATION_BIBLE}.md` | C [D2] | DT7-01, -04, -07 |

**Asmdefs**: ninguno nuevo; ninguna referencia nueva (`RowMate.Scene` no referencia `Unity.Animation.Rigging`). **Manifest**: sin cambios.

---

## 2. Core puro (WP-A)

Reglas de la 6a: `noEngineReferences`, C# 9, sin asignar en `Tick` (todo se reserva al construir), cultura invariante, mismo orden de operaciones que el legacy en lo que es contrato.

### 2.1 El contrato: `StrokeCycle` y `SceneState` (LT-88 a LT-96)

```csharp
// Core/Scene/StrokeCycle.cs — stroke_cycle.dart:5-51, sin cambiar nada
public static class StrokeCycle
{
    public const double CatchSweep = -55 * Math.PI / 180, FinishSweep = 35 * Math.PI / 180;
    public static double Legs(double p);        // :15-18
    public static double Back(double p);        // :21-24
    public static double Arms(double p);        // :27-30
    public static double TorsoLean(double p);   // :33-34 (+25° → −20°)
    public static bool BladeInWater(double p);  // :36  p < 0,38
    public static bool BladeFeathered(double p);
    public static double OarSweep(double p);    // :41-45
    public static double Ramp(double p, double a, double b);  // :47-48
}

// Core/Scene/SceneState.cs (C) — scene_state.dart:18-31, :50-86 (lib/features/workout/scene/)
public struct Puddle { public double X; public double WorldZ; public double Age; }
public sealed class PuddleRing { public int Count; public ref readonly Puddle this[int i]; /* 16 lugares, orden de llegada */ }

public sealed class SceneState
{
    public const double IdleSpm = 12, PuddleLife = 4.0, OarLength = 2.9, RiggerX = 0.85; // :18-23
    public double StrokePhase { get; private set; }   // p
    public double Pitch { get; private set; }         // :85, con p (contrato; lo visual usa φ, §2.6)
    public PuddleRing Puddles { get; }                // :30, la lista del contrato (no se dibuja); _bladeWasIn = true (:31)
    public void Update(double dt, double pace500m, double spm, bool isActive); // misma firma; :59-85 después de la distancia
    internal void ForceStrokeForTest(double phase);   // los tests legacy fijan strokePhase
}

// Core/Scene/Stroke/LegacyBoatGeometry.cs — boat_painter.dart:25-56 (solo LT-96 y la lista del contrato)
public static class LegacyBoatGeometry
{
    public const double Inboard = 0.9, FeetZ = SceneCamera.BoatZ - 1.15, HipY = 0.25;
    public static double SeatZ(double p);
    public static SceneVec3 Handle(double side, double p);
    public static SceneVec3 BladeTip(double side, double p);
}
```

- Orden exacto de `Update` (`:103-136`): paso acotado, tiempo, velocidad, distancia (como la 6a), fase (`:110-116`), charcos al soltar con `_bladeWasIn` que **empieza en `true`** (`:82`), envejecer, borrar los de `age > 4`, cabeceo.
- `PuddleRing` en lugar de `List<Puddle>`: mismo orden y mismo borrado (`removeWhere` conserva el orden), sin asignar. Con más de 16 vivos (imposible por debajo de 120 spm) se pisa el más viejo.

### 2.2 Ritmo y fase visual (AC-SCN-04-04, -05; ANIMATION_BIBLE §3.1, §3.2, §3.7)

```csharp
public static class StrokeRhythm
{
    public static double DriveSeconds(double spm);                  // clamp(1,05 − 0,0194·(spm − 18); 0,60; 1,15)
    public static double DriveRatio(double spm);                    // clamp(T_d·spm/60; 0,22; 0,48)
    public static double ToCanonical(double phase, double ratio);   // 0,40·φ/r | 0,40 + 0,60·(φ − r)/(1 − r)
    public static double FromCanonical(double canonical, double ratio);
}

[Flags] public enum StrokeEvents { None = 0, Catch = 1, BladeEntry = 2, BladeExit = 4, Finish = 8 }

public sealed class VisualStrokeClock
{
    public double Phase { get; }          // φ ∈ [0; 1)
    public double Canonical { get; }      // c
    public double LatchedRatio { get; }   // r, fijado al cruzar φ = 0
    public double PhaseSpeed { get; }     // ω_s
    public double StillSeconds { get; }   // segundos con ω_s < 0,01 y c en el catch o en pausa (para RowerIdle)
    public StrokeEvents Step(double dt, double contractPhase, double spm, bool active);
    public void Reset();
    internal void ForceCanonicalForTest(double? c);   // "fase forzada": φ = FromCanonical(c, r actual), ω_s = 0
}
```

- `ω_s` sigue a `spm/60` con τ 0,5 s activo y cae a 0 con τ 0,15 s en pausa; `φ' = ω_s + 4·wrap(p − φ)` con `φ' ≥ 0`; integración semi-implícita con el `dt` acotado de la escena.
- **Cierre sin paladas** (activo con spm 0, haya terminado `p` o no; QA7-G-03): el objetivo de `ω_s` es `12/60` (no 0) hasta que `φ` cruza 0; en ese cuadro `φ = 0` y `ω_s = 0` exactos, y quedan así mientras siga spm 0 (traba del catch). Si `φ` cruza antes que `p` (p. ej. spm cae a 0 con `p` = 0,99), la traba lo deja en 0 y el término `4·wrap(p − φ)` negativo queda en 0 por `φ' ≥ 0`. No se ve el corte: todas las curvas tienen derivada 0 en el catch. Con spm > 0 la traba se suelta y `ω_s` sube con τ 0,5 s. La pausa no cambia.
- El SPM de `r` pasa por un filtro τ 0,8 s; `r` se fija cuando `φ` cruza 0 (el evento `Catch`) y **no cambia a mitad de palada**: la palada que se cierra sin paladas conserva su `r`; el filtro tiende a 12 spm y el `r` siguiente (≥ `DriveRatio(12)` = 0,230) recién se fija en el próximo catch (QA7-G-07).
- Eventos una vez por palada: `BladeEntry` y `BladeExit` cuando el centro de pala visible (`OarCurves.BladeCenterY(c)`, §2.5) cruza 0 bajando/subiendo; `Finish` cuando `c` cruza 0,36. Sin eventos con `ω_s = 0` (pausa).

### 2.3 Esfuerzo y reposo (AC-SCN-05-10, AC-SCN-08-12)

```csharp
public struct EffortFilter { public double Value; public void Step(double dt, int watts); } // e = clamp((W − 60)/240; 0; 1), τ 1 s
public sealed class RowerIdle
{
    public double Weight { get; }        // 0 → 1 en 0,4 s después de 1,5 s quieto; 1 → 0 en 0,2 s al moverse
    public double ChestOffsetM { get; }  // ± 0,006 · sin(2π·t·15/60)
    public double ShoulderOffsetM { get; } // ± 0,005, en fase
    public double GazeYawDeg { get; }    // ± 2°, un objetivo nuevo cada 4–7 s (DartRandom(7)), resorte crítico 0,6 s
    public void Step(double dt, double stillSeconds, double sceneTime);
}
```

La respiración se suma a la pose; nunca toca `p` ni `φ`.

### 2.4 Medidas y ajuste al cuerpo (AC-SCN-05-06; DA7-01)

```csharp
public readonly struct BoatSpec   // marco del bote; D-ART-04, ART_BIBLE §7.8, arte §5
{
    public static BoatSpec Real { get; }
    public double PinX { get; }            // 0,80
    public double PinY { get; }            // 0,31
    public double OarLength { get; }       // 2,88
    public double Inboard { get; }         // 0,88
    public double GripFromPin { get; }     // 0,82
    public double BladeFromPin { get; }    // 1,76
    public double BladeLength { get; }     // 0,47
    public double BladeWidth { get; }      // 0,22
    public double SeatTopY { get; }        // 0,14
    public double RailLength { get; }      // 0,72
    public double StretcherAngleDeg { get; } // 42
    public double HullStern { get; }       // 5,3 (mundo) · HullBow 13,5 · manga 0,29 / 0,33
}

public readonly struct RowerSpec   // largos de hueso medidos del rig (o la tabla del maniquí)
{
    public static RowerSpec Mannequin { get; }   // DT7-03
    public double HipHalfWidth, HipAboveSeat, TrunkLength, NeckLength, HeadHeight, ShoulderHalfWidth,
                  UpperArm, Forearm, HandToGrip, Thigh, Shin, AnkleHeight, FootLength, Height;
    // + los largos parciales de Spine, Spine1, Spine2 (para repartir el tronco)
}

public readonly struct RigFit { public double HeelZ, HipZCatch, HipZFinish, Travel, ArmExtensionCatch, GripToRibsFinish, ShinTiltCatchDeg; }

public static class RiggingFit
{
    // Busca (bisección, ≤ 40 iteraciones, sin asignar) la pedalera para que en el catch las canillas queden verticales (± 8°)
    // y los brazos al 98,5 % (≥ 97 %); la cadera del finish con las piernas a 178°. Devuelve también los controles.
    public static RigFit Solve(in BoatSpec boat, in RowerSpec rower);
}
```

Prueba (DA7-01): con `RowerSpec.Mannequin` (1,85 m), talones −0,17 ± 0,05, cadera +0,18 / +0,72 ± 0,05, brazos ≥ 97 %, puño a ± 3 cm de las costillas bajas en el finish; lo mismo con un rig de 1,75 m (los largos × 1,75/1,85).

### 2.5 La pose: `ScullingKinematics` (AC-SCN-05-01 a -09, AC-SCN-08-03, -12)

```csharp
public static class OarCurves   // ANIMATION_BIBLE §3.6; cada tramo smooth, C1
{
    public static double VisualSweepDeg(double c);   // −62° + 104°·(α_legacy + 55°)/90° (catch −62°, finish +42°; QA7-G-02), con α_legacy = StrokeCycle.OarSweep(c)
    public static double BladeCenterY(double c);     // −0,13 | → +0,05 (0,355–0,395) | +0,09 | → +0,17 (0,80–0,93) | → −0,13 (0,97 → 1,015, c' = c + 1 si c < 0,5)
    public static double FeatherDeg(double c);       // 0 → 90 (0,385–0,43); 90 → 0 (0,84–0,95)
    public static double ShaftBendM(double c, double effort); // (0,02 + 0,04·e)·sin(π·c/0,36) en el drive, 0 después
    public static double GripY(in BoatSpec b, double bladeY);  // y_pin + (y_pin − y_pala)·0,82/1,76
}

public static class TwoBoneIk
{
    // Rodilla o codo: con la raíz, el objetivo, los dos largos y el hint; si no llega, estira a lo largo (sin NaN).
    public static SceneVec3 Solve(in SceneVec3 root, in SceneVec3 target, double upper, double lower, in SceneVec3 hint);
}

public struct ScullingPose   // marco del bote; se reserva una vez (arreglos de 24 articulaciones)
{
    public SceneVec3[] Joints;      // índice = RowerJoint (Hips, Spine, Spine1, Spine2, Neck, Head, HeadTop, L/R Shoulder,
                                    // Arm, ForeArm, Hand, GripPoint, UpLeg, Leg, Foot, ToeBase)
    public SceneVec3[] Lateral;     // eje lateral de cada segmento (para el giro del hueso alrededor de su eje)
    public SceneVec3 SeatPosition;  // carro
    public double TrunkDeg;         // + hacia popa
    public OarState Left, Right;    // por remo: SweepDeg, PitchDeg (vertical), FeatherDeg, BladeCenter (SceneVec3), Grip (SceneVec3), ShaftBendM
    public SceneVec3 GazeTarget;
}

public static class ScullingKinematics
{
    public static void Evaluate(double canonical, double effort, in BoatSpec boat, in RowerSpec rower, in RigFit fit,
                                in RowerIdleSample idle, ref ScullingPose pose);
}
```

Cómo resuelve (ANIMATION_BIBLE §3.3 a §3.6; arte §4):

1. **Remos**: `α = VisualSweepDeg(c)`; puño en planta `x = ±(0,80 − 0,82·cos α)`, `z = 0,82·sin α`; `y = GripY(BladeCenterY(c))`; pala `x = ±(0,80 + 1,76·cos α)`, `z = −1,76·sin α`. **Cruce** con `|α| < 25°`: la mano izquierda del remero (`x > 0`, D7-02) +2,5 cm arriba y +1,5 cm a popa, la derecha −2,5 cm, peso `1 − smooth((|α| − 20°)/10°)`.
2. **Carro y cadera**: `hipZ = lerp(HipZCatch, HipZFinish, L(c))` con `L` = `StrokeCycle.Legs(c)`; `hipY = SeatTopY + HipAboveSeat`.
3. **Tronco**: `θ = lerp(+28°, −18° − 2°·e, B(c))` (tabla §3.4, no el ±25/−20 del contrato, que sigue en `StrokeCycle.TorsoLean`); pelvis 60 % + 6° base, Spine 15 %, Spine1 13 %, Spine2 12 %; + la respiración de `RowerIdle`.
4. **Piernas**: `TwoBoneIk` de la cadera al tobillo fijo (`HeelZ` de `RigFit`), hint 0,6 m arriba y ±0,12 m afuera.
5. **Brazos**: `TwoBoneIk` del hombro (con protracción +8° en el catch y retracción −6° en el finish) al punto de agarre, hint del codo afuera 0,35, abajo 0,25, a proa 0,25 m.
6. **Cabeza**: mira `GazeTarget` (30 m a popa, a la altura de los ojos, peso 0,85) + `GazeYawDeg`.

Todo es continuo en `c ∈ [0; 1)` incluido 0,999 → 0. "Pala en el agua" (centro ≤ 0) va de c ≈ 0,99 a c ≈ 0,38 (± 0,01).

### 2.6 El bote y la cámara (AC-SCN-06-01 a -03)

```csharp
public readonly struct BoatMotionSample { public double PitchRad, HeaveM, RollRad, SurgeM; }

public static class BoatMotion
{
    public const double LakeRollK = 0.5;
    public static double Pitch(double phi, double sceneTime);   // 0,02·sin(2πφ) + 0,006·sin(0,8t) (scene_state.dart:85, con φ)
    public static double Heave(double phi);                     // 0,006·sin(2π(φ − 0,08))
    public static double Roll(double sceneTime, double k);      // 0,004·sin(0,9t + 1,3)·k
}

public static class BoatSurge   // ANIMATION_BIBLE §4.5
{
    public static double Amplitude(double spm, double effort);  // clamp(0,075 − 0,001·(spm − 20); 0,05; 0,08)·(0,85 + 0,30·e)
    public static double SpeedFactor(double phi, double ratio); // g, media exacta 0
    public static double OffsetMeters(double phi, double ratio, double amplitude, double meanSpeed, double spmSmoothed);
}

// ChaseCameraMotion (C)
public struct BoatCameraFilter { /* arfada τ 0,2 · surge τ 0,35 · cabeceo τ 0,5; reservado en ChaseCameraDirector */ }
public static CameraPose AddBoatTerms(in CameraPose pose, in SceneCamera cam, in BoatMotionSample filtered, double multiplier);
```

- En pausa la parte de la palada del cabeceo queda fija (porque `φ` se detiene) y el balanceo lento sigue; el *surge* escala con la velocidad media y se apaga con ella.
- `AddBoatTerms` suma `0,25·heave` en `y`, `0,30·surge` en `z` y `0,15·pitch` en el cabeceo, por el mismo multiplicador de la respiración (pausa × 0,6, reducir movimiento × 0, entrada `S(0; 0,6 s)`). El cabeceo pasa por el tope de **DT7-06**: `B = max(0; 2,95 − desvío_sin_bote)/focal` (en radianes), `pitch_cam = B·tanh(x/B)`.
- El remero **sigue remando** con reducir movimiento (D7-09): nada de §2.1 a §2.5 mira ese ajuste.

### 2.7 Charcos que se ven (AC-SCN-07-02, -03; D-ANI-02)

```csharp
public struct VisualPuddle { public double X, WorldZ, Age, Spin, Alpha; public int Side; }
public sealed class PuddleField
{
    public const int Capacity = 12;
    public int Count { get; }
    public ref readonly VisualPuddle this[int i] { get; }
    public void OnBladeExit(in ScullingPose pose, double boatWorldZ);   // 2, primero x < 0; donde está cada pala (± 0,1 m)
    public void Step(double dt, double effort);                         // envejece también en pausa; borra a los 4 s
    public static double Radius(double age);                            // 0,3 → 0,9
    public static double AlphaOf(double age, double effort);            // (1 − edad/4)^1,5·(0,30 + 0,35·e), entrada 0,15 s
    public static bool IsBehindCamera(double relZ);                     // relZ ≤ 0,5
    internal void InjectForTest(double x, double worldZ, double age);   // LT-97/98: los 3 charcos del test legacy
}
```

`boatWorldZ` = distancia de escena + 9,5 + *surge* del cuadro. Solo nacen en `BladeExit` (que no ocurre en pausa) y también en la palada que se cierra a 12 spm. Hervidero: dos manchas atadas a cada pala con `c ∈ [0,02; 0,36]`, alfa `0,25 + 0,35·e` (D7-08; no en Bajo).

### 2.8 Salpicadura y gotas (AC-SCN-08-04, -05; DT7-05)

```csharp
public struct SprayParticle { public float X, Y, Z, Vx, Vy, Vz, Size, Age, Life, Kind; }
public sealed class StrokeSpray
{
    public const int Capacity = 128;
    public int Count { get; }
    public SprayParticle[] Items { get; }                 // compacto: [0; Count)
    public void OnBladeEntry(in ScullingPose pose, double boatWorldZ, double effort, double particleScale);
        // por pala: 8 + 14·e gotas (≥ 6), arriba 1,4 + 1,0·e m/s, a proa 0,6, afuera 0,3, cono 35°, 0,02–0,05 m, vida 0,35–0,6 s
        // + la corona (Kind = 1): 0,25 × (0,12 + 0,18·e) m, 0,18 s
    public void EmitDrips(in ScullingPose pose, double boatWorldZ, double u, double dt, double particleScale);
        // de BladeExit a c = 0,55: 50·(1 − u) por segundo y por pala, 0,008–0,015 m, trazos de 0,06 m
    public void Step(double dt);                          // gravedad 9,8; se borran al tocar el agua o al vencer; nunca nacen en pausa
}
```

Semilla fija por palada (`DartRandom(strokeIndex·2 + side)`): dos corridas iguales dan las mismas gotas. `particleScale` = 1 en Alto y Medio, 0,5 en Bajo.

### 2.9 La boya de 500 m (AC-SCN-08-10; arte §9)

```csharp
public readonly struct BuoyInputs { public bool IsWorkStep; public int StepIndex; public int LapCount; public double StepMeters; public long Revision; }
public sealed class BuoyTracker
{
    public bool Visible { get; }  public double WorldZ { get; }  public double Alpha { get; }  public bool Fixed { get; }
    public void Step(double dt, in BuoyInputs inputs, double sceneDistance, double sceneSpeed, double sceneTime);
}
```

- Solo con `IsWorkStep` (rutina, paso de trabajo). Objetivo: `z = D + 9,5 + (500·(LapCount + 1) − d_paso)`, con `d_paso` = `StepMeters` de la última foto **+ lo que avanzó la escena desde esa foto** (las fotos llegan cada segundo).
- Mientras está adelante sigue al objetivo con un resorte crítico τ 0,6 s y una corrección ≤ 0,5 m/s (sin saltos). Cuando `LapCount` sube (el panel cerró la vuelta): **queda fija en el mundo** donde esté y se recicla cuando `relZ < −30`. Se arma la siguiente al quedar fija.
- Aparece con el tramado de la orilla (400 → 360 m). Si el paso termina sin cerrar la vuelta, se funde en 0,6 s; la vuelta que cierra justo al terminar el paso (`LapCount` sube en la misma foto en que `IsWorkStep` cae) también queda fija.
- Prueba: `F-5` (`Series 600`) con `R-4`, el motor real de dotnet (`RowerScripts.R4`, `RoutineSeeds.F5`): `relZ` cruza 9,5 a los 155 ± 1 s; ningún paso de la boya mayor que `v·dt + 0,5·dt`.

### 2.10 La sombra (AC-SCN-08-06; DT7-08)

```csharp
public enum BoatShadowMode { Map, Oval }
public static class BoatShadowRule
{
    public static BoatShadowMode Mode(bool tierHasShadows, double lightShadowStrength);   // DT7-08
    public static double LegacySide(double astroScreenX, double centerX);               // +0,35 si x < centro, si no −0,35 (boat_painter.dart:120)
    public static double OvalAlpha(double ambient);                                     // 0,08·ambiente
}
public struct ShadowSideFader { public double Value; public void Step(double target, double dt, bool instant); } // 1,5 s ease.move
```

El óvalo usa el astro del legacy (`SceneFrame.LegacyLight`, la `x` de pantalla de `LightFor`), igual que el `BoatPainter`.

### 2.11 La huella del remero (AC-WKT-17-14, AC-WKT-18-23)

```csharp
public static class RowerFootprint
{
    // Proyecta con la cámara del contrato (respiración 0) las articulaciones del cuerpo (cabeza, tronco, manos) y, aparte,
    // los remos hasta los pernos; devuelve los rectángulos en dp.
    public static void Body(in SceneCamera cam, in ScullingPose pose, out DpRect body);
    public static void BoatToPins(in SceneCamera cam, in ScullingPose pose, out DpRect boat);
}
```

Con `HudGeometry.Layout` y `SeriesPanelGeometry.Column` (ya en Core) se prueba con dotnet, en los siete tamaños y las 10 poses, que el bote y el remero quedan en la zona y que `Series anteriores` y la llave no tapan `body`.

### 2.12 La sesión (`SceneSession` y `SceneFrame`)

```csharp
public struct RowerFrame                 // SceneFrame.Rower, reservado en SceneFrame.Create
{
    public ScullingPose Pose;  public BoatMotionSample Boat;  public double Phase, Canonical, Ratio, Effort;
    public StrokeEvents Events;  public BoatShadowMode ShadowMode;  public double ShadowSide, ShadowAlpha;
    public PuddleField Puddles;  public StrokeSpray Spray;  public BuoyTracker Buoy;  public bool BoilLeft, BoilRight;
}
```

- `SceneSession` construye una vez `VisualStrokeClock`, `EffortFilter`, `RowerIdle`, `PuddleField`, `StrokeSpray`, `BuoyTracker`, la `RigFit` (con el `RowerSpec` que le pasa `SceneHost` al resolver el personaje: `SetRower(in RowerSpec)`, fuera del `Tick`) y la `ScullingPose`.
- `Tick`, después de `State.Update`: reloj visual → esfuerzo → reposo → pose → bote (*surge* con la velocidad media de `State.Speed`) → eventos (charcos, salpicadura, gotas) → `Step` de efectos → boya → sombra → términos de la cámara. `frame.Distance` sigue siendo `State.Distance` (sin *surge*).
- Controles de prueba (`internal`): `ForceCanonicalForTest(double?)` (fase forzada; congela la pose y los efectos nuevos), `InjectPuddleForTest(...)`, y los de la 6a. `Open` reinicia todo; `NotifyResumed` no dispara eventos (el `dt` acotado no cruza la pala dos veces).

### 2.13 Geometría (`RowerMeshes`, `BoatMeshes`)

- `MannequinSkeleton`: los huesos del cuerpo con nombres `mixamorig:*` (Hips, Spine, Spine1, Spine2, Neck, Head, HeadTop_End, Left/Right Shoulder, Arm, ForeArm, Hand, UpLeg, Leg, Foot, ToeBase, ToeBase_End), en T-pose, con las proporciones de `RowerSpec.Mannequin`. **Sin dedos**: el puño es rígido (Y Bot sí usa los suyos, §3.2).
- `MannequinMesh`: *loft* de superelipse (exponente 2,5, 12 lados, un anillo cada 3–4 cm), pesos lineales en ± 4 cm (± 5 en rodilla y codo), 2 huesos por vértice; regiones en el rojo del color de vértice (0 mono, 1 franjas, 2 panel, 3 piel, 4 medias, 5 anteojos, 6 gorra, 7 articulaciones, 8 pelo); ≤ 6 000 triángulos.
- `OarMesh` (puño, caña interior, manguito, collarín, caña exterior, pala "hacha" con concavidad de 2,5 cm; ≤ 1 200), `BoatFittingsMesh` (ala de carbono por lado, carro, rieles a ± 0,09, pedalera a 42° con zapatillas; posición de la pedalera de `RigFit`; ≤ 2 000), `BuoyMesh` (Ø 0,50 m con la franja; ≤ 400). `HullMesh`: el *cockpit* de 9,05 a 10,45.

---

## 3. Unity (WP-B, shaders de WP-D)

### 3.1 Piezas

```
scene-root
 ├── scene-boat-root      BoatVisual: posición z = 9,5 + surge (cinta)
 │    ├── scene-boat-pitch  pivote en el perno (y 0): cabeceo, rolido, arfada
 │    │    ├── hull           (6a, con el cockpit nuevo)          sombra: proyecta
 │    │    ├── fittings       riggers, carro (se mueve), pedalera   proyecta
 │    │    ├── oar-left/right OarRig                               cañas Off, palas On (DA7-06: submalla de la pala con su propio renderer)
 │    │    └── rower          ProceduralRower o el personaje del slot (RowerRigDriver)   proyecta
 │    └── boat-oval         el óvalo del legacy (solo con BoatShadowMode.Oval; no cabecea)
 ├── scene-water-marks    WaterInteractionRenderer (16 instancias)
 ├── scene-spray          StrokeVfx (≤ 128 instancias)
 └── scene-buoy           MarkerBuoy
```

`SceneDirector.Apply` copia `SceneFrame.Rower` a estas piezas sin asignar. Todo se arma en `SceneWorldBuilder` con un paso nuevo "remero y bote" partido en unidades (mallas una por unidad; el personaje del slot se instancia en una unidad; `RowerOutfitBaker` en otra).

### 3.2 El remero

**Y Bot: para qué** (para explicarle al usuario): es un maniquí humano gratuito de Mixamo con el esqueleto estándar y la piel bien pesada. Sirve para comprobar que el IK y `RiggingFit` funcionan con un cuerpo hecho por otros (si cierra con Y Bot, cierra con cualquier personaje Humanoid), para sacar las proporciones del maniquí (DT7-03) y para que hombros, codos y rodillas deformen mejor. No trae ni usa animaciones: la palada es nuestra. La app funciona entera sin él.

- **`RowerCharacterResolver`** (en el armado; DT7-13): sin *binding* (`Resources.Load<RowerCharacterBinding>` da `null`: CI, clon sin Mixamo) → `ProceduralRower`, sin aviso. Con *binding* → su `Model` (el *builder* elige `RowerRealistic.fbx` antes que `YBot.fbx` si los dos están): valida `Animator.avatar.isValid && isHuman`, los 30 huesos de dedos mapeados en `HumanDescription`, alto (de los pies a `HeadTop_End`) 1,55–2,05 m, **≤ 60 000 triángulos** (suma de `GetIndexCount / 3` de todas las submallas; Y Bot: 34 480 + 20 840 = 55 320; QA7-G-01), ≤ 75 huesos (Y Bot: 65), la malla legible. Si falla o el `Model` del *binding* quedó en `null` (FBX borrado después de generarlo): el maniquí y **un** `[SLOT] rower.character: <motivo>` por `SceneHost`. Nunca un error.
- **`RowerBones`**: para el maniquí, las referencias directas; para el slot, los nombres de `avatar.humanDescription.human` (`HumanTrait` → hueso) buscados bajo la raíz. El `Animator` del personaje queda **deshabilitado** (no hay controlador).
- **`RowerRigDriver.Initialize`** (una vez): mide la pose de reposo (T-pose: la rotación en mundo de cada hueso, la dirección a su hijo y el eje lateral de referencia); mide `RowerSpec` de los huesos (el maniquí ya lo trae) y se lo pasa a la sesión (`SetRower`); aplica `GripPose` a los dedos (Y Bot). El personaje se gira 180° en `y` (mira a la cámara).
- **`RowerRigDriver.Apply`** (cada cuadro): `Hips` a su posición; para cada hueso del cuerpo, `rotación = LookRotation(dir_obj, lat_obj) · inversa(LookRotation(dir_reposo, lat_reposo)) · rotación_reposo`, de la raíz a las puntas; la mano toma el 25 % del plumeo. ≈ 24 huesos, sin asignar, ≤ 0,05 ms.
- **Ropa**: el maniquí, regiones del shader `Rower` (DT7-09). Y Bot: `RowerOutfitBaker` hace una copia de cada submalla con el color de vértice de la región por hueso dominante (`Hips`, `Spine*`, `UpLeg`, `Shoulder` → mono; `Arm`, `ForeArm`, `Hand`, `Leg`, `Neck`, `Head` → piel; `Foot`, `ToeBase` → medias; franjas por `|normal.x| > 0,85` sobre el mono; la submalla de articulaciones → región 7) y le pone `Rower` a las dos.
- **Import (DT7-04)**: `MixamoImportPostprocessor.OnPreprocessModel` para `Assets/ThirdParty/Mixamo/`. El *bootstrapper* reimporta los FBX presentes (el primer import pudo ser con los valores de Unity si alguien abrió el Editor antes). Ni el FBX ni su `.meta` se versionan (DT7-13): cada máquina genera su `.meta` al importar y el postprocesador fija los ajustes, así que el resultado es el mismo en todas.

### 3.3 Remos y bote

- `OarRig`: cada remo es hijo de `scene-boat-pitch` con pivote en su perno; rotación = barrido (eje `y`) · ángulo vertical (de `BladeCenter`) · plumeo (eje del remo); `_RmOarLoad` por `MaterialPropertyBlock` (la flexión). La caña tiene ancho mínimo de 1,2 dp en el *vertex shader* (DA7-07).
- `BoatVisual`: aplica `BoatMotionSample`; el carro se mueve a `SeatPosition`; el óvalo con `ShadowSide` y `ShadowAlpha`, solo en modo `Oval`.
- La luz de la hora: todo con `RowMateLighting` (los colores ya traen el tinte de la hora; nada de `_c()` en C#).

### 3.4 Agua, salpicadura y boya

- `WaterInteractionRenderer`: un `RenderMeshInstanced` de quads horizontales en `y` 0,002 con `Matrix4x4[16]` y `Vector4[16]` reservados (edad, giro, alfa, tipo: charco, hervidero, anillo), shader `WaterMarks` (≤ 20 ALU, 0 texturas, transparente después del agua, sin profundidad); no dibuja los de `relZ ≤ 0,5`.
- `StrokeVfx`: `RenderMeshInstanced` de quads orientados a la cámara (gotas) y estirados por la velocidad (trazos), shader `Spray` con el tamaño mínimo en pantalla (1,5 / 1,0 dp) y el alfa compensado.
- `MarkerBuoy`: la malla y su vaivén (`0,08·sin(1,7t + semilla)`, arfada 0,02 m) con el tiempo de escena; el anillo va en `WaterInteractionRenderer`.
- Sombra con mapa: el agua degradada recibe la sombra de la luz principal (`0,10·ambiente` en sombra plena; WP-D); el casco recibe la del remero.

### 3.5 Integración sin tocar los datos

`SceneInputs.From(snapshot)` suma `Data.PowerWatts` y `BuoyInputs` desde `snapshot.Series`; nada más cambia en el inmersivo, el HUD ni el motor. El HUD no suma elementos (sigue en 56-57 de 60). La llave en L640 y `Series anteriores` en TV y P360 se verifican con `RowerFootprint` (§2.11): si alguno tapa el cuerpo, WP-B corre la llave fuera de la columna del centro (pendiente de la 4a) o baja el panel dentro de su franja, sin cambiar el ancho (DV-50).

### 3.6 Controles de prueba (`internal`, nunca en lo que se instala)

| Control (`SceneHost`) | Para qué |
|---|---|
| `ForceCanonicalForTest(double?)` | fase forzada (CAP-01, -02, -05, -12, -13, -17 a -21) |
| `ForceCharacterForTest(RowerCharacterKind)` | `Mannequin`, `Slot` o `InvalidForTest` (un `GameObject` genérico sin avatar); rearma el remero |
| `SideCameraForTest(bool)` | la cámara lateral: a 4 m a la derecha del remero, 1,0 m de alto, mirando al perno, perspectiva de 40°; apaga la respiración |
| `ShowRulerForTest(bool)` | la regla de 1 m en el perno (capa de depuración, `RowerMarks`) |
| `RowerJointsForTest` | las posiciones en mundo de los huesos después de `Apply` (para los ángulos y contactos medidos en el rig) |

---

## 4. Presupuesto para 60 fps (nivel Medio, sobre la 6a)

| Pieza | *Draw calls* | Triángulos | GPU | CPU |
|---|---|---|---|---|
| Remero (maniquí · Y Bot) | 1 · 2 | 6 k · 55,3 k (tope del slot 60 k) | 0,15 ms (Y Bot ≈ 0,3 ms) | pose + `Apply` ≤ 0,08 ms |
| Remos (2, 1 material) | 2 | 2,4 k | 0,05 | — |
| Herrajes (riggers, carro, pedalera) | 1 | 2 k | 0,03 | — |
| Salpicadura y gotas (≤ 128 instancias) | 1 | < 0,3 k | 0,12 | ≤ 0,04 ms |
| Charcos, hervideros, anillo | 1 | < 0,1 k | 0,10 | — |
| Boya | 1 | 0,4 k | 0,02 | — |
| Óvalo (solo sin mapa) | 0–1 | 2 | 0,02 | — |
| Sombras (remero, palas, casco como emisores) | +3 | ≈ 10 k vueltos a dibujar | 0,15 | — |
| Agua que recibe sombra | 0 | — | 0,10 | — |
| **Suma de la 7** | **≈ +9** | **≈ +11 k** (≈ +56 k con Y Bot) | **≈ +0,7 ms** | **≈ +0,15 ms** |
| **Total con la 6a** | **≈ 96** (tope 150; SetPass ≤ 40) | **≈ 146 k** (≈ 191 k con Y Bot; tope 350 k) | **≈ 11,3 ms** (tope 12) | — |

- Si `[PERF]` lo pide, lo primero que se baja es el hervidero y la distancia de sombras a 30 m. Bajo: partículas × 0,5, sin hervidero, óvalo.
- **0 B por cuadro**: todo reservado al armar; `FrameAllocations.WatchScene` con su control positivo, y el Profiler del build de desarrollo (`-rowmate-perf-scene`).
- `SceneBudgetTests` (DA7-14): ≤ 100 *draw calls* y ≤ 40 SetPass en Medio a las 12 y a las 22 h; ≤ 160 k triángulos con el maniquí, ≤ 205 k con Y Bot (QA7-G-01).

---

## 5. Capturas y videos (WP-B2)

**Reglas**: las de la 6a §5.1 (batchmode sin `-nographics`, compilación sincrónica, `ROWMATE_DATA_DIR` temporal por toma, reloj de prueba 13:00 UTC / UTC−03:00 / 2026-10-28, español, simulador sin ruido, Alto, pasos fijos de 1/60 s, **dos corridas `it7-A` e `it7-B` iguales byte a byte**, `manifest.json` e `INDEX.md` sin rutas de la PC, guardia de `ProjectSettings`), en `docs/migration/iterations/07-remero-palada/snapshots/`. Personaje: el maniquí, salvo las de Y Bot (NO VERIFICABLE si el FBX no está). "Escena sola" = `HideForCapture`, sin degradés, respiración en 0, tiempo de escena 3,2 s, `E-920`. Con hora forzada, la toma 1,6 s después del cambio. **Valores por cuadro de los videos** (`manifest`): `p`, `φ`, `c`, `r`, eventos, esfuerzo, *surge*, cabeceo, charcos vivos, gotas vivas, `relZ` de la boya, vueltas del panel, desvío del horizonte.

| ID | Cómo se arma (`RowerCatalog`) | Sondas en el `manifest` |
|---|---|---|
| CAP-01 | TV, sola, 12:00, `c` = 0,18 | ángulos del rig contra la tabla; centro de pala −0,13 |
| CAP-02 | TV, sola, 12:00, `c` = 0,60 | plumeo 90°, centro de pala +0,09 |
| CAP-03 | TV, sola, 12:00, `R-4`, primera `BladeEntry` después de 30 s + 0,1 s | gotas vivas por pala ≥ 6; tamaño en pantalla ≥ 1,5 dp |
| CAP-04 | TV, sola, 12:00, `R-4`, primera `BladeExit` después de 59,2 s + 1 s (DT7-10) | charcos vivos y su `relZ` |
| CAP-05 a -08 | TH, sola, `c` = 0,5, 09:00 / 12:00 / 17:00 / 22:00 | modo de sombra, lado del astro, lado del óvalo si lo hay |
| CAP-09 | TH, sola, nivel Bajo (`ForceTierForTest` + `TierScope`), 09:00 | óvalo a +0,35 o −0,35, alfa `0,08·ambiente` |
| CAP-10 / -11 | TV / TH, libre `R-4` a 60,2 s, contorno de la zona | rect. del remero y del bote hasta los pernos dentro de la zona |
| CAP-12 / -13 | TV, `Series 600` a 250,2 s, dos paneles abiertos, `c` = 0 / 0,36, contorno | superposición panel ∩ cuerpo = 0 px |
| CAP-14 | L640, libre 60,2 s, con la llave, contorno | superposición llave ∩ cuerpo = 0 px |
| CAP-15 / -16 | TV, `Series 600`, `R-4`, 150,0 s / 155,0 s | `relZ` de la boya; vueltas cerradas (0 / 1) |
| CAP-17 / -18 | cámara lateral, regla de 1 m, las 10 poses (hoja de 5 × 2 armada por el arnés + las 10 sueltas), maniquí / Y Bot | por pose: ángulos medidos del rig y su diferencia con la tabla; contactos (cm); puño, cadera y talones en `z` |
| CAP-19 + LEG-01, LEG-02 | V390, sola, `c` = 0 y 0,36; los LEG del oráculo (§6.2) | posición en pantalla de la cabeza del remero contra la del legacy (dp) |
| CAP-20 / -21 | TV, sola, 12:00, `c` = 0,24, maniquí / Y Bot | personaje usado |
| VID-01 / -02 | TV 12 s, libre `Suave` / `Fuerte` en régimen (desde 20 s) | drive y recuperación medidos entre eventos (± 0,05 s) |
| VID-03 | TV 8 s, `Suave` → `Fuerte` a los 3 s | `r` solo cambia en un `Catch` |
| VID-04 | TH 12 s, `R-4`, `Pausa` a 3 s, `Reanudar` a 8 s | tiempo hasta `ω_s < 0,01`; peso de la respiración desde ≈ 4,8 s |
| VID-05 | TV 10 s, `R-4` con la llave, SPM a 0 a los 2 s | `c` termina en 0 a 12 spm |
| VID-06 | cámara lateral 6 s, `R-4` | plumeo, centro de pala, cruce de manos, gotas |
| VID-07 | TV 15 s, `Series 600`, `R-4`, de 145 a 160 s | instante en que `relZ` cruza 9,5 contra el cierre de `#1` |
| DEMO-01 a -06 | W1280, los pasos 1 a 6 de la spec §1 (con el panel del simulador, `Hora escena` y el slot) | — |

---

## 6. Tests

### 6.1 Suites y comandos

| Suite | Comando | Qué suma |
|---|---|---|
| dotnet | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it7/…` | `Tests/EditMode/Scene/Stroke/**` |
| Unity EditMode / PlayMode (con y sin simulador) | `tools/unity/run-unity-tests.ps1 -Platform EditMode|PlayMode [-Simulator]` | §6.3 |
| Oráculo | `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Stroke` | `scene_stroke.json`, LEG-01, LEG-02 (con la guardia del legacy) |
| Arnés | `tools/unity/capture-snapshots.ps1 -Set it7 -Out …` (A y B) | §5 |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` | solo `Builds/Windows/simulator`; si está en uso, reintento cada 2 min hasta 20 |

### 6.2 El oráculo (`-Stroke`)

`stroke_vectors_test.dart`: `StrokeCycle` en 1 001 fases, `BoatPainter.seatZ/handle/bladeTip` en las mismas, y `SceneState` con pasos de 0,05 s a 24 spm (10 s), a 0 spm y en pausa (fase, cabeceo, charcos por paso). Se comparan con `Within(1e-12)` (trigonometría). `boat_legacy_images_test.dart`: el `BoatPainter` en V390 a las 12 h con `strokePhase` 0 y 0,36, sobre el fondo de LEG-02 de la 6a → `snapshots/legacy/LEG-01.png`, `LEG-02.png`.

### 6.3 Archivos de test

| Archivo | Qué prueba |
|---|---|
| `Stroke/StrokeCycleTests.cs` | LT-92 a LT-95 tal cual |
| `Stroke/SceneStateStrokeTests.cs` | LT-88 a LT-91 tal cual (con el `run` de 0,05 s) |
| `Stroke/LegacyBoatGeometryTests.cs` | LT-96 tal cual |
| `Stroke/StrokeGoldenTests.cs` | los vectores del oráculo |
| `Stroke/StrokeRhythmTests.cs` | la tabla de AC-SCN-04-05 (± 0,001), topes, monotonía, ida y vuelta |
| `Stroke/VisualStrokeClockTests.cs` | régimen < 0,001; escalón 24 → 30 ≤ 0,05 y absorbido en ≤ 1 palada; nunca retrocede; `r` solo en el catch; pausa en ≈ 0,25 s; eventos 1 por palada; spm 0 desde 24 spm con `p` 0,3 y 0,99: después de 6 s `φ = 0` y `ω_s = 0` exactos, un solo `Catch`, `StillSeconds` crece (QA7-G-03) |
| `Stroke/RiggingFitTests.cs` | DA7-01 con 1,85 y 1,75 m |
| `Stroke/ScullingKinematicsTests.cs` | medidas de `BoatSpec`; curvas de remo y pala; continuidad por cuadro a 60 fps; contactos ≤ 2 cm; cruce ≥ 4 cm con la izquierda arriba en `x > 0`; las 10 poses ± 8°; flexión; tronco `−18° − 2°·e` |
| `Stroke/BoatMotionTests.cs` | cabeceo, arfada, rolido; *surge*: media 0 (± 1e-6), 0 en catch y finish, tabla §4.5 (± 0,2 cm) |
| `Stroke/CameraBoatTermsTests.cs` | 10 min de escena en los 7 tamaños: horizonte ≤ 3 dp; reducir movimiento → 0; pausa × 0,6 |
| `Stroke/PuddleFieldTests.cs` | reglas legacy sobre la pala visible (2, izquierdo primero, 4 s, 0,3 → 0,9, fijos en el mundo, nada en pausa, sí a 12 spm, detrás de la cámara no) y LT-91 en paralelo |
| `Stroke/StrokeSprayTests.cs` | ≥ 6 por pala y `8 + 14·e`; visible mientras c < 0,08; altura máx.; gotas hasta 0,55; nada en pausa; determinista |
| `Stroke/BoatShadowRuleTests.cs` | modo por nivel y fuerza; lado del legacy (astro centrado → izquierda); lado por hora 09/12/17/22; fundido continuo |
| `Stroke/BuoyTrackerTests.cs` | `F-5` + `R-4`: paso a los 155 ± 1 s; sin saltos; libre y descanso sin boya; vuelta al terminar el paso |
| `Stroke/EffortAndIdleTests.cs` | `e(60) = 0`, `e(300) = 1`, τ 1 s; respiración a los 1,5 s; no toca `p` |
| `Stroke/RowerFootprintTests.cs` | zona del bote en los 7 tamaños y 10 poses; `Series anteriores` (TV, P360) y la llave (L640) sin tapar el cuerpo |
| `Stroke/RowerGeometryTests.cs` | maniquí ≤ 6 000 triángulos, pesos que suman 1, largos de hueso (± 1 mm); remos, herrajes y boya en su tope; *cockpit* 9,05–10,45 |
| `Stroke/SceneDataIsolationTests.cs` | `SceneSession` no escribe en la foto; `Distance` igual con y sin remero; 0 B en `Tick` (`AllocatedBytes`) |
| `Project/RepoHygieneTests.cs` (C) | `Assets/ThirdParty/**` solo `.meta` en `git ls-files`; 0 archivos bajo `Assets/ThirdParty/Mixamo`; `rower.character.asset` en `Procedural` y sin GUID de Mixamo; `git check-ignore` del *binding* (DT7-13) |
| `UnityEditMode/MixamoImportTests.cs` | con `YBot.fbx` presente: Humanoid, *Optimize Game Objects* apagado, legible (si no, `Ignore` = NO VERIFICABLE) |
| `UnityEditMode/RowerMaterialsTests.cs` | los 5 *shaders* nuevos compilan para los destinos de la 6a; la colección de variantes los cubre |
| `PlayMode/Scene/RowerRigTests.cs` | ángulos y contactos **medidos en el rig** (10 poses, maniquí y Y Bot si está) |
| `PlayMode/Scene/RowerSlotTests.cs` | sin *binding* → maniquí, 0 avisos; con Y Bot → Y Bot (55 320 triángulos pasan el tope de 60 000); *binding* con `Model` en `null` o inválido → maniquí + 1 aviso (`LogAssert`) |
| `PlayMode/Scene/RowerSceneTests.cs` | LT-97/98 (V390, H844, 12 h, 3 charcos, 8 fases, maniquí y Y Bot, `LogAssert.NoUnexpectedReceived`); piezas del bote; modo de sombra por hora y nivel; sesión `R-4` byte a byte con y sin escena |
| `PlayMode/Scene/RowerOcclusionTests.cs` | los rect. reales del panel y de la llave contra la huella proyectada por la cámara real |
| `PlayMode/Scene/SceneBudgetTests.cs` (C), `HudBudgetTests.cs` (C), `FrameAllocations` (C) | DA7-14; HUD ≤ 57; 0 B en 120 cuadros reales con el remero (y el control positivo) |

### 6.4 Matriz AC → test (`It07Ids.cs`)

| AC | dotnet | Unity / captura / dispositivo |
|---|---|---|
| AC-SCN-04-01, -02, -03 (LT-88 a -90) | `SceneStateStrokeTests`, `StrokeGoldenTests` | — |
| AC-SCN-04-04 | `VisualStrokeClockTests` | VID-03, VID-05 |
| AC-SCN-04-05 | `StrokeRhythmTests`, `VisualStrokeClockTests` | VID-01, VID-02 |
| AC-SCN-05-01 a -04 (LT-92 a -95) | `StrokeCycleTests`, `StrokeGoldenTests` | — |
| AC-SCN-05-05 (LT-96) | `LegacyBoatGeometryTests` | — |
| AC-SCN-05-06 | `ScullingKinematicsTests`, `RiggingFitTests` (DT7-02) | CAP-17 |
| AC-SCN-05-07 | `ScullingKinematicsTests` | VID-06 |
| AC-SCN-05-08 | `ScullingKinematicsTests` | `RowerRigTests`; CAP-17, CAP-18 |
| AC-SCN-05-09 | `ScullingKinematicsTests` | `RowerRigTests` |
| AC-SCN-05-10 | `VisualStrokeClockTests`, `EffortAndIdleTests` | VID-04 |
| AC-SCN-06-01, -02 | `BoatMotionTests` | — |
| AC-SCN-06-03 | `CameraBoatTermsTests` | VID-01 |
| AC-SCN-07-01 (LT-91) | `SceneStateStrokeTests` | — |
| AC-SCN-07-02 | `PuddleFieldTests` | CAP-04, VID-04 |
| AC-SCN-07-03 | `PuddleFieldTests` (alfa, radio) | CAP-04 |
| AC-SCN-08-01 | `RowerGeometryTests` | `RowerSceneTests`; CAP-01, -10, -11 |
| AC-SCN-08-02 | — | CAP-01, -02, -20, -21 |
| AC-SCN-08-03 | `ScullingKinematicsTests` (plumeo, flexión) | CAP-01, -02, VID-06 |
| AC-SCN-08-04 | `StrokeSprayTests` | CAP-03, VID-02 |
| AC-SCN-08-05 | `StrokeSprayTests` | VID-06 |
| AC-SCN-08-06 | `BoatShadowRuleTests` | `RowerSceneTests`; CAP-05 a -09 |
| AC-SCN-08-07 (LT-97, -98) | — | `RowerSceneTests` |
| AC-SCN-08-08 | — | `RowerSlotTests`, `MixamoImportTests`; CAP-20, -21 |
| AC-SCN-08-09 | `RepoHygieneTests` | QA: `git ls-files`, CI sin la carpeta |
| AC-SCN-08-10 | `BuoyTrackerTests` | CAP-15, -16, VID-07 |
| AC-SCN-08-11 | `SceneDataIsolationTests` | `RowerSceneTests` (byte a byte) |
| AC-SCN-08-12 | `ScullingKinematicsTests`, `StrokeSprayTests`, `BoatMotionTests`, `EffortAndIdleTests` | VID-01 contra VID-02 |
| AC-WKT-17-14 | `RowerFootprintTests` | `RowerOcclusionTests`; CAP-10, -11, -14 |
| AC-WKT-18-23 | `RowerFootprintTests` | `RowerOcclusionTests`; CAP-12, -13 |
| AC-SCN-01-16 | `SceneDataIsolationTests` (0 B en `Tick`; solo apoyo, no da el AC por bueno: QA7-G-09) | `SceneBudgetTests`, `FrameAllocations`, `HudBudgetTests`; `[PERF]` en `Builds/Windows/simulator` (Profiler del build de desarrollo); Android NO VERIFICABLE |
| AC-SCN-01-08 (cambia) | `RowerGeometryTests` | CAP-10, -11 |
| AC-SCN-02-04 (cambia) | `CameraBoatTermsTests` | VID-01 |
| Se vuelven a verificar (spec §4) | las suites de la 6a | las de la 6a, con el remero |

---

## 7. Paquetes de trabajo

### 7.0 Orden y reglas

**WP-A** (dotnet) → **WP-B0** (Unity: postprocesador, slot, medir Y Bot, `SceneShaderIds`) → **WP-A'** (solo si B0 midió proporciones distintas: `RowerSpec.Mannequin`, una línea) → **WP-D1** (shaders) → **WP-B1** (escena, remero, pruebas PlayMode) → **WP-B2** (arnés `it7`, `[PERF]`, build) → **WP-D2** (pulido con las tomas). Reglas de la 6a §7.0: una instancia de Unity; `ROWMATE_DATA_DIR` temporal; `ProjectSettings` solo por el *bootstrapper* y con la guardia; repo público.

### 7.1 WP-A — Core puro y oráculo (dotnet)

Todo §1.1, `tools/oracle/**` (`-Stroke`), `Tests/Golden/oracle/scene_stroke.json`, `snapshots/legacy/LEG-0{1,2}.png`, `Tests/EditMode/Scene/Stroke/**`, `Tests/EditMode/Traceability/It07Ids.cs`, `Tests/EditMode/Project/{PureCodeRulesTests,RepoHygieneTests}.cs`. **Entrega**: `dotnet test` verde sin avisos, la matriz con los ID dotnet pasando, el oráculo corrido con el legacy limpio.

### 7.2 WP-B — Unity: remero, bote, escena, capturas, build

- **B0**: `Editor/Import/MixamoImportPostprocessor.cs`, `Scene/Slots/RowerCharacterSlot.cs`, `Editor/Bootstrap/SceneAssetsBuilder.cs` (C), `Scene/Slots/RowerCharacterBinding.cs`, `Editor/Slots/RowerCharacterBindingBuilder.cs`, `Slots/rower.character.asset` en `Procedural` (G) y el *binding* (G, ignorado), `Scene/SceneShaderIds.cs` (C), `Tests/UnityEditMode/MixamoImportTests.cs`; un informe con los largos de hueso de Y Bot (en "Cambios por implementación").
- **B1**: `Scene/Rower/**`, `Scene/Boat/**`, `Scene/Water/**`, `Scene/{HorizonRenderer,SceneDirector,SceneHost,SceneWorldBuilder,SceneLighting}.cs` (C), `Editor/Bootstrap/SceneMaterialsBuilder.cs` (C) y lo que genera, `Editor/Slots/SlotValidator.cs` (C), `Tests/UnityEditMode/RowerMaterialsTests.cs`, `Tests/PlayMode/Scene/Rower*.cs`, `SceneBudgetTests.cs`, `HudBudgetTests.cs`, `Tests/UiSupport/FrameAllocations.cs` (C); si `RowerOcclusionTests` falla, `Presentation/Workout/HudZones.cs` o `Core/Design/SeriesPanelGeometry.cs` (excepción de dueño, avisada).
- **B2**: `Snapshots/**`, `tools/unity/{capture-snapshots.ps1,README.md}`, `docs/migration/iterations/07-remero-palada/snapshots/**` (salvo `legacy/`), `SimulatorPresentation/FrameTimeProbe.cs`, `Bootstrap/AppRoot.cs` (etiquetas `[PERF]`), el build y el `[PERF]`.

### 7.3 WP-D — Designer: shaders, materiales y pulido

- **D1**: `Art/Shaders/Scene/{Rower,Oar,Spray,WaterMarks,Buoy}.shader` (N); `WaterDegraded` (recibe la sombra, ≤ 30 ALU), `Hull` (herrajes por color de vértice), `BoatShadow` (el lado como uniform) (C); `Art/Shaders/Include/RowMateLighting.hlsl` (el relleno del remero `0,12·sunColor·saturate(N·V)`); desde acá, dueño de `Core/Scene/Art/RowerArt.cs`.
- **D2**: ajustes con las tomas de `it7` (colores, suavidades, tamaños mínimos, alfa de charcos y gotas), `04-design-notes.md`, ART_BIBLE §7.8–§7.9, ANIMATION_BIBLE §0.3/§7.3 (DT7-01, -07) y ASSET_SLOTS §2.5 (DT7-04).

### 7.4 El contrato de los shaders (`SceneShaderIds`, B0 → D1)

| Shader | Cola | Entradas |
|---|---|---|
| `RowMate/Scene/Rower` | opaco | color de vértice R = región (0–8); `_RmRowerColors[9]` (`Vector4`: color + suavidad), `_RmRimFill`; *skinning* de Unity; `ShadowCaster` |
| `RowMate/Scene/Oar` | opaco | color de vértice R = parte (puño, caña, manguito, pala, punta); `_RmOarLoad` (m), `_RmMinWidthDp` (1,2), `_RmPixelSize`; pasada `ShadowCaster` solo en la submalla de la pala |
| `RowMate/Scene/Spray` | transparente 3100 | instanciado: `_RmSpray[128]` (`Vector4`: tamaño, alfa, tipo, largo del trazo), `_RmMinDropDp` (1,5 / 1,0) |
| `RowMate/Scene/WaterMarks` | transparente 3050 (después del agua) | instanciado: `_RmMarks[16]` (`Vector4`: edad, giro, alfa, tipo), `_RmEffort`; ≤ 20 ALU, 0 texturas, sin profundidad |
| `RowMate/Scene/Buoy` | opaco | `_RmBuoyColor` (`#FF9F1C`), franja blanca; `ShadowCaster` |
| `WaterDegraded` (C) | la de la 6a | `_MAIN_LIGHT_SHADOWS*` con las variantes del nivel; `_RmShadowDarken` (0,10) |

Nada usa `_Time` (todo con el tiempo de escena; determinismo).

---

## 8. Integración del arte y la spec

### 8.1 Pedidos al Architect (00-arte-remero §13)

| ID | Respuesta |
|---|---|
| DA7-01 | **Adoptado** (DT7-02): `RiggingFit` en Core; la spec cambia el texto de AC-SCN-05-06 a "pedalera y recorrido de `RiggingFit`" |
| DA7-02 | **Adoptado**: `HullMesh` con el *cockpit* 9,05–10,45 (WP-A) |
| DA7-03 | **Adoptado en parte**: esqueleto `mixamorig:*` del cuerpo y malla ≤ 6 k; **sin `AvatarBuilder` ni dedos** (DT7-01); proporciones de Y Bot (DT7-03) |
| DA7-04 | **Adoptado** con la regla de DT7-08 |
| DA7-05 | **Adoptado**: shader `Rower` y `RowerOutfitBaker` (por eso Read/Write encendido, DT7-04) |
| DA7-06 | **Adoptado**: cañas `ShadowCastingMode.Off`, palas, casco y remero sí |
| DA7-07 | **Adoptado**: tamaño mínimo en los *vertex shaders* de `Oar` y `Spray` |
| DA7-08 | **Adoptado** con 16 instancias (DT7-12) |
| DA7-09 | **Adoptado con cambio**: `StrokeSpray` en Core + 1 dibujo instanciado, no `ParticleSystem` (DT7-05) |
| DA7-10 | **Adoptado**: `ShadowSideFader` (1,5 s; en el acto con hora forzada) |
| DA7-11 | **Adoptado**: `BuoyTracker`; tolerancia de la spec (± 1 s, DT7-11) |
| DA7-12 | **Adoptado con cambio**: `GripPose` en Core aplicada a los dedos, sin clip (DT7-07) |
| DA7-13 | **Adoptado**: prueba de la mano de `x > 0` arriba en α = 0 |
| DA7-14 | **Adoptado**: `SceneBudgetTests` con los topes pedidos |
| DA7-15 | **Adoptado** (DT7-10): con `R-4` la spec pide algo imposible; CAP-04 se ajusta |

### 8.2 Arte contra spec (manda la spec)

- Proporciones del maniquí: las de Y Bot (spec), no la tabla de 1,85 m (arte D7-03) → DT7-03.
- Pelo: la spec dice "piel y pelo"; el arte ponía la gorra siempre → pelo y gorra apagada (DT7-09).
- Sombra en Medio de noche: la spec pide "real" en Medio; con la fuerza 0 de la 6a no hay sombra real → el óvalo (DT7-08; compatible).
- Boya: `x` +4,5 m (spec) aunque en TV pase por el borde (arte §14, Para issues).

### 8.3 Desvíos (ninguno cambia un dato de la sesión)

DT7-02 (pedalera; imposible), DT7-06 (tope del cabeceo de la cámara en horizontal; el ± 3 dp es la regla dura), DT7-10 (CAP-04; imposible con `R-4`). La spec ya los recoge (QA7-G-05): AC-SCN-05-06 con `RiggingFit` y DV-77, AC-SCN-06-03 con el tope suave y CAP-04 con un par.

---

## 9. Riesgos

| Riesgo | Mitigación |
|---|---|
| La tabla de ángulos ± 8° no cierra con el IK de dos huesos (hombro, cadera) | `ScullingKinematicsTests` lo mide en WP-A; los parámetros libres (altura de la cadera sobre el asiento, hint del codo, protracción) se ajustan ahí, sin tocar la tabla |
| La pose de reposo de Y Bot no es una T-pose limpia (dedos, pulgar) | el solver usa la dirección real al hijo; los dedos con la tabla de `GripPose` relativa al reposo medido; `RowerRigTests` con Y Bot |
| *Skinning* en modo edición dentro del pedido de render | sonda en B1 (CAP-20 en el arnés antes del resto); si falla, `SkinnedMeshRenderer.BakeMesh` a una malla reservada solo en el arnés |
| El primer import de `YBot.fbx` sale Generic (alguien abrió Unity antes de B0) | el *bootstrapper* lo reimporta con el postprocesador |
| El *binding* queda viejo (FBX agregado o borrado con el Editor cerrado) | el *bootstrapper* y el preproceso del build lo regeneran; con `Model` en `null`, maniquí + 1 aviso (DT7-13) |
| Y Bot (55 k triángulos con *skinning*) pesa en móvil | el maniquí es el valor por defecto; LOD de Y Bot a Para issues |
| El presupuesto de GPU queda con poco margen (≈ 11,3 de 12 ms) | `[PERF]` en el build; primero se baja el hervidero y la distancia de sombras |
| Superposición de `Series anteriores` o la llave con el cuerpo | detectada con dotnet antes de las tomas; el ajuste es de layout, sin elementos nuevos |
| Animation Rigging en el manifiesto sin uso (Burst compila igual) | sin efecto en el build; Para issues |

---

## 10. Para issues

- Quitar `com.unity.animation.rigging` del manifiesto si sigue sin uso al cerrar la 7 (DT7-01).
- Boya a `x` +3,5 m en TV (pasa por el borde a +4,5) y boya con número; boya cada 500 m en el libre.
- X Bot y el personaje realista (`Kaya`) para probar `RiggingFit` con otras proporciones; dedos en el maniquí.
- Gorra opcional y tono de piel elegible; personalización de colores (backlog, Q-66).
- Micro-anillos de gotas, reflejo del remero y del bote, estela con *surge* (6b).
- Respiración de remada y tensión de hombros por esfuerzo (no se ven a 47 dp; con cámaras cercanas).
- Rolido por escenario y mar de fondo (It. 8); sincronía con el contador del monitor (It. 10a).
- Sombra del remo entero con un mapa más fino en Alto.
- LOD de Y Bot para móvil (55 k con *skinning*) y alto real de Y Bot medido en B0 anotado en ASSET_SLOTS §2 (gate, Para issues).
- `Assets/New Text Document.txt` sin versionar en `Assets/`: que no entre en un commit (gate).

---

## Cambios por gate

Gate: [03-qa-gate](03-qa-gate.md) (APROBADO CON OBSERVACIONES). Ningún número del contrato cambió.

| Issue | Qué se cambió |
|---|---|
| QA7-G-01 | Tope del slot ≤ 60 000 triángulos (Y Bot: 55 320, 65 huesos) en §0, §3.2 y `RowerSlotTests`; presupuesto +56 k / ≈ 191 k y `SceneBudgetTests` ≤ 205 k con Y Bot (§4). Spec AC-SCN-08-08, ASSET_SLOTS §2 y arte §3.3, §11 y DA7-14 alineados |
| QA7-G-02 | `OarCurves.VisualSweepDeg` = `−62° + 104°·(α_legacy + 55°)/90°` (§2.5). Spec AC-SCN-05-07 y ANIMATION_BIBLE §3.6 alineados |
| QA7-G-03 | Traba del catch en `VisualStrokeClock` (§2.2) y su test (§6.3). Aplicado con un ajuste: la traba salta en el primer cruce de `φ` por 0 con spm 0 aunque `p` todavía no haya llegado, porque si `φ` va adelante de `p` (spm cae con `p` ≈ 0,99) la regla literal del gate le haría dar otra palada entera. Spec AC-SCN-04-04 alineada |
| QA7-G-04 | DT7-13: nada de Mixamo versionado (ni `.meta`), slot siempre en `Procedural`, *binding* generado e ignorado en `Bootstrap/Generated/Resources/`, `RepoHygieneTests` (§1.3, §3.2, §6.3, §7.2, §9). Spec AC-SCN-08-09 y §11, ASSET_SLOTS §0 y §2.5 paso 6 (ya no se arrastra el FBX al slot) alineados |
| QA7-G-05 | Spec: AC-SCN-05-06 (`RiggingFit`, DV-77), AC-SCN-06-03 (tope suave) y CAP-04 (un par); §8.3 lo registra |
| QA7-G-07, -08, -09, -11 (menores) | `r` fijo hasta el catch (§2.2); citas de `scene_state.dart` corregidas (§1.1, §2.1, §2.6); AC-SCN-01-16 solo con `FrameAllocations` y el Profiler (§6.4); escala del maniquí a 1,85 m (DT7-03). QA7-G-12 ya estaba en Para issues. Los menores del PO van en la spec: `e = 0,5` en la tabla de *surge* (QA7-G-06) y el tronco visible en DV-74 (QA7-G-10) |

## Cambios por implementación (WP-A)

dotnet: `tools/dotnet/run-tests.ps1` en verde (3 186 tests: 3 147 pasan, 39 omitidos, 0 fallan; 0 avisos). Oráculo: `run-oracle.ps1 -Stroke` corrido con el legacy limpio (`scene_stroke.json`, LEG-01, LEG-02). Unity no se abrió en WP-A (lo abre B0 con el postprocesador de Mixamo ya puesto; así Y Bot no se importa primero como Generic).

| ID | Qué cambió y por qué (una línea) |
|---|---|
| A7-01 | **La tabla de 10 poses (ANIMATION_BIBLE §3.4) no cierra entera** con las medidas reales, los ramps del legacy y el barrido del contrato (las manos siguen al remo real). Cierran (± 8°): tronco y barrido en las 10, y rodilla, cadera y codo en el catch, la extracción y antes del catch, más el finish. No cierran: rodilla y cadera con medio carro (76° y 33° contra 112° y 52°: con piernas de 0,45 m medio carro dobla la rodilla a ≈ 90°), hombro en el catch (98° contra 80°: el puño a 0,48 m deja el brazo casi perpendicular al tronco), codo con "brazos empiezan" y "manos afuera" (130° y 128° contra 165° y 178°: el remo vuelve más lento que los brazos). `ScullingKinematicsTests.TheTenPosesOfTheTable` exige lo que cierra e imprime la hoja medida (abajo). B1 compara el rig contra esta hoja; la tabla la ajusta el Designer (Para issues) |
| A7-02 | `RiggingFit`: brazos al 99,7 % en el catch (con el 98,5 % del arte el codo queda en 160°, fuera de 178 ± 8) y piernas a **175°** en el finish (a 178° la rodilla casi recta salta 3 cm por cada 3 mm de cadera cuando el brazo deja de tirar). Con el maniquí: talones −0,198, cadera +0,160 / +0,706, recorrido 0,546, puño 1,8 cm debajo de las costillas bajas |
| A7-03 | La cadera es `lerp(catch, finish, piernas)` con un tope: si un brazo tendría que estirarse más del 99,7 %, la cadera va hacia el puño (el cuerpo cuelga del remo; hasta 8 cm detrás del catch). Contactos de las manos ≤ 0,5 cm por el IK en todo el ciclo |
| A7-04 | El cruce se reparte: ± 1,25 cm (y 0,75 a popa) en el puño del remo y otro tanto en la mano. Manos separadas 5 cm, la mano a ≤ 1,5 cm de su puño y la pala sigue ≥ +0,06 afuera y ≤ −0,10 adentro |
| A7-05 | La columna (60/15/13/12 % con la base de 6° que se deshace hacia arriba) se calibra para que la recta de la cadera a la base del cuello quede exactamente en el tronco pedido; la cadera de la tabla se mide contra la pelvis (`Hips → Spine`) |
| A7-06 | Salpicadura: la velocidad vertical va de 0,85 a 1 × (1,4 + 1,0·e), así la altura máxima no pasa de 0,12 + 0,18·e; "hacia proa" en promedio (el cono de 35° abre alrededor de 0,6 m/s). `SprayParticle` en `double` (z de mundo sin perder precisión a 42 km); la corona es `Kind` 1 con `Vy` = su alto |
| A7-07 | Los eventos con la sesión en pausa se descartan (φ termina de alcanzar a p sin crear charcos ni salpicadura) |
| A7-08 | `RowerFootprint.BoatToPins` toma el casco desde 1,5 m a popa de los pernos: la popa de la 6a (z 5,3) cae debajo de la zona en horizontal desde la 6a (Para issues) |
| A7-09 | `SceneSession.SetRenderTier(RowerRenderTier)` (sombras, Alto, escala de partículas, hervidero) y `SetRower(RowerSpec)`; `SceneInputs` suma `PowerWatts` y `BuoyInputs` con el constructor viejo intacto |
| A7-10 | El slot de la boya es `prop.markerBuoy` (los ids de slot solo llevan letras); los otros seis, como §1.1. `SlotIdsTests` ahora pide los de la 7 |
| A7-11 | El carro es una malla aparte (`SeatMesh`, se mueve); la boya marca la franja en el rojo del color de vértice |
| A7-12 | Los 0 B de `Tick` con el remero los cubre `SceneSessionTests.TickDoesNotAllocate` (verde, solo apoyo); no se agrega otra medida con el contador del hilo. AC-SCN-01-16 queda "p eq" en `It07Ids` |
| A7-13 | `.gitignore`: `/Assets/ThirdParty/Mixamo/` y `/Assets/ThirdParty/Mixamo.meta`; `RepoHygieneTests` da vuelta el caso de `YBot.fbx.meta` (ahora ignorado) y suma las pruebas de DT7-13 |

Hoja medida del maniquí (e = 0; grados; carro = z de la cadera en el marco del bote):

| c | Tronco | Rodilla | Cadera | Hombro | Codo | Remo | Carro |
|---|---|---|---|---|---|---|---|
| 0,000 | 30 | 50 | 29 | 98 | 171 | −62 | 0,16 |
| 0,090 | 30 | 76 | 33 | 103 | 171 | −46 | 0,35 |
| 0,180 | 14 | 124 | 62 | 81 | 166 | −10 | 0,60 |
| 0,240 | −6 | 175 | 100 | 45 | 130 | 15 | 0,71 |
| 0,360 | −20 | 175 | 107 | −9 | 70 | 42 | 0,71 |
| 0,395 | −20 | 175 | 107 | −12 | 80 | 42 | 0,71 |
| 0,600 | 0 | 175 | 95 | 39 | 128 | 15 | 0,71 |
| 0,750 | 30 | 109 | 44 | 85 | 147 | −23 | 0,53 |
| 0,850 | 30 | 70 | 31 | 92 | 171 | −46 | 0,31 |
| 0,960 | 30 | 46 | 29 | 88 | 171 | −61 | 0,13 |

### Para issues (WP-A)

- ANIMATION_BIBLE §3.4: actualizar la tabla de 10 poses con la hoja medida (A7-01) o aceptar la hoja como referencia de CAP-17 y AC-SCN-05-08. **Hecho** en la verificación (QA7-V-01, DV-78; 04-design-notes "Cambios por verificación").
- La popa del casco (z 5,3) cae debajo de la zona del bote en TH, L640, W1280 y H844 (ya en la 6a; A7-08).
- `GripPose` traduce los músculos del arte a grados (≈ 90° por unidad): revisar sobre Y Bot en B1.
- Los `.meta` de los archivos nuevos (`Core/Scene/Stroke/`, `Tests/EditMode/Scene/Stroke/`, `scene_stroke.json`) los genera Unity al abrir en B0; el bootstrapper crea los `.asset` de los 7 slots nuevos (B0 le da a `rower.character` su tipo).

## Cambios por implementación (WP-B)

Verificación: dotnet `run-tests.ps1` 3 186 tests (3 147 pasan, 39 omitidos, 0 fallan, 0 avisos). Unity: compilación limpia; EditMode **3 239 / 3 284** (45 ignorados) y **3 431 / 3 476** con simulador, 0 fallan; PlayMode **500 / 500** y **573 / 573** (con simulador). Arnés `it7`: 55 tomas (21 CAP con CAP-17 y CAP-18 en 10 + hoja, CAP-19 en dos, 7 VID, 6 DEMO), 0 con problemas, **it7-A e it7-B iguales byte a byte** (PNG y MP4; el `manifest.json` solo difiere en `durationMs`). Build `Builds/Windows/simulator` y `[PERF]` (abajo). Y Bot estaba en la máquina: todo lo de Y Bot se verificó.

| ID | Qué cambió y por qué (una línea) |
|---|---|
| B7-01 | **WP-D1 no estaba**: B1 escribió versiones mínimas de `Rower`, `Oar`, `Spray`, `WaterMarks` y `Buoy` con el contrato de §7.4 (arreglos `_RmRowerColors[9]` / `_RmOarColors[5]` que pone `SceneWorldBuilder` desde `RowerArt`), el lado del óvalo como uniform en `BoatShadow` (`_RmShadowOffset.x` con signo, de `ShadowSideFader`) y la sombra en `WaterDegraded` (`kRmShadowDarken` 0,10 = `RowerArt.ShadowDarken`, en `ShaderArt.Constants`). Van aparte de los 13 de la 6a (`SceneShaderIds.RowerShaderNames`; `RowerMaterialsTests`); el Designer los pule en D2 |
| B7-02 | **Y Bot mide 1,786 m** (fuera de 1,80–1,88). Largos (m): cadera ±0,091, Hips sobre las articulaciones 0,067, pelvis 0,166, Spine 0,116, Spine1 0,134, Spine2 0,150, hombros caen 0,062, clavícula ±0,061, hombros ±0,188, cuello 0,103, cabeza 0,185, brazo 0,274, antebrazo 0,276, muñeca-nudillo 0,128, muslo 0,406, pierna 0,421, tobillo 0,105, pie 0,300, pies ±0,091. **El maniquí sigue con la tabla del arte** (WP-A' no se hizo: cambia la hoja de A7-01 y `RiggingFit`; Para issues). Con Y Bot la sesión usa sus largos medidos (`SetRower`) |
| B7-03 | Y Bot: 55 320 triángulos; la piel usa **52** huesos (el FBX tiene 65; los `_End` y algunos de la cara no pesan). Valida para el slot |
| B7-04 | Presupuesto: la 6a sola ya da 45–46 SetPass en Medio, así que **≤ 40 no entra**: el tope de la 7 queda en el de la 6a (≤ 50). Medido en Medio (TV, 12/22 h): 82/79 dibujos, **50/50 SetPass**, 164 k/160 k triángulos con el maniquí; con Y Bot 84/80, 50/50, **267 k**/212 k (su pasada de sombras vuelve a dibujar los 55 k: el tope del diseño de 205 k no la contaba). Topes de `TheRowerFitsTheMediumBudget`: ≤ 100 / ≤ 50 / ≤ 170 k maniquí / ≤ 280 k Y Bot. Alto 102 / 65–66 / 224 k; Bajo 64–66 / 35–37 / 110 k |
| B7-05 | Para no sumar SetPass: los remos con **un material por lado** (sin `MaterialPropertyBlock`, quedan en el SRP Batcher) y, con el óvalo (Bajo o Medio de noche), el remero, las palas, los herrajes y el carro **no proyectan** (`SceneDirector.CastShadows`) |
| B7-06 | `ShowRulerForTest` es una bandera: la regla de 1 m la dibuja el arnés (`RowerMarks.Ruler`) con la cámara del cuadro, en el perno de la cámara lateral (x −0,80) |
| B7-07 | El agarre de un rig con dedos: `HandToGrip` = 75 % de la muñeca al nudillo del medio (el 100 % dejaba el puño del remo en los nudillos) |
| B7-08 | Bootstrapper **v5** (slot `rower.character`, reimportación de Mixamo, *binding* y los 5 materiales); `BootstrapperStaticTests` y `BootstrapV3Tests` siguen la versión |
| B7-09 | `RowerCharacterKind` (`Auto` en la app = el *binding*; `Mannequin` por defecto en `UiTestHost` para que las pruebas den lo mismo en todas las máquinas; `Slot`; `InvalidForTest`). `RowerCharacterResolver.Resolve(..., loadFromResources: false)` prueba "sin *binding*" en una máquina con Y Bot |
| B7-10 | Arnés: CAP-15/-16 a los **150,2 / 155,2 s** (k + 0,2: la regla de toma de la 4b); las tomas de la boya y VID-07 avanzan **cuadro a cuadro** (con el salto de `AdvanceTo` la escena integraba todo el tramo con la foto del final y la boya quedaba atrás, como QA-V6a-04). CAP-17/-18 son secuencias de 10 tomas + la hoja (`SnapshotScenario.ShotInspect` nuevo: las sondas de cada toma de una secuencia). DEMO-04 se toma a las 17:00 (09:00 pasa antes) |
| B7-11 | `SceneLighting.ShadowStrength` expone la fuerza del cuadro (la mira `ShadowProbe`); la regla de la sesión sigue en Core (`look.ShadowStrength`) |
| B7-12 | `[PERF]` nuevo: `remero (n.º k): <maniquí | slot ruta>, <triángulos>, <alto>` junto a `escena (n.º k)`. `FrameTimeProbe` sin cambios (la ventana de 10 s ya mide la escena con el remero) |

`[PERF]` en `Builds/Windows/simulator` (`-rowmate-perf-scene 40`, libre con Fuerte, nivel Alto, **Y Bot**): CPU del hilo principal p99 2,9–3,2 ms (máx 4,3 en régimen; 21,5 en la entrada, el JIT del primer pedido de URP, como en la 6a), GPU máx 4,4–5,6 ms, dibujos ≤ 116, SetPass ≤ 66, memoria 130 MB, GC 0 en régimen; peor unidad del armado 21,5 ms (post de URP, como la 6a) y **20,1 ms en el mundo** (el remero de Y Bot: instanciar y vestir; en reposo, antes de Ready). Android: NO VERIFICABLE.

Prueba a mano: no hubo interacción con la ventana; la cubren la demo del arnés (DEMO-01 a -06: Suave, Fuerte, Pausa, `Hora escena`, la boya, Y Bot) y la corrida `[PERF]` del build. Queda para el usuario abrir el build y mirarlo.

### Para issues (WP-B)

- WP-A': maniquí con las proporciones de Y Bot escaladas a 1,85 m (B7-02) o aceptar la tabla del arte.
- Y Bot en el armado: partir la unidad "el remero" (20 ms en el player) en instanciar, medir y vestir.
- La cámara lateral de prueba ve el cielo de pantalla (hecho para la cámara del contrato) partido en dos tonos: pasarlo a dirección de mundo o taparlo en las tomas laterales.
- Shaders de la 7 con sus constantes en `ShaderArt` y sus pruebas de contrato y de costo (D1/D2).
- Hervidero y charcos poco visibles a 47 dp en TV; tamaño y alfa (D2).
- Topes de presupuesto: actualizar §4 y ANIMATION_BIBLE/ART_BIBLE con lo medido (B7-04).
- `Assets/New Text Document.txt` sigue sin versionar en `Assets/` (gate).

## Cambios por implementación (WP-D)

Detalle en [04-design-notes](04-design-notes.md). Ningún número de Core ni de la sesión cambió; la lógica de Unity no se tocó.

| ID | Qué cambió y por qué (una línea) |
|---|---|
| D7-01 | Los cinco shaders de B1 pulidos con las tomas: `Rower` con bordes nítidos (grilla de celdas de 3 × 3 en lugar del color interpolado), piel con envolvente 0,4 y terminador `#E89A80`, brillo de borde del mono, anteojos con Fresnel y especular apagado; `WaterMarks` con el par de remolinos contrarrotantes (ojo oscuro + media luna de espuma) y el hervidero batido; `Spray` con núcleo; `Buoy` con brillo de plástico. `Oar` y `Hull`, `BoatShadow` y `WaterDegraded` sin cambios de aspecto |
| D7-02 | `RowMateLighting.hlsl`: `RmFrontFill` (el relleno 0,12·sunColor·saturate(N·V) de ART_BIBLE §7.9) |
| D7-03 | Contrato y costo: `ShaderArt.Rower` + `ShaderArt.RowerConstants()` (16 `kRm*` desde `RowerArt`), `RowerShaderContractTests` (dotnet) y `SceneShaderTests.TheRowerFragmentsFitTheirBudget` (Unity). Medido en D3D11: Rower 26, Oar 19, Spray 15, WaterMarks 11 + 28 en rama, Buoy 23 ALU; 0 texturas |
| D7-04 | Biblias: ART_BIBLE §7.8–§7.9, ANIMATION_BIBLE §0.3 y §7.3 (DT7-01, DT7-07), ASSET_SLOTS §2.5 (DT7-04) |

### Para issues (WP-D)

- CAP-04 en TV no muestra los charcos (con `R-4` quedan a `relZ` ≈ 1 m y `x` ±2,1 m, fuera del cuadro).
- Remolinos orientados por el eje de la pala; franja más angosta o con antialias; línea del pelo del maniquí; manos de Y Bot en piel; LOD de Y Bot.
