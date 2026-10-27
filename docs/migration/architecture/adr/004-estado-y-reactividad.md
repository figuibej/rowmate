# ADR 004: Estado y reactividad con observables propios

- **Estado:** Aceptado (2026-10-27)
- **Ámbito:** Core, App, Presentation

## Contexto

El legacy usa `ChangeNotifier` (provider) y `Stream` broadcast. En Unity hace falta lo mismo, testeable en `dotnet`, sin allocations por frame y compatible con IL2CPP.

## Decisión

- Core define `IReadOnlyObservable<T>` / `ObservableValue<T>` (valor actual + notificación al cambiar, con `ForceNotify` para reemitir como `_reemitStatus()`) y `EventStream<T>` (broadcast sin buffer, implementa `System.IObservable<T>`). Unas 150 líneas, con suscriptores copy-on-write.
- Los stores de App exponen **un estado inmutable por pantalla** (`sealed record`) con valores listos para mostrar.
- Las vistas se suscriben al adjuntarse al panel y se desuscriben al quitarse, y actualizan solo los elementos que cambian (binding manual).
- Todo ocurre en el hilo principal.
- **Excepciones de los suscriptores** (QA-V-01 de la It. 1): la excepción de un suscriptor **no corta a los demás ni sube a quien asignó el valor o emitió**. Se registra en el `IAppLogger` del observable (nivel `Error`, tag `STATE`, con la excepción adjunta) y se sigue con el siguiente; el suscriptor que lanzó sigue suscripto. Es la semántica del legacy: `ChangeNotifier.notifyListeners` atrapa el error de cada *listener*, lo reporta con `FlutterError.reportError` y sigue, y un `StreamController.broadcast` manda el error de un *listener* a la zona sin cancelar a los otros. Vale también para la emisión inicial de `Subscribe(…, emitCurrent: true)`: la suscripción queda hecha y se devuelve. Solo una excepción del propio logger escaparía.
- Por eso `ObservableValue<T>(T initial, IAppLogger logger, IEqualityComparer<T>? comparer = null)` y `EventStream<T>(IAppLogger logger)` **reciben el logger por constructor** (obligatorio): no hay un sumidero estático (ARCHITECTURE §4.1, sin singletons). Quien crea el observable (servicio, *store*, `LanguageState`) ya tiene el logger del *composition root*; los tests pasan `RecordingLogger` o `NullAppLogger.Instance`. El texto del error se arma solo cuando un suscriptor lanzó: emitir sigue sin reservar memoria.

## Alternativas descartadas

- **UniRx / R3**: dependencia grande para lo que se necesita; operadores que no hacen falta.
- **Runtime data binding de UI Toolkit** en el HUD: usa reflexión/propiedades generadas (`[CreateProperty]`, UnityEngine), no está disponible en Core y suma riesgos de stripping. Se puede usar en formularios estáticos si una iteración lo justifica.
- **Propagar la excepción del suscriptor** (comportamiento de la It. 1): el valor quedaba asignado, los suscriptores siguientes no se enteraban y, como un valor igual no notifica, no se iban a enterar nunca (QA-V-01).
- **Sumidero estático de errores** (como `FlutterError.onError`): evitaría pasar el logger, pero es estado global y la arquitectura no admite singletons fuera de `BuildFlags` y `StravaCredentials`.

## Consecuencias

- La lógica de presentación del legacy (`heart_rate_provider_test`, reglas de `heart_rate_card_test`, panel del simulador) se testea en `dotnet`.
- Las vistas tienen algo más de código de binding, explícito y sin magia.
- Una vista rota no congela a las demás: su error queda en el log con el tag `STATE` y el resto de la app sigue recibiendo los cambios.

## Cambios de la It. 2 (deuda de la It. 1)

| ID | Qué cambió |
|---|---|
| QA-V-01 | Aislamiento de la excepción de cada suscriptor (se registra en el logger y se sigue con el resto) y logger obligatorio en el constructor de `ObservableValue`, `EventStream` y `LanguageState` (`Core/Common/SubscriberErrors.cs`). Tests: `ObservableValueTests.Throwing*`, `EventStreamTests.Throwing*`, `LanguageStateTests.BrokenViewDoesNotStopTheOthers` |
