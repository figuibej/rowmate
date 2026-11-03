using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Platform.Power;
using RowMate.Tests.Support;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // SSL · La fuente de sueño de Windows (P-17 de la 4a; 04b-hud-exr/02-tech-design §2.9.3; QA-G4bT-05 (c), (d)): en el Editor de
    // Windows la medida del núcleo nunca baja y el aviso de suspensión NO se registra (una recarga de dominio dejaría al sistema
    // llamando a código descargado); el orden del registro (reservar la estructura, registrar; al cerrar, dar de baja y RECIÉN
    // DESPUÉS liberar) se prueba con un doble de las llamadas nativas, sin registrar nada en el sistema. El registro real lo mira QA
    // en el build ([Device-Win]).
    public sealed class SystemSleepSourceTests
    {
        private sealed class RecordingApi : IPowerNotifyApi
        {
            public readonly List<string> Calls = new List<string>();
            public IntPtr RegisteredWith;

            public uint Register(IntPtr parameters, out IntPtr handle)
            {
                RegisteredWith = parameters;
                Calls.Add("register:" + (parameters != IntPtr.Zero));
                handle = new IntPtr(42);
                return 0;
            }

            public uint Unregister(IntPtr handle)
            {
                Calls.Add("unregister:" + handle.ToInt64());
                return 0;
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void InTheWindowsEditorTheMeasureNeverGoesDownAndNothingIsRegistered()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE: solo en el Editor de Windows");
            }

            using (var source = (WindowsSleepSource)SleepSourceFactory.Create(new RecordingLogger()))
            {
                Assert.That(source.SignalsRegistered, Is.False, "en el Editor no se registra el aviso (DT4b-20)");
                var first = source.AsleepSeconds;
                Assert.That(double.IsNaN(first), Is.False, "la medida existe (GetTickCount64 − QueryUnbiasedInterruptTime)");
                var previous = first;
                for (var i = 0; i < 20; i++)
                {
                    var next = source.AsleepSeconds;
                    Assert.That(next, Is.GreaterThanOrEqualTo(previous - 0.02), "nunca baja (más que la granularidad del contador)");
                    previous = next;
                }
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void RegisterThenUnregisterBeforeFreeing()
        {
            var api = new RecordingApi();
            var source = new WindowsSleepSource(new RecordingLogger(), true, api);
            Assert.That(source.SignalsRegistered, Is.True);
            Assert.That(source.Parameters, Is.Not.EqualTo(IntPtr.Zero), "la estructura vive en AllocHGlobal mientras dura el registro");
            Assert.That(api.RegisteredWith, Is.EqualTo(source.Parameters));
            source.Dispose();
            Assert.That(api.Calls, Is.EqualTo(new[] { "register:True", "unregister:42" }), "primero la baja");
            Assert.That(source.Parameters, Is.EqualTo(IntPtr.Zero), "y después se libera");
            source.Dispose();
            Assert.That(api.Calls.Count, Is.EqualTo(2), "idempotente");
        }

        [Test, Category("AC-WKT-02-11")]
        public void TheCallbackOnlyCountsSuspendAndResume()
        {
            var api = new RecordingApi();
            using (var source = new WindowsSleepSource(new RecordingLogger(), true, api))
            {
                var suspend = source.SuspendSignals;
                var resume = source.ResumeSignals;
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmSuspend);
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmResumeAutomatic);
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmResumeSuspend);
                WindowsSleepSource.SimulatePowerEvent(10);
                Assert.That(source.SuspendSignals - suspend, Is.EqualTo(1));
                Assert.That(source.ResumeSignals - resume, Is.EqualTo(2), "los dos avisos de reanudar");
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void AndroidAndIosHaveNoSource()
        {
            var none = NullSleepSource.Instance;
            Assert.That(double.IsNaN(none.AsleepSeconds), Is.True);
            Assert.That(none.SuspendSignals + none.ResumeSignals, Is.EqualTo(0));
            Assert.That(double.IsNaN(none.SignalAsleepSeconds), Is.True);
        }

        // It. 5 (05-series-parciales/00-deuda-4b §1): lo que durmió según los avisos es el último ciclo completo, de "se suspende" al
        // "reanuda" que lo siguió (con la hora de cada uno); un "se suspende" sin su "reanuda" todavía no tiene duración.
        [Test, Category("AC-WKT-02-11")]
        public void SignalAsleepIsTheLastCompleteCycle()
        {
            using (var source = new WindowsSleepSource(new RecordingLogger(), true, new RecordingApi()))
            {
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmSuspend, 1000);
                Assert.That(double.IsNaN(source.SignalAsleepSeconds), Is.True, "sin el reanuda");
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmResumeAutomatic, 61400);
                Assert.That(source.SignalAsleepSeconds, Is.EqualTo(60.4).Within(1e-9));
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmSuspend, 70000);
                Assert.That(double.IsNaN(source.SignalAsleepSeconds), Is.True, "un ciclo nuevo sin cerrar");
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmResumeSuspend, 70500);
                Assert.That(source.SignalAsleepSeconds, Is.EqualTo(0.5).Within(1e-9));
            }
        }

        // El callback de verdad (el que llama el sistema) toma la hora con GetTickCount64 (P/Invoke desde ese hilo).
        [Test, Category("AC-WKT-02-11")]
        public void TheCallbackTakesTheSystemTick()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE: solo en el Editor de Windows");
            }

            using (var source = new WindowsSleepSource(new RecordingLogger(), true, new RecordingApi()))
            {
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmSuspend);
                WindowsSleepSource.SimulatePowerEvent(WindowsSleepSource.PbtApmResumeAutomatic);
                Assert.That(source.SignalAsleepSeconds, Is.InRange(0.0, 1.0), "la hora real de los dos avisos, uno detrás del otro");
            }
        }

        [Test, Category("AC-WKT-02-11")]
        public void TheLogSaysWhetherTheMeasureExists()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE: solo en el Editor de Windows");
            }

            var logger = new RecordingLogger();
            using (new WindowsSleepSource(logger, false))
            {
                Assert.That(logger.Contains("[TIME] Info: medida del sueño del núcleo disponible"), Is.True, string.Join("\n", logger.Lines));
            }
        }
    }
}
