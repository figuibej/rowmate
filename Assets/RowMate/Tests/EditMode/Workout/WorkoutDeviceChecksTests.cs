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

        // ── It. 5 (05-series-parciales/02-tech-design §6.3, §6.7) ──

        [Test, Category("AC-WKT-18-13")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (el desplazamiento de los paneles con el dedo)")]
        public void AndroidFingerScrollOfThePanels()
        {
            // Android, TH y L640: con los dos paneles abiertos, arrastrar la columna con el dedo la desplaza 1:1, sin inercia y dentro
            // de sus topes; un toque que se mueve menos de 8 dp abre o cierra. La regla la prueba SPG y la selección real, SCT.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-18-09")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (el área táctil de los chips)")]
        public void AndroidTouchAreaOfTheChips()
        {
            // Android: los chips 500 m y Reps responden en todo su área de 48 a 56 dp y uno nunca activa el otro. Las áreas las prueba
            // SPG (SeriesPanelGeometry.Place) y la selección, SCT.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-02-08")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono (los paneles en el primer cuadro al volver)")]
        public void AndroidPanelsOnTheFirstFrameAfterBackground()
        {
            // Android: al volver del segundo plano, las vueltas y repeticiones de ese lapso ya están en el primer cuadro, sin empuje,
            // sin marcas y sin rodillos. La lógica la prueba SEN (segundo plano) y la pantalla, WBU (PlayMode).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-18-10")]
        [Ignore("[Device-Win] a mano: verificado en el build de simulador al empezar la 6a (06a-escena-lago/00-deuda-5 §1.4, pasos 9 y 11)")]
        public void WindowsPanelsSurviveARestart()
        {
            // Windows: abrir 500 m, cerrar la app y volver a abrirla: al iniciar otra vez una rutina, 500 m ya está abierto. La regla
            // la prueban SPP y JFP (con un archivo temporal) y el arranque, BST.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-18-12")]
        [Ignore("[Device-Win] a mano: verificado en el build de simulador al empezar la 6a (06a-escena-lago/00-deuda-5 §1.4, paso 3)")]
        public void WindowsKeyboardOnThePanels()
        {
            // Windows: Tab recorre Terminar, Pausa, 500 m, Reps y la llave con el anillo de foco; Enter y Espacio abren y cierran.
            // La pantalla la prueba SKT (PlayMode).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-18-21")]
        [Ignore("[Device-Win] [PERF] en el build: verificado al empezar la 6a, el peor cuadro 25,0 ms y p99 ≤ 1,5 ms (06a-escena-lago/00-deuda-5 §1.3)")]
        public void WindowsPanelsPerf()
        {
            // Windows (build de simulador, -rowmate-perf-series): con Fondo 3000 y Series 600 y los paneles abriéndose y cerrándose
            // cada 2 s, ningún cuadro de más de 33 ms y p99 ≤ 16,7 ms. El presupuesto y los 0 B, HBT y FAT (PlayMode).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-WKT-13-15")]
        [Ignore("[Device-Win] [PERF] en el build de simulador, ronda 1 de la verificación de la 6a (06a-escena-lago/02-tech-design, \"Cambios por verificación (ronda 1)\" V1-14): -rowmate-perf-entries 10 (libre, Prueba 4a y Cinco reps con los paneles): la primera del proceso 12,7 ms en el cuadro del toque; las siguientes ≤ 10,5 ms, con el peor en el cuadro del armado del HUD (negro y quieto) y ≤ 6,5 ms con movimiento; salidas ≤ 7,9 ms")]
        public void WindowsEntryWithoutJank()
        {
            // Windows (build de simulador): tres entradas seguidas (libre, Prueba 4a y Cinco reps con los dos paneles abiertos):
            // ningún cuadro de más de 16,7 ms mientras algo se mueve. El orden de la entrada lo prueba ITT (PlayMode).
            Assert.Fail("solo en el equipo");
        }

        // ── It. 6a: la escena 3D (06a-escena-lago/02-tech-design §6.3 "Equipo") ──

        [Test, Category("AC-SCN-01-09")]
        [Ignore("NO VERIFICABLE: [Device-And] sin el módulo de Android ni un teléfono de gama media (60 fps con la escena en el nivel Medio; regla 9, It. 14)")]
        public void AndroidSceneSixtyFps()
        {
            // Android (Galaxy A54 / Pixel 6a): libre con Fuerte 5 min a 60 fps sostenidos con la escena del lago (§4.1: ≈ 11,3 ms de GPU).
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-SCN-01-10"), Category("AC-WKT-13-15"), Category("AC-WKT-14-19")]
        [Ignore("[Device-Win] [PERF] en el build de simulador con el código final, ronda 1 de la verificación de la 6a (06a-escena-lago/02-tech-design, \"Cambios por verificación (ronda 1)\" V1-14): libre con Fuerte 5 min en Alto, dos corridas: después de la entrada p99 ≤ 3,0 ms y máx ≤ 4,1 ms de CPU por ventana de 10 s, salvo una ventana de la primera corrida con un cuadro de 93,5 ms (a los 271 s, sin repetirse en la segunda: NO DEMOSTRADO que sea de la app); GPU ≤ 5,5 ms; Medio forzado (sin adaptación) p99 ≤ 2,4 ms; Serie larga 150 s con la escena máx ≤ 7,5 ms, p99 ≤ 5,4 ms; la entrada por el camino del usuario ≤ 12,7 ms (los drivers con StartRoutine o el toque directo, 21 a 24 ms en el cuadro del toque); 0 subidas de shaders de la escena o de URP después de Ready (tools/build/perf-log-check.ps1)")]
        public void WindowsScenePerf()
        {
            // Windows (build de desarrollo con simulador y ROWMATE_DATA_DIR): las ventanas [PERF] de FrameTimeProbe.
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-SCN-23-03")]
        [Ignore("[Device-Win] a mano en el build de simulador: Hora escena a las 22, cerrar y abrir: vuelve a la hora real (no se guarda; SceneHourSimulatorTests lo prueba con un anfitrión nuevo)")]
        public void WindowsSceneHourDoesNotSurviveRestart()
        {
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-SCN-01-05")]
        [Ignore("[Device-Win] en el build de simulador, ronda 1 de la verificación de la 6a: -rowmate-perf-entries 10 (libre, Prueba 4a y Cinco reps) y las demás corridas de V1-14 sin avisos nuevos en el registro y 0 [SLOT] (sin el pack)")]
        public void WindowsThreeSessionsCleanLog()
        {
            Assert.Fail("solo en el equipo");
        }

        [Test, Category("AC-SCN-01-06")]
        [Ignore("[Device-Win] Workout en reposo con la escena precargada: 0 dibujos de la escena después de Ready (SLC, DEMO-01) y, a mano en la verificación de la 6a, el motor 3D del proceso a la mitad que en el inmersivo (contador GPU Engine; 05-qa-verification §4.1). La sonda [PERF] no mide fuera del inmersivo: sin un número propio de la ronda 1")]
        public void WindowsGpuIdleInWorkout()
        {
            Assert.Fail("solo en el equipo");
        }
    }
}
