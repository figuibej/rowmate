using NUnit.Framework;
using RowMate.Core.Common;

namespace RowMate.Tests.EditMode.Common
{
    // WP · La ventana se recuerda (It. 15, #74, AC-D15-19, DV-145, DV-129, DV-130; DT15-08): dp lógicos, 50 % visible en algún monitor,
    // nunca menos de 640 × 360, maximizada y pantalla completa → la ventana de antes. Más WindowPrefs con preferencias en memoria.
    [TestFixture]
    public sealed class WindowPlacementTests
    {
        private static readonly MonitorArea Main = new MonitorArea(new WindowRect(0, 0, 1920, 1040), 1.0);
        private static readonly MonitorArea HighDpi = new MonitorArea(new WindowRect(1920, 0, 2560, 1400), 1.5);

        [Test, Category("AC-D15-19")]
        public void CaptureInLogicalDp()
        {
            var saved = WindowPlacement.Capture(new WindowRect(300, 150, 1500, 900), 1.5, false, false, default);
            Assert.That(saved, Is.EqualTo(new SavedWindow(200, 100, 1000, 600, false)));
            Assert.That(WindowPlacement.ToPixels(saved, 1.5), Is.EqualTo(new WindowRect(300, 150, 1500, 900)), "ida y vuelta");
            Assert.That(WindowPlacement.Capture(new WindowRect(10, 10, 800, 600), 1.0, true, false, default).Maximized, Is.True);
        }

        [Test, Category("AC-D15-19")]
        public void FullscreenSavesTheWindowBefore()
        {
            // DV-129: la pantalla completa no se recuerda: se guarda el rectángulo de antes (WindowModeState.Restore), sin maximizar.
            var restore = new WindowRect(100, 80, 1280, 720);
            var saved = WindowPlacement.Capture(new WindowRect(0, 0, 1920, 1080), 1.0, true, true, restore);
            Assert.That(saved, Is.EqualTo(new SavedWindow(100, 80, 1280, 720, false)));
        }

        [Test, Category("AC-D15-19")]
        public void RestoresWhereItWas()
        {
            var saved = new SavedWindow(200, 100, 1000, 600, false);
            var choice = WindowPlacement.Choose(saved, new[] { Main, HighDpi });
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice!.Rect, Is.EqualTo(new WindowRect(200, 100, 1000, 600)));
            Assert.That(choice.MonitorIndex, Is.EqualTo(0));
            Assert.That(choice.Maximized, Is.False);
            var maximized = WindowPlacement.Choose(saved with { Maximized = true }, new[] { Main });
            Assert.That(maximized!.Maximized, Is.True, "maximizada: el rectángulo normal y SW_MAXIMIZE");
            Assert.That(maximized.Rect, Is.EqualTo(new WindowRect(200, 100, 1000, 600)));
        }

        [Test, Category("AC-D15-19")]
        public void HalfVisibleIsEnoughLessIsNot()
        {
            // 1000 de ancho con 500 adentro del monitor de 1920: justo el 50 %.
            Assert.That(WindowPlacement.Choose(new SavedWindow(1420, 100, 1000, 600, false), new[] { Main }), Is.Not.Null);
            Assert.That(WindowPlacement.Choose(new SavedWindow(1421, 100, 1000, 600, false), new[] { Main }), Is.Null, "49,9 %: centrada");
            Assert.That(WindowPlacement.VisibleFraction(new WindowRect(1420, 100, 1000, 600), Main.WorkArea), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(WindowPlacement.VisibleFraction(new WindowRect(5000, 5000, 10, 10), Main.WorkArea), Is.EqualTo(0));
        }

        [Test, Category("AC-D15-19")]
        public void RemovedMonitorFallsBackToCentered()
        {
            // Se cerró en el segundo monitor (px = dp × 1,5 → 2 400 a 3 900) y ese monitor ya no está: null (WindowGeometry.Initial).
            var saved = new SavedWindow(1600, 100, 1000, 600, false);
            var both = WindowPlacement.Choose(saved, new[] { Main, HighDpi });
            Assert.That(both!.MonitorIndex, Is.EqualTo(1), "con los dos monitores, el segundo (en px de su escala)");
            Assert.That(both.Rect, Is.EqualTo(new WindowRect(2400, 150, 1500, 900)));
            Assert.That(WindowPlacement.Choose(saved, new[] { Main }), Is.Null);
            Assert.That(WindowPlacement.Choose(null, new[] { Main }), Is.Null, "sin guardado");
            Assert.That(WindowPlacement.Choose(saved with { Width = 0 }, new[] { Main }), Is.Null, "inválido");
            Assert.That(WindowPlacement.Choose(saved, new MonitorArea[0]), Is.Null, "sin monitores");
        }

        [Test, Category("AC-D15-19")]
        public void NeverBelowTheMinimum()
        {
            var tiny = WindowPlacement.Choose(new SavedWindow(100, 100, 300, 200, false), new[] { Main });
            Assert.That(tiny!.Rect, Is.EqualTo(new WindowRect(100, 100, WindowGeometry.MinWidth, WindowGeometry.MinHeight)), "640 × 360, sin moverla");
            var frame = new WindowFrameEdges(8, 31, 8, 8);
            var framed = WindowPlacement.Choose(new SavedWindow(100, 100, 300, 200, false), new[] { HighDpi }, frame);
            Assert.That(framed, Is.Null, "a 1,5 el rectángulo cae a la izquierda del segundo monitor");
            var onHigh = WindowPlacement.Choose(new SavedWindow(1300, 10, 300, 200, false), new[] { HighDpi }, frame);
            Assert.That(onHigh!.Rect.Width, Is.EqualTo(960 + 16), "640 × 1,5 + el marco");
            Assert.That(onHigh.Rect.Height, Is.EqualTo(540 + 39));
        }

        [Test, Category("AC-D15-19")]
        public void PrefsRoundTripWithTheFiveKeys()
        {
            var prefs = new MemoryPreferences();
            Assert.That(WindowPrefs.Read(prefs), Is.Null, "sin claves: centrada");
            var saved = new SavedWindow(-200, 40, 1000, 600, true);
            WindowPrefs.Write(prefs, saved);
            Assert.That(WindowPrefs.Read(prefs), Is.EqualTo(saved));
            Assert.That(prefs.TryGetString(WindowPrefs.XKey, out var x) && x == "-200", Is.True, "dp enteros como texto invariante");
            Assert.That(prefs.TryGetBool(WindowPrefs.MaximizedKey, out var maximized) && maximized, Is.True);
            Assert.That(new[] { WindowPrefs.XKey, WindowPrefs.YKey, WindowPrefs.WidthKey, WindowPrefs.HeightKey, WindowPrefs.MaximizedKey },
                Is.EqualTo(new[] { "window.x", "window.y", "window.width", "window.height", "window.maximized" }));
            prefs.SetString(WindowPrefs.WidthKey, "ancho");
            Assert.That(WindowPrefs.Read(prefs), Is.Null, "un valor que no es entero: centrada");
            prefs.SetString(WindowPrefs.WidthKey, "1000");
            prefs.Remove(WindowPrefs.HeightKey);
            Assert.That(WindowPrefs.Read(prefs), Is.Null, "falta una clave: centrada");
        }
    }
}
