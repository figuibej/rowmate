using System;
using RowMate.Core.Design;
using RowMate.Presentation;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;
using UnityEngine.UIElements;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Bootstrap
{
    // Arma la UI de la app sobre cualquier VisualElement raíz (02-tech-design §0.2, §1.4 de la It. 2): lo usan IGUAL el AppRoot
    // de la escena Boot, las pruebas PlayMode y el arnés de capturas (UiTestHost). Las pantallas nunca saben si los servicios
    // son simulados, reales o de prueba (R-06, AC-BLE-13-04).
    // La raíz del UIDocument se estira con position: absolute y los cuatro lados en 0 en los dos modos (en Play la raíz no se
    // estira sola, R2-09). Desde la It. 3, "Atrás" recorre menú → diálogo → página → raíz (BackNavigation).
    public static class ShellHost
    {
        public static ShellHandle Build(AppContext app, VisualElement root, ShellHostOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            var pacing = new FramePacingPolicy();
            var motion = new MotionDriver(options.Clock, pacing, new MotionSettings(), app.Logger);
            // It. 6a (QA-V6a-REN-06): el toque, el arrastre y la rueda, para el tramo de la escena en reposo (MotionDriver.UserInputActive).
            root.RegisterCallback<PointerDownEvent>(_ => motion.NoteUserInput(true), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(_ => motion.NoteUserInput(false), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(_ => motion.NoteUserInput(false), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pressedButtons != 0)
                {
                    motion.NoteUserInput();
                }
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<WheelEvent>(_ => motion.NoteUserInput(), TrickleDown.TrickleDown);
            var fade = VerticalFadeTexture.Create();
            var ui = new UiContext(app, motion, fade, options.Display, options.IsDesktop, options.Clipboard, options.Keyboard)
            {
                GammaPanel = options.Pipeline == UiColorPipeline.GammaRt,
                BoatZoneOutline = options.BoatZoneOutline,
                ReserveWrench = options.TopLayer != null,
                Scene = options.Scene
            };
            if (options.Thumbnails != null)
            {
                for (var i = 0; i < ui.Thumbnails.Length && i < options.Thumbnails.Length; i++)
                {
                    ui.Thumbnails[i] = options.Thumbnails[i];
                }
            }

            var shell = new AppShellView(ui, options.TopLayer);
            // It. 14 (DT14-10, DT14-11): la señal de "shell listo" siempre (la mide -rowmate-perf-boot) y la capa de arranque si se pide,
            // como última hija del shell.
            var ready = new ShellReadySignal(shell.Tabs);
            if (options.BootSplash)
            {
                shell.AttachBootSplash(new BootSplashView(ui, ready, options.BootMark));
            }

            root.Add(shell);
            return new ShellHandle(shell, motion, pacing, new BackNavigation(options.BackButton, shell.Popups, shell.Dialogs, shell.Pages), fade, ready);
        }
    }
}
