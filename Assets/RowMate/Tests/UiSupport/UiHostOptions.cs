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
    // It. 6a (06a-escena-lago/02-tech-design §1.1 B1, §5.1): sin escena (el fondo provisorio de las It. 2 a 5, por defecto) o la escena
    // real (SceneHost con la compilación de shaders sincrónica mientras dura el anfitrión, la orilla armada en el acto y la escena
    // avanzada en pasos de 1/60 s con el reloj de prueba).
    public enum SceneMode
    {
        None,
        Real,
    }

    public enum SimulatorLayerMode
    {
        None,
        Full,
        ChromeOnly
    }

    public sealed class UiHostOptions
    {
        public static readonly DateTimeOffset DefaultStartUtc = new DateTimeOffset(2026, 10, 27, 16, 45, 0, TimeSpan.Zero);

        // El reloj de prueba de la It. 4a (spec §0.4): la sesión se crea en 2026-10-28T13:00:00.000Z (conjunto it4a).
        public static readonly DateTimeOffset WorkoutStartUtc = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);
        public static readonly TimeSpan DefaultLocalOffset = TimeSpan.FromHours(-3);

        public DeviceProfile Profile = DeviceProfile.TV;
        public HostBackendFactory Backend = HostBackends.TestDevices;

        // Las rutinas (03-rutinas/02-tech-design §5.2): en memoria vacías, SQLite real sembrada (Seeded), F-ROTA, de guion…
        public HostRoutineFactory Routines = HostRoutines.Empty;

        // Las sesiones del motor (It. 4a, §7.1): por defecto el mismo archivo que las rutinas si son SQLite, en memoria si no.
        public HostSessionFactory Sessions = HostSessions.SameFileAsRoutines;

        // El contorno de depuración de la zona del bote en el inmersivo (#hud-boat-zone; solo PlayMode y el arnés, CAP-16).
        public bool BoatZoneOutline;

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

        // It. 5 (05-series-parciales/02-tech-design §2.7, §6.2; AC-HIS-20-04): las preferencias de la app (los paneles de series). Por
        // defecto, una MemoryPreferences nueva por anfitrión; con PreferencesFile = true, el almacén real (JsonFilePreferences) en una
        // carpeta temporal propia que el anfitrión borra al terminar (y que Reopen vuelve a leer: el reinicio). Nunca el archivo del
        // usuario.
        public IPreferences? Preferences;
        public bool PreferencesFile;

        // It. 6a: la escena (None por defecto), su nivel (Alto, el del escritorio), la región de la luna (null = norte) y, para las pruebas
        // de los slots, otro mundo (una copia en memoria con el slot de prueba).
        public SceneMode Scene = SceneMode.None;
        public RowMate.Core.Scene.QualityTier SceneTier = RowMate.Core.Scene.QualityTier.High;
        public string? SceneRegion;
        public RowMate.Scene.Slots.SceneWorldDefinition? SceneWorld;

        // It. 6a (B6a-19): el armado de la escena con el tramo del player (SceneWarmupPolicy) en lugar de todo en el acto.
        public bool ScenePlayerWarmup;

        // It. 6a (QA-V6a-REN-04): la orilla por tramos, como en el player (StreamingBudgetMs por cuadro), en lugar de armada en el acto.
        public bool ScenePlayerStreaming;

        // It. 6a (QA-V6a-REN-16): el nivel del asset de URP (TierScope: sombras, MSAA) mientras dura el anfitrión; null = el del proyecto.
        public RowMate.Core.Scene.QualityTier? SceneUrpTier;

        // Un puerto de escena de prueba en lugar de la real (FakeImmersiveScene; QA-G6aT-16). Gana sobre Scene.
        public RowMate.Presentation.Workout.IImmersiveScene? FakeScene;

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
