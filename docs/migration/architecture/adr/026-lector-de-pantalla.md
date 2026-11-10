# ADR 026: Lector de pantalla con el árbol de accesibilidad de Unity 6.3

- **Estado:** Aceptado (2026-11-09), al implementar WP-B de la It. 14 (DT14-17 a DT14-22 de [02-tech-design](../../iterations/14-pulido-plataformas/02-tech-design.md))
- **Ámbito:** Core (`Accessibility/*`: rol, estado, ítem, orden, textos, política de anuncios, reconciliación y volcado), Presentation (`Accessibility/A11yRules`, `ScreenReaderBridge`, `IA11yAnnouncer`, `UnityA11yAnnouncer`), `Packages/manifest.json` (`com.unity.modules.accessibility`)
- **Relacionados:** [ADR 002](002-capas-y-asmdefs.md) (capas)

## Contexto

Unity 6.3 trae `UnityEngine.Accessibility` (`AssistiveSupport`, `AccessibilityHierarchy`, `AccessibilityNode`, `notificationDispatcher`), y el `UnityPlayer.dll` de Windows lo expone por UI Automation (Narrador). UI Toolkit **no** tiene puente propio: el árbol lo arma la app. El HUD está cerca de su tope de 60 elementos y en 0 B por cuadro con el lector apagado.

## Decisión

1. **Reglas por clase USS** (`A11yRules`, un solo archivo): cada elemento visible con una clase de la tabla (`.rm-button`, `.rm-tab`, `.rm-appbar__title`, `.rm-metric-tile`, `.rm-hud-card`, tarjetas, campos, interruptores, avisos, títulos de diálogo y el gráfico) da un ítem con rol, nombre, valor y estado. No se agrega ningún `VisualElement` ni se tocan los controles.
2. **El puente solo trabaja con el lector encendido** (`AssistiveSupport.isScreenReaderEnabled`): apagado, `Tick` lee un bool. Encendido, reconcilia por clave estable (el `VisualElement`) como mucho cada 250 ms con `A11yReconciler` (`InsertNode`/`MoveNode`/`RemoveNode`), así un elemento que sigue conserva su `AccessibilityNode` y el foco; `Clear()` solo al cambiar de raíz (shell ↔ diálogo). Los marcos salen de `frameGetter` y `RefreshNodeFrames`. La etiqueta de las métricas se actualiza una vez por segundo.
3. **`invoked` = el clic**: un `NavigationSubmitEvent` al botón del elemento.
4. **Anuncios por `IA11yAnnouncer`** con `A11yAnnouncePolicy`: título nuevo, cada aviso una vez (`ToastLayer.Shown`), cambio de paso y completado. Las métricas nunca se anuncian solas.
5. **Pruebas con el lector forzado** (`screenReaderStatusOverride = ForceEnabled`, restaurado al terminar) y el modelo propio (`A11yTreeDump`), que es el mismo que alimenta la jerarquía de Unity; CAP-27 deja el volcado como texto.

## Consecuencias

- Un control nuevo se lee si lleva una clase con regla; lo decorativo y la escena 3D quedan afuera solos.
- Si Narrador no lee algún rol en la demo, queda NO VERIFICABLE con su issue (mapeo propio a UIA). TalkBack y VoiceOver quedan para cuando haya teléfono y Mac.
- El gráfico se lee como un resumen (`a11yChartSummary`); recorrerlo punto a punto queda en el backlog (DV-136).
