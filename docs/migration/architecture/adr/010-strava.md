# ADR 010: Strava: OAuth por plataforma, credenciales inyectadas y subidas bloqueadas en simulador

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Core.Strava, Infrastructure.Strava, Platform, Bootstrap (It. 12-13)

## Contexto

El legacy usa `flutter_web_auth_2` con `rowmate://rowmate.app/callback`; macOS no registra el esquema y Windows no tiene regreso definido (Q-80: el login tiene que andar en las 4 plataformas). Las credenciales viven en un archivo que no se versiona (R-02) y Strava existe solo si son reales (Q-84). En simulador no se sube nada (R-01).

## Decisión

- **OAuth**: iOS y macOS con `ASWebAuthenticationSession` (Swift), Android con navegador + deep link `rowmate://` (`Application.deepLinkActivated` / `absoluteURL`), Windows y Editor con *loopback* `http://127.0.0.1:<puerto>/callback` sobre `TcpListener` (Unity no soporta deep links en Windows Standalone; Strava habilita `localhost` y `127.0.0.1`). Parámetro `state` validado.
- **Credenciales**: `StravaCredentials` `static partial`; `StravaSecrets.g.cs` se genera al compilar desde variables de entorno y está en `.gitignore`. `StravaConfig.IsConfigured` trata vacío y marcadores de ejemplo como no configurado.
- **Tokens**: `ISecureStore` con las 5 claves del legacy; renovación en `expires_at − 60 s`.
- **HTTP**: `IHttpClient` en Core, `UnityWebRequest` en Platform.
- **Simulador**: `StravaApiClient.UploadAsync` corta antes de cualquier request; la UI oculta todo botón de subida (Q-78).
- **Duplicados**: `external_id = rowmate-u_{installId}_{sessionId}` para no chocar con `rowmate_<id>` del legacy (R-11); se conserva la lectura de "duplicate of activity N".

## Alternativas descartadas

- **WebView embebido**: Google y Strava desaconsejan login en WebViews; más superficie nativa.
- **Esquema de URL registrado en Windows**: requiere escribir en el registro al instalar y Unity no entrega el enlace a la app en ejecución.

## Consecuencias

- `redirect_uri` distinto por plataforma (custom scheme en móvil y macOS, loopback en Windows); los dos son válidos para Strava.
- El secreto queda embebido en el binario, como en el legacy (riesgo aceptado; proxy de tokens en backlog).
