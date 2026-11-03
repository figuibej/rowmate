using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // Los dos paneles con las preferencias de la app (F-WKT-18; 02-tech-design §2.7 de la It. 5; AC-WKT-18-10): cerrados por
    // defecto (LT-59); con la clave en verdadero, abiertos desde que se ven (LT-60); un toque antes de que termine la lectura
    // gana (:130-132, :144-149); cada toque guarda en el acto (:153-156: el valor en memoria cambia ya y el guardado puede
    // terminar después o fallar sin aviso, AC-WKT-18-11). El de repeticiones conserva su estado aunque no se vea. Uno por app
    // (AppContext.SeriesPanels): lo comparten todos los inmersivos, así los dos que conviven un instante (QA-V4a-01) ven lo
    // mismo. Hilo principal. Abrir o cerrar no toca el motor, la sesión ni la telemetría (AC-WKT-13-14).
    // legacy: lib/features/workout/series_panels.dart:113-156.
    public sealed class SeriesPanelPrefs : ISeriesPanelsExpansion, IDisposable
    {
        // :120-121.
        public const string LapsKey = "immersive.lapsExpanded";
        public const string RepsKey = "immersive.repsExpanded";

        private readonly IPreferences _preferences;
        private readonly ObservableValue<SeriesPanelsExpanded> _state;
        private readonly Action _onLoaded;
        private bool _lapsTouched;
        private bool _repsTouched;
        private bool _subscribed;
        private bool _disposed;

        public SeriesPanelPrefs(IPreferences preferences, IAppLogger? logger = null)
        {
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _state = new ObservableValue<SeriesPanelsExpanded>(default, logger ?? NullAppLogger.Instance);
            _onLoaded = OnLoaded;
            if (preferences.IsLoaded)
            {
                _state.Value = Read(false, false, default);
                return;
            }

            // :134-151: la lectura llega después; lo que el usuario ya tocó no se pisa.
            preferences.Loaded += _onLoaded;
            _subscribed = true;
            if (preferences.IsLoaded)
            {
                OnLoaded();
            }
        }

        // Un ObservableValue: SubscriberCount (y el de la propiedad de abajo) para las pruebas del precalentado (QA-G5T-05).
        public IReadOnlyObservable<SeriesPanelsExpanded> State => _state;

        public int SubscriberCount => _state.SubscriberCount;

        // :169-175, :184-190: cambia en el acto y guarda (:153-156).
        public void Toggle(SeriesPanelKind kind)
        {
            if (_disposed)
            {
                return;
            }

            var current = _state.Value;
            var expanded = !current.Of(kind);
            if (kind == SeriesPanelKind.Laps)
            {
                _lapsTouched = true;
            }
            else
            {
                _repsTouched = true;
            }

            _state.Value = current.With(kind, expanded);
            _preferences.SetBool(kind == SeriesPanelKind.Laps ? LapsKey : RepsKey, expanded);
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
            _state.Value = Read(_lapsTouched, _repsTouched, _state.Value);
        }

        // prefs.getBool(key) ?? false (:138-143): sin clave, o con otro tipo, cerrado.
        private SeriesPanelsExpanded Read(bool keepLaps, bool keepReps, SeriesPanelsExpanded current)
        {
            var laps = keepLaps ? current.Laps : _preferences.TryGetBool(LapsKey, out var l) && l;
            var reps = keepReps ? current.Reps : _preferences.TryGetBool(RepsKey, out var r) && r;
            return new SeriesPanelsExpanded(laps, reps);
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
