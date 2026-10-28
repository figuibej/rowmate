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

## Cambios de la It. 3 (WP-B)

Detalle en [03-rutinas/02-tech-design](../../iterations/03-rutinas/02-tech-design.md) §4.1-§4.9.

- **Escape no cierra páginas (P-08, DT-04).** Corrige la decisión de arriba ("Botón atrás / Escape: cierra diálogo, después página"): `BackNavigation.HandleBack(BackSource)` cierra, en este orden y **un solo nivel por pulsación**, el menú o la ayuda de `#popup-layer`, después el diálogo de arriba; con `BackSource.Escape` (teclado de la PC) termina ahí. Solo `BackSource.SystemBack` ("Atrás" de Android) sigue con `IPage.OnBackRequested()` de la página de arriba (el editor cierra **sin preguntar**, AC-RUT-11-08) y, en la raíz, con `IBackButtonPolicy.OnBackAtRoot()`. `AppRoot.Update` es el **único** camino de Escape: ningún control registra `NavigationCancelEvent` para cerrar algo (S-B3a, punto 8; `NAV.EscapeClosesOneLevelAtATime`).
- **Capas**: `#tab-content`, `#tab-bar`, `#page-stack`, `#dialog-layer`, `#toast-layer`, `#popup-layer` (menús ⋮, menú de texto, ayudas) y, último hijo, `#simulator-layer`. El menú queda encima de todo salvo el simulador (AC-APP-03-04).
- **Lo que sale no responde** (`ExitLock`, DT-21): una página, un diálogo o un menú que están saliendo dejan de recibir toques y foco desde el cuadro 0 de su salida; el velo y el *catcher* tragan la entrada mientras tanto. Un segundo toque o un Enter durante la salida no repiten la acción (QA-G3-01).
- **Foco**: `FocusTrap` en diálogos y menús (Tab y Mayús+Tab con vuelta). En una página, la barra de pestañas queda con `display: none` mientras la tapa (fuera del recorrido de Tab).
- `DialogService.ShowAsync<T>` no se implementó: cada diálogo es un `ModalDialog` con `Closed` y `WasConfirmed` (el borrado no espera dentro del diálogo, como `routines_screen.dart:115`).
