using NUnit.Framework;

namespace RowMate.Tests.EditMode.Workout
{
    // WDC: las partes de equipo de la It. 4a que no se pueden verificar sin el módulo de Android y un teléfono (QA-G4aT-09;
    // 02-tech-design §8.6). Cada una es un [Ignore("NO VERIFICABLE: …")] con la categoría de su AC, así la matriz dice
    // IGNORADO (y no SIN TEST) cuando es lo único que tiene el AC; si el AC también es de Unity, sigue en PENDIENTE UNITY hasta
    // tener sus resultados. Lo que dicen los cuerpos es lo que QA mide en el equipo cuando estén el módulo y el teléfono.
    [TestFixture]
    public sealed class WorkoutDeviceChecksTests
    {
        [Test, Category("AC-WKT-02-10")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (la app 2 min en segundo plano con una sesión en curso)")]
        public void AndroidBackgroundTwoMinutes()
        {
            // Android: una libre con el remo, la app 2 min en segundo plano (otra app adelante) y de vuelta: el reloj avanzó 2:00
            // (± 1 s), la distancia siguió sumando y la base tiene 24 puntos más (uno cada 5 s); con una rutina, los pasos por
            // tiempo avanzaron como en primer plano. La lógica la prueba WBG con el ciclo de vida de prueba.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-02-10")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (RA-19: el reloj monótono con el teléfono dormido)")]
        public void AndroidMonotonicClockAcrossDeepSleep()
        {
            // RA-19 (R4a-12): con la pantalla apagada y el teléfono en reposo profundo, el reloj monótono de Unity sigue
            // contando (elapsedRealtime) o no (uptimeMillis). Se mide en el mismo teléfono junto al legacy antes de la 10a;
            // FSC.ResumeWithASmallerMonotonicJumpKeepsOrder prueba que un salto menor que el de la hora de pared no rompe el
            // orden del alcance.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-02-08")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (el primer cuadro al volver del segundo plano)")]
        public void AndroidFirstFrameAfterBackground()
        {
            // Android: al volver del segundo plano el primer cuadro ya muestra el presente (reloj, valores, paso, cuenta), sin
            // rodillos ni valores intermedios, y solo el último aviso, una vez. En Windows minimizada la app sigue andando y no
            // hay vuelta que alcanzar (AC-WKT-02-09). La lógica la prueban WEL.OneSnapshotPerReturn y
            // WHS.OneInstantStateAfterBackground; la pantalla, WBU (PlayMode).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-13-03")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (\"Atrás\" del sistema en el inmersivo)")]
        public void AndroidBackOpensTheConfirmation()
        {
            // Android: "Atrás" en el inmersivo abre el diálogo de terminar (y con el diálogo abierto, lo cierra). Documental: el
            // AC es de pantalla (HCT.BackOpensTheConfirmation en PlayMode) y sin sus resultados sigue en PENDIENTE UNITY.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-APP-14-02")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (la pantalla encendida después de volver)")]
        public void AndroidScreenStaysOnAfterResume()
        {
            // Android: con la app adelante la pantalla no se apaga, también después de ir y volver del segundo plano
            // (FLAG_KEEP_SCREEN_ON otra vez). La regla de volver a pedirla la prueba SKA.RequestsAgainOnResumeAndFocus.
            Assert.Fail("solo en el equipo");
        }

        // ── It. 4b (02-tech-design §6.6 de la It. 4b) ──

        [Test, Category("AC-APP-15-01")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (las barras del sistema ocultas y su regreso)")]
        public void AndroidSystemBarsHiddenAndBack()
        {
            // Android: al entrar al inmersivo las barras de estado y de navegación se ocultan en modo persistente (si el usuario las
            // revela deslizando, se vuelven a ocultar solas) y vuelven al terminar la salida; nada del HUD ni de Workout se mueve. La
            // regla la prueba SBR (dotnet) y la pantalla SBU (PlayMode, con el doble).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-APP-15-03")]
        [Ignore("NO VERIFICABLE: [Apple] la barra de estado y el indicador de inicio en iOS van en la It. 10b")]
        public void AppleStatusBarHidden()
        {
            // iOS: prefersStatusBarHidden y prefersHomeIndicatorAutoHidden con el helper Swift (ARCHITECTURE §13). En la 4b,
            // DesktopSystemUi (sin efecto).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-13-07")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (\"Atrás\" del sistema durante la entrada)")]
        public void AndroidBackDuringTheEntry()
        {
            // Android: "Atrás" durante la cascada de la entrada abre la confirmación de terminar desde el cuadro 0 (ITT.BackFromTheFirstFrame
            // en PlayMode). Documental: sin sus resultados de Unity sigue en PENDIENTE UNITY.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-21-02")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (\"Atrás\" con el diálogo de completado)")]
        public void AndroidBackWithTheCompletionDialog()
        {
            // Android: "Atrás" con el diálogo de completado abierto no hace nada (no es descartable). La pantalla la prueba CDT.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-02-11")]
        [Ignore("NO VERIFICABLE: [Device-Win] lo verifica el usuario en la demo (c): QA no suspende su equipo")]
        public void WindowsSuspendDoesNotCount()
        {
            // Windows (build de simulador): con una sesión en marcha se suspende el equipo unos minutos y se lo despierta: el reloj
            // de la sesión sigue desde donde estaba, sin el lapso; el registro dice "[WORKOUT] el equipo durmió N s: no cuenta (P-17;
            // …)" o, sin señal, "[TIME] cuadro de N s sin aviso de suspensión: cuenta". La regla la prueban SUS y WSU.
            Assert.Fail("solo en el equipo");
        }
    }
}
