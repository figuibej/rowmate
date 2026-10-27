# ADR 014: Bootstrap del proyecto Unity sin Editor

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** `Packages/`, `ProjectSettings/`, `RowMate.Editor`

## Contexto

Unity no está instalado. Hay que dejar un proyecto que abra limpio la primera vez, sin escribir a mano archivos YAML frágiles (`ProjectSettings.asset`, escenas, assets de URP) ni GUIDs.

## Decisión

- Se escribe a mano solo texto estable: `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt` (solo `m_EditorVersion: 6000.3.27f1`), asmdefs (referencias **por nombre**), código, UXML/USS (referencias por ruta relativa), shaders, JSON, `link.xml`, `csc.rsp` y fuentes nativas.
- `RowMate.EditorTools.ProjectBootstrapper` (assembly `RowMate.Editor`, namespace `RowMate.EditorTools` por QA-V-04; `[InitializeOnLoad]`, idempotente, con estado en `ProjectSettings/RowMateBootstrap.json`), en **dos etapas** (QA-P-25):
  - **Mínimo (It. 1, NO VERIFICABLE sin Editor)**: ajustes de Player (IL2CPP, .NET Standard 2.1, Linear, Android minSdk 26 ARM64 (Q-90), iOS 15, Input System, `runInBackground` en Standalone, *Frame Timing Stats*) con ids provisionales **`com.iguisoft.rowmate` en Android y `com.iguisoft.rowmate.unity` en iOS, macOS y Standalone** (R-14: no pisar la app legacy en Apple). Windows no tiene identificador propio en Unity (D-02): su identidad es `RowMate` de `iguisoft`, y `com.iguisoft.rowmate` queda reservado para el MSIX de la It. 14. **Un** asset de URP provisorio + renderer, asignado a Graphics y a los niveles existentes, sin niveles de calidad definitivos (QA-P-10).
  - **Completo (It. 2)**: `PanelSettings` a 160 DPI, `Boot.unity` registrada en Build Settings y *smoke test* IL2CPP. Los 3 niveles (`RowMate_URP_{High,Medium,Low}` + `QualityTierProfile`, ADR 018) llegan en la It. 6a.
  - **Un intento automático por sesión del Editor** (QA-V-03): `[InitializeOnLoad]` corre en cada recarga de dominio, así que el intento se marca en `SessionState` (sobrevive a las recargas y se borra al cerrar Unity). Si un paso falla, se reintenta en el **próximo open**, no en cada recompilación.
  - Menú `RowMate/Bootstrap/Reaplicar` (fuerza una corrida en cualquier momento).
- Lo verificable sin Editor en la It. 1 es el *lint* de asmdefs, manifest y `ProjectVersion.txt` (ADR 013) y la revisión estática de QA.
- Si una API de URP falla, deja instrucciones manuales de respaldo en la consola.
- Los `.meta` se generan en ese primer open y se versionan desde entonces.

## Alternativas descartadas

- **Escribir `ProjectSettings/*.asset` y escenas a mano**: formato interno, fácil de romper y difícil de revisar.
- **Copiar una plantilla de Unity Hub**: no disponible sin el Hub; trae contenido que no se usa.

## Consecuencias

- El primer open tarda más y puede pedir reiniciar el Editor (cambio del manejador de entrada).
- QA verifica el bootstrap en cuanto el usuario instale el Editor (It. 1 opcional, It. 2 obligatorio).

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-25 | Bootstrapper mínimo en la It. 1 y completo en la It. 2; *lint* como verificación estática |
| QA-P-10 | Sin niveles de calidad definitivos en la It. 1; los 3 niveles en la It. 6a |
| R-14 (pedido del PO) | Ids provisionales de Apple distintos del legacy (`com.iguisoft.rowmate.unity`) |

## Cambios de la It. 2 (deuda de la It. 1)

| ID | Qué cambió |
|---|---|
| QA-V-03 | Un solo intento automático por sesión del Editor (`SessionState`); un paso fallido se reintenta en el próximo open. Test estático `BootstrapperStaticTests.RetriesAFailedStepOnlyOnTheNextOpen` |
| QA-V-04 | Namespace `RowMate.EditorTools` (el assembly sigue siendo `RowMate.Editor`): `RowMate.Editor` tapaba a `UnityEditor.Editor` (CS0118). Tests `AsmdefLintTests.NoNamespaceShadowsUnityNames` y `EditorAssemblyUsesTheEditorToolsNamespace` |
| D-02 (alineación) | El texto de la decisión ya no dice `com.iguisoft.rowmate` "en Android y Windows": Windows no tiene id en el motor (AC-APP-16-02) |

## Cambios de la It. 2 (primer open real)

| ID | Qué cambió |
|---|---|
| R-T2 | Con `-batchmode -quit` el `EditorApplication.delayCall` nunca corre: el primer open terminó con código 0 y sin `RowMateBootstrap.json`. `BootstrapPostprocessor` (`AssetPostprocessor.OnPostprocessAllAssets` con `didDomainReload`, solo en batchmode) llama a `ProjectBootstrapper.RunInBatchMode`, que usa la misma marca de sesión y corre sin mirar `IsBusy` (dentro del postprocesado el Editor figura `isUpdating`). En el Editor con ventana sigue el `delayCall` |
| R-T2 | Versión 2: `UrpProvisioner` asigna al renderer el `PostProcessData` por defecto de URP (`Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset`), como el menú de URP; a un renderer de la versión 1 se le agrega sin recrearlo. Los 9 pasos aplicados: `ProjectSettings/RowMateBootstrap.json` (versión 2), verificado por `BootstrapperStaticTests.RanOnTheFirstOpenAndLeftItsState` |
| QA-V-03 | Verificado en batchmode: con dos recargas de dominio en la misma sesión, una sola línea `[BOOTSTRAP]` |
