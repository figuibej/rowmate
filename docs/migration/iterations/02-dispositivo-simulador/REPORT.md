# It. 2 — Shell, Dispositivo en vivo y simulador: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA, con una condición.** La primera app usable ya está: abre, muestra el remo y el pulsómetro simulados en vivo y se maneja desde el panel, con los números, textos y tiempos del legacy. Falla un solo criterio: al desconectar el remo se ve un nombre equivocado durante 120 ms (QA-V2-01). Se arregla como **primera tarea de la It. 3**, y ese criterio queda reabierto hasta entonces |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** ([05-qa-verification.md](05-qa-verification.md)): 0 problemas graves, 0 mayores y 15 menores |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió) · [02-tech-design](02-tech-design.md) (cómo se hizo) · [03-qa-harmony](03-qa-harmony.md) (control previo) · [04-design-notes](04-design-notes.md) (aspecto y movimiento) · [05-qa-verification](05-qa-verification.md) (verificación final) · [00-primer-open](00-primer-open.md), [00-deuda-it1](00-deuda-it1.md) y [00-deuda-diseno](00-deuda-diseno.md) (trabajo previo) · [snapshots/INDEX.md](snapshots/INDEX.md) (capturas) |

---

## 1. En pocas palabras

La It. 2 es la **primera versión de RowMate que se puede abrir y usar**:

- Arranca en **Dispositivo** con `Remo simulado` + `Conectado`, y la grilla `SPLIT 500m` / `SPM` / `VATIOS` / `DISTANCIA` cambia cada segundo. Tiene 2 columnas hasta 640 dp de ancho y 3 si la ventana es más ancha.
- El **pulsómetro simulado** se busca, se conecta y muestra el pulso con un corazón que late a ese ritmo. Después se desconecta.
- El **panel del simulador** (botón de la llave) cambia la intensidad, el ritmo y la hora, y provoca caídas del remo y del pulsómetro desde cualquier pestaña.
- La **barra de pestañas** tiene Dispositivo, Workout, Rutinas e Historial. Las tres últimas todavía son provisorias.
- El **tema es oscuro**, cada métrica tiene su color y la tipografía y los íconos son los definitivos. La app sale en español o en inglés según el sistema.

Se comporta como el legacy: mismos números (`02:13`, `24.0`, `150`, `226m` a los 60 s), mismos textos, mismos tiempos (3 s de caída, 10 s de búsqueda) y mismas reglas. Las diferencias a propósito están en §6. Encima se sumaron las mejoras EXR pedidas:

- Los dígitos que cambian ruedan y terminan siempre en el valor real.
- El borde de la tarjeta se enciende cuando llega un dato nuevo, y solo si el valor cambió.
- El corazón late al pulso real.
- La píldora de la barra se desliza.
- El tablero se "apaga" cuando el remo cae y se "enciende" cuando vuelve.
- La app arranca sin destellos blancos.

Además:

- El proyecto **abre en Unity sin errores**.
- Las **1264 pruebas** de la lógica corren también dentro de Unity, incluidas las del simulador.
- Hay **85 pruebas de la interfaz montada**: la app real en ventanas de 640 × 360, 800 × 360 y otras.
- Las **38 capturas y los 3 videos** los genera Unity solo. Si se vuelven a generar, salen idénticos byte a byte.
- El **build normal de Windows** se llama `RowMate`, no trae nada del simulador, guarda en `AppData\LocalLow\iguisoft\RowMate` y no toca la app legacy.

Lo único que no se pudo ver es Android y el build IL2CPP de Windows (faltan los módulos de Unity) y el CI en GitHub (no hay repositorio remoto).

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que pasa todo lo que se pudo verificar. "Con parte pendiente" quiere decir que cumple en el Editor y en la PC, pero falta verlo en Android o en otro caso de equipo (§5.3). "No verificable" está en §5.2.

| ID | Qué quedó listo | Criterios | Dónde se ve |
|---|---|---|---|
| F-APP-01 | **Arranque**: abre siempre en Dispositivo; servicios simulados o normales según el build; pestañas vivas desde el inicio; nombre `RowMate`; sin destellos; cierre sin temporizadores colgados | 8 de 8 (2 con parte Android pendiente) | Al abrir la app |
| F-APP-02 | **Barra de pestañas**: 4 destinos en orden, con íconos que cambian al elegir y píldora; Perfil solo con Strava (probado con una configuración de prueba); cada pestaña conserva su estado; provisorias con título e ícono apagado | 7 de 8 (1 con parte de equipo pendiente); 1 no verificable ("Atrás" de Android) | Barra inferior |
| F-APP-05 | **Tema y estilos**: siempre oscuro, `#0D1B2A` / `#1A2E45`, Inter y Barlow, cifras de ancho fijo, letra del sistema hasta 1,3×, texto terciario más legible, foco de teclado, "reducir movimiento", tamaño físico como el legacy | 8 de 9 (4 con parte de equipo pendiente); 1 no verificable (modo claro de Windows) | Toda la app |
| F-APP-06 | **Colores por métrica** en pantalla (split `#FF9F1C`, SPM `#00B4D8`, vatios `#FFD60A`, distancia `#4CC9F0`, pulso `#EF476F`), medidos en las capturas con ±1 por canal; una sola fuente de colores | 2 de 2 | Tablero y pulsómetro |
| F-APP-08 (+ F-RUT-19) | **Descarte** de `MetricCard` e `IntervalTile` (el legacy no los usa); una sola tarjeta de métrica para el tablero y el pulsómetro | 2 de 2 | — |
| F-APP-19 (también toca) | `ci-unity.yml` | No verificable: no hay repositorio ni licencia, y además el archivo no es válido (QA-V2-14) | — |
| F-BLE-05 (también toca) | Botón `Buscar dispositivos` y estado vacío `No se encontraron dispositivos` | 3 de 3 | Vista de búsqueda |
| F-BLE-07 | **Desconectar** el remo: solo con el remo conectado, sin confirmación y sin reconexión automática | 4 de 4 | Barra superior de Dispositivo |
| F-BLE-08 (también toca) | Caída simulada vista desde Dispositivo: 3 s en la vista de búsqueda y vuelve | 1 de 1 | Dispositivo |
| F-BLE-10 (también toca) | Dispositivo como primera pestaña; el remo ocupado frena la búsqueda del pulsómetro | 2 de 2 | Barra y tarjeta |
| F-BLE-11 | **Vista conectada**: cabecera, grilla de 2 o 3 columnas, tarjetas con destello, valores que se achican sin cortarse, PULSO solo con el pulso del monitor, ceros al reconectar, rodillo que termina en el valor real | 10 de 11; **1 falla** (AC-BLE-11-02, §5.1) | Dispositivo |
| F-BLE-13 (+ F-APP-18) | **Modo simulador**: se activa solo en desarrollo; un build con simulador que no sea de desarrollo no se genera; el build normal sin código ni textos del simulador; las pantallas no saben si los equipos son simulados | 5 de 5 (1 con parte de equipo pendiente) | Build normal y build de simulador |
| F-BLE-15 | **Remo simulado**: conectado desde el arranque, una lectura por segundo con pulso 0, reconexión instantánea, caída de 3 s, la física no se reinicia al volver | 10 de 10 | Dispositivo |
| F-BLE-16 | **Panel del simulador**: cinta `SIMULADOR`, llave, panel de 280 dp, presets, ± vatios y ritmo con topes, `Hora escena` y `Hora real`, `Remando`, las dos fallas; nada se guarda | 12 de 12 | Llave, abajo a la derecha |
| F-HRM-04 | **Pulso en vivo**: cada lectura tal cual, `--` sin lectura, 0 al cortarse una sola vez, independiente del remo | 5 de 5 | Tarjeta del pulsómetro |
| F-HRM-12 | **Estado de la tarjeta**: nombre mientras conecta, errores (incompatible u otro), búsqueda que arranca limpia | 6 de 6 | Tarjeta del pulsómetro |
| F-HRM-13 | **Tarjeta "Pulsómetro"**: los 6 estados, botones habilitados o no, fila de la lista, error en 3 líneas, búsquedas que se excluyen, 640 × 360 sin desbordes, corazón que late | 12 de 12 | Dispositivo |
| F-HRM-15 | **Pulsómetro simulado**: una sola fila `Pulsómetro simulado` / `SIM:HRM`, búsqueda que vence a los 10 s, pulso que sale de la física del remo (`142` a los 60 s), caída de 3 s con `reconectando...`, `Olvidar` y `Buscar otro` | 12 de 12 | Tarjeta del pulsómetro |
| De la It. 1 (12) | Textos de la grilla en pantalla, idioma en pantalla y en vivo, nombre y carpeta de datos en la PC, paridad numérica dentro de Unity, medición del vidrio (R-T1), pruebas dentro de Unity, proyecto que abre sin errores | 9 de 12 (5 con parte de equipo pendiente); 3 no verificables | — |

Además:

- **Los 15 casos de prueba del legacy** están portados 1 a 1, con su nombre original, y pasan: `simulated_ble_service_test` 3, `simulated_heart_rate_service_test` 3, `simulator_overlay_test` 2, `heart_rate_card_test` 4 y `heart_rate_provider_test` 3.
- **Herramientas para el equipo**:
  - arnés de capturas;
  - scripts de build de Windows y Android;
  - el inspector que revisa el build normal en busca de restos del simulador;
  - el freno que impide un build de distribución con simulador;
  - la preparación automática del proyecto (versión 3), que arma la escena `Boot`, las fuentes y el panel.

---

## 3. Cómo ver la app

### 3.1 En Unity, con el simulador (lo más rápido)

Antes de abrir el Editor, verificá que no esté corriendo ninguno de los scripts de §3.3 a §3.5: Unity admite una sola instancia por proyecto.

1. **Abrí el proyecto.** En Unity Hub: *Projects* → *Add* → *Add project from disk* → `<repo>`. La Hub tiene que mostrar el Editor **6000.3.27f1**. Abrilo. Con la caché, tarda menos de un minuto.
2. **Prendé el modo simulador.** Menú **RowMate → Modo simulador**: queda con una tilde y Unity recompila unos segundos.
3. **Abrí la escena.** En la ventana *Project*, `Assets/RowMate/Scenes/Boot.unity` (doble clic). Es la única escena de la app.
4. **Elegí el tamaño en la vista *Game*.** Con la escala de Windows al 100 %, 1 px = 1 dp. Algunos tamaños útiles (con "+" agregás los que falten):
   - `1280x720`: la ventana de Windows, con 3 columnas.
   - `411 x 914`: un teléfono vertical, con 2 columnas.
   - `640 x 360`: el caso del test legacy, con 2 columnas.
   - `800 x 360`: el panel en una ventana baja.
5. **Dale Play.** Tenés que ver `Remo simulado` + `Conectado` y la grilla moviéndose cada segundo. Fuera de las pruebas el simulador tiene ruido, como el legacy (±5 % en vatios y ±1 spm), así que vas a ver valores como `24.5` o `145`. Los valores exactos de la spec (`02:13`, `24.0`, `150`) son los de las pruebas y las capturas, que usan el simulador sin ruido.
6. **Pulsómetro.** Bajá hasta la tarjeta `PULSÓMETRO` con la rueda del mouse (en 1280 × 720 queda debajo de la barra, como en el legacy).
   - `Buscar` → aparece `Pulsómetro simulado` → tocá la fila → `--` → al segundo aparece el pulso y el corazón empieza a latir.
   - `Desconectar` → `Sin sensor`.
   - Si no tocás la fila, a los 10 s la tarjeta vuelve sola a `Sin sensor`.
7. **Panel (la llave, abajo a la derecha).**
   - `Suave`, `Medio` y `Fuerte` cambian los números en la lectura siguiente.
   - − y + mueven los vatios entre 30 y 500 (de a 10) y el ritmo entre 14 y 40.
   - `Hora escena` ± 1 h y `Hora real`: todavía no hay escena, así que solo cambia el valor en el panel (el efecto llega en la 6a).
   - Con `Remando` apagado se ve `--:--`, `0.0` y `0`, y la distancia queda congelada.
   - `Simular desconexión`: 3 s de `No se encontraron dispositivos`, después vuelve la vista conectada en ceros y al segundo los valores, con la distancia siguiendo desde donde iba. Durante un instante se ve `Rower`: es QA-V2-01.
   - `Simular caída del pulsómetro`, con el pulsómetro conectado: 3 s de `Pulsómetro simulado · reconectando...` y vuelve.
8. **Pestañas.** Andá a Workout, Rutinas o Historial (título e ícono apagado) y volvé: Dispositivo sigue donde estaba y la distancia siguió sumando.
9. **Teclado.** `Tab` recorre los botones con un anillo de foco, y `Enter` o `Espacio` los activa.
10. **Idioma, letra grande y "reducir movimiento".** Están en el menú **RowMate → Simular** (`Idioma` → `en-US` / `es-AR` / `Del sistema`, `Letra 1,3×`, `Reducir movimiento`).
    - La app los lee al arrancar y al recuperar el foco: elegí la opción y cambiá a otra ventana de Windows y volvé, o pará y volvé a darle Play.
    - En inglés cambia todo salvo los textos del simulador, que quedan en español a propósito (Q-03).
11. **Sin el modo simulador.** Sacale la tilde a **RowMate → Modo simulador** y dale Play: ves lo mismo que el build normal. Es la vista de búsqueda vacía, `Buscar dispositivos` no hace nada (todavía no hay Bluetooth, llega en la 10a) y el pulsómetro está en `Sin sensor`.

**Antes de cerrar Unity, dejá `RowMate → Modo simulador` sin tilde.** Si queda puesto, se guarda en la configuración del proyecto, y el control del CI falla a propósito.

Si después de abrir o de compilar `git status` muestra cambios en las fuentes (`UI/Fonts/Generated/*.asset`) o en los assets de URP, no rompiste nada. Es QA-V2-07, que se ordena en la It. 3.

### 3.2 Los builds de Windows

Con el Editor **cerrado**, desde `<repo>`:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
Builds\Windows\simulator\RowMate.exe
```

Es la demo como app de Windows. Abajo a la derecha se ve la marca "Development Build" de Unity: con simulador el build tiene que ser de desarrollo, y la marca se acepta solo ahí (P-01).

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows
Builds\Windows\normal\RowMate.exe
```

Es el build normal. Fijate en esto:

- No tiene cinta ni llave.
- La ventana se titula `RowMate` y mide 1280 × 720.
- Muestra la vista de búsqueda vacía y la tarjeta en `Sin sensor`.
- Sale en el primer idioma soportado de tu lista de Windows (en la PC de QA, `en-US, es-ES` → inglés).
- Sus datos quedan en `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`. En `Player.log` está la línea `[APP] RowMate 1.0.0 (1) · WindowsPlayer · simulador=no`.

El script termina con 0 si el build salió bien. Revisa solo que el build normal no tenga restos del simulador (si los encuentra, sale con 2).

No uses `-Backend IL2CPP` hasta tener el módulo: hoy falla con 1 (QA-V2-03). Si un build falla, borrá `Assets/Resources/PerformanceTestRun*` si quedó creado (QA-V2-02).

### 3.3 Las pruebas sin Unity (.NET 8)

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Al final aparecen dos líneas `Passed!`: el simulador con 149 que pasan, y el resto con 1098 que pasan y 17 ignoradas. Si ninguna dice `Failed!`, todo está verde.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los 286 IDs (los 158 de la It. 1 y los 128 de la It. 2). Sin los resultados de Unity, 31 quedan "PENDIENTE UNITY". Si antes corriste las pruebas de §3.4, agregá `-UnityResults tools/unity/.artifacts` y la matriz queda en **285 PASA, 0 FALLA y 1 IGNORADO** (AC-APP-09-07, que es de la It. 3).

Para correr un criterio o un caso legacy: `-Filter "TestCategory=AC-BLE-15-09"` o `-Filter "TestCategory=LT-21"`.

### 3.4 Las pruebas dentro de Unity

Con el Editor **cerrado** (si está abierto, el script sale con 10 y no toca nada):

```
powershell -NoProfile -File tools/unity/run-unity-tests.ps1
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator
```

Resultados esperados:

| Comando | Total | Pasan | Fallan | Ignoradas |
|---|---|---|---|---|
| EditMode | 1132 | 1116 | 0 | 16 |
| EditMode `-Simulator` | 1281 | 1265 | 0 | 16 |
| PlayMode | 53 | 53 | 0 | 0 |
| PlayMode `-Simulator` | 85 | 85 | 0 | 0 |

PlayMode `-Simulator` incluye los casos legacy de UI, LT-26 a LT-31. Con `-Simulator`, el script prende el modo simulador solo durante la corrida y siempre lo deja como estaba. Para un caso en particular: `-Platform PlayMode -Simulator -Category LT-28`. Desde el Editor también se puede: *Window → General → Test Runner*.

### 3.5 Las capturas

Están en [snapshots/](snapshots/), y la tabla completa, con estado, sondas y auditoría de cada toma, en [snapshots/INDEX.md](snapshots/INDEX.md). Las renderiza Unity con la interfaz real; no son maquetas. Para regenerarlas (Editor cerrado, entre 2 y 4 minutos):

```
powershell -NoProfile -File tools/unity/capture-snapshots.ps1
```

Hay un detalle que no es un error: la cinta `SIMULADOR` tapa la primera letra del título (`ispositivo`), exactamente como en el legacy (`docs/image-01.png` dice `evice`).

**Dispositivo con el remo simulado** (teléfono vertical: inglés y búsqueda vacía):

| CAP-01 · conectado, 60 s | CAP-02 · en inglés | CAP-08 · remo desconectado |
|---|---|---|
| ![CAP-01](snapshots/CAP-01-dispositivo-tv.png) | ![CAP-02](snapshots/CAP-02-dispositivo-tv-en.png) | ![CAP-08](snapshots/CAP-08-busqueda-tv.png) |

| CAP-06 · PULSO del monitor (remo de prueba) | CAP-07 · valores largos achicados | CAP-24 · letra 1,3× |
|---|---|---|
| ![CAP-06](snapshots/CAP-06-grilla-pulso.png) | ![CAP-07](snapshots/CAP-07-valores-largos.png) | ![CAP-24](snapshots/CAP-24-letra-130.png) |

**Ventana de Windows (1280 × 720, 3 columnas)**: sin desplazar, y desplazada hasta el final con el pulsómetro conectado:

![CAP-04a](snapshots/CAP-04a-dispositivo-w1280.png)

![CAP-04b](snapshots/CAP-04b-dispositivo-w1280-hrm.png)

**Horizontal chico (640 × 360)**: 2 columnas, y el pulsómetro buscando sin desbordes:

![CAP-03](snapshots/CAP-03-dispositivo-l640.png)

![CAP-09](snapshots/CAP-09-busqueda-l640-hrm.png)

**Tarjeta del pulsómetro**:

| Estado | Captura |
|---|---|
| CAP-10 · sin sensor | ![CAP-10](snapshots/CAP-10-tarjeta-sin-sensor.png) |
| CAP-11 · buscando | ![CAP-11](snapshots/CAP-11-tarjeta-buscando.png) |
| CAP-12b · conectado, `142` | ![CAP-12b](snapshots/CAP-12b-tarjeta-conectada-60s.png) |
| CAP-13 · caída, `reconectando...` | ![CAP-13](snapshots/CAP-13-tarjeta-reconectando.png) |
| CAP-14c · sensor incompatible | ![CAP-14c](snapshots/CAP-14c-tarjeta-incompatible.png) |
| CAP-14d · error largo en 3 líneas | ![CAP-14d](snapshots/CAP-14d-tarjeta-error-largo.png) |

Además: CAP-12a (`--` recién conectado), CAP-14a (conectando), CAP-14b (no encontrado) y CAP-15 (`Buscar` deshabilitado con el remo buscando).

**Panel del simulador**:

| CAP-18 · teléfono vertical | CAP-19 · topes y `Remando` apagado |
|---|---|
| ![CAP-18](snapshots/CAP-18-panel-tv.png) | ![CAP-19](snapshots/CAP-19-panel-limites.png) |

En una ventana baja de 800 × 360, el panel recién abierto y desplazado hasta el final (en CAP-17, el renglón `14:00` queda justo arriba, fuera de la foto: ver QA-V2-05 en §7):

![CAP-16](snapshots/CAP-16-panel-p800.png)

![CAP-17](snapshots/CAP-17-panel-p800-final.png)

**Barra y pestañas provisorias**:

| CAP-21a · Workout | CAP-25 · con Strava (prueba): 5 destinos |
|---|---|
| ![CAP-21a](snapshots/CAP-21a-workout.png) | ![CAP-25](snapshots/CAP-25-strava-5-destinos.png) |

Recortes de la barra con cada destino elegido: [CAP-20a](snapshots/CAP-20a-barra-dispositivo.png), [CAP-20b](snapshots/CAP-20b-barra-workout.png), [CAP-20c](snapshots/CAP-20c-barra-rutinas.png), [CAP-20d](snapshots/CAP-20d-barra-historial.png) y [CAP-20e](snapshots/CAP-20e-barra-en.png). También están [CAP-21b](snapshots/CAP-21b-rutinas.png) y [CAP-21c](snapshots/CAP-21c-historial.png), y la vista en teléfono horizontal: [CAP-05](snapshots/CAP-05-dispositivo-th.png).

**Build normal y medición del vidrio**:

![CAP-22](snapshots/CAP-22-build-normal-w1280.png)

![CAP-23](snapshots/CAP-23-medicion-rt1.png)

CAP-23 pone lado a lado la composición de la app y la directa de Unity: la de la app da los colores del legacy (`#2A2F37`).

**Videos** (10, 10 y 8 s, a 30 cuadros por segundo):

- [VID-01 · tablero vivo y corazón a 142](snapshots/VID-01-tablero.mp4)
- [VID-02 · recorrido por las pestañas](snapshots/VID-02-pestanas.mp4)
- [VID-03 · caída del remo y vuelta](snapshots/VID-03-caida-remo.mp4)

### 3.6 Lo que podés verificar vos en la PC (opcional, 5 minutos)

Cierran tres de los no verificables de §5.2 sin tocar nada del proyecto:

1. **AC-APP-16-03.** Abrí el build normal y después la **app legacy**: tiene que abrir con sus rutinas, su historial y su pulsómetro recordado.
2. **AC-APP-05-01.** Con Windows en **modo claro** para las apps (*Configuración → Personalización → Colores*), el build se tiene que ver igual de oscuro.
3. **AC-APP-09-06.** Con los idiomas preferidos de Windows en `Français (France)` y `Español (Argentina)`, en ese orden y sin inglés, el build tiene que abrir **en español**. Con solo `Português (Brasil)`, en inglés.

---

## 4. Resultado de las pruebas

Son los números de la corrida real de QA ([05-qa-verification §1](05-qa-verification.md)):

| Qué | Resultado |
|---|---|
| Compilación (dotnet) | **0 advertencias, 0 errores** |
| Pruebas dotnet | **1264: 1247 pasan, 0 fallan, 17 ignoradas** (simulador 149 de 149; resto 1115, con 1098 que pasan y 17 ignoradas). En la It. 1 eran 912 |
| Abrir el proyecto en Unity | Código **0** en 22 s, sin `error CS`; la preparación automática dice `versión 3 ya aplicada: sin cambios`; `ProjectVersion.txt` = `6000.3.27f1` |
| Unity EditMode | **1132: 1116 pasan, 0 fallan, 16 ignoradas** |
| Unity EditMode con simulador | **1281: 1265 pasan, 0 fallan, 16 ignoradas, 0 inconclusos**. El modo simulador quedó apagado después: la configuración tiene el mismo SHA-1 antes y después |
| Unity PlayMode (interfaz montada) | **53 de 53**; con simulador **85 de 85** (incluye LT-26 a LT-31) |
| Las ignoradas | Son a propósito. Necesitan un teléfono, una Mac, GitHub o una iteración futura (§5.2), o se verifican ahora en PlayMode y su motivo quedó viejo (QA-V2-06). No son fallas |
| Matriz (286 IDs) | **285 pasan, 0 fallan, 1 ignorado (AC-APP-09-07, It. 3), 0 sin prueba** |
| Casos legacy de la It. 2 | **15 de 15**, con el mismo nombre y los mismos valores |
| Capturas | Código 0: **41 tomas** (38 PNG y 3 MP4), **0 con problemas, 0 desbordes**. La corrida de QA salió **idéntica byte a byte** a la anterior. QA miró las 38 PNG una por una; el Designer revisó los videos cuadro a cuadro |
| Vidrio (R-T1) | Arnés: app `#2A2F37` / `#BBC1C8` (legacy `#2A2F37` / `#BAC0C7`), directa `#6A6A6C` / `#DADBDC`. En un **build de Windows recién compilado**: `#2A2F37` / `#BAC1C8`, primer cuadro del color de fondo, 0 cuadros de atraso (12 de 12) y el clic llega al botón |
| Build normal de Windows (Mono) | Correcto. Sin código, ensamblados ni textos del simulador (lo revisaron el inspector y QA a mano, con 10 cadenas en UTF-8 y UTF-16). Ventana `RowMate` de 1280 × 720; datos en `LocalLow\iguisoft\RowMate`; las preferencias del legacy, con el mismo SHA-1 y la misma fecha antes y después |
| Build con simulador (Mono) | Correcto, siempre de desarrollo |
| IL2CPP y Android | **No verificables**: faltan los módulos |
| Legacy | Intacto: `git status` vacío antes y después. Nadie leyó el archivo de credenciales de Strava |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | Con parte de equipo pendiente (entre los que cumplen) | Fallan | No verificables |
|---|---|---|---|---|---|
| APP (arranque, barra, tema, paleta, descarte, CI) | 30 | 27 | 7 | 0 | 3 |
| BLE (Dispositivo, remo simulado, modo y panel) | 48 | 47 | 1 | **1** | 0 |
| HRM (pulso, estado, tarjeta, pulsómetro simulado) | 35 | 35 | 0 | 0 | 0 |
| De la It. 1, en pantalla o en equipo | 12 | 9 | 5 | 0 | 3 |
| **Total** | **125** | **118** | **13** | **1** | **6** |

### 5.1 El que falla

| Criterio | Qué pasa | Decisión del PO |
|---|---|---|
| **AC-BLE-11-02** · Cabecera | Cuando se desconecta el remo (con `Desconectar` o con `Simular desconexión`), la vista conectada tarda 120 ms en irse. Mientras se va, la cabecera dice `Rower` + `Conectado` en lugar de `Remo simulado`. Con el simulador el legacy nunca muestra `Rower` (QA-V2-01; se ve en 4 cuadros de VID-03) | **No frena la It. 2.** Dura menos que un parpadeo, no cambia ningún dato ni lo que queda en pantalla, y el arreglo es chico. Pero es paridad: **el criterio queda reabierto** y se corrige como primera tarea de la It. 3, con la prueba PlayMode que propone QA (60 ms después de desconectar, el nombre sigue siendo `Remo simulado`) |

### 5.2 Los 6 no verificables y por qué

En todos, QA revisó y aprobó la parte que se puede revisar sin el equipo.

| Criterio | Qué falta ver | Por qué no se pudo | Cuándo |
|---|---|---|---|
| AC-APP-02-06 | "Atrás" de Android: en 8 a 11 cierra la app; en 12 o más nuevo la deja en segundo plano | Falta el módulo de Android y un teléfono (la regla está probada) | Cuando haya módulo y teléfono; a más tardar en la 6a |
| AC-APP-05-01 | Que con Windows o Android en modo claro la app se vea igual | QA no cambió la configuración de tu Windows | Vos, en 1 minuto (§3.6), o en la It. 14 |
| AC-APP-09-06 | Que la app tome la lista entera de idiomas del sistema (`fr-FR, es-AR` → español) | Las listas de la spec no están configuradas en la PC | Vos (§3.6), o con el teléfono |
| AC-APP-16-03 | Que la app legacy siga abriendo con todo después de instalar la nueva | QA comprobó que la nueva no toca las preferencias del legacy, pero no abrió la legacy. No hay teléfono | Vos (§3.6); en Android, con el teléfono |
| AC-APP-16-04 | Android 8.0 como mínimo y solo 64 bits | Falta el módulo de Android | Cuando esté el módulo |
| AC-APP-19-05 | CI de Unity en GitHub | No hay repositorio ni licencia cargada. Además `ci-unity.yml` no es válido (QA-V2-14) | Cuando subas el proyecto (P-03 / P-08), después de corregirlo |

### 5.3 Cumplen, pero falta la parte de equipo

Pasan en el Editor y en la PC. La parte que se nombra queda no verificable y, si falla al verla, el criterio se reabre.

| Criterio | Parte pendiente |
|---|---|
| AC-APP-01-06, AC-APP-01-07 | Sin marcas de depuración y sin destellos al arrancar, en Android |
| AC-APP-02-07 | Rotar un teléfono y redimensionar a mano la ventana real (en PlayMode se probó cambiando el tamaño) |
| AC-APP-05-05, AC-APP-05-08 | Letra grande y "reducir movimiento" configurados en Android |
| AC-APP-05-07 | Tab, Enter y Espacio dentro del build de Windows (en PlayMode pasan) |
| AC-APP-05-09 | Escala de Windows al 150 % y densidad de Android (al 100 % se verificó: 1 dp = 1 px) |
| AC-BLE-13-05 | Tocar `Buscar dispositivos` dentro del build normal (en PlayMode no hace nada, como debe) |
| AC-APP-09-04, AC-APP-09-05 | Cambiar el idioma de Windows entre dos aperturas, y cambiarlo en vivo en Android |
| AC-APP-16-01, AC-APP-16-02 | Nombre, versión y paquete en Android. En Windows, la versión `1.0.0` está en la primera línea del registro: el `.exe` trae la versión de Unity (P-06 de la spec) |
| AC-BASE-01 | La misma paridad numérica en un build IL2CPP de Windows y de Android |

### 5.4 Cumplen, y se completan más adelante

| Criterio | Qué falta | Cuándo |
|---|---|---|
| AC-BLE-16-07 (F-SCN-23) | Que la hora forzada cambie la luz de la escena | It. 6a |
| AC-BLE-11-02 | El nombre `Rower` con un remo real sin nombre (y QA-V2-01) | It. 10a (y QA-V2-01 en la 3) |
| AC-BLE-13-05 | El build normal "sin Bluetooth" es provisorio | It. 10a (remo) y 11 (pulsómetro) |
| AC-HRM-13-08, LT-29 a LT-31 | Exclusión de búsquedas contra el remo real | It. 10a |
| AC-HRM-04-02 | Regla de "sin contacto" con un sensor real | It. 11 |
| AC-APP-02-02 | Perfil con la configuración real de Strava | It. 12 |
| AC-APP-02-08 | Contenido real de Rutinas, Workout, Historial y Perfil | It. 3, 4a, 9 y 12 |
| AC-APP-06-01 | Paleta en el HUD (tiempo y calorías incluidos) | It. 4a |
| AC-BLE-16-01 | La cinta por encima de pantallas apiladas, diálogos y el entrenamiento | It. 3 y 4a |
| AC-APP-09-07 | Teclado en el idioma del sistema (P-06, aceptada) | It. 3 (primer campo de texto) |

---

## 6. Desvíos aceptados

### 6.1 Diferencias visibles con el legacy, a propósito

Ninguna cambia un número ni una regla. Las aceptó el PO en el gate. Si alguna no te gusta, decilo (§8).

| ID | Qué cambia | Por qué |
|---|---|---|
| DV-01 | La cinta `SIMULADOR` tiene texto oscuro (el legacy lo pone blanco) | El blanco sobre naranja no se lee bien (3,2:1); el oscuro da 6,1:1 |
| DV-02 | El borde de color de la tarjeta se enciende un instante cuando llega un dato que cambió | Mejora EXR ("cada dato nuevo se nota"). En reposo está al 60 %, igual que el legacy |
| DV-03 | El ícono y la etiqueta de cada tarjeta van al 100 % del color (el legacy, al 70 %). En el pulso, un rosa un poco más claro | Con el 70 %, los textos chicos no llegan al contraste mínimo. El matiz no cambia |
| DV-04 | La unidad `bpm`, en el rosa del pulso más claro | Ídem |
| DV-05 | `No se encontraron dispositivos` un escalón más claro | La ayuda de abajo sube del 38 % al 54 % (D-ART-03) y no deben quedar iguales |
| DV-06 | "Atrás" en Android 12 o más nuevo deja la app en segundo plano y al volver sigue donde estaba (el legacy la cierra hasta Android 15) | Es lo que hace Android con cualquier app desde la versión 12, y lo que el legacy ya hace en Android 16 |
| DV-07 | Barra de pestañas de 64 dp (el legacy, 80), con la llave 16 dp encima | Diseño de la app; se conserva el aire del legacy |
| DV-08 | La pantalla **se apaga sola** durante la It. 2 (el legacy la mantiene encendida) | Provisorio: "pantalla siempre encendida" llega en la It. 4a |
| D-ART-03 | Texto terciario al 54 % (el legacy, al 38 %) | Contraste mínimo (ya decidido en el plan) |

Diferencias temporales encontradas por QA, que se corrigen (§7):

- QA-V2-08: la barra de título de la ventana de Windows sale clara con el sistema en modo oscuro.
- QA-V2-13: la barra de desplazamiento siempre visible en los teléfonos.
- QA-V2-01: `Rower` durante 120 ms.

### 6.2 Decisiones técnicas con efecto que se puede notar

| ID | Qué | Efecto para vos |
|---|---|---|
| D-01 | La app mezcla los colores translúcidos como el legacy desde ya, y no desde la 4a. Se midió en el arnés y en un build de Windows (ADR 019) | El vidrio y los blancos translúcidos se ven como en el legacy. R-T1 queda cerrado |
| D-19 / P-01 | Sin el logo de Unity al arrancar. La marca "Development Build" aparece solo en los builds con simulador | El build normal no muestra marcas |
| D-16 | Los builds de Windows de la It. 2 son Mono, porque falta el módulo IL2CPP | Ninguno visible. El build IL2CPP se prueba cuando esté el módulo |
| D-25 | El texto técnico de los errores de conexión usa el mensaje del error, sin el prefijo de tipo que agrega Dart | No se ve con el simulador. Se fija con sensores reales en la It. 11 |
| D-26 | El alto máximo del panel descuenta también el margen inferior de los teléfonos con barra de gestos. **Confirmada por el PO al cerrar** | Así la X de cerrar nunca queda tapada. Sin margen (Windows) da lo mismo que la fórmula del legacy |
| D-27 | Los renglones ± del panel usan 64 dp para el valor (el legacy, 72). **Aceptada por el PO** | Solo el panel de desarrollo, más prolijo |
| Diseño | El efecto de "presionado" aparece solo con el puntero o el dedo apoyado; las etiquetas largas de los botones terminan en "…" en lugar de pasar a dos líneas | Ninguno a tamaño normal. En Android se verifica que el dedo active el efecto |

También se aceptaron desvíos internos sin efecto visible: D-02 (vistas armadas en código, sin UXML), D-03 (escala por densidad), D-04 y D-05 (avisos de estado), D-06 (carpeta `snapshots/`), D-07, D-10, D-11, D-13, D-14 (animación con reloj propio), D-15, D-17, D-18, D-20 a D-24 ([02-tech-design §7.1](02-tech-design.md)).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA (todos menores)

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| QA-V2-01 | `Rower` + `Conectado` visibles 120 ms al desconectar el remo | El único criterio que falla (§5.1) | Architect | **Al empezar la It. 3, primera tarea** |
| QA-V2-02 | El build normal lleva dos archivos de configuración de un paquete de pruebas de Unity. Si un build falla, esos archivos quedan en el proyecto y entran en el siguiente | Nada se ve, pero el build normal no tiene que llevar nada de pruebas | Architect | Al empezar la It. 3 |
| QA-V2-03 | Sin el módulo IL2CPP, el script de build dice "falló" en vez de "no verificable", y dos logs se pisan | Confunde al leer el resultado | Architect | It. 3 |
| QA-V2-04 | Una comprobación de color de CAP-04b falla en silencio y el índice dice "ok" (el color está bien: se ve en CAP-12b) | El arnés tiene que avisar | Architect | It. 3 |
| QA-V2-05 | En CAP-17 el renglón `14:00` no entra junto con el final del panel en 800 × 360 | Solo la foto. **El PO acepta la toma así**, y el catálogo tiene que comprobar el valor `14:00` | Architect | It. 3 |
| QA-V2-06 | Algunas pruebas ignoradas todavía dicen "NO VERIFICABLE → It. 2", aunque ya se verifican en PlayMode o quedaron para el equipo | Solo el texto | Architect | It. 3 |
| QA-V2-07 | Cada build o corrida vuelve a escribir las fuentes (hasta 6,8 MB) y dos assets de URP | Ruido en git, de varios MB | Architect | Al empezar la It. 3, antes del primer commit |
| QA-V2-08 | Barra de título de Windows clara con el sistema oscuro (el legacy la pone oscura) | Diferencia visible | Architect | It. 3, si aceptás el valor por defecto (§8) |
| QA-V2-09 | El build normal acepta dos opciones de diagnóstico por línea de comandos (una reemplaza la pantalla y escribe un archivo donde se le pida) | Nada en el uso normal, pero no tienen que estar en lo que se distribuye. **El PO decide: solo en builds de desarrollo** | Architect | Al empezar la It. 3 |
| QA-V2-10 | El inspector del build busca nombres de código, no los textos visibles del simulador (QA los buscó a mano: 0) | Refuerzo del control | Architect | It. 3 |
| QA-V2-11 | El freno "nunca simulador en distribución" depende de cómo se arme el build | Refuerzo de R-01 (una línea de código) | Architect | It. 3 |
| QA-V2-12 | El rosa del pulso sobre el vidrio del HUD da 4,50:1, justo en el mínimo | Ninguno hoy. Importa en el HUD de la 4a | Designer | Antes de la It. 4a |
| QA-V2-13 | En los perfiles de teléfono la barra de desplazamiento está siempre visible (el legacy en Android no la muestra en reposo) | Detalle visual | Architect + Designer | It. 3 |
| QA-V2-14 | `ci-unity.yml` no es válido, corre solo a mano y no incluye las pruebas del simulador | Sin efecto hasta que haya repositorio | Architect | Antes de activar el CI; a más tardar en la 14 |
| QA-V2-15 | El video VID-03 provoca la caída por código y no tocando el botón del panel | El botón ya lo prueba LT-26 | Architect | It. 3 |

### 7.2 Otra deuda

- **Biblias de diseño**: ART_BIBLE y ANIMATION_BIBLE todavía no tienen los cambios de [04-design-notes §11 y §15.6](04-design-notes.md) (presionado con el puntero apoyado, botones sin dos líneas, etc.). Manda el código; es documental. Designer, al empezar la It. 3.
- **Arnés**:
  - Sumar al catálogo las comprobaciones de aspecto que hoy mide un script del Designer.
  - Hacer que la toma falle si una transición queda congelada.
  
  Architect, It. 3.
- **Para la 10a**: cuando lleguen los avisos del Bluetooth real desde otro hilo, el reloj de la app necesita protección (QA §5). Architect, en el diseño de la 10a.
- **Con teléfono**:
  - que el dedo active el "presionado";
  - el costo de copiar la pantalla en un Android de gama baja;
  - el color de la barra de estado de Android.
- **Con dos monitores**: la escala de Windows en monitores con escalas distintas. It. 14.

### 7.3 Riesgos

| ID | Estado |
|---|---|
| R-T1 (vidrio del HUD) | **Cerrado.** Medido en el arnés y en un build: da los colores del legacy |
| R-T2 (primer open) | **Cerrado** ([00-primer-open](00-primer-open.md)): se corrigieron 5 defectos de la It. 1 que solo aparecían dentro de Unity |
| R-T5 (pruebas dentro de Unity) | **Cerrado** |
| R-T12 (avisos del compilador) | **Cerrado** |
| R-T6 (IL2CPP) | **Abierto** hasta tener el módulo |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar. Las decisiones del gate de la It. 1 (Q-01, Q-14, Q-90, R-14 / Q-85, Q-92, P-01, P-04 y P-06) ya las aceptaste.

### 8.1 Gate de la It. 2

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **P-03** | La letra grande del sistema hace crecer los textos de la app hasta 1,3× y no más. El legacy no tiene tope, pero con más el diseño no entra. A 1,3× no se pierde nada (CAP-24) | Se adopta |
| **DV-01 a DV-08** | Las diferencias visibles de §6.1. La que más se nota es DV-06: en Android 12 o más nuevo, "Atrás" deja la app en segundo plano en lugar de cerrarla | Se mantienen todas |
| **QA-V2-08** | Con Windows en modo oscuro, la barra de título de la ventana sale clara, y el legacy la pone oscura | Paridad: se corrige al empezar la It. 3. La alternativa es dejarlo para la It. 14 (ventana de escritorio) |
| **S-1** | *Spike* opcional de una semana corta: leer un pulsómetro real desde la PC, sin Unity, para bajar antes el riesgo del Bluetooth nativo | No se hace; el riesgo se ve en la It. 10a. Si tenés un pulsómetro y querés adelantarlo, decilo |

### 8.2 Decididas por el PO al cerrar (no necesitan tu respuesta)

- **D-26** confirmada, y D-25 y D-27 aceptadas (§6.2).
- **QA-V2-05**: CAP-17 se acepta sin el renglón `14:00` en la foto. El valor lo comprueban las pruebas (LT-26) y el catálogo.
- **QA-V2-09**: las opciones de diagnóstico por línea de comandos solo en builds de desarrollo. El build normal no tiene que tener modos ocultos que cambien la pantalla o escriban archivos.
- **P-01**: la marca "Development Build" solo en los builds con simulador.

### 8.3 Opcional, sin urgencia

- **P-03 de la It. 1 / P-08**: ¿creás un repositorio en GitHub y cargás tu licencia Personal de Unity como secreto? Sin eso no corre el CI. Antes, el Architect corrige `ci-unity.yml` (QA-V2-14).

---

## 9. Prerrequisitos para la It. 3

### 9.1 De tu lado

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| Tu OK a §8.1 | Cerrar el gate | Sí (alcanza con "OK a todo") |
| Módulos **Windows Build Support (IL2CPP)** (con Visual Studio 2022 "Desarrollo de escritorio con C++" y Windows SDK 10.0.22621+) y **Android Build Support** (con OpenJDK y Android SDK & NDK Tools) | Cerrar AC-BASE-01 en IL2CPP, AC-APP-16-04 y las partes Android de §5.3 | No para la It. 3. El teléfono hace falta a más tardar en la 6a |
| Un **Android 8.0+ de 64 bits** con depuración USB | Ídem, más "Atrás", rotación, idioma en vivo y letra grande | No para la It. 3 |
| Las 3 pruebas de §3.6 | Cerrar AC-APP-16-03, -05-01 y -09-06 en Windows | Recomendado |
| Repositorio en GitHub y licencia (§8.3) | CI | No |

La base de datos local de la It. 3 usa un paquete que ya está instalado: no hace falta descargar nada.

### 9.2 Del equipo, al empezar la It. 3

1. **Architect**:
   - Primero QA-V2-01, con su prueba PlayMode. Cierra AC-BLE-11-02.
   - Después QA-V2-02, -07 y -09, antes de cualquier commit o build que se reparta.
   - En el curso de la It. 3, el resto de §7.1: QA-V2-03 a -06, -08 (si lo aceptás), -10, -11, -13 y -15. QA-V2-14, antes de activar el CI.
2. **Designer**: alinear las biblias (§7.2) y dejar resuelto QA-V2-12 antes de la 4a.
3. **PO**: escribir la spec de la It. 3 (`iterations/03-rutinas/01-po-spec.md`), con:
   - el tope de Q-92 (ya aceptado);
   - AC-APP-09-07 (teclado del sistema en el primer campo de texto);
   - la base separada del simulador (F-HIS-20);
   - la cinta `SIMULADOR` por encima de las pantallas apiladas y los diálogos (F-APP-03).
4. **QA**: verificar el arreglo de QA-V2-01 antes de construir encima.

---

## 10. Por qué el PO acepta la It. 2

- **El objetivo se cumple.** La app abre en Dispositivo con el remo simulado conectado y sus métricas en vivo, la tarjeta del pulsómetro funciona de punta a punta, el panel controla intensidad, ritmo, hora y fallas desde cualquier pestaña, y quedaron fijados el tema oscuro, la paleta por métrica, la tipografía y la barra de pestañas. Los 7 puntos de la demo de la spec se pueden mostrar, salvo la parte de Android. Los números, textos, tiempos y reglas son los del legacy, y lo prueban los 15 casos legacy portados y 125 criterios con prueba o captura.
- **La base es confiable.** El proyecto abre sin errores, las mismas pruebas pasan en .NET y dentro de Unity (con y sin simulador), la interfaz real se prueba montada y las capturas son deterministas. El build normal no lleva el simulador y convive con el legacy.
- **Lo que falla es menor y está acotado.** Un nombre equivocado que se ve 120 ms durante una transición (QA-V2-01). No cambia ningún dato y se corrige con una línea y una prueba. Queda como primera tarea de la It. 3, con el criterio reabierto hasta entonces.
- **Los 6 no verificables dependen del equipo**: el módulo de Android, un teléfono, la configuración de tu Windows o GitHub. La spec preveía que quedaran así sin frenar la iteración, y QA no encontró defectos en lo que sí pudo revisar. Tres de ellos los podés cerrar vos en 5 minutos (§3.6).
- **Lo único que falta para cerrar del todo es tu OK** a las decisiones de §8.1.
