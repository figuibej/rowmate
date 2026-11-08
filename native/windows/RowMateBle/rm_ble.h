// RowMateBle.dll: el plugin de Bluetooth LE de Windows (It. 10a, 02-tech-design §3). API C plana, __cdecl, cadenas UTF-8
// terminadas en 0. Nada lanza: todo error es un código (<= 0) o un RequestDone con mensaje en el búfer de eventos (formato v1,
// wire.h; lo lee Core/Ble/Native/BleEventReader). El hilo que llama (el de Unity) nunca toca WinRT: cada función encola el
// trabajo en el hilo MTA del adaptador o en la cola GATT del equipo y vuelve en el acto.
// Nunca empareja, desempareja ni cambia la radio (AC-BLE-SEC-01).
#pragma once

#include <stdint.h>

#ifdef RM_BLE_EXPORTS
#define RM_BLE_API extern "C" __declspec(dllexport)
#else
#define RM_BLE_API extern "C" __declspec(dllimport)
#endif

// 0 bien. Idempotente: arranca el hilo del adaptador y espera (como mucho 3 s) la primera lectura del estado.
RM_BLE_API int32_t __cdecl rm_ble_init(void);

// Detiene la búsqueda, cierra las sesiones y une los hilos. Idempotente; después se puede volver a llamar rm_ble_init.
RM_BLE_API void __cdecl rm_ble_shutdown(void);

// Versión del formato de eventos y de esta API (BleWire.ApiVersion = 1).
RM_BLE_API int32_t __cdecl rm_ble_api_version(void);

// Reloj de los eventos: QueryPerformanceCounter en segundos (la base del Stopwatch de la app, DT10-18).
RM_BLE_API double __cdecl rm_clock_monotonic(void);

// Estado del adaptador en el orden de BleAdapterState (0 Unknown, 1 Unsupported, 3 Off, 5 On); nunca 2 (Unauthorized).
RM_BLE_API int32_t __cdecl rm_ble_adapter_state(void);

// Búsqueda activa SIN filtro (DT10-07: el filtro lo aplica Core); el CSV de servicios se ignora. 0 bien; las fallas llegan como
// ScanFailed. Reemplaza la búsqueda en curso.
RM_BLE_API int32_t __cdecl rm_ble_start_scan(const char* servicesCsv);

RM_BLE_API int32_t __cdecl rm_ble_stop_scan(void);

// reqId > 0 (termina con RequestDone) o un código <= 0. Un connect sobre un id con sesión (abierta o pendiente) primero la cierra
// (DT10-17). highMtu se ignora: Windows negocia MaxPduSize solo.
RM_BLE_API int32_t __cdecl rm_ble_connect(const char* id, int32_t highMtu);

// Cierra la sesión del id; con un connect pendiente, lo cancela (su RequestDone sale con -5). Sin sesión: 0.
RM_BLE_API int32_t __cdecl rm_ble_disconnect(const char* id);

RM_BLE_API int32_t __cdecl rm_ble_discover(const char* id);

RM_BLE_API int32_t __cdecl rm_ble_set_notify(const char* id, const char* service, const char* characteristic, int32_t enable);

RM_BLE_API int32_t __cdecl rm_ble_write(const char* id, const char* service, const char* characteristic, const uint8_t* data,
                                        int32_t length, int32_t withResponse);

// Sin keepalive nativo en Windows (DT10-09: el loop de Unity sigue minimizado). Devuelven 0.
RM_BLE_API int32_t __cdecl rm_ble_keepalive_start(const char* id, const char* service, const char* characteristic, const uint8_t* data,
                                                  int32_t length, int32_t periodMs);

RM_BLE_API int32_t __cdecl rm_ble_keepalive_stop(const char* id);

// Bytes pendientes en el búfer de eventos (0 = nada que drenar; no toma más que un mutex).
RM_BLE_API int32_t __cdecl rm_ble_pending(void);

// Copia registros ENTEROS hasta capacity; devuelve los bytes copiados.
RM_BLE_API int32_t __cdecl rm_ble_drain(uint8_t* buffer, int32_t capacity);

// La fijación del formato (Tests/Golden/ble/events-v1.bin) con el codificador real: devuelve los bytes escritos, o -n si capacity
// no alcanza (n = los necesarios). Para WindowsDllContractTests; no necesita rm_ble_init.
RM_BLE_API int32_t __cdecl rm_ble_wire_fixture(uint8_t* buffer, int32_t capacity);
