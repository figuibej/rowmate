using System;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SBR: la regla de las barras del sistema (F-APP-15; AC-APP-15-01, -04; 02-tech-design §4.1 de la It. 4b). La página pide en
    // cada entrada y al terminar cada salida (a los 640 ms); la regla cuenta los inmersivos (QA-V4a-01, DT4b-05). FakeSystemUi
    // anota cada pedido con el instante del programador. legacy: immersive_workout_screen.dart:32-41 (immersiveSticky al crear la
    // página, edgeToEdge al destruirla).
    [TestFixture]
    public sealed class ImmersiveSystemBarsTests
    {
        [Test, Category("AC-APP-15-01"), Category("AC-APP-15-04")]
        public void RequestsInOrder()
        {
            // Entrada y salida normales: true en el cuadro del toque; false al terminar la salida (640 ms después de su comienzo).
            var f = new Fixture();
            var page = new object();
            f.Bars.EntryStarted(page);
            Assert.That(f.Bars.Hidden, Is.True);
            f.Scheduler.Advance(TimeSpan.FromSeconds(30));
            f.Scheduler.Advance(TimeSpan.FromMilliseconds(640));
            f.Bars.ExitFinished(page);
            Assert.That(f.Ui.Describe(), Is.EqualTo("true@0.000 false@30.640"));
            Assert.That(f.Bars.Hidden, Is.False);
        }

        [Test, Category("AC-APP-15-04")]
        public void SessionEndsDuringTheEntry()
        {
            // La salida arranca desde donde estaba la entrada: un solo true y el false al terminar la salida.
            var f = new Fixture();
            var page = new object();
            f.Bars.EntryStarted(page);
            f.Scheduler.Advance(TimeSpan.FromMilliseconds(500));
            f.Scheduler.Advance(TimeSpan.FromMilliseconds(640));
            f.Bars.ExitFinished(page);
            Assert.That(f.Ui.Describe(), Is.EqualTo("true@0.000 false@1.140"));
        }

        [Test, Category("AC-APP-15-04")]
        public void NewSessionDuringThePreviousExit()
        {
            // QA-V4a-01 (DT4b-05): la sesión nueva abre su inmersivo durante la salida de la anterior: true (la nueva); la salida de
            // la vieja termina sin pedir false; false al terminar la salida de la nueva.
            var f = new Fixture();
            var old = new object();
            var fresh = new object();
            f.Bars.EntryStarted(old);
            f.Scheduler.Advance(TimeSpan.FromSeconds(10));
            f.Bars.EntryStarted(fresh);
            f.Scheduler.Advance(TimeSpan.FromMilliseconds(300));
            f.Bars.ExitFinished(old);
            Assert.That(f.Bars.Hidden, Is.True, "la salida de la vieja no las muestra");
            Assert.That(f.Bars.Owners, Is.EqualTo(1));
            f.Scheduler.Advance(TimeSpan.FromSeconds(20));
            f.Bars.ExitFinished(fresh);
            Assert.That(f.Ui.Describe(), Is.EqualTo("true@0.000 true@10.000 false@30.300"));
            f.Bars.ExitFinished(fresh);
            Assert.That(f.Ui.Requests.Count, Is.EqualTo(3), "una salida que ya terminó no pide nada");
        }

        [Test, Category("AC-APP-15-01")]
        public void BackFromBackgroundAndFocusAskAgain()
        {
            // Android las puede devolver al volver de otra app: con Hidden, al volver del segundo plano y al recuperar el foco, true
            // otra vez. Sin inmersivo, nada.
            var f = new Fixture();
            f.Lifecycle.SetBackground(true);
            f.Lifecycle.SetBackground(false);
            Assert.That(f.Ui.Requests.Count, Is.EqualTo(0));
            var page = new object();
            f.Bars.EntryStarted(page);
            f.Lifecycle.SetBackground(true);
            f.Lifecycle.SetBackground(false);
            f.Lifecycle.SetFocus(false);
            f.Lifecycle.SetFocus(true);
            Assert.That(f.Ui.Describe(), Is.EqualTo("true@0.000 true@0.000 true@0.000"));
        }

        [Test, Category("AC-APP-15-04")]
        public void QuitWithTheImmersiveUpShowsThem()
        {
            // La app se cierra con el inmersivo arriba: false en Dispose. Con el diálogo de completado no hay pedidos (es de UI
            // Toolkit, no una ventana del sistema): la regla no tiene ese caso.
            var f = new Fixture();
            f.Bars.EntryStarted(new object());
            f.Bars.Dispose();
            Assert.That(f.Ui.Describe(), Is.EqualTo("true@0.000 false@0.000"));
            f.Bars.EntryStarted(new object());
            Assert.That(f.Ui.Requests.Count, Is.EqualTo(2), "dispuesta, no pide nada");
            var idle = new Fixture();
            idle.Bars.Dispose();
            Assert.That(idle.Ui.Requests.Count, Is.EqualTo(0), "si estaban visibles, nada");
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            var f = new Fixture();
            Assert.Throws<ArgumentNullException>(() => new ImmersiveSystemBars(null!, f.Lifecycle, f.Logger));
            Assert.Throws<ArgumentNullException>(() => new ImmersiveSystemBars(f.Ui, null!, f.Logger));
            Assert.Throws<ArgumentNullException>(() => new ImmersiveSystemBars(f.Ui, f.Lifecycle, null!));
            Assert.Throws<ArgumentNullException>(() => f.Bars.EntryStarted(null!));
            Assert.Throws<ArgumentNullException>(() => f.Bars.ExitFinished(null!));
        }

        private sealed class Fixture
        {
            public Fixture()
            {
                Logger = new RecordingLogger();
                Scheduler = new FakeScheduler();
                Lifecycle = new FakeAppLifecycle(Logger);
                Ui = new FakeSystemUi(Scheduler);
                Bars = new ImmersiveSystemBars(Ui, Lifecycle, Logger);
            }

            public RecordingLogger Logger { get; }

            public FakeScheduler Scheduler { get; }

            public FakeAppLifecycle Lifecycle { get; }

            public FakeSystemUi Ui { get; }

            public ImmersiveSystemBars Bars { get; }
        }
    }
}
