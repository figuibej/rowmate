using NUnit.Framework;
using RowMate.App.Device;

namespace RowMate.Tests.EditMode.Device
{
    // DRG: el acceso al diagnóstico oculto (AC-BLE-12-01; device_screen.dart:52-74, :124-134; Q-37, DV-104).
    [TestFixture]
    public sealed class DebugRevealGestureTests
    {
        [Test, Category("AC-BLE-12-01")]
        public void ThreeDragsOver20()
        {
            // ds:63-74: cada vez que la posición pasa 20 por debajo de la marca, cuenta una; subir más de 20 solo mueve la marca.
            var g = new DebugRevealGesture();
            g.OnScroll(20);
            Assert.That(g.ScrollCount, Is.EqualTo(0), "20 justos no alcanzan (> 20)");
            g.OnScroll(21);
            g.OnScroll(42);
            Assert.That(g.ScrollCount, Is.EqualTo(2));
            Assert.That(g.ShowButton, Is.False);
            g.OnScroll(10);
            g.OnScroll(25);
            Assert.That(g.ScrollCount, Is.EqualTo(2), "subió: la marca pasó a 10; bajar 15 no cuenta");
            g.OnScroll(31);
            Assert.That(g.ScrollCount, Is.EqualTo(3));
            Assert.That(g.ShowButton, Is.True, "Show BLE debug");
            g.OpenPanel();
            Assert.That(g.PanelOpen, Is.True);
            Assert.That(g.ShowButton, Is.False, "con el panel abierto, el botón se va");
        }

        [Test, Category("AC-BLE-12-01")]
        public void LongPressOpensPanel()
        {
            // DV-104: mantener presionada la cabecera ≥ 0,5 s abre el panel directo; soltar antes no.
            var g = new DebugRevealGesture();
            g.PressStart(10);
            Assert.That(g.PressProgress(10.25), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(g.PressEnd(10.4), Is.False);
            Assert.That(g.PanelOpen, Is.False);
            Assert.That(g.PressProgress(10.5), Is.EqualTo(0), "soltó");
            g.PressStart(20);
            Assert.That(g.PressUpdate(20.499), Is.False);
            Assert.That(g.PressUpdate(20.5), Is.True);
            Assert.That(g.PanelOpen, Is.True);
            Assert.That(g.PressEnd(21), Is.False, "ya estaba abierto");
        }

        [Test, Category("AC-BLE-12-01")]
        public void PanelResetsOnReconnect()
        {
            // El panel sigue abierto mientras dura la conexión; al reconectar hay que repetir el gesto (la vista llama Reset al
            // salir de la vista conectada).
            var g = new DebugRevealGesture();
            g.PressStart(0);
            g.PressUpdate(1);
            Assert.That(g.PanelOpen, Is.True);
            g.Reset();
            Assert.That(g.PanelOpen, Is.False);
            Assert.That(g.ScrollCount, Is.EqualTo(0));
            Assert.That(g.ShowButton, Is.False);
            g.OnScroll(21);
            Assert.That(g.ScrollCount, Is.EqualTo(1), "la marca vuelve a 0");
        }
    }
}
