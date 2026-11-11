# It. 12 — Exportar TCX (spec de producto)

| Campo | Valor |
|---|---|
| Iteración | 12 (`12-exportar-tcx`), [ROADMAP §It. 12](../../ROADMAP.md). Reemplaza a la vieja `12-strava-subida` |
| Motivo | **Decisión del usuario (2026-11-09): Strava descartado.** Desde junio de 2026 la API de Strava exige suscripción paga incluso para apps personales de un solo usuario. Se conserva solo el archivo TCX, que el usuario sube a mano (strava.com/upload es gratis) o a Garmin Connect, intervals.icu, etc. |
| Incluye | F-STR-09 (archivo TCX), sin los metadatos de F-STR-10 ([strava-profile](../../product/inventory/strava-profile.md)) |
| También toca | F-HIS-09 (botón en el Detalle), F-HIS-19 (lee la sesión y sus puntos; nunca escribe), F-BLE-13 (sesiones del simulador), F-APP-03 (avisos con acción), F-APP-10 (7 claves nuevas: 252 por idioma), F-APP-13 (Android sin permisos nuevos) |
| Contrato | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) (solo lectura, commit `3ae0f3a`). Leídos: `lib/core/strava/tcx_builder.dart` entero, `strava_metadata.dart:1-185`, `strava_api_service.dart:60-140`, `lib/features/profile/profile_provider.dart:95-225`, `lib/features/history/session_detail_screen.dart:70-135`, `lib/core/models/workout_session.dart:118-136`. **No** se leyó `lib/core/strava/strava_secrets.dart` |
| Modo | Entrega rápida, sin debates. Ante una duda, el legacy o un valor por defecto, anotado en una línea (§8) |
| Criterios | **28** (§3), sin tests legacy (no hay), 12 capturas (§7) y 2 guiones de demo (§1) |

---

## 0. Cómo leer esta spec

- Tamaños (**TV** 411 × 914, **TH** 914 × 411, **L640** 640 × 360, **W1280** 1280 × 720, en dp) y estado de prueba (reloj de prueba, `ROWMATE_DATA_DIR` temporal, nada en `prefs.json` ni en las bases del usuario): los de la [spec de la 6a §0](../06a-escena-lago/01-po-spec.md). Textos entre comillas invertidas: **exactos al carácter**.
- Marcas: `[dotnet]` armado del TCX y del nombre con `dotnet test` (zona horaria inyectable, sin disco real o con carpeta temporal); `[Editor]` vistas y avisos con un **destino de exportación falso** (no escribe fuera de la carpeta temporal y registra "abrir carpeta" sin abrir nada); `[Captura]` = §7; `[Device-Win]` build de desarrollo con `ROWMATE_DATA_DIR` temporal (QA) y build normal (demo del usuario); `[Device-Android]` **NO VERIFICABLE** sin "Android Build Support" (QA solo ve que el Kotlin compile con `native/android/check.ps1`); `[Manual]` subida a mano en la demo del usuario; `[Apple]` a la 10b.
- **Ninguna prueba, captura ni build del workflow escribe en Documentos ni en Descargas, ni abre el Explorador.** Con `ROWMATE_DATA_DIR` o con el destino falso, la exportación va a la carpeta temporal.
- Fuentes legacy: `tcx` = `lib/core/strava/tcx_builder.dart`, `api` = `lib/core/strava/strava_api_service.dart`, `pp` = `lib/features/profile/profile_provider.dart`, `sd` = `lib/features/history/session_detail_screen.dart`, `ws` = `lib/core/models/workout_session.dart`.
- Bases: `F-9` de la [It. 9 §6](../09-historial/01-po-spec.md) (6 sesiones) para la UI; sesiones de prueba `T-1` a `T-6` (§6) para el archivo. IDs `AC-STR-09-nn`; `DV-116` a `DV-127` siguen a DV-115.

---

## 1. Objetivo y demo

**Objetivo.** Desde el Detalle de cualquier sesión con datos, `Exportar TCX` genera el mismo archivo que el legacy armaba para Strava (una vuelta por paso, pulso, cadencia, potencia, calorías, hora local con desfase), sin metadatos ocultos. En Windows queda en `Documentos\RowMate\TCX` con un aviso que lo dice y `Abrir carpeta`; en Android se abre la hoja de compartir del sistema. Sin red y sin cuentas.

**Lo que el workflow deja verificado:** el archivo byte a byte contra el golden ([dotnet]), el botón, sus estados y los avisos ([Editor], §7) y, en un build de desarrollo, que el archivo aparece en `<ROWMATE_DATA_DIR>\exports` ([Device-Win]).

**Demo del usuario en Windows** (build normal, W1280, español; 10 minutos):

1. Remar una rutina corta con pasos (o usar una sesión real ya guardada). Historial → abrir la sesión.
2. Arriba a la derecha, `Exportar TCX` (el ícono; al pasar el cursor dice `Exportar TCX`). Tocarlo: indicador breve y el aviso `Guardado en Documentos\RowMate\TCX` con el nombre `RowMate_AAAA-MM-DD_HHmm_{rutina}.tcx` y `Abrir carpeta`.
3. `Abrir carpeta`: se abre el Explorador en esa carpeta con el archivo seleccionado. Exportar otra vez la misma sesión: aparece `…_2.tcx` (no pisa el anterior).
4. Entrar a <https://www.strava.com/upload/select> (o Strava → `+` → Subir actividad → Archivo), elegir el `.tcx`. Verificar: **hora de inicio** igual a la del Historial; **distancia**; una **vuelta por paso** de la rutina (pestaña Vueltas); gráficos de **pulso**, **cadencia** y **potencia**.
5. El TCX solo admite deporte `Other`: en Strava, editar la actividad y elegir **Remo** (y "Ergómetro/Indoor" si se quiere). El nombre se pone a mano. La descripción puede mostrar el nombre de la rutina.
6. (Opcional) Subir el mismo archivo a Garmin Connect o intervals.icu.

**Demo del usuario en Android** (con "Android Build Support" y el APK): Historial → sesión → `Exportar TCX` → se abre la hoja de compartir con `RowMate_….tcx`; mandarlo a Drive o al correo y subirlo desde la PC como en el paso 4.

**Lo que depende del usuario:** una cuenta gratis de Strava (u otro servicio) para el paso 4. Nada más.

---

## 2. Historias de usuario

- Como remero quiero exportar una sesión como TCX para subirla a Strava (u otra plataforma) sin pagar la API ni conectar cuentas.
- Como remero quiero que el archivo tenga mis vueltas por paso, pulso, cadencia, potencia y la hora correcta, igual que cuando RowMate subía a Strava.
- Como remero quiero saber dónde quedó el archivo y abrir esa carpeta con un toque, sin que una exportación pise otra.

---

## 3. Criterios de aceptación

### Contenido del archivo (F-STR-09)

- **AC-STR-09-01 · Estructura y golden.** *Dado* `T-1` en la zona UTC−03:00, *entonces* el archivo es **idéntico byte a byte** al golden de §6: UTF-8 **sin BOM**, saltos `\n`, sangría de 2 espacios por nivel, una línea final `\n`, la declaración `<?xml version="1.0" encoding="UTF-8"?>`, la raíz con exactamente los 3 espacios de nombres del legacy (`TrainingCenterDatabase/v2`, `xsi`, `ns3` = `ActivityExtension/v2`), **una** `<Activity Sport="Other">` y el orden de elementos del golden. [dotnet] · `tcx:18-23`, `:35-42`, `:72-77`
- **AC-STR-09-02 · Formato de fecha y hora.** *Entonces* `Id`, `Lap StartTime` y `Time` usan `AAAA-MM-DDTHH:MM:SS±HH:MM` en **hora local**: año con 4 dígitos, el resto con 2, **sin** milisegundos (se truncan: `22:02:00.750Z` → `19:02:00-03:00`). `Id` = inicio de la sesión; `StartTime` de la vuelta = inicio + segundos del primer punto; `Time` del punto = inicio + sus segundos. La zona es la del equipo **al exportar** (como el legacy, que convierte al leer). [dotnet] · `tcx:24`, `:45-46`, `:130-143`, `:173`; `ws:57`
- **AC-STR-09-03 · Desfases.** *Dado* `T-2` (inicio `2026-11-05T22:02:00Z`), *entonces* `Id` vale `2026-11-05T22:02:00+00:00` en UTC (cero lleva `+`), `2026-11-06T03:32:00+05:30` en Asia/Kolkata, `2026-11-06T03:47:00+05:45` en Asia/Kathmandu y `2026-11-05T18:32:00-03:30` en America/St_Johns. *Dado* `T-2M` (inicio `2026-10-25T00:59:50Z`, puntos a 5 y 15 s) en Europe/Madrid, *entonces* `Id` = `2026-10-25T02:59:50+02:00`, el punto de 5 s = `2026-10-25T02:59:55+02:00` y el de 15 s = `2026-10-25T02:00:05+01:00`: **cada** instante lleva el desfase vigente en ese instante. [dotnet] · `tcx:130-134`
- **AC-STR-09-04 · Vueltas.** *Entonces* si **ningún** punto tiene índice de paso hay **una** vuelta con todos los puntos; si alguno tiene, se abre una vuelta nueva cada vez que el índice cambia entre puntos consecutivos (un índice que vuelve más tarde abre **otra** vuelta: `T-1` tiene 3). Una racha de puntos sin índice entre puntos con índice es su propia vuelta (DV-118). Las vueltas **no** dependen de la rutina: si se editó con menos pasos o se borró, el archivo sale igual (DV-118; el legacy fallaba). Con `F-9` n.º 1 hay 8 vueltas; n.º 2, 1; n.º 3 (cortada), 2. [dotnet] · `tcx:83-116`
- **AC-STR-09-05 · Datos de la vuelta.** *Entonces* cada `Lap` lleva, en este orden: `TotalTimeSeconds` = segundos del último punto − del primero; `DistanceMeters` = distancia del último − del primero (enteros, sin tope: si el monitor reinicia la distancia puede dar negativo, como el legacy); `Calories` (AC-STR-09-06); `Intensity` `Active`; `TriggerMethod` `Manual`; `Track`. Una vuelta de un punto tiene `0` y `0`. El hueco entre el último punto de una vuelta y el primero de la siguiente no se cuenta (paridad). [dotnet] · `tcx:36-42`, `:162-188`
- **AC-STR-09-06 · Calorías por vuelta.** *Entonces* `Calories` = `redondear(calorías totales × puntos de la vuelta ÷ (tiempo total ÷ 5))` con el redondeo de Dart (**mitad lejos de cero**, nunca el del banquero). Con 100 kcal y 600 s: 60 puntos → `50`, 3 → `3` (2,5), 2 → `2`, 1 → `1`. Con tiempo total ≤ 0 → `0` en todas (DV-119; el legacy daba error). [dotnet] · `tcx:177-179`
- **AC-STR-09-07 · Totales de una sesión cortada.** *Dado* una sesión **sin fin** o con tiempo total 0 (`T-3`, `F-9` n.º 3), *entonces* para las calorías se usan el tiempo y las calorías **de sus puntos** (el máximo de cada uno, F-RUT-18), la regla de reparación del legacy; con `T-3` (3 puntos de 5 a 15 s, calorías 1, 2 y 3) la única vuelta lleva `Calories` `3`. La base **no** se modifica (DV-119; el legacy escribía la sesión reparada al sincronizar). [dotnet] · `pp:164-188`; `ws:118-136`
- **AC-STR-09-08 · Puntos.** *Entonces* cada `Trackpoint` lleva `Time` y `DistanceMeters` (siempre, también `0`); `<HeartRateBpm><Value>n</Value></HeartRateBpm>` **solo si** el pulso > 0; `<Cadence>` = SPM redondeado (mitad lejos de cero: `24.4` → `24`, `24.6` → `25`, `22.5` → `23`) **solo si** el SPM **crudo** > 0 (`0.3` da `<Cadence>0</Cadence>`, como el legacy); y la potencia en `<Extensions><ns3:TPX><ns3:Watts>n</ns3:Watts></ns3:TPX></Extensions>` (5 líneas, como el golden) **solo si** > 0. No se escriben ritmo, velocidad, paladas ni posición. Los puntos van en el orden del tiempo del monitor (AC-HIS-19-02). [dotnet] · `tcx:44-69`
- **AC-STR-09-09 · Notas.** *Entonces* **no** hay metadatos `<!-- ROWMATE:… -->` (F-STR-10 descartado). Si la sesión tiene nombre de rutina no vacío, va `<Notes>{nombre}</Notes>` **después de la última vuelta** (orden del esquema TCX v2; DV-116, DV-117); en el simulador, `[SIM] {nombre}` o `[SIM]` en una libre (DV-123). Una libre del build normal no lleva `Notes`. El texto escapa `&` → `&amp;` (primero), `<` → `&lt;`, `>` → `&gt;`, `"` → `&quot;`, `'` → `&apos;`, y quita los caracteres que XML 1.0 no admite (controles salvo tab, `\n` y `\r`; DV-120). Con `T-1`: `Escalera &amp; &lt;Co&gt; &quot;A&quot; &apos;B&apos;`. [dotnet] · `tcx:26-30`, `:119-126`
- **AC-STR-09-10 · Qué sesiones se pueden exportar.** *Entonces* se exporta una sesión con al menos un punto, salvo que el tiempo **y** la distancia de sus puntos sean 0 (`T-5`), como el legacy al sincronizar. Las terminadas, las cortadas (`F-9` n.º 3), las libres y las de rutina borrada (`F-9` n.º 6) se exportan; `F-9` n.º 4 (sin puntos) y `T-5`, no. `T-6` (un punto en 0 s con 10 m) se exporta con una vuelta `0` s, `0` m, `0` kcal. [dotnet] · `pp:160-170`
- **AC-STR-09-11 · Determinista y solo lectura.** *Entonces* exportar dos veces la misma sesión en la misma zona da el mismo contenido. Exportar **solo lee** la sesión y sus puntos: no cambia la base, `prefs.json`, el Historial ni la sesión (sin campo nuevo de "exportada"). No hace ningún pedido de red. [dotnet] · [Editor]
- **AC-STR-09-12 · Rápido.** *Dado* una sesión de 3 h (2 160 puntos con todos los campos), *entonces* el armado tarda ≤ 200 ms en la PC [dotnet] y, en la app, el aviso llega en ≤ 1 s desde el toque sin ningún cuadro > 50 ms (el trabajo no corre en el cuadro de la UI). [Editor]

### Nombre y carpeta

- **AC-STR-09-13 · Nombre del archivo.** *Entonces* es `RowMate_{AAAA-MM-DD}_{HHmm}_{nombre}.tcx` con el inicio en hora local. `{nombre}` = el de la rutina guardado en la sesión, saneado: se conservan letras y dígitos Unicode (también `á`, `ñ`), `-` y `_`; todo lo demás (espacios incluidos) pasa a `-`; los `-` seguidos se juntan; se quitan los de los bordes; máximo 40 caracteres. Una libre usa `historyFree` (`Libre` / `Free`). Si el saneado queda vacío, no hay `_{nombre}`. Ejemplos con inicio `05/11/2026 18:40`: `Escalera` → `RowMate_2026-11-05_1840_Escalera.tcx`; `Escalera & <Co> "A" 'B'` → `…_Escalera-Co-A-B.tcx`; `Series 4×500 m` → `…_Series-4-500-m.tcx`; `Ñandú 2K` → `…_Ñandú-2K.tcx`; `***` → `RowMate_2026-11-05_1840.tcx`; `..\..\CON` → `…_CON.tcx`. Nunca lleva `\ / : * ? " < > |` ni controles (DV-121). [dotnet]
- **AC-STR-09-14 · Sin colisiones ni archivos a medias.** *Entonces* si el nombre ya existe en la carpeta (sin distinguir mayúsculas), se agrega `_2`, `_3`, … antes de `.tcx`: **nunca** se pisa un archivo. El archivo se escribe con otro nombre y recién completo toma el final: un error nunca deja un `.tcx` a medias ni temporales sueltos. [dotnet]
- **AC-STR-09-15 · Simulador.** *Dado* el build de simulador, *entonces* las sesiones se exportan (DV-123; el legacy bloqueaba la subida) con `_SIM` antes de la extensión y del sufijo de colisión (`…_Escalera_SIM.tcx`, `…_Escalera_SIM_2.tcx`) y `[SIM]` en `Notes` (AC-STR-09-09). El build normal nunca lleva la marca. [dotnet] · `api:84-87`
- **AC-STR-09-16 · Carpeta en Windows.** *Entonces* el archivo va a `{Documentos}\RowMate\TCX`, donde `{Documentos}` es la carpeta Documentos del usuario según Windows (también si OneDrive la redirige); `RowMate\TCX` se crea si falta. Sin diálogo de guardado (DV-121). Con `ROWMATE_DATA_DIR` (Editor, pruebas, builds de desarrollo y simulador con esa variable) va a `{ROWMATE_DATA_DIR}\exports` y **nunca** a Documentos. [dotnet] · [Device-Win]

### Detalle y avisos

- **AC-STR-09-17 · Botón.** *Dado* el Detalle cargado de una sesión exportable (AC-STR-09-10), *entonces* en la barra superior, a la derecha del título, hay un botón de ícono con área de toque de 48 dp (Windows: ícono de descarga; Android: ícono de compartir), con la ayuda `exportTcx` (`Exportar TCX`) al pasar el cursor y como nombre accesible. Mientras carga, con error de carga y en una sesión no exportable (`F-9` n.º 4) **no** aparece (como el botón de subida del legacy, que solo se veía cuando servía; DV-126). Se llega con Tab y se activa con Enter o Espacio. El título se recorta con `…` antes que tapar el botón. [Editor] · [Captura] CAP-01 a -03, CAP-09 · `sd:86-120`
- **AC-STR-09-18 · Exportando.** *Cuando* se toca, *entonces* el ícono pasa a un indicador circular de 18 dp con trazo de 2 (el del legacy al subir), con la ayuda `exportTcxExporting` (`Exportando…`), el botón queda deshabilitado y otro toque no hace nada; el indicador se ve al menos 300 ms para que no parpadee. Volver al Historial durante la exportación no la cancela: el aviso igual aparece. [Editor] · [Captura] CAP-04 · `sd:95-106`
- **AC-STR-09-19 · Aviso de éxito en Windows.** *Entonces* aparece el aviso de la app (el de AC-APP-03-05) por **6 s** con dos líneas: `exportTcxSaved` con `{folder}` = carpeta **abreviada** (`Guardado en Documentos\RowMate\TCX`, con `exportTcxDocuments` en el idioma de la app; con `ROWMATE_DATA_DIR`, `Guardado en ROWMATE_DATA_DIR\exports`) y el nombre del archivo en texto secundario; a la derecha, la acción `exportTcxOpenFolder` (`Abrir carpeta`). **Nunca** muestra la ruta completa ni el nombre de usuario de Windows. El texto se ajusta en varias líneas, nunca con `…` en el nombre. [Editor] · [Captura] CAP-05, -06, -10, -11
- **AC-STR-09-20 · Abrir carpeta.** *Cuando* se toca `Abrir carpeta`, *entonces* se abre el Explorador en la carpeta con el archivo **seleccionado**; si el archivo ya no está, se abre la carpeta; el aviso se cierra. En el workflow lo registra el destino falso y nunca abre el Explorador. [Editor] · [Device-Win] (demo del usuario)
- **AC-STR-09-21 · Error al escribir.** *Dado* que no se puede escribir (carpeta de solo lectura, disco lleno, sin acceso; el destino falso lo simula), *entonces* el botón vuelve a su ícono y aparece el aviso `exportTcxFailed` (`No se pudo exportar la sesión`) por 6 s con la acción `retry` (`Reintentar`), que vuelve a exportar. No queda ningún archivo a medias (AC-STR-09-14). El detalle técnico va al registro con la ruta abreviada como en AC-STR-09-19. [Editor] · [Captura] CAP-07, -08
- **AC-STR-09-22 · Avisos y otras pantallas.** *Entonces* el aviso de exportación usa la misma cápsula que los demás (los del remo y del pulsómetro tienen prioridad como en la It. 11) y, si llega otro aviso común, lo reemplaza. Con "reducir movimiento" entra y sale sin animar; con letra 1,3× y en L640 se lee entero; en el Detalle (página completa, sin barra de pestañas) va al pie de la pantalla, sin tapar la barra superior (*Corregido en la It. 15*, #72, QA-V12-03: decía "sin tapar la barra de pestañas"). [Editor] · [Captura] CAP-10, -11
- **AC-STR-09-23 · Textos.** *Entonces* las 7 claves de §4 existen en es y en con el texto exacto (252 por idioma) y la exportación no muestra ningún texto fuera del catálogo. Las claves de Strava (`profile*`, `navProfile`) siguen en el catálogo pero no se muestran en ninguna pantalla. [dotnet]

### Plataformas y demo

- **AC-STR-09-24 · Android: hoja de compartir.** *Cuando* se toca el botón en Android, *entonces* el archivo (nombre de AC-STR-09-13, tipo `application/vnd.garmin.tcx+xml`) se guarda en la caché de la app y se abre la hoja de compartir del sistema con el título `exportTcxShareTitle` (`Compartir sesión`), con permiso de lectura **temporal** para la app elegida (DV-122). No hay aviso de éxito; cancelar no muestra nada; un error muestra AC-STR-09-21. Los archivos de más de 24 h se borran en la exportación siguiente. El manifiesto **no** suma permisos (ni `INTERNET` ni de almacenamiento). [Device-Android] (NO VERIFICABLE; el Kotlin compila con `check.ps1`)
- **AC-STR-09-25 · Apple.** *Entonces* en iOS el botón abre la hoja de compartir y en macOS guarda en `~/Documents/RowMate/TCX` con `Mostrar en Finder` en lugar de `Abrir carpeta`. [Apple] (10b)
- **AC-STR-09-26 · Build de Windows.** *Dado* el build de desarrollo con `ROWMATE_DATA_DIR` temporal y `F-9` sembrada, *cuando* se exporta la n.º 1, *entonces* aparece `{ROWMATE_DATA_DIR}\exports\RowMate_2026-11-05_1840_Escalera.tcx` (igual al que arma `dotnet` para esa sesión) y, exportándola otra vez, `…_Escalera_2.tcx`; Documentos no cambia. QA no toca `Abrir carpeta`. En el build normal, la demo del usuario (§1, pasos 2-3). [Device-Win]
- **AC-STR-09-27 · Subida a Strava.** *Dado* un TCX de una sesión real con rutina, *cuando* el usuario lo sube en strava.com/upload, *entonces* Strava lo acepta y muestra la hora de inicio del Historial, la distancia, una vuelta por paso y los gráficos de pulso, cadencia y potencia. [Manual] (demo del usuario, §1 pasos 4-5)
- **AC-STR-09-28 · Nada de Strava.** *Entonces* la app sigue con **4 pestañas** (sin Perfil), sin botones, diálogos, íconos ni preferencias de Strava, sin pedidos de red y sin credenciales en el build (`BuildOutputInspector` o equivalente lo revisa). El diálogo de rutina completada no cambia. [Editor] · [Device-Win]

---

## 4. Textos nuevos (F-APP-10: 245 → 252 por idioma)

| Clave | es | en |
|---|---|---|
| `exportTcx` | `Exportar TCX` | `Export TCX` |
| `exportTcxExporting` | `Exportando…` | `Exporting…` |
| `exportTcxSaved` | `Guardado en {folder}` | `Saved to {folder}` |
| `exportTcxDocuments` | `Documentos` | `Documents` |
| `exportTcxOpenFolder` | `Abrir carpeta` | `Open folder` |
| `exportTcxFailed` | `No se pudo exportar la sesión` | `Couldn't export the session` |
| `exportTcxShareTitle` | `Compartir sesión` | `Share session` |

Se reusan `retry` (`Reintentar` / `Retry`) y `historyFree` (`Libre` / `Free`). `…` es el carácter U+2026.

---

## 5. Checklist de paridad

| Comportamiento legacy | Fuente | AC |
|---|---|---|
| Cabecera, espacios de nombres, `Sport="Other"`, `Id` = inicio | `tcx:18-24` | -01, -02 |
| Hora local con desfase, sin milisegundos | `tcx:130-143` | -02, -03 |
| Vueltas por cambio de índice; una sola si no hay índices | `tcx:83-116` | -04 |
| Duración y distancia último − primero, `Active`, `Manual` | `tcx:36-41`, `:171-175` | -05 |
| Calorías proporcionales a los puntos, `round()` | `tcx:177-179` | -06 |
| Distancia siempre; pulso, cadencia y watts solo si > 0 | `tcx:48-68` | -08 |
| Escape de `& < > " '` | `tcx:119-126` | -09 |
| Reparación (tiempo y calorías de los puntos), sesiones sin datos se saltean | `pp:155-191` | -07, -10 |
| Botón en la barra del Detalle, indicador 18 dp al trabajar | `sd:86-118` | -17, -18 |
| **Cambia (decidido):** sin metadatos, `Notes` al final, vueltas sin depender de la rutina, tiempo 0 sin error, simulador exporta | `tcx:26-30`, `:103`, `:179`; `api:84-87` | -06, -07, -09, -15 |

---

## 6. Sesiones de prueba y golden

Zona UTC−03:00 salvo que se diga. Todos los datos son inventados.

- **T-1**: rutina `Escalera & <Co> "A" 'B'`, inicio `2026-11-05T22:02:00.750Z`, terminada, tiempo total 600 s, calorías 100. Puntos (s, m, spm, W, pulso, paso): `5, 20, 24.4, 150, 0, 0` · `10, 41, 24.6, 0, 140, 0` · `15, 62, 0, 155, 141, 0` · `20, 80, 22.5, 160, 142, 1` · `25, 100, 23.5, 0, 0, 1` · `30, 121, 24.0, 150, 143, 0`.
- **T-2**: libre, inicio `2026-11-05T22:02:00Z`, un punto en 0 s con 10 m. **T-2M**: libre, inicio `2026-10-25T00:59:50Z`, puntos en 5 s (10 m) y 15 s (30 m), Europe/Madrid.
- **T-3**: rutina `Corta`, **sin fin**, totales 0; puntos `5, 20`, `10, 41`, `15, 62` con calorías 1, 2 y 3, todos del paso 0.
- **T-5**: libre, un punto en 0 s con 0 m. **T-6**: libre, un punto en 0 s con 10 m, totales 0.
- No hay sesión **T-4**: el número quedó libre al definir las sesiones y no se usa (*Corregido en la It. 15*, #72).

**Golden de `T-1`** (cada línea termina en `\n`):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<TrainingCenterDatabase xmlns="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:ns3="http://www.garmin.com/xmlschemas/ActivityExtension/v2">
  <Activities>
    <Activity Sport="Other">
      <Id>2026-11-05T19:02:00-03:00</Id>
      <Lap StartTime="2026-11-05T19:02:05-03:00">
        <TotalTimeSeconds>10</TotalTimeSeconds>
        <DistanceMeters>42</DistanceMeters>
        <Calories>3</Calories>
        <Intensity>Active</Intensity>
        <TriggerMethod>Manual</TriggerMethod>
        <Track>
          <Trackpoint>
            <Time>2026-11-05T19:02:05-03:00</Time>
            <DistanceMeters>20</DistanceMeters>
            <Cadence>24</Cadence>
            <Extensions>
              <ns3:TPX>
                <ns3:Watts>150</ns3:Watts>
              </ns3:TPX>
            </Extensions>
          </Trackpoint>
          <Trackpoint>
            <Time>2026-11-05T19:02:10-03:00</Time>
            <DistanceMeters>41</DistanceMeters>
            <HeartRateBpm><Value>140</Value></HeartRateBpm>
            <Cadence>25</Cadence>
          </Trackpoint>
          <Trackpoint>
            <Time>2026-11-05T19:02:15-03:00</Time>
            <DistanceMeters>62</DistanceMeters>
            <HeartRateBpm><Value>141</Value></HeartRateBpm>
            <Extensions>
              <ns3:TPX>
                <ns3:Watts>155</ns3:Watts>
              </ns3:TPX>
            </Extensions>
          </Trackpoint>
        </Track>
      </Lap>
      <Lap StartTime="2026-11-05T19:02:20-03:00">
        <TotalTimeSeconds>5</TotalTimeSeconds>
        <DistanceMeters>20</DistanceMeters>
        <Calories>2</Calories>
        <Intensity>Active</Intensity>
        <TriggerMethod>Manual</TriggerMethod>
        <Track>
          <Trackpoint>
            <Time>2026-11-05T19:02:20-03:00</Time>
            <DistanceMeters>80</DistanceMeters>
            <HeartRateBpm><Value>142</Value></HeartRateBpm>
            <Cadence>23</Cadence>
            <Extensions>
              <ns3:TPX>
                <ns3:Watts>160</ns3:Watts>
              </ns3:TPX>
            </Extensions>
          </Trackpoint>
          <Trackpoint>
            <Time>2026-11-05T19:02:25-03:00</Time>
            <DistanceMeters>100</DistanceMeters>
            <Cadence>24</Cadence>
          </Trackpoint>
        </Track>
      </Lap>
      <Lap StartTime="2026-11-05T19:02:30-03:00">
        <TotalTimeSeconds>0</TotalTimeSeconds>
        <DistanceMeters>0</DistanceMeters>
        <Calories>1</Calories>
        <Intensity>Active</Intensity>
        <TriggerMethod>Manual</TriggerMethod>
        <Track>
          <Trackpoint>
            <Time>2026-11-05T19:02:30-03:00</Time>
            <DistanceMeters>121</DistanceMeters>
            <HeartRateBpm><Value>143</Value></HeartRateBpm>
            <Cadence>24</Cadence>
            <Extensions>
              <ns3:TPX>
                <ns3:Watts>150</ns3:Watts>
              </ns3:TPX>
            </Extensions>
          </Trackpoint>
        </Track>
      </Lap>
      <Notes>Escalera &amp; &lt;Co&gt; &quot;A&quot; &apos;B&apos;</Notes>
    </Activity>
  </Activities>
</TrainingCenterDatabase>
```

Calorías: 100 × 3 ÷ 120 = 2,5 → `3`; 100 × 2 ÷ 120 = 1,67 → `2`; 100 × 1 ÷ 120 = 0,83 → `1`. Con el legacy, el mismo archivo salvo `Notes` (que iba antes de la primera vuelta y era el comentario de metadatos).

---

## 7. Capturas (set `it12`)

**Reglas:** las de la 6a §7.1 (español salvo que se diga, reloj de prueba, `ROWMATE_DATA_DIR` temporal, dos corridas iguales byte a byte, sin rutas de la PC), en `docs/migration/iterations/12-exportar-tcx/snapshots/` (y no en otra carpeta). Base `F-9`, destino de exportación **falso** que muestra `Documentos\RowMate\TCX` y escribe en la carpeta temporal.

| ID | Tamaño | Estado | Qué tiene que verse |
|---|---|---|---|
| **CAP-01** | W1280 | Detalle n.º 1 | Botón `Exportar TCX` a la derecha del título `Escalera` |
| **CAP-02** | TV | Detalle n.º 1 | Botón a la vista en vertical; título sin pisarlo |
| **CAP-03** | TH | Detalle n.º 1 | Botón en horizontal |
| **CAP-04** | W1280 | Detalle n.º 1, exportando (destino falso en pausa) | Indicador de 18 dp en lugar del ícono |
| **CAP-05** | W1280 | Detalle n.º 1, exportado | Aviso `Guardado en Documentos\RowMate\TCX`, `RowMate_2026-11-05_1840_Escalera.tcx` y `Abrir carpeta` |
| **CAP-06** | TV | Igual que CAP-05 | Aviso de 16 a 16, en varias líneas, sin cortes |
| **CAP-07** | W1280 | Detalle n.º 1, error de escritura | `No se pudo exportar la sesión` y `Reintentar`; botón con su ícono |
| **CAP-08** | TH | Igual que CAP-07 | Error en horizontal |
| **CAP-09** | TV | Detalle n.º 4 (sin puntos) | `No hay datos de telemetría` y **sin** botón |
| **CAP-10** | W1280, inglés, letra 1,3× | Exportado | `Saved to Documents\RowMate\TCX` y `Open folder`, sin cortes |
| **CAP-11** | L640 | Exportado | Aviso entero al pie de la pantalla, sin tapar la barra superior (*Corregido en la It. 15*, QA-V12-03: el Detalle no tiene barra de pestañas) |
| **CAP-12** | W1280 | Cursor sobre el botón | Ayuda `Exportar TCX` |

---

## 8. Decisiones (valen por defecto; van al gate)

| ID | Decisión | Por defecto | Si se rechaza |
|---|---|---|---|
| DV-116 | Sin metadatos `ROWMATE`; `Notes` = nombre de la rutina | Se acepta | Sin `Notes` |
| DV-117 | `Notes` después de la última vuelta (orden del esquema; Garmin es más estricto que Strava) | Se acepta | Antes de la primera vuelta, como el legacy |
| DV-118 | Las vueltas no dependen de la rutina; rachas sin índice son su propia vuelta | Se acepta | — (el legacy fallaba) |
| DV-119 | Sesión cortada con tiempo y calorías de los puntos, sin tocar la base; tiempo 0 → 0 kcal | Se acepta | Reparar y guardar en la base |
| DV-120 | Quitar los caracteres que XML 1.0 no admite | Se acepta | Escapar solo los 5 del legacy |
| DV-121 | Windows: `Documentos\RowMate\TCX`, sin diálogo; nombre `RowMate_{fecha}_{hora}_{nombre}.tcx` | Se acepta | Diálogo "Guardar como" (issue) |
| DV-122 | Android: hoja de compartir | Se acepta | Guardar en Descargas (issue) |
| DV-123 | El simulador exporta con `_SIM` y `[SIM]` | Se acepta | No exporta, como el legacy |
| DV-124 | Solo desde el Detalle, no desde la tarjeta del Historial | Se acepta | Acción en la tarjeta (issue) |
| DV-125 | `Sport="Other"`: el usuario elige Remo en Strava | Paridad (el esquema TCX no tiene remo) | FIT (issue) |
| DV-126 | Botón solo con sesión exportable | Se acepta | Botón deshabilitado |
| DV-127 | Distancia del monitor tal cual (con el simulador sin reconectar arranca de lo acumulado, como en la It. 9) | Paridad | Restar el primer punto (issue de la It. 9) |

---

## 9. Fuera de alcance

Todo Strava (decisión del usuario del 2026-11-09): conexión y OAuth, API, credenciales y su almacenamiento seguro, regreso por `rowmate://`, pestaña Perfil, preferencia de subida, subida al terminar y su diálogo, botón `Subir a Strava`, íconos de sincronizado, sincronización en ambos sentidos, importación, subidas pendientes, limpieza y la reparación que escribía en la base. También: exportar varias sesiones, otros formatos (FIT, GPX, CSV) e importar TCX.

## 10. Para issues

1. Acción `Exportar TCX` en la tarjeta del Historial (menú o pulsación larga) y exportar varias o todas.
2. Diálogo "Guardar como" / elegir carpeta en Windows; en Android, además, guardar en Descargas (MediaStore).
3. Exportar FIT (Strava lo reconoce como remo, con nombre de actividad) y `<Creator>` `RowMate` con versión en el TCX.
4. `Abrir carpeta` que falla no avisa nada.
5. Limpiar restos de Strava en Unity: la opción de la pestaña Perfil (`ShellOptions`, `TabDestinationId`), el comentario de `lastFinishedSessionId` en `WorkoutEngine`, y decidir en la It. 14 si las claves `profile*` salen del catálogo (hoy quedan por la paridad del catálogo de la It. 1). La columna `strava_activity_id` se queda por compatibilidad de la base.
6. Una vuelta con distancia negativa si el monitor reinicia la distancia (paridad hoy).

## 11. Cambios por gate ([03-qa-gate](03-qa-gate.md))

- **m-04**: AC-STR-09-08 aclara que la condición de la cadencia es sobre el SPM crudo (> 0), como `tcx:52-66` y el diseño (motivo: con el valor redondeado, `0.3` no tendría `<Cadence>` y el legacy la escribe).
