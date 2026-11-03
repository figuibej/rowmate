# ADR 021: Las preferencias de la app (prefs.json en la carpeta de la base, guardado entero fuera del hilo principal)

- **Estado:** Aceptado (2026-10-30), con el diseño de la It. 5 ([05-series-parciales/02-tech-design](../../iterations/05-series-parciales/02-tech-design.md) §2.7, DT5-05) y su gate de QA (QA-G5T-05, QA-G5T-07)
- **Ámbito:** Core.Ports (`IPreferences`), Core.Common (`MemoryPreferences`), Infrastructure.Preferences (`JsonFilePreferences`, `PreferencesFiles`), App (`SeriesPanelPrefs`, `AppServices.Preferences`), Bootstrap (`DataLocation`, `AppRoot`); It. 5 (los paneles de series) y las que guarden preferencias después (It. 8, 11 y 12)

## Contexto

La It. 5 es la primera que recuerda algo del usuario fuera de la base: si los paneles de parciales y de repeticiones del inmersivo quedaron abiertos o cerrados (`immersive.lapsExpanded`, `immersive.repsExpanded`; [rowmate-legacy](https://github.com/figuibej/rowmate-legacy) `lib/features/workout/series_panels.dart:120-156`). El legacy usa `shared_preferences`: un caché en memoria que cambia en el acto y una escritura que puede terminar después o fallar sin aviso. ARCHITECTURE §9.4 preveía "`persistentDataPath/prefs.json`", pero en Android `persistentDataPath` es la carpeta externa (§9.1) y las pruebas necesitan aislarse del archivo del usuario (AC-HIS-20-04).

## Decisión

1. **Un puerto en Core** (`IPreferences`): `TryGetBool`, `TryGetString`, `SetBool`, `SetString`, `Remove`, `IsLoaded` y `Loaded`. "No existe" y "otro tipo" dan los dos `false` (el llamador usa su valor por defecto). `Set*` cambia la memoria en el acto y pide el guardado. Hilo principal.
2. **Dónde**: `prefs.json` (`PreferencesFiles.Name`), **el mismo nombre para el build normal y el de simulador** (P-11), en **la carpeta de la base** (`DataLocation.DatabaseDirectory`: la privada de la app; en Android la interna), y en el Editor y en desarrollo en la de `ROWMATE_DATA_DIR`, como la base. Nunca se leen ni se importan las preferencias del legacy (Q-11; `LegacyIsolationTests`).
3. **Leer**: una vez, al construirse `JsonFilePreferences`, en el hilo que lo construye (el arranque; es un archivo de unos cientos de bytes), con el lector JSON del Core (`JsonParser`, sin deserialización genérica). Si el archivo falta, está vacío, no se puede leer o no es un objeto JSON, arranca vacío, sin aviso (una línea Info en el registro, sin la ruta) y el próximo `Set` escribe un archivo válido. Una clave de otro tipo da `false` sola; todas las claves se conservan tal cual (las que la app no conoce también, con sus números exactos) hasta que un `Set` las reemplace.
4. **Guardar**: el texto entero (JSON compacto, UTF-8) se arma en el hilo del `Set`; un ejecutor serie fuera del hilo principal solo escribe bytes: `prefs.json.tmp` y después lo pone en lugar de `prefs.json` (`File.Replace` si existe, `File.Move` si no). Un corte deja el viejo o el nuevo, nunca uno a medias. A lo sumo una escritura en curso y una pendiente: un `Set` nuevo reemplaza los bytes pendientes y la última siempre gana. `FlushAsync()` espera lo pendiente (las pruebas y el cierre de la app, con un tope de 1 s como la base).
5. **Si guardar falla**: una línea Warning (sin la ruta ni el mensaje del sistema, que la trae) y nada más. El valor en memoria queda: la misma ejecución sigue con lo que tocó el usuario y, después de reiniciar, vale lo último guardado (como el caché de `shared_preferences`, AC-WKT-18-11).
6. **Composición**: `AppServices.Preferences` (opcional): `AppRoot` pasa `JsonFilePreferences`; sin preferencias, `AppComposer` usa `MemoryPreferences`. `AppContext.SeriesPanels` (uno por app) las usa; `AppContext.Preferences` las expone. Las dispone quien las creó.
7. **Aislamiento de las pruebas**: dotnet y las pruebas de pantalla usan memoria (`MemoryPreferences`, `RecordingPreferences`) o un archivo en una carpeta temporal propia (`TempDatabase`); el arnés, una `MemoryPreferences` por toma; el arranque de las pruebas de Unity y los scripts ponen `ROWMATE_DATA_DIR`. Ninguna prueba calcula la ruta real y escribe en ella. El precalentado del HUD usa un estado local (`LocalSeriesPanelsExpansion`) y nunca toca las preferencias (QA-G5T-05).

## Alternativas descartadas

- **`PlayerPrefs` de Unity**: en Windows va al registro, no se puede aislar en las pruebas sin tocar el del usuario y no se compila con dotnet.
- **`persistentDataPath/prefs.json`** (ARCHITECTURE §9.4 antes de la It. 5): en Android es la carpeta externa; con la carpeta de la base, `ROWMATE_DATA_DIR` aísla las pruebas sin código nuevo.
- **Escribir en el hilo principal**: un guardado lento en el teléfono costaría un cuadro en el toque.
- **Newtonsoft `JObject`** (lo que proponía el diseño): Infrastructure no referencia Newtonsoft (`overrideReferences` sin la dll); el lector del Core ya distingue tipos, conserva el orden y el texto exacto de los números, y no suma referencias.

## Consecuencias

- Un solo archivo de preferencias para los dos builds, junto a sus bases (que sí tienen nombres distintos).
- El reemplazo atómico con IL2CPP en Android queda NO VERIFICABLE sin el módulo (R5-04): si fallara, el próximo arranque lee el archivo viejo, nunca uno a medias.
- Las próximas preferencias (el pulsómetro recordado de la It. 11, la escena de la 8, Strava de la 12) suman claves al mismo archivo; las que no conoce una versión se conservan.
- P-12: si en la It. 11 un archivo dañado debiera avisar (se perdería más que dos interruptores), se revisa el punto 3.

## Cambios de la It. 5

Detalle en [05-series-parciales/02-tech-design](../../iterations/05-series-parciales/02-tech-design.md) §2.7, §2.8 y "Cambios por gate QA" (QA-G5T-05, QA-G5T-07). Lo implementa WP-A (Core, App, Infrastructure y sus pruebas: `JsonFilePreferencesTests`, `SeriesPanelPrefsTests`); `DataLocation.PreferencesFile` y `AppRoot`, WP-B2.
