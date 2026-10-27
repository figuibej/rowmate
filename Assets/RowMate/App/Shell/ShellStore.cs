using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Text;

namespace RowMate.App.Shell
{
    // Destinos y selección de la barra de pestañas (F-APP-01, F-APP-02).
    // - 4 destinos en orden (Dispositivo, Workout, Rutinas, Historial) y Perfil al final solo con ShellOptions.ProfileTabEnabled
    //   (AC-APP-02-01, -02); Historial queda siempre en el índice 3.
    // - Al abrir está elegido el 0 y la selección no se guarda (:124, AC-APP-01-01). Elegir el ya elegido no cambia nada
    //   (AC-APP-02-03). La recarga de Historial al tocarla (Q-22) es de la It. 9.
    // legacy: lib/main.dart:116-181 (MainShell)
    public sealed class ShellStore
    {
        public const int HistoryIndex = 3; // :141

        private static readonly TabDestination DeviceTab = new TabDestination(TabDestinationId.Device, L10nKeys.NavDevice,
            L10nKeys.DeviceTitle, Icons.Bluetooth, Icons.BluetoothConnected, false, "tab-device");

        private static readonly TabDestination WorkoutTab = new TabDestination(TabDestinationId.Workout, L10nKeys.NavWorkout,
            L10nKeys.WorkoutTitle, Icons.Timer, Icons.Timer, true, "tab-workout");

        private static readonly TabDestination RoutinesTab = new TabDestination(TabDestinationId.Routines, L10nKeys.NavRoutines,
            L10nKeys.RoutinesTitle, Icons.FitnessCenter, Icons.FitnessCenter, false, "tab-routines");

        private static readonly TabDestination HistoryTab = new TabDestination(TabDestinationId.History, L10nKeys.NavHistory,
            L10nKeys.HistoryTitle, Icons.History, Icons.History, false, "tab-history");

        private static readonly TabDestination ProfileTab = new TabDestination(TabDestinationId.Profile, L10nKeys.NavProfile,
            L10nKeys.ProfileTitle, Icons.Person, Icons.Person, true, "tab-profile");

        private readonly ObservableValue<int> _selected;

        public ShellStore(ShellOptions options, IAppLogger logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Options = options;
            var destinations = new List<TabDestination> { DeviceTab, WorkoutTab, RoutinesTab, HistoryTab };
            if (options.ProfileTabEnabled)
            {
                destinations.Add(ProfileTab);
            }

            Destinations = destinations;
            _selected = new ObservableValue<int>(0, logger);
            SelectedIndex = new ReadOnlyObservable<int>(_selected);
        }

        public ShellOptions Options { get; }

        // 4, o 5 con Perfil al final (:126-134, :150-176).
        public IReadOnlyList<TabDestination> Destinations { get; }

        // 0 al abrir; no se guarda (:124).
        public IReadOnlyObservable<int> SelectedIndex { get; }

        public TabDestination Selected => Destinations[_selected.Value];

        // :146-149. Fuera de rango → ArgumentOutOfRangeException (la barra solo manda índices que existen).
        public void Select(int index)
        {
            if (index < 0 || index >= Destinations.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "ShellStore: no hay destino " + Fmt.Integer(index) + " (hay " + Fmt.Integer(Destinations.Count) + ").");
            }

            _selected.Value = index; // el mismo índice no avisa
        }

        // Índice de un destino, o −1 si no está (Perfil sin Strava).
        public int IndexOf(TabDestinationId id)
        {
            for (var i = 0; i < Destinations.Count; i++)
            {
                if (Destinations[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
