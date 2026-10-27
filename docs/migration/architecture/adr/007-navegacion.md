# ADR 007: Shell de pestañas que preservan estado, pila de páginas y capas

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Presentation (F-APP-02, F-APP-03)

## Contexto

El legacy usa `IndexedStack` para 4 pestañas (5 con Strava) que conservan su estado, `Navigator.push` para pantallas apiladas que tapan la barra, diálogos, `SnackBar` y la capa del simulador por encima de todo. Tocar Historial recarga.

## Decisión

- `AppShellController` crea las pestañas una vez (Perfil solo si `StravaConfig.IsConfigured`) y alterna `display: none` / `flex`: los elementos y su estado se conservan.
- `PageStack` (`Push`/`Pop`, `IPage` con `OnBackRequested`) para editor de rutina, detalle de sesión y entrenamiento inmersivo.
- Capas en un solo `UIDocument`: contenido de pestañas, barra, pila, diálogos, avisos, simulador.
- `DialogService.ShowAsync<T>()` devuelve `Task<T>`.
- Botón atrás (Android) / Escape: cierra diálogo, después página; en la raíz, Android manda la app al fondo.
- Historial se recarga al tocar su pestaña, sin parpadeo (Q-22).

## Alternativas descartadas

- **Una escena de Unity por pantalla**: pierde el estado al cambiar y carga más lento.
- **Destruir y recrear pantallas**: rompe la paridad con `IndexedStack`.

## Consecuencias

- Las pantallas ocultas siguen suscriptas a sus stores (costo bajo, sin dibujo).
- Un solo panel de UI: el orden de capas es explícito y testeable en PlayMode.

## Cambios de la It. 2 (WP-B)

- `AppShellView` + `TabHost` (= `IndexedStack`): las pestañas se construyen una vez y alternan `display`; capas `#tab-content`, `#tab-bar`, `#page-stack`, `#dialog-layer`, `#toast-layer` y, último hijo, `#simulator-layer` (solo con el define).
- "Atrás" (D-09, DV-06): `AppRoot.Update` lee `Escape` del Input System y llama a `ShellHandle.HandleBack()`; `BackNavigation` (It. 2: siempre en la raíz) llama a `IBackButtonPolicy.OnBackAtRoot()`. En Android API ≥ 31 `moveTaskToBack(true)`, en < 31 `Application.Quit()`: el legacy **termina** la app con "Atrás" en Android 8 a 15 (solo en 16, con atrás predictivo, queda en segundo plano). Escritorio y Editor: nada.
