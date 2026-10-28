# It. 3 — Rutinas: lista, editor y base local: spec de producto

| Campo | Valor |
|---|---|
| Autor | rowmate-po |
| Fecha | 2026-10-27 |
| Iteración | 3 · `03-rutinas` ([ROADMAP §2, It. 3](../../ROADMAP.md)) |
| Estado | **Cerrada**: verificada por QA ([05-qa-verification](05-qa-verification.md), ronda 3: APROBADO CON OBSERVACIONES) y aceptada por el PO ([REPORT](REPORT.md)); espera el OK del usuario al gate. Antes, corregida por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md)): §16. Cambios al cerrar: §17 |
| IDs dueños | **11 = 10 canónicos + 1 alias**: F-RUT-08 (+ F-HIS-18), F-RUT-09, F-RUT-10, F-RUT-11, F-RUT-12, F-RUT-13, F-RUT-14, F-HIS-17, F-HIS-20, F-APP-03 |
| También toca | F-APP-01 (las rutinas se cargan al arrancar), F-APP-02 (Rutinas deja de ser provisoria), F-APP-05 (barra de título de Windows, QA-V2-08; barra de desplazamiento en el teléfono, QA-V2-13), F-APP-09 (AC-APP-09-07, el primer campo de texto), F-BLE-11 (AC-BLE-11-02, reabierto por QA-V2-01), F-BLE-16 (AC-BLE-16-01, la cinta encima de las pantallas apiladas y los diálogos; AC-BLE-16-02, la llave que se aparta del `+` en Rutinas, DV-13), F-RUT-01 y F-RUT-04 (íconos y subtítulos en el editor) |
| Criterios | **102** = **91 nuevos** (RUT 70: F-RUT-08 9, F-RUT-09 6, F-RUT-10 11, F-RUT-11 10, F-RUT-12 6, F-RUT-13 16, F-RUT-14 12; HIS 10: F-HIS-17 6, F-HIS-20 4; APP 11: F-APP-03 8, F-APP-01 1, F-APP-05 2) + **11 de iteraciones anteriores** que se verifican, se reabren o cambian acá (§5; AC-BLE-16-02 cambia por DV-13). Además: **0 casos de test legacy** (el área no tiene tests: ROADMAP §5), **28 capturas, 12 tomas de la demo y 3 videos** (§7) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Las rutas `lib/...:línea` de esta spec son relativas a ese repositorio. Cada valor se leyó en `lib/features/routines/*`, `lib/core/database/database_service.dart`, `lib/core/models/routine.dart`, `interval_step.dart`, `lib/shared/theme.dart`, `lib/main.dart`, `lib/core/dev/simulator_overlay.dart` y en `lib/l10n/app_es.arb` / `app_en.arb`; las capturas de referencia son `docs/image-04.png` (editor) y `docs/image-02.png` (resumen en la UI en inglés). Los comportamientos que pone Flutter (avisos, ayudas emergentes, menú de texto, foco, Escape) se leyeron en el SDK local, Flutter 3.47.6 (`<flutter-sdk>/packages/flutter/...`) |
| Insumos | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) §4.1-§4.2 (Q-01, Q-10, Q-11, Q-13, Q-17, Q-18, Q-19, Q-25, Q-26, Q-92); inventarios [routines-models](../../product/inventory/routines-models.md) (F-RUT-08 a F-RUT-14, §3, §5.3 casos 16-21), [history-persistence](../../product/inventory/history-persistence.md) (F-HIS-17, F-HIS-18, F-HIS-20) y [app-shell-platform](../../product/inventory/app-shell-platform.md) (F-APP-01, F-APP-03, F-APP-15.4, F-APP-18); [REPORT de la It. 2](../02-dispositivo-simulador/REPORT.md) §5.4, §7 y §9.2; [01-po-spec de la It. 1](../01-fundaciones/01-po-spec.md) (AC-RUT-01 a -07, AC-APP-09-07, P-09) y [de la It. 2](../02-dispositivo-simulador/01-po-spec.md) (AC-BLE-16-01, AC-APP-02-08, §11.1); [ARCHITECTURE](../../architecture/ARCHITECTURE.md) §7.2, §8 y §9; [ADR 008](../../architecture/adr/008-persistencia.md); [ART_BIBLE](../../design/ART_BIBLE.md) §3.6-§3.8; [ANIMATION_BIBLE](../../design/ANIMATION_BIBLE.md) §2.2-§2.3; [STATUS](../../STATUS.md) |

---

## 0. Cómo leer esta spec

### 0.1 Identificadores

- **`AC-<ÁREA>-<NN>-<nn>`**: criterio `nn` de la ficha `F-<ÁREA>-<NN>`. El alias F-HIS-18 se verifica con F-RUT-08.
- Los IDs de "también toca" siguen la numeración de su iteración dueña: AC-APP-01-09 sigue a los 8 de la It. 2; AC-APP-05-10 y -11 siguen a los 9 de la It. 2.
- **§5** repite IDs de iteraciones anteriores con lo que falta ver ahora; si fallan, se reabren acá.
- **`CAP-nn`**, **`DEMO-nn`** y **`VID-nn`**: capturas, tomas de la demo y videos de esta iteración (§7), en `docs/migration/iterations/03-rutinas/snapshots/`.
- **`DV-nn`**: diferencias visibles con el legacy, a propósito (§11.1). Siguen la numeración de la It. 2 (DV-01 a DV-08).
- **`F-VACIA`**, **`F-LISTA`**, **`F-BORDE`**, **`F-RARA`**, **`F-ROTA`**: bases de prueba (§0.4).

### 0.2 Cómo se verifica cada criterio

Las mismas marcas de la It. 2:

| Marca | Qué es | Disponible |
|---|---|---|
| **[dotnet]** | `dotnet test` del Core y de los repositorios, fuera de Unity, con SQLite real y reloj de prueba | Sí |
| **[Editor-EM]** | Unity Test Framework, EditMode, Editor 6000.3.27f1 | Sí |
| **[Editor-PM]** | Unity Test Framework, PlayMode: la UI real montada, con la base de prueba, el tamaño, el idioma y los servicios que pide el criterio | Sí |
| **[Captura]** | Imagen o video renderizado por Unity con la UI real (no una maqueta), revisado por QA y el Designer | Sí |
| **[Device-Win]** | Build de Windows abierto en la PC (normal y con simulador) | Sí con Mono; IL2CPP solo con el módulo |
| **[Device-And]** | Build de Android en un teléfono 8.0+ de 64 bits | Solo con el módulo de Android y el teléfono |
| **[QA]** | Revisión estática de QA (código, base, build) | Sí |

Si falta el módulo de Android, el de IL2CPP o el teléfono, el criterio (o su parte de equipo) queda **NO VERIFICABLE** con el motivo y no frena la iteración. Nunca se da por cumplido sin verlo.

### 0.3 Notación

- Textos entre comillas invertidas: **exactos al carácter**. `·` es U+00B7 con un espacio a cada lado; `␣␣·␣␣` marca los **dos** espacios a cada lado del punto medio del subtítulo del editor; `⏎` es un salto de línea; `×` es U+00D7 (signo de multiplicación); `-` es el guion ASCII (U+002D) y `−` (U+2212) solo aparece donde se dice "signo menos tipográfico".
- Tamaños en dp, como en la It. 2 (Windows al 100 %: 1 dp = 1 px).
- "Aviso" = mensaje breve abajo, sin acción (el `SnackBar` del legacy). "Diálogo" = ventana modal centrada.
- Los textos visibles se citan por clave del catálogo y su valor es / en. Las claves **nuevas** de esta iteración están en §11.2.

### 0.4 Estado de prueba

Para que pruebas y capturas den siempre lo mismo:

- **Base de prueba**: cada prueba y cada captura usa un archivo de base **propio y temporal**, que se borra al terminar. **Nunca** se abre ni se escribe `rower_app.db` ni `rower_app_dev.db` del usuario (AC-HIS-20-04), con una sola excepción, CAP-27, y sus condiciones (AC-HIS-20-04).
- **Reloj de prueba**: la fecha de creación de una rutina nueva sale de un reloj fijo (`2026-10-27T13:00:00.000Z`, un minuto más por cada rutina creada en la misma prueba).
- **Bases sembradas** (las arma la prueba, en v8):
  - **`F-VACIA`**: base nueva, sin rutinas.
  - **`F-LISTA`**: 4 rutinas, de la más vieja a la más nueva: `Intervalos 4×` (2026-10-20; pasos sueltos trabajo 60 s, descanso 60 s, trabajo 60 s, descanso 60 s, trabajo 60 s, descanso 60 s, trabajo 60 s; sin descripción), `Solo calentamiento` (2026-10-21; calentamiento 45 s), `Pirámide 500` (2026-10-22; descripción `Bajar 2 s el split en cada vuelta`; calentamiento 600 s + serie ×4 [trabajo 500 m, vatios 150-180, split 125, progresión W +10 y split −2; trabajo 250 m, SPM 30; descanso 90 s] + enfriamiento 300 s) y `Escalera SPM` (2026-10-23; calentamiento 300 s + serie ×3 [trabajo 300 s, SPM 18, progresión SPM +2; descanso 120 s] + enfriamiento 300 s; sin descripción).
  - **`F-BORDE`**: la rutina `Casos borde`, con los 11 pasos de AC-RUT-12-06.
  - **`F-RARA`**: rutinas que la app nunca deja guardar, cargadas a mano en la base: una con vatios mín. 50 000; una serie ×2 000 000 000; una serie ×0; un paso con tipo `sprint`. Cada caso en su propia base cuando el criterio lo pide.
  - **`F-ROTA`**: un archivo con el nombre de la base que no es una base SQLite (texto cualquiera).
- **Base que falla a pedido**: un doble de prueba que tarda lo que la prueba pide o falla al cargar, al guardar o al borrar.
- **Idioma, escala de letra y "reducir movimiento" de prueba**, como en la It. 2. **Portapapeles de prueba** para el menú de texto.
- **Simulador sin ruido** (AC-BLE-14-06) cuando la prueba usa el modo simulador.
- Ninguno de estos dobles existe en un build que se instala.

---

## 1. Objetivo y demo

**Objetivo.** El usuario arma, corrige y borra sus rutinas (pasos sueltos y series ×N con progresiones) en la pestaña Rutinas, y las encuentra iguales cada vez que abre la app. Nace la base local propia, en el formato v8, con las 4 tablas del legacy, y la base separada del simulador. Las pantallas apiladas, los diálogos y los avisos funcionan como en el legacy, con la capa del simulador siempre encima.

**Demo (se le muestra al usuario antes de la It. 4a).** Con el modo simulador, en el Editor (Play) o en el build de desarrollo de Windows:

1. **Rutinas** con la base nueva: el ícono apagado, `No hay rutinas todavía` y `Presioná + para crear una`. El botón `+` se ve entero y la llave del simulador queda 16 dp por encima, sin taparlo.
2. `+` → `Nueva rutina`. Nombre `Escalera SPM`.
3. `Paso` → `Calentamiento`, `Por tiempo`, Min `5` → `Confirmar`: aparece `Calentamiento` con `5m`.
4. `Serie` → aparece un bloque `Serie` `×3` con `Trabajo` `2m` y `Descanso` `1m`.
5. Lápiz de `Trabajo` → Min `5`, `SPM objetivo` `18`, `Incremento SPM (+/rep)` `2` → `Confirmar`: `Trabajo` `5m␣␣·␣␣18 spm · 📈 SPM +2/rep` (el `📈`, como ícono: DV-10). Lápiz de `Descanso` → Min `2` → `2m`.
6. `Paso` → `Enfriamiento`, Min `5`: queda **después** de la serie. La fila dice `Pasos (4)`.
7. `Guardar` → la lista muestra `Escalera SPM` con `1 serie · 1 descanso` y la mini-barra de 8 tramos (amarillo, rosa, verde, rosa, verde, rosa, verde, azul).
8. Tocar la tarjeta → `Editar rutina`. Descripción `Sube 2 spm por repetición`; tocar `×3` → `Cantidad` `4` → `OK` → `×4`. `Guardar`: la tarjeta queda en el mismo lugar, muestra la descripción y la mini-barra tiene 10 tramos.
9. **Cerrar la app y abrirla de nuevo** (parar y volver a dar Play, o cerrar y abrir el `.exe`): `Escalera SPM` sigue ahí; el editor muestra `×4`, `18 spm` y `SPM +2/rep`.
10. Opcional: abrir el **build normal**. No aparece `Escalera SPM`: el simulador usa su propia base (`rower_app_dev.db`).
11. Menú `⋮` → `Eliminar` → diálogo `Eliminar rutina` / `¿Eliminar "Escalera SPM"?` → `Eliminar`: la tarjeta desaparece y vuelve el estado vacío.
12. Con el sistema en inglés: `Routines`, `New routine`, `Step`, `Series`, `1 interval · 1 rest`.
13. Las pruebas: `dotnet test` en verde con los repositorios y las reglas del editor; las PlayMode de Rutinas y del editor en verde; la carpeta de capturas de §7, con la secuencia DEMO-01 a DEMO-12.

---

## 2. Alcance

| ID | Funcionalidad | Qué se cierra en la It. 3 | Vuelve en |
|---|---|---|---|
| F-RUT-08 (+ F-HIS-18) | Persistencia de rutinas y pasos | Alta, edición, baja, lectura por id, orden, todo o nada, reapertura | 9 (vínculo con sesiones, F-RUT-20), 12 (lectura por id para Strava) |
| F-RUT-09 | Estado de la colección | Carga al arrancar, alta arriba, edición en su lugar, baja; un toque una rutina (Q-19); errores (Q-13) | 4a (lista de Workout, que comparte la colección) |
| F-RUT-10 | Pantalla Rutinas | Completa (más la mini-barra EXR) | 4a (la misma tarjeta en Workout) |
| F-RUT-11 | Editor: datos y guardado | Completo, con edición de texto en escritorio y en el teléfono | — |
| F-RUT-12 | Pasos sueltos | Completo | — |
| F-RUT-13 | Diálogo de paso | Completo, con los topes de Q-92 | — |
| F-RUT-14 | Series en el editor | Completo, con el tope de repeticiones | — |
| F-HIS-17 | Base local | Archivo, esquema v8, sin base legacy, errores | 4a (sesiones y telemetría), 13 (`origin`) |
| F-HIS-20 | Base del simulador | Completo | — |
| F-APP-03 | Pantallas apiladas, diálogos y avisos | Completo para el editor; el patrón queda para el detalle (It. 9) y el inmersivo (It. 4a-4b) | 4a, 9 |

---

## 3. Historias de usuario

| ID | Historia |
|---|---|
| H-01 | Como **remero**, quiero armar una rutina con calentamiento, series ×N y enfriamiento, para no cargar cada repetición a mano. |
| H-02 | Como **remero que entrena por intervalos**, quiero que una serie suba (o baje) el objetivo en cada repetición, para armar escaleras y pirámides. |
| H-03 | Como **remero**, quiero ver mis rutinas en una lista, con su resumen y su estructura de un vistazo, para elegir la de hoy. |
| H-04 | Como **remero**, quiero corregir una rutina o borrarla (con confirmación), sin perder lo demás. |
| H-05 | Como **usuario**, quiero que mis rutinas sigan ahí cuando cierro y abro la app, aunque se corte la luz justo después de guardar. |
| H-06 | Como **usuario distraído**, quiero que la app me avise si me olvido el nombre o los pasos, y que no me deje guardar números que rompan el entrenamiento. |
| H-07 | Como **usuario en la PC**, quiero escribir, copiar y pegar en los campos con el teclado y el mouse como en cualquier programa. |
| H-08 | Como **usuario de la app vieja**, quiero que la nueva no toque mis rutinas ni mi historial del legacy. |
| H-09 | Como **desarrollador o tester**, quiero que el simulador use su propia base y que su capa quede encima de todo, para probar sin ensuciar los datos reales. |

---

## 4. Criterios de aceptación

### 4.0 Conteo

| Ficha | AC | Ficha | AC | Ficha | AC |
|---|---|---|---|---|---|
| F-RUT-08 | 9 | F-RUT-12 | 6 | F-HIS-17 | 6 |
| F-RUT-09 | 6 | F-RUT-13 | 16 | F-HIS-20 | 4 |
| F-RUT-10 | 11 | F-RUT-14 | 12 | F-APP-03 | 8 |
| F-RUT-11 | 10 | | | F-APP-01 | 1 |
| | | | | F-APP-05 | 2 |
| **RUT** | | | **70** | **HIS 10 · APP 11** | |

Total: **91 criterios nuevos** (§4.1-§4.11) + **11 de iteraciones anteriores** (§5) = **102**.

### 4.1 F-RUT-08 (+ F-HIS-18) — Persistencia de rutinas y pasos

- **AC-RUT-08-01 · Alta completa.** *Dado* una rutina nueva con nombre, descripción y pasos, *cuando* se guarda y se vuelve a leer, *entonces* tiene el mismo nombre y la misma descripción, al carácter (nombre `Pirámide ñ ¿¡`; descripción con un salto de línea interno, `Sube 2 spm⏎2.ª vuelta`), una fecha de creación, y sus pasos en el mismo orden con posiciones 0…n−1 y **los 14 atributos** de AC-RUT-02-01 idénticos: tipo, duración, distancia, vatios mín. y máx., SPM, split, identificador de serie, repeticiones y las 3 progresiones. Un valor vacío vuelve vacío y un 0 vuelve 0 (duración 0 s ≠ sin duración; progresión 0 ≠ sin progresión). [dotnet] · `lib/core/database/database_service.dart:154-163`; `lib/core/models/interval_step.dart:111-145`
- **AC-RUT-08-02 · Edición reemplaza los pasos.** *Dado* `Escalera SPM` guardada, *cuando* se edita (nombre `Escalera SPM 2`, se borra el calentamiento y se agrega un paso al final) y se guarda, *entonces* al leerla tiene el nombre nuevo, **la misma fecha de creación**, exactamente los pasos nuevos (ni uno viejo de más, ningún duplicado) con posiciones 0…n−1 renumeradas, y las demás rutinas no cambian. Guardar sin tocar nada deja la rutina igual. [dotnet] · `database_service.dart:165-181`; `lib/core/models/routine.dart:107-112`
- **AC-RUT-08-03 · Baja.** *Dado* `F-LISTA`, *cuando* se borra `Pirámide 500`, *entonces* deja de existir con todos sus pasos (Q-17: el legacy los deja huérfanos e invisibles; acá se borran, sin efecto visible) y las otras 3 rutinas quedan intactas. Una rutina creada después nunca muestra pasos de una borrada. [dotnet] · `database_service.dart:183-186`; FEATURE_INVENTORY DC-06
- **AC-RUT-08-04 · Lectura por id.** *Dado* el id de una rutina guardada, *entonces* la lectura devuelve la rutina con sus pasos en orden; *dado* un id que no existe (o el de una borrada), devuelve "no existe" sin error. La usan el historial (It. 9) y Strava (It. 12). [dotnet] · `database_service.dart:206-212`
- **AC-RUT-08-05 · Orden de la lista y fecha de creación (Q-18).** *Entonces* la lista sale de la **más nueva a la más vieja** según la creación (`F-LISTA`: `Escalera SPM`, `Pirámide 500`, `Solo calentamiento`, `Intervalos 4×`), y editar nunca cambia la fecha ni el lugar. La fecha se guarda en un formato que no depende de la zona horaria (ARCHITECTURE §9.2): *dado* A creada a las 10:00 con UTC−3 (13:00 UTC) y el equipo pasado a UTC−5, *cuando* se crea B 30 minutos después (08:30 local, 13:30 UTC), *entonces* B queda **arriba** de A. El legacy guarda la hora local sin zona y ahí B quedaría abajo: es la corrección que permite Q-18. [dotnet] · `database_service.dart:145`; `routine.dart:111`
- **AC-RUT-08-06 · Las series se reconocen al volver.** *Dado* una rutina con dos series contiguas creadas en el mismo milisegundo (identificadores distintos por el contador, como `g{milisegundos}_{contador}` del legacy) y una serie de un solo paso, *cuando* se guarda y se reabre, *entonces* el editor muestra los mismos bloques, cada uno con sus pasos, su `×N` y sus progresiones. El identificador no se ve en ninguna pantalla. [dotnet] · [Editor-PM] · `lib/features/routines/routine_editor_screen.dart:111`
- **AC-RUT-08-07 · Todo o nada, y para siempre.** *Dado* que el guardado de una rutina (alta o edición) se corta a la mitad (falla inyectada después del primer paso), *entonces* la rutina queda **como estaba antes** (o no existe, si era nueva): nunca con pasos parciales ni sin pasos (el legacy no usa transacción: F-RUT-08, casos borde). *Dado* que la app se cierra a la fuerza 1 s después de que el editor se cerró por un `Guardar` válido, *cuando* se vuelve a abrir, *entonces* la rutina está, completa. [dotnet] · [Device-Win] (cerrar el proceso desde el Administrador de tareas) · F-RUT-08 Mejora EXR
- **AC-RUT-08-08 · Tipo de paso desconocido (efecto visible de AC-RUT-01-05).** *Dado* `F-RARA` con un paso de tipo `sprint`, *cuando* se cargan las rutinas, *entonces* la pestaña muestra el estado de error de carga (AC-RUT-09-04) y no un indicador de carga eterno; la base no se modifica. En el legacy falla la lista entera y queda cargando para siempre (`firstWhere` sin respaldo); con Q-13 se ve el error (P-07). Lo mismo con un número guardado que no entra en el rango de enteros de la app (fuera de −2 147 483 648 … 2 147 483 647; solo posible a mano, porque ningún tope de Q-92 llega ahí): la carga falla con el mismo error y la base no se toca. El legacy lo leería, porque sus enteros son de 64 bits (DT-10). [dotnet] · [Editor-PM] · `interval_step.dart:133`; `lib/features/routines/routines_provider.dart:19-25`
- **AC-RUT-08-09 · Reapertura.** *Dado* `Escalera SPM` creada y editada como en la demo, *cuando* la app se cierra y se vuelve a abrir sobre la misma base, *entonces* la lista y el editor muestran exactamente lo mismo que antes de cerrar (nombre, descripción, resumen, pasos, subtítulos, `×4`, progresiones) y en el mismo orden. [Editor-PM] (se arma la app de nuevo sobre la misma base de prueba) · [Device-Win] (cerrar la ventana y abrir el `.exe`) · [Captura] DEMO-10

### 4.2 F-RUT-09 — Estado de la colección de rutinas

- **AC-RUT-09-01 · Carga al arrancar.** *Entonces* las rutinas se leen **al abrir la app**, aunque el usuario no vaya a Rutinas (es AC-APP-01-09). Mientras carga, Rutinas muestra el indicador; después, la lista o el vacío. Ir y volver de otra pestaña **no** vuelve a leer la base: con la base de prueba lenta (2 s), la segunda visita a Rutinas no muestra el indicador. [dotnet] · [Editor-PM] · `routines_provider.dart:12-14`; `lib/main.dart:83`
- **AC-RUT-09-02 · Alta arriba, edición en su lugar, baja (caso 21).** *Dado* `F-LISTA`, *cuando* se crea `Nueva`, *entonces* aparece **primera**; *cuando* se edita `Solo calentamiento` (tercera) y se le cambia el nombre, *entonces* sigue tercera con el nombre nuevo; *cuando* se borra `Pirámide 500`, *entonces* desaparece y las otras no se mueven de su orden. Cada cambio se ve en el acto, sin recargar. [dotnet] · [Editor-PM] · `routines_provider.dart:27-52` (alta al tope `:35`, mismo lugar `:42-43`)
- **AC-RUT-09-03 · Un toque, una rutina (Q-19).** *Dado* el editor de una rutina nueva válida, *cuando* se toca `Guardar` dos veces seguidas (o se toca y se pulsa Enter sobre el botón enfocado), *entonces* se crea **una sola** rutina y el editor se cierra una sola vez; los toques que llegan mientras se guarda **o mientras el editor se está cerrando** no hacen nada (por ejemplo, un segundo toque 50 ms después del primero, con el guardado ya terminado y el editor saliendo). Lo mismo al editar: un solo reemplazo. El legacy puede crear dos mientras espera a la base (F-RUT-09, casos borde); lo que ya está saliendo no responde, como en el legacy (AC-APP-03-01). [Editor-PM]
- **AC-RUT-09-04 · Error al cargar (Q-13).** *Dado* la base que falla al cargar (o `F-ROTA`), *entonces* Rutinas no queda en el indicador: muestra centrado `routinesLoadError` (`No se pudieron cargar las rutinas` / `Couldn't load routines`) y el botón `retry` (`Reintentar` / `Retry`); el `+` sigue visible. *Cuando* la base vuelve a responder y se toca `Reintentar`, *entonces* aparece la lista. El resto de la app sigue funcionando (Dispositivo, simulador). [Editor-PM] · [Captura] CAP-03
- **AC-RUT-09-05 · Error al guardar (Q-13).** *Dado* la base que falla al guardar, *cuando* se toca `Guardar` con datos válidos, *entonces* el editor **queda abierto con todo lo escrito** y aparece el aviso `editorSaveError` (`No se pudo guardar la rutina` / `Couldn't save the routine`); tocar `Guardar` de nuevo, con la base ya bien, guarda una sola rutina. El legacy se queda abierto sin decir nada. [Editor-PM] · [Captura] CAP-28
- **AC-RUT-09-06 · Error al borrar (Q-13).** *Dado* la base que falla al borrar, *cuando* se confirma `Eliminar`, *entonces* la tarjeta **sigue** en la lista y aparece el aviso `routineDeleteError` (`No se pudo eliminar la rutina` / `Couldn't delete the routine`). Con la base bien, la tarjeta desaparece apenas termina el borrado (como el legacy, que no espera en el diálogo). [Editor-PM] · `lib/features/routines/routines_screen.dart:115`

### 4.3 F-RUT-10 — Pantalla Rutinas (lista)

- **AC-RUT-10-01 · Pestaña y título.** *Entonces* Rutinas es el destino 2 de la barra (`navRoutines` `Rutinas` / `Routines`, ícono `fitness_center`, AC-APP-02-01), su barra superior dice `routinesTitle` (`Rutinas` / `Routines`) y **no** tiene acciones; abajo a la derecha está el botón flotante `+` (ícono `add`). Desde esta pantalla no se inicia ningún entrenamiento (eso es Workout, It. 4a). [Editor-PM] · [Captura] CAP-01, CAP-04 · `routines_screen.dart:16-23`
- **AC-RUT-10-02 · Cargando y vacío.** *Entonces*, mientras carga, se ve solo un indicador circular centrado; aparece con un fundido si la carga sigue a los 120 ms, así una carga rápida no deja un parpadeo (el legacy lo muestra en el acto; Mejora EXR, 04-design-notes §4.1). *Dado* `F-VACIA`, se ve centrado: el ícono `fitness_center` de 56 dp en blanco al 12 %; a 12 dp, `routinesEmpty` (`No hay rutinas todavía` / `No routines yet`) en `text.secondary`; a 8 dp, `routinesEmptyHint` (`Presioná + para crear una` / `Press + to create one`) en 13 dp y `text.tertiary` (el legacy los pone al 38 % y al 24 %: DV-11, D-ART-03). [Editor-PM] · [Captura] CAP-01, CAP-02 · `routines_screen.dart:24-40`
- **AC-RUT-10-03 · Tarjeta.** *Dado* `F-LISTA`, *entonces* hay una tarjeta por rutina (fondo `#1A2E45`), en el orden de AC-RUT-08-05, cada una con: a la izquierda el ícono `fitness_center` en `#00B4D8`; el **nombre** en seminegrita; debajo, la **descripción** (12 dp) **solo si no está vacía** (`Pirámide 500`: `Bajar 2 s el split en cada vuelta`; con saltos de línea, en varias líneas); después el **resumen** (AC-RUT-10-10); a la derecha el menú `⋮`. Un nombre o una descripción largos (200 caracteres, también una sola palabra de 200 sin espacios, que se parte donde haga falta) pasan a otra línea: nunca se cortan ni salen de la tarjeta. El nombre y la descripción se ven **tal cual se escribieron**, en la tarjeta, en el diálogo de eliminar y en los campos del editor: `<b>A</b>` no se pone en negrita y `Serie\n2` (la barra y la `n` escritas a mano) no salta de línea (paridad: el legacy no interpreta marcas en el texto del usuario). [Editor-PM] · [Captura] CAP-04, CAP-06 · `routines_screen.dart:41-84` (descripción `:56-58`, resumen `:59-61`)
- **AC-RUT-10-04 · Botón `+`.** *Cuando* se toca `+`, *entonces* se abre el editor en modo **nueva rutina** (AC-RUT-11-01). *Dado* el modo simulador con el panel cerrado, *entonces* el `+` se ve entero y se toca en toda su superficie: la llave del simulador no lo tapa (el panel abierto sí lo tapa, como en el legacy: AC-BLE-16-02). En el legacy la llave (40 dp, a 16 de la derecha y 96 de abajo) queda encima del `+` (56 dp, a 16 de la derecha y 16 sobre la barra de 80, o sea también a 96 de abajo): le tapa los 40 × 40 dp de abajo a la derecha, la mitad del botón. En la app, mientras se ve Rutinas, la llave queda 16 dp por encima del `+` (AC-BLE-16-02 con el texto de esta iteración, DV-13). [Editor-PM] · [Captura] CAP-01 · `routines_screen.dart:17-23`; `lib/core/dev/simulator_overlay.dart:38-51`; `<flutter-sdk>/packages/flutter/lib/src/material/floating_action_button_location.dart:25`
- **AC-RUT-10-05 · Tocar una tarjeta.** *Cuando* se toca una tarjeta (fuera del `⋮`), *entonces* se abre el editor en modo **editar** esa rutina, con su nombre, su descripción y sus pasos precargados (AC-RUT-11-01). [Editor-PM] · `routines_screen.dart:76-80`
- **AC-RUT-10-06 · Menú `⋮`.** *Cuando* se toca `⋮`, *entonces* se abre un menú junto al botón con `routineEdit` (`Editar` / `Edit`) y `routineDelete` (`Eliminar` / `Delete`) en rojo. `Editar` hace lo mismo que tocar la tarjeta; `Eliminar` abre el diálogo de AC-RUT-10-07; tocar afuera, Escape o "Atrás" lo cierran sin hacer nada. El botón tiene la ayuda emergente `menuShow` (`Mostrar menú` / `Show menu`; AC-APP-03-06), que en el legacy pone Flutter. [Editor-PM] · [Captura] CAP-07 · `routines_screen.dart:64-75`; `<flutter-sdk>/packages/flutter_localizations/lib/src/l10n/material_es.arb:14`
- **AC-RUT-10-07 · Diálogo de eliminar.** *Entonces* el diálogo tiene título `routineDeleteTitle` (`Eliminar rutina` / `Delete routine`), texto `routineDeleteContent` con el nombre tal cual se escribió (`¿Eliminar "Escalera SPM"?` / `Delete "Escalera SPM"?`, con comillas rectas `"`; AC-RUT-10-03), y los botones `cancel` (`Cancelar` / `Cancel`, de texto) y `delete` (`Eliminar` / `Delete`, relleno `#D32F2F` con texto blanco). **Solo** `Eliminar` borra; `Cancelar`, tocar afuera, Escape y "Atrás" no cambian nada. [Editor-PM] · [Captura] CAP-08, DEMO-11 · `routines_screen.dart:97-115`
- **AC-RUT-10-08 · Sin otras acciones.** *Entonces* la lista no tiene búsqueda, otro orden, duplicar, deslizar para borrar ni deshacer (paridad; BKL-12). [QA] · [Editor-PM]
- **AC-RUT-10-09 · Desplazamiento y estado.** *Dado* 30 rutinas, *entonces* la lista se desplaza (margen 12 dp en el legacy), la última tarjeta queda por encima del `+` al final del desplazamiento (con el simulador, también por encima de la llave, que en Rutinas está sobre el `+`: AC-BLE-16-02), y al abrir y cerrar el editor o al cambiar de pestaña la lista vuelve a la misma posición. En los perfiles de teléfono no hay barra de desplazamiento fija (AC-APP-05-11). [Editor-PM] · [Captura] CAP-06
- **AC-RUT-10-10 · Resumen en pantalla (caso 11, AC-RUT-05-02).** *Dado* `F-LISTA` y `F-BORDE`, *entonces* los resúmenes son: [Editor-PM] · [Captura] CAP-04, CAP-05

  | Rutina | es | en | Legacy (igual en es y en) |
  |---|---|---|---|
  | `Escalera SPM` | `1 serie · 1 descanso` | `1 interval · 1 rest` | `1 series · 1 descansos` |
  | `Pirámide 500` | `2 series · 1 descanso` | `2 intervals · 1 rest` | `2 series · 1 descansos` |
  | `Solo calentamiento` | `1 paso` | `1 step` | `1 pasos` |
  | `Intervalos 4×` | `4 series · 3 descansos` | `4 intervals · 3 rests` | `4 series · 3 descansos` |
  | `Casos borde` | `7 series · 2 descansos` | `7 intervals · 2 rests` | `7 series · 2 descansos` |

  El resumen cuenta pasos de la **plantilla** (no de la rutina expandida): `×4` en `Escalera SPM` no lo cambia.
- **AC-RUT-10-11 · Mini-barra de estructura (Mejora EXR, agregado).** *Entonces*, debajo del resumen, cada tarjeta muestra una franja con **un tramo por paso de la rutina expandida**, en orden y en el color de su tipo (`work` `#EF476F`, `rest` `#06D6A0`, `warmup` `#FFD166`, `cooldown` `#118AB2`): `Escalera SPM` ×3 → 8 tramos (calentamiento, trabajo, descanso, trabajo, descanso, trabajo, descanso, enfriamiento); con ×4, 10; `Pirámide 500` → 14; `Solo calentamiento` → 1; `Intervalos 4×` → 7. Nunca sale de la tarjeta ni ocupa más de un renglón. *Dado* `F-RARA` (serie ×2 000 000 000 o ×0), *entonces* la lista igual carga, la tarjeta se ve con su nombre y su resumen y la app sigue respondiendo. La franja sigue en un renglón: si los tramos no entran, se dibujan los que caben y los últimos se van apagando (se lee "sigue"); una rutina que expandida no tiene ningún paso (solo una serie ×0) muestra una pista vacía del mismo alto (04-design-notes §4.3). No reemplaza nada del legacy: es el agregado de ART_BIBLE §3.7. [dotnet] (la secuencia de tramos) · [Editor-PM] · [Captura] CAP-04, DEMO-07, DEMO-09

### 4.4 F-RUT-11 — Editor de rutina: pantalla, datos generales y guardado

- **AC-RUT-11-01 · Barra superior.** *Entonces* el título es `editorNewRoutine` (`Nueva rutina` / `New routine`) al crear y `editorEditRoutine` (`Editar rutina` / `Edit routine`) al editar (nunca el nombre de la rutina); a la izquierda, la flecha de volver con la ayuda `navBack` (`Atrás` / `Back`, P-09 de la It. 1); a la derecha, el botón de texto `save` (`Guardar` / `Save`). [Editor-PM] · [Captura] CAP-09, CAP-10, CAP-24 · `routine_editor_screen.dart:206-211`; `<flutter-sdk>/packages/flutter_localizations/lib/src/l10n/material_es.arb:5`
- **AC-RUT-11-02 · Campos.** *Entonces*, con márgenes de 16 dp, hay dos campos con borde y etiqueta: `editorRoutineName` (`Nombre de la rutina` / `Routine name`), de **una línea** (un salto de línea pegado se quita), y a 12 dp `editorDescription` (`Descripción (opcional)` / `Description (optional)`), de **2 líneas visibles**, donde Enter agrega un salto de línea y el texto que no entra se desplaza dentro del campo. Al editar vienen precargados. [Editor-PM] · [Captura] CAP-09 · `routine_editor_screen.dart:214-236`
- **AC-RUT-11-03 · Fila de pasos.** *Entonces*, después de una línea divisoria, una fila con `editorStepsCount` en seminegrita (`Pasos ({count})` / `Steps ({count})`) y, a la derecha, los botones de texto `+ editorStep` (`Paso` / `Step`, ícono `add`) y `editorSeries` (`Serie` / `Series`, ícono `repeat`). `count` es la cantidad de pasos de la **plantilla**: `Escalera SPM` → `Pasos (4)` (la serie de 2 pasos ×3 suma 2); `F-BORDE` → `Pasos (11)`. Se actualiza en el acto al agregar o quitar. [Editor-PM] · `routine_editor_screen.dart:238-260`
- **AC-RUT-11-04 · Sin pasos.** *Entonces*, sin pasos, se ve centrado `editorEmptySteps` en dos líneas (`Todavía no hay pasos.⏎Presioná "Paso" o "Serie".` / `No steps yet.⏎Press "Step" or "Series".`) en `text.tertiary` (el legacy, al 38 %: DV-11). [Editor-PM] · [Captura] CAP-09 · `routine_editor_screen.dart:262-268`
- **AC-RUT-11-05 · Validaciones al guardar, en orden (caso 16).** *Cuando* se toca `Guardar`: [dotnet] (la regla) · [Editor-PM] · [Captura] CAP-18, CAP-19 · `routine_editor_screen.dart:42-53`
  1. Nombre vacío después de quitar los espacios de los extremos (`` o `␣␣␣`) → aviso `editorNameEmpty` (`El nombre no puede estar vacío` / `Name cannot be empty`) y no guarda. Con nombre vacío **y** sin pasos, solo este aviso.
  2. Sin pasos → aviso `editorNoSteps` (`Agregá al menos un paso` / `Add at least one step`) y no guarda.
  3. Algún paso fuera de los topes de Q-92 (solo posible con una base tocada a mano, `F-RARA`) → aviso `editorStepOutOfRange` (`Hay un paso con valores fuera de rango` / `A step has out-of-range values`) y no guarda. Agregado por Q-92; debajo del tope nunca aparece. Se controlan los mismos topes del diálogo sobre los valores guardados de cada paso (AC-RUT-13-11) y, en una serie, sobre cada repetición (AC-RUT-13-12); las progresiones de un paso suelto no se controlan, porque no se ven ni se aplican (AC-RUT-12-03). Una serie de más de 999 repeticiones cuenta como fuera de rango; una ×0 (o con repeticiones negativas) **no**: se guarda tal cual, porque se muestra (AC-RUT-14-03) y no aporta pasos al expandir (DT-13).
- **AC-RUT-11-06 · Guardado válido.** *Cuando* pasa las validaciones, *entonces* el nombre y la descripción se guardan **recortados** en sus extremos (`␣␣Mi rutina␣␣` → `Mi rutina`; una descripción `␣␣␣` queda vacía y la tarjeta no la muestra), los pasos se renumeran 0…n−1, al crear la fecha es la del momento y al editar se conservan id y fecha (AC-RUT-08-02, -05); el editor se cierra y se vuelve a la lista, que ya muestra el cambio. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:55-72`
- **AC-RUT-11-07 · Sin otras validaciones.** *Entonces* se pueden guardar dos rutinas con el mismo nombre y un nombre de 500 caracteres; no hay largo máximo ni aviso. [dotnet] · [Editor-PM]
- **AC-RUT-11-08 · Volver descarta sin preguntar.** *Dado* el editor con cambios sin guardar, *cuando* se toca la flecha o "Atrás" de Android, *entonces* el editor se cierra **sin preguntar** y la rutina queda como estaba (una nueva no se crea). Escape **no** cierra el editor (en el legacy Escape solo cierra diálogos y menús: AC-APP-03-02). [Editor-PM] · [Device-And] · F-RUT-11
- **AC-RUT-11-09 · Edición de texto en la PC.** *Dado* Windows, *entonces* en los campos de texto del editor y de los diálogos: [Editor-PM] (lo que se pueda simular) · [Device-Win] · [Captura] CAP-26 · `<flutter-sdk>/packages/flutter/lib/src/widgets/editable_text.dart:2116-2128`, `:2649-2696`, `:4963-4981`
  - se selecciona con el mouse (arrastrar, doble clic = palabra) y con Mayús + flechas; Ctrl+C, Ctrl+X, Ctrl+V y Ctrl+A copian, cortan, pegan y seleccionan todo; se escriben acentos y `ñ` con el teclado del sistema (teclas muertas, AltGr);
  - **clic derecho** abre un menú con `textCut` (`Cortar` / `Cut`) y `textCopy` (`Copiar` / `Copy`) solo si hay texto seleccionado, `textPaste` (`Pegar` / `Paste`) solo si el portapapeles tiene texto, y `textSelectAll` (`Seleccionar todo` / `Select all`) solo si hay texto y no está todo seleccionado; en el **idioma de la app** (es lo que hace el legacy en escritorio, con los textos de Flutter: `material_es.arb:27-32`). Sin ninguna acción posible, no se abre nada (P-06);
  - al llegar a un campo de **una línea** con Tab (o al abrirse un diálogo con foco en el campo) se selecciona todo su texto; con un clic, el cursor va donde se hizo clic. En la descripción (varias líneas) no se selecciona todo.
- **AC-RUT-11-10 · Edición de texto en el teléfono (parte de AC-APP-09-07).** *Dado* Android, *entonces* al tocar un campo se abre el teclado del sistema y el texto se edita **en el mismo campo** (sin una caja de texto extra encima del teclado); la pantalla se acomoda para que el campo con foco quede visible por encima del teclado (F-APP-15.4), también dentro de un diálogo; el menú de seleccionar, copiar y pegar es el del sistema, en el idioma **del sistema** (P-06, ya aceptada). [Device-And] · `android/app/src/main/AndroidManifest.xml:32`

### 4.5 F-RUT-12 — Pasos sueltos en el editor

- **AC-RUT-12-01 · Agregar.** *Cuando* se toca `+ Paso`, *entonces* se abre el diálogo de paso en modo **nuevo** y **sin** la sección de progresiones (AC-RUT-13-01). `Confirmar` agrega el paso **al final** de la plantilla como paso suelto, aunque lo último sea una serie (queda después de su pie). `Cancelar` (o tocar afuera) no agrega nada. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:75-88`
- **AC-RUT-12-02 · Fila del paso.** *Entonces* cada paso es una fila con fondo de tarjeta, separación vertical de 3 dp (6 entre filas) y, a la izquierda, un **borde de 3 dp en el color del tipo** (`work` `#EF476F`, `rest` `#06D6A0`, `warmup` `#FFD166`, `cooldown` `#118AB2`); un cuadrado con el ícono del tipo (AC-RUT-01-04: `whatshot`, `fitness_center`, `pause_circle_outline`, `ac_unit`) sobre el color del tipo al 20 %; el **título** es el nombre localizado del tipo (`Calentamiento` / `Warmup`, `Trabajo` / `Work`, `Descanso` / `Rest`, `Enfriamiento` / `Cooldown`) en seminegrita. El título y el ícono van en la **variante legible del color del tipo** (`step.<tipo>.hud`): trabajo `#FF5D7F`, descanso `#06D6A0`, calentamiento `#FFD166`, enfriamiento `#37A2CB`; el borde y el fondo del cuadrado siguen en el color del tipo. El legacy pinta título e ícono en el color del tipo, que en trabajo y enfriamiento no llega a AA (DV-12). El **subtítulo** sigue AC-RUT-04-06 (duración sola, o `{duración}␣␣·␣␣{objetivos}`); a la derecha, el lápiz (editar) y una cruz en rojo (eliminar). Tamaños: DV-12. [Editor-PM] · [Captura] CAP-10, CAP-12 · `routine_editor_screen.dart:316-369`, `:447-458`; `lib/shared/theme.dart:56-69`
- **AC-RUT-12-03 · Editar.** *Cuando* se toca el lápiz, *entonces* se abre el diálogo en modo **editar** con los valores del paso (AC-RUT-13-10), con progresiones **solo si el paso está en una serie**. `Confirmar` reemplaza el paso **en su lugar**, conservando su serie y sus repeticiones; `Cancelar` no cambia nada. *Dado* un paso suelto que trae progresiones de la base (`F-RARA`), *cuando* se edita y se confirma, *entonces* las progresiones se conservan aunque no se vean (y no se aplican: AC-RUT-06-07). [dotnet] · [Editor-PM] · `routine_editor_screen.dart:90-108`, `:498-521`
- **AC-RUT-12-04 · Borrar.** *Cuando* se toca la cruz, *entonces* el paso se borra **al instante**, sin confirmación ni deshacer, y `Pasos (N)` baja en 1. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:190-194`
- **AC-RUT-12-05 · Lo que no se puede.** *Entonces* no hay forma de reordenar, duplicar, mover un paso a una serie ni sacarlo de una (no hay manija, gesto ni menú para eso; BKL-12). [QA] · [Editor-PM]
- **AC-RUT-12-06 · Subtítulos exactos en pantalla (casos 9 y 10).** *Dado* `F-BORDE` (`Casos borde`), *entonces* el editor muestra, en este orden (el `📈` dibujado como ícono, DV-10; si un subtítulo no entra, pasa a otra línea, nunca se corta con "…"): [dotnet] (las etiquetas) · [Editor-PM] · [Captura] CAP-12

  | # | Paso guardado | Título | Subtítulo |
  |---|---|---|---|
  | 1 | trabajo 0 s | `Trabajo` | `0s` |
  | 2 | descanso sin meta | `Descanso` | `?` |
  | 3 | trabajo 90 s, vatios mín. 150 | `Trabajo` | `1m 30s␣␣·␣␣>150W` |
  | 4 | trabajo 3600 s, solo vatios máx. 180 | `Trabajo` | `60m` |
  | 5 | enfriamiento 3725 s | `Enfriamiento` | `62m 5s` |
  | 6 | trabajo 2000 m, SPM 0 | `Trabajo` | `2000m␣␣·␣␣0 spm` |
  | 7 | calentamiento −90 s | `Calentamiento` | `30s` |
  | 8 | serie sin repeticiones (`×1`): trabajo 60 s, split 59 | `Trabajo` | `1m␣␣·␣␣00:59/500m` |
  | 9 | trabajo 120 s, vatios 120-180, SPM 22, split 125 | `Trabajo` | `2m␣␣·␣␣120-180W · 22 spm · 02:05/500m` |
  | 10 | serie ×2: trabajo 300 s, SPM 20, progresión SPM −1 | `Trabajo` | `5m␣␣·␣␣20 spm · 📈 SPM -1/rep` |
  | 11 | (misma serie) descanso 60 s, progresión split +3, sin split | `Descanso` | `1m␣␣·␣␣📈 Split +3s/rep` |

### 4.6 F-RUT-13 — Diálogo de paso

- **AC-RUT-13-01 · Título y valores iniciales.** *Entonces* el título es `editorNewStep` (`Nuevo paso` / `New step`) o `editorEditStep` (`Editar paso` / `Edit step`). En modo nuevo: tipo **Trabajo**, modo **Por tiempo**, todos los campos vacíos. [Editor-PM] · [Captura] CAP-14 · `routine_editor_screen.dart:484-485`, `:568`
- **AC-RUT-13-02 · Campos, en orden.** *Entonces*, de arriba abajo: el selector `editorStepType` (`Tipo` / `Type`); el selector de dos opciones `editorByTime` (`Por tiempo` / `By time`) y `editorByDistance` (`Por distancia` / `By distance`), con la elegida marcada; en modo tiempo, `editorMinutes` (`Min` / `Min`) y `editorSeconds` (`Seg` / `Sec`) lado a lado, y en modo distancia, `editorMeters` (`Metros` / `Meters`); una línea divisoria y el rótulo `editorOptionalTargets` (`Objetivos opcionales` / `Optional targets`, 12 dp, `text.tertiary`); `editorWattsMin` (`W mín` / `W min`) y `editorWattsMax` (`W máx` / `W max`) lado a lado; `editorTargetSpm` (`SPM objetivo` / `Target SPM`); `editorSplitMin` (`Split min` / `Split min`) y `editorSplitSec` (`Split seg` / `Split sec`) lado a lado; y **solo para un paso de serie**, una línea divisoria, el rótulo `editorProgressions` (`Progresiones (solo aplica en series)` / `Progressions (only applies to series)`) y tres campos con su ayuda, que se ve siempre que el campo está vacío, con foco o sin él, con la etiqueta arriba (en el legacy, solo con foco: DV-19): `editorProgressionSpm` (`Incremento SPM (+/rep)` / `SPM increment (+/rep)`, ayuda `Ej: 2` / `e.g: 2`), `editorProgressionWatts` (`Incremento Watts (+/rep)` / `Watts increment (+/rep)`, ayuda `Ej: 5` / `e.g: 5`) y `editorProgressionSplit` (`Cambio Split segundos (+/-/rep)` / `Split change in seconds (+/-/rep)`, ayuda `Ej: -2 (más rápido)` / `e.g: -2 (faster)`). Abajo, `cancel` (`Cancelar` / `Cancel`) y `confirm` (`Confirmar` / `Confirm`). [Editor-PM] · [Captura] CAP-14, CAP-15, CAP-24 · `routine_editor_screen.dart:574-736`
- **AC-RUT-13-03 · Selector de tipo.** *Entonces* ofrece exactamente los 4 tipos, en el orden `Calentamiento`, `Trabajo`, `Descanso`, `Enfriamiento` (`Warmup`, `Work`, `Rest`, `Cooldown`), con uno solo elegido. En la app son 4 opciones visibles a la vez con ícono y nombre; el legacy usa una lista desplegable (DV-09). [Editor-PM] · `routine_editor_screen.dart:574-581`
- **AC-RUT-13-04 · Cambiar de modo.** *Dado* Min `5` escrito, *cuando* se pasa a `Por distancia`, se escribe Metros `500` y se confirma, *entonces* el paso es de 500 m **sin** duración; *dado* lo mismo pero volviendo a `Por tiempo` antes de confirmar, *entonces* Min sigue diciendo `5` y el paso es de 300 s **sin** distancia. Al confirmar solo cuenta el modo elegido. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:531-540`, `:596-625`
- **AC-RUT-13-05 · Cómo se lee cada número.** *Entonces* cada campo numérico se recorta y se interpreta como entero con la regla del legacy (`int.tryParse` de Dart); lo que no es entero cuenta como **vacío**: [dotnet] · `routine_editor_screen.dart:535-553`; `<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm_shared/lib/integers_patch.dart:84-123`

  | Texto escrito | Valor |
  |---|---|
  | `` (vacío), `␣␣` | vacío |
  | `150`, `+150`, `␣150␣`, `0150` | 150 |
  | `-10` | −10 |
  | `0x10`, `0X10` | 16 (hexadecimal, como Dart; P-04) |
  | `abc`, `15a`, `1␣5`, `+`, `-`, `--2`, `+-2` | vacío |
  | `5.0`, `1,5`, `1e3` | vacío |
  | `−2` (signo menos tipográfico), `５` (dígito de ancho completo) | vacío |
  | un entero que pasa el tope de su campo, de cualquier largo (`10000` en `W mín`, `99999999999999999999`) | **fuera de rango** (AC-RUT-13-11; en el legacy, el de 20 cifras cuenta como vacío y `10000` vale 10 000). Cuenta su valor, por largo que sea: en el par del split, un resultado ≤ 0 es "sin split" y no un error (AC-RUT-13-08) |

- **AC-RUT-13-06 · Por tiempo (caso 20).** *Entonces* la duración es Min × 60 + Seg, con un campo vacío o no numérico como 0, y se guarda **siempre** una duración (aunque sea 0), con la distancia vacía: Min `5` → 300 s (`5m`); Seg `90` → 90 s (`1m 30s`; se pueden cargar segundos > 59); Min `1` Seg `-30` → 30 s; Min `abc` Seg `45` → 45 s; **los dos vacíos → 0 s** (`0s`, que en el entrenamiento dura un segundo: It. 4a). [dotnet] · [Editor-PM] · `routine_editor_screen.dart:536-540`
- **AC-RUT-13-07 · Por distancia (caso 20).** *Entonces* la distancia es Metros y la duración queda vacía: `500` → `500m`; `0` → `0m`; `-100` → `-100m`; **vacío o `abc` → sin meta** (`?`, un paso que en el entrenamiento nunca termina solo: It. 4a). [dotnet] · [Editor-PM] · `routine_editor_screen.dart:534-535`
- **AC-RUT-13-08 · Split (caso 20).** *Entonces* el split es Split min × 60 + Split seg (vacíos como 0) y **si da ≤ 0 no hay objetivo de split**: `2` y `5` → 125 (`02:05/500m`); `0` y `90` → 90 (`01:30/500m`); `1` y vacío → 60 (`01:00/500m`); `abc` y `59` → 59 (`00:59/500m`); `0` y `0`, los dos vacíos, o `-1` y `30` (−30) → sin split. "≤ 0 = sin split" gana sobre el tope de AC-RUT-13-11, que solo se controla si el split da más de 0: `-200` y vacío (−12 000) → sin split, sin error (como el legacy). [dotnet] · `routine_editor_screen.dart:557-562`
- **AC-RUT-13-09 · Sin validaciones de sentido (Q-25).** *Entonces* `Confirmar` acepta tal cual: vatios mín. 200 y máx. 100 → `200-100W`; vatios mín. 0 → `>0W`; vatios −10 a 5 → `-10-5W`; SPM 0 → `0 spm`; SPM −5 → `-5 spm`; solo vatios máx. → no se muestra nada de vatios; progresión SPM −1 → `SPM -1/rep`; un paso de 0 s o sin meta. Las únicas negativas son las de los topes (AC-RUT-13-11, -12). [dotnet] · AC-RUT-02-04
- **AC-RUT-13-10 · Precarga al editar.** *Entonces* el diálogo se abre con: el tipo; el modo **distancia si el paso tiene distancia**, si no tiempo; la duración partida en minutos (división entera que trunca) y segundos (resto **nunca negativo**, como Dart) sin ceros a la izquierda; los metros; vatios y SPM; el split partido igual; las progresiones (un 0 se ve `0`). Casos: [dotnet] · [Editor-PM] · [Captura] CAP-15 · `routine_editor_screen.dart:498-521`

  | Paso guardado | Precarga | Confirmar sin tocar nada |
  |---|---|---|
  | 90 s | tiempo, `1` y `30` | 90 s |
  | 45 s / 0 s / 3600 s | `0` y `45` / `0` y `0` / `60` y `0` | igual |
  | −90 s | `-1` y `30` | **−30 s** (etiqueta `30s` antes y después; paridad) |
  | −30 s | `0` y `30` | **30 s** (paridad) |
  | 500 m | distancia, Metros `500`, Min y Seg vacíos | 500 m |
  | sin meta (`?`) | **tiempo**, Min y Seg vacíos | **0 s** (`0s`; paridad) |
  | split 125, vatios 150-180, SPM 22 | `2` y `5`, `150`, `180`, `22` | igual |
  | en serie, progresiones SPM 2, W 0, split vacía | `2`, `0`, vacío | igual |

- **AC-RUT-13-11 · Topes por campo (Q-92).** *Cuando* se toca `Confirmar` con un valor fuera del tope de su campo, *entonces* el diálogo **queda abierto**, el campo (o el par Min/Seg, o el par de split) se marca en rojo y debajo dice `editorErrorRange` (`Fuera de rango: hasta ±{max}` / `Out of range: up to ±{max}`) con el tope del campo; el error se borra en cuanto se cambia ese campo y se vuelve a evaluar al confirmar. Debajo del tope todo es idéntico al legacy. Solo se controlan los campos que cuentan al confirmar (AC-RUT-13-04): en modo distancia, Min y Seg no; en modo tiempo, Metros no; en un paso suelto, las progresiones no (AC-RUT-13-12). Con algún error, el foco va al primer campo marcado y el contenido se desplaza hasta mostrarlo con su texto (04-design-notes §4.11). Los topes (en valor absoluto; los negativos se siguen aceptando, Q-25): [dotnet] · [Editor-PM] · [Captura] CAP-20a · FEATURE_INVENTORY Q-92

  | Campo | Qué se controla | Tope | `{max}` |
  |---|---|---|---|
  | `Min` + `Seg` | duración = Min × 60 + Seg | 359 999 s | `99:59:59` |
  | `Metros` | distancia | 999 999 m | `999999` |
  | `W mín`, `W máx` | cada uno | 9 999 W | `9999` |
  | `SPM objetivo` | SPM | 999 | `999` |
  | `Split min` + `Split seg` | split = min × 60 + seg, solo si da más de 0 (AC-RUT-13-08) | 5 999 s | `99:59` |
  | `Incremento SPM (+/rep)` | progresión de SPM | 999 | `999` |
  | `Incremento Watts (+/rep)` | progresión de vatios | 9 999 | `9999` |
  | `Cambio Split segundos (+/-/rep)` | progresión de split | 5 999 | `5999` |

  Ejemplos: `W mín` `10000` → `Fuera de rango: hasta ±9999`; Min `6000` → `Fuera de rango: hasta ±99:59:59`; `W mín` `9999` y `W máx` `-9999` → se aceptan (`9999--9999W`).
- **AC-RUT-13-12 · Tope en cada repetición (Q-92).** *Dado* un paso de una serie ×N (el N actual de esa serie), *cuando* se confirma, *entonces* también se controla el objetivo **ya progresado** de cada repetición (r = 0…N−1, AC-RUT-06): SPM + progresión × r dentro de ±999; vatios mín. y máx. + progresión × r dentro de ±9 999; split + progresión × r dentro de ±5 999. Si alguno se pasa, el diálogo queda abierto y debajo de la **progresión** que lo causa dice `editorErrorRangeRep` (`Fuera de rango en la repetición {rep}: hasta ±{max}` / `Out of range in repetition {rep}: up to ±{max}`), con la primera repetición (desde 1) que se pasa. Ejemplos con ×10: SPM 18 y progresión 200 → `Fuera de rango en la repetición 6: hasta ±999` (18 + 200 × 5 = 1018); vatios máx. 9 990 y progresión 10 → `… repetición 2: hasta ±9999`; SPM 18 y progresión 100 → se acepta (la 10.ª da 918). Una progresión sin su objetivo no se controla (no tiene efecto). Un paso suelto no se progresa y sus progresiones no se controlan. [dotnet] · [Editor-PM] · [Captura] CAP-20b · `routine.dart:50-63`
- **AC-RUT-13-13 · Teclado y foco.** *Entonces* todos los campos numéricos abren el teclado numérico del teléfono **con signo menos** (DV-15, P-05), y en la PC Tab recorre el diálogo en orden de lectura sin salir de él. [Device-And] · [Editor-PM] · `routine_editor_screen.dart:602`, `:611`, `:622` (`TextInputType.number`)
- **AC-RUT-13-14 · Cerrar sin cambios.** *Entonces* `Cancelar`, tocar fuera del diálogo, Escape y "Atrás" lo cierran **sin cambios** (lo escrito se pierde, como en el legacy). Fuera de los topes no hay validación: `Confirmar` siempre cierra y devuelve el paso. [Editor-PM] · `routine_editor_screen.dart:729-736`; `<flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:975-990`
- **AC-RUT-13-15 · Ventana baja.** *Dado* 640 × 360 y el diálogo de un paso de serie (con progresiones), *entonces* el contenido se desplaza dentro del diálogo, `Cancelar` y `Confirmar` se ven siempre, y nada desborda (sin "…" en las etiquetas: si no entran, se acomodan). [Editor-PM] · [Captura] CAP-23 · `routine_editor_screen.dart:569`
- **AC-RUT-13-16 · Vista previa del paso (Mejora EXR, agregado).** *Entonces* el diálogo muestra, mientras se escribe, cómo va a quedar la fila del paso en el editor (la fila de AC-RUT-12-02, con sus colores: borde, cuadrado con el ícono, nombre del tipo y subtítulo); al confirmar, la fila del editor es **exactamente** esa. No suma textos al catálogo. [Editor-PM] · [Captura] CAP-14, CAP-15

### 4.7 F-RUT-14 — Series en el editor

- **AC-RUT-14-01 · `Serie` (caso 17).** *Cuando* se toca `Serie`, *entonces*, sin abrir ningún diálogo, se agrega **al final** una serie nueva con identificador propio y dos pasos: `Trabajo` 120 s (`2m`) y `Descanso` 60 s (`1m`), ambos **×3** y sin objetivos. Dos toques seguidos agregan dos series distintas. [dotnet] · [Editor-PM] · [Captura] DEMO-04 · `routine_editor_screen.dart:110-133`
- **AC-RUT-14-02 · El bloque.** *Entonces* cada serie se ve como un bloque, con 8 dp de aire arriba y abajo: un **encabezado** con fondo `#0077B6` al 12 %, esquinas superiores redondeadas (12) y borde izquierdo de 3 dp en `#0077B6`, con el ícono `repeat` y `editorSeries` (`Serie` / `Series`) en `#00B4D8` negrita, el chip `×N` (AC-RUT-14-03) y, a la derecha, un tacho rojo con la ayuda `editorDeleteSeries` (`Eliminar serie` / `Delete series`); los **pasos** de la serie con 16 dp de sangría a la izquierda; y un **pie** con fondo `#0077B6` al 6 %, esquinas inferiores redondeadas, el mismo borde y el botón de texto `+ editorAddStepToSeries` (`Agregar paso a serie` / `Add step to series`) en `#00B4D8`, 12 dp. Comparable con `docs/image-04.png`: encabezado, sangría, pie, chip, tacho, lápiz y cruz en el mismo lugar. Como agregado (Mejora EXR §10.1), el borde de 3 dp y un tinte de `#0077B6` al 6 % siguen también junto a los pasos, de modo que el bloque se lee como un corchete continuo, y debajo del pie asoman una (×2) o dos (×3 o más) "hojas" dentro de los 8 dp de aire, sin mover nada (04-design-notes §4.7). Encabezado y pie de 48 dp: DV-12. [Editor-PM] · [Captura] CAP-10, CAP-11 · `routine_editor_screen.dart:371-445`
- **AC-RUT-14-03 · Chip `×N`.** *Entonces* el chip dice `editorRepeatCount` (`×{count}`, igual en es y en: `×3`) con el número en `#38CBEF` (`accent.onTonal`, el mismo cian más claro) sobre una cápsula de `#00B4D8` al 20 %, y se toca en un área de 48 dp. El legacy escribe el número en `#00B4D8`, que sobre su cápsula no llega a AA (DV-12); la cápsula crece con el número y nunca lo corta. Una serie sin repeticiones guardadas (`F-BORDE`, paso 8) muestra `×1`. *Dado* `F-RARA` con una serie ×0, muestra `×0` (paridad: esa serie no aporta pasos al expandir, AC-RUT-06-08). [Editor-PM] · [Captura] CAP-10, CAP-12 · `routine_editor_screen.dart:394-409`
- **AC-RUT-14-04 · Diálogo de repeticiones.** *Cuando* se toca `×N`, *entonces* se abre un diálogo con título `editorRepetitions` (`Repeticiones` / `Repetitions`), un campo numérico con etiqueta `editorAmount` (`Cantidad` / `Amount`) precargado con N (`1` si la serie no tiene) y con el foco puesto, y los botones `cancel` y `ok` (`OK` / `OK`). En la PC el número precargado queda seleccionado (escribir `4` deja `4`, como el legacy en escritorio); en el teléfono el cursor queda al final (paridad). [Editor-PM] · [Captura] CAP-17 · `routine_editor_screen.dart:151-177`
- **AC-RUT-14-05 · Reglas de repeticiones (caso 18).** *Cuando* se toca `OK`, *entonces*, con el texto recortado: [dotnet] · [Editor-PM] · `routine_editor_screen.dart:173`, `:179-186`

  | Texto | Resultado |
  |---|---|
  | `5`, `+5`, `05`, `␣5␣`, `0x5` | **×5 en todos los pasos de la serie** |
  | `abc`, vacío, `5.0`, `1e3`, `−2` (signo menos tipográfico) | **×1** (lo no numérico vale 1) |
  | `0`, `-2`, `-99999999999999999999` | **sin cambio** (queda el N anterior). Cuenta el valor, por largo que sea; en el legacy, el negativo de 20 cifras da ×1, porque no entra en su entero y vale como no numérico (`:173`) |
  | `999` | ×999 |
  | `1000`, `3000000000`, `99999999999999999999` | el diálogo **queda abierto** con `editorRepetitionsMax` (`Máximo {max}` / `Maximum {max}`, `Máximo 999`) (AC-RUT-14-06) |

  `Cancelar`, tocar afuera, Escape y "Atrás" no cambian nada.
- **AC-RUT-14-06 · Tope de repeticiones (Q-92 sobre Q-26).** *Entonces* las repeticiones van de 1 a **999** (P-01: el legacy no tiene máximo; una serie ×2 000 000 000 traba el inicio del entrenamiento). Además, *dado* que con el N nuevo algún paso de la serie quedaría con un objetivo progresado fuera de tope (AC-RUT-13-12), *cuando* se toca `OK`, *entonces* el diálogo queda abierto con `editorRepetitionsOutOfRange` (`Con ×{count}, un objetivo queda fuera de rango` / `With ×{count}, a target goes out of range`) y la serie no cambia. En `Escalera SPM` (SPM 18, progresión +2): `×491` se acepta (la última repetición da 18 + 2 × 490 = 998); `×492` (1000) y `×999` (2014) **no**: el diálogo queda abierto con `Con ×492, un objetivo queda fuera de rango`. Bajar N siempre se acepta. [dotnet] · [Editor-PM] · [Captura] CAP-21
- **AC-RUT-14-07 · Agregar paso a una serie (caso 19).** *Cuando* se toca `+ Agregar paso a serie` y se confirma el diálogo (modo nuevo, **con** progresiones), *entonces* el paso se inserta **después del último paso de esa serie** (no al final de la rutina) y con las mismas repeticiones que la serie. Con dos series contiguas, queda en la que se tocó. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:135-149`
- **AC-RUT-14-08 · Editar un paso de serie.** *Cuando* se edita un paso de una serie, *entonces* el diálogo muestra las progresiones y, al confirmar, el paso queda en la misma serie, con las mismas repeticiones y en el mismo lugar. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:90-108`
- **AC-RUT-14-09 · Borrar una serie.** *Cuando* se toca el tacho del encabezado, *entonces* la serie entera (encabezado, todos sus pasos y pie) se borra **al instante**, sin confirmación, y `Pasos (N)` baja en la cantidad de pasos que tenía. [dotnet] · [Editor-PM] · `routine_editor_screen.dart:196-200`
- **AC-RUT-14-10 · Serie que se vacía.** *Dado* una serie de 2 pasos, *cuando* se borran los dos con la cruz, *entonces* el bloque desaparece entero (no queda un encabezado ni un pie vacíos). Una serie puede tener **un solo paso** (como en `docs/image-04.png`). [dotnet] · [Editor-PM] · `routine_editor_screen.dart:275-295`
- **AC-RUT-14-11 · Series contiguas.** *Dado* dos series seguidas (×2 y ×3), *entonces* se ven como dos bloques separados por **16 dp** (los 8 de aire de cada bloque, AC-RUT-14-02: `routine_editor_screen.dart:377`, `:427`), cada uno con su chip, su tacho y su pie, y cambiar el `×N` de una no toca la otra. [dotnet] · [Editor-PM] · [Captura] CAP-13
- **AC-RUT-14-12 · Lo que no se puede.** *Entonces* no se puede mover un paso suelto a una serie, sacar un paso de una serie, reordenar series ni convertir un paso suelto en serie (BKL-12). [QA] · [Editor-PM]

### 4.8 F-HIS-17 — Base local: archivo, esquema y versiones

- **AC-HIS-17-01 · Archivo y lugar.** *Dado* un equipo sin la app nueva, *cuando* se abre el build normal por primera vez, *entonces* se crea la base `rower_app.db`, vacía, en la carpeta de datos de la app: en Windows `%USERPROFILE%\AppData\LocalLow\iguisoft\RowMate`; en Android, el almacenamiento **privado** de la app, que otras apps y la conexión USB con la PC no ven (como el legacy, que la guarda en la carpeta de bases del sistema). [Device-Win] · [Device-And] · [QA] · `database_service.dart:11`, `:20-28`
- **AC-HIS-17-02 · Esquema v8.** *Entonces* la base nueva tiene exactamente las 4 tablas del legacy v7 (`routines`, `interval_steps`, `workout_sessions`, `data_points`) con las mismas columnas, tipos, obligatorios y valores por defecto (`database_service.dart:75-139`; F-HIS-17 del inventario), **más** `workout_sessions.origin` (texto obligatorio, por defecto `app`) y un índice de `data_points` por sesión y tiempo, con versión de esquema **8** (P-02 del Architect, ADR 008). QA lo comprueba abriendo el archivo con una herramienta de SQLite. Sesiones y telemetría quedan vacías hasta la It. 4a. [dotnet] · [QA] · ARCHITECTURE §9.2
- **AC-HIS-17-03 · La base del legacy no se toca (Q-11, R-14).** *Dado* la PC con la app legacy y sus datos, *cuando* se usa la app nueva (crear, editar y borrar rutinas), *entonces* la base y las preferencias del legacy siguen con el mismo contenido y la misma fecha de modificación, y la app nueva no las abrió (no se importa nada). La app legacy abre después con todas sus rutinas. [Device-Win] · [QA] · FEATURE_INVENTORY Q-11
- **AC-HIS-17-04 · Abrir una base existente.** *Dado* una base v8 con rutinas, *cuando* se abre la app, *entonces* la usa tal cual: no la recrea, no la vacía y no le cambia nada hasta que el usuario edita. [dotnet] · [Editor-PM]
- **AC-HIS-17-05 · Base ilegible o de otra versión.** *Dado* `F-ROTA`, una base con versión de esquema mayor que 8 (por ejemplo, de un build más nuevo) o una base que la app no reconoce como suya (versión 1 a 7, como la del legacy, o sin versión pero con tablas: la app nunca las crea y no hay importación, Q-11; DT-09), *cuando* se abre la app, *entonces* Rutinas muestra el error de carga (AC-RUT-09-04), la app no se cae, y el archivo **no se borra, no se reemplaza y no se modifica** (QA compara su contenido antes y después). [dotnet] · [Editor-PM]
- **AC-HIS-17-06 · Sin bloquear la pantalla.** *Dado* `F-LISTA` más 50 rutinas de 20 pasos, *entonces* en la PC la lista aparece en menos de 1 s desde que la app abre, y ni la carga ni un guardado frenan las animaciones ni la escritura (sin tirones a la vista; ANIMATION_BIBLE §0). [Editor-PM] · [Device-Win]

### 4.9 F-HIS-20 — Base separada en modo simulador

- **AC-HIS-20-01 · Nombre según el modo.** *Entonces* con el modo simulador la base es `rower_app_dev.db` y sin él `rower_app.db`, en la misma carpeta de datos y con el mismo esquema (AC-HIS-17-02). Ningún camino del build normal abre la base del simulador ni al revés. [dotnet] (composición) · [QA] · [Device-Win] · `database_service.dart:10-11`
- **AC-HIS-20-02 · No se mezclan.** *Dado* `Escalera SPM` creada con el build de simulador, *cuando* se abre el build normal, *entonces* no aparece; *dado* una rutina creada con el build normal, no aparece en el de simulador. Lo mismo en el Editor con y sin `RowMate → Modo simulador`. [Device-Win] · [Editor-PM] (dos composiciones sobre la misma carpeta de prueba)
- **AC-HIS-20-03 · Mismo producto.** *Entonces* con el simulador las reglas, el esquema y las pantallas de Rutinas son las mismas; el único aviso visible del modo es la cinta `SIMULADOR` (no hay ningún texto de "base de prueba"). [Editor-PM] · [Captura] CAP-04
- **AC-HIS-20-04 · Las pruebas no tocan las bases del usuario.** *Entonces* ninguna prueba de `dotnet`, de Unity ni del arnés de capturas abre, crea ni modifica `rower_app.db` ni `rower_app_dev.db` en la carpeta de datos del usuario: QA compara el contenido y la fecha de las dos antes y después de correr todo (§0.4). **Única excepción: CAP-27** (DT-12, QA-G3-06), porque el build normal no acepta otra carpeta de datos. Esa toma: se hace solo a pedido explícito, nunca dentro de las corridas habituales ni del CI; no arranca si la app o el Editor están abiertos o si la base está en uso; antes de tocar nada guarda una copia de la base del build normal (con sus archivos auxiliares) y comprueba que la copia es idéntica; muestra una rutina **sintética**, nunca las del usuario (el repositorio es público); al terminar, también si falla, deja la base como estaba, con el mismo contenido y la misma fecha (o sin base, si no existía), y lo comprueba; si no puede, conserva la copia y dice cómo recuperarla. La comparación de QA de antes y después incluye esta toma. [QA]

### 4.10 F-APP-03 — Navegación secundaria: pantallas apiladas, diálogos y avisos

- **AC-APP-03-01 · El editor se apila.** *Entonces* el editor se abre desde `+`, desde una tarjeta y desde `⋮` → `Editar`, a pantalla completa y **por encima de la barra de pestañas**, que no se ve ni se puede tocar mientras está abierto. Al cerrarse, vuelve Rutinas con la barra y la lista donde estaban. Mientras el editor se está cerrando ya no responde a toques ni al teclado, como en el legacy, donde la pantalla que sale ignora los toques y suelta el foco (`<flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:1154-1156`, `:1220-1222`). [Editor-PM] · [Captura] CAP-09 · `routines_screen.dart:18-21`, `:76-80`, `:92-95`; F-APP-02.8
- **AC-APP-03-02 · Volver.** *Entonces* el editor se cierra con la flecha de su barra superior o con "Atrás" de Android, siempre sin guardar ni preguntar (AC-RUT-11-08). Con un diálogo o un menú abiertos, "Atrás" y Escape cierran **solo** ese diálogo o menú (como cancelar) y el editor sigue. Escape con el editor solo (sin diálogo) **no hace nada**: en el legacy Escape cierra lo que se cierra tocando afuera (diálogos y menús), no las pantallas (P-08). Tampoco con el foco en un campo: el texto no se revierte y el campo sigue con el foco; con el menú del clic derecho abierto, Escape cierra solo ese menú (`<flutter-sdk>/packages/flutter/lib/src/widgets/editable_text.dart:5642-5648`). [Editor-PM] · [Device-And] · `<flutter-sdk>/packages/flutter/lib/src/widgets/routes.dart:975-990`
- **AC-APP-03-03 · Diálogos.** *Entonces* los diálogos (eliminar rutina, paso, repeticiones) son modales y centrados sobre un velo oscuro, con título, contenido y acciones abajo a la derecha: la secundaria de texto (`Cancelar`) y la principal rellena (`Confirmar`, `OK`, o `Eliminar` en rojo). Tocar el velo equivale a cancelar. Mientras hay uno abierto, lo de atrás no responde y Tab recorre solo el diálogo. Un diálogo que se está cerrando ya no responde: dos toques seguidos en `Confirmar`, `OK` o `Eliminar` actúan una sola vez (AC-APP-03-01). [Editor-PM] · [Captura] CAP-08, CAP-14, CAP-17 · F-APP-03.3
- **AC-APP-03-04 · Menús emergentes.** *Entonces* el menú `⋮` se abre junto a su botón, por encima de todo menos la capa del simulador, y se cierra al elegir, al tocar afuera, con Escape o con "Atrás". Un menú que se está cerrando ya no responde: dos toques seguidos en `Eliminar` abren un solo diálogo (AC-APP-03-01). [Editor-PM] · [Captura] CAP-07
- **AC-APP-03-05 · Avisos.** *Entonces* los avisos (`editorNameEmpty`, `editorNoSteps`, `editorStepOutOfRange`, `editorSaveError`, `routineDeleteError`) aparecen abajo, sin acción, se ven **4 s** y se van solos (el `SnackBar` del legacy: `<flutter-sdk>/packages/flutter/lib/src/material/snack_bar.dart:29`); se leen enteros (no los tapa la llave ni ellos tapan la llave). *Dado* un aviso a la vista, *cuando* se pide otro (el mismo o uno distinto), *entonces* el nuevo **reemplaza** al visible y cuenta sus 4 s desde ahí: el legacy los encola (tres toques en `Guardar` con el nombre vacío dan 12 s de avisos) (DV-14, P-03). [Editor-PM] · [Captura] CAP-18, CAP-19 · [Captura] VID-03
- **AC-APP-03-06 · Ayudas emergentes.** *Entonces* la flecha de volver (`Atrás` / `Back`), el `⋮` (`Mostrar menú` / `Show menu`) y el tacho de la serie (`Eliminar serie` / `Delete series`) muestran su ayuda: en la PC, apenas el puntero se apoya encima, y se va 0,1 s después de salir; en el teléfono, al mantener presionado, durante 1,5 s. El mismo texto queda como su nombre para los lectores de pantalla; que un lector lo lea **no se verifica en esta iteración**: el motor no expone la interfaz de la app a los lectores de pantalla del sistema (en el legacy sí se leen), así que esa parte queda NO VERIFICABLE y pasa a la It. 14 (DT-05, §13). Los demás íconos no tienen ayuda (paridad). [Editor-PM] · `<flutter-sdk>/packages/flutter/lib/src/material/tooltip.dart:392-394`
- **AC-APP-03-07 · La capa del simulador, encima de todo (completa AC-BLE-16-01).** *Dado* el modo simulador, *entonces* la cinta `SIMULADOR` y la llave (o el panel abierto) quedan **por encima** del editor, de los diálogos con su velo, del menú `⋮` y de los avisos. La cinta tapa en parte la flecha de volver, como en `docs/image-04.png`, pero no la bloquea: tocar la flecha, aun debajo de la cinta, vuelve. Con un diálogo abierto, la llave abre el panel y el panel funciona. [Editor-PM] · [Captura] CAP-08, CAP-09, DEMO-02 · `lib/main.dart:108-110`
- **AC-APP-03-08 · Transiciones.** *Entonces* abrir y cerrar el editor, los diálogos, el menú y los avisos tiene la transición de ANIMATION_BIBLE §2.2 (entra desde la derecha con fundido; el diálogo crece de 0,96 a 1 sobre el velo; el aviso sube 16 dp), sin saltos y a la frecuencia de la pantalla; con "reducir movimiento", fundidos cortos. El legacy usa la transición estándar de cada sistema (Mejora EXR). [Editor-PM] · [Captura] VID-02

### 4.11 También toca

**F-APP-01 (arranque; se cerró en la It. 2).**

- **AC-APP-01-09 · Rutinas cargadas al arrancar.** *Dado* `F-LISTA` y la app recién abierta en Dispositivo, *cuando* el usuario va a Rutinas después de que terminó la carga (sin haber visitado la pestaña), *entonces* ve la lista enseguida, sin indicador de carga. La carga no demora ni cambia lo que muestra Dispositivo (AC-APP-01-01 a -05 siguen igual). [Editor-PM] · `lib/main.dart:83`; `routines_provider.dart:12-14`

**F-APP-05 (tema y estilos; se cerró en la It. 2, vuelve en la 14). Dos criterios nuevos, por QA-V2-08 y QA-V2-13.**

- **AC-APP-05-10 · Barra de título de Windows según el sistema (QA-V2-08).** *Dado* Windows con el modo de apps en **Oscuro**, *cuando* se abre la app (build normal o de simulador), *entonces* la barra de título de la ventana es **oscura**; con el modo **Claro** (o sin el valor en el registro), clara. *Dado* la app abierta, *cuando* el usuario cambia el modo, *entonces* la barra se actualiza sin reiniciar: a más tardar 2 s después, y en el acto si la ventana vuelve a tener el foco, como pasa al cambiar el modo desde Configuración (el legacy lo hace en el instante en que recibe el aviso de cambio de colores del sistema; DD-01). El contenido de la app es siempre oscuro (AC-APP-05-01). [Device-Win] · [Captura] CAP-27 · `windows/runner/win32_window.cpp:21-27`, `:147`, `:216-217`, `:275-285`
- **AC-APP-05-11 · Barra de desplazamiento en el teléfono (QA-V2-13).** *Dado* un perfil de teléfono (TV, TH) o Android, *entonces* la lista de Rutinas, el editor y el contenido de los diálogos no muestran una barra de desplazamiento fija en reposo (como el legacy en Android); en la PC se comportan como en la It. 2. [Editor-PM] · [Captura] CAP-04, CAP-10 · [Device-And]

---

## 5. Criterios de iteraciones anteriores que se verifican acá

Si fallan, se reabren en esta iteración. AC-BLE-16-02 además **cambia** (DV-13): desde esta iteración vale el texto de abajo, que reemplaza al de la [It. 2](../02-dispositivo-simulador/01-po-spec.md) (§4.10); AC-BLE-16-03 no cambia.

- **AC-BLE-11-02 · Cabecera (reabierto por QA-V2-01).** *Dado* el remo simulado conectado, *cuando* se toca `Desconectar` o `Simular desconexión`, *entonces* durante la salida de la vista conectada la cabecera sigue diciendo `Remo simulado` (nunca `Rower`): 60 ms después del toque, `Remo simulado`. Es la **primera tarea** de la iteración, con la prueba PlayMode que propuso QA, y se verifica antes de construir encima. [Editor-PM] · [Captura] (VID-03 de la It. 2, regenerado) · [05-qa-verification de la It. 2](../02-dispositivo-simulador/05-qa-verification.md) §6
- **AC-BLE-16-01 · Cinta SIMULADOR por encima de pantallas apiladas y diálogos.** Lo que faltaba (el entrenamiento a pantalla completa es de la It. 4a) se verifica con AC-APP-03-07. [Editor-PM] · [Captura] CAP-08, CAP-09
- **AC-BLE-16-02 · Botón de la llave (cambia por DV-13).** *Entonces*, con el panel cerrado (como arranca siempre), hay un botón flotante chico con el ícono `build` (llave) a 16 dp del borde derecho y 16 dp por encima de la barra de pestañas (DV-07; legacy: 96 dp desde abajo, sobre una barra de 80 dp). **En Rutinas**, mientras es la pestaña visible y ninguna pantalla apilada la tapa, la llave sube y queda **16 dp por encima del `+`** (88 dp sobre la barra), con el borde derecho alineado con el del `+`, en cualquier estado de la pantalla (cargando, vacío, lista o error). En las otras pestañas, y con el editor abierto, queda a 16 dp sobre la barra; con el editor abierto la barra no se ve, pero la llave sigue a esa altura (como en el legacy, donde la llave no se mueve al apilar una pantalla). Pasa de un lugar al otro con un desplazamiento vertical corto y sin saltos al cambiar de pestaña, cuando el editor termina de entrar y cuando empieza a salir (con "reducir movimiento", en el acto), y se puede tocar mientras se mueve. Tocarla abre el panel **siempre en el mismo lugar**, en todas las pestañas: anclado abajo a la derecha, a 16 dp del borde derecho y 16 dp sobre la barra, con el tamaño de AC-BLE-16-03, que no cambia. En Rutinas, el panel abierto tapa el `+`, como en el legacy, donde el panel se abre en el lugar de la llave, encima del `+`; al cerrarlo, la llave vuelve a su lugar sobre el `+`. Mientras el panel está abierto, la llave no se ve. [Editor-PM] (con `-Simulator`) · [Captura] CAP-01, CAP-06, CAP-09 · `lib/core/dev/simulator_overlay.dart:29`, `:40-49`; 04-design-notes §4.13, §5.8
- **AC-RUT-01-04 · Íconos en el editor.** Con AC-RUT-12-02: `whatshot`, `fitness_center`, `pause_circle_outline` y `ac_unit` en el cuadrado de cada fila. [Editor-PM] · [Captura] CAP-12
- **AC-RUT-01-05 · Tipo desconocido, efecto visible.** Con AC-RUT-08-08. [Editor-PM]
- **AC-RUT-04-06 · Subtítulo del editor en pantalla.** Con AC-RUT-12-06 (dos espacios a cada lado del punto medio). [Editor-PM] · [Captura] CAP-12
- **AC-APP-09-07 · Textos del sistema (P-06, aceptada).** En la PC el menú de texto lo dibuja la app, con textos del catálogo en el idioma de la app (AC-RUT-11-09). En Android el teclado y su menú de copiar y pegar son del sistema y siguen su idioma (AC-RUT-11-10). *Dado* Android con el sistema en portugués (la app en inglés, AC-APP-09-03), el menú dice `Copiar` / `Colar` y la app dice `New routine`. [Device-Win] · [Device-And] (NO VERIFICABLE sin módulo y teléfono) · [Apple] It. 10b
- **AC-APP-02-08 · Contenido provisorio.** Rutinas deja de ser provisoria (este spec). Workout (It. 4a) e Historial (It. 9) siguen con el título y el ícono apagado. [Editor-PM] · [Captura] CAP-01
- **AC-APP-05-05 · Letra del sistema 1,3×, en las pantallas nuevas.** Lista, editor y diálogos sin cortes ni desbordes; lo que no entra se desplaza. [Editor-PM] · [Captura] CAP-25
- **AC-APP-05-07 · Teclado y foco, en las pantallas nuevas.** Tab recorre `+`, tarjetas, `⋮`, la flecha, `Guardar`, campos, `+ Paso`, `Serie`, chips, lápices, cruces, tachos y pies con el anillo de foco; Enter o Espacio activan el control enfocado. En un campo de texto, Enter no guarda ni confirma el diálogo: en un campo de una línea le quita el foco al campo y en la descripción agrega un salto de línea; Tab, también desde la descripción, pasa al control siguiente y no escribe una tabulación (paridad: `<flutter-sdk>/packages/flutter/lib/src/widgets/editable_text.dart:3738-3753`, `:3914-3947`; `widgets/app.dart:1275-1276`). [Editor-PM] · [Device-Win]
- **AC-APP-05-08 · Reducir movimiento, en las pantallas nuevas.** Transiciones del editor, diálogos, avisos, filas y la mini-barra pasan a fundidos cortos. [Editor-PM]

---

## 6. Casos de test legacy

**No hay** tests legacy del área (ROADMAP §5): el editor, la lista y la base no tienen ninguno. Los criterios nuevos de ROADMAP It. 3 (casos 16 a 21 de [routines-models §5.3](../../product/inventory/routines-models.md), persistencia y reapertura, base separada, v8 y Q-92) quedan cubiertos así:

| Caso | AC |
|---|---|
| 16 · nombre `␣␣␣` → `editorNameEmpty`; sin pasos → `editorNoSteps`; `␣␣Mi rutina␣␣` → `Mi rutina` | AC-RUT-11-05, -06 |
| 17 · `Serie` → Trabajo 120 s + Descanso 60 s ×3 al final | AC-RUT-14-01 |
| 18 · repeticiones `abc` → ×1; `0` o `-2` → sin cambio; `5` → ×5 en todos | AC-RUT-14-05 |
| 19 · agregar paso a una serie → después de su último paso, con sus repeticiones | AC-RUT-14-07 |
| 20 · diálogo: tiempo vacío → 0 s; distancia vacía → `?`; split 0:00 → sin split | AC-RUT-13-06, -07, -08 |
| 21 · crear arriba, editar sin reordenar, borrar con confirmación y el nombre | AC-RUT-09-02, AC-RUT-10-07 |
| Persistencia y reapertura | AC-RUT-08-01 a -09 |
| Base `rower_app_dev.db` separada | AC-HIS-20-01 a -04 |
| Base v8 propia | AC-HIS-17-01 a -06 |
| Q-92 | AC-RUT-13-11, -12, AC-RUT-14-06, AC-RUT-11-05 (3) |

---

## 7. Capturas, demo y videos

### 7.1 Reglas

Las mismas de la It. 2 ([01-po-spec §7.1](../02-dispositivo-simulador/01-po-spec.md)): las renderiza Unity con la UI real; van a `docs/migration/iterations/03-rutinas/snapshots/` como `CAP-nn-<tema>.png`, `DEMO-nn-<tema>.png` y `VID-nn-<tema>.mp4`, con `manifest.json` e `INDEX.md`; cada una representa el tamaño en dp que indica; salvo que diga otra cosa: español, modo simulador, simulador sin ruido, reloj de prueba y la base de prueba indicada (§0.4). QA verifica "Qué tiene que verse" y el Designer el aspecto. Además: **ninguna captura escribe en las bases del usuario** (AC-HIS-20-04), salvo CAP-27 con las condiciones de esa AC; y ni el `manifest.json` ni el `INDEX.md` llevan rutas de la PC (el repositorio es público).

### 7.2 Tamaños

Los de la It. 2: **TV** 411 × 914 dp (1080 × 2400 a 2,625), **TH** 914 × 411 dp, **L640** 640 × 360 dp, **W1280** 1280 × 720 dp.

### 7.3 Lista

| ID | Tamaño | Estado (cómo se llega) | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | TV | Rutinas con `F-VACIA` | Barra `Rutinas` sin acciones; centrado, el ícono `fitness_center` grande y muy tenue, `No hay rutinas todavía` y debajo, más chico, `Presioná + para crear una` (los dos legibles, AC-RUT-10-02); el `+` abajo a la derecha **entero**, y la llave del simulador 16 dp por encima, **sin tocarlo** y con los bordes derechos alineados (AC-BLE-16-02); cinta `SIMULADOR`; barra de pestañas con Rutinas elegida |
| **CAP-02** | TV | Rutinas con la base lenta, mientras carga (pasados los 120 ms de gracia y su fundido, AC-RUT-10-02) | Solo el indicador circular centrado; ningún texto; el `+` visible |
| **CAP-03** | TV | Rutinas con `F-ROTA` | `No se pudieron cargar las rutinas` y el botón `Reintentar`, centrados; el `+` visible; ningún indicador de carga |
| **CAP-04** | TV | Rutinas con `F-LISTA` | 4 tarjetas en este orden: `Escalera SPM` (`1 serie · 1 descanso`, mini-barra de 8), `Pirámide 500` (descripción `Bajar 2 s el split en cada vuelta` arriba del resumen `2 series · 1 descanso`, mini-barra de 14), `Solo calentamiento` (`1 paso`, 1 tramo amarillo), `Intervalos 4×` (`4 series · 3 descansos`, 7 tramos rosa y verde alternados); cada una con ícono `#00B4D8`, nombre en seminegrita y `⋮`; sin barra de desplazamiento fija |
| **CAP-05** | TV | Igual que CAP-04, en inglés | `Routines`; `1 interval · 1 rest`, `2 intervals · 1 rest`, `1 step`, `4 intervals · 3 rests`; los nombres y la descripción, como se escribieron |
| **CAP-06** | L640 | `F-LISTA` más 26 rutinas `Rutina 01`…`Rutina 26`, desplazada al final | La última tarjeta entera y por encima del `+`; nada desborda; cinta y llave visibles; la llave no tapa el `+` |
| **CAP-07** | TV | `F-LISTA`, `⋮` de `Escalera SPM` abierto | Menú junto al botón con `Editar` y `Eliminar` (en rojo); la cinta por encima del menú |
| **CAP-08** | TV | `F-LISTA`, `⋮` → `Eliminar` en `Escalera SPM` | Velo oscuro; diálogo centrado `Eliminar rutina`, `¿Eliminar "Escalera SPM"?`, `Cancelar` de texto y `Eliminar` relleno `#D32F2F` con texto blanco; la cinta y la llave **por encima del velo** |
| **CAP-09** | TV | `+` con `F-VACIA` | Barra superior: flecha de volver (en parte bajo la cinta), `Nueva rutina` y `Guardar` a la derecha; campos `Nombre de la rutina` y `Descripción (opcional)` (2 líneas) vacíos; línea; `Pasos (0)`, `+ Paso`, `Serie`; centrado, `Todavía no hay pasos.` y `Presioná "Paso" o "Serie".` en dos líneas; **sin** barra de pestañas; la llave visible a 16 dp sobre el lugar de la barra, que no se ve (AC-BLE-16-02) |
| **CAP-10** | TV | Editor de `Escalera SPM` | `Editar rutina`; nombre `Escalera SPM`; `Pasos (4)`; `Calentamiento` `5m` (amarillo); bloque de serie: encabezado azul con `Serie`, chip `×3` y tacho; `Trabajo` `5m␣␣·␣␣18 spm · ` + ícono de tendencia + `SPM +2/rep` (rosa) y `Descanso` `2m` (verde), con sangría; pie `+ Agregar paso a serie`; `Enfriamiento` `5m` (borde `#118AB2`) después del pie; cada fila con su borde de color, su ícono, el lápiz y la cruz roja; títulos e íconos en la variante legible de su tipo y el número del chip en el cian claro (AC-RUT-12-02, AC-RUT-14-03) |
| **CAP-11** | W1280 | Igual que CAP-10 | La misma estructura a lo ancho de la ventana, comparable con `docs/image-04.png` (encabezado, sangría, pie, chip, tacho, lápiz y cruz en el mismo lugar) |
| **CAP-12** | TV | Editor de `F-BORDE` (desplazado si hace falta: 2 tomas a/b) | `Pasos (11)` y los 11 títulos y subtítulos de AC-RUT-12-06, al carácter (con `␣␣·␣␣`), la serie `×1` y la serie `×2`; los subtítulos largos en dos líneas, sin "…" |
| **CAP-13** | TV | Editor de `Pirámide 500` más una serie ×2 [trabajo 60 s] agregada justo después de la ×4 | Descripción en 2 líneas; serie `×4` con 3 pasos (`500m␣␣·␣␣150-180W · 02:05/500m · ` + ícono + `W +10/rep, Split -2s/rep`, `250m␣␣·␣␣30 spm`, `1m 30s`) y, a continuación, la serie `×2` como **otro** bloque con su encabezado y su pie, con **16 dp** entre el pie de la primera y el encabezado de la segunda (8 + 8, como el legacy: AC-RUT-14-11) |
| **CAP-14** | TV | `+ Paso` | `Nuevo paso`; 4 tipos con `Trabajo` elegido; `Por tiempo` elegido; `Min` y `Seg` vacíos; `Objetivos opcionales`; `W mín` / `W máx`; `SPM objetivo`; `Split min` / `Split seg`; **sin** `Progresiones`; vista previa `Trabajo` `0s`; `Cancelar` y `Confirmar`; la cinta por encima del velo |
| **CAP-15** | TV (2 tomas) | Lápiz de `Trabajo` en `Escalera SPM`. a) arriba; b) desplazado al final | a) `Editar paso`, `Trabajo`, `Por tiempo`, `5` y `0`, `18`; vista previa `5m␣␣·␣␣18 spm · ` + ícono + `SPM +2/rep`. b) `Progresiones (solo aplica en series)`, `Incremento SPM (+/rep)` `2`, `Incremento Watts (+/rep)` vacío con `Ej: 5`, `Cambio Split segundos (+/-/rep)` vacío con `Ej: -2 (más rápido)` (las ayudas, sin foco en esos campos: DV-19); botones visibles |
| **CAP-16** | TV | `+ Paso`, `Por distancia`, Metros `500` | `Por distancia` elegido; `Metros` `500`; sin `Min`/`Seg`; vista previa `Trabajo` `500m` |
| **CAP-17** | TV | Chip `×3` de `Escalera SPM` | Diálogo `Repeticiones`, campo `Cantidad` con `3` (seleccionado si la toma es de escritorio) y foco; `Cancelar` y `OK` |
| **CAP-18** | TV | Editor nuevo con nombre `␣␣␣`, `Guardar` | Aviso `El nombre no puede estar vacío` abajo, entero y legible; la llave no lo tapa; el editor sigue abierto |
| **CAP-19** | TV | Editor nuevo con nombre `Prueba` y sin pasos, `Guardar` | Aviso `Agregá al menos un paso` |
| **CAP-20** | TV (2 tomas) | a) `+ Paso` con `W mín` `10000` y `Confirmar`; b) paso de una serie ×10 con SPM `18` e `Incremento SPM` `200` y `Confirmar` | a) `W mín` en rojo y debajo `Fuera de rango: hasta ±9999`; el diálogo abierto. b) el campo de incremento en rojo y debajo `Fuera de rango en la repetición 6: hasta ±999` |
| **CAP-21** | TV | Chip `×3` de `Escalera SPM`, `Cantidad` `1000`, `OK` | El diálogo sigue abierto con `Máximo 999` debajo del campo |
| **CAP-22** | L640 | Editor de `Escalera SPM` | Barra superior y campos enteros; la lista de pasos se desplaza; nada desborda; sin barra de pestañas |
| **CAP-23** | L640 | Lápiz de `Trabajo` en `Escalera SPM` | Diálogo con el contenido desplazable; título, `Cancelar` y `Confirmar` siempre visibles; ninguna etiqueta cortada |
| **CAP-24** | TV (2 tomas) | En inglés: a) editor de `Escalera SPM`; b) su diálogo de `Trabajo` desplazado al final | a) `Edit routine`, `Save`, `Routine name`, `Description (optional)`, `Steps (4)`, `Step`, `Series`, `Warmup`, `Work`, `Rest`, `Cooldown`, `×3`, `Add step to series`. b) `Edit step`, `Optional targets`, `W min`, `W max`, `Target SPM`, `Split min`, `Split sec`, `Progressions (only applies to series)`, `SPM increment (+/rep)`, `e.g: 5`, `e.g: -2 (faster)`, `Cancel`, `Confirm` |
| **CAP-25** | TV (2 tomas) | Letra 1,3×: a) `F-LISTA`; b) editor de `Escalera SPM` | Textos más grandes; nada cortado ni fuera de su tarjeta, fila o bloque; lo que no entra se desplaza |
| **CAP-26** | W1280 | Editor con el nombre `Escalera SPM` seleccionado entero, portapapeles con texto, clic derecho en el campo | Menú con `Cortar`, `Copiar` y `Pegar` (sin `Seleccionar todo`, porque ya está todo seleccionado) |
| **CAP-27** | W1280 (ventana entera) | **Build normal**, Windows con el modo de apps en Oscuro, pestaña Rutinas con una rutina sintética, con las condiciones de AC-HIS-20-04 (DT-12). Si Windows no está ya en Oscuro, la toma no se hace y queda NO VERIFICABLE: nunca se cambia el tema del usuario | Barra de título **oscura** con `RowMate`; sin cinta ni llave; la rutina del build normal (no las del simulador) |
| **CAP-28** | TV | Base que falla al guardar; editor de una rutina válida, `Guardar` | Aviso `No se pudo guardar la rutina`; el editor sigue abierto con todo lo escrito |

### 7.4 Secuencia de la demo

W1280, español, modo simulador, `F-VACIA` y reloj de prueba. Cada toma sigue a la anterior sin rearmar nada, salvo DEMO-10 (la app se vuelve a armar sobre la misma base).

| ID | Paso | Qué tiene que verse |
|---|---|---|
| **DEMO-01** | Rutinas | Estado vacío (como CAP-01, a lo ancho) |
| **DEMO-02** | `+`, nombre `Escalera SPM` | `Nueva rutina`; el nombre escrito; `Pasos (0)` y el texto vacío; la cinta sobre la flecha |
| **DEMO-03** | `+ Paso`: `Calentamiento`, `Por tiempo`, Min `5` | Diálogo con `Calentamiento` elegido y la vista previa `Calentamiento` `5m` |
| **DEMO-04** | `Confirmar` y `Serie` | `Pasos (3)`: `Calentamiento` `5m` y el bloque `Serie` `×3` con `Trabajo` `2m` y `Descanso` `1m` |
| **DEMO-05** | Lápiz de `Trabajo`: Min `5`, `SPM objetivo` `18`, `Incremento SPM (+/rep)` `2` | Diálogo `Editar paso` con progresiones y la vista previa `5m␣␣·␣␣18 spm · ` + ícono + `SPM +2/rep` |
| **DEMO-06** | `Confirmar`; lápiz de `Descanso` Min `2`; `+ Paso` `Enfriamiento` Min `5` | `Pasos (4)`: `Calentamiento` `5m`; serie `×3` con `Trabajo` `5m␣␣·␣␣18 spm · …SPM +2/rep` y `Descanso` `2m`; `Enfriamiento` `5m` después del pie |
| **DEMO-07** | `Guardar` | Lista con `Escalera SPM`, `1 serie · 1 descanso` y la mini-barra de 8 tramos (amarillo, rosa, verde, rosa, verde, rosa, verde, azul) |
| **DEMO-08** | Tocar la tarjeta; descripción `Sube 2 spm por repetición`; `×3` → `4` → `OK` | `Editar rutina` con la descripción y el chip `×4` |
| **DEMO-09** | `Guardar` | La tarjeta en el mismo lugar, con `Sube 2 spm por repetición` arriba de `1 serie · 1 descanso` y la mini-barra de 10 |
| **DEMO-10** | Se cierra la app y se vuelve a abrir sobre la misma base; Rutinas | Lo mismo que DEMO-09 |
| **DEMO-11** | `⋮` → `Eliminar` | Diálogo `Eliminar rutina` / `¿Eliminar "Escalera SPM"?` con el velo; cinta y llave encima |
| **DEMO-12** | `Eliminar` | Vuelve el estado vacío |

### 7.5 Videos

| ID | Tamaño y duración | Qué se hace | Qué tiene que verse |
|---|---|---|---|
| **VID-01** | TV, 10 s | En el editor: `+ Paso` y `Confirmar`; `Serie`; cruz de un paso; tacho de la serie | El diálogo entra y sale con su transición; la fila nueva crece desde 0 y entra con fundido; el bloque de serie se estira con el mismo resorte; lo borrado se desvanece y colapsa; nada salta (ANIMATION_BIBLE §2.3) |
| **VID-02** | TV, 8 s | Lista → tocar `Escalera SPM` → flecha de volver → `⋮` → `Eliminar` → `Eliminar` | El editor entra desde la derecha y sale al revés; el menú y el diálogo aparecen sobre el velo; la tarjeta borrada colapsa y las de abajo suben sin saltar |
| **VID-03** | TV, 8 s | Editor nuevo vacío: `Guardar` tres veces, una por segundo | Un solo aviso `El nombre no puede estar vacío`, que se reinicia en cada toque (no se encolan) y se va 4 s después del último |

---

## 8. Qué se verifica dónde

| Dónde | Qué |
|---|---|
| **[dotnet]** | Repositorio de rutinas sobre SQLite real: alta, edición, baja, lectura por id, orden y fechas, series, todo o nada, tipo desconocido, base ilegible o de otra versión (AC-RUT-08-01 a -08, AC-HIS-17-02, -04, -05); esquema v8 y nombre de base por modo (AC-HIS-17-02, AC-HIS-20-01); reglas del editor que se pueden aislar: lectura de números, tiempo, distancia, split, precarga, topes y repeticiones (AC-RUT-13-04 a -12, AC-RUT-14-05, -06), operaciones sobre la plantilla (agregar, serie, agregar a serie, editar, borrar, borrar serie, contar: AC-RUT-11-03, AC-RUT-12-01, -03, -04, AC-RUT-14-01, -07 a -11), validaciones al guardar (AC-RUT-11-05 a -07), colección (AC-RUT-09-01, -02), subtítulos y resúmenes (AC-RUT-12-06, AC-RUT-10-10), secuencia de la mini-barra (AC-RUT-10-11) |
| **[Editor-EM]** | Las pruebas del Core y de los repositorios dentro de Unity, con y sin simulador (AC-BASE-08 de la It. 2, que sigue) |
| **[Editor-PM]** | La UI montada con base de prueba: Rutinas (AC-RUT-10), editor (AC-RUT-11, -12, -14), diálogo de paso (AC-RUT-13), colección y errores (AC-RUT-09), reapertura y series (AC-RUT-08-06, -09), base por modo (AC-HIS-20-02, -03), navegación, diálogos, menús, avisos, ayudas y la capa del simulador (AC-APP-03), arranque (AC-APP-01-09), barra de desplazamiento (AC-APP-05-11) y los de §5. Corren sin y con `-Simulator` (los de la cinta y la llave, solo con) |
| **[Captura]** | CAP-01 a CAP-28, DEMO-01 a DEMO-12 y VID-01 a VID-03 (§7) |
| **[Device-Win]** | Build normal y de simulador: base y carpeta (AC-HIS-17-01), legacy intacto (AC-HIS-17-03), velocidad (AC-HIS-17-06), bases separadas (AC-HIS-20-01, -02), reapertura real y cierre forzado (AC-RUT-08-07, -09), edición de texto y menú del clic derecho (AC-RUT-11-09, AC-APP-09-07), barra de título (AC-APP-05-10), Tab (AC-APP-05-07) |
| **[Device-And]** | Solo con módulo y teléfono: teclado del sistema, campo visible sobre el teclado, sin caja extra, menú del sistema en su idioma (AC-RUT-11-10, AC-APP-09-07), teclado numérico con signo (AC-RUT-13-13), "Atrás" en el editor y en los diálogos (AC-RUT-11-08, AC-APP-03-02), base privada (AC-HIS-17-01), sin barra de desplazamiento fija (AC-APP-05-11) |
| **[QA]** | Esquema abierto con una herramienta de SQLite (AC-HIS-17-02), legacy intacto (AC-HIS-17-03), bases del usuario intactas después de todas las pruebas (AC-HIS-20-04), lo que no se puede hacer (AC-RUT-10-08, AC-RUT-12-05, AC-RUT-14-12) |

---

## 9. Checklist de paridad

El área no tiene tests legacy: la columna "Test legacy" es "—" en todas las filas, salvo la 63, que es del simulador (su test ya se portó en la It. 2).

| # | Comportamiento del legacy | Legacy | Test legacy | AC |
|---|---|---|---|---|
| 1 | Base `rower_app.db`; con simulador `rower_app_dev.db` | `lib/core/database/database_service.dart:10-11` | — | AC-HIS-17-01, AC-HIS-20-01 |
| 2 | Esquema de 4 tablas (v7) | `database_service.dart:75-139` | — | AC-HIS-17-02 |
| 3 | Listado por `created_at` descendente, pasos por `step_order` | `database_service.dart:143-152`, `:190-199` | — | AC-RUT-08-05 |
| 4 | Alta: rutina y pasos con orden 0…n−1 | `database_service.dart:154-163` | — | AC-RUT-08-01 |
| 5 | Edición: actualiza la rutina, borra y reinserta los pasos, conserva la fecha | `database_service.dart:165-181`; `routine.dart:107-112` | — | AC-RUT-08-02 |
| 6 | Baja de la rutina (pasos huérfanos invisibles; acá se borran, Q-17) | `database_service.dart:183-186` | — | AC-RUT-08-03 |
| 7 | Lectura por id o "no existe" | `database_service.dart:206-212` | — | AC-RUT-08-04 |
| 8 | Tipo desconocido hace fallar la carga | `interval_step.dart:133` | — | AC-RUT-08-08 |
| 9 | Carga de rutinas al crear el estado (arranque) | `routines_provider.dart:12-14`; `main.dart:83` | — | AC-RUT-09-01, AC-APP-01-09 |
| 10 | Crear inserta al tope; editar en el mismo lugar; borrar quita | `routines_provider.dart:27-52` | — | AC-RUT-09-02 |
| 11 | Título `routinesTitle`, botón `+` | `routines_screen.dart:16-23` | — | AC-RUT-10-01, -04 |
| 12 | Cargando: indicador centrado | `routines_screen.dart:24-25` | — | AC-RUT-10-02 |
| 13 | Vacío: ícono 56 al 12 %, `routinesEmpty`, `routinesEmptyHint` 13 | `routines_screen.dart:26-40` | — | AC-RUT-10-02 |
| 14 | Tarjeta: ícono `#00B4D8`, nombre w600, descripción solo si no vacía, resumen 11 al 54 %, `⋮` | `routines_screen.dart:41-75` | — | AC-RUT-10-03 |
| 15 | Tocar tarjeta → editar | `routines_screen.dart:76-80` | — | AC-RUT-10-05 |
| 16 | Menú `Editar` / `Eliminar` en rojo, con la ayuda `Mostrar menú` | `routines_screen.dart:64-75`; `material_es.arb:14` | — | AC-RUT-10-06 |
| 17 | Diálogo de eliminar con el nombre, `Eliminar` `#D32F2F`, solo él borra | `routines_screen.dart:97-115` | — | AC-RUT-10-07 |
| 18 | Resumen por pasos de la plantilla | `routine.dart:100-105` | — | AC-RUT-10-10 |
| 19 | Título nueva / editar, `Guardar` | `routine_editor_screen.dart:206-211` | — | AC-RUT-11-01 |
| 20 | Flecha de volver con la ayuda `Atrás` | `routine_editor_screen.dart:206`; `material_es.arb:5` | — | AC-RUT-11-01, AC-APP-03-06 |
| 21 | Nombre de una línea, descripción de 2 líneas, precargados | `routine_editor_screen.dart:23-31`, `:214-236` | — | AC-RUT-11-02 |
| 22 | `Pasos ({count})` con la cantidad de la plantilla, `+ Paso`, `Serie` | `routine_editor_screen.dart:240-260` | — | AC-RUT-11-03 |
| 23 | Vacío `editorEmptySteps` | `routine_editor_screen.dart:262-268` | — | AC-RUT-11-04 |
| 24 | Guardar: nombre vacío → aviso; sin pasos → aviso; en ese orden | `routine_editor_screen.dart:42-53` | — | AC-RUT-11-05 |
| 25 | Recorte de nombre y descripción; renumeración; cierre | `routine_editor_screen.dart:55-72` | — | AC-RUT-11-06 |
| 26 | Sin unicidad ni largo máximo | `routine_editor_screen.dart:40-73` | — | AC-RUT-11-07 |
| 27 | Volver descarta sin preguntar | `routines_screen.dart:18-21` (ruta estándar) | — | AC-RUT-11-08 |
| 28 | Menú de texto del clic derecho en el idioma de la app; seleccionar todo al enfocar en escritorio | `<flutter-sdk>/…/editable_text.dart:2116-2128`, `:2649-2696`; `material_es.arb:27-32` | — | AC-RUT-11-09 |
| 29 | Teclado del sistema; contenido que se achica con el teclado | `AndroidManifest.xml:32` | — | AC-RUT-11-10 |
| 30 | `+ Paso`: diálogo sin progresiones, agrega al final como suelto | `routine_editor_screen.dart:75-88` | — | AC-RUT-12-01 |
| 31 | Fila: borde 3, ícono sobre color al 20 %, título del tipo en su color (en la app, título e ícono en la variante legible: DV-12), subtítulo, lápiz y cruz | `routine_editor_screen.dart:316-369`, `:447-458` | — | AC-RUT-12-02 |
| 32 | Editar en su lugar, conservando serie y repeticiones (y progresiones ocultas) | `routine_editor_screen.dart:90-108`, `:517-519` | — | AC-RUT-12-03 |
| 33 | Borrar al instante | `routine_editor_screen.dart:190-194` | — | AC-RUT-12-04 |
| 34 | Subtítulo `{duración}␣␣·␣␣{objetivos}` | `routine_editor_screen.dart:345-349`; `interval_step.dart:67-109` | — | AC-RUT-12-06 |
| 35 | Diálogo: título, valores iniciales Trabajo / Por tiempo | `routine_editor_screen.dart:484-485`, `:568` | — | AC-RUT-13-01 |
| 36 | Campos en orden, progresiones solo en serie, con ayudas | `routine_editor_screen.dart:574-725` | — | AC-RUT-13-02 |
| 37 | 4 tipos en orden | `routine_editor_screen.dart:574-581` | — | AC-RUT-13-03 |
| 38 | Modo tiempo / distancia; solo cuenta el elegido | `routine_editor_screen.dart:531-540`, `:585-625` | — | AC-RUT-13-04 |
| 39 | `int.tryParse` (signo, ceros, `0x`, no numérico = vacío) | `routine_editor_screen.dart:535-553`; `integers_patch.dart:84-123` | — | AC-RUT-13-05 |
| 40 | Tiempo = min × 60 + seg, siempre con duración | `routine_editor_screen.dart:536-540` | — | AC-RUT-13-06 |
| 41 | Distancia vacía = sin meta | `routine_editor_screen.dart:534-535` | — | AC-RUT-13-07 |
| 42 | Split ≤ 0 = sin objetivo | `routine_editor_screen.dart:557-562` | — | AC-RUT-13-08 |
| 43 | Sin validaciones (Q-25) | `routine_editor_screen.dart:729-736` | — | AC-RUT-13-09, -14 |
| 44 | Precarga con `~/` y `%` de Dart; modo distancia si hay distancia | `routine_editor_screen.dart:498-521` | — | AC-RUT-13-10 |
| 45 | Teclado numérico | `routine_editor_screen.dart:602-722` | — | AC-RUT-13-13 |
| 46 | Cancelar o tocar afuera sin cambios; Escape cierra diálogos | `routine_editor_screen.dart:730-732`; `routes.dart:975-990` | — | AC-RUT-13-14, AC-APP-03-02 |
| 47 | Diálogo con desplazamiento | `routine_editor_screen.dart:569` | — | AC-RUT-13-15 |
| 48 | `Serie`: Trabajo 120 s + Descanso 60 s ×3 al final, id `g{ms}_{contador}` | `routine_editor_screen.dart:110-133` | — | AC-RUT-14-01, AC-RUT-08-06 |
| 49 | Bloque: encabezado `#0077B6` 12 %, pasos con sangría 16, pie 6 %, márgenes 8 | `routine_editor_screen.dart:275-314`, `:371-445` | — | AC-RUT-14-02 |
| 50 | Chip `×N` (`×1` si vacío), tocable | `routine_editor_screen.dart:372-409` | — | AC-RUT-14-03 |
| 51 | Diálogo de repeticiones con foco y precarga | `routine_editor_screen.dart:151-177` | — | AC-RUT-14-04 |
| 52 | No numérico → 1; ≤ 0 → ignora; > 0 → todos los pasos | `routine_editor_screen.dart:173`, `:179-186` | — | AC-RUT-14-05 |
| 53 | Agregar paso a serie: después del último, con sus repeticiones | `routine_editor_screen.dart:135-149` | — | AC-RUT-14-07 |
| 54 | Editar paso de serie conserva serie y repeticiones | `routine_editor_screen.dart:100-105` | — | AC-RUT-14-08 |
| 55 | Borrar serie al instante | `routine_editor_screen.dart:196-200` | — | AC-RUT-14-09 |
| 56 | La serie vacía desaparece; series contiguas separadas (8 + 8 = 16 dp) | `routine_editor_screen.dart:275-295` | — | AC-RUT-14-10, -11 |
| 57 | Pantallas apiladas que tapan la barra | `routines_screen.dart:18-21`; `main.dart:143` | — | AC-APP-03-01 |
| 58 | Diálogos modales, tocar afuera = cancelar | `routines_screen.dart:98-114` | — | AC-APP-03-03 |
| 59 | Avisos abajo, 4 s | `routine_editor_screen.dart:43-51`; `snack_bar.dart:29` | — | AC-APP-03-05 |
| 60 | Ayudas emergentes (al pasar el puntero, sin espera; 1,5 s al mantener presionado) | `routine_editor_screen.dart:416`; `tooltip.dart:392-394` | — | AC-APP-03-06 |
| 61 | Capa del simulador por encima del `Navigator` (pila y diálogos) | `main.dart:108-110`; `simulator_overlay.dart:33-51` | — | AC-APP-03-07 |
| 62 | Barra de título de Windows según el modo del sistema | `windows/runner/win32_window.cpp:275-285` | — | AC-APP-05-10 |
| 63 | Llave a 16 de la derecha y 96 de abajo, también en Rutinas (encima del `+`); el panel se abre en ese lugar | `simulator_overlay.dart:40-49` | `test/simulator_overlay_test.dart:17` (LT-26, It. 2) | AC-BLE-16-02 (en Rutinas cambia: DV-13) |

---

## 10. Mejoras EXR (cómo se tiene que sentir)

Son agregados: ninguno cambia un dato, un texto, un orden ni una regla del legacy.

1. **Un constructor de sesiones, no un formulario.** La rutina se lee como una línea de tiempo: cada paso con el color de su tipo (el trabajo cálido e intenso, el descanso calmo y fresco), el ícono nítido del tipo y su meta legible. Las series se ven como un bloque "apilado" que deja intuir sus repeticiones, con el chip `×N` grande, claro y fácil de tocar.
2. **Todo responde al dedo.** Agregar un paso hace crecer la lista con un resorte suave; borrar lo desvanece y cierra el hueco; el bloque de serie se estira y se encoge con el mismo movimiento. Cambiar `×3` por `×4` hace rodar el número.
3. **Ver antes de confirmar.** El diálogo de paso muestra en vivo cómo va a quedar la fila (AC-RUT-13-16), así "5m · 18 spm" no sorprende después.
4. **Errores amables y en el lugar.** Los topes de Q-92 se avisan debajo del campo que los causa, sin cerrar nada y sin perder lo escrito; los avisos generales aparecen abajo, uno por vez, sin acumularse.
5. **Tarjetas que cuentan la sesión.** Cada rutina muestra su "silueta": la mini-barra con los tramos de la rutina remada, en sus colores (AC-RUT-10-11). `Escalera SPM` se reconoce de un vistazo.
6. **Pantallas que se apilan con continuidad.** El editor entra desde la derecha y la lista queda detrás, un poco más oscura; los diálogos crecen apenas sobre un velo; nada aparece de golpe.
7. **Seguridad.** Guardar es instantáneo y nunca deja una rutina a medias; un error de la base se explica y se puede reintentar, nunca es un indicador que no termina.
8. **Respeto por quien lo pide.** Con "reducir movimiento", fundidos cortos; con letra grande, todo entra; con teclado, todo se alcanza.

Se comprueba con AC-RUT-10-11, AC-RUT-13-16, AC-APP-03-05, AC-APP-03-08, AC-RUT-08-07 y los videos VID-01 a VID-03, que revisan QA y el Designer.

---

## 11. Decisiones aplicadas y gate de la It. 3

| Decisión | Qué se aplica | Confirmar en el gate |
|---|---|---|
| Q-01 | Textos del editor, la lista y los diálogos desde el catálogo es/en; el `×N` pasa al catálogo (`editorRepeatCount`); **16 claves nuevas** (§11.2), 190 en total | No |
| Q-10 | En el editor el `📈` se dibuja como el ícono `trending_up` (ART_BIBLE §3.8); el resto del texto, idéntico (DV-10) | No |
| Q-11 / R-14 | Sin importar la base legacy: base propia v8 desde cero; la importación queda diseñada y sin implementar | **Sí** (ya estaba agendada para este gate) |
| Q-13 | Error de carga con `Reintentar`; avisos al fallar guardar o borrar (AC-RUT-09-04 a -06) | No |
| Q-17 | Al borrar una rutina se borran también sus pasos; sin efecto visible | No |
| Q-18 | Fecha de creación independiente de la zona horaria; "la más nueva arriba" se mantiene (AC-RUT-08-05) | No |
| Q-19 | Un toque, una rutina (AC-RUT-09-03) | No |
| Q-25 | Sin validaciones de sentido en el diálogo, salvo los topes de Q-92 | No |
| Q-26 | **Ajustada por Q-92**: repeticiones de 1 a 999 (P-01) | **Sí** (sin urgencia) |
| Q-92 | Topes por campo y por repetición de AC-RUT-13-11, -12 y AC-RUT-14-06, con sus mensajes es/en (P-02) | **Sí**: el usuario ya aceptó el criterio; acá se le muestran los números |
| D-ART-03 | Textos tenues a ≥ 54 % (DV-11) | No |
| P-06 (It. 1) | Teclado y menú de texto del teléfono en el idioma del sistema | Ya aceptada |
| P-09 (It. 1) | Ayuda de la flecha de volver desde el catálogo (`navBack`) | No |
| DV-01, DV-07 (It. 2) | Cinta con texto oscuro; llave sobre la barra (en Rutinas sube sobre el `+`: DV-13) | Ya aceptadas |
| DV-09 a DV-19 | Diferencias visibles con el legacy, a propósito (§11.1). En el gate de QA, DV-12 se amplió, DV-13 quedó decidida y se sumaron DV-18 y DV-19 (§16) | **Sí**: se le muestran al usuario al cerrar, como DV-01 a DV-08 en la It. 2 |
| DT-05 (Architect) | Las ayudas emergentes se ven y guardan su texto como nombre accesible, pero los lectores de pantalla no las leen: el motor no expone la interfaz de la app a los lectores del sistema. Pasa a la It. 14 (AC-APP-03-06, §13) | **Sí**, informativo: el legacy sí se lee con lectores de pantalla |

### 11.1 Diferencias visibles con el legacy (a propósito)

Siguen la numeración de la It. 2. No incluyen las mejoras EXR de §10 ni el sistema visual de la app (tipografía, juego de íconos, superficies), que no cambian datos, textos, orden ni colores de tipo.

| ID | Legacy (verificado) | App | Motivo | AC |
|---|---|---|---|---|
| DV-09 | Tipo de paso en una lista desplegable (`routine_editor_screen.dart:574-581`) | Las 4 opciones visibles a la vez, con ícono y nombre (ART_BIBLE §3.8); mismo orden, mismos nombres, `Trabajo` por defecto | Un toque en lugar de dos y el color del tipo a la vista | AC-RUT-13-03 |
| DV-10 | `📈` emoji en el subtítulo (`interval_step.dart:105`) | Ícono `trending_up` en el subtítulo y la vista previa; el texto que sigue, idéntico | Q-10 lo permite; el emoji depende de la fuente del sistema | AC-RUT-12-06 |
| DV-11 | `routinesEmpty` al 38 %, `routinesEmptyHint` al 24 %, `editorEmptySteps` al 38 % (`routines_screen.dart:34`, `:37`; `routine_editor_screen.dart:267`) | `text.secondary` (70 %), `text.tertiary` (54 %) y `text.tertiary` | D-ART-03: ningún texto que no esté deshabilitado debajo del 54 %; el título queda un escalón arriba de la ayuda, como DV-05 | AC-RUT-10-02, AC-RUT-11-04 |
| DV-12 | Fila de paso con radio 10, cuadrado 28 e ícono 14, título 13 y subtítulo 11, lápiz y cruz de 16 en 32; **título e ícono en el color del tipo** (`#EF476F`, `#06D6A0`, `#FFD166`, `#118AB2`); encabezado de serie de ≈ 36; chip `×N` de 14 con el número en `#00B4D8` sobre `#00B4D8` al 20 %; tarjeta de rutina con el ícono suelto y resumen 11; lista con márgenes laterales de 12 y 8 entre tarjetas (`routine_editor_screen.dart:316-445`; `routines_screen.dart:41-61`; `lib/shared/theme.dart:56-69`) | Medidas del sistema visual (04-design-notes §4.1, §4.2, §4.6, §4.7): fila con radio 12 y alto mínimo 56, cuadrado 32, ícono 16, título 14 y subtítulo 12, lápiz y cruz de 20 en un área de 48; encabezado y pie de serie de 48; chip: cápsula de 28 con el número en 16, dentro de un área de 48; tarjeta con el ícono de 22 sobre un cuadro tonal de 36 y resumen 13; lista con márgenes laterales de 16 y 10 entre tarjetas. **Texto e íconos del tipo en su variante legible** (`step.<tipo>.hud`): trabajo `#FF5D7F` y enfriamiento `#37A2CB`; descanso y calentamiento no cambian. El borde de la fila, el fondo del cuadrado, la mini-barra y el borde del mosaico elegido siguen en el color del tipo. **Número del chip** en `#38CBEF` (`accent.onTonal`), sobre la cápsula de `#00B4D8` al 20 % | Contraste AA. Sobre la tarjeta `#1A2E45`: título de trabajo 3,815 → 4,685 y de enfriamiento 3,487 → 4,730 (texto de 14: pide 4,5). Ícono del enfriamiento sobre su cuadrado: 2,744 → 3,723 (gráfico: pide 3). `×N` sobre su cápsula: 4,370 → 5,620. Área táctil mínima de 48. Los matices no cambian, como DV-03 y DV-04 en la It. 2 | AC-RUT-12-02, AC-RUT-13-16, AC-RUT-10-03, AC-RUT-14-02, AC-RUT-14-03 |
| DV-13 | Con el simulador, la llave queda encima del `+` de Rutinas y le tapa la mitad (`simulator_overlay.dart:38-51`) | En Rutinas, mientras se ve la pestaña, la llave sube 16 dp por encima del `+` (88 sobre la barra); en las otras pestañas, y con el editor abierto, queda donde estaba (16 sobre la barra). El panel se abre siempre en el mismo lugar y con el tamaño de siempre (AC-BLE-16-03) | El `+` es la acción principal de la pantalla. Apilada sobre el `+`, la llave no tapa la lista y en Dispositivo no cambia de lugar (04-design-notes §4.13; DT-03) | AC-RUT-10-04, AC-BLE-16-02 |
| DV-14 | Los avisos se encolan, 4 s cada uno | Un aviso nuevo reemplaza al visible | Tres toques en `Guardar` no deben dar 12 s de avisos viejos (P-03) | AC-APP-03-05 |
| DV-15 | Teclado numérico del teléfono sin signo (`TextInputType.number`); en iPhone no tiene `-` | Teclado numérico **con** signo | La ayuda del split pide `-2`; sin signo no se puede escribir (P-05) | AC-RUT-13-13 |
| DV-16 | Sin validación: errores de guardado y carga en silencio, spinner eterno, rutinas duplicadas por doble toque | Q-13 y Q-19 | Correcciones ya decididas | AC-RUT-09-03 a -06 |
| DV-17 | Sin topes: acepta miles de millones de vatios y repeticiones; un número decimal que no entra en el entero de 64 bits de Dart cuenta como vacío, o como ×1 en las repeticiones (`<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm_shared/lib/integers_patch.dart:84-123`; `routine_editor_screen.dart:173`) | Topes de Q-92 (y Q-26 ajustada); un número de cualquier largo cuenta por su valor | Ningún número que el editor deje guardar puede romper la expansión ni el entrenamiento | AC-RUT-13-05, -11, -12, AC-RUT-14-05, -06 |
| DV-18 | En el editor quedan fijos los campos, la línea y la fila `Pasos (N)` con `+ Paso` y `Serie`; solo la lista de pasos se desplaza (`routine_editor_screen.dart:212-269`) | Un solo desplazamiento: campos, fila de pasos y lista se mueven juntos. Con una rutina larga, para llegar a `+ Paso` y `Serie` hay que volver arriba | En una ventana baja, con letra 1,3× o con el teclado abierto, la parte fija no entra: a 640 × 360 con letra 1,3× ocuparía ≈ 250 de los 304 dp visibles y dejaría ≈ 50 para la lista. Así nada desborda y el campo con foco siempre se puede llevar a la vista (DT-01) | AC-RUT-11-02, AC-RUT-11-03, AC-RUT-11-10, AC-APP-05-05; CAP-22 |
| DV-19 | Las ayudas de los tres campos de progresión (`Ej: 2`, `Ej: 5`, `Ej: -2 (más rápido)`) se ven solo con el campo enfocado y vacío; sin foco, la etiqueta ocupa su lugar (`routine_editor_screen.dart:688-724`; `<flutter-sdk>/packages/flutter/lib/src/material/input_decorator.dart:2176-2178`, `:2346`) | Con el campo vacío la ayuda se ve siempre, con foco o sin él, y la etiqueta de esos tres campos queda arriba | La ayuda explica cómo se escribe la progresión (en el split, `-2` es más rápido) antes de tocar el campo; no cambia textos ni datos (DT-02) | AC-RUT-13-02; CAP-15, CAP-24 |
| DV-20 | En los campos de una línea (`routine_editor_screen.dart:218-224`: `TextField` sin `maxLines` ni formateadores propios), lo que se pega pierde solo el salto de línea `\n` (`FilteringTextInputFormatter.singleLineFormatter` = `deny('\n')`: `<flutter-sdk>/packages/flutter/lib/src/services/text_formatter.dart:425`, `widgets/editable_text.dart:967-970`). Pegar `Pirámide⏎500` copiado en Windows (CRLF) guarda `Pirámide\r500`, con un retorno de carro invisible; pegar `Serie`, una tabulación y `2` guarda la tabulación | Los campos de una línea quitan `\n`, `\r` y `\t`: `Pirámide500` y `Serie2` | Evita caracteres invisibles en el nombre; con texto de Windows da lo mismo que el legacy con un salto de línea simple. Agregada al cerrar (QA-V3-07, §17) | AC-RUT-11-02 |

### 11.2 Claves nuevas del catálogo (Q-01)

Español idéntico al tono del legacy (voseo donde corresponde) y su inglés. Con estas, el catálogo pasa de 174 a **190** claves por idioma.

| Clave | es | en | Parámetros | Dónde |
|---|---|---|---|---|
| `navBack` | `Atrás` | `Back` | — | ayuda de la flecha de volver (P-09 de la It. 1) |
| `menuShow` | `Mostrar menú` | `Show menu` | — | ayuda del `⋮` |
| `textCut` | `Cortar` | `Cut` | — | menú de texto en la PC |
| `textCopy` | `Copiar` | `Copy` | — | ídem |
| `textPaste` | `Pegar` | `Paste` | — | ídem |
| `textSelectAll` | `Seleccionar todo` | `Select all` | — | ídem |
| `editorRepeatCount` | `×{count}` | `×{count}` | `count` (entero) | chip de la serie |
| `editorErrorRange` | `Fuera de rango: hasta ±{max}` | `Out of range: up to ±{max}` | `max` (texto: `9999`, `99:59:59`…) | AC-RUT-13-11 |
| `editorErrorRangeRep` | `Fuera de rango en la repetición {rep}: hasta ±{max}` | `Out of range in repetition {rep}: up to ±{max}` | `rep` (entero), `max` (texto) | AC-RUT-13-12 |
| `editorRepetitionsMax` | `Máximo {max}` | `Maximum {max}` | `max` (entero) | AC-RUT-14-05 |
| `editorRepetitionsOutOfRange` | `Con ×{count}, un objetivo queda fuera de rango` | `With ×{count}, a target goes out of range` | `count` (entero) | AC-RUT-14-06 |
| `editorStepOutOfRange` | `Hay un paso con valores fuera de rango` | `A step has out-of-range values` | — | AC-RUT-11-05 |
| `editorSaveError` | `No se pudo guardar la rutina` | `Couldn't save the routine` | — | AC-RUT-09-05 |
| `routineDeleteError` | `No se pudo eliminar la rutina` | `Couldn't delete the routine` | — | AC-RUT-09-06 |
| `routinesLoadError` | `No se pudieron cargar las rutinas` | `Couldn't load routines` | — | AC-RUT-09-04 |
| `retry` | `Reintentar` | `Retry` | — | AC-RUT-09-04 (se reusa en la It. 9) |

Las 4 de `text*` existen porque en la PC el menú lo dibuja la app (el legacy usa los textos de Flutter en el idioma de la app). Si el Architect confirma que el menú del clic derecho no es posible, esas 4 claves no se agregan y se aplica la alternativa de P-06 de esta spec.

---

## 12. Restricciones de producto para el Architect

1. **Determinismo y aislamiento.** Pruebas y capturas usan una base temporal propia, reloj de prueba y las bases sembradas de §0.4; **nunca** las bases del usuario (AC-HIS-20-04), salvo la excepción de CAP-27 con sus condiciones. Ningún doble llega a un build instalable.
2. **Todo o nada, y durable.** Un guardado de rutina deja la base como antes o como después, nunca a medias, y lo que el editor dio por guardado sobrevive a un cierre forzado inmediato (AC-RUT-08-07).
3. **Nunca se desborda un número.** Leer un entero de cualquier largo, sumar Min × 60 + Seg y aplicar progresión × repetición no pueden desbordarse ni lanzar errores: lo que no entra es "fuera de rango" (AC-RUT-13-05, -11, -12). Una rutina con valores imposibles cargada a mano (`F-RARA`) no hace caer ni colgar la app (AC-RUT-10-11).
4. **La base no frena la pantalla** (AC-HIS-17-06): cargar y guardar no traban la escritura ni las animaciones.
5. **R-14 y Q-11.** La app nueva nunca abre ni modifica la base del legacy; no hay importación.
6. **R-01.** El modo define la base (`rower_app_dev.db` con simulador, `rower_app.db` sin él) y ningún camino cruza (AC-HIS-20-01). En el Editor sin el modo simulador, Play usa la misma `rower_app.db` que el build normal de esa PC (como la configuración "RowMate" del legacy); es esperado.
7. **Base privada en Android** (AC-HIS-17-01): no en un almacenamiento que vean otras apps o la PC por USB.
8. **Una base que no se entiende no se toca** (AC-HIS-17-05): ni se borra ni se reemplaza; se muestra el error.
9. **Teclado del teléfono.** El texto se edita en el campo mismo, sin la caja extra del sistema sobre el teclado; teclado numérico con signo; el campo con foco queda visible (AC-RUT-11-10, AC-RUT-13-13; ARCHITECTURE §7.3, `KeyboardAvoider`).
10. **Menú de texto y ayudas en la PC.** Hace falta un menú del clic derecho (AC-RUT-11-09) y ayudas emergentes (AC-APP-03-06), con textos del catálogo. Si alguno no se puede en Unity 6000.3, el Architect lo dice en `02-tech-design` y se aplica la alternativa de P-06.
11. **Escape no cierra pantallas** (AC-APP-03-02): solo diálogos y menús. ARCHITECTURE §8 dice que Escape también hace "atrás" en las páginas; hay que alinearlo (§15).
12. **La capa del simulador sigue siendo la última** (AC-APP-03-07): por encima de `PageStack`, `DialogLayer` y `ToastLayer`, y la cinta no recibe toques.
13. **Identificadores estables para las pruebas**, por ejemplo: `routines-add`, `routines-retry`, `routine-card-<n>`, `routine-menu-<n>`, `routine-menu-edit`, `routine-menu-delete`, `dialog-cancel`, `dialog-confirm`, `editor-back`, `editor-save`, `editor-name`, `editor-description`, `editor-add-step`, `editor-add-series`, `step-row-<n>`, `step-edit-<n>`, `step-delete-<n>`, `series-repeat-<n>`, `series-delete-<n>`, `series-add-step-<n>`, `step-type-<tipo>`, `step-mode-time` / `-distance`, `step-min`, `step-sec`, `step-meters`, `step-wmin`, `step-wmax`, `step-spm`, `step-split-min`, `step-split-sec`, `step-prog-spm`, `step-prog-watts`, `step-prog-split`, `repeat-amount`. El legacy no tiene claves de prueba en el área: los nombres finales los fija el Architect.
14. **16 claves nuevas en el catálogo** (§11.2), con su inglés; el control de 174 claves de la It. 1 se actualiza a 190.

---

## 13. Fuera de alcance

- **Workout** (It. 4a): la lista de rutinas de la pestaña Workout, el diálogo `¿Iniciar entrenamiento?` y el entrenamiento con la rutina (expansión en vivo, píldoras, HUD). Workout sigue provisoria (AC-APP-02-08).
- **Sesiones** (It. 4a) e **Historial** (It. 9): las tablas existen vacías; el vínculo rutina ↔ sesiones (F-RUT-20) y qué pasa con las sesiones de una rutina borrada se verifican en la 9.
- **Strava** (It. 12-13): lectura por id para subir (ya probada como regla, AC-RUT-08-04).
- **Importar la base legacy** (Q-11): diseñada, sin implementar.
- **Mejoras del editor** (BKL-12): reordenar, duplicar, deshacer, aviso de cambios sin guardar, deslizar para borrar, validaciones de sentido.
- **Unidades explícitas** en la etiqueta de duración (Q-07: `2m` sigue siendo 2 minutos y `500m`, 500 metros).
- **Vista desplegada de la serie** con cada repetición progresada, y duración y distancia totales en la tarjeta (propuestas de F-RUT-05, F-RUT-06 y F-RUT-14 del inventario): después de la paridad.
- **Háptica** (It. 14), Enter que confirma un diálogo desde un campo (el legacy no lo hace), ayudas en el lápiz y la cruz (el legacy no las tiene).
- **Lectores de pantalla** (It. 14, DT-05): las ayudas guardan su texto como nombre accesible, pero que un lector de pantalla del sistema lea la app queda para la It. 14 (AC-APP-03-06).
- **Bluetooth real** (It. 10a-11).

---

## 14. Preguntas abiertas

| ID | Pregunta | Por defecto | Para |
|---|---|---|---|
| P-01 | Q-26 decía "sin máximo de repeticiones". Con Q-92, ¿se fija 999? | **Sí, 1 a 999.** Una serie ×2 000 000 000 traba el inicio del entrenamiento también en el legacy | Usuario (gate, sin urgencia) |
| P-02 | ¿Están bien los números de los topes (99:59:59, 999 999 m, 9 999 W, 999 spm, 99:59 de split; progresiones 999 / 9 999 / 5 999)? | Sí: son de 5 a 100 veces el uso real (el récord de 24 h ronda los 370 km; un remero de elite, 1 500-2 000 W de pico). El Architect puede pedir otros si no le alcanzan, siempre muy por encima del uso real | Usuario (gate) y Architect |
| P-03 | Avisos: ¿reemplazar al visible o encolarlos como el legacy? | Reemplazar (DV-14) | — (decidida por el PO) |
| P-04 | `0x10` en un campo numérico vale 16 en el legacy (regla de Dart). ¿Se mantiene? | **Paridad**: se mantiene. Nadie lo escribe a propósito, pero así el resultado es el mismo número en las dos apps | — (decidida por el PO) |
| P-05 | Teclado numérico del teléfono: el legacy no pide signo (en iPhone no hay `-`) aunque la ayuda del split dice `-2` | Con signo (DV-15) | — (decidida por el PO); se verifica en [Device-And] y [Apple] |
| P-06 | ¿Y si Unity no permite el menú del clic derecho o las ayudas emergentes en la PC? | El Architect lo dice en `02-tech-design`. Alternativa: sin menú en la PC (quedan los atajos de teclado, que se deben cumplir igual), anotado como diferencia visible nueva; las ayudas siguen siendo nombre de accesibilidad. **En Android** (QA-G3-16): si con el texto editado en el mismo campo, sin la caja extra, el sistema no muestra su menú de seleccionar, copiar y pegar, la app muestra al mantener presionado su propio menú, el mismo de la PC, con los textos del catálogo en el idioma **de la app**; el ejemplo en portugués de AC-APP-09-07 pasa a decir `Copy` / `Paste`, y se anota como diferencia visible nueva. Se decide con el teléfono ([Device-And]) | Architect |
| P-07 | Una fila con un tipo desconocido (`sprint`): ¿falla toda la lista (como el legacy) o se saltea esa rutina? | Toda la lista, con el error de Q-13 y `Reintentar` (lo más parecido al legacy). Solo pasa tocando la base a mano | — (decidida por el PO) |
| P-08 | ¿Escape cierra el editor en la PC? ARCHITECTURE §8 dice que sí | **No**: en el legacy Escape solo cierra diálogos y menús; cerrar el editor con Escape descartaría la rutina sin preguntar | Architect (alinear ARCHITECTURE §8) |
| P-09 | Emojis escritos por el usuario en el nombre o la descripción | Se guardan tal cual (AC-RUT-08-01); cómo se dibujan depende de la fuente (el Designer elige el respaldo); nunca rompen el texto que los rodea | Designer |

---

## 15. Discrepancias encontradas al especificar

| Dónde | Qué dice | Qué es (verificado en el legacy) | Acción |
|---|---|---|---|
| ROADMAP §2, It. 3 (criterios nuevos) | Repeticiones `"−2"` | Con el signo menos tipográfico (U+2212), `int.tryParse` no lo reconoce: da ×1. Con el guion ASCII, `-2` no cambia nada | AC-RUT-14-05 trae los dos. **Pendiente** (PO, al cerrar): aclarar en el ROADMAP |
| routines-models F-RUT-13 | "Se aceptan negativos y el signo +" | Además: ceros a la izquierda y hexadecimal `0x…` (regla de Dart, `integers_patch.dart:106-117`) | AC-RUT-13-05, P-04 |
| routines-models F-RUT-13 | Precarga "minutos (división entera) y segundos (resto)" | Con negativos usa la regla de Dart (resto nunca negativo): −90 s se precarga `-1` y `30` y al confirmar queda −30 s; −30 s queda 30 s. Un paso sin meta se abre en modo tiempo y al confirmar queda en 0 s | AC-RUT-13-10. **Pendiente** (PO): sumarlo al inventario |
| routines-models F-RUT-10 y app-shell F-APP-03 | Con el simulador, nada | La llave del simulador tapa la mitad del `+` de Rutinas (`simulator_overlay.dart:40-41` contra la posición estándar del botón flotante, `floating_action_button_location.dart:25`, y la barra de 80, `navigation_bar.dart:1383`) | DV-13 |
| app-shell F-APP-03.4 | Avisos con "duración estándar del sistema" | 4 s y **encolados** (`snack_bar.dart:29`; `ScaffoldMessenger`) | AC-APP-03-05, DV-14 |
| ARCHITECTURE §8 | "Botón atrás (Android, Escape en escritorio) … `PageStack.OnBackRequested()`" | En el legacy Escape no cierra pantallas, solo lo que se cierra tocando afuera (`routes.dart:975-990`) | AC-APP-03-02, P-08. **Pendiente** (Architect) |
| ART_BIBLE §3.7 | Tarjeta de rutina con chevron | En Rutinas va el menú `⋮`; el chevron es de la lista de Workout (It. 4a) | AC-RUT-10-03. **Pendiente** (Designer) |
| ART_BIBLE §3.8 y LEGACY_VISUAL_AUDIT §5.4 | "+ Agregar paso a la serie" | `editorAddStepToSeries` = `Agregar paso a serie` (sin "la") | Manda el catálogo. **Pendiente** (Designer) |
| LEGACY_VISUAL_AUDIT §5.4 | Lista: "resumen 12, descripción 11 @54 %" | Descripción 12; resumen 11 al 54 % (`routines_screen.dart:56-61`) | AC-RUT-10-03. **Pendiente** (Designer) |
| ART_BIBLE §3.8 | Selector de tipo con chips | El legacy usa una lista desplegable | DV-09 |
| app-shell F-APP-09.5 | Textos del sistema en el idioma de la app | En la PC el legacy además selecciona todo al enfocar con Tab y arma el menú del clic derecho según la selección y el portapapeles (`editable_text.dart`) | AC-RUT-11-09 |
| It. 1 (01-po-spec §14, P-06) y 02-tech-design de la It. 1 §10.3 | "El teclado y su menú de copiar y pegar siguen el idioma del sistema" | Vale para el teléfono. En la PC no hay menú del sistema en un campo de Unity: si existe, lo dibuja la app | AC-APP-09-07 (§5), AC-RUT-11-09, P-06 |
| Esta spec, primera versión (AC-RUT-12-02, AC-RUT-14-03, DV-12) | Título e ícono "en el color del tipo" y número del `×N` en `#00B4D8`, como paridad; enfriamiento en `#35A0C9` | Son los colores del legacy, pero no llegan a AA: título de trabajo 3,815 y de enfriamiento 3,487 sobre la tarjeta, ícono del enfriamiento 2,744 sobre su cuadrado, `×N` 4,370 sobre su cápsula (04-design-notes §8, recalculados). `#35A0C9` era el valor viejo de `step.cooldown.hud` (hoy `#37A2CB`, QA-V2-12) | **Resuelto en el gate** (QA-G3-03): DV-12 ampliada |
| Esta spec, primera versión (CAP-13) | "8 dp entre los dos" bloques de serie | 16 dp: `margin top 8` del encabezado más `margin bottom 8` del pie (`routine_editor_screen.dart:377`, `:427`) | **Resuelto en el gate** (QA-G3-04) |
| Esta spec, primera versión (AC-RUT-13-02, CAP-15b) | Ayudas de progresión "cuando están vacíos", como paridad | En el legacy solo se ven con foco: sin foco, la etiqueta ocupa su lugar (`input_decorator.dart:2346`) | **Resuelto en el gate** (QA-G3-17): DV-19 |
| AC-BLE-16-02 de la It. 2 frente a DV-13 | "Tocarlo abre el panel en ese mismo lugar" | Con DV-13 la llave sube en Rutinas y el panel sigue abriendo en el lugar de siempre | **Resuelto en el gate** (QA-G3-05): texto nuevo en §5 |
| ROADMAP §2, It. 14 | No nombra los lectores de pantalla | El legacy expone sus textos y ayudas a los lectores de pantalla del sistema; la app de Unity, todavía no (DT-05) | **Pendiente** (PO, al cerrar): sumar el criterio a la It. 14 |

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después de escribir esta spec. No se leyó `lib/core/strava/strava_secrets.dart`.

---

## 16. Cambios por gate QA

Correcciones por el gate de armonía de QA ([03-qa-harmony](03-qa-harmony.md), APROBADO CON OBSERVACIONES: 0 bloqueantes, 7 mayores, 14 menores), 2026-10-27. Cada valor nuevo se volvió a leer en el legacy (`routine_editor_screen.dart:151-188`, `:202-270`, `:316-445`, `:531-562`, `:688-724`; `routines_screen.dart:14-118`; `lib/shared/theme.dart:55-69`; `lib/core/dev/simulator_overlay.dart:28-55`, `:81-94`; `windows/runner/win32_window.cpp:21-27`, `:216-217`, `:275-285`; `test/simulator_overlay_test.dart:17`), en el SDK local de Flutter (`material/input_decorator.dart:1969`, `:2076-2078`, `:2176-2178`, `:2346`; `widgets/editable_text.dart:3738-3753`, `:3914-3947`, `:5642-5648`; `widgets/routes.dart:1154-1156`, `:1220-1222`; `widgets/app.dart:1272-1276`) y en el repo (`Assets/RowMate/Core/Design/Tokens.cs`, `Assets/RowMate/Simulator/Panel/SimulatorPanelLayout.cs`). Los contrastes de DV-12 se recalcularon con un programa propio (mezcla sRGB sin redondear, el método de `Contrast.cs`) y coinciden con 04-design-notes §2.3 y §4.7 al milésimo. No se leyó `lib/core/strava/strava_secrets.dart`; `git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después.

| Issue | Sev. | Qué cambió | Dónde |
|---|---|---|---|
| QA-G3-03 | mayor | DV-12 ampliada con la propuesta de 04-design-notes §10: título **e ícono** de cada tipo en su variante legible `step.<tipo>.hud` (trabajo `#FF5D7F`, enfriamiento `#37A2CB`; descanso y calentamiento no cambian) y número del chip en `#38CBEF` (`accent.onTonal`) sobre `#00B4D8` al 20 %. Borde, cuadrado, mini-barra y mosaico elegido siguen en el color del tipo. Resuelve DT-19: el `#35A0C9` de la primera versión era el valor viejo del token. DV-12 suma además las medidas del sistema visual de 04-design-notes (fila de 56, acciones de 20 en 48, encabezado y pie de serie de 48, cápsula de 28, ícono de la tarjeta sobre un cuadro tonal, márgenes de la lista) | AC-RUT-12-02, AC-RUT-13-16, AC-RUT-14-03; §11.1 DV-12; §7.3 CAP-10; §9 fila 31; §15 |
| QA-G3-04 | mayor | CAP-13: **16 dp** entre las dos series (8 + 8, como el legacy). AC-RUT-14-11 lo dice también, con la cita del legacy | §7.3 CAP-13; AC-RUT-14-11; §9 fila 56; §15 |
| QA-G3-05 | mayor | AC-BLE-16-02 entra en §5 con un texto nuevo que reemplaza al de la It. 2: en Rutinas, sin página encima, la llave sube a 16 dp sobre el `+` (88 sobre la barra); en las otras pestañas y con el editor abierto, 16 sobre la barra; se mueve con una transición corta (en el acto con "reducir movimiento"). El panel se abre **siempre en el mismo lugar** (16 sobre la barra), también en Rutinas, donde tapa el `+` como en el legacy. **AC-BLE-16-03 no cambia.** DV-13 queda decidida. §5 pasa a 11 criterios: **102** en total | §0 (cabecera), §4.0, §5; AC-RUT-10-04, AC-RUT-10-09; §7.3 CAP-01, CAP-09; §9 fila 63; §11, §11.1 DV-13; §15 |
| QA-G3-17 | menor | DT-01 pasa a **DV-18** (un solo desplazamiento en el editor) y DT-02 a **DV-19** (ayudas de progresión visibles sin foco): se aceptan como cambio, con motivo, y AC-RUT-13-02 y CAP-15 lo dicen. Se confirman: **DT-05** (el lector de pantalla queda NO VERIFICABLE y pasa a la It. 14: AC-APP-03-06, §13), **DT-09** (una base de versión 1 a 7, o sin versión con tablas, es una base no reconocida: error y no se toca; AC-HIS-17-05), **DT-10** (un entero de la base fuera de 32 bits hace fallar la carga, como un tipo desconocido; AC-RUT-08-08), **DT-13** (una serie ×0 no frena el guardado; ×1000 o más, sí; AC-RUT-11-05) y **DD-01** (la barra de título se actualiza en ≤ 2 s, o en el acto al volver el foco; AC-APP-05-10) | §11, §11.1 DV-18, DV-19; AC-RUT-13-02, AC-APP-03-06, AC-HIS-17-05, AC-RUT-08-08, AC-RUT-11-05, AC-APP-05-10; §7.3 CAP-15; §13; §15 |
| Alineación (partes del PO en issues de otros) | — | **QA-G3-06**: excepción explícita de CAP-27 en AC-HIS-20-04, §0.4, §7.1 y §12.1, con las condiciones de QA (solo a pedido y fuera del CI; no corre con la app o el Editor abiertos; copia verificada antes de tocar; restauración siempre, con el mismo contenido y la misma fecha; rutina sintética); CAP-27 no se toma si Windows no está ya en Oscuro. **QA-G3-13**: (a) split ≤ 0 = sin split, por encima del tope (AC-RUT-13-05, -08, -11); (b) `-99999999999999999999` en repeticiones = sin cambio, como `0` y `-2` (AC-RUT-14-05; el legacy da ×1); (c) las progresiones de un paso suelto tampoco se controlan al guardar (AC-RUT-11-05); (d) solo se controlan los campos del modo elegido (AC-RUT-13-11); (f) una palabra de 200 caracteres sin espacios se parte (AC-RUT-10-03). **QA-G3-01**: lo que se está cerrando (editor, diálogo, menú) no responde, como en el legacy (AC-RUT-09-03, AC-APP-03-01, -03, -04). **QA-G3-10**: Escape con foco en un campo no revierte el texto ni quita el foco (AC-APP-03-02); Enter en un campo de una línea quita el foco y Tab nunca escribe una tabulación (AC-APP-05-07); el texto del usuario se ve tal cual, sin interpretar marcas (AC-RUT-10-03, -07). **QA-G3-16**: variante de P-06 para Android (§14). **QA-G3-02**: lo que 04-design-notes cambia en algo que un AC describe queda escrito: gracia de 120 ms del indicador (AC-RUT-10-02, CAP-02; DN3-15), mini-barra con valores imposibles (AC-RUT-10-11; DN3-07), riel continuo y hojas de la serie (AC-RUT-14-02; DN3-05) y foco al primer error (AC-RUT-13-11; DN3-10) | AC-HIS-20-04, §0.4, §7.1, §7.3 CAP-27, §12.1; AC-RUT-13-05, -08, -11, AC-RUT-14-05, AC-RUT-11-05, AC-RUT-10-03, -07; AC-RUT-09-03, AC-APP-03-01 a -04; AC-APP-05-07; §14 P-06; AC-RUT-10-02, -11, AC-RUT-14-02; §7.3 CAP-02 |

**Para el Architect** (derivado del gate): AC-BLE-16-02 en `It03Ids` (102 IDs) y en las pruebas del simulador que miden la llave, con el panel abierto en Rutinas (QA-G3-05); la comprobación de CAP-13 con 16 ± 1 (QA-G3-04); las pruebas de los casos de QA-G3-13 y de lo que sale sin recibir toques (QA-G3-01), con el texto de esta spec.

**Para el usuario, al cerrar** (sin cambios): P-01, P-02 y Q-11 (§11, §14). Además se le muestran DV-09 a DV-19 y DT-05 (§11).

**Sin disputas**: los cuatro issues del PO (QA-G3-03, -04, -05 y -17) se aceptan como los pide QA, y en QA-G3-13 se toman las dos propuestas de QA.

Sin otros cambios de alcance: siguen los 91 criterios nuevos, las 28 capturas, las 12 tomas de la demo, los 3 videos y las 16 claves nuevas (190 en total). Con AC-BLE-16-02, §5 pasa de 10 a 11 criterios y el total, de 101 a **102**.

---

## 17. Cambios al cerrar (PO)

Por la verificación de QA ([05-qa-verification](05-qa-verification.md), rondas 1 a 3), 2026-10-28. No cambia ningún criterio ni su resultado (101 PASS, 0 FAIL, 1 NO VERIFICABLE).

| Origen | Qué cambió | Dónde |
|---|---|---|
| QA-V3-07 | **DV-20** nueva: los campos de una línea quitan también `\r` y `\t`, y el legacy solo `\n`. Se acepta lo que hace la app y se le muestra al usuario en el gate, con DV-09 a DV-19. AC-RUT-11-02 ("un salto de línea pegado se quita") sigue igual | §11.1; [REPORT](REPORT.md) §6.1 y §8.1 |
| §15, pendientes del PO | Hechos: el ROADMAP distingue `-2` y `−2` en la It. 3 y suma los lectores de pantalla a la It. 14 (DT-05); el inventario [routines-models](../../product/inventory/routines-models.md) suma la precarga con negativos y la regla completa de `int.tryParse` (F-RUT-13, caso 18) | [ROADMAP](../../ROADMAP.md) §2 y §8; routines-models "Cambios al cerrar la It. 3" |
| R3-08 (02-tech-design §8.2) | El ROADMAP suma a la It. 4a un criterio para iniciar una rutina fuera de los topes de Q-92 cargada a mano | [ROADMAP](../../ROADMAP.md) §2, It. 4a |
| Q-26, Q-92 | [FEATURE_INVENTORY](../../product/FEATURE_INVENTORY.md) con los topes aplicados (P-01, P-02) | FEATURE_INVENTORY §4.2 |

`git -C ../rowmate-legacy status --porcelain` quedó vacío antes y después. No se leyó `lib/core/strava/strava_secrets.dart`.
