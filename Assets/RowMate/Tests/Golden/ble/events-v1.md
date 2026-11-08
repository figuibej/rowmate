# Formato de eventos BLE v1 (`events-v1.bin`)

Fijación byte por byte del búfer de eventos que escriben el plugin de Windows (`RowMateBle.dll`, C++) y el de Android (`RmBle`, Kotlin), y que lee `Core/Ble/Native/BleEventReader` ([10a 02-tech-design §2.6](../../../../../docs/migration/iterations/10a-ble-android-windows/02-tech-design.md)). `rm_ble_api_version()` = `RmBle.apiVersion()` = `BleWire.ApiVersion` = **1**; C# rechaza otra versión.

## Registro

Little endian. Cada registro:

| Campo | Tipo | Notas |
|---|---|---|
| `kind` | u8 | 1 a 7 (abajo); un tipo desconocido se saltea por su largo |
| `flags` | u8 | 0 |
| `payloadLength` | u16 | bytes del payload; registro entero ≤ 64 KiB |
| `monotonicSeconds` | f64 | reloj nativo: QPC (Windows) o `System.nanoTime()` (Android), la base del `Stopwatch` de la app |
| payload | | según `kind` |

`str` = u16 `n` + `n` bytes UTF-8 (`n` = 0 → null). `uuid` = 16 bytes en el orden del texto (`00001826-0000-1000-8000-00805f9b34fb` → `00 00 18 26 00 00 10 00 80 00 00 80 5f 9b 34 fb`).

| kind | Nombre | Payload |
|---|---|---|
| 1 | AdapterState | i32 `state` (orden de `BleAdapterState`: 0 Unknown, 1 Unsupported, 2 Unauthorized, 3 Off, 4 TurningOn, 5 On, 6 TurningOff; los plugins nunca emiten 2) |
| 2 | ScanResult | str `id` · str `name` · i16 `rssi` · u8 `n` · `n` × uuid |
| 3 | ScanFailed | i32 `code` (-2 = sin permiso) · str `message` |
| 4 | Connection | str `id` · u8 `connected` · i32 `code` |
| 5 | RequestDone | i32 `reqId` · i32 `status` (0 bien, -5 cancelado) · si es un descubrimiento bien: u8 `nSvc` { uuid · u8 `nChr` { uuid · u8 `props` } } · si `status` ≠ 0: str `message` |
| 6 | Notify | str `id` · uuid `characteristic` · u16 `n` · `n` bytes |
| 7 | Dropped | i32 `count` (el búfer descartó los más viejos) |

`props`: 1 Read, 2 Write, 4 WriteNoResponse, 8 Notify, 16 Indicate.

## La fijación (11 registros, 456 bytes)

Solo direcciones de los equipos ficticios K-10 (`AA:BB:CC:…`). La generan `Tests/Support/BleWireFixture.Encode()` (C#), `rm_ble_wire_fixture` (DLL) y `native/android/golden/EmitFixture.kt`; `BleWireTests.GoldenMatchesTheEncoder` la compara con el codificador de C#.

| # | t | Registro |
|---|---|---|
| 1 | 1,5 | AdapterState `5` (On) |
| 2 | 2,0 | ScanResult `AA:BB:CC:00:06:70`, `AMS-670B`, -61, [`1826`] |
| 3 | 2,25 | ScanResult `AA:BB:CC:00:00:02`, sin nombre, -75, [`1826`, `180d`] |
| 4 | 3,0 | ScanFailed -2, `permission denied` |
| 5 | 4,0 | Connection `AA:BB:CC:00:06:70`, conectado, 0 |
| 6 | 4,5 | RequestDone 1, 0 (un connect) |
| 7 | 5,0 | RequestDone 2, 0, servicio `1826` con `2ad1` (Notify) y `2ad9` (Write + Indicate) |
| 8 | 5,5 | RequestDone 3, 133, `GATT error 133` |
| 9 | 6,0 | Notify `AA:BB:CC:00:06:70`, `2ad1`, `2c 00 30 2a 01 e2 01 00 85 00 96 00` |
| 10 | 7,0 | Connection `AA:BB:CC:00:06:70`, desconectado, 8 |
| 11 | 8,0 | Dropped 3 |

Para regenerarla (solo si cambia el formato, junto con la versión): `dotnet test` con `--filter Name=RegenerateGolden`.
