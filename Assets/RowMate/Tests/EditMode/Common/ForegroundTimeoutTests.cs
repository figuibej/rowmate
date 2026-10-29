using System;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // FTO: el tope de espera que no cuenta el segundo plano (02-tech-design §4.1 de la It. 4a; spec §12 punto 2; QA-G4aT-03).
    // Con FakeScheduler el "alcance" es Advance mientras InBackground = true, como el orden de AppRoot.
    [TestFixture]
    public sealed class ForegroundTimeoutTests
    {
        [Test, Category("AC-WKT-02-06")]
        public void RunsLikeAScheduleInTheForeground()
        {
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle();
            var fired = 0;
            var timeout = new ForegroundTimeout(scheduler, lifecycle, TimeSpan.FromSeconds(15), () => fired++);
            scheduler.Advance(TimeSpan.FromSeconds(14.9));
            Assert.That(fired, Is.EqualTo(0));
            Assert.That(timeout.Remaining.TotalSeconds, Is.EqualTo(0.1).Within(1e-9));
            scheduler.Advance(TimeSpan.FromSeconds(0.1));
            Assert.That(fired, Is.EqualTo(1));
            Assert.That(timeout.HasFired, Is.True);
            scheduler.Advance(TimeSpan.FromSeconds(60));
            Assert.That(fired, Is.EqualTo(1), "una sola vez");
        }

        [Test, Category("AC-WKT-02-06")]
        public void StopsInTheBackgroundAndResumesWithWhatWasLeft()
        {
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle();
            var fired = 0;
            var timeout = new ForegroundTimeout(scheduler, lifecycle, TimeSpan.FromSeconds(15), () => fired++);
            scheduler.Advance(TimeSpan.FromSeconds(10));
            lifecycle.SetBackground(true);
            scheduler.Advance(TimeSpan.FromSeconds(300));
            Assert.That(fired, Is.EqualTo(0), "el segundo plano no cuenta");
            Assert.That(timeout.Remaining, Is.EqualTo(TimeSpan.FromSeconds(5)));
            lifecycle.SetBackground(false);
            scheduler.Advance(TimeSpan.FromSeconds(4.9));
            Assert.That(fired, Is.EqualTo(0));
            scheduler.Advance(TimeSpan.FromSeconds(0.1));
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-02-06")]
        public void CreatedInTheBackgroundStartsWholeOnReturn()
        {
            // El cierre de una rutina que termina en el alcance (Corta, vuelta a los 330 s): el tope arranca entero al volver.
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle();
            lifecycle.SetBackground(true);
            var fired = 0;
            var timeout = new ForegroundTimeout(scheduler, lifecycle, TimeSpan.FromSeconds(15), () => fired++);
            scheduler.Advance(TimeSpan.FromSeconds(300));
            Assert.That(fired, Is.EqualTo(0));
            Assert.That(timeout.Remaining, Is.EqualTo(TimeSpan.FromSeconds(15)));
            lifecycle.SetBackground(false);
            scheduler.Advance(TimeSpan.FromSeconds(14));
            Assert.That(fired, Is.EqualTo(0));
            scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(fired, Is.EqualTo(1));
        }

        [Test]
        public void DisposeCancels()
        {
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle();
            var fired = 0;
            var timeout = new ForegroundTimeout(scheduler, lifecycle, TimeSpan.FromSeconds(15), () => fired++);
            scheduler.Advance(TimeSpan.FromSeconds(3));
            timeout.Dispose();
            scheduler.Advance(TimeSpan.FromSeconds(60));
            lifecycle.SetBackground(true);
            lifecycle.SetBackground(false);
            scheduler.Advance(TimeSpan.FromSeconds(60));
            Assert.That(fired, Is.EqualTo(0));
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
            Assert.That(timeout.HasFired, Is.False);
            timeout.Dispose();
        }

        [Test]
        public void FocusDoesNotStopIt()
        {
            // Sin foco (Windows minimizada) el loop sigue: el tope corre.
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle();
            var fired = 0;
            using (new ForegroundTimeout(scheduler, lifecycle, TimeSpan.FromSeconds(2), () => fired++))
            {
                lifecycle.SetFocus(false);
                scheduler.Advance(TimeSpan.FromSeconds(2));
                Assert.That(fired, Is.EqualTo(1));
            }
        }

        [Test]
        public void ArgumentsAreChecked()
        {
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle();
            Assert.Throws<ArgumentNullException>(() => new ForegroundTimeout(null!, lifecycle, TimeSpan.Zero, () => { }));
            Assert.Throws<ArgumentNullException>(() => new ForegroundTimeout(scheduler, null!, TimeSpan.Zero, () => { }));
            Assert.Throws<ArgumentNullException>(() => new ForegroundTimeout(scheduler, lifecycle, TimeSpan.Zero, null!));
            Assert.That(ForegroundLifecycle.Instance.IsUserFacing, Is.True);
            Assert.That(ForegroundLifecycle.Instance.InBackground.Value, Is.False);
            Assert.That(ForegroundLifecycle.Instance.HasFocus.Value, Is.True);
        }
    }
}
