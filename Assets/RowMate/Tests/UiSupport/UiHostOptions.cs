using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Surface;

namespace RowMate.Tests.UiSupport
{
    // Cómo se arma un UiTestHost (02-tech-design §4.2 de la It. 2; 03-rutinas/02-tech-design §5.2). Por defecto: TV, dispositivos
    // de prueba, sin capa del simulador, sin área segura, español de Argentina, letra 1, sin reducir movimiento, sin Perfil,
    // GammaRt (la tubería de la app), el reloj de prueba a las 13:45 hora local del 27/10/2026 (así "Hora escena" + da 14:00),
    // rutinas en memoria y vacías, la fecha de creación de un SequenceClock propio (13:00:00.000Z y un minuto más por rutina),
    // un FakeClipboard vacío y el teclado del teléfono oculto.
    public enum SimulatorLayerMode
    {
        None,
        Full,
        ChromeOnly
    }

    public sealed class UiHostOptions
    {
        public static readonly DateTimeOffset DefaultStartUtc = new DateTimeOffset(2026, 10, 27, 16, 45, 0, TimeSpan.Zero);
        public static readonly TimeSpan DefaultLocalOffset = TimeSpan.FromHours(-3);

        public DeviceProfile Profile = DeviceProfile.TV;
        public HostBackendFactory Backend = HostBackends.TestDevices;

        // Las rutinas (03-rutinas/02-tech-design §5.2): en memoria vacías, SQLite real sembrada (Seeded), F-ROTA, de guion…
        public HostRoutineFactory Routines = HostRoutines.Empty;

        // Reloj de la fecha de creación (lo lee SOLO RoutinesStore; QA-G3-19); null = un SequenceClock nuevo por host.
        public IClock? CreationClock;

        // Portapapeles del menú de texto; null = un FakeClipboard vacío por host (ninguna prueba toca el real, R3-10).
        public IClipboard? Clipboard;

        // Menú de texto al mantener presionado en el teléfono (R3-13).
        public TextMenuTouchMode TextMenuTouchMode = TextMenuTouchMode.SystemMenu;

        // Full exige el backend del simulador; ChromeOnly: cinta y llave sin panel. Sin el define, solo None.
        public SimulatorLayerMode SimulatorLayer = SimulatorLayerMode.None;

        // FakeDisplayMetrics (QA-G2-06); por defecto ninguno.
        public SafeInsets Insets = SafeInsets.None;
        public IReadOnlyList<string> LanguageTags = new[] { "es-AR" };
        public double TextScale = 1;
        public bool ReduceMotion;
        public bool ProfileTab;
        public UiColorPipeline Pipeline = UiColorPipeline.GammaRt;
        public DateTimeOffset StartUtc = DefaultStartUtc;
        public TimeSpan LocalOffset = DefaultLocalOffset;
        public bool IsDesktop = true;

        // Política de "Atrás" (falsa en SHT.BackAtRoot…); por defecto, una que solo cuenta.
        public IBackButtonPolicy? BackButton;

        // false: no llama a AppContext.Start() al crear (para probar el arranque a mano).
        public bool StartApp = true;

        public UiHostOptions Clone()
        {
            return (UiHostOptions)MemberwiseClone();
        }
    }

    // "Atrás" de prueba: cuenta las llamadas (SHT.BackAtRootGoesToThePolicyOnce).
    public sealed class CountingBackButtonPolicy : IBackButtonPolicy
    {
        public int Calls { get; private set; }

        public void OnBackAtRoot()
        {
            Calls++;
        }
    }
}
