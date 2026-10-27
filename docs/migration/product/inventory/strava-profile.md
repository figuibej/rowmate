# Inventario de producto: Strava y pantalla Perfil

- **Área:** `STR` (abarca la integración con Strava y la pantalla Perfil, que es su única pantalla propia).
- **Rol:** RowMate PO. Este documento es el **contrato de paridad**: lo que no está acá se pierde en la migración.
- **Fuente:** `https://github.com/figuibej/rowmate-legacy` (solo lectura). Leídos completos: `lib/core/strava/` (salvo `strava_secrets.dart`, que no se leyó), `lib/features/profile/`, y los puntos de contacto en `lib/features/workout/`, `lib/features/history/`, `lib/core/database/database_service.dart`, `lib/core/models/workout_session.dart`, `lib/main.dart`, `.arb`, manifiestos de plataforma, `README.md`, `CLAUDE.md` y specs.
- **Regla:** cuando un doc y el código difieren, manda el código. Las diferencias están en la sección 4.
- **Tests legacy:** **no hay ningún test** de Strava ni de Perfil. Solo `test/session_stats_test.dart` cubre de forma indirecta el cálculo de estadísticas que usan la reparación y la importación. La sección 5 propone casos de verificación para cubrir ese hueco.

---

## 0. Qué es y por qué importa

Strava es **opcional**: la app funciona completa sin conexión a internet. Cuando la build tiene credenciales, aparece la pestaña **Perfil**. Desde ahí el remero:

1. conecta su cuenta de Strava,
2. elige si las sesiones se suben solas, si se le pregunta o si las sube a mano,
3. sube en lote todo lo que falta (y la app repara antes las sesiones mal guardadas),
4. importa a su historial las actividades de remo que ya tiene en Strava,
5. ve y sube las sesiones pendientes, y
6. borra las sesiones vinculadas a Strava para volver a sincronizarlas.

Cada sesión subida lleva **pulso, cadencia, potencia y distancia por punto**, una **vuelta por paso de la rutina** y, en la descripción, **metadatos ocultos de la rutina**. Con esos metadatos, al importar, la app puede recuperar el tipo de cada intervalo.

## 1. Mapa de funcionalidades

| ID | Funcionalidad | Lógica pura | Tests legacy |
|---|---|---|---|
| F-STR-01 | Disponibilidad de Strava según credenciales | no | ninguno |
| F-STR-02 | Conectar con Strava (autorización) | no | ninguno |
| F-STR-03 | Sesión persistente y renovación de credenciales | parcial (regla de vencimiento) | ninguno |
| F-STR-04 | Desconectar Strava | no | ninguno |
| F-STR-05 | Preferencia de subida (auto / preguntar / manual) | no | ninguno |
| F-STR-06 | Subida al terminar un entrenamiento | no | ninguno |
| F-STR-07 | Subir una sesión (operación individual) | no | ninguno |
| F-STR-08 | Envío a Strava, confirmación y duplicados | parcial (lectura del duplicado) | ninguno |
| F-STR-09 | Archivo de actividad TCX | **sí** | ninguno |
| F-STR-10 | Metadatos de rutina ocultos en la descripción | **sí** | ninguno |
| F-STR-11 | Sincronizar a Strava (subida en lote + auto-reparación) | parcial (regla de reparación) | indirecto: `session_stats_test.dart` |
| F-STR-12 | Sincronizar desde Strava (importación) | no | ninguno |
| F-STR-13 | Reconstrucción de la telemetría importada | **sí** | indirecto: `session_stats_test.dart` |
| F-STR-14 | Tarjeta "Subidas pendientes" | no | ninguno |
| F-STR-15 | Mantenimiento: limpiar sesiones de Strava | no | ninguno |
| F-STR-16 | Pantalla Perfil: estructura y estados | no | ninguno |
| F-STR-17 | Estado de sincronización en Historial y Detalle | no | ninguno |
| F-STR-18 | Strava en modo simulador | no | ninguno |

---

## 2. Contratos transversales

### 2.1 Datos persistidos

| Dónde | Clave / columna | Valor | Escribe | Lee | Borra |
|---|---|---|---|---|---|
| Almacenamiento seguro del SO | `strava_access_token` | token de acceso | conectar, renovar | "¿conectado?", cada llamada | desconectar |
| Almacenamiento seguro | `strava_refresh_token` | token de renovación | conectar, renovar | renovar | desconectar |
| Almacenamiento seguro | `strava_expires_at` | segundos epoch, como texto (p. ej. `"1730000000"`) | conectar, renovar | antes de cada llamada | desconectar |
| Almacenamiento seguro | `strava_athlete_name` | `"<firstname> <lastname>"` sin espacios en los extremos | conectar/renovar, solo si la respuesta trae atleta | Perfil | desconectar |
| Almacenamiento seguro | `strava_athlete_avatar` | URL de la foto del atleta, o `""` | igual que el nombre | Perfil | desconectar |
| SharedPreferences | `strava_upload_pref` | `"auto"` \| `"ask"` \| `"manual"` (falta o valor desconocido = `auto`) | Perfil | Perfil, pantallas de entrenamiento | nunca |
| Base de datos, tabla `workout_sessions` | columna `strava_activity_id` (TEXT, admite nulo) | ID de la actividad en Strava, como texto | subida OK, importación | Historial, Detalle, Perfil, sincronizaciones | "Limpiar sesiones de Strava" borra **la fila entera** |

- La columna `strava_activity_id` se agregó en la versión 6 del esquema (`database_service.dart:57-61`). Una sesión cuenta como "sincronizada" si esa columna no es nula.
- Las sesiones importadas se guardan como filas normales de `workout_sessions` + `data_points`: sin `routine_id`, con `routine_name` = nombre de la actividad en Strava.
- En modo simulador todo lo anterior se escribe en la base separada `rower_app_dev.db` (`database_service.dart:11`).

### 2.2 Textos visibles (.arb)

| Clave | es | en | Uso |
|---|---|---|---|
| `navProfile` | Perfil | Profile | pestaña de la barra inferior |
| `profileTitle` | Perfil | Profile | título de la pantalla |
| `profileConnectStrava` | Conectar con Strava | Connect with Strava | botón de conexión |
| `profileConnected` | Conectado | Connected | nombre de respaldo si no hay nombre de atleta |
| `profileConnectedToStrava` | Conectado a Strava | Connected to Strava | subtítulo bajo el nombre |
| `profileDisconnect` | Desconectar | Disconnect | botón y confirmación |
| `profileDisconnectTitle` | Desconectar Strava | Disconnect Strava | título del diálogo |
| `profileDisconnectContent` | Tus sesiones no se subirán automáticamente. ¿Continuar? | Your sessions won't be uploaded automatically. Continue? | cuerpo del diálogo |
| `profileUploadSettings` | Configuración de subida | Upload Settings | título de la tarjeta |
| `profileAutoUpload` / `profileAutoUploadDesc` | Subida automática / Subir sesiones a Strava automáticamente al terminar | Auto-upload / Upload sessions to Strava automatically after finishing | opción 1 |
| `profileAskUpload` / `profileAskUploadDesc` | Preguntar cada vez / Mostrar un diálogo después de cada entrenamiento | Ask me each time / Show a dialog after each workout | opción 2 |
| `profileManualUpload` / `profileManualUploadDesc` | Solo manual / Subir sesiones manualmente desde el Historial | Manual only / Upload sessions manually from History | opción 3 |
| `profileSync` | Sincronización | Sync | título de la tarjeta |
| `profileSyncToDesc` | Subir todas las sesiones locales sin sincronizar a Strava | Upload all unsynced local sessions to Strava | texto sobre el botón de subida |
| `profileSyncToStrava` | Sincronizar a Strava | Sync to Strava | botón de subida en lote |
| `profileUploading` | Subiendo... | Uploading... | botón de subida mientras sube |
| `profileUploadResult(count)` | {count} sesiones subidas | {count} sessions uploaded | aviso al terminar el lote (sin plural: "1 sesiones subidas") |
| `profileSyncDesc` | Descargar tus actividades de remo desde Strava | Download your rowing activities from Strava | texto sobre el botón de descarga |
| `profileSyncFromStrava` | Sincronizar desde Strava | Sync from Strava | botón de descarga |
| `profileSyncing` | Sincronizando... | Syncing... | botón de descarga mientras descarga |
| `profileSyncResult(count)` | {count} sesiones sincronizadas | {count} sessions synced | aviso al terminar la descarga (sin plural) |
| `profilePendingUploads` | Subidas pendientes | Pending Uploads | título de la tarjeta |
| `profileUploadToStrava` | Subir a Strava | Upload to Strava | botón, título de diálogo, tooltip en Detalle |
| `profileUploadingToStrava` | Subiendo a Strava... | Uploading to Strava... | aviso al empezar una subida automática |
| `profileUploaded` | Subido a Strava | Uploaded to Strava | aviso de éxito |
| `profileUploadFailed` | Error al subir | Upload failed | aviso de fallo |
| `profileAskUploadDialog` | ¿Subir esta sesión a Strava? | Upload this session to Strava? | cuerpo del diálogo "preguntar" |
| `cancel` | Cancelar | Cancel | diálogos |
| `historyFree` | Libre | Free | nombre de una sesión sin rutina en "Subidas pendientes" |
| `workoutFinish` | Terminar | Finish | botón del diálogo de rutina completada |
| `workoutCompleted` / `workoutCompletedMessage` | ¡Entrenamiento completado! / Has completado tu rutina exitosamente. | Workout Completed! / You have successfully completed your routine. | diálogo de rutina completada |

### 2.3 Textos visibles que NO están en los .arb (hardcodeados en el legacy)

| Texto literal | Dónde | Idioma |
|---|---|---|
| `Strava` (título de la tarjeta de conexión) | `profile_screen.dart:54` | marca |
| `Mantenimiento` | `profile_screen.dart:422` | solo español |
| `Limpiar sesiones de Strava` (botón y título del diálogo) | `profile_screen.dart:430,460` | solo español |
| `Esto eliminará todas las sesiones descargadas de Strava. Podrás volver a sincronizarlas después.\n\n¿Continuar?` | `profile_screen.dart:432-434` | solo español |
| `Cancelar` / `Eliminar` | `profile_screen.dart:438,442` | solo español |
| `Sesiones de Strava eliminadas. Puedes sincronizar de nuevo.` | `profile_screen.dart:452-453` | solo español |
| tooltip `Upload` | `profile_screen.dart:400` | solo inglés |
| `Session not found` | `profile_provider.dart:98` | solo inglés |
| `Upload failed – check console for details` | `profile_provider.dart:125` | solo inglés |
| `Failed to connect to Strava` | `profile_provider.dart:65` | solo inglés |
| `Strava error: <error>`, `No authorization code received. Callback: <url>`, `OAuth error: <excepción>`, `Token exchange failed: <respuesta>`, `Token exchange error: <excepción>` | `strava_auth_service.dart:70,76,84,107,117` | solo inglés, técnico |
| Nombre de la actividad sin rutina en Strava: `RowMate Session` | `strava_api_service.dart:93` | solo inglés (lo ve el usuario en Strava) |
| Respaldos `'¡Entrenamiento completado!'`, `'Finalizar'`, etc. | `immersive_workout_screen.dart:217,237` | código muerto: la clave .arb siempre existe |

### 2.4 Contrato externo con Strava (restricción de producto)

No es una decisión técnica: es lo que Strava ve y lo que ya está en las cuentas de los usuarios. La versión nueva tiene que **seguir entendiendo las actividades que subió el legacy**, sobre todo los metadatos de F-STR-10.

| Operación | Contrato observado en el legacy |
|---|---|
| Autorizar | `https://www.strava.com/oauth/authorize` · `client_id` · `redirect_uri=rowmate://rowmate.app/callback` · `response_type=code` · `approval_prompt=auto` · `scope=activity:read_all,activity:write` |
| Token (canje y renovación) | `https://www.strava.com/oauth/token` · `grant_type=authorization_code` (con `code`) o `refresh_token` (con `refresh_token`) · siempre `client_id` + `client_secret` |
| Subir | `POST https://www.strava.com/api/v3/uploads` · `data_type=tcx` · `sport_type=Rowing` · `name` · `trainer=1` · `external_id=rowmate_<idLocal>` · archivo `rowmate_<idLocal>.tcx` |
| Estado de la subida | `GET /api/v3/uploads/<uploadId>` → `activity_id`, `status`, `error` |
| Listar actividades | `GET /api/v3/athlete/activities?page=1&per_page=50` (el parámetro `after` existe pero nunca se usa) |
| Telemetría | `GET /api/v3/activities/<id>/streams?keys=time,watts,cadence,heartrate,distance&key_by_type=true` |
| Detalle | `GET /api/v3/activities/<id>` → `description`, `laps[]` (`id`, `start_index`, `end_index`, `elapsed_time`, `distance`) |
| Esquema de retorno | `rowmate` (registrado en Android `AndroidManifest.xml:41-51` y en iOS `Info.plist:58-68`). En Windows el login se abre en una ventana web propia de la app (`windows/flutter/generated_plugins.cmake:6-10`). En macOS no hay esquema registrado: lo resuelve la sesión de autenticación del sistema. |

### 2.5 Identidad visual del legacy

- Naranja Strava `#FC4C02`: botones de conectar y de subida en lote, ícono "sincronizado".
- Ícono "sincronizado" (nube con tilde): 14 px en la tarjeta del Historial, 20 px en la barra del Detalle.
- Ícono de remo `#00B4D8` en las filas de pendientes.
- Avatar circular de radio 22.
- Errores en rojo (`redAccent`), 12 pt. Textos secundarios en blanco al 54 %.

---

## 3. Funcionalidades

### F-STR-01 · Disponibilidad de Strava según credenciales

**Valor:** Strava es opcional. Sin credenciales la app queda completa y sin rastros de una función rota.

**Comportamientos observables**
1. Strava está disponible si el secreto de cliente **no está vacío** y **no es** el texto de ejemplo `TU_CLIENT_SECRET` (`strava_config.dart:19-21`).
2. Sin Strava disponible, la barra inferior tiene **4** destinos: Dispositivo, Entrenar, Rutinas, Historial. Con Strava son **5**: el quinto es `navProfile`, con ícono de persona (contorno sin seleccionar, relleno seleccionado) (`main.dart:126-135, 171-176`).
3. Historial queda siempre en la posición 3. Al tocar esa pestaña se recarga la lista; el Perfil (posición 4) no recarga nada al seleccionarlo (`main.dart:141-148`).
4. Sin Strava disponible, el Historial y el Detalle no muestran el ícono "sincronizado", **aunque la sesión tenga ID de Strava** (`history_screen.dart:96`, `session_detail_screen.dart:89`).
5. Sin Strava disponible, el Detalle no muestra el botón de subir y al terminar un entrenamiento no hay ni aviso ni diálogo de subida (`workout_screen.dart:307, 317-321`; `immersive_workout_screen.dart:143, 199-205`).
6. El ID de cliente es público y compartido por todos los usuarios. El secreto es personal y nunca se versiona: está en `.gitignore:47-48` y la CI de iOS lo genera desde secretos del repositorio (`.github/workflows/build-ios.yml:24-30`; `README.md:224`).

**Casos borde:** el README propone como marcador `'YOUR_SECRET'`. Ese valor **sí** cuenta como "configurado": aparece el Perfil y la conexión falla recién al canjear el token (ver 4.7).

**UI:** solo la presencia o ausencia de la pestaña y de los indicadores.
**Persistencia:** ninguna; la disponibilidad se decide al compilar.
**Fuentes:** `strava_config.dart:5-21`, `main.dart:56, 86-91, 126-135, 171-176`, `strava_secrets.dart.example:1-4`.
**Tests legacy:** ninguno.
**Dependencias:** APP (barra de navegación), HIS (indicadores), WKT (disparadores de fin de sesión).
**Mejora EXR:** Strava se ve como un "servicio conectado" de primer nivel, al estilo de las cuentas vinculadas de una app deportiva premium. Si no está disponible, no aparece ningún hueco, botón muerto ni mensaje técnico.

---

### F-STR-02 · Conectar con Strava (autorización)

**Valor:** el remero vincula su cuenta una sola vez para subir e importar entrenamientos.

**Comportamientos observables**
1. Tocar `profileConnectStrava` borra el error anterior y abre la página de autorización de Strava en el navegador del sistema (en Windows, en una ventana web de la app). La sesión del navegador **no** es efímera: si el usuario ya entró a Strava en ese navegador, no le vuelve a pedir usuario y contraseña (`strava_auth_service.dart:38-61`).
2. Se piden los permisos `activity:read_all,activity:write`, con `approval_prompt=auto`: si el usuario ya había autorizado la app, Strava no le vuelve a mostrar la pantalla de permisos (`strava_config.dart:12`, `strava_auth_service.dart:43-50`).
3. La app espera volver por `rowmate://rowmate.app/callback` (`strava_config.dart:9-10`).
4. Si la vuelta trae el parámetro `error` (p. ej. el usuario tocó "Cancelar" en Strava), falla con `Strava error: <error>` (`strava_auth_service.dart:69-73`).
5. Si la vuelta no trae `code`, falla con `No authorization code received. Callback: <url completa>` (`:75-79`).
6. Si el usuario cierra el navegador o la ventana, o se produce cualquier otra excepción, falla con `OAuth error: <excepción>` (`:83-88`).
7. Con `code`, la app lo canjea por tokens. Si la respuesta no es 200, falla con `Token exchange failed: <cuerpo de la respuesta>`. Ante una excepción (p. ej. sin red), falla con `Token exchange error: <excepción>` (`:92-122`).
8. Si sale bien, se guardan el token de acceso, el de renovación y el vencimiento (segundos epoch). Si la respuesta trae el atleta, también se guardan el **nombre** `"<firstname> <lastname>"` (un dato faltante cuenta como vacío y se recortan los espacios) y la **URL del avatar** (`profile`, o `""` si falta) (`:161-172`).
9. Al conectar bien, el Perfil pasa al estado conectado, carga nombre y avatar, y muestra las demás tarjetas (`profile_provider.dart:53-70`).
10. Si falla, el error que se muestra es el del servicio o, si no hay, `Failed to connect to Strava`. Aparece en rojo, 12 pt, debajo de la tarjeta Strava (`profile_provider.dart:65`, `profile_screen.dart:106-110`).
11. Mientras dura la autorización **no hay estado "conectando"**: el botón queda habilitado y sin indicador, así que se pueden abrir varios flujos a la vez (`profile_screen.dart:92-104`).
12. La app no verifica qué permisos aceptó el usuario. Si destilda el de escritura, la conexión "sale bien" y después cada subida falla con `profileUploadFailed`.

**Casos borde y errores:** todos los mensajes están en inglés y son técnicos (2.3). Un nombre vacío (atleta sin nombre ni apellido) se muestra vacío: el respaldo `profileConnected` solo aparece si no hay nombre guardado.

**UI:** tarjeta "Strava" desconectada: botón ancho completo naranja `#FC4C02`, texto blanco, ícono de enlace y 14 px de relleno vertical. Si hay error, va en rojo debajo.
**Persistencia:** las 5 claves del almacenamiento seguro (2.1).
**Fuentes:** `strava_auth_service.dart:9-122, 161-172`; `strava_config.dart:8-16`; `profile_provider.dart:53-70`; `profile_screen.dart:91-110`; `AndroidManifest.xml:41-51`; `ios/Runner/Info.plist:58-68`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-01, F-STR-03, F-STR-16.
**Mejora EXR:** el vínculo se siente como enlazar una cuenta en una app de primer nivel. El botón respeta la identidad de Strava. Mientras el navegador está abierto, la tarjeta muestra que se espera la autorización. Al volver, aparece el avatar del atleta con una transición suave y una confirmación breve ("Conectado como <nombre>"). Los errores se explican en el idioma del usuario y con una acción para reintentar.

---

### F-STR-03 · Sesión persistente y renovación de credenciales

**Valor:** el usuario se conecta una vez y la app sigue conectada aunque se reinicie o pasen días.

**Comportamientos observables**
1. "Conectado" significa que **existe un token de acceso guardado**. Al iniciar no se consulta a Strava si el token sigue valiendo (`strava_auth_service.dart:23-26`).
2. Al abrir la app, el Perfil lee del almacenamiento el estado de conexión, el nombre, el avatar y la preferencia de subida. **Hasta que termina esa lectura se ve desconectado**: un parpadeo breve del botón "Conectar" (`profile_provider.dart:16, 38-51`).
3. Antes de cada llamada a Strava, si hay vencimiento guardado y `ahora (segundos epoch) ≥ vencimiento − 60`, se renuevan las credenciales (`strava_auth_service.dart:125-136`). Ejemplo: con vencimiento 1000, en 939 no se renueva y en 940 sí.
4. La renovación manda el token de renovación. Si la respuesta es 200, guarda tokens nuevos (y el atleta, si viene). Si no es 200 o falta el token de renovación, falla (`:139-158`).
5. Si no hay vencimiento guardado, se usa el token tal cual.
6. Si la renovación falla, **la app no se desconecta**: el Perfil sigue diciendo "Conectado". Las subidas terminan en `profileUploadFailed` y la importación devuelve "0 sesiones sincronizadas" sin explicar por qué.
7. Si la renovación tira una excepción de red, se corta la operación en curso. En una subida individual se muestra el texto técnico en rojo; en el lote, esa sesión se saltea en silencio; en la importación, se muestra el texto en rojo (F-STR-07, F-STR-11, F-STR-12).

**Casos borde:** si el usuario revoca el acceso desde Strava, la app sigue "conectada" para siempre hasta que se desconecte a mano.
**UI:** ninguna propia.
**Persistencia:** `strava_access_token`, `strava_refresh_token`, `strava_expires_at`, `strava_athlete_name`, `strava_athlete_avatar`.
**Fuentes:** `strava_auth_service.dart:11-36, 125-172`; `profile_provider.dart:38-51`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-02; la usan F-STR-07, F-STR-08, F-STR-11 y F-STR-12.
**Mejora EXR:** la conexión es invisible mientras funciona. Si Strava deja de aceptar las credenciales, la app lo dice claro ("Tu conexión con Strava venció. Volvé a conectar") en lugar de fallar en silencio. El estado de conexión se muestra bien desde el primer cuadro, sin parpadeos.

---

### F-STR-04 · Desconectar Strava

**Valor:** el usuario controla su cuenta y puede desvincularla.

**Comportamientos observables**
1. Conectado, la tarjeta Strava muestra a la derecha el botón de texto `profileDisconnect` en rojo (`profile_screen.dart:84-88`).
2. Al tocarlo aparece un diálogo con título `profileDisconnectTitle`, texto `profileDisconnectContent` y las acciones `cancel` (texto) y `profileDisconnect` (botón relleno) (`:117-134`).
3. Al confirmar se borran las 5 claves del almacenamiento seguro, el estado pasa a desconectado, se olvidan el nombre y el avatar, y la pantalla queda solo con la tarjeta de conexión (`strava_auth_service.dart:175-181`; `profile_provider.dart:72-78`).
4. Cancelar o tocar fuera del diálogo no cambia nada.
5. **No** se revoca la autorización en Strava: la app sigue figurando como autorizada en la cuenta del usuario.
6. Las sesiones locales **conservan** su ID de Strava: los íconos "sincronizado" siguen en el Historial. La preferencia de subida también se conserva.
7. Desconectar no borra un error que se estuviera mostrando.

**UI:** diálogo de confirmación estándar.
**Persistencia:** borra las claves de 2.1; no toca `strava_upload_pref` ni `strava_activity_id`.
**Fuentes:** citadas arriba.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-02, F-STR-16, F-STR-17.
**Mejora EXR:** la desconexión es deliberada y tranquila: el diálogo explica qué pasa (las sesiones quedan en el historial, se dejan de subir solas) y la tarjeta vuelve al estado inicial con una transición suave.

---

### F-STR-05 · Preferencia de subida

**Valor:** cada remero decide si sus sesiones van solas a Strava, si se le pregunta, o si las sube él.

**Comportamientos observables**
1. Hay tres opciones excluyentes, en este orden: **auto** (`profileAutoUpload` + `profileAutoUploadDesc`), **preguntar** (`profileAskUpload` + `profileAskUploadDesc`) y **manual** (`profileManualUpload` + `profileManualUploadDesc`) (`profile_screen.dart:139-181`).
2. El valor por defecto es **auto**: en la primera instalación y ante cualquier valor guardado desconocido (`profile_provider.dart:19, 45-49`).
3. La elección se guarda al instante en `strava_upload_pref` como `auto`, `ask` o `manual` (`profile_provider.dart:80-85`).
4. La tarjeta solo se ve con Strava conectado (`profile_screen.dart:23-26`).
5. La preferencia sobrevive a desconectar y volver a conectar.
6. Qué hace cada opción al terminar un entrenamiento se describe en F-STR-06. **Manual** no desactiva nada: la subida desde el Detalle, desde Pendientes y en lote sigue disponible.

**UI:** tarjeta `profileUploadSettings` (título 16 pt negrita) con tres opciones de radio compactas (título 14 pt, subtítulo 11 pt al 54 % de blanco).
**Persistencia:** `strava_upload_pref`.
**Fuentes:** `profile_provider.dart:9, 19, 44-51, 80-85`; `profile_screen.dart:137-210`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-06, F-STR-16.
**Mejora EXR:** la elección se presenta como tres tarjetas claras con una frase de "qué pasa al terminar". Al cambiar de opción hay una confirmación sutil, con animación y vibración en móvil.

---

### F-STR-06 · Subida al terminar un entrenamiento

**Valor:** el entrenamiento llega a Strava sin pasos extra, o con una sola pregunta.

**Condiciones comunes:** Strava disponible, **sin** modo simulador, Strava conectado, y existe "la última sesión guardada" (`lastFinishedSessionId`). Si falta cualquiera, no se hace nada.

**A. Terminar a mano** (botón Terminar o "atrás" → confirmar). La pantalla activa es la inmersiva, `immersive_workout_screen.dart:125-192`; la vieja `_FullscreenWorkoutPage` tiene el mismo código (`workout_screen.dart:289-312, 447-489`) y además existe la variante `_CompactTopBar` (`:532-598`), pero ya no se navega a esa página (CLAUDE.md legacy).

1. Primero se guarda la sesión; después se evalúa la preferencia.
2. **auto:** aviso `profileUploadingToStrava`, subida en segundo plano (F-STR-07) y, al terminar, aviso `profileUploaded` o `profileUploadFailed`, **solo si la pantalla de entrenamiento sigue abierta**. Como esa pantalla se cierra sola al volver a reposo, lo más probable es que el usuario **no vea el resultado** (a verificar en dispositivo; ver preguntas abiertas).
3. **preguntar:** diálogo con título `profileUploadToStrava`, texto `profileAskUploadDialog` y acciones `cancel` / `profileUploadToStrava`. Si acepta, sube **sin ningún aviso** de progreso ni de resultado. Riesgo: la vuelta a reposo cierra la ruta de arriba, y es probable que ese diálogo **se cierre solo** antes de que el usuario responda (a verificar).
4. **manual:** nada.

**B. Rutina completada** (termina el último paso). Aparece un diálogo que no se cierra tocando fuera ni con "atrás", con tilde verde `#06D6A0`, `workoutCompleted`, `workoutCompletedMessage` y las estadísticas de tiempo, distancia y calorías (`immersive_workout_screen.dart:196-296`; equivalente en `workout_screen.dart:314-406`).

5. **preguntar** (conectado, sin simulador): dos botones. `workoutFinish` (contorno) cierra y vuelve a reposo sin subir. `profileUploadToStrava` (relleno, ícono de nube 18) cierra, sube, avisa `profileUploaded` o `profileUploadFailed` si la pantalla sigue montada, y vuelve a reposo.
6. **auto:** un botón `workoutFinish` que cierra, avisa `profileUploadingToStrava`, sube, avisa el resultado (con la misma salvedad de la pantalla montada) y vuelve a reposo.
7. **manual**, desconectado, simulador o sin Strava: un botón `workoutFinish` que cierra y vuelve a reposo.

**Comunes**
8. Se sube la sesión indicada por `lastFinishedSessionId`. Ese valor se fija **solo cuando el guardado salió bien** y **no se borra** al volver a reposo (`workout_provider.dart:78-80, 296, 305-318`).

**Casos borde y errores**
- Si falla el guardado de la sesión actual, `lastFinishedSessionId` sigue apuntando a la **sesión anterior**. En modo auto se vuelve a subir esa, Strava responde "duplicado" y el usuario ve `profileUploaded` aunque la sesión de hoy no se subió.
- En "rutina completada", el diálogo aparece en cuanto la fase pasa a "terminado", **antes** de que termine el guardado. Si el usuario toca muy rápido, el ID puede ser nulo (no sube) o el de la sesión anterior.
- Sin red, la sesión queda pendiente y vuelve a aparecer en F-STR-14 y F-STR-11.

**UI:** avisos breves en la parte inferior; diálogos descriptos arriba.
**Persistencia:** la de F-STR-07.
**Fuentes:** citadas en cada punto.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-05, F-STR-07, F-STR-18; WKT (fin de sesión, `lastFinishedSessionId`, diálogo de completado).
**Mejora EXR:** el cierre del entrenamiento es un momento de logro. La tarjeta de resumen post-sesión integra el estado de Strava: progreso visible ("Enviando a Strava…" con un anillo que se completa), confirmación clara ("En Strava ✓"), o "Pendiente: se reintentará" si falla. El resultado **siempre** se ve. En "preguntar", la decisión forma parte del resumen y no aparece como un diálogo que interrumpe.

---

### F-STR-07 · Subir una sesión (operación individual)

**Valor:** es la operación base de todos los caminos individuales: Detalle, Pendientes y fin de entrenamiento.

**Comportamientos observables**
1. Sin conexión con Strava, devuelve "falló" sin mensaje (`profile_provider.dart:89`).
2. Al empezar marca "subiendo" y borra el error anterior. "Subiendo" afecta a otras pantallas: deshabilita los dos botones de la tarjeta Sincronización (el de subida muestra "Subiendo...") y pone un indicador giratorio en el botón del Detalle (`:91-93`).
3. Si la sesión no existe, el error es `Session not found` (`:96-102`).
4. Toma **todos** los puntos de telemetría de la sesión, ordenados por segundo transcurrido. Si la sesión tiene rutina **y esa rutina todavía existe**, toma también sus pasos expandidos (series desplegadas con progresiones) (`:104-113`).
5. Envía según F-STR-08. Si sale bien, guarda el ID de Strava en la sesión y devuelve "OK" (`:117-123`).
6. Si el envío falla, el error es `Upload failed – check console for details` (`:124-129`).
7. Ante cualquier excepción, el error es el texto de la excepción (`:130-136`).
8. **No hay auto-reparación** en este camino (solo en el lote, F-STR-11).
9. El error queda guardado y se ve en rojo bajo la tarjeta Strava del Perfil, aunque la subida se haya pedido desde otra pantalla.

**Casos borde y errores**
- Sesión con tiempo total 0: el cálculo de calorías de la vuelta divide por cero y la subida termina en error técnico (F-STR-09.7).
- Sesión sin puntos: se manda un archivo sin vueltas y Strava lo rechaza → `profileUploadFailed`.
- Si la rutina se editó y quedó con menos pasos que los que se remaron, la subida termina en error técnico (F-STR-09, casos borde).
- Si la rutina se borró, se sube sin metadatos (F-STR-10.6), con una vuelta por paso igual.
- Nada impide subir la misma sesión dos veces a la vez (la fila de Pendientes no se deshabilita). Strava resuelve el duplicado (F-STR-08.8).

**Persistencia:** `workout_sessions.strava_activity_id` (`database_service.dart:231-239`).
**Fuentes:** `profile_provider.dart:87-137`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-03, F-STR-08, F-STR-09, F-STR-10; DB (sesiones y puntos); RUT (pasos expandidos).
**Mejora EXR:** cada subida muestra en su propio lugar (fila, botón, resumen) que está en curso, que terminó bien o que falló, con un motivo entendible ("Sin conexión", "Strava rechazó el archivo") y la opción de reintentar.

---

### F-STR-08 · Envío a Strava, confirmación y duplicados

**Valor:** la actividad aparece en Strava como remo indoor, con nombre y datos correctos, sin duplicarse.

**Comportamientos observables**
1. En modo simulador no se hace ninguna llamada y el resultado es "falló" (`strava_api_service.dart:81-84`).
2. Sin token válido (F-STR-03), "falló" (`:86-90`).
3. El nombre de la actividad es el de la rutina; si es una sesión libre, `RowMate Session` (`:93`).
4. Se envía: tipo de archivo TCX, deporte **Rowing**, marca de **entrenamiento indoor** (`trainer=1`), identificador externo `rowmate_<idLocal>` y archivo `rowmate_<idLocal>.tcx`. **No** se manda descripción aparte: la descripción viaja dentro del archivo (F-STR-09.4) (`:97-112`).
5. Strava tiene que aceptar el envío con 201; cualquier otra respuesta es "falló" (`:120-123`).
6. Después la app consulta el estado: **espera 2 s y consulta, hasta 15 veces** (unos 30 s). Las respuestas distintas de 200 se ignoran y se sigue consultando (`:132-145`).
7. Si la respuesta trae `activity_id`, la subida sale bien con ese ID (`:153`).
8. Si trae un error que contiene `duplicate of activity <N>`, la subida **sale bien vinculándose a la actividad N** que ya existía (`:155-162`).
9. Cualquier otro error es "falló" (`:163-164`).
10. Si pasan las 15 consultas sin resultado, "falló", aunque Strava termine de procesar después (`:167-168`).

**Casos borde**
- Si se agota la espera pero Strava creó la actividad: la sesión local queda pendiente. La próxima importación (F-STR-12) **la trae como una sesión nueva** (duplicado local) y la próxima subida en lote vincula la original como "duplicado". Resultado: dos sesiones locales con el mismo ID de Strava.
- Cuando Strava limita la cantidad de llamadas, la respuesta cuenta como fallo y no se reintenta.

**Fuentes:** `strava_api_service.dart:60-169`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-03, F-STR-09, F-STR-18.
**Mejora EXR:** la espera se ve como un progreso con etapas ("Enviando", "Strava procesando", "Listo") en lugar de un silencio de 30 s. Si Strava tarda más, la app no lo da por perdido: lo marca "en proceso" y lo confirma después.

---

### F-STR-09 · Archivo de actividad TCX

**Valor:** Strava recibe la sesión completa: pulso, cadencia, potencia, distancia, hora local correcta y una vuelta por cada paso de la rutina.

**Comportamientos observables (contenido exacto del archivo)**
1. Es un documento XML UTF-8 de "Training Center Database v2", con la extensión de actividad v2 para potencia. Tiene una sola actividad con deporte `Other` (el deporte real, Rowing, va en el envío, F-STR-08.4) (`tcx_builder.dart:18-23`).
2. Las fechas y horas van en **hora local con desfase**, formato `AAAA-MM-DDTHH:MM:SS±HH:MM` (p. ej. `2024-03-11T19:51:00-03:00`): año con 4 dígitos, el resto con 2, sin milisegundos (`:130-143`).
3. El identificador de la actividad es la hora de inicio de la sesión (`:24`).
4. Si la descripción no está vacía, va como "notas" de la actividad, con `&`, `<`, `>`, `"` y `'` escapados. En la práctica la descripción es **solo** el comentario de metadatos de F-STR-10, porque nunca se pasa texto del usuario (`:26-30, 119-126`).
5. **Vueltas:** si ningún punto tiene paso de rutina, hay una sola vuelta con todos los puntos. Si no, se abre una vuelta nueva cada vez que cambia el índice de paso entre puntos consecutivos (`:83-116`).
6. Cada vuelta lleva: inicio = inicio de la sesión + segundos del primer punto; duración = segundos del último punto − segundos del primero; distancia = distancia del último − distancia del primero (metros enteros); intensidad `Active`; disparador `Manual` (`:36-41, 162-188`).
7. **Calorías por vuelta** = `redondear(calorías totales × puntos de la vuelta ÷ (tiempo total ÷ 5))`. Asume un punto cada 5 s, que es el muestreo real del legacy (`workout_provider.dart:158-160`). Ejemplo: 100 kcal en 600 s y una vuelta de 60 puntos → 100 × 60 ÷ 120 = **50** (`tcx_builder.dart:179`).
8. **Cada punto** lleva: hora = inicio + segundos del punto; distancia acumulada (entera, siempre); pulso **solo si > 0**; cadencia = paladas por minuto redondeadas **solo si > 0**; potencia en watts **solo si > 0** (`:44-69`).
9. No se escriben ritmo, velocidad ni conteo de paladas.

**Casos borde**
- Sin puntos: no hay vueltas (Strava lo rechaza).
- Con tiempo total 0, el cálculo de calorías no tiene resultado válido y la subida termina en error.
- Una vuelta de un solo punto tiene duración y distancia 0.
- Como la duración de la vuelta se mide del primer al último punto, el tramo entre el último punto de una vuelta y el primero de la siguiente (hasta 5 s) no cuenta en ninguna.
- Si se pasan pasos y algún índice de paso de los puntos queda fuera de la lista (rutina editada con menos pasos), el armado falla. Una mezcla de puntos con y sin paso también fallaría; con el registro actual no ocurre, porque o todos los puntos tienen paso o ninguno.
- El paso de rutina que recibe cada vuelta no se usa para nada (no cambia el contenido).

**Fuentes:** `tcx_builder.dart:1-189`.
**Tests legacy:** ninguno. Es lógica pura: conviene cubrirla con casos de verificación (sección 5).
**Dependencias:** F-STR-10; WKT (muestreo de 5 s, índices de paso); HRM (pulso).
**Mejora EXR:** la actividad en Strava se ve tan completa como la de un ergómetro conectado de marca: vueltas con nombre del tipo de paso, potencia y pulso continuos, y la hora exacta en la zona del usuario.

---

### F-STR-10 · Metadatos de rutina ocultos en la descripción

**Valor:** al importar una sesión que subió RowMate, se puede recuperar la estructura de la rutina (tipos de intervalo y objetivos).

**Comportamientos observables (formato)**
1. El metadato va en la descripción como un comentario `<!-- ROWMATE:<carga> -->`. La carga es un JSON codificado en base64 "apto para URL" (`strava_metadata.dart:179`).
2. Se prueban tres niveles, de más a menos detallado, y se usa el primero cuya carga codificada tenga **≤ 200 caracteres**. Si ninguno entra, no hay metadato (`:18-33`).
3. **Nivel 1** (solo con rutina y pasos): `{"v":1,"r":<idRutina>,"n":<nombre>,"i":[{"t":<letra>,"d":<seg>,"m":<metros>,"pw":<wattsMín>,"Pw":<wattsMáx>,"s":<spm>,"p":<splitSeg>}, …]}`, con los campos vacíos como `null`, en ese orden (`:36-57`). En la práctica **solo entra en rutinas de 1 paso**: un paso con nombre "Test" da 128 caracteres y dos pasos ya superan los 200.
4. **Nivel 2:** `{"v":2,"r":<idRutina>,"n":<nombre>,"i":"<letras>"}`. Las letras son la inicial del tipo de cada paso, una cada vez que cambia el índice de paso entre puntos. Si un punto no tiene tipo, la letra es `w` (`:60-82`). Ejemplo: rutina 3 "Test Routine" con cinco pasos → `{"v":2,"r":3,"n":"Test Routine","i":"wwrwc"}`, 60 caracteres codificados.
5. **Nivel 3:** `{"v":3,"c":<cantidad de pasos distintos>}` (`:85-96`).
6. **Sin metadato** (descripción vacía) si la sesión no tiene rutina, si la rutina ya no existe o si no tiene pasos. En esos casos el nivel 1 delega en un nivel 2 sin puntos, que da vacío, y lo vacío "entra" en el límite. Aunque los puntos tengan pasos, no se escribe nada (`:37-39`).
7. La inicial del tipo es la primera letra de su nombre interno: calentamiento `w`, **trabajo `w`**, descanso `r` y enfriamiento `c`. **Trabajo y calentamiento quedan con la misma letra** (el comentario del código dice `k=work`, pero eso nunca se produce) (`:46`).
8. **Lectura:** se busca el primer `<!-- ROWMATE:<carga> -->` cuya carga tenga solo letras, dígitos, `_` y `-`. Se decodifica y se interpreta según `v`. Con `v=1` se reconstruyen los intervalos con tipo y objetivos; con `v=2`, solo los tipos; `v=3`, otra versión o cualquier error dan "sin metadato" (`:99-161`).
9. Al leer, `w` se traduce a **calentamiento**, `k` a trabajo, `r` a descanso, `c` a enfriamiento, y cualquier otra letra a trabajo (`:163-171`). Por el punto 7, **todo paso de trabajo subido por el legacy vuelve como calentamiento**.
10. **La carga codificada termina con `=` de relleno** cuando su largo no es múltiplo de 3 bytes, y la lectura no acepta `=`. Por eso **no se reconocen alrededor de 2 de cada 3 metadatos**: en los ejemplos de arriba, los dos tienen relleno (`=` o `==`).

**Casos borde:** que Strava guarde el contenido de "notas" del archivo como descripción, y que el comentario quede **visible** como texto para el usuario en Strava, no está verificado (preguntas abiertas).
**Fuentes:** `strava_metadata.dart:1-205`; usado en `tcx_builder.dart:27` y `strava_api_service.dart:262`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-09, F-STR-13; RUT (tipos de paso, objetivos, pasos expandidos).
**Mejora EXR:** al importar una sesión propia, el historial la muestra como la rutina original, con colores por tipo de paso y objetivos. Para el usuario esto se ve como "la app recuerda tu entrenamiento" y no como un detalle técnico.

---

### F-STR-11 · Sincronizar a Strava (subida en lote + auto-reparación)

**Valor:** con un toque, todo lo que falta queda en Strava, incluso las sesiones que se guardaron mal.

**Comportamientos observables**
1. Botón `profileSyncToStrava` (relleno naranja, ícono de nube con flecha hacia arriba), debajo del texto `profileSyncToDesc` (`profile_screen.dart:231-266`).
2. Sin conexión, no hace nada y devuelve 0 (`profile_provider.dart:142`).
3. Al empezar marca "subiendo" (no "sincronizando"), borra el error y pone el contador en 0 (`:144-147`).
4. Toma **todas** las sesiones sin ID de Strava, de la más reciente a la más vieja, **incluidas las que no tienen hora de fin** (`:150-151`; orden en `database_service.dart:248-252`).
5. Una sesión **sin puntos** de telemetría se saltea (`:158-162`).
6. Se calculan estadísticas desde los puntos: máximos de distancia, tiempo y calorías; percentil 99 de watts y paladas por minuto (solo valores > 0). Si el tiempo **y** la distancia dan 0, se saltea (`:165-171`; reglas en `workout_session.dart:111-136`).
7. **Auto-reparación:** si a la sesión le falta la hora de fin **o** su tiempo total es 0, se reescribe con: fin = inicio + tiempo calculado; distancia, tiempo y calorías = los calculados; potencia "media" = percentil 99 de watts; paladas "media" = percentil 99 de spm. **La reparación se guarda antes de subir**, aunque la subida después falle (`:174-192`).
8. Las demás sesiones se suben tal como están guardadas (`:193-195`).
9. Las subidas van **una por una**, con la rutina expandida si existe. Cada éxito guarda el ID y suma 1 al contador; los fallos y excepciones de una sesión **se ignoran en silencio** y se pasa a la siguiente (`:200-226`).
10. Al final se apaga "subiendo", se recarga el Historial y aparece el aviso `profileUploadResult(count)` con la cantidad subida (`:228-230`; `profile_screen.dart:240-248`).
11. Un error general (p. ej. al leer la base) se muestra en rojo y el aviso informa lo subido hasta ese momento (`:231-237`).

**Casos borde**
- Las sesiones sin hora de fin **no** aparecen en "Subidas pendientes" (F-STR-14) ni tienen botón en el Detalle, pero **sí** entran en el lote, después de repararse.
- **Alcance real de la reparación:** como el legacy guarda los puntos recién al terminar (`workout_provider.dart:252-280`), una sesión cortada a mitad de camino (app cerrada) **no tiene puntos** y se saltea. La reparación solo ayuda cuando los puntos se guardaron pero falló la actualización de la sesión (ver 4.1).
- En modo simulador las reparaciones se hacen igual, en la base de desarrollo, y el resultado siempre es "0 sesiones subidas".
- Si se toca mientras hay un entrenamiento en curso, su sesión todavía no tiene puntos y se saltea.

**UI:** mientras sube, el botón muestra un indicador giratorio blanco de 16 px y el texto `profileUploading`. Los **dos** botones de la tarjeta quedan deshabilitados.
**Persistencia:** `workout_sessions` (reparación y `strava_activity_id`).
**Fuentes:** `profile_provider.dart:139-238`; `profile_screen.dart:214-266`.
**Tests legacy:** `session_stats_test.dart` cubre de forma indirecta el cálculo que se usa en la reparación. (a) Pulso promedio y máximo ignoran los ceros: [0,120,150,0,141] → promedio 137, máximo 150, "tiene pulso" = sí. (b) Sin pulso → promedio y máximo 0, "tiene pulso" = no. (c) Sin puntos → "tiene pulso" = no.
**Dependencias:** F-STR-07, F-STR-08; DB/HIS (estadísticas de sesión, recarga del Historial); WKT (guardado al terminar).
**Mejora EXR:** el lote se ve como una cola en vivo ("Subiendo 3 de 8", la fila actual resaltada, cada sesión que llega se marca con su tilde). Al terminar hay un resumen claro: subidas, reparadas, falladas con motivo. "Reparada" se comunica como algo positivo ("recuperamos 2 sesiones incompletas").

---

### F-STR-12 · Sincronizar desde Strava (importación)

**Valor:** el historial incluye los remos que el usuario tiene en Strava (de otros dispositivos o anteriores a la app).

**Comportamientos observables**
1. Botón `profileSyncFromStrava` (contorno, ícono de nube con flecha hacia abajo), debajo del texto `profileSyncDesc` (`profile_screen.dart:268-298`).
2. Sin conexión, devuelve 0 (`profile_provider.dart:242`).
3. Al empezar marca "sincronizando", borra el error y pone el contador en 0 (`:244-247`).
4. Arma el conjunto de IDs de Strava ya presentes en el historial local, sean subidas propias o importaciones anteriores (`:251-255`).
5. Pide a Strava **solo la primera página de 50 actividades** más recientes, de cualquier deporte, y se queda con las de deporte **Rowing** o **VirtualRow**. Si Strava responde algo distinto de 200, la lista queda vacía sin aviso de error (`strava_api_service.dart:174-199`).
6. Saltea toda actividad cuyo ID ya esté en el historial (`profile_provider.dart:259-261`).
7. Para cada actividad nueva pide su telemetría (tiempo, watts, cadencia, pulso, distancia) y su detalle (descripción, vueltas y metadatos de F-STR-10) (`:264-265`).
8. Crea la sesión local con nombre = nombre de la actividad, inicio = inicio en Strava, fin = inicio + tiempo transcurrido, distancia redondeada, tiempo = transcurrido, calorías = las de Strava o 0, ID de Strava, **sin rutina** (`:272-281`).
9. Si hay telemetría con tiempos, reconstruye los puntos (F-STR-13), los guarda y **reescribe la sesión** con las estadísticas calculadas: distancia y tiempo máximos, percentil 99 de watts y spm, y **calorías = máximo de los puntos, que siempre es 0**. Las calorías de Strava se pisan con 0 (`:284-390`).
10. Si no hay telemetría, la sesión queda sin puntos (el Detalle muestra `sessionNoTelemetry`).
11. Cada actividad procesada suma 1, tenga o no telemetría (`:392`).
12. Al final se apaga "sincronizando", se recarga el Historial y aparece `profileSyncResult(count)` (`:395-397`; `profile_screen.dart:277-285`).
13. Ante un error, se muestra el texto en rojo, se apaga "sincronizando" y se devuelve lo importado hasta ese momento (`:398-403`).

**Casos borde**
- Usuarios con muchas actividades de otros deportes pueden no ver remos más viejos: solo se revisan las últimas 50 actividades.
- Si la telemetría de cadencia o de distancia trae huecos nulos, la importación se corta con un error técnico **después** de haber creado la sesión de esa actividad, que queda sin puntos. Los huecos en watts o pulso se toman como 0.
- El listado de Strava normalmente no trae calorías, así que quedan en 0 aunque no haya telemetría.
- La hora de inicio llega en UTC y se muestra en hora local.
- En modo simulador la importación funciona y escribe en la base de desarrollo.
- Duplicados locales posibles: ver F-STR-08, casos borde.

**UI:** mientras importa, el botón muestra un indicador giratorio de 16 px y `profileSyncing`. Los dos botones quedan deshabilitados. No hay progreso por actividad.
**Persistencia:** `workout_sessions` y `data_points` nuevos.
**Fuentes:** `profile_provider.dart:240-404`; `strava_api_service.dart:12-58, 171-303`.
**Tests legacy:** indirecto, `session_stats_test.dart` (estadísticas calculadas; casos en F-STR-11).
**Dependencias:** F-STR-03, F-STR-10, F-STR-13; DB, HIS.
**Mejora EXR:** la importación se ve como traer los remos al "diario de entrenamiento": progreso real ("Importando 4 de 12"), cada sesión que entra aparece en el historial con una animación suave, y al final un resumen ("12 sesiones nuevas · 38 km").

---

### F-STR-13 · Reconstrucción de la telemetría importada

**Valor:** las sesiones de Strava se ven en los gráficos con un ritmo creíble y sin picos absurdos, separadas por intervalos.

**Comportamientos observables (por cada muestra de Strava, índice i)**
1. Distancia del punto = distancia de Strava **redondeada a metros enteros**; 0 si falta (`profile_provider.dart:289`).
2. **Ritmo crudo (s/500 m):**
   - en i > 0, si la diferencia de distancia redondeada es ≥ 2 m y la de tiempo es > 0: `(Δtiempo ÷ Δdistancia) × 500`; si no, 0;
   - en i = 0, si distancia > 0 y tiempo > 0: `(tiempo ÷ distancia) × 500`; si no, 0.
   Ejemplo: Δt = 1 s y Δd = 4 m → 125 s (2:05/500 m) (`:286-306`).
3. **Fuera de rango:** un ritmo es válido entre **60 y 600 s/500 m**, ambos incluidos. Uno no válido (incluido el 0) se reemplaza por el último valor ya filtrado. Si todavía no hay ninguno, se busca el próximo crudo válido; si no hay ninguno en toda la serie, se usa **120 s** (2:00/500 m) (`:309-330`).
4. **Suavizado:** media móvil centrada de **15 muestras** (7 a cada lado). En los bordes la ventana se recorta: en i = 0 promedia las primeras 8 (`:333, 414-430`).
5. El ritmo guardado es el suavizado, **redondeado a segundos** (`:368`).
6. **Intervalos:** cada vuelta de Strava *k* asigna el índice de paso *k* a las muestras desde su inicio hasta su fin, **ambos incluidos**. Si dos vueltas comparten la muestra de borde, gana la **siguiente**. Sin vueltas, no hay índice de paso (`:336-344`).
7. **Tipo de paso:** si hay metadatos con intervalos y *k* está dentro de la lista, el tipo es el del intervalo *k*. Si no, no hay tipo (`:354-359`).
8. Cada punto guarda: segundo transcurrido, watts (0 si falta), cadencia como spm (0 si falta), pulso (0 si falta), distancia, ritmo, índice y tipo de paso. **Conteo de paladas y calorías quedan en 0** (`:361-371`).

**Casos borde**
- Sin telemetría de distancia, todos los ritmos son 0, se invalidan y la sesión queda con **2:00/500 m constante**.
- Una pausa larga en Strava (Δd < 2 m) arrastra el último ritmo válido.
- Las vueltas de actividades que no subió RowMate (p. ej. parciales automáticos de otro dispositivo) también generan índices de paso, sin tipo.
- Por F-STR-10.9 y F-STR-10.10, el tipo de paso recuperado suele faltar, o un paso de trabajo aparece como calentamiento.

**Fuentes:** `profile_provider.dart:283-390, 412-430`.
**Tests legacy:** ninguno directo. `session_stats_test.dart` cubre el cálculo de estadísticas que se aplica después.
**Dependencias:** F-STR-10, F-STR-12; HIS (gráficos y desglose por paso del Detalle).
**Mejora EXR:** las curvas importadas se ven igual de limpias que las grabadas en vivo, y los intervalos se reconocen a simple vista por color.

---

### F-STR-14 · Tarjeta "Subidas pendientes"

**Valor:** ver qué falta subir y subir de a una, sin entrar al Historial.

**Comportamientos observables**
1. Lista las sesiones **sin ID de Strava y con hora de fin**, de la más reciente a la más vieja (`profile_screen.dart:327-336`).
2. Mientras carga, o si no hay pendientes, la tarjeta **no se muestra** (sin estado vacío) (`:341-342`).
3. Cada fila: ícono de remo `#00B4D8` de 16 px, nombre (rutina o `historyFree`) en 13 pt, duración en 11 pt gris, y botón de nube con flecha de 20 px con tooltip `Upload` (`:373-406`).
4. La duración se muestra como `MM:SS`, o `H:MM:SS` desde 1 h, a partir del tiempo total guardado (`workout_session.dart:29-37`).
5. Al tocar subir se ejecuta F-STR-07. Si sale bien, la lista se recarga y aparece `profileUploaded`. **Si falla no hay aviso en la fila**: el error aparece en rojo bajo la tarjeta Strava (`:355-364`).
6. **La lista se carga una sola vez**, cuando aparece la tarjeta (al iniciar la app conectada, o al conectar), y solo se recarga tras una subida individual exitosa desde ella. No se actualiza después de "Sincronizar a Strava", de subidas automáticas ni de sesiones nuevas, hasta reiniciar la app o desconectar y volver a conectar.
7. El botón de cada fila no se deshabilita mientras sube.

**Persistencia:** la de F-STR-07.
**Fuentes:** `profile_screen.dart:306-406`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-07, F-STR-16; HIS (formato de duración).
**Mejora EXR:** la lista está siempre al día, cada fila muestra su propio estado (pendiente, subiendo con progreso, subida con tilde que se desvanece y la fila se retira) y, si está vacía, se dice algo positivo ("Todo está en Strava ✓").

---

### F-STR-15 · Mantenimiento: limpiar sesiones de Strava

**Valor:** volver a empezar la sincronización desde cero (p. ej. después de importar mal).

**Comportamientos observables**
1. Tarjeta `Mantenimiento` con el botón de contorno `Limpiar sesiones de Strava` (ícono de barrido, 18) (`profile_screen.dart:410-467`). Textos solo en español (2.3).
2. Al tocar aparece un diálogo con el mismo título, el texto "Esto eliminará todas las sesiones descargadas de Strava. Podrás volver a sincronizarlas después. ¿Continuar?" y las acciones `Cancelar` / `Eliminar`.
3. Al confirmar se borran **todos los puntos y todas las sesiones con ID de Strava** (`database_service.dart:325-337`). Después aparece "Sesiones de Strava eliminadas. Puedes sincronizar de nuevo."
4. **Diferencia crítica con el texto:** se borran también las **sesiones grabadas en la app que ya se subieron a Strava**, no solo las descargadas. Si se vuelven a importar, regresan con los datos de Strava: sin rutina, sin conteo de paladas, sin calorías y con el ritmo reconstruido.
5. El Historial no se recarga en el momento; se actualiza al volver a tocar su pestaña.
6. No hay indicador de progreso.

**Persistencia:** borra filas de `workout_sessions` y `data_points`.
**Fuentes:** `profile_provider.dart:406-410`; `profile_screen.dart:408-467`; `database_service.dart:324-337`.
**Tests legacy:** ninguno.
**Dependencias:** DB, HIS, F-STR-12.
**Mejora EXR:** una acción destructiva se trata como tal: lugar discreto, explicación honesta de qué se borra con la cantidad exacta ("Se eliminarán 23 sesiones, 9 grabadas en este dispositivo") y confirmación deliberada (mantener presionado o escribir), localizada en los dos idiomas. Ver la propuesta de cambio en preguntas abiertas.

---

### F-STR-16 · Pantalla Perfil: estructura y estados

**Valor:** un solo lugar para todo lo relacionado con la cuenta de Strava.

**Comportamientos observables**
1. Barra superior con `profileTitle`. El contenido es una lista desplazable con 16 px de margen (`profile_screen.dart:17-21`).
2. **Desconectado:** solo la tarjeta Strava, con el botón de conectar (F-STR-02).
3. **Conectado:** tarjeta Strava (avatar, nombre, `profileConnectedToStrava`, Desconectar), y debajo, separadas por 16 px y en este orden: Configuración de subida (F-STR-05), Sincronización (F-STR-11 y F-STR-12), Subidas pendientes (F-STR-14, solo si hay) y Mantenimiento (F-STR-15) (`:22-32`).
4. **Avatar:** foto circular (radio 22) desde la URL guardada si no está vacía; si no, un círculo con ícono de persona (`:60-69`).
5. **Nombre:** el guardado o, si no hay, `profileConnected`, en 15 pt semi-negrita (`:75-77`).
6. **Error:** el último error de cualquier operación de Strava aparece debajo del contenido de la tarjeta Strava, en rojo 12 pt, conectado o desconectado (`:106-110`). Se borra al empezar otra operación.
7. **Ocupado:** "subiendo" y "sincronizando" deshabilitan los dos botones de la tarjeta Sincronización, y cada botón muestra su propio indicador (`:221`).
8. La pantalla vive en la barra inferior y **conserva su estado** al cambiar de pestaña. Se construye al iniciar la app junto con las demás pestañas.

**Fuentes:** `profile_screen.dart:1-467`; `profile_provider.dart:11-51`; `main.dart:126-135`.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-01 a F-STR-15.
**Mejora EXR:** el Perfil se siente como la "ficha del atleta" de una app deportiva premium: cabecera con el avatar grande, el nombre y el estado de la conexión, totales del historial, y tarjetas con el mismo lenguaje visual del resto de la app (vidrio, profundidad, transiciones suaves). Cada estado (cargando, vacío, error, ocupado) tiene su representación propia, sin parpadeos.

---

### F-STR-17 · Estado de sincronización en Historial y Detalle

**Valor:** saber de un vistazo qué ya está en Strava y subir lo que falta desde el Detalle.

**Comportamientos observables**
1. **Historial:** con Strava disponible y una sesión con ID de Strava, la tarjeta muestra una nube con tilde naranja `#FC4C02` de 14 px, a la izquierda de la fecha. No hay ícono para "no sincronizada" (`history_screen.dart:96-100`).
2. **Detalle:** el mismo ícono, de 20 px, en la barra superior (`session_detail_screen.dart:89-93`).
3. **Botón de subir en el Detalle:** solo aparece si Strava está disponible, está conectado, la sesión **no** tiene ID de Strava (nulo o vacío) y **tiene hora de fin** (`:78-83`).
4. Ícono de nube con flecha y tooltip `profileUploadToStrava`. Mientras sube cualquier sesión (estado global), muestra un indicador giratorio de 18 px y queda deshabilitado (`:94-105`).
5. Si sale bien, se relee la sesión (el botón desaparece y aparece la nube con tilde) y se avisa `profileUploaded`. Si falla, `profileUploadFailed` (`:106-118`).
6. Al volver del Detalle al Historial, la lista se recarga (`history_screen.dart:53-56`).
7. El botón **no** se oculta en modo simulador: la subida falla siempre (F-STR-18).

**Casos borde:** una sesión con ID vacío (`""`) mostraría la nube con tilde y además el botón de subir. Con el código actual no se generan IDs vacíos.
**Fuentes:** citadas.
**Tests legacy:** ninguno.
**Dependencias:** F-STR-01, F-STR-07; HIS.
**Mejora EXR:** un sello "Strava" sutil y coherente en cada sesión sincronizada. Para las no sincronizadas, un indicador discreto de "pendiente" (agregado, no reemplazo). En el Detalle, la subida muestra progreso dentro del botón y una transición del ícono de "subir" al sello de "en Strava".

---

### F-STR-18 · Strava en modo simulador

**Valor:** probar la app sin ensuciar la cuenta real de Strava.

**Comportamientos observables**
1. En modo simulador **nunca se sube nada**: el envío devuelve "falló" antes de hacer cualquier llamada. Todos los caminos de subida pasan por ahí (`strava_api_service.dart:81-84`).
2. Al terminar un entrenamiento simulado no hay aviso ni diálogo de subida, y el diálogo de rutina completada tiene un solo botón (`immersive_workout_screen.dart:151, 199-201`; `workout_screen.dart:317, 448, 559`).
3. El Perfil sigue disponible: se puede **conectar e importar**, y lo importado va a la base de desarrollo (CLAUDE.md legacy:99; spec del simulador:71-74).
4. Los botones de subida del Detalle, de Pendientes y de "Sincronizar a Strava" **siguen visibles** y siempre terminan en `profileUploadFailed` o "0 sesiones subidas". La auto-reparación del lote sí escribe en la base de desarrollo.
5. Regla del legacy: **toda nueva escritura o pregunta de subida a Strava tiene que respetar el modo simulador** (CLAUDE.md legacy:99).

**Fuentes:** citadas; `database_service.dart:11`.
**Tests legacy:** ninguno.
**Dependencias:** SIM (modo simulador), F-STR-06, F-STR-08.
**Mejora EXR:** en modo simulador la app lo deja claro también en el Perfil ("Modo prueba: no se sube nada a Strava"), en lugar de ofrecer botones que siempre fallan.

---

## 4. Diferencias entre docs y código (manda el código)

1. **Auto-reparación** (README:234 dice "sessions that didn't save properly (e.g. app closed mid-workout) are automatically repaired"). En el código, los puntos se guardan recién al terminar, así que una sesión cortada a mitad de camino no tiene puntos y **se saltea**. La reparación solo cubre sesiones con puntos a las que les falta la hora de fin o el tiempo total.
2. **Subida manual** (README:231 dice "from the session detail screen"; `profileManualUploadDesc` dice "desde el Historial"). En el código también se puede desde Perfil, en "Subidas pendientes".
3. **"Limpiar sesiones de Strava"** (diálogo y comentario: "sesiones descargadas de Strava"). En el código borra **toda sesión con ID de Strava**, incluidas las grabadas en la app y subidas (`database_service.dart:325-337`).
4. **Letra del tipo "trabajo"** (comentario `strava_metadata.dart:46`: `k=work`). En el código, trabajo produce `w`, igual que calentamiento, y al leer `w` vuelve como calentamiento.
5. **Límite de metadatos** (comentario `strava_metadata.dart:21`: "~255 caracteres con el wrapper"). En el código, la carga es ≤ 200 y el comentario completo ≤ 217.
6. **Lectura de metadatos:** ningún doc lo menciona, pero la lectura no acepta el relleno `=` que produce la codificación, así que falla en alrededor de 2/3 de los casos.
7. **Marcador del secreto** (README:221 usa `'YOUR_SECRET'`). En el código solo `'TU_CLIENT_SECRET'` (y vacío) cuenta como "no configurado".
8. **Detección de duplicados** (README:235: "re-uploading an already synced session links to the existing Strava activity"). En el código, una sesión sincronizada no se puede volver a subir desde la UI. El vínculo con la actividad existente solo pasa cuando Strava responde con el texto `duplicate of activity N` (p. ej. tras una espera agotada o con un `lastFinishedSessionId` viejo).
9. **Estado de sincronización** (README:233: "each session in history shows a Strava icon indicating sync status"). En el código solo se muestra un ícono para las **sincronizadas**, y solo si Strava está disponible.
10. **Spec del simulador** (`2026-10-01-dev-simulator-design.md:73`: "los dos `_triggerStravaUpload` de `workout_screen.dart`"). En el código hay un tercero en `immersive_workout_screen.dart`, que es la pantalla en uso, más los diálogos de completado. El CLAUDE.md legacy:99 sí está al día. El README:190 dice "Nothing is ever uploaded" pero no aclara que los botones de subida siguen visibles.
11. **"Promedios"** de potencia y paladas: la reparación y la importación guardan el **percentil 99** en los campos "promedio", igual que el guardado normal (`workout_provider.dart:291-292`).
12. **README:132** describe `profile/` como "Strava connection + upload settings". También tiene sincronización en ambos sentidos, pendientes y mantenimiento.
13. **CLAUDE.md legacy** describe `history/` como "Read-only list". El Detalle tiene el botón de subir a Strava.

## 5. Casos de verificación sugeridos (no existen en el legacy)

Lógica pura que QA debería convertir en criterios de aceptación, con los números del legacy:

- **Vencimiento:** con vencimiento V, renovar si ahora ≥ V − 60 (939 no, 940 sí, con V = 1000).
- **Preferencia:** si falta o vale `"xyz"`, es auto. Los valores guardados son exactamente `auto`, `ask` y `manual`.
- **TCX:** formato de hora con desfase negativo y positivo (`-03:00`, `+05:30`); una vuelta si no hay pasos; vueltas por cambio de paso; duración y distancia de cada vuelta = último − primero; calorías 100 kcal / 600 s / 60 puntos = 50; pulso, cadencia y watts omitidos cuando son 0; cadencia redondeada; escape de `& < > " '`; sin notas si la descripción está vacía.
- **Metadatos:** nivel 3 con 5 pasos → `{"v":3,"c":5}`; sin rutina → vacío; rutina borrada → vacío; nivel 1 con 1 paso entra y con 2 no; la lectura de `v=3` da "sin metadato"; letra desconocida → trabajo. Comportamiento actual de `w`, y del relleno `=` según se decida en las preguntas abiertas.
- **Duplicado:** error "… duplicate of activity 123456" → éxito con 123456.
- **Ritmo importado:** Δt 1 / Δd 4 → 125; Δd 1 → 0, que luego se arrastra; serie sin distancia → todo 120; primera muestra inválida → toma el próximo válido; suavizado con ventana 15 recortada en bordes (i = 0 promedia 8 valores); vueltas que comparten el índice de borde → gana la siguiente.
- **Reparación:** sin puntos → se saltea; tiempo y distancia 0 → se saltea; sin hora de fin → fin = inicio + tiempo máximo de los puntos; percentil 99 = valor ordenado en la posición ⌊(n−1)·0,99⌋ de los valores > 0.
- **Importación:** filtra Rowing y VirtualRow; saltea IDs ya presentes; las calorías quedan en 0 cuando hay telemetría.

## 6. Preguntas abiertas

1. **Borrado de sesiones propias** en "Limpiar sesiones de Strava" (F-STR-15.4): ¿se mantiene la paridad o es un cambio de producto? **Propuesta del PO:** borrar solo las importadas y no las grabadas en la app. Motivo: el legacy pierde telemetría que Strava no tiene. Requiere aprobación del usuario.
2. **Letra `w` duplicada** (F-STR-10.7 y 10.9): para seguir leyendo bien lo que el legacy ya subió, la lectura de `w` tiene que mantenerse. ¿La escritura nueva usa `k` para trabajo, como quería el comentario? Sería cambio de producto, con compatibilidad hacia atrás.
3. **Relleno `=` en la lectura** (F-STR-10.10): ¿se corrige para aceptar el relleno? Mejoraría la recuperación de rutinas, incluidas actividades ya subidas por el legacy.
4. **¿Strava guarda las "notas" del TCX como descripción? ¿El comentario `<!-- ROWMATE:… -->` se ve como texto en la actividad?** Hay que verificarlo con una cuenta real. Si se ve, es un problema de imagen.
5. **Resultado invisible tras subir al terminar** (F-STR-06.2) y **diálogo "preguntar" que se cierra solo** (F-STR-06.3): ¿es lo que pasa en dispositivo? Propuesta: el resultado se ve siempre y la pregunta espera respuesta. Corrige un defecto; requiere confirmación.
6. **Calorías importadas en 0** (F-STR-12.9): ¿se conservan las de Strava cuando vienen?
7. **Importación limitada a 50 actividades** sin paginar (F-STR-12.5): ¿se mantiene?
8. **Textos fuera de los .arb** (2.3), incluidos mensajes técnicos en inglés y "RowMate Session": ¿se pasan a es/en? Propuesta: sí, con mensajes entendibles.
9. **Plural** de `profileUploadResult` / `profileSyncResult` ("1 sesiones"): ¿se corrige?
10. **Lista de pendientes desactualizada** (F-STR-14.6): ¿se acepta que se actualice siempre (mejora sin pérdida de comportamiento)?
11. **Simulador:** ¿se ocultan o se marcan en modo prueba los botones de subida que siempre fallan (F-STR-18.4)?
12. **Migración del historial legacy a la app nueva:** si no se migra, los IDs locales nuevos pueden repetir `rowmate_<id>` en sesiones distintas. ¿Strava usa el identificador externo para detectar duplicados? Hay que verificarlo; condiciona F-STR-08.
13. **Credencial de cliente:** el secreto viaja dentro de la app y se inyecta al compilar. Restricción de producto: Strava solo aparece si la build tiene credenciales válidas y el secreto nunca se versiona. Cómo se resuelve queda para el Unity Architect.
14. **Windows:** ¿alguien validó el login en Windows con el esquema `rowmate`? ¿Y en macOS?
15. **Credenciales revocadas o permisos parciales** (F-STR-02.12, F-STR-03.6): ¿se agrega un aviso para "volver a conectar"? Sería una mejora.
16. **Doble sesión local tras una espera agotada** (F-STR-08, casos borde): ¿se acepta como está o la importación debería reconocer las actividades que subió RowMate (p. ej. por el identificador externo o la hora de inicio)?
