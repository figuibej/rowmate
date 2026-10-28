# It. 3 — Rutinas: lista, editor y base local: reporte de cierre

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-28 |
| Estado | **Cerrada, espera tu OK** |
| Veredicto del PO | **ACEPTADA, sin condiciones.** La pestaña Rutinas funciona de punta a punta: armás, corregís y borrás rutinas con pasos sueltos y series ×N con progresiones, y las encontrás iguales cada vez que abrís la app. Los números, los textos y las reglas son los del legacy. Se cumplió la condición de la It. 2: al desconectar el remo ya no aparece `Rower` (AC-BLE-11-02). No falla ningún criterio. Quedan 10 observaciones menores, que se resuelven al empezar la It. 4a o antes de tomar CAP-27 |
| Veredicto de QA | **APROBADO CON OBSERVACIONES** en la ronda 3 ([05-qa-verification.md](05-qa-verification.md)): 0 problemas graves, 0 mayores y 10 menores. Los dos mayores que aparecieron, ambos de la herramienta de CAP-27, se cerraron en las rondas 2 y 3 |
| Documentos | [01-po-spec](01-po-spec.md) (qué se pidió) · [02-tech-design](02-tech-design.md) (cómo se hizo) · [03-qa-harmony](03-qa-harmony.md) (control previo) · [04-design-notes](04-design-notes.md) (aspecto y movimiento) · [05-qa-verification](05-qa-verification.md) (verificación final, en tres rondas) · [00-deuda-it2](00-deuda-it2.md) y [00-deuda-diseno](00-deuda-diseno.md) (deuda de la It. 2) · [snapshots/INDEX.md](snapshots/INDEX.md) (capturas) |

---

## 1. En pocas palabras

La It. 3 convierte **Rutinas** en una pestaña de verdad:

- **La lista.** Con la base vacía dice `No hay rutinas todavía` y `Presioná + para crear una`. Cada tarjeta muestra el nombre, la descripción (si tiene), el resumen (`1 serie · 1 descanso`) y una **mini-barra** con un tramo por paso, en el color de su tipo. El menú `⋮` tiene `Editar` y `Eliminar`, y eliminar pide confirmación con el nombre.
- **El editor.** Pasos sueltos (`+ Paso`) y series (`Serie`, que arranca con Trabajo 2m + Descanso 1m ×3). Cada fila lleva el color, el ícono y el subtítulo exactos del legacy, por ejemplo `5m  ·  18 spm · 📈 SPM +2/rep`, con el 📈 dibujado como ícono. El chip `×N` abre `Repeticiones`. `Guardar` avisa si falta el nombre o los pasos.
- **El diálogo de paso.**
  - Los 4 tipos se ven a la vez.
  - El paso va por tiempo o por distancia, con objetivos y, dentro de una serie, con progresiones.
  - Los números se leen con la regla del legacy: `0150` vale 150, `abc` cuenta como vacío y `0x10` vale 16.
  - Mientras escribís, muestra cómo va a quedar la fila.
  - Un número imposible queda marcado en rojo debajo del campo, sin cerrar nada (Q-92). Por ejemplo, `10000` W, o una progresión que en la 6.ª repetición pasa de 999 spm.
- **La base propia.** Se llama `rower_app.db` y usa el formato v8: las 4 tablas del legacy, más el origen de la sesión y un índice.
  - Con el simulador se usa otra base, `rower_app_dev.db`, y las dos nunca se mezclan.
  - La base del legacy no se abre ni se toca.
  - Guardar es "todo o nada": una rutina nunca queda a medias, y lo guardado sobrevive a un cierre forzado inmediato.
- **Pantallas apiladas, diálogos, menús, avisos y ayudas**, como en el legacy:
  - el editor tapa la barra de pestañas;
  - "Atrás" y Escape cierran lo de arriba, pero Escape no cierra el editor;
  - los avisos duran 4 s;
  - la capa del simulador queda siempre encima, y en Rutinas la llave sube 16 dp por encima del `+` para no taparlo (DV-13).

Se comporta como el legacy:

- los mismos textos, en español y en inglés;
- los mismos subtítulos, al carácter (incluidas las 11 filas de casos borde);
- las mismas reglas para leer números, tiempos, distancias, splits y repeticiones;
- el mismo orden en la lista.

Las diferencias a propósito están en §6. Lo que el legacy hace mal y ya estaba decidido corregir, quedó corregido:

- un doble toque en `Guardar` crea **una** rutina (Q-19);
- un error de la base se explica y se puede reintentar; nunca queda un indicador de carga eterno (Q-13);
- el orden de la lista no se rompe si cambiás de zona horaria (Q-18).

Encima se sumaron las mejoras EXR pedidas:

- La **mini-barra** de cada tarjeta: la "silueta" de la rutina remada, para reconocerla de un vistazo.
- La **vista previa** del paso mientras escribís.
- Lo nuevo **crece**, y lo borrado **se desvanece y cierra el hueco**. Falta pulir un caso: el último bloque (QA-V3-02).
- El editor **entra desde la derecha** con la lista oscureciéndose detrás, y los diálogos **crecen** apenas sobre un velo.
- Los errores aparecen **debajo del campo** que los causa, y los avisos no se acumulan.

Además:

- La **condición de la It. 2** está cumplida, y las **15 observaciones de la It. 2** quedaron resueltas. Entre ellas, la barra de título de Windows, que ahora es oscura cuando el sistema está en modo oscuro (QA-V2-08).
- Hay **1599 pruebas de la lógica** (eran 1264), que también corren dentro de Unity. Las pruebas de la interfaz montada son **188**, y **235** con el simulador (eran 53 y 85).
- Las **44 capturas y los 3 videos** de esta iteración los genera Unity solo, con la interfaz real y una base de prueba. Incluyen la demo toma por toma, y si se vuelven a generar salen idénticos byte a byte.
- Ninguna prueba ni captura tocó tus bases ni las del legacy: QA las comparó con SHA-256 antes y después.

Lo que no se pudo ver:

- **Android y el build IL2CPP**: faltan los módulos.
- **La ventana real del build, manejada con mouse y teclado.** Había avisos del Firewall de Windows abiertos en tu pantalla que retienen la entrada. La cubren las capturas de la demo, las pruebas de la interfaz y el build manejado sin interfaz, pero conviene que la veas vos: son 5 minutos (§3.1).
- **Que un lector de pantalla lea la app**: queda para la It. 14 (DT-05).

---

## 2. Qué se entregó, por funcionalidad

"Cumple" quiere decir que pasa todo lo que se pudo verificar. "Con parte pendiente" quiere decir que cumple en el Editor y en la PC, pero falta verlo en la ventana real, en Android o con un lector de pantalla (§5.3). "No verificable" está en §5.2.

| ID | Qué quedó listo | Criterios | Dónde se ve |
|---|---|---|---|
| F-RUT-08 (+ F-HIS-18) | **Guardar rutinas y pasos**. Alta. Edición, que reemplaza los pasos y conserva la fecha de creación. Baja, también de sus pasos (Q-17). Lectura por id. Orden "la más nueva arriba", aunque cambie la zona horaria (Q-18). Las series se reconocen al volver. El guardado es todo o nada y sobrevive a un cierre forzado. Un tipo desconocido o un número imposible cargados a mano dan error de carga, sin tocar la base | 9 de 9 (1 con parte pendiente: la ventana real) | Al cerrar y abrir la app |
| F-RUT-09 | **Colección de rutinas**. Se cargan al abrir la app, aunque no vayas a Rutinas. La nueva va arriba, la editada queda en su lugar y la borrada desaparece, sin recargar. Un toque en `Guardar` crea una sola rutina (Q-19). Los errores de carga, guardado y borrado dan un aviso o `Reintentar` (Q-13) | 6 de 6 | Rutinas |
| F-RUT-10 | **Pantalla Rutinas**. Título sin acciones y `+`. Indicador de carga con 120 ms de gracia. Vacío legible. Tarjetas con nombre, descripción, resumen en es/en y mini-barra (mejora EXR). Menú `⋮` con `Editar` y `Eliminar` y la ayuda `Mostrar menú`. Diálogo de eliminar con el nombre. Con 30 rutinas, la lista se desplaza y conserva el lugar. El texto del usuario se ve tal cual | 11 de 11 | Rutinas |
| F-RUT-11 | **Editor**. `Nueva rutina` / `Editar rutina` y `Guardar`. Nombre de una línea y descripción de 2. `Pasos (N)`, con la cuenta de la plantilla. Vacío. Validaciones en orden (nombre, pasos, fuera de rango) y recorte. Sin unicidad ni largo máximo. Volver descarta sin preguntar. Edición de texto en la PC: selección, atajos y menú del clic derecho en el idioma de la app | 9 de 10 (2 con parte pendiente: "Atrás" de Android y la ventana real); 1 no verificable (el teclado de Android, AC-RUT-11-10) | Editor |
| F-RUT-12 | **Pasos sueltos**. Se agregan al final, aunque lo último sea una serie. Cada fila tiene el borde y el cuadrado del color del tipo, título e ícono legibles, el subtítulo exacto (`␣␣·␣␣` y el `📈` como ícono), el lápiz y la cruz. Se editan en su lugar y se borran al instante. No se pueden reordenar | 6 de 6 | Editor |
| F-RUT-13 | **Diálogo de paso**. Los 4 tipos en mosaicos. Por tiempo o por distancia. Objetivos. Progresiones solo en una serie, con sus ayudas siempre a la vista. Los números se leen como en el legacy. Tiempo, distancia y split con sus reglas. Precarga, también con negativos. Topes por campo y por repetición (Q-92), con el error debajo del campo. Vista previa en vivo (mejora EXR). En una ventana baja, el contenido se desplaza y los botones quedan fijos | 16 de 16 (1 con parte pendiente: el teclado numérico del teléfono) | Diálogo de paso |
| F-RUT-14 | **Series**. `Serie` agrega Trabajo 2m + Descanso 1m ×3. El bloque tiene encabezado, riel continuo, pie y "hojas" que dejan intuir el `×N`. Chip `×N` con área táctil de 48 dp. Diálogo `Repeticiones` con las reglas del legacy y un tope de 999. Se puede agregar un paso a una serie, editarlo y borrar la serie entera. La serie que se vacía desaparece. Dos series contiguas quedan a 16 dp | 12 de 12 | Editor |
| F-HIS-17 | **Base propia**. `rower_app.db`, en la carpeta de datos de la app. Esquema v8: las 4 tablas del legacy, `origin` y un índice. La base del legacy nunca se abre (Q-11). Una base existente se usa tal cual. Una ilegible o de otra versión muestra el error y no se toca. Cargar y guardar no traban la pantalla | 6 de 6 (2 con parte pendiente: Android y la fluidez a la vista) | Al abrir la app y en la carpeta de datos |
| F-HIS-20 | **Base del simulador**. `rower_app_dev.db`, que nunca se mezcla con la normal. Mismas pantallas y reglas. Ninguna prueba toca tus bases | 4 de 4 (1 con parte pendiente: verlo en la ventana del build normal) | Build de simulador frente al normal |
| F-APP-03 | **Pantallas apiladas, diálogos, menús, avisos y ayudas**. El editor tapa la barra, y al volver todo está donde estaba. "Atrás" y Escape cierran lo de arriba, y Escape no cierra el editor (P-08). Diálogos modales con velo y foco atrapado. Avisos de 4 s que se reemplazan (DV-14). Ayudas en la flecha, el `⋮` y el tacho. La capa del simulador, siempre encima. Transiciones de ANIMATION_BIBLE. Lo que se está cerrando no responde | 8 de 8 (2 con parte pendiente: "Atrás" de Android y el lector de pantalla) | Toda la navegación de Rutinas |
| F-APP-01 (también toca) | Las rutinas se cargan al arrancar, sin demorar Dispositivo | 1 de 1 | Al ir a Rutinas |
| F-APP-05 (también toca) | **Barra de título de Windows** según el modo del sistema (QA-V2-08). En el teléfono, **sin barra de desplazamiento fija** (QA-V2-13) | 2 de 2 (con parte pendiente: el modo claro, el cambio en vivo y Android) | Ventana de Windows; perfiles de teléfono |
| De iteraciones anteriores (11) | AC-BLE-11-02: `Remo simulado` al desconectar, nunca `Rower` (la condición de la It. 2). AC-BLE-16-01: la cinta sobre pantallas apiladas y diálogos. AC-BLE-16-02: la llave sobre el `+` en Rutinas (DV-13). Íconos y subtítulos en el editor. El tipo desconocido, visible. Rutinas deja de ser provisoria. Letra 1,3×, teclado y foco, y "reducir movimiento" en las pantallas nuevas. Menú de texto en el idioma de la app | 11 de 11 (2 con parte pendiente: la ventana real y Android) | — |

Además:

- **No hay tests legacy que portar**: el área no tiene ninguno. Los casos 16 a 21 del inventario quedaron cubiertos por pruebas nuevas, en verde: nombre vacío, sin pasos, `Serie`, repeticiones, agregar a una serie, diálogo con campos vacíos, y crear, editar y borrar.
- **16 claves nuevas** en el catálogo, en español y en inglés (190 por idioma).
- **Herramientas para el equipo**:
  - el arnés de capturas por conjunto (`-Set it3` o `it2`), con 78 sondas de aspecto y una regla que hace fallar la toma si algo no quedó quieto;
  - `capture-cap27.ps1`, para la foto del build normal: solo a pedido, con resguardo de tu base y un ensayo que no toca nada (`-Rehearsal`);
  - el humo de la base en un build (`build.ps1 -DbSmoke`) y la ida y vuelta entre dos procesos (`-rowmate-db-roundtrip`);
  - el inspector del build normal, que ahora también busca los textos del simulador y los restos de pruebas, y un freno dentro del propio código del simulador;
  - una guardia que evita reescribir las fuentes generadas en cada corrida, así no hay ruido en git;
  - `ci-unity.yml` válido, con las suites del simulador.
- **Documentos al día**:
  - ART_BIBLE, ANIMATION_BIBLE, LEGACY_VISUAL_AUDIT, ASSET_SLOTS y ARCHITECTURE;
  - los ADR 007, 008 y 019;
  - al cerrar, el ROADMAP, el inventario de rutinas y FEATURE_INVENTORY ([01-po-spec §17](01-po-spec.md)).
- **Emojis en los nombres**: se guardan tal cual y se ven con un respaldo monocromo (Noto Emoji), sin correr el texto (P-09).

---

## 3. Cómo ver la app

### 3.1 La demo en el build de Windows con simulador (lo más rápido)

El coordinador te deja el build de simulador en la carpeta de prueba: es `RowMate.exe`, con su carpeta `RowMate_Data` al lado. Si preferís generarlo vos, mirá §3.3.

**Antes de empezar, los avisos del Firewall.** Las corridas de QA dejaron en tu pantalla **tres avisos de "Seguridad de Windows" sin contestar**, de las 03:56, 03:57 y 05:20.

- Mientras sigan abiertos, la app no responde: oscurecen la pantalla y retienen el mouse y el teclado.
- Contestá **`Cancelar`** en los tres, y también en el que aparece la primera vez que abrís el build desde una carpeta nueva.
- La app no usa la red: `Cancelar` solo deja bloqueadas las conexiones entrantes de ese `.exe` (QA-V3-08).
- Si el build no responde a los clics, casi seguro hay uno de esos avisos abierto: buscalo en la barra de tareas.

**El idioma.** El build sigue la lista de idiomas de Windows, como el legacy. Si Windows está en inglés, como en esta PC, el build sale en inglés. Por eso, en los pasos, el texto en inglés va entre paréntesis. Para verlo en español:

- en el Editor, elegí el idioma desde el menú (§3.2);
- en el build, poné el español primero en la lista de idiomas de Windows y volvé a abrirlo.

1. **Abrí `RowMate.exe`.** Se abre la ventana `RowMate`, de 1280 × 720.
   - Con Windows en modo oscuro, la barra de título es oscura.
   - Se ve la cinta `SIMULADOR`, y abajo a la derecha la marca "Development Build": con simulador el build siempre es de desarrollo (P-01 de la It. 2).
   - Arranca en Dispositivo, con el remo simulado conectado.
2. **Rutinas** (`Routines`): el ícono apagado, `No hay rutinas todavía` (`No routines yet`) y `Presioná + para crear una` (`Press + to create one`). El `+` se ve entero, y la llave queda 16 dp más arriba, sin taparlo. → DEMO-01
3. `+` → `Nueva rutina` (`New routine`). En `Nombre de la rutina` (`Routine name`) escribí `Escalera SPM`. → DEMO-02
4. `+ Paso` (`Step`) → `Calentamiento` (`Warmup`), `Por tiempo` (`By time`), Min `5` → `Confirmar` (`Confirm`). Aparece `Calentamiento` con `5m`. → DEMO-03
5. `Serie` (`Series`) → aparece un bloque `Serie` `×3` con `Trabajo` (`Work`) `2m` y `Descanso` (`Rest`) `1m`. → DEMO-04
6. Lápiz de `Trabajo` → Min `5` y `SPM objetivo` (`Target SPM`) `18`. Bajá hasta `Progresiones` y escribí `2` en `Incremento SPM (+/rep)` (`SPM increment (+/rep)`). Arriba, la vista previa dice `5m  ·  18 spm · 📈 SPM +2/rep`. → `Confirmar`. → DEMO-05
7. Lápiz de `Descanso` → Min `2` → `Confirmar`: queda `2m`. Después, `+ Paso` → `Enfriamiento` (`Cooldown`), Min `5` → `Confirmar`. Queda **después** del pie de la serie, y la fila dice `Pasos (4)` (`Steps (4)`). → DEMO-06
8. `Guardar` (`Save`). La lista muestra `Escalera SPM` con `1 serie · 1 descanso` (`1 interval · 1 rest`) y la mini-barra de 8 tramos: amarillo, rosa, verde, rosa, verde, rosa, verde y azul. → DEMO-07
9. Tocá la tarjeta → `Editar rutina` (`Edit routine`). En `Descripción (opcional)` (`Description (optional)`) escribí `Sube 2 spm por repetición`. Tocá `×3` → `Cantidad` (`Amount`) `4` → `OK`: queda `×4`. → DEMO-08
10. `Guardar`. La tarjeta sigue en el mismo lugar, ahora con la descripción y 10 tramos. → DEMO-09
11. **Cerrá la ventana y abrí el `.exe` otra vez.** `Escalera SPM` sigue en Rutinas, igual. En el editor se ven `×4`, `18 spm` y `SPM +2/rep`. → DEMO-10
12. *(Opcional)* Abrí el **build normal** (§3.3). Ahí no aparece `Escalera SPM`, porque el simulador usa su propia base.
13. `⋮` → `Eliminar` (`Delete`) → diálogo `Eliminar rutina` / `¿Eliminar "Escalera SPM"?` (`Delete routine` / `Delete "Escalera SPM"?`) → `Eliminar`. La tarjeta desaparece y vuelve el estado vacío. → DEMO-11 y DEMO-12

Si querés probar más, en el mismo build:

- **Texto.** En el nombre:
  - escribí acentos y `ñ` con el teclado (teclas muertas, AltGr), y probá Ctrl+A, C, X y V;
  - con clic derecho se abre el menú `Cortar` / `Copiar` / `Pegar` / `Seleccionar todo` (`Cut` / `Copy` / `Paste` / `Select all`), solo con lo que se puede hacer en ese momento;
  - si llegás a un campo de una línea con Tab, se selecciona todo; con un clic, el cursor va donde tocaste;
  - Enter le saca el foco al campo; no guarda.
- **Teclado.** Tab recorre los controles con un anillo de foco, y Enter o Espacio los activan. Escape cierra diálogos y menús, pero **no** el editor (como el legacy).
- **Avisos.** `Guardar` sin nombre muestra `El nombre no puede estar vacío` (`Name cannot be empty`) durante 4 s. Con tres toques seguidos se ve un solo aviso, que vuelve a contar los 4 s (DV-14).
- **Topes.** En un paso, `W mín` `10000` → `Confirmar`: el campo queda en rojo con `Fuera de rango: hasta ±9999`. En `×3`, `1000` → `Máximo 999`.
- **Doble toque.** Dos toques rápidos en `Guardar` crean una sola rutina.
- **Cierre forzado.** Tocá `Guardar` y, antes de un segundo, terminá el proceso desde el Administrador de tareas. Al abrir, la rutina está completa.

Así cerrás la parte "ventana real" de 6 criterios (§5.3). Si algo no se ve como dice acá, avisá: ese criterio se reabre.

**Tus datos.** El build de simulador guarda las rutinas en `rower_app_dev.db`, dentro de `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`, y el build normal usa `rower_app.db` en la misma carpeta. Para empezar de cero, cerrá la app y borrá ese archivo, junto con `-wal` y `-shm` si están. La app legacy guarda sus datos en otro lugar y la nueva no los toca.

### 3.2 En Unity, con el simulador

Unity admite una sola instancia por proyecto: antes de abrir el Editor, verificá que no esté corriendo ninguno de los scripts de §3.3 a §3.6.

1. **Abrí el proyecto** en Unity Hub, con el Editor **6000.3.27f1**.
2. **Prendé el modo simulador** con el menú **RowMate → Modo simulador**. Queda con una tilde y Unity recompila unos segundos.
3. **Abrí la escena** `Assets/RowMate/Scenes/Boot.unity`.
4. **Elegí el tamaño en la vista *Game*.** Con la escala de Windows al 100 %, 1 px = 1 dp. Sirven:
   - `1280x720`: la ventana de Windows, como la demo;
   - `411 x 914`: un teléfono vertical, como CAP-04 y CAP-10;
   - `640 x 360`: una ventana baja, como CAP-22 y CAP-23.
5. **Dale Play y andá a Rutinas.** Seguí los pasos de §3.1. Para "cerrar y abrir la app", pará y volvé a darle Play.
6. **Idioma, letra grande y "reducir movimiento".** Están en el menú **RowMate → Simular**: `Idioma` → `es-AR` / `en-US` / `Del sistema`, `Letra 1,3×` y `Reducir movimiento`. La app los lee al arrancar y cuando recupera el foco.
7. **Las bases.**
   - Con el modo simulador, Play usa `rower_app_dev.db`, el mismo archivo que el build de simulador: lo que crees en uno aparece en el otro.
   - Sin el modo, usa `rower_app.db`, la misma del build normal en esta PC (como la configuración "RowMate" del legacy).
   - Las pruebas y las capturas usan siempre una carpeta temporal propia.

**Antes de cerrar Unity, dejá `RowMate → Modo simulador` sin tilde.** Si queda puesto, se guarda en la configuración del proyecto y el control del CI falla a propósito.

### 3.3 Los builds de Windows

Con el Editor **cerrado**, desde la carpeta del repositorio:

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows -Simulator
Builds\Windows\simulator\RowMate.exe
```

Es el build de la demo. Con simulador el build siempre es de desarrollo: por eso lleva la marca "Development Build" y la primera vez dispara el aviso del Firewall (contestá `Cancelar`). Con `-DbSmoke` además prueba la base en una carpeta temporal y termina con `[DB] humo: ok`.

```
powershell -NoProfile -File tools/build/build.ps1 -Target Windows
Builds\Windows\normal\RowMate.exe
```

Es el build normal. Fijate en esto:

- No tiene cinta, llave ni marca "Development Build", y la barra de título sigue el modo de Windows.
- La primera vez crea `rower_app.db` vacía en `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`. En `Player.log` aparecen `[APP] base rower_app.db` y `[DB] base rower_app.db creada (v8)`.
- No muestra las rutinas del build de simulador.

El script termina con 0 si el build salió bien. Termina con 2 si el build normal tiene restos del simulador, de diagnósticos o de pruebas, y con 3 si falta un módulo (NO VERIFICABLE). Hoy, `-Backend IL2CPP` y Android terminan con 3.

### 3.4 Las pruebas sin Unity (.NET 8)

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1
```

Al final aparecen dos líneas `Passed!`: el simulador, con 151 que pasan, y el resto, con 1448 pruebas, 1431 que pasan y 17 ignoradas. Si ninguna dice `Failed!`, está todo verde. Las pruebas de la base usan SQLite real sobre archivos temporales y nunca tocan tus bases. La primera vez se bajan de NuGet los paquetes de SQLite, así que hace falta red.

```
powershell -NoProfile -File tools/dotnet/run-tests.ps1 -Matrix
```

Escribe `tools\dotnet\.artifacts\TestResults\ac-matrix.md` con los 377 IDs: 158 de la It. 1, 128 de la It. 2 y 91 nuevos de la It. 3 (los otros 11 de la It. 3 ya estaban). Sin los resultados de Unity, 65 quedan "PENDIENTE UNITY". Si antes corriste §3.5, agregá `-UnityResults tools/unity/.artifacts` y la matriz queda en **377 PASA**, 0 FALLA y 0 IGNORADO.

Para correr un solo criterio: `-Filter "TestCategory=AC-RUT-13-11"`.

### 3.5 Las pruebas dentro de Unity

Con el Editor **cerrado** (si está abierto, el script sale con 10 y no toca nada):

```
powershell -NoProfile -File tools/unity/run-unity-tests.ps1
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Simulator
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode
powershell -NoProfile -File tools/unity/run-unity-tests.ps1 -Platform PlayMode -Simulator
```

| Comando | Total | Pasan | Fallan | Ignoradas |
|---|---|---|---|---|
| EditMode | 1479 | 1463 | 0 | 16 |
| EditMode `-Simulator` | 1630 | 1614 | 0 | 16 |
| PlayMode | 188 | 188 | 0 | 0 |
| PlayMode `-Simulator` | 235 | 235 | 0 | 0 |

Las PlayMode montan la app real con una base de prueba: la lista, el editor, los diálogos, los errores, la reapertura y la capa del simulador.

- En su registro, las líneas `[PRF]` dan los tiempos de la lista de 54 rutinas y del editor de 20 pasos.
- Con `-Simulator`, el script prende el modo simulador solo durante la corrida y lo deja como estaba.
- Para un criterio en particular: `-Platform PlayMode -Category AC-RUT-09-03`.

### 3.6 Las capturas

Están en [snapshots/](snapshots/). La tabla completa, con el estado, las sondas y la auditoría de cada toma, está en [snapshots/INDEX.md](snapshots/INDEX.md). Las renderiza Unity con la interfaz real, una base de prueba y el reloj de prueba; no son maquetas. Para regenerarlas, con el Editor cerrado (alrededor de un minuto):

```
powershell -NoProfile -File tools/unity/capture-snapshots.ps1
```

Con `-Set it2` se regeneran las de la It. 2.

La cinta `SIMULADOR` tapa en parte la primera letra de `Rutinas` y, en el editor, la flecha de volver, como en el legacy (`docs/image-04.png`). La flecha se puede tocar igual.

**La demo, toma por toma** (ventana de 1280 × 720, con la base vacía al empezar):

| DEMO-01 · Rutinas vacía | DEMO-02 · `Nueva rutina` con el nombre |
|---|---|
| ![DEMO-01](snapshots/DEMO-01-rutinas-vacia.png) | ![DEMO-02](snapshots/DEMO-02-nueva-rutina.png) |

| DEMO-03 · calentamiento de 5 min, con la vista previa | DEMO-04 · `Serie` ×3 |
|---|---|
| ![DEMO-03](snapshots/DEMO-03-paso-calentamiento.png) | ![DEMO-04](snapshots/DEMO-04-serie.png) |

| DEMO-05 · `Trabajo`: 5 min, 18 spm y +2/rep | DEMO-06 · `Pasos (4)`, con el enfriamiento después de la serie |
|---|---|
| ![DEMO-05](snapshots/DEMO-05-editar-trabajo.png) | ![DEMO-06](snapshots/DEMO-06-enfriamiento.png) |

| DEMO-07 · guardada, con 8 tramos | DEMO-08 · descripción y `×4` |
|---|---|
| ![DEMO-07](snapshots/DEMO-07-guardada.png) | ![DEMO-08](snapshots/DEMO-08-descripcion-x4.png) |

| DEMO-09 · actualizada, con 10 tramos | DEMO-11 · eliminar, con la cinta y la llave sobre el velo |
|---|---|
| ![DEMO-09](snapshots/DEMO-09-actualizada.png) | ![DEMO-11](snapshots/DEMO-11-eliminar.png) |

Dos tomas no se repiten porque son idénticas byte a byte a otras:

- [DEMO-10](snapshots/DEMO-10-reabierta.png), con la app armada de nuevo sobre la misma base, es igual a DEMO-09;
- [DEMO-12](snapshots/DEMO-12-vacia-otra-vez.png), después de eliminar, es igual a DEMO-01.

Dos detalles vienen del arnés y no de la app: en DEMO-04 y DEMO-06 el nombre aparece seleccionado, y en DEMO-05 no se ven las progresiones (QA-V3-03 y -04). Las progresiones se ven en CAP-15b.

**Rutinas** (teléfono vertical):

| CAP-01 · vacía | CAP-04 · lista | CAP-05 · en inglés |
|---|---|---|
| ![CAP-01](snapshots/CAP-01-rutinas-vacia.png) | ![CAP-04](snapshots/CAP-04-lista.png) | ![CAP-05](snapshots/CAP-05-lista-en.png) |

| CAP-02 · cargando | CAP-03 · error de carga | CAP-07 · menú `⋮` |
|---|---|---|
| ![CAP-02](snapshots/CAP-02-cargando.png) | ![CAP-03](snapshots/CAP-03-error-de-carga.png) | ![CAP-07](snapshots/CAP-07-menu-tarjeta.png) |

| CAP-08 · eliminar | CAP-25a · lista con letra 1,3× | CAP-25b · editor con letra 1,3× |
|---|---|---|
| ![CAP-08](snapshots/CAP-08-eliminar.png) | ![CAP-25a](snapshots/CAP-25a-lista-letra-130.png) | ![CAP-25b](snapshots/CAP-25b-editor-letra-130.png) |

**Editor** (teléfono vertical):

| CAP-09 · rutina nueva | CAP-10 · `Escalera SPM` | CAP-24a · en inglés |
|---|---|---|
| ![CAP-09](snapshots/CAP-09-editor-nuevo.png) | ![CAP-10](snapshots/CAP-10-editor-escalera.png) | ![CAP-24a](snapshots/CAP-24a-editor-en.png) |

| CAP-12a · casos borde | CAP-12b · casos borde, al final | CAP-13 · series contiguas |
|---|---|---|
| ![CAP-12a](snapshots/CAP-12a-casos-borde.png) | ![CAP-12b](snapshots/CAP-12b-casos-borde-final.png) | ![CAP-13](snapshots/CAP-13-series-contiguas.png) |

| CAP-18 · sin nombre | CAP-19 · sin pasos | CAP-28 · no se pudo guardar |
|---|---|---|
| ![CAP-18](snapshots/CAP-18-aviso-nombre.png) | ![CAP-19](snapshots/CAP-19-aviso-sin-pasos.png) | ![CAP-28](snapshots/CAP-28-aviso-guardado.png) |

**Diálogos** (teléfono vertical):

| CAP-14 · nuevo paso | CAP-15a · editar paso | CAP-15b · progresiones |
|---|---|---|
| ![CAP-14](snapshots/CAP-14-nuevo-paso.png) | ![CAP-15a](snapshots/CAP-15a-editar-paso.png) | ![CAP-15b](snapshots/CAP-15b-editar-paso-progresiones.png) |

| CAP-16 · por distancia | CAP-20a · fuera de rango | CAP-20b · fuera de rango en la repetición 6 |
|---|---|---|
| ![CAP-16](snapshots/CAP-16-por-distancia.png) | ![CAP-20a](snapshots/CAP-20a-error-vatios.png) | ![CAP-20b](snapshots/CAP-20b-error-repeticion.png) |

| CAP-17 · repeticiones | CAP-21 · máximo 999 | CAP-24b · en inglés |
|---|---|---|
| ![CAP-17](snapshots/CAP-17-repeticiones.png) | ![CAP-21](snapshots/CAP-21-repeticiones-maximo.png) | ![CAP-24b](snapshots/CAP-24b-dialogo-en.png) |

**Ventana de Windows (1280 × 720)**: el editor, comparable con `docs/image-04.png` del legacy, y el menú del clic derecho:

![CAP-11](snapshots/CAP-11-editor-w1280.png)

![CAP-26](snapshots/CAP-26-menu-texto.png)

**Ventana baja (640 × 360)**: la lista desplazada al final, el editor y el diálogo compacto:

![CAP-06](snapshots/CAP-06-lista-l640-final.png)

![CAP-22](snapshots/CAP-22-editor-l640.png)

![CAP-23](snapshots/CAP-23-dialogo-l640.png)

En CAP-11, CAP-12a y CAP-22, la llave del simulador tapa en reposo una cruz o el botón `Serie`. Se destapan desplazando la pantalla (QA-V3-09, §7.1).

**Videos** (30 cuadros por segundo):

- [VID-01 · editor: paso, serie, cruz y tacho (10 s)](snapshots/VID-01-editor-pasos-series.mp4). Al final, el tacho saca el último bloque de golpe: es QA-V3-02.
- [VID-02 · abrir, volver y eliminar (8 s)](snapshots/VID-02-editar-y-eliminar.mp4).
- [VID-03 · tres `Guardar` sin nombre: un solo aviso, que vuelve a contar (8 s)](snapshots/VID-03-aviso-reemplazado.mp4).
- De la It. 2, regenerado: [VID-03 · caída del remo](../02-dispositivo-simulador/snapshots/VID-03-caida-remo.mp4). La vista que sale dice `Remo simulado`, nunca `Rower`.

**CAP-27 no está.** Es la ventana del build normal con la barra de título oscura. Se toma solo a pedido, porque reemplaza por un rato la base real del build normal, y antes hay que resolver QA-V3-12 (§8.3). La barra oscura ya se verificó en los dos builds.

### 3.7 Lo que podés verificar vos (opcional, 5 minutos más)

Cierran partes que QA no puede ver sin tocar tu configuración:

1. **AC-APP-05-10 y AC-APP-05-01: el modo claro.**
   - Con el build abierto, poné en **Claro** el modo de las apps de Windows (*Configuración → Personalización → Colores*).
   - La barra de título tiene que pasar a clara en 2 s como máximo, o en el acto al volver a la ventana. El contenido de la app tiene que seguir oscuro.
   - Después volvé a Oscuro.
2. **AC-APP-16-03 y AC-HIS-17-03**: después de usar la nueva, abrí la **app legacy**. Tiene que abrir con sus rutinas, su historial y su pulsómetro recordado.
3. **AC-APP-09-06 (de la It. 2)**: con los idiomas preferidos de Windows en `Français (France)` y `Español (Argentina)`, en ese orden y sin inglés, el build tiene que abrir en español.

---

## 4. Resultado de las pruebas

Son los números de la ronda 3 de QA ([05-qa-verification, "Ronda 3"](05-qa-verification.md)):

| Qué | Resultado |
|---|---|
| Compilación (dotnet) | **0 advertencias, 0 errores** |
| Pruebas dotnet | **1599: 1582 pasan, 0 fallan, 17 ignoradas**. El simulador, 151 de 151; el resto, 1448, con 1431 que pasan y 17 ignoradas. Al cerrar la It. 2 eran 1264 |
| Unity EditMode | **1479: 1463 pasan, 0 fallan, 16 ignoradas** (35 s) |
| Unity EditMode con simulador | **1630: 1614 pasan, 0 fallan, 16 ignoradas, 0 inconclusos**. El modo simulador quedó como estaba (55 s) |
| Unity PlayMode (interfaz montada) | **188 de 188**, y con simulador **235 de 235**. En la It. 2 eran 53 y 85 |
| Las ignoradas | Son a propósito: necesitan teléfono, Mac, GitHub o una iteración futura. Tres tienen un motivo viejo (QA-V3-05). No son fallas |
| Matriz (377 IDs) | **377 pasan**, 0 fallan, 0 ignorados y 0 sin prueba. Incluye los 102 de la It. 3 |
| Casos legacy | El área no tiene tests legacy. Los casos 16 a 21 del inventario están cubiertos y en verde |
| Rendimiento | **197 ms** hasta ver 54 rutinas (192 con simulador; el criterio pide menos de 1 s). En un cuadro, la app usa como máximo **7,9 ms**, la lista de 54, sobre un umbral de 16 ms. El editor de 20 pasos usa 1,2 ms, y guardar no frena el cuadro (0,00 ms) |
| Capturas | **47 tomas** (44 PNG y 3 MP4): **0 con problemas, 0 desbordes** y 78 sondas de aspecto dentro de tolerancia. Iguales byte a byte en las tres rondas de QA. QA miró las 44 PNG una por una y los videos cuadro a cuadro, igual que el Designer, y el PO revisó las 44 al cerrar. Las 41 de la It. 2 siguen iguales |
| Persistencia en un build real | En el build de simulador recién compilado, una rutina guardada sigue ahí al abrir otro proceso. Con el proceso matado justo después de guardar (con el archivo sin cerrar), al reabrir la rutina está completa. El humo de la base da ok |
| Esquema v8 | QA lo leyó con una herramienta de SQLite: las 4 tablas son, columna por columna, iguales a las del legacy, más `origin` y el índice. Versión 8 |
| Build normal de Windows (Mono) | Correcto. Crea `rower_app.db` vacía en `LocalLow\iguisoft\RowMate` e ignora la variable de carpeta de pruebas. No tiene rastros del simulador, de diagnósticos ni de pruebas: lo revisaron el inspector y QA a mano, en UTF-8 y en UTF-16. La barra de título es oscura |
| Build con simulador (Mono) | Correcto. Siempre de desarrollo; usa solo `rower_app_dev.db`; barra de título oscura |
| IL2CPP y Android | **No verificables**: faltan los módulos (`build.ps1` sale con 3, "NO VERIFICABLE") |
| Tus datos y los del legacy | Las bases y las preferencias del legacy tienen el mismo SHA-256 y la misma fecha antes y después de todo. Ninguna prueba ni captura creó ni tocó bases en tu carpeta de datos. El repositorio legacy está intacto (`git status` vacío), y nadie leyó el archivo de credenciales de Strava |
| Repositorio público | 0 rutas de la PC en lo versionado, en `INDEX.md` y en `manifest.json` |

---

## 5. Criterios de aceptación

| Área | Criterios | Cumplen | Con parte de equipo pendiente (entre los que cumplen) | Fallan | No verificables |
|---|---|---|---|---|---|
| RUT (lista, editor, diálogo, series, guardado y colección) | 70 | 69 | 4 | 0 | 1 |
| HIS (base local y base del simulador) | 10 | 10 | 3 | 0 | 0 |
| APP (navegación secundaria, arranque y barras) | 11 | 11 | 4 | 0 | 0 |
| De iteraciones anteriores (BLE 3, RUT 3, APP 5) | 11 | 11 | 2 | 0 | 0 |
| **Total** | **102** | **101** | **13** | **0** | **1** |

### 5.1 No falla ninguno, y la condición de la It. 2 está cumplida

AC-BLE-11-02 quedó reabierto al cerrar la It. 2: la cabecera decía `Rower` durante 120 ms al desconectar el remo. Se corrigió como primera tarea de esta iteración y ahora pasa:

- tres pruebas PlayMode, que fallaban antes del arreglo, revisan cuadro por cuadro que la vista que sale siga diciendo `Remo simulado` y `Conectado`;
- en el VID-03 de la It. 2, regenerado, nunca aparece `Rower`.

### 5.2 El no verificable y por qué

| Criterio | Qué falta ver | Por qué no se pudo | Cuándo |
|---|---|---|---|
| AC-RUT-11-10 | En Android: el teclado del sistema edita en el mismo campo, sin la caja extra; el campo con foco queda sobre el teclado, también dentro de un diálogo; el menú de copiar y pegar es el del sistema, en su idioma | Faltan el módulo de Android y un teléfono. La parte que se puede simular pasa | Cuando estén el módulo y el teléfono. Si Android no muestra su menú, ahí se decide la variante de P-06: el menú de la app, con los textos del catálogo |

### 5.3 Cumplen, pero falta una parte de equipo

Pasan en el Editor y en la PC. La parte que se nombra queda no verificable, y si falla al verla, el criterio se reabre.

| Criterio | Parte pendiente | Quién y cuándo |
|---|---|---|
| AC-RUT-08-09 | Crear, cerrar y reabrir con la interfaz, en la ventana real. Ya pasa en el arnés, con la interfaz real, y en el build sin interfaz | Vos, §3.1 |
| AC-RUT-11-09 | Atajos, teclas muertas y AltGr con el teclado de Windows, en la ventana real | Vos, §3.1 |
| AC-APP-05-07 | Tab en la ventana real | Vos, §3.1 |
| AC-APP-09-07 | El menú de texto en la ventana real (PC) y el teclado del sistema en Android | Vos (PC) y el teléfono |
| AC-HIS-17-06 | La fluidez a la vista en la ventana real. Medido: 197 ms y 7,9 ms | Vos, §3.1 |
| AC-HIS-20-02 | Ver en la ventana del build normal que no aparece la rutina del simulador. Cada build abre solo su archivo: se comprobó en el registro y en la carpeta | Vos, §3.1, paso 12 |
| AC-APP-05-10 | El modo claro y el cambio en vivo, porque nunca se cambia tu tema; también CAP-27 | Vos, §3.7, si querés |
| AC-RUT-11-08, AC-APP-03-02 | "Atrás" de Android | El teléfono |
| AC-RUT-13-13 | El teclado numérico con signo en el teléfono | El teléfono |
| AC-HIS-17-01 | La base en el almacenamiento privado de Android | El teléfono |
| AC-APP-05-11 | Sin barra de desplazamiento fija en Android. En los perfiles de teléfono ya pasa | El teléfono |
| AC-APP-03-06 | Que un lector de pantalla lea las ayudas (DT-05) | It. 14 |

### 5.4 Cumplen, y se completan más adelante

| Criterio | Qué falta | Cuándo |
|---|---|---|
| AC-RUT-08-04 | La lectura por id la van a usar el Historial y Strava | It. 9 y 12 |
| F-RUT-09, F-RUT-10 | La misma colección y la misma tarjeta, en la lista de Workout | It. 4a |
| AC-HIS-17-02 | Las tablas de sesiones y puntos empiezan a llenarse; `origin` se usa en la 13 | It. 4a y 13 |
| AC-BLE-16-01 | La cinta por encima del entrenamiento a pantalla completa | It. 4a |
| AC-APP-03-01 a -08 | El mismo patrón en el inmersivo y en el detalle del historial | It. 4a, 4b y 9 |
| AC-APP-02-08 | Workout, Historial y Perfil siguen provisorias | It. 4a, 9 y 12 |
| AC-BLE-11-02 | El nombre `Rower` con un remo real sin nombre | It. 10a |

---

## 6. Desvíos aceptados

### 6.1 Diferencias visibles con el legacy, a propósito

Salvo los topes (DV-17), ninguna cambia un número ni una regla del legacy. El PO aceptó DV-09 a DV-19 en el gate; DV-20 se agregó al cerrar. DV-01 a DV-08 ya las aceptaste en la It. 2. Si alguna no te gusta, decilo (§8).

| ID | Qué cambia | Por qué |
|---|---|---|
| DV-09 | El tipo de paso se elige entre 4 mosaicos a la vista, con ícono y nombre. El legacy usa una lista desplegable. Mismo orden, y `Trabajo` por defecto | Un toque en lugar de dos, y el color del tipo a la vista |
| DV-10 | El `📈` de los subtítulos se dibuja como un ícono de tendencia; el texto que sigue es idéntico | El emoji depende de la fuente del sistema (Q-10) |
| DV-11 | `No hay rutinas todavía` y `Todavía no hay pasos.` se ven más legibles, igual que la ayuda del vacío: 70 % y 54 %. En el legacy, 38 % y 24 % | Contraste mínimo (D-ART-03) |
| DV-12 | Filas, encabezados y chips con las medidas del sistema visual: fila de 56 dp, acciones de 48 y chip de 28. El título y el ícono del trabajo y del enfriamiento, un tono más claro: `#FF5D7F` y `#37A2CB` en lugar de `#EF476F` y `#118AB2`. El número del `×N`, en un cian más claro (`#38CBEF`) | Contraste AA y áreas táctiles de 48. El matiz no cambia: el borde, la mini-barra y los cuadrados siguen en los colores del legacy |
| DV-13 | Con el simulador, en Rutinas la llave sube 16 dp por encima del `+`. En el legacy le tapa la mitad | El `+` es la acción principal de la pantalla |
| DV-14 | Un aviso nuevo reemplaza al que se ve. El legacy los encola: tres toques en `Guardar` dan 12 s de avisos | Que no se acumulen avisos viejos |
| DV-15 | El teclado numérico del teléfono trae el signo menos. En el legacy no, y en iPhone no se podía escribir `-2` | La ayuda del split pide `-2` |
| DV-16 | Los errores de la base se ven: `No se pudieron cargar las rutinas` con `Reintentar`, `No se pudo guardar la rutina` y `No se pudo eliminar la rutina`. Nunca hay un indicador eterno ni rutinas duplicadas | Correcciones ya decididas (Q-13, Q-19) |
| DV-17 | Topes por campo y por repetición (Q-92), y repeticiones hasta 999. Un número de cualquier largo cuenta por su valor | Ningún número que el editor deje guardar puede romper el entrenamiento |
| DV-18 | En el editor, los campos, la fila `Pasos (N)` y la lista se desplazan juntos; en el legacy solo se desplaza la lista. En una rutina larga, para llegar a `+ Paso` y `Serie` hay que volver arriba | En una ventana baja, con letra 1,3× o con el teclado abierto, la parte fija no entra |
| DV-19 | Las ayudas de las progresiones (`Ej: 2`, `Ej: 5`, `Ej: -2 (más rápido)`) se ven siempre que el campo está vacío, no solo con foco | Explican cómo se escribe la progresión antes de tocar el campo |
| **DV-20** (nueva, QA-V3-07) | Al pegar en un campo de una línea, se quitan también el retorno de carro (`\r`) y la tabulación (`\t`); el legacy solo quita el salto de línea (`\n`). Por ejemplo, pegar `Pirámide⏎500` copiado del Bloc de notas da `Pirámide500`. El legacy guarda un `\r` invisible | Evita caracteres invisibles en el nombre. Con texto copiado en Windows, da lo mismo que el legacy con un salto de línea simple |

Diferencias temporales que encontró QA (§7):

- QA-V3-02: el último bloque se va sin colapsar;
- QA-V3-09: con el simulador, la llave tapa en reposo alguna acción del editor.

### 6.2 Decisiones técnicas con efecto que se puede notar

| ID | Qué | Efecto para vos |
|---|---|---|
| DT-07 | Cada guardado espera a que el disco lo confirme (`synchronous = FULL`) | Una rutina guardada no se pierde aunque se corte la luz enseguida. Cuesta unos milisegundos por guardado, que no se notan |
| DT-08 / Q-18 | La fecha de creación se guarda en UTC, con milisegundos | El orden "la más nueva arriba" se mantiene aunque cambies de zona horaria. La fecha no se muestra |
| DT-09, DT-10 | Una base de otra versión (también una del legacy copiada a mano) o con números que no entran en la app muestra el error de carga y no se toca | Nunca se pierde un archivo que la app no entiende |
| DT-11 | `0xFFFFFFFFFFFFFFFF` en un campo es "fuera de rango"; Dart lo lee como −1 | Solo pasa escribiéndolo a propósito |
| DT-13 | Una serie ×0, posible solo tocando la base a mano, no impide guardar; ×1000 o más, sí | El legacy muestra la ×0, y se respeta |
| DT-17 | Con un campo de texto con foco, la app dibuja a 60 cuadros por segundo; en reposo, a 30 | Escribir se siente fluido |
| DT-18 | Un segundo toque en `+` mientras entra el editor se ignora | Nunca se abren dos editores |
| DT-20 | Al cargar se ve el indicador del legacy, con 120 ms de espera y un fundido, y no las tarjetas "esqueleto" de la biblia | Una carga rápida no parpadea |
| DT-21 | Durante los 150 ms en que se cierra un diálogo o un menú, un toque no pasa a lo de atrás. En el legacy, sí pasa. **El PO lo acepta** | Un doble toque en `Confirmar` no puede borrar sin querer un paso del editor de atrás |
| DT-05 | Las ayudas guardan su texto como nombre accesible, pero los lectores de pantalla del sistema todavía no leen la app | Diferencia con el legacy hasta la It. 14 (§8.1) |
| DT-12 | CAP-27 se toma reemplazando la base real del build normal por una sintética y restaurándola después | Solo si la pedís (§8.3) |

También se aplicaron, sin efecto visible:

- DT-01 a DT-04, que pasaron a DV-18, DV-19, DV-13 y P-08;
- DT-06, DT-14, DT-15, DT-16 y DT-19;
- Q-17: al borrar una rutina se borran también sus pasos; en el legacy quedaban huérfanos e invisibles.

Detalle en [02-tech-design §8.1](02-tech-design.md).

---

## 7. Issues abiertos y deuda

### 7.1 Issues de QA (todos menores)

Los dos mayores de la verificación, QA-V3-01 y QA-V3-11 (ambos de la herramienta de CAP-27), quedaron cerrados en las rondas 2 y 3. Quedan 10 menores, y ninguno hace fallar un criterio.

| ID | Qué pasa, en simple | Impacto | Quién | Cuándo |
|---|---|---|---|---|
| QA-V3-02 | Al borrar el último bloque (o el último paso), desaparece de golpe y el texto vacío aparece entero, sin el fundido que tiene el resto. Tampoco se animan todavía las "hojas" de la serie, el cambio de color al elegir el tipo y la línea de error | Detalle de movimiento: no cambia ningún dato ni ningún lugar | Architect, con el visto bueno del Designer | Al empezar la It. 4a |
| QA-V3-03 | En DEMO-05 no se ven las progresiones: el diálogo quedó arriba. Se ven en CAP-15b | Solo la foto | Architect | Al empezar la It. 4a, con -02 y -04 en una sola regeneración de capturas |
| QA-V3-04 | En algunas fotos el cursor queda al principio del texto, y en DEMO-04 y -06 el nombre aparece seleccionado. Viene del arnés: en la app real no pasa | Solo las fotos | Architect | Ídem |
| QA-V3-05 | Tres pruebas ignoradas todavía dicen "NO VERIFICABLE → It. 3", aunque ya se verifican en pantalla | Solo el texto | Architect | It. 4a |
| QA-V3-06 | Una sola vez, el arnés de la It. 2 corrido justo después del de la It. 3 dio diferencias de 1 o 2 tonos en algunos píxeles del borde de las letras. No se repitió | Las comparaciones de capturas: hay que documentarlo o comparar con tolerancia | Architect | It. 4a |
| QA-V3-07 | Los campos de una línea quitan `\r` y `\t`; el legacy, solo `\n` | **El PO lo propone como DV-20** (§6.1, §8.1). Además, hay que corregir el comentario del código, que dice que es la regla del legacy | PO (gate) y Architect | Gate de la It. 3 / It. 4a |
| QA-V3-08 | Cada build de desarrollo nuevo dispara el aviso del Firewall de Windows, y quedaron tres sin contestar en tu pantalla | Impidió manejar la ventana real | Architect: una misma carpeta de salida para los builds de desarrollo, y explicarlo en `tools/unity/README.md`. Vos: contestar `Cancelar` | Ahora (los avisos) y al empezar la It. 4a |
| QA-V3-09 | Con el simulador, la llave tapa en reposo alguna acción del editor: la cruz de la 4.ª fila a 1280 × 720 (se ve en DEMO-06), la de la fila 8 en el teléfono vertical y `Serie` a 640 × 360. Desplazando se destapan | Solo con el simulador, y en el legacy pasa lo mismo. **El PO lo acepta en la It. 3** | Designer | Diseño de la It. 4a, donde la llave va a convivir con el HUD |
| QA-V3-10 | `Guardar`, `Eliminar` y `Reintentar` no registran una falla inesperada que no venga de la base | Hoy no pasa; es robustez | Architect | Al empezar la It. 4a |
| QA-V3-12 | Si se toma CAP-27, la foto saldría con franjas de unos 8 px del escritorio de atrás, y se guardaría directo en `docs/`, que es público | Se podría publicar un pedazo de tu escritorio | Architect | Antes de cualquier toma de CAP-27 |

### 7.2 Otra deuda

- **Para la It. 4a**:
  - **R3-08.** Antes de expandir una rutina al iniciar el entrenamiento, hay que comprobar que respeta los topes. Si no, una base tocada a mano con ×2 000 000 000 agotaría la memoria. Ya está como criterio de la 4a en el ROADMAP, y su spec fija qué ve el usuario.
  - **Kerning** de los títulos grandes y del HUD: revisarlo antes de las capturas de la 4a ([04-design-notes §13](04-design-notes.md)).
- **Deseables, sin criterio (P2)**:
  - `Guardar` muestra un indicador si tarda más de 300 ms;
  - un subtítulo que pasa a dos líneas crece animado;
  - la mini-barra interpola sus tramos.
- **Para la 10a**: el reloj de la app necesita protección cuando lleguen los avisos del Bluetooth real (viene de la It. 2).
- **Para la It. 14**: los lectores de pantalla (DT-05) y la escala de Windows con dos monitores (viene de la It. 2).
- **Con teléfono**:
  - el teclado del sistema, el campo visible sobre el teclado y el menú de texto (AC-RUT-11-10 y R3-13);
  - el "presionado" con el dedo, el costo de la copia de pantalla y la barra de estado (vienen de la It. 2).
- **CI**:
  - `ci-core` ya corre en GitHub, y la prueba nueva de la herramienta de capturas se va a ver en el próximo *push*;
  - `ci-unity` espera la licencia (P-08).

### 7.3 Riesgos

| ID | Estado |
|---|---|
| R3-01 / R-T6 (IL2CPP con el SQLite nativo) | **Abierto** hasta que esté el módulo. En Mono, el humo de la base pasa |
| R3-02 (Android: carpeta privada, teclado y "Atrás") y R3-13 (menú de texto en Android) | **Abiertos** hasta que estén el módulo y el teléfono |
| R3-03 (hilos de la base) | **Mitigado**: la base trabaja en un hilo propio, con cola, y el resultado vuelve al hilo principal. Está probado |
| R3-08 (expandir una rutina imposible) | **Pasa al diseño de la 4a** |
| R3-09 (armar listas largas en un cuadro) | **Cerrado**: 54 rutinas en 197 ms, y como máximo 7,9 ms por cuadro sobre un umbral de 16 ms |

---

## 8. Decisiones que tenés que confirmar

Si no decís nada, se aplica lo que dice "Por defecto". Alcanza con un "OK a todo", o con decir cuál querés cambiar. Las decisiones de los gates de la It. 1 y la It. 2 ya las aceptaste.

### 8.1 Gate de la It. 3

| Decisión | Qué significa | Por defecto |
|---|---|---|
| **Q-11 / R-14** | La app nueva no importa las rutinas ni el historial de la app vieja: arranca con su base propia vacía, y las dos apps conviven sin tocarse. La importación queda diseñada, sin hacer. En Android no sería posible, porque las dos apps tienen distinto identificador | No se importa |
| **P-01** (Q-26 ajustada por Q-92) | Las repeticiones de una serie van de 1 a 999. El legacy no tiene tope, pero una serie ×2 000 000 000 traba el inicio del entrenamiento | Se adopta |
| **P-02** (los números de Q-92, que ya aceptaste como criterio) | Los topes son de 5 a 100 veces el uso real (CAP-20a, CAP-20b y CAP-21). Para los valores del paso: duración 99:59:59, distancia 999 999 m, vatios ±9 999, SPM ±999 y split 99:59. Para las progresiones: ±999 spm, ±9 999 W y ±5 999 s. Los topes valen también para cada repetición ya progresada | Se adoptan |
| **DV-09 a DV-20** | Las diferencias visibles de §6.1. Las que más se notan: los mosaicos de tipo (DV-09), la llave sobre el `+` (DV-13), los avisos que se reemplazan (DV-14) y el desplazamiento único del editor (DV-18). DV-20 es nueva | Se mantienen todas |
| **DT-05** (informativo) | Los lectores de pantalla del sistema todavía no leen la app; el legacy, sí. Las ayudas ya guardan su texto para cuando se pueda | Pasa a la It. 14, donde ya figura como criterio en el ROADMAP |

### 8.2 Decididas por el PO al cerrar (no necesitan tu respuesta)

- **QA-V3-07 → DV-20**: se acepta lo que hace la app (sin `\r` ni `\t` en los campos de una línea) y se te muestra con las demás DV. El Architect corrige el comentario del código.
- **QA-V3-09**: se acepta en esta iteración. En el legacy, la capa del simulador también tapa contenido en ese lugar. El Designer lo revisa en el diseño de la 4a.
- **DT-21**: se acepta. Lo que se cierra se traga los toques durante 150 ms.
- **CAP-27**: no se toma en la It. 3.
  - AC-APP-05-10 se da por cumplido con la barra oscura vista en los dos builds y con las pruebas de la regla. Queda pendiente el modo claro (§5.3).
  - La foto se toma solo si la pedís, después de QA-V3-12.
- **QA-V3-02 a -04**: no frenan el cierre. Se corrigen al empezar la 4a, con una sola regeneración de capturas.
- **Pendientes del PO** de la spec (§15): hechos.
  - En el ROADMAP: `-2` y `−2` en la It. 3, los lectores de pantalla en la It. 14 y la rutina fuera de topes en la 4a.
  - En el inventario de rutinas: la precarga con negativos.
  - En FEATURE_INVENTORY: Q-26 y Q-92.
  - El detalle está en [01-po-spec §17](01-po-spec.md).

### 8.3 Opcional, sin urgencia

- **Licencia de Unity (P-08)**: ¿cargás tu licencia Personal de Unity como secreto del repositorio? Hace falta para que corra `ci-unity`. El repositorio ya está en GitHub y `ci-core` corre en verde.
- **CAP-27**, si la querés. Hace falta Windows en modo oscuro; si no, el script sale con 3 sin tocar nada. Los pasos:
  1. Cerrá RowMate (también el `RowMate.exe` de la It. 2 que quedó abierto) y el Editor.
  2. Contestá los avisos del Firewall.
  3. Cuando esté resuelto QA-V3-12, corré `powershell -NoProfile -File tools/unity/capture-cap27.ps1 -Rehearsal`.
  4. Si da 0, corré lo mismo con `-ReplaceUserDatabase`.

  Mientras QA-V3-12 no esté resuelto, se puede tomar con `-Out` en una carpeta temporal y mirar la foto antes de pasarla al repositorio.

---

## 9. Prerrequisitos para la It. 4a

### 9.1 De tu lado

| Qué | Para qué | ¿Obligatorio? |
|---|---|---|
| Tu OK a §8.1 | Cerrar el gate | Sí (alcanza con "OK a todo") |
| Contestar `Cancelar` en los avisos del Firewall | Que la PC responda y se pueda manejar la ventana real (QA-V3-08) | Sí; mejor ahora |
| Los 5 minutos de §3.1 | Cerrar la parte "ventana real" de 6 criterios | Recomendado |
| Módulos **Windows Build Support (IL2CPP)** y **Android Build Support**, y un Android 8.0+ de 64 bits con depuración USB | Cerrar AC-RUT-11-10, IL2CPP (R3-01) y las partes Android de §5.3 | No para la 4a. El teléfono hace falta a más tardar en la 6a |
| Licencia de Unity como secreto (P-08) | Que corra `ci-unity` | No |

La It. 4a no necesita nada nuevo de tu lado: se prueba con el simulador, y que la pantalla no se apague se verifica en la PC. En su gate te vamos a mostrar:

- D-ART-01: el contraste del HUD y el rojo fuera de objetivo (`#FF5F59` con flecha);
- D-ART-02: el HUD entre un 15 y un 25 % más grande.

### 9.2 Del equipo, al empezar la It. 4a

1. **Architect**:
   - QA-V3-02, -03 y -04 juntos, con una sola regeneración de capturas (dos corridas seguidas, por QA-V3-06).
   - También QA-V3-05, -06, -08 y -10, y el comentario de -07.
   - QA-V3-12, antes de cualquier toma de CAP-27.
   - En el diseño de la 4a: R3-08, y reusar la pila de pantallas, los diálogos y los avisos de la It. 3 para `¿Iniciar entrenamiento?` y el inmersivo.
2. **Designer**:
   - QA-V3-09: la llave frente a las acciones del editor y del HUD;
   - el visto bueno de QA-V3-02;
   - el kerning de los títulos grandes y del HUD.
3. **PO**: escribir la spec de la 4a (`iterations/04a-motor-hud-funcional/01-po-spec.md`), con:
   - la lista de Workout, que comparte la colección y la tarjeta de Rutinas;
   - qué pasa al iniciar una rutina fuera de los topes (R3-08);
   - la cinta por encima del entrenamiento a pantalla completa (AC-BLE-16-01);
   - la pantalla siempre encendida (con eso termina DV-08);
   - la paleta en el HUD (AC-APP-06-01);
   - las tablas de sesiones y puntos, que empiezan a llenarse;
   - las decisiones de su gate (D-ART-01, D-ART-02, D-ART-09, QA-P-17 y Q-91).
4. **QA**: verificar esas correcciones antes de construir encima.

---

## 10. Por qué el PO acepta la It. 3

- **El objetivo se cumple.**
  - En la pestaña Rutinas se crean, corrigen y borran rutinas con pasos sueltos y series ×N con progresiones, y siguen ahí al volver a abrir la app.
  - Nació la base propia v8, con las 4 tablas del legacy, y la del simulador quedó separada.
  - Las pantallas apiladas, los diálogos, los menús y los avisos funcionan como en el legacy, con la capa del simulador siempre encima.
  - La demo de la spec se ve paso a paso en DEMO-01 a DEMO-12, con la interfaz real y SQLite real. La reapertura muestra exactamente lo mismo: DEMO-10 es igual a DEMO-09.
  - En un build real, la rutina de la demo sobrevivió a dos procesos y a un cierre forzado justo después de guardar.
- **La paridad está probada contra el legacy.**
  - Se comprobaron las 11 filas de casos borde al carácter, los resúmenes en español y en inglés, y 105 casos de lectura de números.
  - También las reglas de tiempos, distancias, splits y repeticiones, y los casos 16 a 21 del inventario.
  - Cada una de las 63 filas de la checklist de paridad tiene su criterio. Pasan 101 de los 102 criterios, y el que falta es de Android.
- **La base es confiable.**
  - Las mismas pruebas pasan en .NET y dentro de Unity, con simulador y sin él, y la interfaz se prueba montada (235 pruebas).
  - Las capturas son deterministas, y el build normal no lleva nada del simulador.
  - Tus datos y los del legacy no se tocaron.
- **Lo que queda es menor.** Son 10 observaciones y ninguna afecta un criterio:
  - cuatro son de fotos, del arnés o de motivos de pruebas;
  - una es de movimiento (el último bloque, que se va sin colapsar);
  - una es de robustez, sin efecto hoy;
  - el resto, de herramientas y de documentación.

  Todas se resuelven al empezar la 4a, salvo QA-V3-12, que va antes de tomar CAP-27.
- **La condición de la It. 2 está cumplida** y su deuda está saldada.
- **Lo único que falta es tu OK** a §8.1 y, si podés, los 5 minutos de §3.1.
