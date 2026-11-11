using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Platform.Display;
using UnityEngine.InputSystem;

namespace RowMate.Tests.UnityEditMode
{
    // It. 15 (#81; AC-D15-30; 15-deuda-pc/02-tech-design §6; QA-V15-02): WindowMode de ItFourteenGapTests. Va en el EditMode de Unity y no en
    // PlayMode porque AsmdefLintTests solo deja referenciar RowMate.Platform (WindowModeController) desde Bootstrap, Editor y este assembly.
    // A diferencia de WindowModeTests (que llama a OnKey), acá las teclas pasan por el Input System (InputTestFixture con un teclado de
    // prueba) y por WindowModeController.Update, el mismo camino del player: F11 y Alt+Enter, ida y vuelta al rectángulo de antes; Enter
    // solo y Esc no cambian la ventana.
    public sealed class ItFourteenGapTests
    {
        private sealed class FakeFrame : IWindowFrame
        {
            public WindowRect Rect = new WindowRect(140, 90, 1280, 720);
            public readonly List<string> Calls = new List<string>();

            public WindowRect Current => Rect;

            public void EnterFullscreen()
            {
                Calls.Add("entrar");
                Rect = new WindowRect(0, 0, 1920, 1080);
            }

            public void ExitFullscreen(WindowRect restore)
            {
                Calls.Add("salir " + restore);
                Rect = restore;
            }

            public void Tick()
            {
            }
        }

        [Test, Category("AC-D15-30"), Category("AC-APP-15-07")]
        public void WindowMode()
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                var frame = new FakeFrame();
                var controller = new WindowModeController(frame);
                var before = frame.Rect;

                // F11 → pantalla completa; F11 → vuelve.
                input.Press(keyboard.f11Key);
                controller.Update();
                input.Release(keyboard.f11Key);
                controller.Update();
                Assert.That(controller.State.IsFullscreen, Is.True, "F11 entra");
                input.Press(keyboard.f11Key);
                controller.Update();
                input.Release(keyboard.f11Key);
                controller.Update();
                Assert.That(controller.State.IsFullscreen, Is.False, "F11 sale");
                Assert.That(frame.Rect, Is.EqualTo(before), "al rectángulo de antes");

                // Enter solo y Esc: nada.
                input.Press(keyboard.enterKey);
                controller.Update();
                input.Release(keyboard.enterKey);
                input.Press(keyboard.escapeKey);
                controller.Update();
                input.Release(keyboard.escapeKey);
                controller.Update();
                Assert.That(controller.State.IsFullscreen, Is.False, "Enter sin Alt y Esc no cambian la ventana");

                // Alt+Enter → pantalla completa; Alt+Enter → vuelve.
                input.Press(keyboard.leftAltKey);
                controller.Update();
                input.Press(keyboard.enterKey);
                controller.Update();
                input.Release(keyboard.enterKey);
                controller.Update();
                Assert.That(controller.State.IsFullscreen, Is.True, "Alt+Enter entra");
                input.Press(keyboard.enterKey);
                controller.Update();
                input.Release(keyboard.enterKey);
                input.Release(keyboard.leftAltKey);
                controller.Update();
                Assert.That(controller.State.IsFullscreen, Is.False, "Alt+Enter sale");
                Assert.That(frame.Rect, Is.EqualTo(before));
                Assert.That(frame.Calls, Is.EqualTo(new[] { "entrar", "salir " + before, "entrar", "salir " + before }));
            }
            finally
            {
                input.TearDown();
            }
        }
    }
}
