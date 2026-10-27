# ADR 009: BLE con IBleAdapter mínimo en Core y puentes nativos propios

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Core.Ble, Platform, plugins nativos (It. 10a, 10b y 11; interfaces desde la It. 2; *spike* S-1)

## Contexto

El legacy usa `flutter_blue_plus` (con implementación WinRT en Windows) para dos conexiones GATT simultáneas (remo FTMS y pulsómetro), con lógica rica de reintentos, generaciones, watchdogs y handshake FTMS. El usuario exige plugins nativos propios (Kotlin, Swift/CoreBluetooth, WinRT), sin plugins pagos, y siempre una implementación simulada.

## Decisión

- Core define `IBleAdapter` / `IBlePeripheral`: estado del adaptador, escaneo (uno a la vez, reemplaza el anterior), conexión por id, descubrimiento, notify y write. Sin lógica de producto.
- La lógica del legacy se porta **en Core**: `RowerService` (de `ble_service.dart`), `HeartRateService` (de `heart_rate_service.dart`), `ReconnectLoop`, `StaleWatchdog`, `RowerWatchdog`, `ScanFilter`. Timeouts y cadencias con `IScheduler`. Tests con `FakeBleAdapter`.
- Puentes nativos con la misma API C (`rm_ble_*`). **Modelo *pull***: el plugin guarda cada evento (estado, resultado de búsqueda, conexión, fin de pedido, notificación) con la marca del reloj monótono nativo en un *buffer* circular con tope, y C# lo drena al inicio de cada frame (`rm_ble_drain`, sin asignaciones). Los eventos sobreviven con Unity pausado y no hay llamadas de hilos nativos a código administrado.
- **Keepalive nativo** (`rm_ble_keepalive_start/stop`) que Core activa solo con la app en segundo plano (ADR 017).
- **Contador de paladas**: `RowerService` emite `StrokeCountIncrement` cuando sube el `strokeCount` FTMS (D-ANI-06, apagado hasta verificar en la It. 10a).
- Por plataforma:
  - Android: fuentes Kotlin en `Assets/Plugins/Android` (compiladas por el Gradle de Unity), cola de operaciones GATT por dispositivo, MTU 512 para el remo, permisos de Android 12+ y ≤ 11.
  - iOS: Swift en `Assets/Plugins/iOS` con `@_cdecl`, `bluetooth-central` en segundo plano.
  - macOS: las mismas fuentes Swift como `.bundle` universal (`native/macos`).
  - Windows: DLL C++/WinRT (`native/windows`), escaneo sin filtro (lo filtra Core, como el legacy).
- `BleUuid` normaliza las formas cortas y largas de los UUID (CoreBluetooth reporta "180D").

## Alternativas descartadas

- **Lógica de reintentos en cada plugin**: triplicaría el código más delicado y no se podría testear en `dotnet`.
- **Plugins BLE de terceros**: los gratuitos no cubren las 4 plataformas con calidad suficiente y el usuario pidió propios.
- **WinRT desde C#**: IL2CPP en Windows Standalone no proyecta WinRT; hace falta la DLL nativa.

## Consecuencias

- Los plugins son chicos y "tontos"; el riesgo queda en Core, donde hay tests.
- macOS y Windows necesitan binarios precompilados (Mac y VS C++ respectivamente).
- Con Unity pausado en segundo plano, los eventos se acumulan en el *buffer* nativo (6 h a 2 Hz por dispositivo) y al volver se reproducen con su hora (alcance, ADR 017); el keepalive lo mantiene el plugin.
- El *spike* S-1 prueba la DLL de Windows sin Unity, con una consola `net8.0` y P/Invoke a la misma API (QA-P-24).

## Cambios por QA (plan)

| ID | Qué cambió |
|---|---|
| QA-P-06 | Modelo *pull* con marca de tiempo, keepalive nativo en segundo plano |
| QA-P-24 | S-1 sin Unity |
