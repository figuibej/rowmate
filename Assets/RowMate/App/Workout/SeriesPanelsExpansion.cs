using System;
using RowMate.Core.Common;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // Los dos paneles, abiertos o cerrados (02-tech-design §2.7 de la It. 5).
    public readonly struct SeriesPanelsExpanded : IEquatable<SeriesPanelsExpanded>
    {
        public SeriesPanelsExpanded(bool laps, bool reps)
        {
            Laps = laps;
            Reps = reps;
        }

        public bool Laps { get; }

        public bool Reps { get; }

        public bool Of(SeriesPanelKind kind)
        {
            return kind == SeriesPanelKind.Laps ? Laps : Reps;
        }

        public SeriesPanelsExpanded With(SeriesPanelKind kind, bool expanded)
        {
            return kind == SeriesPanelKind.Laps ? new SeriesPanelsExpanded(expanded, Reps) : new SeriesPanelsExpanded(Laps, expanded);
        }

        public bool Equals(SeriesPanelsExpanded other)
        {
            return Laps == other.Laps && Reps == other.Reps;
        }

        public override bool Equals(object? obj)
        {
            return obj is SeriesPanelsExpanded other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Laps ? 1 : 0) | (Reps ? 2 : 0);
        }

        public override string ToString()
        {
            return "laps=" + (Laps ? "abierto" : "cerrado") + " reps=" + (Reps ? "abierto" : "cerrado");
        }
    }

    // Lo que la vista de los paneles lee y toca (HudView, SeriesColumn): el estado abierto o cerrado y Toggle. Lo implementan
    // SeriesPanelPrefs (las preferencias de la app) y LocalSeriesPanelsExpansion (en memoria; el precalentado: QA-G5T-05).
    public interface ISeriesPanelsExpansion
    {
        IReadOnlyObservable<SeriesPanelsExpanded> State { get; }

        void Toggle(SeriesPanelKind kind);
    }

    // Un estado abierto o cerrado que vive solo en memoria y NUNCA toca las preferencias: Toggle cambia su State y nada más
    // (el precalentado del HUD, HudWarmup: no escribe prefs.json ni se suscribe al State de la app; QA-G5T-05, DD5-12).
    public sealed class LocalSeriesPanelsExpansion : ISeriesPanelsExpansion
    {
        private readonly ObservableValue<SeriesPanelsExpanded> _state;

        public LocalSeriesPanelsExpansion(bool laps, bool reps, IAppLogger? logger = null)
        {
            _state = new ObservableValue<SeriesPanelsExpanded>(new SeriesPanelsExpanded(laps, reps), logger ?? NullAppLogger.Instance);
        }

        public IReadOnlyObservable<SeriesPanelsExpanded> State => _state;

        public void Toggle(SeriesPanelKind kind)
        {
            var current = _state.Value;
            _state.Value = current.With(kind, !current.Of(kind));
        }
    }
}
