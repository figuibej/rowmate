using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Core.Scene;

namespace RowMate.App.Scene
{
    // El escenario elegido (F-SCN-21; 02-tech-design §2.8 de la It. 8; DT8-14): el lago por defecto; la preferencia
    // scene.environment con lake, river, coast o regatta (AC-SCN-21-02); elegir cambia en el acto y guarda (AC-SCN-21-03); un
    // toque antes de que termine la lectura gana, también si es el lago (AC-SCN-21-04). Un valor desconocido o sin clave deja el
    // lago sin escribir. Si la escritura falla, JsonFilePreferences deja un aviso y el valor vale para esta sesión. Uno por app
    // (AppContext.SceneEnvironment). Hilo principal. La sesión guardada no registra el escenario.
    // legacy: lib/features/workout/scene/scene_settings.dart:15-41.
    public sealed class SceneEnvironmentChoice : IDisposable
    {
        private readonly IPreferences _preferences;
        private readonly ObservableValue<EnvironmentId> _selected;
        private readonly Action _onLoaded;
        private bool _touched;
        private bool _subscribed;
        private bool _disposed;

        public SceneEnvironmentChoice(IPreferences preferences, IAppLogger? logger = null)
        {
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _selected = new ObservableValue<EnvironmentId>(EnvironmentId.Lake, logger ?? NullAppLogger.Instance);
            _onLoaded = OnLoaded;
            if (preferences.IsLoaded)
            {
                // AC-SCN-21-04: leída al construir, el primer cuadro de Workout ya muestra el guardado.
                Apply();
                return;
            }

            preferences.Loaded += _onLoaded;
            _subscribed = true;
            if (preferences.IsLoaded)
            {
                OnLoaded();
            }
        }

        public IReadOnlyObservable<EnvironmentId> Selected => _selected;

        public int SubscriberCount => _selected.SubscriberCount;

        // scene_settings.dart:36-41: _touched = true ANTES de comparar; el mismo no cambia ni escribe; si no, cambia y guarda.
        public void Select(EnvironmentId id)
        {
            if (_disposed)
            {
                return;
            }

            _touched = true;
            if (id == _selected.Value)
            {
                return;
            }

            _selected.Value = id;
            _preferences.SetString(EnvironmentKeys.PreferenceKey, EnvironmentKeys.ToKey(id));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Unsubscribe();
        }

        private void OnLoaded()
        {
            if (_disposed)
            {
                return;
            }

            Unsubscribe();
            // :29: lo leído se ignora si el usuario ya eligió.
            if (!_touched)
            {
                Apply();
            }
        }

        // :28-31: asNameMap()[getString(key)]; desconocido o sin clave → nada (queda el lago), sin escribir.
        private void Apply()
        {
            if (_preferences.TryGetString(EnvironmentKeys.PreferenceKey, out var key) && EnvironmentKeys.TryParse(key, out var id)
                && id != _selected.Value)
            {
                _selected.Value = id;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed)
            {
                _subscribed = false;
                _preferences.Loaded -= _onLoaded;
            }
        }
    }
}
