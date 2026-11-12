using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // WMS · La regla de elección del agua (06b-agua-avanzada/02-tech-design §3.2; AC-6B-06; F-SCN-13; water_shader.dart:12-22): una
    // prueba por fila de la tabla de la spec.
    [TestFixture]
    public sealed class WaterModeSelectorTests
    {
        [Test, Category("AC-6B-06")]
        public void UnsupportedOrFailedIsDegraded()
        {
            var unsupported = WaterSelectorInputs.For(QualityTier.High, true);
            unsupported.Supported = false;
            var choice = WaterModeSelector.Choose(unsupported);
            Assert.That(choice.Kind, Is.EqualTo(WaterKind.Degraded));
            Assert.That(choice.Showing, Is.EqualTo(WaterKind.Degraded));
            Assert.That(choice.Reason, Is.EqualTo(WaterModeSelector.UnsupportedReason));

            var failed = WaterSelectorInputs.For(QualityTier.Medium, true);
            failed.Failed = true;
            failed.Reason = "RT del reflejo perdida";
            choice = WaterModeSelector.Choose(failed);
            Assert.That(choice.Kind, Is.EqualTo(WaterKind.Degraded));
            Assert.That(choice.Reason, Is.EqualTo("RT del reflejo perdida"), "el motivo de la caída");
        }

        [TestCase(QualityTier.High, TestName = "AC-6B-06 · Alto: la avanzada con el planar")]
        [TestCase(QualityTier.Medium, TestName = "AC-6B-06 · Medio: la avanzada con el reflejo analítico")]
        [TestCase(QualityTier.Low, TestName = "AC-6B-06 · Bajo: la avanzada Lite")]
        [Category("AC-6B-06")]
        public void EachTierGetsItsAdvancedVariant(QualityTier tier)
        {
            var ready = WaterModeSelector.Choose(WaterSelectorInputs.For(tier, true));
            Assert.That(ready.Kind, Is.EqualTo(WaterKind.Advanced));
            Assert.That(ready.Variant, Is.EqualTo(tier));
            Assert.That(ready.Showing, Is.EqualTo(WaterKind.Advanced));
            Assert.That(ready.Reason, Is.Null);

            // AC-6B-07: mientras la avanzada se prepara se ve la degradada.
            var preparing = WaterModeSelector.Choose(WaterSelectorInputs.For(tier, false));
            Assert.That(preparing.Kind, Is.EqualTo(WaterKind.Advanced));
            Assert.That(preparing.Showing, Is.EqualTo(WaterKind.Degraded));
        }

        [Test, Category("AC-6B-06")]
        public void ThermalExhaustedIsDegraded()
        {
            var inputs = WaterSelectorInputs.For(QualityTier.Low, true);
            inputs.ThermalDegraded = true;
            var choice = WaterModeSelector.Choose(inputs);
            Assert.That(choice.Kind, Is.EqualTo(WaterKind.Degraded));
            Assert.That(choice.Reason, Is.EqualTo(WaterModeSelector.ThermalReason));
        }

        [Test, Category("AC-6B-06")]
        public void ForcedOnlyInDevelopment()
        {
            var degraded = WaterSelectorInputs.For(QualityTier.High, true);
            degraded.Force = WaterForce.Degraded;
            Assert.That(WaterModeSelector.Choose(degraded).Kind, Is.EqualTo(WaterKind.Advanced), "el build normal ignora el argumento");
            degraded.ForceAllowed = true;
            Assert.That(WaterModeSelector.Choose(degraded).Kind, Is.EqualTo(WaterKind.Degraded), "forzada en desarrollo");

            // "advanced" en un equipo sin soporte: la degradada, con su motivo (DT6b-14).
            var advanced = WaterSelectorInputs.For(QualityTier.High, true);
            advanced.Force = WaterForce.Advanced;
            advanced.ForceAllowed = true;
            advanced.Supported = false;
            var choice = WaterModeSelector.Choose(advanced);
            Assert.That(choice.Kind, Is.EqualTo(WaterKind.Degraded));
            Assert.That(choice.Reason, Is.EqualTo(WaterModeSelector.UnsupportedReason));

            // Con soporte, la avanzada forzada gana sobre el escalón térmico (con el nivel forzado la adaptación está apagada).
            advanced.Supported = true;
            advanced.ThermalDegraded = true;
            Assert.That(WaterModeSelector.Choose(advanced).Kind, Is.EqualTo(WaterKind.Advanced));
            advanced.Force = WaterForce.Auto;
            Assert.That(WaterModeSelector.Choose(advanced).Kind, Is.EqualTo(WaterKind.Degraded), "Auto respeta el escalón térmico");
        }

        [Test, Category("AC-6B-06")]
        public void ThumbnailIsAlwaysDegraded()
        {
            // F-SCN-22, paridad: las miniaturas del selector siguen con la degradada (DT6b-19), también forzando la avanzada.
            var inputs = WaterSelectorInputs.For(QualityTier.High, true);
            inputs.Thumbnail = true;
            inputs.Force = WaterForce.Advanced;
            inputs.ForceAllowed = true;
            var choice = WaterModeSelector.Choose(inputs);
            Assert.That(choice.Kind, Is.EqualTo(WaterKind.Degraded));
            Assert.That(choice.Reason, Is.EqualTo(WaterModeSelector.ThumbnailReason));
            Assert.That(ThumbnailRule.Water, Is.EqualTo(WaterForce.Degraded), "la regla de la miniatura lo dice");
        }
    }

    // WAV · El agua de la sesión de la app (DT6b-01, DT6b-13; AC-6B-08; DV-152): pegada sin reintento y una sola línea del registro.
    [TestFixture]
    public sealed class WaterAvailabilityTests
    {
        [Test, Category("AC-6B-08")]
        public void StickyNoRetry()
        {
            var a = new WaterAvailability();
            Assert.That(a.IsDegradedForApp, Is.False);
            Assert.That(WaterModeSelector.Choose(a.Inputs(QualityTier.High, WaterForce.Auto, false, false, true)).Kind, Is.EqualTo(WaterKind.Advanced));
            Assert.That(a.ReportFailure("excepción en el dibujo"), Is.True);
            for (var session = 0; session < 5; session++)
            {
                // Salir y volver a entrar (otra sesión de escena) no reintenta, aunque la avanzada esté "lista".
                var choice = WaterModeSelector.Choose(a.Inputs(QualityTier.High, WaterForce.Auto, false, false, true));
                Assert.That(choice.Kind, Is.EqualTo(WaterKind.Degraded));
                Assert.That(choice.Reason, Is.EqualTo("excepción en el dibujo"));
            }

            var thermal = new WaterAvailability();
            Assert.That(thermal.ReportThermal(), Is.True);
            Assert.That(thermal.Reason, Is.EqualTo(WaterModeSelector.ThermalReason));
            Assert.That(WaterModeSelector.Choose(thermal.Inputs(QualityTier.Low, WaterForce.Auto, false, false, true)).Kind, Is.EqualTo(WaterKind.Degraded));
        }

        [Test, Category("AC-6B-08")]
        public void OneLogLine()
        {
            var a = new WaterAvailability();
            var lines = 0;
            if (a.ReportUnsupported(WaterModeSelector.UnsupportedReason))
            {
                lines++;
            }

            if (a.ReportFailure("otra"))
            {
                lines++;
            }

            if (a.ReportThermal())
            {
                lines++;
            }

            Assert.That(lines, Is.EqualTo(1), "una sola línea por sesión de la app");
            Assert.That(a.Reason, Is.EqualTo(WaterModeSelector.UnsupportedReason), "el motivo de la primera");
            Assert.That(WaterFallbackLog.Tagged(a.Reason!), Is.EqualTo("[Scene] Shader de agua no disponible, usando fallback: sin SubShader avanzado"),
                "el texto del legacy (water_shader.dart:18) con su etiqueta");
            Assert.That(WaterFallbackLog.Line(""), Does.StartWith(WaterFallbackLog.Prefix));
        }
    }

    // WFD · El fundido entre la avanzada y la degradada (AC-6B-07, -08; DT6b-12): 0,6 s; en el acto con reducir movimiento; sin saltos.
    [TestFixture]
    public sealed class WaterFadeTests
    {
        [Test, Category("AC-6B-07")]
        public void ArrivesWithAFadeOfSixTenths()
        {
            var fade = default(WaterFade);
            Assert.That(fade.DegradedBlend(0), Is.EqualTo(1), "sin empezar: la degradada");
            fade.Start(10, true, false);
            Assert.That(fade.DegradedBlend(10), Is.EqualTo(1));
            Assert.That(fade.DegradedBlend(10.3), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(fade.Running(10.3), Is.True);
            Assert.That(fade.DegradedBlend(10.6), Is.EqualTo(0).Within(1e-12));
            Assert.That(fade.Running(10.61), Is.False);
            Assert.That(fade.TowardAdvanced, Is.True);
        }

        [Test, Category("AC-6B-07")]
        public void ReduceMotionIsInstant()
        {
            var fade = default(WaterFade);
            fade.Start(1, true, true);
            Assert.That(fade.DegradedBlend(1), Is.EqualTo(0));
            Assert.That(fade.Running(1), Is.False);
        }

        [Test, Category("AC-6B-08")]
        public void FallingBackFadesWithoutJumps()
        {
            var fade = default(WaterFade);
            fade.Start(0, true, false);
            var mid = fade.DegradedBlend(0.2);
            fade.Start(0.2, false, false);
            Assert.That(fade.DegradedBlend(0.2), Is.EqualTo(mid).Within(1e-12), "sigue desde donde estaba");
            var last = mid;
            for (var t = 0.2; t <= 1.0; t += 1.0 / 60)
            {
                var b = fade.DegradedBlend(t);
                Assert.That(b, Is.GreaterThanOrEqualTo(last - 1e-12));
                Assert.That(b - last, Is.LessThanOrEqualTo(1.0 / 60 / WaterFade.Seconds + 1e-9), "a lo sumo un paso de 0,6 s por cuadro");
                last = b;
            }

            Assert.That(last, Is.EqualTo(1));
            fade.Snap(false);
            Assert.That(fade.DegradedBlend(5), Is.EqualTo(1), "el corte en el acto (la avanzada no puede dibujar, DT6b-12)");
        }
    }
}
