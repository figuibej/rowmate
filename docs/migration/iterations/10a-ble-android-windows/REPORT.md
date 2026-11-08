# It. 10a — Bluetooth real en Android y Windows: reporte

**Estado:** cerrada por el PO, sin criterios en FAIL. Falta tu OK y la demo con el remo.
**QA:** APROBADO CON OBSERVACIONES. No hay bloqueantes ni mayores; quedan 6 menores, que pasan a issues.

## 1. Qué se entregó

- **Lógica del Bluetooth del remo, toda testeable sin hardware.** Es un puerto línea por línea del legacy, verificado con un adaptador falso con equipos ficticios (`AMS-670B`, sin nombre, una bici FTMS, un remo sin Control Point y unos auriculares que nunca aparecen):
  - Estado del adaptador y permisos.
  - Búsqueda de 10 s solo de FTMS, con la lista en vivo.
  - Conexión con tope de 15 s, toma de control (`0x00` → 300 ms → `0x07`) y keepalive cada 2 s.
  - Reconexión 3 s después de cada fallo, sin límite.
  - Aviso de monitor apagado a los 5-6 s.
  - Diagnóstico oculto.
- **Plugin nativo de Windows.** `RowMateBle.dll` en C++/WinRT. Se compila con un script versionado que busca Visual Studio con vswhere. Es reproducible byte a byte y no tiene rutas embebidas.
- **Plugin nativo de Android.** Kotlin con su manifiesto de permisos. Compila contra Android 36 y sus firmas JNI coinciden con C# (24 de 24). El APK todavía no se generó: falta el módulo de Unity.
- **Pantallas nuevas en Dispositivo:**
  - Las vistas "Bluetooth apagado", "sin permiso" y "sin Bluetooth LE".
  - El sonar mientras busca.
  - Las tarjetas con los pasos `Conectando…` → `Tomando control del monitor…` → `Listo`.
  - La caja de reconexión y el panel de diagnóstico en mono.
- **Avisos sobre el HUD.** `Reconectando con {remo}…`, `Reconectado con {remo}` y `El monitor dejó de enviar datos` van en el lugar de los avisos y no tapan ninguna métrica. El HUD sigue en 59 elementos.
- **Paso por distancia.** Un monitor que manda la distancia cada 2 s ya no traba el paso ni los parciales.
- **Herramientas:**
  - `tools/ble-probe` lista los FTMS y pulsómetros cercanos. Nunca conecta.
  - `build.ps1 -BleSmoke` hace 3 búsquedas reales en un build de desarrollo, sin conectar.
- **El simulador no cambió.** Lo usan los tests y el build `simulator`.

## 2. Cómo probarlo

### (a) En esta PC, con el build de Windows y el AMS-670B (≈ 25 min)

**Antes de empezar:**
- Encendé el monitor del remo.
- Cerrá cualquier otra app que lo use (la legacy, la del fabricante). Un monitor FTMS acepta una sola conexión.
- Opcional, para ver que la PC lo detecta: `dotnet run --project tools/ble-probe -- --show`. Solo lee y lista FTMS y pulsómetros.

Abrí `Builds/Windows/normal/RowMate.exe`.

1. **Bluetooth apagado.** Apagá el Bluetooth a mano en Configuración → Bluetooth y dispositivos.
   - Dispositivo muestra `Bluetooth apagado` con `Activar Bluetooth` ([CAP-01b](snapshots/CAP-01b-bt-apagado-w1280.png)).
   - Tocar el botón abre los ajustes de Bluetooth.
   - Encendelo: la vista se transforma sola en la de búsqueda, pero no empieza a buscar.
2. **Buscar.** Tocá `Buscar dispositivos`.
   - Durante 10 s ves `Buscando...` con el sonar.
   - El remo aparece con su nombre y su dirección.
   - No aparece ningún equipo que no sea FTMS (ni auriculares ni parlantes).
3. **Conectar.** Tocá el remo.
   - Ves `Conectando…`, `Tomando control del monitor…` y `Listo` con el check ([CAP-09](snapshots/CAP-09-conectando-tv.png) a [CAP-11](snapshots/CAP-11-listo-tv.png)).
   - El monitor pasa a modo Bluetooth.
   - Al remar, el tablero muestra datos reales en menos de 3 s.
4. **Diagnóstico.** Bajá el tablero 3 veces o mantené presionada la cabecera del remo 0,5 s, y tocá `Show BLE debug`. Tiene que mostrar `RECEIVING`, el contador subiendo y los bytes en hex ([CAP-19b](snapshots/CAP-19b-diagnostico-w1280.png)).
5. **Entrenar.** Workout → `Entrenamiento libre`. Remá 1 min.
6. **Segundo plano.** Minimizá la ventana 30 s y volvé. El monitor sigue en modo Bluetooth y la sesión siguió.
7. **Corte breve.** Con el entrenamiento en marcha, apagá el Bluetooth de la PC a mano unos 5 s (o alejate del remo).
   - Sobre el HUD aparece `Reconectando con AMS-670B…` ([CAP-15a](snapshots/CAP-15a-hud-reconectando-w1280.png)).
   - Encendelo: `Reconectado con AMS-670B` durante 2 s y los números siguen.
8. **Pausa.** Dejá de remar 10 s sin apagar nada. Anotá si salta `El monitor dejó de enviar datos`: si salta, el monitor no notifica en las pausas y se abre un issue.
9. **Monitor apagado.** Apagalo.
   - A los 5-6 s se ve `El monitor dejó de enviar datos` sobre el HUD ([CAP-18](snapshots/CAP-18-hud-monitor-apagado-w1280.png)). En Dispositivo aparece con `Reintentar` ([CAP-17](snapshots/CAP-17-monitor-apagado-tv.png)).
   - `Terminar` y mirá la sesión en Historial.
   - Encendé el monitor, tocá `Reintentar` y comprobá que reconecta.
10. **Diez minutos.** Remá 10 min sin tocar el monitor: tiene que seguir en modo Bluetooth.
11. **Desconectar.** Tocá `Desconectar` en la barra superior. A los 10 s sigue desconectado: el remo queda olvidado.
12. **Mandame la cadencia.** Copiá las líneas `[BLE] Cadencia:` de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate\Player.log`, junto con lo que anotaste:
    - si los contadores arrancan en cero al conectar;
    - si notifica en las pausas.

    El registro no se sube al repo: las direcciones van enmascaradas, pero el nombre del equipo puede aparecer.

### (b) En Android (requiere instalación previa)

1. Unity Hub → Installs → 6000.3.27f1 → ⚙ → Add modules → **Android Build Support**, con "OpenJDK" y "Android SDK & NDK Tools".
2. Abrí el proyecto una vez en el Editor (importa el Kotlin y el `.androidlib`) y cerralo.
3. `powershell -NoProfile -File tools/build/build.ps1 -Target Android -Backend IL2CPP`. El APK sale en `Builds/Android/normal/`.
4. Revisá los permisos del APK con `aapt2 dump xmltree <apk> --file AndroidManifest.xml`. Tienen que estar `BLUETOOTH_SCAN` (`neverForLocation`), `BLUETOOTH_CONNECT`, `BLUETOOTH`, `BLUETOOTH_ADMIN` y `ACCESS_FINE_LOCATION` con tope en API 30, `WAKE_LOCK` y `bluetooth_le` requerido.
5. `adb install -r <apk>` en un Android 12 o más nuevo, y la demo:
   - Con el Bluetooth apagado → `Activar Bluetooth` → diálogo del sistema → la vista pasa sola a la búsqueda.
   - `Buscar` → diálogo de "Dispositivos cercanos". Si lo negás y reabrís la app, se ve `Permiso de Bluetooth denegado`.
   - Conectar, remar y abrir el diagnóstico. Al quedar `Listo` hay una vibración corta.
   - Con un entrenamiento en marcha, bloqueá el teléfono 60 s: el monitor y la sesión siguen, y la pantalla no se apaga con la app adelante.

Si el Gradle de Unity no toma los `.kt` (riesgo R-01), avisame: el mismo código se empaqueta como `.aar` (está en los issues).

### (c) Con el simulador

`Builds/Windows/simulator/RowMate.exe` funciona como en la 9. Nunca escanea ni abre la radio. Las demos de las iteraciones 2 a 9 siguen valiendo. QA hizo una sesión libre y una rutina, y quedaron las 2 guardadas.

## 3. Resultados reales

| Corrida | Resultado |
|---|---|
| dotnet `-Matrix` | 3632 tests: 3593 pasan, 39 omitidos, **0 fallan**. Herramientas 192/192. Matriz: 772 PASA, 0 FALLA |
| Unity EditMode | 3688/3736 pasan, 48 ignorados, 0 fallan. Con simulador: 3880/3928, 0 fallan |
| Unity PlayMode | 622/622. Con simulador: 697/697 |
| Kotlin (`check.ps1 -Golden`) | Compila contra android-36 con `-Werror`. JNI 24/24. El archivo de eventos que escribe Kotlin es idéntico al de C# |
| DLL (`build.ps1 -Check`) | Reproducible (mismo SHA-256) y sin rutas |
| `ble-probe --selftest` ×3 | Adaptador `On`, `ftms=0`, equipos no FTMS descartados sin mostrarse |
| Build normal | Abre en la búsqueda con `Adapter state: On` y sin excepciones |
| Build `-BleSmoke` | 3 búsquedas reales de 10 s (una minimizada): `fallas=0`, `colgado=no`, `soloFtms=si` |
| Build `-Simulator` | OK: libre y rutina guardadas |
| ProjectSettings | Sin cambios en ninguna corrida |
| Repo público | Sin rutas, usuario, MAC reales ni nombres de equipos. Las capturas usan solo el adaptador falso |

Durante el workflow nadie apagó ni prendió la radio, ni emparejó o conectó nada. Tus equipos emparejados no se tocaron.

## 4. Criterios no verificables (6 de 43; ninguno falla)

**Esperan tu demo con el remo:**
- AC-BLE-06-08: modo Bluetooth, datos en menos de 3 s y 10 min estable (pasos 3 y 10).
- AC-BLE-08-07: corte breve en un entrenamiento (paso 7).
- AC-BLE-09-03: monitor apagado y pausa de 10 s (pasos 8 y 9).
- AC-BLE-91-02: segundo plano con el remo real. En Windows, minimizar mientras busca ya pasó.

**Esperan Android Build Support y un teléfono:**
- AC-APP-13-02: el diálogo de permiso.
- AC-APP-14-05: la pantalla siempre encendida.

**Además, verificados solo en parte:**
- AC-APP-13-01: el manifiesto pasa como texto; falta revisar el APK.
- AC-BLE-04-04: en Windows pasa por código (no se podía apagar la radio); en Android queda pendiente.
- AC-BLE-06-07: la vibración en Android.

## 5. Observaciones de QA (menores, van a issues)

- **m-01.** En pantalla vertical, `Reconectando con AMS-67…` corta el nombre ([CAP-14](snapshots/CAP-14-reconectando-tv.png)).
- **m-02.** En Android, mientras el Bluetooth se enciende se ve `Bluetooth apagado`.
- **m-03.** `Buscando...` tiene poco contraste ([CAP-05](snapshots/CAP-05-buscando-tv.png)).
- **m-04.** El banner repite la operación: `Error al escanear: scan: error 2` ([CAP-08](snapshots/CAP-08-error-escanear-tv.png)).
- **m-05.** Una DLL de otra versión deja el remo sin mensaje.
- **m-06.** En horizontal en el teléfono, la lista queda apretada y el pulsómetro se corta ([CAP-07b](snapshots/CAP-07b-lista-pulsometro-th.png)).

## 6. Decisiones por defecto (respondé solo si querés cambiar alguna)

- **Reconexión.** Reintenta cada 3 s sin límite, y `Buscar` la cancela. El legacy hace un solo intento.
- **Bluetooth apagado durante la reconexión.** Reconecta solo al encenderlo.
- **Monitor apagado.** Avisa en Dispositivo con `Reintentar` y 4 s sobre el HUD. La sesión sigue.
- **Avisos sobre el HUD.** `Reconectando…` mientras dura y `Reconectado` durante 2 s, en el lugar de los avisos.
- **Permiso negado en Android.** Se ve la vista "sin permiso", no un error, y se recuerda al reabrir la app.
- **Equipo sin nombre.** Se muestra `Dispositivo desconocido`, traducido, también en la cabecera.
- **`Activar Bluetooth` en Windows.** Abre los ajustes de Bluetooth. Las pistas siguen hablando de iOS, como en el legacy (hay issue para pistas por plataforma).
- **Distancia.** Tope de 10 m por segundo desde el último cambio, en el paso y en los parciales.
- **Diagnóstico.** También se abre manteniendo presionada la cabecera 0,5 s.
- **Android en segundo plano.** Sin servicio en primer plano hasta ver si el sistema mata la app. El reloj de la app no cuenta el sueño profundo; se mide en el teléfono.
- **DLL de Windows.** Se compila con un script y se versiona sin LFS.

## 7. Capturas (adaptador falso, sin equipos reales)

**Estados del adaptador:**
- Apagado: [CAP-01a](snapshots/CAP-01a-bt-apagado-tv.png), [CAP-01b](snapshots/CAP-01b-bt-apagado-w1280.png), [CAP-02 (en)](snapshots/CAP-02-bt-apagado-tv-en.png).
- Sin permiso: [CAP-03](snapshots/CAP-03-sin-permiso-tv.png).
- Sin Bluetooth LE: [CAP-04](snapshots/CAP-04-sin-ble-tv.png).
- "Se despierta" al encender: [AUX-10a-02a](snapshots/AUX-10a-02a-0ms.png) → [AUX-10a-02d](snapshots/AUX-10a-02d-720ms.png).

**Búsqueda y lista:**
- Buscando: [CAP-05](snapshots/CAP-05-buscando-tv.png).
- Lista: [CAP-06a](snapshots/CAP-06a-lista-tv.png), [CAP-06b](snapshots/CAP-06b-lista-w1280.png).
- Con el pulsómetro en horizontal: [CAP-07a](snapshots/CAP-07a-lista-pulsometro-l640.png), [CAP-07b](snapshots/CAP-07b-lista-pulsometro-th.png).
- Error al buscar: [CAP-08](snapshots/CAP-08-error-escanear-tv.png).

**Conexión:**
- Pasos: [CAP-09](snapshots/CAP-09-conectando-tv.png), [CAP-10](snapshots/CAP-10-tomando-control-tv.png), [CAP-11](snapshots/CAP-11-listo-tv.png).
- Error al conectar: [CAP-12](snapshots/CAP-12-error-conectar-tv.png).
- Conectado: [CAP-13a](snapshots/CAP-13a-conectado-tv.png), [CAP-13b](snapshots/CAP-13b-conectado-w1280.png).

**Reconexión y monitor apagado:**
- Reconectando en Dispositivo: [CAP-14](snapshots/CAP-14-reconectando-tv.png).
- Sobre el HUD: [CAP-15a](snapshots/CAP-15a-hud-reconectando-w1280.png), [CAP-15b](snapshots/CAP-15b-hud-reconectando-tv.png), [CAP-16 (en)](snapshots/CAP-16-hud-reconectado-w1280-en.png).
- Peor caso del HUD: [AUX-10a-01](snapshots/AUX-10a-01-hud-tv-peor-caso-aviso.png).
- Monitor apagado: [CAP-17](snapshots/CAP-17-monitor-apagado-tv.png), [CAP-18](snapshots/CAP-18-hud-monitor-apagado-w1280.png).

**Diagnóstico:** [CAP-19a](snapshots/CAP-19a-diagnostico-tv.png), [CAP-19b](snapshots/CAP-19b-diagnostico-w1280.png), [CAP-20 (sin datos)](snapshots/CAP-20-diagnostico-sin-datos-tv.png).

## 8. Lo que viene

**It. 11, pulsómetro real.** Necesita una banda o un reloj que transmita pulso por Bluetooth (`0x180D`), y reutiliza los plugins nativos de esta iteración. La 6b sigue postergada y la 10b espera una Mac. Los menores y los pendientes quedan en 17 issues de GitHub.
