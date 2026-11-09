# It. 11 — Bluetooth real: pulsómetro: reporte

**Estado:** el PO la cerró sin criterios en FAIL. Falta tu OK y la demo con una banda o un reloj.
**QA:** APROBADO CON OBSERVACIONES. No hay bloqueantes ni mayores. Quedan 3 menores, que pasan a issues.

## 1. Qué se entregó

- **El pulsómetro real, con la capa nativa de la 10a.** No se tocaron los plugins de Windows ni de Android. Todo se verificó con el adaptador falso y sensores ficticios: `Banda de prueba`, `Reloj de prueba`, uno sin nombre, `Sensor sin pulso` y los `Auriculares`, que nunca aparecen.
  - **Búsqueda:** dura 10 s y lista solo equipos con `0x180D`, en vivo y sin repetidos. Nunca muestra el remo ni los auriculares.
  - **Conexión manual:** tiene un tope de 15 s y comprueba que el sensor tenga `0x2A37`. Si no lo tiene, sale el error de incompatible.
  - **Memoria:** guarda el último sensor (`hrm.deviceId` / `hrm.deviceName`). Si conectás un sensor sin nombre, se ve `Sensor desconocido`.
  - **Al abrir la app, reconexión sola:** 6 intentos cada 10 s, directo a la dirección recordada y sin buscar. Si no lo encuentra, muestra `{nombre} · no encontrado` con `Conectar`, `Buscar otro` y `Olvidar`.
  - **Tras una caída:** reintenta cada 3 s sin límite. Si el Bluetooth está apagado, reconecta cuando lo prendés.
  - **Lectura vencida:** a los 10 s sin pulso se ve `--` con `Sin señal del sensor`. La conexión no se corta.
  - `Desconectar` y `Olvidar` cortan todo lo que esté en curso. Lo que tocás siempre manda sobre lo automático.
- **Tarjeta del pulsómetro** con todos sus estados: buscando, conectando, conectado, sin señal, reconectando, no encontrado y errores. Los errores van en lenguaje humano, con el detalle técnico debajo.
- **Avisos:**
  - `Pulsómetro conectado` dura 2 s y sale solo cuando la reconexión es automática.
  - `Se perdió el pulsómetro, reconectando…` dura 4 s sobre el HUD y no agrega elementos. Si hay un aviso del remo, ese tiene prioridad.
- **Entrenamiento:** el pulso del sensor tiene prioridad sobre el del monitor, en el HUD y en la sesión guardada. Si el sensor se cae, vuelve el pulso del monitor; si el monitor no manda pulso, la tarjeta BPM desaparece y la sesión sigue.
- **Remo y pulsómetro a la vez**, cada uno con su reconexión. Las búsquedas se excluyen entre sí: nunca corren dos a la vez.
- **Herramientas:** `build.ps1 -BleSmoke` ahora también busca pulsómetros 10 s, sin conectarse a nada.
- **El simulador no cambió.** Lo usan los tests y el build `simulator`.

## 2. Cómo probarlo

### (a) En Windows, con `Builds/Windows/normal` y un pulsómetro BLE (≈ 20 min)

**Antes de empezar:**
- Mojá la banda y ponétela. Si usás un reloj, abrí una app que transmita el pulso por Bluetooth (por ejemplo, Heart for Bluetooth en Wear OS, con los permisos "Sensores corporales" y "Dispositivos cercanos").
- Cerrá cualquier otra app que use el sensor.
- Si vas a usar el remo, encendé el AMS-670B.
- Los builds `normal` y `simulator` ya están regenerados con el arte final.
- Opcional: `dotnet run --project tools/ble-probe -- --show`. Lista los FTMS y los pulsómetros cercanos y no conecta.

Abrí `Builds/Windows/normal/RowMate.exe` en W1280:

1. **Sin sensor.** En Dispositivo se ve la tarjeta `PULSÓMETRO` con `Sin sensor` y la pista para el smartwatch ([CAP-01b](snapshots/CAP-01b-sin-sensor-w1280.png)).
2. **Buscar.** Tocá `Buscar`.
   - Durante 10 s se ve `Buscando sensores...` y aparece tu sensor. Un reloj suele salir como `Sensor desconocido`.
   - Mientras tanto, `Buscar dispositivos` del remo queda deshabilitado.
   - No aparece nada que no sea un pulsómetro ([CAP-02a](snapshots/CAP-02a-buscando-tv.png)).
3. **Conectar.** Tocá el sensor. Primero se ve `Conectando a {nombre}...` ([CAP-04](snapshots/CAP-04-conectando-tv.png)) y después el pulso en vivo ([CAP-05b](snapshots/CAP-05b-conectado-w1280.png)).
4. **Sin señal.** Sacate la banda o pausá la app del reloj 15 s.
   - A los 10 s se ve `--` con `Sin señal del sensor` ([CAP-06](snapshots/CAP-06-sin-senal-tv.png)).
   - Al volver, aparece el pulso real.
5. **Reabrir.** Cerrá y abrí la app. Se ve `{nombre} · reconectando...` ([CAP-07a](snapshots/CAP-07a-reconectando-tv.png)), se conecta sola y sale `Pulsómetro conectado`.
6. **Con el remo.** `Buscar dispositivos` → conectá el AMS-670B. Quedan los dos conectados, con la tarjeta del pulsómetro bajo la grilla ([CAP-15b](snapshots/CAP-15b-remo-y-pulsometro-w1280.png)).
7. **Entrenar.** Workout → `Entrenamiento libre` y remá 2 min.
   - El HUD muestra el pulso del sensor ([CAP-13a](snapshots/CAP-13a-hud-pulso-sensor-w1280.png)).
   - Apagá la banda o alejate 15 s. Sale `Se perdió el pulsómetro, reconectando…` y desaparece la tarjeta BPM ([CAP-14a](snapshots/CAP-14a-hud-pulsometro-perdido-w1280.png)).
   - Al volver la banda, sale `Pulsómetro conectado` y reaparece el pulso.
   - `Terminar` → Historial → detalle: el pulso promedio, el máximo y la curva tienen que ser los del sensor.
8. **Desconectar.** En Dispositivo, `Desconectar` → `Sin sensor`. Cerrá y abrí la app: **no** tiene que reconectarse.
9. **(Opcional) No encontrado.** Conectá el sensor, cerralo, apagá el sensor y abrí la app.
   - Se ve `· reconectando...` entre unos 50 s y 2,5 min (6 intentos).
   - Después, `· no encontrado` ([CAP-08a](snapshots/CAP-08a-no-encontrado-tv.png)).
   - `Olvidar` → `Sin sensor`.
10. **Mandame en privado** las líneas `[HRM]` de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate\Player.log`. Las direcciones van enmascaradas, pero el nombre del equipo aparece, así que el log no se sube. Anotá también si el paso 5 reconectó sin buscar: es el riesgo R-01 de Windows.

**Android:** con "Android Build Support" instalado y el APK generado, repetí los pasos 1 a 8 en el teléfono. Además, dejá la app 60 s en segundo plano durante el entrenamiento: al volver, el pulso tiene que seguir llegando.

### (b) Con el simulador, sin hardware (≈ 5 min)

`Builds/Windows/simulator/RowMate.exe`:

1. Dispositivo → `Buscar` en la tarjeta → `Pulsómetro simulado`. La tarjeta muestra el pulso (QA vio 143 bpm).
2. Workout → `Entrenamiento libre`. El HUD muestra `BPM` con ese valor.
3. A los 30-40 s, `Terminar` y confirmar. En Historial → detalle, el pulso promedio y el máximo salen del sensor.

El simulador no escribe `hrm.*` ni abre el Bluetooth real. Los estados del servicio real (reconectando, no encontrado, avisos) se ven en las capturas y en los tests, no en este build.

## 3. Resultados reales

| Corrida | Resultado |
|---|---|
| dotnet `-Matrix` | Build sin advertencias ni errores. Herramientas 194/194. Core 3776: 3737 pasan, 39 omitidos (dependen del equipo), 0 fallan |
| Matriz con Unity | 1003 PASA, 0 FALLA, 4 ignorados, 0 pendientes de Unity, 13 sin test (todos de equipo) |
| Casos legacy LT-139 a LT-145 (`reconnect_loop_test`) | 7 de 7 pasan, con los mismos números |
| Unity EditMode | 3880: 3832 pasan, 48 ignorados, 0 fallan. Con simulador, 4074: 4026 pasan, 0 fallan |
| Unity PlayMode | 638/638; con simulador, 713/713. Con el sensor conectado, 0 bytes asignados por cuadro |
| Build `-BleSmoke` (adaptador real de la PC) | `[HRM] humo: busquedas=1 sensores=0 ajenos=0 fallas=0` y el remo con `ftms=0 fallas=0`. No hubo conexiones y la radio y los equipos emparejados quedaron intactos |
| Build `-Simulator` | Sesión libre guardada con 10/10 puntos con pulso (promedio y máximo 143), sin `prefs.json` |
| Repo público | No hay rutas, nombres ni direcciones reales: solo `AA:BB:CC:…`. Todo está en UTF-8. El legacy sigue intacto |

## 4. Criterios que fallan o no se pueden verificar

**Ninguno falla.** Cumplen 39 de 41.

Esperan tu demo con el sensor:
- **AC-HRM-HW-01**: pasos 1 a 9 de la demo de Windows con un pulsómetro real y el AMS-670B.

Esperan el teléfono:
- **AC-HRM-HW-02**: la demo de Android, que incluye los 60 s en segundo plano. Hace falta "Android Build Support" y un teléfono.

Menores de QA (van a issues):
- La captura del aviso en Dispositivo sale con la tarjeta vacía por un problema de tiempo de la toma ([CAP-12a](snapshots/CAP-12a-aviso-conectado-tv.png)). La app está bien.
- En L640, el desplazamiento automático tapa el título de la tarjeta mientras busca ([CAP-03a](snapshots/CAP-03a-lista-l640.png)).
- Falta un test de "reducir movimiento" para el aviso.

## 5. Decisiones por defecto (respondé solo si querés cambiar alguna)

- **Sensor sin nombre:** se ve `Sensor desconocido` y se borra el nombre del sensor anterior.
- **Errores en lenguaje humano:** `No se pudo buscar sensores` / `No se pudo conectar con {nombre}`, con el detalle técnico debajo. Si fallan `Desconectar` u `Olvidar`, también se ve `No se pudo conectar con {nombre}`.
- **Avisos:** `Pulsómetro conectado` dura 2 s y sale solo en las reconexiones automáticas. `Se perdió el pulsómetro, reconectando…` dura 4 s sobre el HUD, y el remo tiene prioridad.
- **Sin señal:** `--` + `Sin señal del sensor` con el corazón atenuado. El legacy mostraba solo `--`.
- **Bluetooth apagado durante una caída:** sigue intentando cada 3 s y reconecta al encenderlo, igual que el legacy.
- **Reconexión directa a la dirección recordada, sin buscar, también en Windows.** Se confirma en tu demo (paso 10).
- **Android en segundo plano:** sin servicio en primer plano, como en la 10a.
- **Registro:** en el `Player.log` las direcciones van enmascaradas (`AA:BB:…:0D`).

## 6. Capturas

Son 28 tomas, todas con el adaptador falso: el [índice](snapshots/INDEX.md). Destacadas:
- Búsqueda: [CAP-02a](snapshots/CAP-02a-buscando-tv.png)
- Conectado: [CAP-05a](snapshots/CAP-05a-conectado-tv.png), en inglés [CAP-05c](snapshots/CAP-05c-conectado-tv-en.png)
- Sin señal: [CAP-06](snapshots/CAP-06-sin-senal-tv.png)
- Reconectando: [CAP-07a](snapshots/CAP-07a-reconectando-tv.png)
- No encontrado: [CAP-08a](snapshots/CAP-08a-no-encontrado-tv.png)
- Incompatible: [CAP-09](snapshots/CAP-09-incompatible-tv.png)
- Error al conectar: [CAP-10](snapshots/CAP-10-error-conectar-tv.png)
- Error al buscar: [CAP-11](snapshots/CAP-11-error-buscar-tv.png)
- HUD con el pulso del sensor: [CAP-13a](snapshots/CAP-13a-hud-pulso-sensor-w1280.png)
- HUD con el sensor perdido: [CAP-14a](snapshots/CAP-14a-hud-pulsometro-perdido-w1280.png)
- Remo y pulsómetro: [CAP-15a](snapshots/CAP-15a-remo-y-pulsometro-tv.png)
- Remo buscando: [CAP-16](snapshots/CAP-16-remo-buscando-tv.png)

Las capturas se regeneraron con el arte final (corazón en los avisos), 28 tomas sin problemas.

## 7. Lo que viene

**La 12: Strava, conexión, subida y TCX.** Necesita una cuenta de Strava. Quedan pendientes las demos de la 10a (remo) y de la 11 (pulsómetro). La deuda queda en 10 issues nuevos.

¿Le das el OK a la 11? Con tu OK la commiteo, la pusheo y arranco la 12.
