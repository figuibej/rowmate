# ADR 025: Identidad propia, ventana de escritorio y CI de Unity con secretos opcionales

- **Estado:** Aceptado (2026-11-09), al implementar WP-B de la It. 14 (DT14-01 a DT14-16, DT14-31 y §8 de [02-tech-design](../../iterations/14-pulido-plataformas/02-tech-design.md))
- **Ámbito:** Core (`Design/AppIcon*`, `PngEncoder`, `DeflateEncoder`, `Common/WindowGeometry`, `WindowModeState`, `VersionInfoBlock`), Editor (`Bootstrap/IdentityStep`, `Build/ApplePostprocess`, `WindowsVersionInfoPostprocess`, `BuildCli`, `AndroidSigning`), Platform (`Display/WindowsWindowFrame`, `WindowModeController`), Presentation (`Shell/BootSplashView`), `tools/icon`, `tools/build/build.ps1`, `.github/workflows/ci-unity.yml`
- **Relacionados:** [ADR 014](014-bootstrap-sin-editor.md) (bootstrapper), [ADR 013](013-tests-dotnet-y-unity.md) (tests en dotnet y en Unity)

## Contexto

La app tenía el ícono y el nombre de versión del motor, arrancaba sin capa propia, la ventana de Windows no tenía mínimo ni pantalla completa a pedido y el CI de Unity fallaba sin licencia. No hay módulos de Android ni de Apple instalados, ni Mac ni teléfono. Todo tiene que ser gratis, propio y sin credenciales versionadas.

## Decisión

1. **El ícono sale del código** (Core, dotnet): geometría como datos, rasterizador propio y PNG con deflate propio (los mismos bytes en la PC, el CI y Unity). `tools/icon` escribe `Art/Identity`; un test compara los bytes y fija el SHA-256 de 16 y 1024.
2. **`ProjectSettings` solo por el bootstrapper (v7, paso `identity`)**: `SetIcons` para Standalone; Android (adaptativo, redondo, legacy y la imagen de arranque) y la ruta del storyboard de iOS por `SerializedObject`, porque falta el módulo; `ProjectSettingsIt14Tests` (dotnet) fija cada valor de §2.4. Los `m_Kind` de Android quedan [Device-Android] hasta tener el módulo.
3. **La ventana de Windows desde C#**: subclase del `WndProc` (`SetWindowLongPtrW` + `CallWindowProcW`, delegado estático) para el mínimo de 640 × 360 lógicos (`WM_GETMINMAXINFO`, original primero) y sin pitido en `Alt+Enter`; se restaura en `Application.quitting` y `AppRoot.OnDestroy`. `F11` / `Alt+Enter` los maneja `WindowModeController` (`FullScreenWindow` en el monitor de la ventana y vuelta al rectángulo anterior), con `allowFullscreenSwitch` en 0. La ventana inicial (1280 × 720 lógicos, centrada) se pisa en cada arranque sin leer el registro, salvo que la línea de comandos traiga `-screen-*` o `-popupwindow`.
4. **Arranque oscuro sin espera**: `BootSplashView` es la última hija del shell desde el primer cuadro y se funde en 200 ms cuando el shell está listo (`ShellReadySignal`). `build.ps1 -BootSmoke` mide la diferencia en el mismo build de desarrollo.
5. **Versión del `.exe`**: `WindowsVersionInfoPostprocess` reescribe `VS_VERSIONINFO` con `UpdateResourceW` (armado por `VersionInfoBlock` con los valores de `PlayerSettings`); `build.ps1` lo exige (código 9). En el CI de Linux queda un aviso (escritor PE puro: issue).
6. **CI con compuerta**: el job `gate` lee los secretos solo en `env` y sus salidas condicionan los jobs de Unity; sin licencia, aviso y verde. Builds normales de Windows y Android con `BuildCli`; Android firmado solo con los 4 secretos opcionales (en memoria) y si no `-debugsigned`; Apple solo a mano, sin firmar. `MiniYaml` + `WorkflowYamlTests` validan todos los workflows en la PC y en `ci-core`.

## Consecuencias

- Cambiar el ícono es cambiar la geometría, correr `tools/icon` y actualizar los dos hashes en el mismo cambio.
- Un `-screen-*` en la línea de comandos (humos, QA) deja la ventana como la pide Unity.
- Si Narrador o el mínimo de ventana fallan en otro equipo, el plan B es una DLL nativa chica con el mismo contrato (`IWndProcApi`).
- Android y Apple (íconos, arranque, orientaciones, firma) quedan NO VERIFICABLES hasta tener los módulos, el teléfono y la Mac.
