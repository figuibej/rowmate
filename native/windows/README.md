# native/windows — RowMateBle.dll

El plugin de Bluetooth LE de Windows de la It. 10a ([02-tech-design §3](../../docs/migration/iterations/10a-ble-android-windows/02-tech-design.md)): una DLL C++/WinRT (`Windows.Devices.Bluetooth`) con una API C plana (`rm_ble_*`, [`RowMateBle/rm_ble.h`](RowMateBle/rm_ble.h)) que Unity usa por P/Invoke (`Platform/Ble/WindowsBleApi`) y `tools/ble-probe` también.

## Qué hace

- **Hilos**: el hilo de Unity nunca toca WinRT. `rm_ble_init` arranca un hilo MTA del adaptador (estado de la radio y búsqueda) y cada `connect` abre una sesión con su propio hilo MTA (la cola GATT del equipo: una operación a la vez, tope de 10 s por operación). Los *callbacks* de WinRT solo copian registros al búfer.
- **Búfer de eventos**: 4 MiB con el formato binario v1 (`wire.h`, el mismo que `Core/Ble/Native/BleWire.cs` y Kotlin; fijado en `Assets/RowMate/Tests/Golden/ble/events-v1.bin`). Lleno, descarta los más viejos y avisa con un `Dropped`. Unity lo drena en cada cuadro (`rm_ble_pending` / `rm_ble_drain`, sin asignar cuando no hay nada).
- **Búsqueda**: `BluetoothLEAdvertisementWatcher` activo, sin filtro; fusiona anuncio y respuesta de escaneo por dirección (el nombre y el UUID suelen venir separados) y suma el UUID de 16 bits del *Service Data* (`0x16`). El filtro FTMS lo aplica Core.
- **Sesiones**: un `connect` sobre un id con sesión primero la cierra; `disconnect` cancela un `connect` pendiente (`RequestDone` con `-5`). Nunca empareja ni desempareja, nunca cambia el estado de la radio ni le pide acceso.
- **Reloj**: `QueryPerformanceCounter`, el mismo ritmo que el `Stopwatch` de la app (el origen puede diferir; `BleEventClock` lo calibra).

## Compilar

```
powershell -NoProfile -File native/windows/build.ps1          # compila y copia a Assets/Plugins/Windows/x86_64/RowMateBle.dll
powershell -NoProfile -File native/windows/build.ps1 -Check   # compila en temporal y compara el SHA-256 con la versionada
```

Necesita Visual Studio (2022 o posterior) con "Desarrollo para el escritorio con C++" y el Windows SDK 10.0.26100 o posterior (trae C++/WinRT). El script lo encuentra con `vswhere` y `Launch-VsDevShell`, sin rutas fijas. Compila con `/std:c++20 /MT /W4 /WX /Brepro /DWINRT_NO_SOURCE_LOCATION`, sin PDB, con la CRT estática (sin redistribuible).

Revisiones que el script nunca saltea:

- **Fuentes** (código 3): ninguna macro de archivo, `source_location` ni `assert(`, y ninguna API de emparejado ni de la radio.
- **Binario** (código 2): ningún `C:\`, `\Users\`, `\Windows Kits\`, el usuario ni el nombre del equipo, en ASCII ni en UTF-16. El repo es público.

Se niega a copiar con el proyecto abierto en Unity (código 5: el Editor tiene la DLL cargada). `/Brepro` la hace reproducible: con el mismo Visual Studio, `-Check` da idéntica.

La DLL va versionada **sin LFS** (`.gitattributes`): un clon sin LFS la tiene igual. `WindowsDllContractTests` (dotnet y Unity) comprueba que escribe `events-v1.bin` byte por byte, la versión, el ritmo del reloj y que no lleva rutas.

## Probar a mano

`tools/ble-probe` busca con la misma DLL y lista **solo** remos FTMS (`0x1826`) y pulsómetros (`0x180D`), con la dirección enmascarada (`--show` la muestra entera). Nunca conecta.

```
dotnet build tools/ble-probe/BleProbe.csproj -c Release --artifacts-path <scratch>/probe
<scratch>/probe/bin/BleProbe/release/RowMate.BleProbe.exe [--seconds 10] [--show] [--selftest]
```

`--selftest` (QA): 3 s de búsqueda e imprime solo contadores. El humo dentro del player es `tools/build/build.ps1 -Target Windows -BleSmoke`.
