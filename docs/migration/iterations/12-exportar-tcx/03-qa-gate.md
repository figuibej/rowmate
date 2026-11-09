# It. 12 — Exportar TCX: gate de QA

| Campo | Valor |
|---|---|
| Autor | rowmate-qa |
| Fecha | 2026-11-09 |
| Entrada | [01-po-spec](01-po-spec.md) (28 AC), [02-tech-design](02-tech-design.md) (DT12-01 a -20), [00-arte-exportar](00-arte-exportar.md); diffs de `ROADMAP.md`, `STATUS.md` y `product/` (replanificación sin Strava) |
| Legacy | [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `3ae0f3a` (= el de la spec), solo lectura: `lib/core/strava/tcx_builder.dart` entero, `workout_session.dart:1-175`, `profile_provider.dart:140-200`, `session_detail_screen.dart:60-140`, `strava_api_service.dart:55-140`. No se leyó el módulo de credenciales. `git status --porcelain` vacío antes y después |
| dotnet (línea de base, sin código de la 12) | `run-tests.ps1 -ArtifactsPath <scratch>/it12tcx/gate`: **3776 en total, 3737 pasan, 39 omitidas, 0 fallan** (catálogo hoy en 245 claves: `CatalogParityTests.cs:210`) |
| Veredicto | **APROBADO CON OBSERVACIONES**: sin bloqueantes; 2 mayores a resolver en la implementación (no hace falta otra vuelta de diseño) |

## 1. Paridad del TCX (verificada contra el legacy)

| Punto | Legacy | Spec / diseño | Resultado |
|---|---|---|---|
| Cabecera, 3 espacios de nombres, `Sport="Other"`, `Id` = inicio | `tcx:18-24` | AC-01, -02; DT12-01 | OK. La raíz del golden es la concatenación exacta de `tcx:19-21` |
| `writeln` = `\n`, sangría de 2 | `tcx:18-77` | DT12-01 (sin `AppendLine`), `.gitattributes *.tcx eol=lf` | OK |
| Hora local con desfase, sin ms, `+` para cero, `-03:30` con `inHours.abs()` / `inMinutes.abs() % 60` | `tcx:130-143` | AC-02, -03; DT12-02 | OK. Recalculado: Kolkata 03:32+05:30, Kathmandu 03:47+05:45, St. John's 18:32−03:30 (el horario de verano terminó el 1/11/2026), Madrid 02:59:50+02:00 / 02:59:55+02:00 / 02:00:05+01:00 (25/10/2026 es el último domingo) |
| Instante de punto y vuelta = inicio + segundos (Dart suma sobre el instante: cada uno con su desfase) | `tcx:45`, `:173` | DT12-02 | OK |
| Vueltas por cambio de `stepIndex` (comparando `int?`), una sola sin índices; el paso no se usa | `tcx:83-116`, `:162-188` | AC-04; DV-118 | OK. Con `steps == null`, el legacy ya hace "racha de null = su vuelta" |
| Duración y distancia último − primero, sin tope | `tcx:174-175` | AC-05 | OK |
| Calorías `round(cal × n / (t / 5))`, `round` de Dart | `tcx:179` | AC-06; DT12-04 | OK. El legacy con `t = 0` da `Infinity/NaN.round()` → excepción (DV-119 lo cambia a 0) |
| Reparación en memoria (`finishedAt == null || totalTime == 0` → máximos de los puntos); saltear sin puntos y con tiempo y distancia 0 | `pp:159-188` | AC-07, -10; DT12-04, -05 | OK |
| Pulso, cadencia (`strokeRate.round()`) y watts solo si > 0 (sobre el valor crudo) | `tcx:52-66` | AC-08 | OK (ver m-04) |
| Escape `&` primero y los otros 4 | `tcx:119-126` | AC-09; DT12-02 | OK |
| Golden de T-1 | — | spec §6 | **Recalculado a mano: correcto** (vueltas 3; 10 s/42 m/3 kcal, 5 s/20 m/2 kcal, 0/0/1 kcal; cadencias 24, 25, 23, 24, 24; omisiones y `Notes` escapado) |
| Simulador | `api:84-87` (bloquea) | DV-123: exporta con `_SIM` y `[SIM]` | Cambio decidido, con test (`FN`, `ES`) |

STR §5 (casos de verificación del TCX): los 9 tienen AC y test planificado.

## 2. Issues

| ID | Sev. | Resp. | Issue | Evidencia | Corrección |
|---|---|---|---|---|---|
| QA-G12-01 | **mayor** | architect | La prueba de AC-STR-09-28 ("sin credenciales en el build") depende de `StravaNeedles`, que el diseño no define, y a la vez el diseño **deja** los restos de Strava (§11.1). Con una aguja amplia (`strava`, `Strava`) el inspector del build normal falla siempre; sin lista, la prueba no prueba nada | `02-tech-design` §1.2 y §6.3 (`BOI.StravaNeedlesFlagged`); hoy hay `Strava` en 21 fuentes que no son de test (`WorkoutSession.cs`, `DatabaseSchema.cs`, `Tokens.cs`, `L10nKeys.cs`…) y en los textos `profile*` del catálogo | Fijar las agujas de **credenciales y red**: `strava.com/api`, `strava.com/oauth`, `client_secret`, `refresh_token`, `rowmate://`, `api/v3/uploads`. `BOI` prueba que cada una se detecta **y** que `strava_activity_id`, `StravaActivityId` y los textos `profile*` no disparan |
| QA-G12-02 | **mayor** | architect | AC-STR-09-24 tiene partes verificables con dotnet sin test: la purga de `.tcx` de más de 24 h (`FolderExportWriter.DeleteOlderThan`, netstandard) y el tipo `application/vnd.garmin.tcx+xml` y el título `exportTcxShareTitle` en el `ExportRequest`. La matriz solo lista el manifiesto y `check.ps1` | `02-tech-design` §3.2, §3.4 y §6.3 (fila AC-STR-09-24) | Sumar `FW.DeleteOlderThanOnlyOldTcx` (borra los `.tcx` viejos; deja los nuevos, otras extensiones y los `.part`) y `ES.RequestHasTcxMimeAndShareTitle` (con `FakeFileExporter(delivery: Share)`: sin aviso de éxito y con el título del idioma vigente) |

### Menores (una línea cada uno)

- **m-01** (designer) `00-arte-exportar` contradice la spec (botón tonal en el Resumen y sin ícono, carpeta `Exportaciones`, claves `export*`/`folderDocuments`, antiparpadeo 250/400 ms, recorte en el medio). El diseño §9 ya lo resuelve a favor de la spec; `04-design-notes.md` tiene que reflejar §9.2 y no la base del arte.
- **m-02** (architect) `ExportLocations.Resolve` no tiene que crear `Documentos\RowMate\TCX` al arrancar (solo `FolderExportWriter` al escribir): si no, abrir el Editor o un build sin `ROWMATE_DATA_DIR` toca Documentos. Un caso en `ELT`.
- **m-03** (architect) `TcxFileName.Sanitize`: recortar a 40 **y después** quitar el `-` del borde (un `-` en la posición 40 quedaría al final). Un caso en `FN`.
- **m-04** (po) AC-STR-09-08: aclarar que la condición de la cadencia es sobre el SPM **crudo** (> 0), como el legacy y el diseño: `0.3` spm da `<Cadence>0</Cadence>`.
- **m-05** (architect) AC-STR-09-26 fija `RowMate_2026-11-05_1840_Escalera.tcx`, que solo vale con el equipo en UTC−03:00; el humo tendría que comparar también el nombre con el de `tools/sessions tcx` en la zona local.
- **m-06** (architect) `TED.NoNetworkAccess` es una revisión, no un test: cambiarlo por un test de que `App/Export` y `Platform/Export` no referencian `System.Net`, `UnityWebRequest` ni `HttpClient`, o sacarlo de la matriz.
- **m-07** (architect) AC-STR-09-17 en Android (ícono de compartir) no tiene caso: `TED` con `FakeFileExporter(delivery: Share)` verifica el glifo `Share`.
- **m-08** (architect) `RmExport.share` devuelve `true` antes de que corra el bloque de `runOnUiThread`: una excepción ahí no llega al aviso de error (AC-STR-09-21). Atraparla adentro y registrarla; queda NO VERIFICABLE igual.
- **m-09** (architect) `TB.ThreeHoursUnder200Ms`: con calentamiento y mediana de varias corridas, para que no sea inestable en el CI.
- **m-10** (architect) `TestZones.Madrid`: en `CreateCustomTimeZone` el fin del horario de verano va en hora de verano (03:00) y el inicio en hora estándar (02:00); probar los tres instantes de AC-STR-09-03 también en Unity (R-02).
- **m-11** (architect) `02-tech-design` §12 apunta a `03-qa-harmony.md`; el gate es este archivo (`03-qa-gate.md`, como en la 11).

## 3. Cobertura AC → test (del diseño §6.3)

Los 28 AC tienen componente y test o marca: 01-16 y 23 con `dotnet`; 17-22 con PlayMode y capturas; 24 con manifiesto y `check.ps1` (+ QA-G12-02); 25 a la 10b; 26 con `build.ps1 -ExportSmoke`; 27 manual; 28 con la barra de 4 pestañas y el inspector (+ QA-G12-01). Sin contradicciones spec ↔ diseño técnico: las del arte quedan resueltas en el diseño §9 (m-01).

## 4. Carpetas del usuario y repo público

- Ninguna corrida planificada escribe en Documentos ni en Descargas: dotnet con carpetas temporales (`FolderExportWriter`), Editor, PlayMode y capturas con `FakeFileExporter`, `TEF` con el exportador real sobre una carpeta temporal, el humo en un build de **desarrollo** con `ROWMATE_DATA_DIR` (y `-Simulator` también fuerza desarrollo en `build.ps1:90`, así que respeta la variable). `ExplorerRevealTests` solo prueba cadenas. Ver m-02.
- `ROWMATE_DATA_DIR` y `-rowmate-export-smoke` solo bajo `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, con agujas del inspector (DT12-10, DT12-20).
- Los 3 documentos y los diffs no tienen rutas de la PC, usuarios ni datos del equipo; los datos de T-1 a T-6 son inventados. `ExportLog.Scrub` y la etiqueta abreviada evitan la ruta completa en avisos y registros.

## 5. Replanificación sin Strava (ROADMAP, STATUS, product)

Coherente. 25 IDs descartados (de la vieja 12: 13 canónicos + 3 alias; de la 13: 7 + 2) y 133 vivos: el recuento por iteración suma 133 (1 en la 12). F-STR-09 vive en la 12 y toca la 10b ([Apple]); F-HIS-09, -19, F-BLE-13, F-APP-03, -10 y -13 la nombran en "También toca". La 13 figura DESCARTADA en el ROADMAP y el STATUS, la 10b quedó sin login ni almacenamiento seguro, la marca [Strava] pasó a [Manual] y no quedan carpetas `12-strava-subida` ni `13-*`. `strava-profile.md` conserva la ficha del legacy como registro, con la nota de descarte.

## 6. Para issues

1. Lo de m-05 a m-10 si no entra en la implementación.
2. Lo de la spec §10 y el diseño §11 (restos de Strava, glifos de la fuente, pausa del aviso con el puntero, `SHOpenFolderAndSelectItems`).
