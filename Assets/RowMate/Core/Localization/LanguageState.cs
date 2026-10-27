using System;
using System.Collections.Generic;
using RowMate.Core.Common;

namespace RowMate.Core.Localization
{
    // Idioma vigente de la app (F-APP-09). Sin selector ni persistencia (Q-05, AC-APP-09-04): la única entrada es la
    // lista de idiomas del sistema, que se vuelve a resolver en cada arranque (constructor) y cuando el sistema avisa
    // que cambió (Refresh, AC-APP-09-05; legacy: android/app/src/main/AndroidManifest.xml:30, configChanges "locale").
    // La lista la da ISystemLocale (Platform, It. 2); esta clase no la lee sola para seguir siendo pura.
    // El logger es solo de diagnóstico: recibe la excepción de una vista suscripta que falla (QA-V-01), no guarda nada.
    public sealed class LanguageState
    {
        private readonly ObservableValue<AppLanguage> _current;

        public LanguageState(IReadOnlyList<string>? initialTags, IAppLogger logger)
        {
            _current = new ObservableValue<AppLanguage>(LocaleResolver.Resolve(initialTags), logger);
            Current = new ReadOnlyLanguage(_current);
        }

        // De solo lectura: nadie puede fijar el idioma desde afuera.
        public IReadOnlyObservable<AppLanguage> Current { get; }

        // Re-resuelve con la lista nueva; notifica solo si el idioma cambia.
        public void Refresh(IReadOnlyList<string>? tags)
        {
            _current.Value = LocaleResolver.Resolve(tags);
        }

        // Vista que no expone el setter de ObservableValue (ni siquiera con un cast).
        private sealed class ReadOnlyLanguage : IReadOnlyObservable<AppLanguage>
        {
            private readonly ObservableValue<AppLanguage> _inner;

            public ReadOnlyLanguage(ObservableValue<AppLanguage> inner)
            {
                _inner = inner;
            }

            public AppLanguage Value => _inner.Value;

            public IDisposable Subscribe(Action<AppLanguage> onChange, bool emitCurrent = true)
            {
                return _inner.Subscribe(onChange, emitCurrent);
            }
        }
    }
}
