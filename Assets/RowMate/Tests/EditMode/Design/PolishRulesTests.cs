using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Ports;

namespace RowMate.Tests.EditMode.Design
{
    // BS · La salida de la capa de arranque (It. 14, AC-APP-16-10; DT14-11).
    [TestFixture]
    public sealed class BootSplashMotionTests
    {
        [Test, Category("AC-APP-16-10")]
        public void ExitIn200WithEaseExitOr120Linear()
        {
            Assert.That(BootSplashMotion.DurationMs(false), Is.EqualTo(200));
            Assert.That(BootSplashMotion.DurationMs(true), Is.EqualTo(120));
            Assert.That(BootSplashMotion.MarkDp, Is.EqualTo(96));
            Assert.That(BootSplashMotion.Opacity(double.NaN, false), Is.EqualTo(1), "shell sin listo: tapa todo");
            Assert.That(BootSplashMotion.Opacity(0, false), Is.EqualTo(1));
            Assert.That(BootSplashMotion.Opacity(100, false), Is.EqualTo(1 - Easing.Evaluate(Tokens.EaseExit, 0.5)).Within(1e-12));
            Assert.That(BootSplashMotion.Opacity(100, false), Is.GreaterThan(0.5), "ease.exit arranca lento");
            Assert.That(BootSplashMotion.Opacity(200, false), Is.EqualTo(0));
            Assert.That(BootSplashMotion.Opacity(60, true), Is.EqualTo(0.5).Within(1e-12), "lineal");
            Assert.That(BootSplashMotion.Opacity(120, true), Is.EqualTo(0));
            Assert.That(BootSplashMotion.IsDone(199, false), Is.False);
            Assert.That(BootSplashMotion.IsDone(200, false), Is.True);
            Assert.That(BootSplashMotion.IsDone(double.NaN, true), Is.False);
            Assert.That(BootSplashMotion.BlocksInput(double.NaN), Is.True);
            Assert.That(BootSplashMotion.BlocksInput(0), Is.False, "desde que empieza la salida, responde");
        }

        [Test, Category("AC-APP-16-10")]
        public void SpinnerOnlyAfter1500()
        {
            Assert.That(BootSplashMotion.SpinnerOpacity(1500), Is.EqualTo(0));
            Assert.That(BootSplashMotion.SpinnerOpacity(1580), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(BootSplashMotion.SpinnerOpacity(1660), Is.EqualTo(1));
            Assert.That(BootSplashMotion.SpinnerDp, Is.EqualTo(32));
            Assert.That(BootSplashMotion.SpinnerGapDp, Is.EqualTo(32));
        }
    }

    // SL · El indicador de carga de la escena (It. 14, #21, AC-DEBT-09; DT14-30).
    [TestFixture]
    public sealed class SceneLoadingSpecsTests
    {
        [Test, Category("AC-DEBT-09")]
        public void GraceFadeAndExit()
        {
            Assert.That(SceneLoadingSpecs.GraceMs, Is.EqualTo(120));
            Assert.That(SceneLoadingSpecs.FadeMs, Is.EqualTo(160));
            Assert.That(SceneLoadingSpecs.IndicatorDp, Is.EqualTo(32));
            Assert.That(SceneLoadingSpecs.NeedsIndicator(true), Is.False, "las entradas siguientes, sin indicador");
            Assert.That(SceneLoadingSpecs.Opacity(true, 500, double.NaN), Is.EqualTo(0));
            Assert.That(SceneLoadingSpecs.Opacity(false, 120, double.NaN), Is.EqualTo(0), "gracia");
            Assert.That(SceneLoadingSpecs.Opacity(false, 200, double.NaN), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(SceneLoadingSpecs.Opacity(false, 400, double.NaN), Is.EqualTo(1));
            Assert.That(SceneLoadingSpecs.Opacity(false, 480, 80), Is.EqualTo(0.5).Within(1e-12), "sale desde 1 en 160 ms");
            Assert.That(SceneLoadingSpecs.Opacity(false, 600, 160), Is.EqualTo(0));
            Assert.That(SceneLoadingSpecs.Opacity(false, 150, 50), Is.EqualTo(0), "lista dentro de la gracia: nunca se ve");
            Assert.That(SceneLoadingSpecs.IsVisible(false, 300, double.NaN), Is.True);
            Assert.That(SceneLoadingSpecs.IsVisible(false, 1000, 200), Is.False);
        }
    }

    // HP · La política de la háptica (It. 14, AC-POL-03, AC-DEBT-10; DT14-23; QA-G14-23).
    [TestFixture]
    public sealed class HapticPolicyTests
    {
        [Test, Category("AC-POL-03")]
        public void OneEvery250Ms()
        {
            var policy = new HapticPolicy();
            Assert.That(policy.TryPlay(HapticKind.Light, 10.0, false), Is.True);
            Assert.That(policy.TryPlay(HapticKind.Light, 10.2, false), Is.False, "a 200 ms");
            Assert.That(policy.TryPlay(HapticKind.Light, 10.25, false), Is.True, "a 250 ms");
        }

        [Test, Category("AC-POL-03")]
        public void SuccessWinsOverMedium()
        {
            var policy = new HapticPolicy();
            Assert.That(policy.TryPlay(HapticKind.Medium, 5.0, false), Is.True);
            Assert.That(policy.TryPlay(HapticKind.Success, 5.1, false), Is.True, "H-03 tapa a H-02");
            Assert.That(policy.TryPlay(HapticKind.Medium, 5.2, false), Is.False, "un Medium después de Success, dentro de la ventana");
        }

        [Test, Category("AC-POL-03")]
        public void NothingInBackground()
        {
            var policy = new HapticPolicy();
            Assert.That(policy.TryPlay(HapticKind.Success, 1, true), Is.False);
            Assert.That(policy.TryPlay(HapticKind.Success, 1, false), Is.True, "lo de segundo plano no cuenta para la ventana");
        }

        [Test, Category("AC-DEBT-10")]
        public void ReconnectConfirmInsideTheWindowIsLost()
        {
            // QA-G14-23: un Confirm de reconexión dentro de los 250 ms de un cambio de paso se pierde (aceptado).
            var policy = new HapticPolicy();
            Assert.That(policy.TryPlay(HapticKind.Medium, 2.0, false), Is.True);
            Assert.That(policy.TryPlay(HapticKind.Confirm, 2.1, false), Is.False);
            Assert.That(policy.TryPlay(HapticKind.Confirm, 2.3, false), Is.True);
        }
    }

    // TC · El reloj del aviso con acción (It. 14, #69, AC-DEBT-05; A12-04).
    [TestFixture]
    public sealed class ToastClockTests
    {
        [Test, Category("AC-DEBT-05")]
        public void PausesWithHoverOrFocusAndResumes()
        {
            var clock = new ToastClock(4000);
            Assert.That(clock.Advance(1000), Is.False);
            clock.SetHover(true);
            Assert.That(clock.IsPaused, Is.True);
            clock.Advance(10000);
            Assert.That(clock.RemainingMs, Is.EqualTo(3000), "con el puntero encima no corre");
            clock.SetFocus(true);
            clock.SetHover(false);
            clock.Advance(500);
            Assert.That(clock.RemainingMs, Is.EqualTo(3000), "con el foco tampoco");
            clock.SetFocus(false);
            Assert.That(clock.Advance(2999), Is.False, "sigue desde donde estaba");
            Assert.That(clock.Advance(1), Is.True);
            Assert.That(clock.Expired, Is.True);
            Assert.That(clock.Advance(1), Is.False, "vence una vez");
            clock.Restart();
            Assert.That(clock.RemainingMs, Is.EqualTo(4000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ToastClock(0));
        }
    }

    // PB · Cortes de la ruta en el aviso (It. 14, #69, AC-DEBT-05).
    [TestFixture]
    public sealed class PathBreakTests
    {
        [Test, Category("AC-DEBT-05")]
        public void BreaksAfterBackslashButNeverLeavesOneLetter()
        {
            var z = PathBreak.ZeroWidthSpace;
            Assert.That(PathBreak.Insert("Guardado en Documentos\\RowMate\\TCX"), Is.EqualTo("Guardado en Documentos\\" + z + "RowMate\\" + z + "TCX"));
            Assert.That(PathBreak.Insert("C:\\x"), Is.EqualTo("C:\\x"), "no deja una sola letra en el último renglón");
            Assert.That(PathBreak.Insert("a/b/cd"), Is.EqualTo("a/b/" + z + "cd"));
            Assert.That(PathBreak.Insert("Sin ruta"), Is.EqualTo("Sin ruta"));
            Assert.That(PathBreak.Strip(PathBreak.Insert("Saved to Documents\\RowMate\\TCX")), Is.EqualTo("Saved to Documents\\RowMate\\TCX"));
        }
    }
}
