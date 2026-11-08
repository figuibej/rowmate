# It. 8 — Río, Costa, Regata y selector de escenario: diseño técnico

| Campo | Valor |
|---|---|
| Autor | unity-architect |
| Fecha | 2026-11-07 |
| Iteración | 8 · `08-entornos-selector` ([ROADMAP §It. 8](../../ROADMAP.md)), sobre la escena de la 6a (agua **degradada**; la 6b sigue postergada, issue #6) y el remero de la 7 |
| Entrada | [01-po-spec](01-po-spec.md) (40 AC, LT-99 a LT-138, CAP-01 a -46, LEG-01 a -03, VID-01 a -06, DEMO-01 a -05, DV-79 a -89); [00-arte-entornos](00-arte-entornos.md) (D8-01 a -11, DA8-01 a -18); [06a/02-tech-design](../06a-escena-lago/02-tech-design.md) (§2.9 a §2.17, §3.1 a §3.7, §4, §5); [07/02-tech-design](../07-remero-palada/02-tech-design.md) (§2.6, §3, §4, §5) |
| Legacy leído (solo lectura, `3ae0f3a`) | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (`../rowmate-legacy`): `scene/environments/{river,coast,regatta}_environment.dart`, `scene/painters/{shore,thumbnail,water}_painter.dart`, `shaders/water.frag:45-68`, `scene/environment_picker.dart`, `scene/scene_settings.dart`, `features/workout/workout_screen.dart:60-100`, `test/scene/water_painter_test.dart`. `git -C ../rowmate-legacy status --porcelain` vacío al empezar y al terminar. No se leyó el módulo de credenciales |
| Código leído (este repo) | `Core/Scene/{ShoreEnvironment,LakeEnvironment,EnvironmentId,ShoreSegments,SceneHash,SlotIds}.cs`, `Core/Ports/IPreferences.cs`, `App/Scene/SceneSettings.cs`, `App/Workout/SeriesPanelPrefs.cs`, `App/Composition/{AppComposer,AppContext}.cs`, `Scene/{SceneHost,SceneWorldBuilder,ShoreStreamer,SegmentStore,SceneShaderIds}.cs`, `Scene/Slots/{EnvironmentDefinition,SceneWorldDefinition}.cs`, `Presentation/Workout/WorkoutScreenView.cs`, `Snapshots/{SnapshotCatalog,SnapshotCli}.cs`, `Slots/*.asset` |
| Estado | Diseño compacto, **sin código**. Modo "entrega rápida": cada duda se resolvió con el legacy, la spec o el valor por defecto, en una línea. Orden: **WP-A** → **WP-B0** → **WP-D1** → **WP-B1** → **WP-B2** → **WP-D2** → **WP-B3** (§8) |

Convenciones: las de la 6a y la 7. Rutas relativas a `Assets/RowMate/` salvo `tools/`, `docs/`, `ProjectSettings/`, `Assets/ThirdParty/`. Marcas: **N** nuevo, **C** cambia, **G** generado. Mundo: `x` lateral (+ derecha de la pantalla), `y` altura sobre el agua, `z` adelante; `relZ = z − d`. "Kit" = lo que es propio de un escenario (§3.1).

---

## 0. Resumen

1. **Toda regla nueva es Core puro**: los tres generadores del legacy (`RiverEnvironment`, `CoastEnvironment`, `RegattaEnvironment`) con el `DartRandom` de la 6a, **bit a bit** contra el oráculo en los segmentos 0 a 2 000; topes 60 / 200; hitos en metros exactos; oleaje de la costa (la ola de `water.frag` sin ruido) para el agua y el casco; rolido por escenario; siluetas lejanas; ventanas del legacy; las 28 entradas del agua (LT-124); la preferencia `scene.environment`; la huella de las miniaturas.
2. **Un escenario = un kit sobre la base común** (DT8-01): cielo, agua, bote, remero, luz, post y *shaders* son comunes y se arman una vez; terreno, orilla, relleno, horizonte y perfil del agua son del kit. **Un kit en memoria**: elegir otro en reposo descarta el viejo y arma el nuevo por tramos (la misma política de la 6a, 0 ms mientras algo anima). Sin escenas aditivas (spec §11.2).
3. **El lago no cambia**: su kit es el armado de la 6a y la 7 movido de lugar; lo nuevo del agua y del bote solo se activa con `WaveAmplitude ≥ 0,5` (la costa) y el rolido del lago sigue con `k` 0,5. CAP-01 a -08 y una corrida de control de `it7` lo comprueban.
4. **Lo que se ve pasar se dibuja con lo que ya hay**: `ShoreStreamer` e `InstanceBatch` con un `float4` por instancia (tono, semilla, máscara de ventanas, fase); 5 *shaders* nuevos (`Facade`, `Glow`, `Foam`, `Crowd`, `MarkerText`) y variantes de `Prop`, `WaterDegraded`, `Terrain` y `Horizon`. Todo lo que se mueve se mueve en el vértice con el tiempo de escena: 0 B y 0 trabajo por objeto en CPU.
5. **Miniaturas horneadas** en el Editor con la escena real (12:00, 20 m, sin bote, objetos con `relZ < 150`), 456 × 288, versionadas con una huella que un test de dotnet vigila. En Workout se muestran como texturas: nunca se arma ni se dibuja una escena para el selector.
6. **Selector** en UI Toolkit, en la vista de reposo de Workout, con anillo y check absolutos (sin *relayout*), teclado, rueda y la preferencia en `prefs.json` (un toque antes de la lectura gana, como el legacy).
7. **Arnés `it8`**: 46 capturas, LEG-01 a -03 del oráculo, 6 videos y 5 tomas de demo, dos corridas iguales byte a byte; sondas de luces nocturnas en el `manifest` (cierra el issue #9).

### 0.1 Decisiones del Architect (una línea cada una)

| ID | Decisión |
|---|---|
| **DT8-01** | Kit por escenario sobre la base común, uno en memoria (§3.1). ADR 023 corto. Las variantes de **todos** los *shaders* de los 4 escenarios se calientan una vez en la parte común: cambiar de escenario no compila nada |
| **DT8-02** | Miniaturas con **`relZ < 150`** (spec AC-SCN-22-04, legacy `thumbnail_painter.dart:33-36`); se rechaza D8-09 (400 m). El puente de 270 m y la tribuna de 210 m no salen en la miniatura (a 20 m quedan a 250 y 190) |
| **DT8-03** | Tribuna de **12 m** (spec AC-SCN-18-04: 4 gradas de 12 × 1,2) **a lo largo del canal** y 6 m de fondo, mirando al agua (orientación de D8-04, medida de la spec); ancla `(12; z₀ + 10)` = centro del borde del agua. Público ≈ 34 por tribuna |
| **DT8-04** | Cifras del cartel de **0,55 m** de alto (spec) centradas a 2,0 m, con el ancho ajustado a ≤ 1,60 m (D8-05): `"10000 m"` entra |
| **DT8-05** | Oleaje = la fórmula de la spec (AC-SCN-12-01, `water.frag:45-53` sin ruido) para el color y para el casco (`0,15·h` en proa, centro y popa; τ 0,3 s); cámara × 1,8 con tope ± 5,4 dp en la costa. Se rechazan las bandas de 16 m a 3 m/s y los números de arfada, cabeceo y rolido del arte §3.4 (manda la spec). Se adopta el umbral de D8-06 (`WaveAmplitude ≥ 0,5` → solo la costa) |
| **DT8-06** | La ola en precisión de cinta (Q-68): Core da `φ₁ = Mod(d/s + 0,6t; 2π)` y `φ₂ = Mod(0,7·d/s + 0,9t; 2π)` en `double`; el *shader* usa `sin(relZ/s + φ₁)·0,6 + sin((0,7·relZ + 1,3x)/s + φ₂)·0,4` (idéntico a `zMundo = relZ + d`) |
| **DT8-07** | Bajo el puente la luz baja un **40 %** (spec AC-SCN-16-03): clave y ambiente × `(1 − 0,4·o)`, por fragmento con el intervalo `z` del puente más cercano (`_RmUnderBridge`), rampa de 1,5 m. Se ajusta DA8-07 (que pedía −60 % / −40 %) |
| **DT8-08** | El puente no se trama ni se corta: `ShoreSegments.TrailingLandmarks` (Core) lo sigue dando hasta 15 m detrás de la cámara aunque ya salió de la lista del legacy (`relZ < 1`); la lista del legacy no cambia. Cara del lado de la cámara en el ancla `z₀ + 20`, tablero hasta `z₀ + 30` |
| **DT8-09** | Acantilado lejano de la costa **sin fase** (spec DV-81: "sin cambiar de forma"); se rechaza la fase `distance·0,002` de D8-02. Río y regata se acercan al 4 % (D8-02 adoptado) |
| **DT8-10** | Faro sobre un promontorio de **2,5 m** (no 2,8, D8-03): AC-SCN-14-10 deja subir la base ≤ 2,5 m. Desvío contra AC-SCN-17-04 solo en ese bloque (§9.3) |
| **DT8-11** | Sin columnas de luz en el agua (spec §12: son de la 6b); `Glow` solo dibuja halos, globos, linterna y haz del faro |
| **DT8-12** | Sin árboles de vereda en el malecón (`|x|` 11,5): spec DV-84 deja en `|x| < 18` solo objetos del legacy, suelo y vegetación baja. Las manzanas de relleno en `|x|` 22–120 sí (como el bosque del lago) |
| **DT8-13** | Talud de la regata desde **`|x|` = 9,8** (spec AC-SCN-18-04) hasta 11,95 (1,0 m a 25°), no 9,5–11,6 |
| **DT8-14** | Preferencia en una clase propia de App (`SceneEnvironmentChoice`, el patrón de `SeriesPanelPrefs`); `SceneSettings` no cambia (las pruebas del simulador quedan iguales) |
| **DT8-15** | Números del cartel: atlas SDF de 12 glifos horneado en el Editor desde Barlow Semi Condensed Bold (OFL, ya versionada) a `Art/Textures/Scene/MarkerGlyphs.png` + métricas en Core; si falta, glifos de 7 segmentos generados en Core (el número siempre está, DV-82) |
| **DT8-16** | Miniaturas: PNG versionado + `thumbnails.json` con la huella = FNV-1a de (constantes de arte de Core, texto de `Art/Shaders/**`, versión del horneador, parámetros). El test es de **dotnet** (lee archivos) y falla con "miniatura vieja: volver a hornear". WP-B3 re-hornea al final, después del pulido de D2 |
| **DT8-17** | "Reiniciar la app" en el Editor (VID-06, AC-SCN-21-04 [Editor-PM]) = desarmar el anfitrión y armar otro con el mismo `ROWMATE_DATA_DIR` temporal y `JsonFilePreferences`; en el build, dos corridas con argumentos de prueba prohibidos en el build normal (§6.4) |
| **DT8-18** | AC-SCN-22-05 ("no reserva memoria por cuadro") se mide con el kit listo: mientras se arma un kit nuevo en reposo hay creación de mallas, en tramos de ≤ 3 ms y nunca durante una animación (la regla de la 6a) |
| **DT8-19** | **Colocación 3D del río sin cruces** (gate QA-G8-01, §2.4.1): ancla de edificio y club = su cara hacia la cámara en el `z` y la `x` del legacy (el volumen crece hacia +z); el fondo se recorta contra la casa siguiente y el puente; todo volumen de casa en `\|x\|` ≥ 10,1; el puente termina en `\|x\|` 12,5; rampa paralela al muro en `\|x\|` 8,5–10,0. El generador del legacy no cambia: la paridad (AC-SCN-14-10) es del ancla, no del contorno |

---

## 1. Árbol de archivos

### 1.1 Core y App (puro) — WP-A

| Archivo | Marca | Contenido (fuente legacy) |
|---|---|---|
| `Core/Scene/Environments/RiverEnvironment.cs` | N | 0,35 / 2,0; `Generate` (`river_environment.dart:18-42`) |
| `Core/Scene/Environments/CoastEnvironment.cs` | N | 0,8 / 4,0; tinte `#0E6B7A` 30 %; tope 60; `Generate` (`coast_environment.dart:12-48`) |
| `Core/Scene/Environments/RegattaEnvironment.cs` | N | 0,3 / 1,5; tope 200; `Generate` (`regatta_environment.dart:12-40`) |
| `Core/Scene/Environments/EnvironmentCatalog.cs` | N | `Create(EnvironmentId)`, `All` en el orden del selector; `EnvironmentProfile` (rolido, cámara, bruma, horizonte) |
| `Core/Scene/Environments/EnvironmentKeys.cs` | N | `ToKey` / `TryParse` (`lake`, `river`, `coast`, `regatta`; `scene_settings.dart:11, :28`) |
| `Core/Scene/LakeEnvironment.cs` | C | se mueve a `Environments/` (mismo namespace; `.meta` conservado con `git mv`) |
| `Core/Scene/EnvironmentId.cs` | C | solo el comentario |
| `Core/Scene/ShoreSegments.cs` | C | `TrailingLandmarks` (DT8-08); `MaxPropsPerSegment` por escenario (regata 23, río 16, costa 4) |
| `Core/Scene/CoastSwell.cs` | N | la ola (AC-SCN-12-01), fases en `double` (DT8-06), muestreo del casco (AC-SCN-06-04) |
| `Core/Scene/WaterShaderInputs.cs` | N | las 28 entradas de `water_painter.dart:29-60` (LT-124; contrato, no se dibuja) |
| `Core/Scene/DegradedWater.cs` | C | los tres tramos con `TintWater` del escenario |
| `Core/Scene/BridgeShade.cs` | N | `o(z)` con rampa de 1,5 m y el factor 0,4 (DT8-07) |
| `Core/Scene/HorizonModel.cs` | C | `HorizonSpec.River/Coast/Regatta`; `Skyline`, `CoastFarCliff`, `RegattaBackdrop`; px del legacy en TV → ángulo (`px/411` rad) |
| `Core/Scene/NightLights.cs` | C | luces por tipo (ventanas, farol, faro, *skyline*, torre) con su minuto (DV-60) |
| `Core/Scene/FacadeWindows.cs` | N | máscara del legacy: `DartRandom(semilla)`, piso por piso, 3 ventanas, `< 0,6` (`shore_painter.dart:141-152`); 24 bits |
| `Core/Scene/DistanceMarkerText.cs`, `MarkerGlyphs.cs` | N | `"<metros> m"` sin separador (`shore_painter.dart:273`); métricas del atlas, ajuste a 1,60 m, glifos de 7 segmentos de respaldo (DT8-15) |
| `Core/Scene/SeagullFlight.cs` | N | 5 gaviotas: el recorrido de `coast_environment.dart:72-87` pasado a ángulos (arte §3.4) |
| `Core/Scene/CrowdAtlas.cs` | N | atlas procedural 512 × 256 de 9 cuadros (DA8-12), píxeles en un `byte[]` |
| `Core/Scene/ThumbnailRule.cs` | N | la escena de la miniatura: 12:00, `d` 20, `t` 0, `relZ < 150`, cámara de 152 × 96 dp; `BoxDownsample` 4× |
| `Core/Scene/ThumbnailFingerprint.cs` | N | la huella de DT8-16 |
| `Core/Scene/TerrainProfile.cs` | C | cortes de río, costa y regata (§2.4) |
| `Core/Scene/PropPlacement.cs` | C | pie de los objetos nuevos (muro, playa, talud, promontorio); `RiverPlacement`: volúmenes y rampas del río sin cruces (DT8-19, §2.4.1) |
| `Core/Scene/EnvironmentFill.cs` | N | relleno con sales propias: manzanas, rocas y matas de la costa, álamos, público; nunca consume el generador del legacy |
| `Core/Scene/SceneSession.cs`, `SceneFrame.cs` | C | toma el `EnvironmentProfile`; deja en el cuadro las fases de la ola, el puente cercano, las gaviotas y las luces por tipo |
| `Core/Scene/Stroke/BoatMotion.cs` | C | `Roll(t, k)` con `k` del escenario; `SwellHeave`, `SwellPitch` (τ 0,3 s) |
| `Core/Scene/ChaseCameraMotion.cs` | C | multiplicador de la respiración y tope (± 3 dp; ± 5,4 en la costa) |
| `Core/Scene/Geometry/{BuildingMesh,BoathouseMesh,BridgeMesh,LampMesh,LaneBuoyMesh,QuayWallMesh,CliffMesh,LighthouseMesh,UmbrellaMesh,GrandstandMesh,FinishTowerMesh,FlagMesh,MarkerMesh,PoplarMesh,CityBlockMesh,SeagullMesh,FoamStripMesh}.cs` | N | los 12 tipos del catálogo + relleno, con 3 LOD donde el arte los pide (§2.6) |
| `Core/Scene/Art/{RiverArt,CoastArt,RegattaArt}.cs` | N [A crea, D dueño] | medidas, colores, triángulos, bruma, factores (arte §2–§5) |
| `Core/Scene/SlotIds.cs` | C | `ui.thumbnail`, `prop.distanceMarker.glyphs` |
| `App/Scene/SceneEnvironmentChoice.cs` | N | `Selected`, `Select(id)`, lectura demorada, el toque gana (§2.8) |
| `App/Composition/{AppComposer,AppContext}.cs` | C | `AppContext.SceneEnvironment`, armado después de las preferencias |
| `tools/oracle/engine/test/environment_vectors_test.dart`, `environment_legacy_images_test.dart`, `tools/oracle/run-oracle.ps1` | N/C | `-Environments` (§7.2) |
| `Tests/Golden/oracle/scene_{river,coast,regatta,horizons}.json` | G | vectores |
| `docs/migration/iterations/08-entornos-selector/snapshots/legacy/LEG-0{1,2,3}.png` | G | referencias del legacy |
| `Tests/EditMode/Scene/Environments/**`, `Tests/EditMode/App/SceneEnvironmentChoiceTests.cs`, `Tests/EditMode/Traceability/It08Ids.cs` | N | §7.3 |
| `Tests/EditMode/Project/{PureCodeRulesTests,RepoHygieneTests}.cs` | C | `Core/Scene/Environments` sin `UnityEngine` ni reloj; miniaturas y `thumbnails.json` versionados; nada de `Assets/ThirdParty` |

### 1.2 Unity — WP-B (los *shaders* son de WP-D)

| Archivo | Marca | Contenido |
|---|---|---|
| `Scene/SceneShaderIds.cs` | C [B0] | nombres de §8.4 |
| `Scene/Slots/EnvironmentDefinition.cs`, `SceneWorldDefinition.cs` | C [B0] | los 4 escenarios (`Environment(EnvironmentId)`), el atlas de glifos; `WithEnvironment` para pruebas |
| `Editor/Bootstrap/SceneAssetsBuilder.cs` | C [B0] | `Scenes/Environments/{River,Coast,Regatta}.asset` con sus slots en `Procedural`; *bootstrapper* v6 |
| `Scene/Environments/EnvironmentKit.cs` | N [B1] | lo del escenario: `SegmentStore`, mallas por tipo y LOD, texturas, tiras del horizonte, materiales de la orilla; `Dispose` |
| `Scene/Environments/{GlowRenderer,MarkerTextRenderer,CrowdRenderer,SeagullRenderer,FoamRenderer}.cs` | N [B1] | un `RenderMeshInstanced` cada uno, arreglos reservados al armar el kit |
| `Scene/SceneWorldBuilder.cs` | C [B1] | `BuildCommon()` + `BuildKit(EnvironmentId)` en unidades (§3.2) |
| `Scene/SceneHost.cs` | C [B1] | escenario elegido en `Preload` y `Open`; cambio de kit; modo miniatura; controles de prueba (§3.5) |
| `Scene/{SceneDirector,ShoreStreamer,FillStreamer,TerrainStreamer,SegmentStore,HorizonRenderer,InstanceBatch,SceneLighting}.cs` | C [B1] | los tipos nuevos, el `float4` por instancia (DA8-03), el puente que sigue, el oleaje, la luz bajo el puente |
| `Scene/Boat/BoatVisual.cs` | C [B1] | arfada y cabeceo de la ola |
| `Scene/AssemblyInfo.cs` | C [B1] | `InternalsVisibleTo("RowMate.Editor")` (el horneador usa el modo miniatura) |
| `Editor/Bootstrap/SceneMaterialsBuilder.cs` | C [B1] | materiales y variantes de los 5 *shaders* nuevos y las palabras clave nuevas |
| `Editor/Thumbnails/EnvironmentThumbnailBaker.cs` | N [B1] | menú `RowMate/Escena/Hornear miniaturas` y `-executeMethod …BakeFromCommandLine` |
| `Editor/Fonts/MarkerGlyphBaker.cs` | N [B1] | el atlas SDF de 12 glifos (DT8-15) |
| `Art/Textures/Thumbnails/{lake,river,coast,regatta}.png`, `thumbnails.json`; `Art/Textures/Scene/MarkerGlyphs.png` | G [B1, B3] | versionados (procedurales, sin terceros) |
| `Presentation/Workout/EnvironmentPicker.cs`, `EnvironmentCard.cs` | N [B1] | §4 |
| `Presentation/Workout/WorkoutScreenView.cs` | C [B1] | el título, la fila, la precarga al cambiar |
| `Presentation/UiContext.cs`, `Bootstrap/{UiAssets,AppRoot,SceneBridge}.cs` | C [B1] | las 4 miniaturas; el escenario al puente |
| `Snapshots/EnvironmentCatalog.cs`, `EnvironmentMarks.cs` | N [B2] | el conjunto `it8` (§6) |
| `Snapshots/{SnapshotCatalog,SnapshotCli,SnapshotRunner,ScenarioScript,SnapshotManifest}.cs` | C [B2] | `-snapshotSet it8` (por defecto), `-prefs env=<id>`, sondas |
| `SimulatorPresentation/FrameTimeProbe.cs`, `Editor/Build/BuildOutputInspector.cs` | C [B2] | `[PERF]` por escenario; argumentos de prueba prohibidos en el build normal |
| `tools/unity/{capture-snapshots.ps1,README.md}`, `tools/unity/bake-thumbnails.ps1` | C/N [B2] | `-Set it8`; horneado en *batchmode* con la guardia de `ProjectSettings` |
| `Tests/UnityEditMode/{EnvironmentAssetsTests,EnvironmentMaterialsTests,ThumbnailAssetsTests}.cs` | N | §7.3 |
| `Tests/PlayMode/Scene/{EnvironmentSceneTests,EnvironmentParityTests,EnvironmentSwitchTests,EnvironmentSlotTests,BridgePassTests}.cs`, `Tests/PlayMode/Workout/{EnvironmentPickerTests,EnvironmentRestartTests}.cs` | N | §7.3 |
| `Tests/PlayMode/Scene/SceneBudgetTests.cs`, `Tests/UiSupport/{FrameAllocations,UiHostOptions,UiTestHost}.cs`, `Tests/PlayMode/WorkoutRestLayoutTests.cs` | C | por escenario; `UiHostOptions.Environment`; AC-WKT-12-02 invertido |
| `docs/migration/architecture/adr/023-kits-de-escenario.md`, `ARCHITECTURE.md` §14.5 | N/C [B0, B3] | DT8-01; miniaturas de 456 × 288 |

### 1.3 Arte — WP-D

`Art/Shaders/Scene/{Facade,Glow,Foam,Crowd,MarkerText}.shader` (N); `Prop.shader` (C: tela de banderas y sombrillas, cabeceo de boyas por instancia), `WaterDegraded.shader` (C: `RM_WATER_SWELL`, bajo el puente), `Terrain.shader` (C: adoquín, arena con franja húmeda, césped con franjas por color de vértice), `Horizon.shader` (C: ventanas emisivas del *skyline*); `Art/Shaders/Include/RowMateLighting.hlsl` (C: `_RmUnderBridge`); `Core/Scene/Art/ShaderArt.cs` (C) y, desde D1, dueño de `RiverArt`, `CoastArt`, `RegattaArt`; `Presentation/Motion/EnvironmentCardMotion.cs` (N); `Presentation/Styles/EnvironmentPicker.uss` (N) y su línea en `RowMateTheme.tss` (excepción de dueño); `04-design-notes.md`; ART_BIBLE §7.3–§7.6, ASSET_SLOTS `ui.thumbnail`.

**Asmdefs**: ninguno nuevo ni referencias nuevas. **Manifest**: sin cambios.

---

## 2. Core puro (WP-A)

Reglas de la 6a: `noEngineReferences`, C# 9, sin asignar en `Tick` ni en `Generate` (búferes del *pool*), cultura invariante, `DartMath.Mod`, mismo orden de operaciones que el legacy en lo que es contrato.

### 2.1 Los generadores (AC-SCN-14-08, -16-01 a -03, -17-02, -17-03, -18-01 a -03)

```csharp
public sealed class RiverEnvironment : ShoreEnvironment    // river_environment.dart:18-42
{
    public override EnvironmentId Id => EnvironmentId.River;
    public override double WaveAmplitude => 0.35;  public override double WaveScale => 2.0;
    public override int MaxPropsPerSegment => 16;
    public override void Generate(long k, DartRandom rnd, ShorePropBuffer into);
}
public sealed class CoastEnvironment : ShoreEnvironment    // coast_environment.dart:12-48
{
    public const double CliffX = -14;
    public override int MaxVisibleProps => 60;
    public override SceneColor TintWater(in SceneColor c);   // SceneColor.Lerp(c, #0E6B7A, 0.3) (Color.lerp de Flutter)
}
public sealed class RegattaEnvironment : ShoreEnvironment  // regatta_environment.dart:12-40
{
    public override int MaxVisibleProps => 200;  public override int MaxPropsPerSegment => 23;
}
```

| Escenario | Orden exacto de tiradas y objetos (los mismos operandos en el mismo orden) |
|---|---|
| Río | por orilla (−1, +1): `z = z₀ + u·8`; mientras `z < z₀ + 50`: `boathouse = u < 0,3`; `x = side·(BankX + 4 + u·4)`; `scale = 0,6 + u·0,4`; `seed = NextInt(1000)`; `z += 15 + u·15`. Después, en esa orilla, faroles en `side·(BankX + 0,5)` a `z₀ + 12,5` y `+ 37,5`, y la boya `(side·3; z₀ + 25; LaneBuoy; seed 1)`. Al final, si `k mod 16 = 5`, puente `(0; z₀ + 20; seed k)` |
| Costa | si `k mod 5 = 2`: 3 sombrillas (`x = CliffX + 1 + u·3`, `z = z₀ + 8 + i·14 + u·6`, `seed = NextInt(1000)`); si no, acantilados en `z₀` y `z₀ + 25` (`scale = 0,8 + u·0,5`, `seed = NextInt(1000)`). Si `k mod 20 = 10`, faro `(CliffX; z₀ + 30)` |
| Regata | `color = (k div 5) mod 3`; para `z` de `z₀` a `z₀ + 40` cada 10 y `x` en −9, −3, 3, 9: boya `seed color`; si `k mod 5 = 0`, cartel `(−10,5; z₀; seed = round(50k))`; `k mod 20 = 4` tribuna `(12; z₀ + 10)`; `k mod 20 = 12` torre `(−12; z₀ + 10)`; bandera `(k par ? 10,5 : −10,5; z₀ + 30; seed k)`. No consume |

- Los argumentos con nombre de Dart se evalúan en el orden escrito (como en el lago): en el río `x`, después `scale`, después `seed`.
- `k` entero de 64 bits, `k ~/ 5` = división entera hacia cero para `k ≥ 0` (los segmentos negativos no se piden; `ShoreSegments` empieza en 0).
- **Ventanas, bordes y siluetas** usan su propio `DartRandom(seed)` como el legacy (no tocan el del segmento).

### 2.2 Lista visible, topes y el puente que sigue (AC-SCN-14-08, -09; DT8-08)

- `ShoreSegments` no cambia su regla (6a §2.11): `[d + 1; d + 400]`, de lejos a cerca, tope del escenario con hitos primero. Los empates en `z` (faroles izquierda/derecha, las 4 boyas de cada fila, cartel con la fila `z₀`) se comparan contra el oráculo ordenando cada tramo de `z` iguales por (tipo, `x`, semilla) en los dos lados (la regla de la 6a).
- `public int TrailingLandmarks(double distance, double behind, ShorePropBuffer into)`: los `Bridge` de los segmentos en caché con `z ∈ [d − behind; d + 1)` (`behind` = 15). Lo dibuja `ShoreStreamer` sin tramado; no entra en la lista ni en el tope (la lista sigue igual al legacy, LT-101).
- `CapEnvironment` de la 6a se repite con los topes 60 y 200 contra el oráculo (AC-SCN-14-05, cambia).

### 2.3 Oleaje, agua y rolido (AC-SCN-12-01, -17-01, -06-04; DT8-05, DT8-06)

```csharp
public static class CoastSwell
{
    public const double ColorGain = 0.25, HullScale = 0.15, PitchBase = 8.2;
    public static double Height(double x, double zWorld, double t, double scale);       // sin(zw/s + 0,6t)·0,6 + sin((0,7zw + 1,3x)/s + 0,9t)·0,4
    public static void Phases(double distance, double t, double scale, out double p1, out double p2);   // DT8-06, Mod 2π en double
    public static double ColorFactor(double h, double amplitude);                         // 1 + h·A·0,25
    public static void SampleHull(double distance, double t, double scale, out double heaveM, out double pitchRad);
        // y = 0,15·h en z 13,5 / 9,5 / 5,3 (x 0): arfada = promedio, cabeceo = atan((y_proa − y_popa)/8,2)
}
public static class WaterShaderInputs { public static void Uniforms(in SceneCamera cam, SceneState s, in ScenePalette p, in SceneLight l, ShoreEnvironment e, double[] into28); } // LT-124
```

- `SceneSession`: con `WaveAmplitude ≥ 0,5` (solo la costa), filtra arfada y cabeceo con τ 0,3 s (`SwellFilter`, reservado) y los suma a `BoatMotionSample` del cuadro (también en pausa; el tiempo de escena corre). Nunca toca `State.Distance` (AC-SCN-08-11).
- Rolido: `BoatMotion.Roll(t, profile.RollK)` con `k` lago 0,5, río 0,8, costa 2,0, regata 0,4 (AC-SCN-06-04).
- Cámara: `ChaseCameraMotion` toma `profile.BreathingGain` (1; costa 1,8) y `profile.BreathingCapDp` (3; costa 5,4); el tope suave del cabeceo de DT7-06 usa ese tope. Reducir movimiento: la cámara × 0 (el bote sigue la ola).
- `DegradedWater.Bands` usa `TintWater` (los tres tramos y la franja con niebla, AC-SCN-17-01).

### 2.4 Terreno y colocación (AC-SCN-16-04, -17-04, -18-04; DT8-12, DT8-13)

`TerrainProfile.Height(id, x, zWorld)` (continua entre segmentos, costura < 1 mm):

| Escenario | Corte |
|---|---|
| Río | agua hasta `|x|` 9; **muro** vertical a 9 (1,20 m, 0,5 m visible bajo el agua); malecón plano a 1,20 de 9 a 12,5 y la vereda de las fachadas hasta 22; ciudad plana a 1,20 hasta 120. Rampa del club: la de §2.4.1 (paralela al muro, corta muro y malecón en su tramo de `z`) |
| Costa | derecha: mar (sin terreno; el fondo a −7). Izquierda: base de rocas en −12,5, pared desde −14 (el acantilado es un objeto, el terreno de atrás es la meseta a su alto); playa (`k mod 5 = 2`): arena desde −9,5 con 6° hasta −20, duna de 2 m a −30; transición de 10 m en `z` entre tramos |
| Regata | agua hasta 9,8; talud de 25° hasta 1,0 m (`|x|` 11,95); césped plano a 1,0 hasta 1 500 m |

`PropPlacement`: fachadas, clubes y faroles sobre 1,20 (el muro, AC-SCN-14-10 lo admite); sombrillas sobre la arena; tribuna, torre, banderas y carteles sobre el talud o el césped; faro sobre el promontorio de 2,5 m (DT8-10). dotnet en 100 km: ningún pie bajo el agua ni más de 2,5 m sobre ella salvo el muro del río.

#### 2.4.1 Volúmenes del río sin cruces (DT8-19, gate QA-G8-01)

El legacy dibuja en 2D y nada choca; en 3D, con las plantas del arte, sí (sonda del gate: 116 de 125 puentes con una casa en su `z`, 63 faroles dentro de un club, 954 pares de casas vecinas cruzadas, rampa hasta `|x|` 2,55). Reglas de `RiverPlacement` (Core, WP-A), en este orden; "casa" = edificio o club, `s` = escala, `ax` = `|x|` del legacy:

| # | Regla | Constante |
|---|---|---|
| R1 | **Ancla**: la cara hacia la cámara está en el `z` del legacy y su eje en la `x` del legacy (la marca de CAP-44 a -46 y de `EnvironmentParityTests`). El volumen crece hacia +z con el fondo nominal (edificio 10·s, club 12·s) | — |
| R2 | **Lado del agua**: todo el volumen de la casa (aleros y cornisas incluidos) en `\|x\|` ≥ 10,1; si `ax − ancho·s/2` (o el alero del club, `ax − 4,5·s`) queda más adentro, se recorta la parte interior. El eje no se mueve. Con esto ningún farol (±9,5) queda dentro de una casa y el frente nunca pisa el muro (9,0). En el legacy el frente va de ≈ 10,0 a 15,2 | `FaceMinX = 10.1` |
| R3 | **Casa siguiente** (misma orilla, la primera con `z` mayor, aunque sea del segmento `k + 1`): hueco `g = zSig − z`; fondo = `mín(nominal; g − 1)` si `g ≥ 3` (pasaje de 1 m); si no, `mín(nominal; g)` (medianera: las dos caras se tocan, no se cruzan); con `g < 0,05` la casa de adelante es solo su cara (sin cuerpo) | `HouseGap = 1.0` |
| R4 | **Puente** (`zB` = ancla, tablero hasta `zB + 10`): termina en `\|x\|` 12,5 (estribo que baja al malecón, con el arco lateral de 3 m para el paseo en 9,5–12,5); el tablero de 24 m (±12) queda entero. Casa con ancla antes del puente: fondo ≤ `zB − 0,5 − z`. Casa con ancla en `[zB − 0,5; zB + 10,5]`: su interior se recorta a `\|x\|` ≥ 12,6. Casa con ancla después: no lo toca (crece hacia +z) | `BridgeEndX = 12.5`, `BridgeClearX = 12.6`, `BridgeClearZ = 0.5` |
| R5 | **Rampa del club**: paralela al muro, en `\|x\|` 8,5–10,0 (1,5 m de ancho), 12° de 1,20 a −0,2 m = 6,6 m en `z`, arrancando a nivel del malecón frente al portón (centro de la cara del río ya recortada) hacia +z; si en `[zR; zR + 6,6]` cae un farol (± 0,5) o el puente (± 0,5), va hacia −z; si los dos sentidos chocan, ese club va sin rampa (solo pasa entre un farol y un puente). En su tramo de `z` corta el muro y el malecón (`TerrainProfile`). Si dos rampas se pisan, la del club más lejano en `z` se quita (cada una se decide sin mirar a la otra: sin cadenas) | `RampMinX = 8.5`, `RampMaxX = 10.0`, `RampLength = 6.6` |
| R6 | **Relleno**: las manzanas siguen en `\|x\|` ≥ 22 (las casas llegan a 21,5 con el alero) | — |

- Todo depende solo de los segmentos `k − 1` a `k + 1` (los pide con `PropsForSegment`, sin asignar): el resultado es el mismo en cualquier orden de armado y entre corridas. Ni `Generate` ni la lista visible cambian (LT-99 a -110 y el oráculo quedan iguales).
- API: `RiverPlacement.Volumes(long k, PropVolume[] into)` (cajas alineadas: casa, puente por piezas, farol, rampa, muro con sus cortes) y `RiverPlacement.Ramps(long k, RampCut[] into)`; `TerrainProfile` y `ShoreStreamer` las usan (el fondo y el recorte viajan en la instancia como escala por eje; la malla no cambia).
- Las medidas del catálogo (AC-SCN-20-03, ± 10 %) se miden en la malla sin recorte; el recorte de colocación solo achica el fondo o el ancho interior.
- **Test** (`EnvironmentGeometryTests`, dotnet, segmentos 0 a 1 999 = 100 km): ningún par de volúmenes del río se cruza (contacto ≤ 1 mm permitido); ninguno cruza el muro salvo la rampa en su corte y el puente que apoya en él; ninguno en `|x|` < 8,5 salvo el puente (arco, pilares, tablero) y las boyas; ancla = (`x`, `z`) del legacy en todas las casas; ≥ 95 % de los clubes con rampa (se informa la cuenta). Control positivo: con las reglas apagadas el test encuentra los cruces de la sonda del gate.

### 2.5 Siluetas (AC-SCN-19-03 a -05; DT8-09)

```csharp
public readonly struct HorizonSpec { public static HorizonSpec Lake, River, Coast, Regatta; public double HeightFraction; /* 0,12 / 0,12 / 0,08 / 0,06 */ }
public static class Skyline        // river_environment.dart:46-71: DartRandom(i·31 + 7) → alto, ancho, ventanas (r < 0,5) en ese orden
{ public static void Column(int i, double hMaxDeg, out double heightDeg, out double widthDeg, out uint windowMask); }   // 4,73° por columna
public static class CoastFarCliff { public static double Height(int step, double hMaxDeg); }   // 20 pasos, fade, sin fase (DT8-09)
public static class RegattaBackdrop { /* tierra 0,25·hMax; tribuna cada 28,6° desde 4,6°, 9,2° de ancho; torre a 20,6°; 4 banderas */ }
```

- Alto: `tan Θ = mín(f·alto/focal; tan 9°)` (DV-87). Color: `HorizonModel.Color(paleta, tono, profundidad)` = `lerp(tono; fog; 0,35 + 0,4·p)` (`environment.dart:127-129`).
- Acercamiento al 4 % (Q-65) en los tres (`ApproachScale`), forma fija: con el bote quieto, quietas; nunca se corren de costado (DV-81).
- Los px del legacy son los de TV (411 dp de ancho, focal 411): `px/411` rad. La discrepancia del arte (26,6° en DA8-05, 28,6° en §4.2) se resuelve con la cuenta: 205,5 px = **28,6°**.
- Las ventanas del *skyline* se fijan por bloque (no titilan) y se encienden con `NightLights` (ambiente < 0,6).

### 2.6 Catálogo y geometría (AC-SCN-20-03, -16-04, -17-04, -18-04)

- Los 12 tipos con las medidas de la spec (± 10 %) y las formas del arte §2.3, §3.3, §4.3; colores del legacy por variante (`seed mod 3` edificio, boya, sombrilla; `seed mod 4` bandera; pisos `3 + seed mod 4`). Material por color de vértice (`a` = material: 0 rugoso, 0,5 liso, 1 emisivo de noche), como la cabaña.
- Edificios y clubes orientados al agua (D8-01), anclados por la cara a la cámara (DT8-19, R1): esa cara conserva el ancho, el alto, las 3 ventanas por piso y los colores del legacy. Pisos y ventanas por *shader* (`Facade`) con la máscara de `FacadeWindows`; una malla por tipo de techo × 3 LOD.
- Puente (AC-SCN-16-03): arco de 17 m (arranque ±8,5 a 1,5 m, clave ≈ 5 m, la Bézier de `shore_painter.dart:165-172`), pilares 1 × 1,5, tablero 24 m entre 6,0 y 7,2, parapeto hasta 7,7, 10 m de profundidad, estribos hasta `|x|` 12,5 (DT8-19, R4; no ±24).
- Acantilado: borde de 5 escalones con `DartRandom(seed)` como el legacy (`:209-213`), 28 m en `z` (solape de 3 m, sin costuras), 7 m de fondo. En el segmento del faro, el bloque de `z₀ + 25` a 2,5 m (DT8-10).
- Cartel: tablero 1,8 × 1,0 de 1,5 a 2,5 m, de frente a la cámara; los glifos aparte (`MarkerTextRenderer`).
- Triángulos LOD0/1/2 del arte (topes en `*Art`); `EnvironmentGeometryTests` los fija.

### 2.7 Vida, luces, gaviotas, público (AC-SCN-20-04, -20-06, -19-04)

- `SceneFrame` lleva solo escalares (tiempo de escena, ambiente, niveles de `NightLights` por tipo); banderas (`sin(4t + seed)·0,2` + 2.º armónico del arte), boyas (`0,08·sin(1,7t + seed)` rad, arfada 0,02), sombrillas, público y haz del faro (12 s por vuelta) se calculan en el vértice.
- `NightLights`: cada luz con su minuto en ± 10 min y fundido de 0,4 s (DV-60), por tipo y semilla; torre y portón del club (D8-10) se suman con la misma regla.
- `SeagullFlight.Sample(int i, double t, out double azimuthRad, out double elevationRad, out double flap)`: velocidad `(8 + 3i)/411` rad/s, altura = `atan` de la `y` del legacy (`horizonY·(0,35 + 0,1i)`) con la cámara de TV, ondulación `± 4/411` rad `sin(2t + i)`, aleteo `sin(6t + i)`, tamaño `(5 + i)/5`; vuelve a la izquierda con fundido de 1 s. Opacidad `0,5 + 0,4·ambiente`.
- `CrowdAtlas.Bake(byte[] rgba)` determinista; poses por vértice (2–3 cuadros, período 1,5–4 s por semilla).

### 2.8 La preferencia (AC-SCN-21-02 a -05; LT-125, LT-126)

```csharp
public static class EnvironmentKeys
{
    public const string PreferenceKey = "scene.environment";             // scene_settings.dart:11
    public static string ToKey(EnvironmentId id);                         // lake, river, coast, regatta
    public static bool TryParse(string? key, out EnvironmentId id);       // asNameMap(): exacto, sensible a mayúsculas
}
public sealed class SceneEnvironmentChoice : IDisposable                  // App/Scene; scene_settings.dart:15-41
{
    public SceneEnvironmentChoice(IPreferences preferences, IAppLogger? logger = null);
    public IReadOnlyObservable<EnvironmentId> Selected { get; }           // Lake por defecto
    public void Select(EnvironmentId id);   // _touched = true ANTES de comparar (:37); igual → nada; si no, cambia y SetString
}
```

- Lectura: si `IsLoaded`, lee en el constructor (el primer cuadro de Workout ya lo ve: AC-SCN-21-04); si no, en `Loaded`, ignorada si hubo un toque. Valor desconocido o sin clave → lago, **sin escribir**.
- Fallo de escritura: `JsonFilePreferences` ya deja un `Warning` y conserva el valor en memoria (AC-SCN-21-03).
- La sesión guardada no registra el escenario; la hora forzada sigue sin persistir.

### 2.9 Miniaturas (AC-SCN-22-04; DT8-02, DT8-16)

- `ThumbnailRule`: hora 12:00, `d` = 20, tiempo 0, respiración 0, sin bote, remero, efectos, boya ni degradés; objetos de la lista visible con `z < d + 150`; cielo, terreno, relleno y horizonte sí; cámara del contrato con 152 × 96 dp (apaisada: horizonte al 42 %, focal = lado corto, como `SceneCamera(size)` del legacy). Render a 1 824 × 1 152 y `BoxDownsample(4)` → **456 × 288**.
- `ThumbnailFingerprint.Compute(...)`: FNV-1a 64 de (constantes de `LakeArt`, `RiverArt`, `CoastArt`, `RegattaArt`, `SkyArt`, `WaterArt`, `HourLookTable` serializadas en invariante; texto de `Art/Shaders/**` con fin de línea normalizado; `ThumbnailRule.Version`).

---

## 3. Unity (WP-B)

### 3.1 Kits sobre la base de la 6a (DT8-01)

```
SceneWorld (común, se arma una vez)         EnvironmentKit (uno en memoria)
 ├ cámara, luz, Volume, RT                   ├ SegmentStore + ShoreStreamer + FillStreamer + TerrainStreamer
 ├ cielo, estrellas, nubes, astro            ├ mallas de su catálogo (tipo × LOD) y de su relleno
 ├ agua (material por escenario: clon        ├ tiras del horizonte (HorizonRenderer)
 │   con la ola o sin ella)                   ├ Glow, MarkerText, Crowd, Seagull, Foam (los que use)
 ├ bote, remos, remero, efectos, boya        ├ texturas propias (atlas del público, glifos)
 ├ materiales comunes y TODAS las variantes   └ perfil: EnvironmentProfile, slots del escenario
 └ texturas del cielo, ruido, espuma
```

- El lago es un kit más: sus pasos de la 6a (pinos, bosque, rocas, juncos, pasto, cabaña, muelle, cordilleras) se mueven a `BuildKit(Lake)` sin cambiar nada de lo que dibujan.
- `EnvironmentDefinition` por escenario (`Scenes/Environments/*.asset`), con los slots de ASSET_SLOTS §1.3–§1.4 que ya existen en `Slots/` (`prop.*`, `vegetation.poplar`, `horizon.*`, `water.profile.*`, `water.foam`). Todos en `Procedural`: 0 avisos. Uno con fuente `Asset` que falta o no valida → el procedural y **un** `[SLOT]` (AC-SCN-20-07).

### 3.2 Armado, cambio y precarga (AC-SCN-01-18; DA8-16)

| Paso | Unidades (≤ 2–3 ms cada una, política de la 6a) |
|---|---|
| Común (primera vez) | lo de la 6a y la 7 sin la orilla + las variantes de los 4 escenarios (`WarmUpProgressively(1)` por unidad) |
| Kit | slots del escenario → texturas por filas → mallas una por unidad → materiales → `Tick` sintéticos del escenario (12 h y 22 h en su distancia de prueba) → **un** dibujo de calentamiento del kit (fuera de pantalla) |

- `SceneHost.Preload()` (lo llama `WorkoutScreenView` al quedar en reposo y **cada vez que cambia `SceneEnvironment.Selected`**): si el kit armado no es el elegido, `Readiness = Loading`, descarta el kit (sus mallas y texturas en unidades, no de una vez) y arma el nuevo. Con un movimiento finito en curso (la animación del selector), el tramo es 0 (6a §2.16).
- `Open(now)` crea la `SceneSession` con `EnvironmentCatalog.Create(elegido)`. Si el kit no está listo: la escena demorada de la 6a (fondo provisorio + fundido de 0,6 s, AC-SCN-01-07).
- Memoria: un kit por vez; `EnvironmentSwitchTests` alterna 10 veces entrando y saliendo y exige ± 5 % de la memoria del primer recorrido y 0 avisos de objetos destruidos.

### 3.3 Lo que se dibuja (con los *shaders* de WP-D)

| Pieza | Cómo |
|---|---|
| Objetos del legacy | `ShoreStreamer`: lotes por (tipo, forma, LOD) con `float4` por instancia (tono, semilla, máscara de ventanas de 24 bits en un `float` exacto, nivel de luz), armados solo cuando cambia el conjunto de segmentos (≤ 0,3 ms). Aparición 400 → 360, tramado de salida 2 → 1 m (DV-62) **salvo el puente** (DT8-08). Material `Prop` salvo edificios y clubes (`Facade`) |
| Puente | lista + `TrailingLandmarks`; `_RmUnderBridge = (zIni, zFin, rampa, 0,4)` global por cuadro desde el puente más cercano |
| Luces | `GlowRenderer`: halos de faroles (r 1,5), globos, linterna y halo del faro (r 3), haz de 2 conos de 60 m (solo de noche y con el faro a < 400 m), aditivo, 1 dibujo |
| Cartel | `MarkerTextRenderer`: quads de glifos instanciados, 1 dibujo; texto de `DistanceMarkerText` |
| Público, gaviotas, espuma | `CrowdRenderer` (1), `SeagullRenderer` (5 instancias, 1; en el cielo a 60–120 m en la dirección de `SeagullFlight`), `FoamRenderer` (tira por segmento al pie del acantilado y en la línea de la playa, latido 6,5 s) |
| Agua | `WaterDegraded` con `RM_WATER_SWELL` solo en el material de la costa (palabra clave constante en la sesión) y `_RmSwell = (A, s, φ₁, φ₂)` |
| Terreno | `TerrainStreamer` con el corte de §2.4 y el material por color de vértice (adoquín, piedra, arena húmeda, césped con franjas, pasto) |
| Horizonte | `HorizonRenderer` con las tiras del kit (columnas por acimut, ventanas emisivas en el alfa) |
| Bote | `BoatVisual` suma arfada y cabeceo de la ola al pivote del perno |

### 3.4 Miniaturas (AC-SCN-22-04, -05; DA8-14)

- `EnvironmentThumbnailBaker` (Editor, *batchmode* sin `-nographics`, compilación sincrónica): por escenario, un `SceneHost` con `ThumbnailMode` (`ThumbnailRule`), slots **forzados a `Procedural`**, nivel Alto, RT 1 824 × 1 152; lee, `BoxDownsample` de Core, `EncodeToPNG`; escribe los 4 PNG y `thumbnails.json` (escenario, tamaño, hora, huella). Dos horneados seguidos iguales byte a byte. `tools/unity/bake-thumbnails.ps1` con la guardia de `ProjectSettings`.
- Importación: sin *mipmaps*, `Clamp`, `Bilinear`, BC7 en escritorio, ASTC 4×4 en Android; las 4 ≤ 1 MB en el build (`ThumbnailAssetsTests`).
- En ejecución: `UiAssets` las referencia; `EnvironmentPicker` las muestra. Si falta una: la tarjeta con el degradé de las 12 h (`skyTop` → `skyHorizon` arriba del 42 %, `waterFar` → `waterNear` abajo) y **un** `[SLOT] ui.thumbnail.<id>`.

### 3.5 Controles de prueba (`internal`)

| Control | Para qué |
|---|---|
| `UiHostOptions.Environment` / `SceneEnvironmentChoice.Select` sobre `MemoryPreferences` | escenario de una prueba o toma de escena sola |
| `-prefs env=<id>` (`SnapshotCli`) | "escenario forzado": `prefs.json` de la carpeta temporal con `scene.environment` (o `marte`, o sin clave) |
| `ForceDistanceForTest` + `PrimeSpeedForTest(4.0)` (nuevo si no existe) | `E-x` y "en régimen desde `E-x`" |
| `ThumbnailModeForTest(bool)` | el horneador y CAP-33 a -36 |
| `UnderBridgeForTest` (lectura) | la sonda de la luz bajo el puente (CAP-43) |
| Los de la 6a y la 7 | hora a un minuto, tiempo de escena, respiración en 0, fase forzada, nivel, escena demorada |

---

## 4. Selector en Workout (AC-SCN-22-01 a -03, -05; AC-WKT-12-02)

```
#workout-rest
 ├── #workout-banner            (aviso sin remo, si aparece)
 ├── Label .rm-env-title        workoutScene, blanco 54 %, 13
 ├── EnvironmentPicker          ScrollView horizontal, mode Horizontal, scrollers ocultos, 8 arriba, 16 abajo
 │    └── EnvironmentCard × 4   focusable; 152 × 96 + 6 + nombre 13; separación 10
 │         ├── .rm-env-card__thumb   la miniatura (radio 12, recortada)
 │         ├── .rm-env-card__veil    velo negro 15 % (no elegida)
 │         ├── .rm-env-card__ring    absoluto, 2,5 #00B4D8 (elegida) — no se anima border-width (D8-11)
 │         ├── .rm-env-card__check   disco 22 + glifo check (elegida)
 │         ├── .rm-env-card__name    400 @70 %   ┐ dos etiquetas superpuestas,
 │         └── .rm-env-card__name--bold 700 blanco ┘ fundido cruzado del peso
 └── botón Entrenamiento libre  (16 después de la fila)
```

- **Comportamiento** (B): `PointerUp` en la tarjeta (miniatura o nombre) → `SceneEnvironmentChoice.Select`; `NavigationMoveEvent` ← → mueve el foco y desplaza la fila hasta ver la tarjeta + 16; `NavigationSubmitEvent` (Enter, Espacio) elige; `WheelEvent` sobre la fila desplaza en horizontal y se detiene (no mueve la página); el arrastre con el dedo es el del `ScrollView`. Habilitado sin remo. Ningún texto se corta con letra 1,3×. Las tarjetas se arman una vez (las 4 texturas ya cargadas): 0 B por cuadro.
- **Movimiento** (D, `EnvironmentCardMotion`, arte §8.2): anillo y escala con `spring.snappy`, miniatura 1 → 1,03 en 240, check, velo y nombre; con reducir movimiento, en el acto (opacidades 120 lineal). Pide 60 fps a `FramePacing` mientras anima. Contrato: `void Apply(EnvironmentCard card, bool selected, bool instant)`.
- **Adiciones del arte adoptadas** (no contradicen la spec): el check y el velo de la no elegida (que la elección no dependa solo del color).
- No suma elementos al HUD (no es HUD). Workout en reposo sigue con 0 dibujos de escena.

---

## 5. Presupuesto para 60 fps (nivel Medio)

Base medida en la 7 (lago, Medio, maniquí): 82 dibujos, 50 SetPass, 164 k triángulos. Cada escenario reemplaza la orilla del lago (tabla del arte §9.1, sin columnas de luz ni árboles de vereda):

| Escenario (peor hora) | Dibujos | SetPass | Triángulos (maniquí) | GPU estimada |
|---|---|---|---|---|
| Lago (12 h) | 82 | 50 | 164 k | 11,3 ms (la 7) |
| Río (22 h; de día con sombras) | ≈ 76 / ≈ 84 | ≈ 44 | ≈ 104 k | ≈ 10,2 ms |
| Costa (12 h; 18 h con el brillo) | ≈ 74 | ≈ 45 | ≈ 95 k | ≈ 10,2 ms |
| Regata (`E-180`, 12 h, con público) | ≈ 80 | ≈ 46 | ≈ 100 k | ≈ 10,0 ms |

- **Topes de `SceneBudgetTests`** (DA8-17, por escenario, 12 y 22 h): Medio ≤ 95 dibujos, **≤ 50 SetPass**, ≤ 220 k triángulos con el maniquí y ≤ 280 k con Y Bot (el tope medido de la 7); Alto ≤ 80 SetPass; Bajo ≤ 70 dibujos, sin sombras ni haz.
- **Regla de *shaders***: ≤ 6 pasadas propias de la orilla por escenario; todo lo nuevo usa `Prop` salvo `Facade`, `Glow`, `Foam`, `Crowd`, `MarkerText`.
- **0 B por cuadro en régimen**: `FrameAllocations.WatchScene` con su control positivo en los 4 escenarios (12 y 22 h) y en la regata `E-180`; Profiler del build de desarrollo. Las instancias se rearman solo al cambiar de segmento (sin asignar).
- **`[PERF]`** (AC-SCN-01-19) en `Builds/Windows/simulator`: `-rowmate-perf-scene 40 -rowmate-perf-env <id> -rowmate-perf-hour <h>` (prohibidos en el build normal): libre `Fuerte` en los 4 a las 12 y 22 h, y la regata en `E-180`; p99 ≤ 16,7 ms, ningún cuadro > 33 ms, 0 B. Más la entrada al inmersivo 1 s después de cambiar de escenario (DA8-16): ningún cuadro > 33 ms. Android: NO VERIFICABLE.
- Si falta margen: primero el haz y el público en Bajo, después la distancia de sombras de las fachadas a 60 m.

---

## 6. Capturas y videos (WP-B2, conjunto `it8`)

**Reglas** de la 6a §5.1 y la 7 §5: *batchmode* sin `-nographics`, compilación sincrónica, `ROWMATE_DATA_DIR` temporal por toma, reloj de prueba, español, simulador sin ruido, nivel Alto, pasos de 1/60 s, **`it8-A` e `it8-B` iguales byte a byte**, `manifest.json` e `INDEX.md` sin rutas de la PC, guardia de `ProjectSettings`, maniquí. Destino: `docs/migration/iterations/08-entornos-selector/snapshots/`. **Matriz** = libre `R-4`, HUD visible, tiempo de escena 3,2 s, `c` = 0,18, hora fijada, distancia del escenario (lago `E-920`, río `E-250`, costa `E-500`, regata `E-180`). "Escena sola" = `HideForCapture` + degradés apagados + respiración 0.

| ID | Cómo se arma (`EnvironmentCatalog`) | Sondas en el `manifest` |
|---|---|---|
| CAP-01 a -08 | matriz, lago, 6/12/18/22 h, TV (01–04) y TH (05–08) | iguales a la toma equivalente de la 7 donde exista; **ventana y frente de la cabaña** (issue #9) a las 22 h |
| CAP-09 a -16 | matriz, río, ídem | puente a `relZ` 20; ventana de edificio, farol y ventana del *skyline* a las 22 h (`#FFE082`, `#FFF8E1`, `#FFE08A` ± 3) |
| CAP-17 a -24 | matriz, costa, ídem | faro a `relZ` 30; halo del faro a las 22 h; tinte del agua; arfada y cabeceo del cuadro |
| CAP-25 a -32 | matriz, regata, ídem | colores de boyas por tramo; cartel `250 m` a `relZ` 70; tribuna a 30 |
| CAP-33 a -36 | las 4 miniaturas 1:1 (copia de los PNG horneados) | huella de `thumbnails.json` al día |
| CAP-37 | Workout en reposo, TV, sin remo, lago | orden de los elementos (y), tarjetas visibles (2 enteras + la 3.ª asomando) |
| CAP-38 | Workout en reposo, W1280, con remo, río (`-prefs env=river`) | 4 tarjetas dentro del ancho; anillo en río |
| CAP-39 | Workout en reposo, TV, inglés, costa, foco en `Regatta course` | `Scenery`; foco en la 4.ª; la fila desplazada |
| CAP-40 / -41 | escena sola, regata, TV 12 h `E-220` / TH 22 h `E-720` | alto de las cifras en dp y contraste tablero/cifra |
| CAP-42 | escena sola, costa, TH 12 h `E-500` | `h` en el centro de la pantalla; cabeceo del bote |
| CAP-43 | escena sola, río, TV 12 h `E-266` | `o` bajo el puente; luminancia del casco contra la toma sin puente |
| CAP-44 a -46 + LEG-01 a -03 | escena sola, V390, 12 h, con marcas (`EnvironmentMarks`): río `E-250`, costa `E-500`, regata `E-180`; los LEG del oráculo | por objeto: `x` de pantalla del eje contra el legacy (± 2 dp) y desnivel de la base (≤ 2,5 m; muro 1,2) |
| VID-01 | TV 20 s, lago, en régimen desde `E-900` | — |
| VID-02 | TH 20 s, río, desde `E-200` | instante en que `relZ` del puente cruza 0 (≈ 17,5 s); `o` por cuadro |
| VID-03 | TV 20 s, costa, desde `E-470` | faro a `relZ` 0 a ≈ 15 s; gaviotas; arfada |
| VID-04 | TH 20 s, regata, desde `E-190` | tribuna a ≈ 5 s; cartel `250 m` y cambio de color a ≈ 15 s |
| VID-05 | TH 10 s, costa, escena sola, quieto y en pausa | arfada y cabeceo por cuadro (≠ 0) |
| VID-06 | W1280 ≈ 30 s: elegir `Costa` → desarmar el anfitrión → armar otro con la misma carpeta (DT8-17) → `Entrenamiento libre` | escenario al primer cuadro de Workout; escenario de la sesión |
| DEMO-01 a -05 | W1280, pasos 1 a 5 de la spec §1 | — |

LEG-01 a -03: el oráculo dibuja el `SkyPainter`, el respaldo del `WaterPainter` y el `ShorePainter` del legacy a la misma distancia, V390, 12 h, con una marca en la base de cada objeto (§7.2).

---

## 7. Tests

### 7.1 Suites y comandos

| Suite | Comando |
|---|---|
| dotnet | `powershell -NoProfile -File tools/dotnet/run-tests.ps1 -ArtifactsPath <scratch>/it8/…` |
| Unity EditMode / PlayMode (con y sin simulador) | `tools/unity/run-unity-tests.ps1 -Platform EditMode|PlayMode [-Simulator]` |
| Oráculo | `powershell -NoProfile -File tools/oracle/run-oracle.ps1 -Environments` (con la guardia del legacy) |
| Miniaturas | `tools/unity/bake-thumbnails.ps1` (dos veces: iguales) |
| Arnés | `tools/unity/capture-snapshots.ps1 -Set it8 -Out …` (A y B) |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` (solo `Builds/Windows/simulator`; en uso → cada 2 min hasta 20, si no NO VERIFICABLE) |

### 7.2 El oráculo (`-Environments`)

`environment_vectors_test.dart`: para río, costa y regata, el resumen FNV-1a de cada segmento 0 a 2 000 (y filas completas de 0 a 99) con el generador real del legacy; las listas visibles a 0, 1, 49,5, 50, 920, 1 234, 5 000 y 42 195 m; la lista de `CapEnvironment` con los topes 60 y 200; las columnas del *skyline* (alto, ancho, ventanas) 0 a 63, el perfil del acantilado y los rectángulos de la regata en TV; los 28 uniformes del agua de LT-124. `environment_legacy_images_test.dart`: LEG-01 a -03 con marcas.

### 7.3 Archivos de test

| Archivo | Qué prueba |
|---|---|
| `Environments/RiverEnvironmentTests.cs` | LT-99 a -101, LT-110 tal cual; faroles y boyas por metro; puente en 270, 1 070, 1 870 |
| `Environments/CoastEnvironmentTests.cs` | LT-102 a -104; playa en 100–150, 350–400…; faro en 530, 1 530 |
| `Environments/RegattaEnvironmentTests.cs` | LT-105 a -109; colores por tramo de 250 m; carteles; tribuna 210, torre 610; banderas alternadas |
| `Environments/EnvironmentGoldenTests.cs` | segmentos 0–2 000 bit a bit; listas visibles; tope con `CapEnvironment` (AC-SCN-14-05) |
| `Environments/WaterInputsTests.cs` | LT-124 tal cual; tinte de la costa por hora |
| `Environments/CoastSwellTests.cs` | fórmula contra `water.frag` sin ruido; fases en cinta = `zMundo` directo (± 1e-9) a 42 km; casco: arfada ± 0,12, cabeceo, τ; nada si `A < 0,5`; distancia intacta |
| `Environments/BoatRollAndCameraTests.cs` | `k` por escenario; respiración × 1,8 y tope 5,4 en la costa; 3 en los otros; reducir movimiento |
| `Environments/HorizonEnvironmentTests.cs` | *skyline*, acantilado y regata contra el oráculo (± 1 dp en TV); colores por hora; tope 9°; quietas con el bote quieto; sin corrimiento lateral |
| `Environments/FacadeWindowsTests.cs`, `NightLightsEnvironmentTests.cs` | máscara = legacy en 20 semillas; encendido < 0,6 y escalonado; a 6/12/18 h todo apagado |
| `Environments/EnvironmentGeometryTests.cs` | medidas del catálogo ± 10 %; puente; triángulos por LOD; paletas por variante; **río sin cruces en 100 km y ancla = legacy** (§2.4.1, con control positivo) |
| `Environments/TerrainEnvironmentTests.cs`, `EnvironmentFillTests.cs` | cortes, costuras, nada flota (100 km); cortes de rampa en el muro y el malecón continuos entre segmentos; relleno determinista en cualquier orden; no consume el generador; `|x| < 18` solo bajo |
| `Environments/BridgeTests.cs` | `TrailingLandmarks`; `o(z)` continuo; 0,4 |
| `Environments/MarkerTextTests.cs`, `SeagullFlightTests.cs`, `CrowdAtlasTests.cs` | `"250 m"`, `"1000 m"`, `"10000 m"` en 1,60 m; gaviotas deterministas; atlas igual en dos corridas |
| `Environments/ThumbnailTests.cs` | `ThumbnailRule` (`relZ < 150`), `BoxDownsample`, **huella de los PNG versionados al día** |
| `App/SceneEnvironmentChoiceTests.cs` | LT-125, LT-126; `marte` y sin clave → lago sin escribir; toque antes de la lectura (también el lago); misma tarjeta no escribe; fallo de escritura |
| `UnityEditMode/EnvironmentAssetsTests.cs`, `EnvironmentMaterialsTests.cs`, `ThumbnailAssetsTests.cs` | assets de los 4 escenarios y slots en `Procedural`; los 5 *shaders* compilan y la colección los cubre; importación de miniaturas y ≤ 1 MB |
| `PlayMode/Scene/EnvironmentSceneTests.cs` | LT-111 a -123 y LT-127 a -138 (`LogAssert.NoUnexpectedReceived`) |
| `PlayMode/Scene/EnvironmentParityTests.cs` | AC-SCN-14-10: eje en la `x` del legacy ± 2 dp en V390, H844 y los 5 tamaños |
| `PlayMode/Scene/BridgePassTests.cs` | puente dibujado de `relZ` 20 a −10 sin tramado; `o` en el casco |
| `PlayMode/Scene/EnvironmentSwitchTests.cs`, `EnvironmentSlotTests.cs` | AC-SCN-01-18; AC-SCN-20-07 (slot que falta → 1 aviso) |
| `PlayMode/Scene/SceneBudgetTests.cs` (C), `FrameAllocations` (C) | §5 por escenario |
| `PlayMode/Workout/EnvironmentPickerTests.cs` | orden y medidas, estados, toque, teclado, rueda sin mover la página, letra 1,3×, 0 dibujos de escena, 0 B en el desplazamiento |
| `PlayMode/Workout/EnvironmentRestartTests.cs` | AC-SCN-21-04 con `JsonFilePreferences` en carpeta temporal: los 4, primer cuadro, toque gana |
| `PlayMode/WorkoutRestLayoutTests.cs` (C) | AC-WKT-12-02: ahora `Escenario` **sí** se ve, en su orden |

### 7.4 Matriz AC → test (`It08Ids.cs`)

| AC | dotnet | Unity / captura / dispositivo |
|---|---|---|
| AC-SCN-14-08 (LT-99 a -107) | `River/Coast/RegattaEnvironmentTests`, `EnvironmentGoldenTests` | — |
| AC-SCN-14-09 (LT-109) | `RegattaEnvironmentTests`, `EnvironmentGoldenTests` | — |
| AC-SCN-14-10 | `HorizonEnvironmentTests` (apoyo), `EnvironmentGeometryTests` (ancla de las casas) | `EnvironmentParityTests`; CAP-44 a -46 con LEG-01 a -03 |
| AC-SCN-16-01, -02 | `RiverEnvironmentTests` | CAP-09 a -16 |
| AC-SCN-16-03 (LT-110) | `RiverEnvironmentTests`, `BridgeTests` | `BridgePassTests`; CAP-43, VID-02 |
| AC-SCN-16-04 | `EnvironmentGeometryTests`, `TerrainEnvironmentTests` | CAP-09 a -16 |
| AC-SCN-17-01 | `WaterInputsTests` | CAP-17 a -24 |
| AC-SCN-17-02, -03 | `CoastEnvironmentTests` | CAP-20, -24, VID-03 |
| AC-SCN-17-04 | `EnvironmentGeometryTests`, `TerrainEnvironmentTests` | CAP-17 a -24, CAP-42 |
| AC-SCN-12-01 (LT-124) | `CoastSwellTests`, `WaterInputsTests` | CAP-42, VID-05 |
| AC-SCN-06-04 | `CoastSwellTests`, `BoatRollAndCameraTests` | VID-03, VID-05 |
| AC-SCN-18-01 (LT-108) | `RegattaEnvironmentTests` | CAP-25 a -32 |
| AC-SCN-18-02 | `RegattaEnvironmentTests`, `MarkerTextTests` | CAP-40, -41, VID-04 |
| AC-SCN-18-03, -04 | `RegattaEnvironmentTests`, `EnvironmentGeometryTests` | CAP-25 a -32, VID-04 |
| AC-SCN-19-03 a -05 | `HorizonEnvironmentTests`, `SeagullFlightTests` | CAP-12, -16, VID-03, CAP-25 a -32 |
| AC-SCN-20-03 | `EnvironmentGeometryTests` | `EnvironmentSceneTests` (LT-123: 15 tipos) |
| AC-SCN-20-04 | `FacadeWindowsTests`, `NightLightsEnvironmentTests` | CAP-12, -16, -24 (sondas) |
| AC-SCN-20-05 (LT-111 a -123) | — | `EnvironmentSceneTests` |
| AC-SCN-20-06 | `CrowdAtlasTests`, `SeagullFlightTests` | VID-01 a -04 |
| AC-SCN-20-07 | `RepoHygieneTests` | `EnvironmentSlotTests`, `EnvironmentAssetsTests`; QA: `git ls-files`, CI |
| AC-SCN-21-02, -03 (LT-125) | `SceneEnvironmentChoiceTests` | `EnvironmentPickerTests` |
| AC-SCN-21-04 (LT-126) | `SceneEnvironmentChoiceTests` | `EnvironmentRestartTests`; VID-06; [Device-Win] 4 de 4 |
| AC-SCN-21-05 | — | `EnvironmentSwitchTests` (la sesión usa el elegido); DEMO-02 a -05 |
| AC-SCN-22-01, -02, -03 | — | `EnvironmentPickerTests`; CAP-37 a -39 |
| AC-SCN-22-04 | `ThumbnailTests` | `ThumbnailAssetsTests`; CAP-33 a -36 |
| AC-SCN-22-05 | — | `EnvironmentPickerTests`, `ThumbnailAssetsTests`; `[PERF]` |
| AC-SCN-01-17 (LT-127 a -138) | — | `EnvironmentSceneTests` |
| AC-SCN-01-18 | — | `EnvironmentSwitchTests`; `[PERF]` de la entrada |
| AC-SCN-01-19 | — | `SceneBudgetTests`, `FrameAllocations`; `[PERF]`; Android NO VERIFICABLE |
| AC-WKT-12-02 (cambia) | — | `WorkoutRestLayoutTests`; CAP-37 |
| AC-SCN-21-01 (cambia) | `SceneEnvironmentChoiceTests` | (reemplazado por -02 a -05) |
| AC-SCN-14-05 (cambia) | `EnvironmentGoldenTests` | — |
| Se vuelven a verificar (spec §4) | las suites de la 6a y la 7 | las de la 6a y la 7 parametrizadas por escenario (`[ValueSource]` de los 4) |

---

## 8. Paquetes de trabajo

### 8.0 Orden y reglas

**WP-A** → **WP-B0** (assets, `SceneShaderIds`, ADR) → **WP-D1** (*shaders*) → **WP-B1** (kits, escena, selector, horneado) → **WP-B2** (arnés `it8`, `[PERF]`, build) → **WP-D2** (pulido con las tomas) → **WP-B3** (re-hornear miniaturas, segunda corrida A/B, docs). Una instancia de Unity; nada de editar fuentes mientras corre; `ROWMATE_DATA_DIR` temporal; `ProjectSettings` solo por el *bootstrapper* con la guardia; UTF-8 sin BOM duplicado (revisar `á`, `ñ`, `—` en cada `.cs` nuevo); repo público.

### 8.1 WP-A — Core puro, App y oráculo (dotnet)

Todo §1.1. **Entrega**: `dotnet test` verde sin avisos; `It08Ids` con los ID dotnet pasando; oráculo corrido con el legacy limpio. Unity no se abre.

### 8.2 WP-B — Unity

- **B0**: `Scene/SceneShaderIds.cs`, `Scene/Slots/{EnvironmentDefinition,SceneWorldDefinition}.cs`, `Editor/Bootstrap/SceneAssetsBuilder.cs`, `Scenes/Environments/*.asset` (G), `docs/migration/architecture/adr/023-kits-de-escenario.md`, `Tests/UnityEditMode/EnvironmentAssetsTests.cs`.
- **B1**: `Scene/Environments/**`, `Scene/{SceneWorldBuilder,SceneHost,SceneDirector,ShoreStreamer,FillStreamer,TerrainStreamer,SegmentStore,HorizonRenderer,InstanceBatch,SceneLighting,AssemblyInfo}.cs`, `Scene/Boat/BoatVisual.cs`, `Editor/Bootstrap/SceneMaterialsBuilder.cs`, `Editor/Thumbnails/**`, `Editor/Fonts/MarkerGlyphBaker.cs`, `Art/Textures/{Thumbnails,Scene}/**` (G), `Presentation/Workout/{EnvironmentPicker,EnvironmentCard,WorkoutScreenView}.cs`, `Presentation/UiContext.cs`, `Bootstrap/{UiAssets,AppRoot,SceneBridge}.cs`, las pruebas Unity de §7.3.
- **B2**: `Snapshots/**`, `tools/unity/{capture-snapshots.ps1,bake-thumbnails.ps1,README.md}`, `SimulatorPresentation/FrameTimeProbe.cs`, `Editor/Build/BuildOutputInspector.cs`, `docs/migration/iterations/08-entornos-selector/snapshots/**` (salvo `legacy/`), el build y el `[PERF]`.
- **B3**: re-horneado, `it8-A`/`it8-B` finales, `ARCHITECTURE.md` §14.5.

### 8.3 WP-D — Designer

- **D1**: los 5 *shaders* nuevos y los cambios de §1.3 con el contrato de §8.4; dueño de `RiverArt`, `CoastArt`, `RegattaArt`, `ShaderArt`; `EnvironmentCardMotion.cs`, `EnvironmentPicker.uss` y su línea en `RowMateTheme.tss`.
- **D2**: ajustes con las tomas de `it8` (colores, materiales, alfa de halos, legibilidad del cartel de noche, espuma, público), `04-design-notes.md`, ART_BIBLE §7.3–§7.6, ASSET_SLOTS `ui.thumbnail`. Si D2 toca un *shader* o un `*Art`, la huella cambia y B3 re-hornea.

### 8.4 El contrato de los *shaders* (`SceneShaderIds`, B0 → D1)

| Shader | Cola | Entradas |
|---|---|---|
| `RowMate/Scene/Facade` | opaco | color de vértice (tono, `a` material); instancia `_RmInst` (`float4`: tono, semilla, máscara 24 bits, nivel de luz); `_RmFloorHeight` 3, ventanas 1,0 × 1,2; `ShadowCaster` |
| `RowMate/Scene/Glow` | transparente 3010, aditivo | instancia (`float4`: radio, alfa, tipo halo/globo/haz, fase); `_RmBeamPeriod` 12; niebla aplicada |
| `RowMate/Scene/Foam` | transparente 3020 | `water.foam` 256²; `_RmFoamPulse` (6,5 s) |
| `RowMate/Scene/Crowd` | *alpha-test* | atlas 512 × 256; instancia (ropa, pose, período, fase) |
| `RowMate/Scene/MarkerText` | *alpha-test* sobre el tablero | atlas SDF R8; instancia (glifo, avance) |
| `Prop` (C) | la de la 6a | `RM_PROP_CLOTH` (banderas, sombrillas) y el cabeceo de boyas por la semilla de la instancia |
| `WaterDegraded` (C) | la de la 6a | `RM_WATER_SWELL` + `_RmSwell`; `_RmUnderBridge` |
| `Terrain` (C) | la de la 6a | material por color de vértice (adoquín, piedra, arena húmeda, césped con franjas) |
| `Horizon` (C) | 1003 | ventanas del *skyline* en el alfa del color de vértice (emisivas, sin niebla) |
| `RowMateLighting.hlsl` (C) | — | `_RmUnderBridge = (zIni, zFin, rampa, fuerza)`: con fuerza 0 el resultado es idéntico al de la 7 |

Nada usa `_Time`. Topes de ALU de arte §9.2 (`WaterDegraded` ≤ 38 con ola y puente).

---

## 9. Integración del arte y la spec

### 9.1 Pedidos al Architect (00-arte-entornos §11)

| ID | Respuesta |
|---|---|
| DA8-01 | **Adoptado**: `RiverArt`, `CoastArt`, `RegattaArt` (WP-A crea, D dueño) |
| DA8-02 | **Adoptado** con los cortes de la spec (talud desde 9,8, DT8-13) |
| DA8-03 | **Adoptado**: `float4` por instancia en `InstanceBatch`, armado al cambiar de segmento |
| DA8-04 | **Adoptado**: `Facade` + `FacadeWindows` |
| DA8-05 | **Adoptado** salvo la fase de la costa (DT8-09); 28,6° por la cuenta |
| DA8-06 | **Adoptado sin columnas de luz** (DT8-11) |
| DA8-07 | **Adoptado con cambio**: −40 % por fragmento (DT8-07) |
| DA8-08 | **Adoptado con 0,55 m** de alto y ancho ≤ 1,60 (DT8-04); atlas horneado + respaldo de 7 segmentos (DT8-15) |
| DA8-09 | **Adoptado con cambio**: la ola de la spec (DT8-05); las tomas de la 6a y la 7 quedan iguales (umbral 0,5) |
| DA8-10 | **Adoptado** |
| DA8-11 | **Adoptado con 2,5 m** (DT8-10) |
| DA8-12 | **Adoptado** (≈ 34 por tribuna por DT8-03) |
| DA8-13 | **Adoptado** (hora de escena) |
| DA8-14 | **Adoptado con `relZ < 150`** (DT8-02); 456 × 288 |
| DA8-15 | **Adoptado** |
| DA8-16 | **Adoptado**: la precarga al cambiar (§3.2) y el `[PERF]` de la entrada |
| DA8-17 | **Adoptado** con Y Bot ≤ 280 k (lo medido en la 7) |
| DA8-18 | **Adoptado**: cierra el issue #9 |

### 9.2 Arte contra spec (manda la spec)

D8-09 → DT8-02; D8-04 (24 m) → 12 m, DT8-03; D8-05 (0,62 m) → 0,55 m, DT8-04; arte §3.4 (mar de fondo de 16 m) → la ola de la spec, DT8-05; D8-02 en la costa (fase) → sin fase, DT8-09; DA8-07 (−60 %) → −40 %, DT8-07; columnas de luz (arte §2.4) → fuera (spec §12), DT8-11; árboles de vereda → fuera (DV-84), DT8-12; talud 9,5 → 9,8, DT8-13.

### 9.3 Desvíos propuestos (ninguno cambia un dato de la sesión)

- **DT8-10**: AC-SCN-17-04 pide acantilados de "8 m × escala"; en el segmento del faro, el bloque de `z₀ + 25` se dibuja a 2,5 m para que la base del faro cumpla AC-SCN-14-10 (≤ 2,5 m). El generador y sus números no cambian.
- **DT8-18**: AC-SCN-22-05 se mide con el kit listo.

---

## 10. Riesgos

| Riesgo | Mitigación |
|---|---|
| SetPass en el tope (el lago ya da 50 en Medio) | los kits no suman los *shaders* del lago; `SceneBudgetTests` por escenario desde B1; si pasa, `Crowd` y `MarkerText` como variantes de `Prop` |
| El primer armado con las variantes de los 4 escenarios tarda más | por tramos, en reposo; `[PERF]` mide la peor unidad |
| Cambiar de kit deja memoria o avisos de objetos destruidos | `EnvironmentKit.Dispose` en unidades, `EnvironmentSwitchTests` (± 5 %) |
| El lago cambia sin querer (tomas de la 6a y la 7) | umbral `A ≥ 0,5`, `_RmUnderBridge` con fuerza 0 idéntico; CAP-01 a -08 y una corrida de control de `it7` |
| Empates en `z` en la regata y el río contra el oráculo | comparación ordenada por (tipo, `x`, semilla) en cada tramo (regla de la 6a) |
| La ola tiembla a 42 km | fases en `double` (DT8-06), `CoastSwellTests` a 42 195 m |
| Miniaturas viejas tras el pulido de D2 | huella en dotnet (DT8-16); B3 re-hornea al final |
| `ScrollView` horizontal dentro del vertical de Workout se roba la rueda o el arrastre | `WheelEvent` propio con `StopPropagation`; `EnvironmentPickerTests` lo prueba |
| Puente cerca del plano cercano (0,5 m) | el tablero está a 6 m: el plano no lo corta; `BridgePassTests` |
| Doble codificación en `.cs` (la 7) | Write en UTF-8 y revisión de `á`, `ñ`, `—` en cada archivo nuevo antes de entregar |
| Iteración grande | orden estricto de WP; lo menor a Para issues |

---

## 11. Para issues

- Miniaturas a la hora actual (4 por escenario) si el usuario no lo pide en el gate (spec §14).
- Columnas de luz y reflejos de faroles, ventanas y puente en el agua (6b, issue #6).
- Árboles de vereda en el malecón y baranda de hierro (si se acepta romper DV-84 / P-09).
- Racks de botes frente a los clubes; pontones, torre de largada y números de andarivel en la regata; velas en el mar; patos; tráfico sobre el puente.
- Haz del faro visible más allá de los 400 m.
- Espuma de crestas y boyas que siguen la ola (6b).
- Rolido y respiración por escenario desde un perfil de datos editable.
- Público con más variantes; descripción del lugar bajo el nombre en el selector.
- Kits precargados en paralelo (dos en memoria) si `[PERF]` muestra que la escena demorada aparece seguido al cambiar.
- 60 fps en Android de gama media (It. 14).
- Rampa perpendicular al muro o escalonada si la paralela (DT8-19, R5) no convence en las tomas.

---

## 12. Cambios por gate

| Issue | Cambio |
|---|---|
| QA-G8-01 (mayor) | Nueva DT8-19 y §2.4.1 (reglas R1 a R6 de `RiverPlacement`: ancla en la cara a la cámara, `\|x\|` ≥ 10,1, fondo recortado contra la casa siguiente y el puente, puente hasta `\|x\|` 12,5, rampa paralela al muro en 8,5–10,0); §1.1 `PropPlacement`, §2.4 corte del río, §2.6 (ancla, estribos 12,5 en vez de ±24), §7.3 `EnvironmentGeometryTests` y `TerrainEnvironmentTests`, §7.4 AC-SCN-14-10. Texto corregido en [01-po-spec](01-po-spec.md) AC-SCN-16-04 ("fachadas desde 13 m" → eje en 13–17, frente desde 10,1) y en [00-arte-entornos](00-arte-entornos.md) §2.1 y §2.3 (rampa, puente, plantas), con su línea de motivo |
| QA-G8-02 a -15 (menores) | Fuera de esta corrección (no estaban en el pedido); quedan para su dueño en la implementación o como issues según el gate |
| WP-A (implementación) | **R5**: además de farol y puente, bloquea el portón de otro club de la orilla (± 0,5); el primer sentido es el opuesto al portón vecino más cercano; si un farol cae frente al portón, el arranque se corre medio portón (1,5 m); y cada club esquiva la rampa de primer nivel de los clubes más cercanos en z (dos niveles fijos, sin cadenas). Resultado: 2 924 de 2 997 clubes con rampa (97,6 %) en 100 km, sin cruces. **Cartel**: las cifras quedan siempre en 0,55 m y el texto se comprime de costado hasta 1,60 m (DT8-04). **Costa**: la meseta pasa de un bloque al siguiente en rampa a lo largo del solape de 3 m (costura < 1 mm). `PropPlacement.FootY` da el pie de los objetos nuevos. Archivos agrupados: `WaterShaderInputs`, `BridgeShade` y `SwellFilter` en `CoastSwell.cs`; el tinte del agua degradada en `ShoreShading`; las mallas en `Geometry/{River,Coast,Regatta}Meshes.cs`; `MarkerGlyphs` en `DistanceMarkerText.cs` |

## 13. Cambios por implementación de WP-B

Hecho por unity-architect con WP-D1 todavía sin entregar (no existen `Facade`, `Glow`, `Foam`, `Crowd` ni `MarkerText`); se eligió lo que funciona con los *shaders* de la 6a y la 7 y se anotó en una línea. Detalle en [ADR 023](../../architecture/adr/023-kits-de-escenario.md).

| ID | Cambio |
|---|---|
| B8-01 | **Objetos de los kits por segmento**, no instanciados con `float4` (§3.3): `Core/Scene/Geometry/KitSegmentMesh.cs` junta en una malla por segmento y LOD (cercano < 120 m) los objetos del legacy con su colocación (`RiverPlacement` en el río), el muro del río, el relleno (`EnvironmentFill`), las ventanas de la máscara de `FacadeWindows` (las encendidas en la submalla de la noche) y las cifras del cartel en 7 segmentos (el respaldo de DT8-15). `Scene/Environments/KitPropRenderer.cs` la dibuja con `Prop` (y la submalla 1 con el material de ventana de noche). El fundido de aparición es por segmento |
| B8-02 | **Silueta lejana** en la misma codificación que la del lago: `Core/Scene/Geometry/EnvironmentHorizonStrip.cs` (skyline del río, acantilado de la costa, tierra, tribunas y torre de la regata) en la tira lejana de `HorizonRenderer`, con el tono de `EnvironmentHorizonColors`; la tira cercana es solo del lago. Sin las ventanas del skyline de noche (necesitan el `Horizon` de WP-D1) |
| B8-03 | **Cambio de kit**: `SceneWorld.SetEnvironment` (un `SegmentStore` nuevo y `Rebind` de los *streamers*) + `SceneHost.SwitchKit` (vuelve a la fase 2 del armado de la 6a). No hay `EnvironmentKit.cs` aparte: el kit es el `SegmentStore` del escenario y su tira |
| B8-04 | **Miniaturas horneadas por el arnés** (conjunto `thumbs` de `Snapshots/EnvironmentSnapshotCatalog.cs`), no por `Editor/Thumbnails/EnvironmentThumbnailBaker.cs`: el horneado necesita una `AppContext` armada (la de `UiTestHost`). `Editor/Thumbnails/ThumbnailAssets.cs` las importa y las asigna a `UiAssets`; `tools/unity/bake-thumbnails.ps1` corre los dos pasos. El corte `relZ < 150` es por objeto en el lago y por segmento en los kits |
| B8-05 | **Sin** `MarkerGlyphBaker`, `GlowRenderer`, `CrowdRenderer`, `SeagullRenderer` ni `FoamRenderer` (dependen de los *shaders* de WP-D1); las cifras van en 7 segmentos, el público como figuras de dos cajas, sin gaviotas ni espuma |
| B8-06 | La ola de la costa mueve el casco (Core, WP-A) pero **no el color del agua** (`RM_WATER_SWELL` es de WP-D1); tampoco la luz bajo el puente (`_RmUnderBridge`) |
| B8-07 | El selector va **sin** `EnvironmentCardMotion` (WP-D): el cambio de estado es en el acto, como con reducir movimiento; estilos en `Base.uss` (sección It. 8) con los tokens existentes |
| B8-08 | Bootstrapper **v6** (River, Coast y Regatta.asset; miniaturas en `UiAssets` en `scene.boot`); `SceneAssetsBuilder.EnvironmentSlotIds` |
| B8-09 | El terreno lejano levanta la orilla (+0,4 m) y la copa del bosque **solo en el lago** (`TerrainChunkMesh`): el mar de la costa no se levanta |
| B8-10 | **Arnés `it8` completo** (B2): CAP-44 a -46 (escena sola en V390 con las marcas del legacy de `SceneMarks` y el desnivel de la base, `Snapshots/EnvironmentMarks.cs`; `ShoreStreamer.TryAnchor` usa el pie del objeto en los kits), VID-06 (elegir Costa → `UiTestHost.Reopen` con `PreferencesFile` = el `JsonFilePreferences` real en la carpeta temporal del anfitrión, DT8-17 → Entrenamiento libre; `FrameCheck`: Costa elegida y la sesión en la costa después de reabrir) y DEMO-01 a -05 (una secuencia W1280 con la hora por `Hora escena` y el paso 5 reabriendo). **Sondas del issue #9**: `SnapshotScenario.SceneProbes` → `probes` del `manifest`; en las tomas de las 22 h y CAP-41, la pasada sin post, la caja del objeto encendido más cercano de cada tipo proyectada con la cámara real y el píxel más parecido a `#FFE082` (± 3) adentro; el frente, como dato. El farol y el faro se miden contra el mismo color (se encienden con el material de ventana hasta el `Glow` de WP-D1, B8-05) |
| B8-11 | **`SceneBudgetTests` (CPU de la escena)** (la falla de la ronda 2): el desglose nuevo (`SceneHost.LastStreamMs`, `LastTickMs`, en el registro `[SBT]`) da sesión 0,06-0,10 ms, orilla 0 (salvo 1-3 cuadros de 360) y el resto (aplicar el cuadro y pedir los dibujos instanciados) 1,2-1,6 ms en Alto, en proporción a las instancias de relleno (Alto ≈ 3 700, Medio ≈ 1 700, Bajo ≈ 900). Con la PC sola: Alto 1,24 y 1,30 ms, Medio 0,88-0,94, Bajo 0,66-0,84 (la 6a medía 1,15 / 0,79-0,84 / 0,62-0,71; la diferencia es el remero de la 7). **Causa**: es reloj de pared y en todas las corridas que fallaron había otras instancias de Unity y del *player* en la misma PC (otras sesiones con tests, capturas, builds y `[PERF]`, de 1 a 11 a la vez): Alto 1,50-2,08, Medio hasta 1,53, Bajo hasta 1,16, y `HudBudgetTests.SeriesWorstCaseWork` también pasa su tope. No es una regresión de la 8: **el tope no cambia** (1,5 ms en los tres niveles). Con la PC compartida no se pudo dejar en verde: hay que correr la suite con la PC sola (o medir el tiempo de CPU del hilo en vez del reloj de pared: para issues) |
| B8-12 | **`NoFbxOutsideThirdParty`** (la otra falla de la ronda 2): corrida en la copia principal, el escaneo entraba en el worktree de git anidado y veía el `YBot.fbx` del worktree. `SourceScan.RepositoryFiles` ya no entra en carpetas con su propio `.git` (otra copia de trabajo); `RepositoryScanSkipsNestedWorkingCopies` lo reproduce con un árbol temporal |
| B8-13 | **`[PERF]` por escenario**: `-rowmate-perf-env <id>` y `-rowmate-perf-hour <h>` con `-rowmate-perf-scene` (solo en desarrollo; en la lista de `BuildOutputInspector`). Con un escenario, el lago listo en reposo, la elección y la entrada 1 s después (DA8-16) |
| B8-14 | **`[PERF]` por escenario** (`Builds/Windows/simulator`, W1280, Alto, Y Bot, `-rowmate-perf-scene 40`, libre con `Fuerte`, la entrada 1 s después de elegir; con otras sesiones usando la PC): p99 del hilo principal por ventana de 10 s 1,9-6,6 ms (lago 3,5-6,6; río 2,0-4,5; costa 1,9-6,6; regata 2,0-3,1), GPU máx 6-22 ms, memoria 130-135 MB, GC 0 en régimen (1 en la entrada del río de las 22 h), 0 *shaders* después de Ready; en cada corrida el kit y la sesión son los del escenario elegido. Máximos: el cuadro del toque 23-30 ms (40 ms en la regata a las 22 h) y cuadros sueltos de 33-48 ms a mitad de la corrida (lago 22 h, costa 12 y 22 h) coincidentes con los *players* y Editores de otras sesiones; a repetir con la PC sola. **Reinicio en el build**: una corrida con `-rowmate-perf-env regatta` deja `{"scene.environment":"regatta"}` en `prefs.json` y la siguiente, sin el argumento, rema con el kit y la sesión de la regata (AC-SCN-21-04) |

## 14. Estado de WP-B

Cerrado por unity-architect (B0, B1, B2 y B3 de §8.2 con los cambios de §13). Resultados en `<scratch>/it8/wp-b-fin/`.

- **Hecho**: los 4 escenarios (definiciones, `SceneAssetsBuilder` v6, ADR 023), kits por segmento (`KitSegmentMesh` + `KitPropRenderer`), cambio de kit en reposo y modo miniatura en `SceneHost`, miniaturas horneadas 456 × 288 en `UiAssets`, selector en la vista de reposo, arnés `it8` (48 tomas, `it8-A` = `it8-B` byte a byte, sondas nocturnas del issue #9), `ARCHITECTURE` §14.5.
- **Falla `NoFbxOutsideThirdParty`**: falso positivo; el recorrido entraba en `.claude/worktrees/` (un worktree local de otro agente con su `YBot.fbx`). Ya lo saltea `SourceScan` (cambio hecho en paralelo; no se tocó la regla).
- **Falla `SceneBudgetTests` Alto (CPU p95 1,77 > 1,5 ms)**: es el lago, no un kit (los kits miden 0,1–0,3 ms). El 78 % de la CPU de la escena del lago era `FillStreamer.Apply` (0,87 de 1,12 ms en Alto), que rehacía por cuadro el TRS de cada mata. Ahora la base de cada objeto (giro × escala) se calcula una vez (`FillBasis`, `FillBasisCache`, idéntica bit a bit al TRS: `FillBasisTests`) y los bloques de 32 matas fuera de la franja (por `z` y por la cota de la distancia en el plano) se saltean sin cambiar qué se dibuja. Lago Alto 1,37 → 0,87 ms, Medio 0,86 → 0,57, Bajo 0,60 → 0,39 (corrida aislada); mismas instancias (3 704 / 1 737 / 894) y CAP-01 a -09 iguales byte a byte a las versionadas. El tope no se tocó. Con otras dos corridas de Unity de otros agentes en paralelo la misma prueba dio 1,06–1,63 ms: la medición en el Editor es sensible a la carga de la PC.
- **Pruebas nuevas**: `EnvironmentBudgetTests` (río, costa y regata × Alto/Medio/Bajo × 12 y 22 h: dibujos, SetPass, triángulos, CPU, y 0 B con control positivo); `FillBasisTests`. Río 73 dibujos / 23 SetPass / 104 k triángulos en Medio, costa 64 / 23 / 93 k, regata 62 / 23 / 125 k.
- **`[PERF]`** (`Builds/Windows/simulator`, `-rowmate-perf-scene 40` con `prefs.json` de una carpeta temporal por escenario): p99 3,2 ms lago, 2,6 río, 2,2 costa, 2,2 regata; ningún cuadro > 33 ms (máx 28,1 en el cuadro del toque). `-rowmate-perf-hours` (17 → 23 h): lago p99 4,6, máx 30,2; río p99 2,1. Al reiniciar con la misma carpeta, el kit y la sesión son los guardados y `prefs.json` queda igual. Nueva línea `[PERF] escenario (n.º k): kit …, sesión …, cambios de kit …`.
- **Desvíos**: además de §13 (B8-01 a B8-09): sin `-rowmate-perf-env`/`-rowmate-perf-hour` (el escenario va por `prefs.json`; la hora, con `-rowmate-perf-hours`); sin `EnvironmentParityTests`, `BridgePassTests`, `EnvironmentSwitchTests` (10 cambios, ± 5 % de memoria), `EnvironmentSlotTests`, `EnvironmentRestartTests`, `EnvironmentMaterialsTests` ni `ThumbnailAssetsTests` como archivos propios (lo cubren en parte `EnvironmentSceneTests`, `EnvironmentPickerTests` y `EnvironmentAssetsTests`); la elección con el dedo en el build no se probó a mano (la cubre `EnvironmentPickerTests.TappingChoosesAndSaves`).

## 15. Reconciliación del worktree

Dos corridas trabajaron en paralelo: la segunda en un worktree local de git (`.claude/worktrees/it08-entornos`, rama `worktree-it08-entornos`, el mismo `HEAD` que `main`), copiado del árbol principal a las 13:09 con sus fechas. Las dos hicieron WP-D (shaders, `Glow`, ola, puente, selector) y el worktree, además, el cierre de B2 (B8-10 a B8-14). Se fusionó todo en el árbol principal; **el worktree no se borró** (lo decide el usuario).

**Método.** Para cada archivo distinto, `git merge-file` de 3 vías con base `HEAD` (con los fines de línea normalizados). Si un lado no había tocado el archivo desde la copia de las 13:09, se tomó el otro entero. Si los dos hicieron lo mismo, quedó lo del árbol principal, que es lo que verificó QA en la ronda 1, y se sumó lo que solo hizo el worktree. 168 archivos ya eran iguales.

| Archivos | Resultado |
|---|---|
| `BuildOutputInspector`, `SceneHost`, `ShoreStreamer`, `SnapshotRunner`, `SnapshotScenario`, `EnvironmentSnapshotCatalog`, `KitSegmentMesh`, `ItSixAToolingTests`, `SceneBudgetTests`, `tools/unity/README.md` | Del worktree: el principal no los había tocado desde la copia (B8-10, B8-11 y B8-13: arnés `it8` completo con las sondas del issue #9, desglose `LastStreamMs`/`LastTickMs`, `-rowmate-perf-env`/`-rowmate-perf-hour`, marcas de vida en el uv) |
| `Foam.shader`, `Foam.mat`, `FoamRenderer`, `EnvironmentMarks` (con sus `.meta`) | Nuevos del worktree, copiados. Sus GUID no chocan con ninguno |
| `PureCodeRulesTests` | Fusión limpia: `SourceScan` saltea `.claude/worktrees` (principal) y cualquier carpeta con su propio `.git` (worktree, con `RepositoryScanSkipsNestedWorkingCopies`) |
| `Prop.shader` | Fusión: la vida en el vértice del worktree (`PropMotion` en las tres pasadas) y el puente por fragmento del principal. `RmVertexLightAt` → `RmVertexLight` |
| `RowMateLighting.hlsl`, `RowMateScene.hlsl`, `Hull`, `Oar`, `Rower`, `Terrain` | **Conflicto (los dos hicieron el puente y la ola)**: queda el principal (`RmUnderBridgeGamma2` en un salto por el uniforme, los suelos de los kits). Se descarta `RmUnderBridgeG`/`RmVertexLightAt` del worktree |
| `WaterDegraded.shader` | **Conflicto**: queda el principal (ola de 120 a 300 m en el fragmento, 27 ALU). Del worktree se suma el mar abierto de la costa (`_RmWaterExtras.w`: sin franja somera del lado del mar), que pone `WaterSurface.Apply` en `HorizonRenderer` |
| `Buoy.shader` | El principal no lo tenía bajo el puente y el worktree sí (con `RmVertexLightAt`): `RmUnderBridgeGamma2` por vértice, como el casco y los remos |
| `Glow.shader`, `Glow.mat`, `GlowRenderer` | **Conflicto (los dos lo hicieron)**: queda el principal (halo de la torre, 96 instancias); se suma `GlowRenderer.Beam` del worktree (sin el haz en Bajo, lo ponen `SceneHost` y `SceneWorldBuilder`) |
| `ShaderArt`, `CoastArt`, `SceneShaderIds` | **Conflicto**: el principal más `Foam` (spec 3020/30 ALU, `kRmFoamNoiseM`, `kRmFoamTaperM`, valores por defecto, ids y constantes de la tira) y las constantes de la vida de `Prop` (`kRmFlag*`, `kRmBuoy*`). Sin `EnvironmentConstants()`/`EnvironmentMaterialDefaults()` del worktree: van en `Constants()` y `MaterialDefaults()`, como `Glow` |
| `WaterArt`, `RiverArt`, `RegattaArt`, `SceneLighting`, `SegmentStore`, `FillStreamer`, `Base.uss` | El principal (lo del worktree era la copia o lo mismo con otro texto) |
| `SceneDirector`, `SceneWorldBuilder`, `SceneWorldDefinition`, `SceneMaterialsBuilder`, `SceneWorld.asset`, `SceneShaderVariants` | **Conflicto**: el principal más `Foam` (material, renderer, `Rebind` al cambiar de kit, ruido de la escena, entrada en la colección de variantes) |
| `AppRoot` | **Conflicto**: la línea `[PERF] escenario` del principal y `-rowmate-perf-env`/`-rowmate-perf-hour` del worktree; la línea `[PERF] escena` queda la de antes |
| `EnvironmentCardMotion`, `EnvironmentPicker` | **Conflicto (los dos hicieron el selector animado)**: queda el principal. `EnvironmentCardLook` (y sus pruebas) del worktree **no se copió**: lo usaba solo el `EnvironmentCardMotion` del worktree → para issues |
| `SceneShaderContractTests`, `RowerShaderContractTests` | El principal (agua en 27 ALU) |
| `SceneAssetsTests`, `EnvironmentShaderContractTests` | El principal más `Foam`, la marca de la vida (`FlagsUmbrellasAndBuoysAreMarkedToMove`) y `Buoy` bajo el puente |
| `ProjectSettings.asset` | El principal: el del worktree tenía `ROWMATE_SIMULATOR` olvidado en las tres plataformas. `RowMateBootstrap.json`: el principal |
| `ART_BIBLE`, `ASSET_SLOTS` | El principal (el mismo dato con otras palabras). Este documento: el principal más las filas B8-10 a B8-14 del worktree (B8-13 reemplaza el "sin `-rowmate-perf-env`" de §14). [04-design-notes](04-design-notes.md): el principal más §10 |
| Miniaturas (`Art/Textures/Thumbnails`) | Vueltas a hornear con `tools/unity/bake-thumbnails.ps1` (la huella de los *shaders* cambió con la fusión) |
| Presupuesto de `Foam` | `SceneShaderTests.TheEnvironmentFragmentsFitTheirBudget` lo mide en **62 ALU** (D3D11, Medio); el worktree decía 30 sin esa prueba. Tope en 64 con su motivo en `ShaderArt`: es solo la costa y una tira de 1,6 m. Bajarlo es para issues |
| Tomas (`snapshots/`) | Vueltas a sacar con el código fusionado (`capture-snapshots.ps1 -Set it8`): 57 tomas (CAP-01 a -46, DEMO-01 a -05, VID-01 a -06), 0 con problemas. 2 sondas de aspecto fallan: CAP-20 y CAP-24, el faro de las 22 h (la ventana sale `#FFF390` / `#EAD482` contra `#FFE082`: el halo `#FFF59D` del `Glow` cae sobre la linterna que mide la sonda) → para issues |

**Verificación después de fusionar.** dotnet: compila con 0 avisos y 0 errores; herramientas 192/192; Core 3 343, con 3 304 que pasan, 0 que fallan y 39 omitidos. Unity EditMode completo: 3 447, con 3 401 que pasan, 1 que falla (el presupuesto de `Foam`, arreglado arriba) y 45 ignorados. Después del arreglo, `SceneShaderTests`, `SceneShaderContractTests`, `EnvironmentShaderContractTests` y `SceneAssetsTests` pasan 35/35. La guardia de `ProjectSettings` quedó sin cambios en todas las corridas, y el legacy, con `git status` vacío.

## 16. Cambios por verificación (ronda 1 → ronda 2)

Correcciones de los 4 mayores de [05-qa-verification](05-qa-verification.md) §6 y de los menores baratos. Primero la prueba que reproduce, después el arreglo. Resultados en `<scratch>/it8c/`.

| Issue | Prueba que lo reproduce | Arreglo |
|---|---|---|
| **QA-V8-01** · gaviotas | `SeagullFlockTests` (dotnet: cada gaviota a 90 m en el acimut y la elevación de `SeagullFlight`, 5 px de TV de envergadura × (5 + i)/5, la punta ± 0,25 m, invisibles con alfa 0, 0 B por `Pose`) | `Core/Scene/Geometry/SeagullFlock` (las 5 copias de `SeagullMesh` en una malla, las dos caras, `#2B2B2B`; por cuadro, la posición desde la cámara y el aleteo en el vértice) y `Scene/Environments/SeagullRenderer` (un dibujo instanciado con `Prop`, la opacidad del legacy `0,5 + 0,4·ambiente` en `_RmFade`, sin sombras; solo en la costa). Lo arma `SceneWorldBuilder` y lo aplica y dibuja `SceneDirector` |
| QA-V8-01 · público | `EnvironmentShaderContractTests.TheCrowdIsMarkedToMove` | `KitSegmentMesh.MotionCrowd = 7`: cada persona lleva −(7 + w) en uv.x (w = la altura sobre el asiento) y en uv.y su período (cs) + su fase (vueltas) de `EnvironmentFill`. `Prop.shader` lo mece `0,04·w·sin a` y lo levanta `0,06·w·máx(sin 2a; 0)` con `a = 2π(t/período + fase)`, con el tiempo de escena (`RegattaArt.CrowdSway`, `CrowdBob`, en `ShaderArt.Constants`) |
| QA-V8-01 · banderas lejanas | `KitSegmentMeshTests.TheRegattaStripCarriesTheFarFlags` (río y costa sin; regata con ≥ 4 grupos de 4, mástil por bandera, punta en p 0,55, acimut del legacy) | `EnvironmentHorizonStrip.AddRegattaFlags`: en la tira de la regata, con z = código (1 a 4 bandera, 5 mástil), el paño de 6 px y el mástil de 1 px de `regatta_environment.dart:56-68`. `Horizon.shader` toma el tono por código (`_RmFlagTone0..3`, `_RmFlagMast`, que `HorizonRenderer` pone por hora con `EnvironmentHorizonColors.RegattaFlag`/`RegattaTower`) y mueve la punta `sin(3t + i)·2 px` (`kRmFarFlagWave`, `kRmFarFlagRate`). Un salto por z > 0: el lago no cambia |
| QA-V8-01 · banderas, boyas, sombrillas y espuma | ya estaban de la fusión del worktree (§15: `PropMotion`, `FoamRenderer`); `FlagsUmbrellasAndBuoysAreMarkedToMove` | — |
| **QA-V8-02** · 25 casos legacy | `EnvironmentSceneTests`: `Paints()` (LT-111 a -122), `BehindAndAbove()` (LT-123) y `Animates()` (LT-127 a -138), con el nombre del legacy en `SetName`, su `Category("LT-…")` y el comentario `// legacy: …` | Río, costa y regata × V390/H844 × 12/23 h: a 0 y 1 234 m con tiempo 3,2 (dibuja, cielo no negro, `LogAssert.NoUnexpectedReceived`); LT-123: el puente del río a relZ 0,3 y 1,0, los 15 tipos en x = 12 a 20 m (variante 3, la malla del segmento de cada kit con todos, dibujada con `Prop`) y el cartel de 750 m a 30 m, a las 22 h; LT-127 a -138: SPM 24, split 110, 300 m, 6 cuadros de 100 ms, un dibujo por cuadro y desmontar (`DisposeHost`) sin avisos |
| **QA-V8-03** · paridad en pantalla | `EnvironmentParityTests.EachObjectSitsOnTheLegacyX` (río, costa, regata × los 7 tamaños × 0 m, el lugar del escenario y 1 234 m) | El ancla de `ShoreStreamer.TryAnchor` (el pie del objeto en el kit) proyectada por la cámara real contra `SceneCamera` del legacy: ± 2 dp, y la base ≤ 2,5 m. Las marcas en CAP-44 a -46 (`EnvironmentMarks`) ya estaban de §15 |
| **QA-V8-04** · tomas y sondas | el catálogo `it8` (§15: CAP-44 a -46, VID-06, DEMO-01 a -05 y `probes`) | Las sondas del farol y del faro (CAP-20, -24) fallaban porque el halo aditivo del `Glow` caía sobre la linterna: en la pasada sin post `GlowRenderer.SuppressDrawForTest` lo saca y la sonda mide el color emisivo. Tomas A y B vueltas a sacar con el código final |
| QA-V8-08 (parte) | `EnvironmentSceneTests.TenSwitchesKeepTheMemoryFlat` (AC-SCN-01-18) | 10 cambios en reposo: las mallas vivas y la memoria administrada ± 5 % contra la primera vuelta; el mundo común es el mismo |
| QA-V8-09 | `git check-ignore .claude/worktrees/x` | `/.claude/worktrees/` en `.gitignore` |
| QA-V8-10 | `VID-05.values` (d constante) | `SceneHost.StopBoatForTest` (`SceneSession`: velocidad de escena en 0) en el armado de VID-05 |
| QA-V8-11 | — | Ya estaba (B8-13, §15) |

**Verificación después de corregir** (PC sin otras sesiones de Unity):

| Qué | Resultado |
|---|---|
| dotnet (`run-tests.ps1`) | Build **0 avisos, 0 errores**. Herramientas 192 / 192. Core **3 350: 3 311 pasan, 0 fallan, 39 omitidos** |
| Unity EditMode | 3 453: 3 407 pasan, 45 ignorados, 1 falla: `SimulatorModeTests.SimulatorDoesNotCompileInANonDevelopmentPlayer` ("AssemblyBuilder no terminó", porque se editaban fuentes mientras corría). Repetida sola: 13 / 13 |
| Unity EditMode + simulador | **3 645: 3 600 pasan, 0 fallan, 45 ignorados** |
| Unity PlayMode | 551: 550 pasan; la falla era la auditoría de `TenSwitchesKeepTheMemoryFlat` (el anillo de la tarjeta a mitad de su animación): se espera `Settle()` y repetida pasa. `EnvironmentParityTests`: río 1 855, costa 385 y regata 3 584 objetos medidos, máx \|Δx\| **0 dp**, base máx 1,2 / 2,5 / 1,0 m. Memoria en 10 cambios: mallas 116 → 116, administrada −0,01 % |
| Unity PlayMode + simulador | **624: 624 pasan** |
| Tomas `it8` | **57 tomas, 0 con problemas**; A (en el repo) = B (`<scratch>`) **byte a byte en las 57**. VID-05: `d 500` fijo con arfada y cabeceo ≠ 0. Gaviotas en CAP-17 a -24 y VID-03; banderas lejanas en CAP-25 a -32. Sondas: todas las de TV pasan (ventana de la cabaña, edificios, farol, faro, *skyline* del río); 2 de aspecto fallan en TH (CAP-16 farol `#F1D37B`, CAP-24 faro `#D2B86D`: la linterna queda cortada bajo la barra superior, QA-V8-14) |
| Miniaturas | Vueltas a hornear (`bake-thumbnails.ps1`: la huella cambió con las constantes nuevas) |
| Build | `tools/build/build.ps1 -Target Windows -Simulator` → **Éxito**, `Builds/Windows/simulator/RowMate.exe` con `THIRD_PARTY_NOTICES.md` |
| Guardia de `ProjectSettings` | sin cambios en todas las corridas; legacy con `git status` vacío |

**Para issues** (de esta ronda): el encuadre TH de la costa y del río de noche corta la linterna del faro y del farol (QA-V8-14: las dos sondas de aspecto); el público sigue en cajas sin el atlas de poses (DA8-12); la sombra del paso de las gaviotas; `BridgePassTests`, `EnvironmentSlotTests`, `EnvironmentRestartTests` y el selector con rueda, flechas, letra 1,3× y 0 B al desplazar (el resto de QA-V8-08).
