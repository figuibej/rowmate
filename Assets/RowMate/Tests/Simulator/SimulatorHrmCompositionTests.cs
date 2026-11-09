using System;
using System.IO;
using NUnit.Framework;
using RowMate.App.Composition;
using RowMate.App.Device;
using RowMate.App.Shell;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // It. 11 (AC-HRM-15-13; 02-tech-design §5): el build simulator sigue con el pulsómetro simulado, nunca ve el Bluetooth real, no
    // tiene enlace (HrmLinkSource.Idle: sin avisos) y nunca escribe hrm.* en las preferencias.
    [TestFixture]
    public sealed class SimulatorHrmCompositionTests
    {
        private static LocalizationSet Texts()
        {
            return LocalizationSet.Load(File.ReadAllText(RepoPaths.Catalog("es")), File.ReadAllText(RepoPaths.Catalog("en")));
        }

        [Test, Category("AC-HRM-15-13")]
        public void SimulatorUsesSimulatedHrm()
        {
            var scheduler = new FakeScheduler();
            var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: false);
            Assert.That(backend.HeartRate, Is.InstanceOf<SimulatedHeartRateService>());
            Assert.That(backend.HeartRate, Is.Not.InstanceOf<HeartRateService>());
            Assert.That(backend.HeartRate.Link, Is.SameAs(HrmLinkSource.Idle), "sin enlace real");
            var store = new HeartRateStore(backend.HeartRate, NullAppLogger.Instance);
            var notices = new StateRecorder<HrmNotice>(store.Notices);
            store.StartScanAsync();
            scheduler.Advance(TimeSpan.FromSeconds(1));
            store.ConnectAsync(SimulatedHeartRateService.DeviceId, "Pulsómetro simulado");
            scheduler.Advance(TimeSpan.FromSeconds(5));
            store.RetryAsync();
            store.DisconnectAsync();
            scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(notices.Count, Is.EqualTo(0), "el simulador no muestra avisos del pulsómetro");
            Assert.That(store.State.Value.Link, Is.EqualTo(HrmLink.Idle));
            notices.Dispose();
            store.Dispose();
            backend.Dispose();
        }

        [Test, Category("AC-HRM-15-13")]
        public void SimulatorNeverWritesHrmKeys()
        {
            var scheduler = new FakeScheduler();
            var logger = new RecordingLogger();
            var backend = SimulatorBackend.Create(scheduler, logger, noise: false);
            var preferences = new MemoryPreferences();
            var app = AppComposer.Compose(new AppServices(backend, new InMemoryRoutineRepository(), new InMemorySessionRepository(), scheduler, logger,
                Texts(), new FakeSystemLocale("es-AR"), new FakeAccessibilitySettings(1.0, reduceMotion: false), new ShellOptions(false))
            {
                Preferences = preferences,
            });
            app.Start();
            app.HeartRate.StartScanAsync();
            scheduler.Advance(TimeSpan.FromSeconds(1));
            app.HeartRate.ConnectAsync(SimulatedHeartRateService.DeviceId, "Pulsómetro simulado");
            scheduler.Advance(TimeSpan.FromSeconds(5));
            app.HeartRate.DisconnectAsync();
            app.HeartRate.ForgetAsync();
            scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(preferences.TryGetString("hrm.deviceId", out _), Is.False);
            Assert.That(preferences.TryGetString("hrm.deviceName", out _), Is.False);
            app.Dispose();
            backend.Dispose();
        }
    }
}
