using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // NAV · Capas, "Atrás", menús, diálogos, avisos, ayudas y transiciones (F-APP-03; 03-rutinas/02-tech-design §4.1-§4.9).
    public sealed class NavigationLayersTests : RoutinesUiTestBase
    {
        private IEnumerator OpenDeleteDialog(int card = 0)
        {
            yield return Tap("routine-menu-" + card);
            yield return Tap("routine-menu-delete");
            Assert.That(H.TopDialog, Is.InstanceOf<DeleteRoutineDialog>());
        }

        [UnityTest, Category("AC-RUT-10-06")]
        public IEnumerator CardMenuEditAndDelete()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Tap("routine-menu-1");
            Assert.That(H.TextOf("routine-menu-edit"), Is.EqualTo("Editar"));
            Assert.That(H.TextOf("routine-menu-delete"), Is.EqualTo("Eliminar"));
            yield return Tap("routine-menu-edit");
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False);
            Assert.That(Editor.NameField.Value, Is.EqualTo("Pirámide 500"), "Editar abre el editor de esa rutina");
            yield return Tap("editor-back");
            yield return OpenDeleteDialog(1);
            Assert.That(((DeleteRoutineDialog)H.TopDialog!).Message.text, Is.EqualTo("¿Eliminar \"Pirámide 500\"?"));
            Assert.That(H.TextOf("dialog-title"), Is.EqualTo("Eliminar rutina"));
        }

        [UnityTest, Category("AC-RUT-10-06")]
        public IEnumerator MenuClosesOnOutsideEscapeAndBack()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Tap("routine-menu-0");
            H.TapAt(new Vector2(20, H.Root.worldBound.center.y));
            yield return Settle();
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False, "un toque afuera lo cierra");
            Assert.That(H.Editor, Is.Null, "y no llega a la tarjeta de atrás");
            yield return Tap("routine-menu-0");
            yield return Back(BackSource.Escape);
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False, "Escape lo cierra");
            yield return Tap("routine-menu-0");
            yield return Back(BackSource.SystemBack);
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False, "Atrás lo cierra");
            Assert.That(H.BackButton is CountingBackButtonPolicy p && p.Calls == 0, Is.True, "sin llegar a la raíz");
        }

        [UnityTest, Category("AC-RUT-10-06"), Category("AC-APP-03-06")]
        public IEnumerator MenuButtonTooltip()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            H.PointerEnter("routine-menu-0");
            H.Advance(0.2);
            yield return Frames();
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.True, "aparece apenas se apoya el puntero");
            Assert.That(H.Shell.Popups.Tooltip.text, Is.EqualTo("Mostrar menú"));
            Assert.That(H.Shell.Popups.Tooltip.pickingMode, Is.EqualTo(PickingMode.Ignore), "la ayuda no tapa toques");
            H.PointerLeave("routine-menu-0");
            H.Advance(0.05);
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.True, "sigue 0,1 s después de salir");
            H.Advance(0.1);
            yield return Settle();
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.False);
        }

        [UnityTest, Category("AC-APP-03-06")]
        public IEnumerator TooltipsOnHoverAndLongPress()
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.IsDesktop = false);
            yield return OpenEditorOf(0);
            H.PointerDown("editor-back");
            H.Advance(0.3);
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.False, "antes de 0,5 s, nada");
            H.Advance(0.3);
            yield return Frames();
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.True, "al mantener presionado 0,5 s");
            Assert.That(H.Shell.Popups.Tooltip.text, Is.EqualTo("Atrás"));
            H.PointerUp("editor-back");
            H.Advance(1.4);
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.True, "queda 1,5 s después de soltar");
            H.Advance(0.2);
            yield return Settle();
            Assert.That(H.Shell.Popups.IsTooltipVisible, Is.False);
            Assert.That(H.Editor, Is.Not.Null, "mantener presionado no activa la flecha");

            // Solo los tres: flecha, ⋮ y tacho de la serie.
            Assert.That(Editor.Back.Help, Is.Not.Null);
            Assert.That(Block(0).DeleteButton.Help, Is.Not.Null);
            Assert.That(Row(0).Edit!.Help, Is.Null, "el lápiz no lleva ayuda");
            Assert.That(Row(0).Delete!.Help, Is.Null);
        }

        [UnityTest, Category("AC-RUT-10-06"), Category("AC-APP-03-04")]
        public IEnumerator MenuItemTwiceOpensOneDialog()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Tap("routine-menu-0");
            var item = H.BoundsOf("routine-menu-delete").center;
            H.Tap("routine-menu-delete");
            H.TapAt(item);
            yield return Settle();
            Assert.That(H.Shell.Dialogs.Count, Is.EqualTo(1), "un solo diálogo (el menú que sale no responde)");
        }

        [UnityTest, Category("AC-RUT-10-07")]
        public IEnumerator DeleteDialogOnlyConfirmDeletes()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            foreach (var way in new[] { "cancelar", "velo", "Escape", "Atrás" })
            {
                yield return OpenDeleteDialog();
                switch (way)
                {
                    case "cancelar":
                        yield return Tap("dialog-cancel");
                        break;
                    case "velo":
                        H.TapScrim();
                        yield return Settle();
                        break;
                    case "Escape":
                        yield return Back(BackSource.Escape);
                        break;
                    default:
                        yield return Back(BackSource.SystemBack);
                        break;
                }

                Assert.That(H.TopDialog, Is.Null, way + " cierra");
                Assert.That(Listed.Count, Is.EqualTo(4), way + " no borra");
            }

            yield return OpenDeleteDialog();
            Assert.That(H.TextOf("dialog-confirm"), Is.EqualTo("Eliminar"));
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(3), "solo Eliminar borra");
        }

        [UnityTest, Category("AC-RUT-10-07"), Category("AC-APP-03-03")]
        public IEnumerator DialogButtonTwiceActsOnce()
        {
            CreateScripted(_ => { }, new[] { RoutineSeeds.EscaleraSpm(), RoutineSeeds.SoloCalentamiento() });
            yield return GoToRoutines();
            yield return OpenDeleteDialog();
            var confirm = H.BoundsOf("dialog-confirm").center;
            H.Tap("dialog-confirm");
            H.TapAt(confirm);
            yield return Settle();
            Assert.That(H.ScriptedRoutines!.DeleteCalls, Is.EqualTo(1), "un solo borrado");
            Assert.That(Listed.Count, Is.EqualTo(1));
        }

        [UnityTest, Category("AC-APP-03-01")]
        public IEnumerator EditorCoversTheTabBar()
        {
            CreateRoutines(RoutineSeeds.ListaPlus(10));
            yield return GoToRoutines();
            H.ScrollTo("routine-card-8");
            yield return Frames(2);
            var offset = H.ScrollOffsetOf("routines-list");
            yield return Tap("routine-card-8");
            Assert.That(H.Shell.Pages.CoversShell, Is.True);
            Assert.That(H.IsShown("tab-bar"), Is.False, "la barra queda tapada");
            Assert.That(H.WhyNotTappable("tab-routines"), Is.Not.Null, "y no se puede tocar");
            Assert.That(H.Find("tab-bar")!.focusable && H.Find("tab-bar")!.enabledInHierarchy && H.Find("tab-bar")!.resolvedStyle.display != DisplayStyle.None, Is.False,
                "fuera del recorrido de Tab");
            yield return Tap("editor-back");
            yield return Frames(2);
            Assert.That(H.IsShown("tab-bar"), Is.True, "al volver, la barra");
            Assert.That(H.ScrollOffsetOf("routines-list"), Is.EqualTo(offset).Within(0.5f), "y la lista donde estaba");
        }

        [UnityTest, Category("AC-APP-03-02")]
        public IEnumerator BackClosesOnlyTheTop()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-0");
            Assert.That(H.TopDialog, Is.Not.Null);
            yield return Back(BackSource.SystemBack);
            Assert.That(H.TopDialog, Is.Null, "primero el diálogo");
            Assert.That(H.Editor, Is.Not.Null, "el editor sigue");
            yield return Back(BackSource.SystemBack);
            Assert.That(H.Editor, Is.Null, "después el editor");
            Assert.That(((CountingBackButtonPolicy)H.BackButton).Calls, Is.EqualTo(0));
            yield return Back(BackSource.SystemBack);
            Assert.That(((CountingBackButtonPolicy)H.BackButton).Calls, Is.EqualTo(1), "en la raíz, la política del sistema");
        }

        [UnityTest, Category("AC-APP-03-02"), Category("AC-RUT-11-08")]
        public IEnumerator EscapeClosesOnlyMenusAndDialogs()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return Back(BackSource.Escape);
            Assert.That(H.Editor, Is.Not.Null, "Escape no cierra el editor (P-08)");
            yield return ScrollAndTap("series-repeat-0");
            Assert.That(H.TopDialog, Is.InstanceOf<RepeatDialog>());
            yield return Back(BackSource.Escape);
            Assert.That(H.TopDialog, Is.Null, "Escape cierra el diálogo");
            Assert.That(H.Editor, Is.Not.Null);
            Assert.That(((CountingBackButtonPolicy)H.BackButton).Calls, Is.EqualTo(0), "Escape nunca llega a la raíz");
        }

        [UnityTest, Category("AC-RUT-11-08")]
        public IEnumerator BackDiscardsWithoutAsking()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return Type("editor-name", "Cambiado");
            yield return Tap("editor-add-series");
            yield return Back(BackSource.SystemBack);
            Assert.That(H.Editor, Is.Null, "cierra sin preguntar");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Listed[0].Name, Is.EqualTo("Escalera SPM"), "lo cambiado se descarta");
            yield return Tap("routine-card-0");
            yield return Tap("editor-back");
            Assert.That(H.Editor, Is.Null, "la flecha también");
        }

        [UnityTest, Category("AC-RUT-11-08")]
        public IEnumerator EscapeDoesNotCloseTheEditor()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            Editor.NameField.FocusInput(true);
            yield return Frames();
            H.PressKey(KeyCode.Escape);
            yield return Back(BackSource.Escape);
            Assert.That(H.Editor, Is.Not.Null);
            Assert.That(Editor.NameField.Value, Is.EqualTo("Escalera SPM"));
        }

        [UnityTest, Category("AC-APP-03-02")]
        public IEnumerator EscapeClosesOneLevelAtATime()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-1");
            yield return TypeInDialog("step-min", "7");
            var field = H.Find<RmTextField>("step-min")!;
            field.Select(0, 1);
            H.RightClick("step-min");
            yield return Settle();
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.True, "el menú de texto sobre el diálogo");
            yield return Back(BackSource.Escape);
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.False, "la primera pulsación cierra el menú");
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>(), "el diálogo sigue");
            yield return Back(BackSource.Escape);
            Assert.That(H.TopDialog, Is.Null, "la segunda, el diálogo");
            Assert.That(H.Editor, Is.Not.Null, "el editor nunca con Escape");
        }

        [UnityTest, Category("AC-APP-03-01"), Category("AC-APP-03-02")]
        public IEnumerator BackSkipsWhatIsClosing()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-0");
            H.PressBack(BackSource.SystemBack);
            Assert.That(H.TopDialog, Is.Null, "el diálogo está saliendo: ya no es el de arriba");
            H.PressBack(BackSource.SystemBack);
            yield return Settle();
            Assert.That(H.Editor, Is.Null, "el segundo Atrás va al editor, no al diálogo que sale");
            Assert.That(((CountingBackButtonPolicy)H.BackButton).Calls, Is.EqualTo(0), "ni a la raíz");
        }

        [UnityTest, Category("AC-APP-03-03")]
        public IEnumerator DialogsAreModal()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            var save = H.BoundsOf("editor-save").center;
            yield return ScrollAndTap("series-repeat-0");
            Assert.That(H.WhyNotTappable("editor-save"), Is.Not.Null, "lo de atrás no se toca");
            var picked = H.TapAt(save);
            yield return Settle();
            Assert.That(picked == null || !H.Find("routine-editor")!.Contains(picked), Is.True, "el toque no llega al editor");
            Assert.That(H.Editor, Is.Not.Null, "el editor no guardó ni se cerró");
        }

        [UnityTest, Category("AC-APP-03-03")]
        public IEnumerator ScrimCancels()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("series-repeat-0");
            yield return TypeInDialog("repeat-amount", "7");
            H.TapScrim();
            yield return Settle();
            Assert.That(H.TopDialog, Is.Null, "tocar el velo cancela");
            Assert.That(Block(0).Count.Text, Is.EqualTo("×3"), "sin cambios");
        }

        [UnityTest, Category("AC-APP-03-03"), Category("AC-APP-05-07")]
        public IEnumerator TabIsTrappedInTheDialog()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return OpenDeleteDialog();
            var dialog = H.TopDialog!;
            dialog.Box.Q<Button>()?.Focus();
            for (var i = 0; i < 6; i++)
            {
                H.PressTab(i % 2 == 1);
                yield return Frames();
                var focused = H.Focused as VisualElement;
                Assert.That(focused != null && dialog.Box.Contains(focused), Is.True, "el foco no sale del diálogo");
            }
        }

        [UnityTest, Category("AC-APP-03-04")]
        public IEnumerator MenuAboveEverythingButTheSimulator()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            var root = H.Find("app-root")!;
            int Index(string name) => root.hierarchy.IndexOf(H.Find(name)!);
            Assert.That(Index("tab-content"), Is.LessThan(Index("tab-bar")));
            Assert.That(Index("tab-bar"), Is.LessThan(Index("page-stack")));
            Assert.That(Index("page-stack"), Is.LessThan(Index("dialog-layer")));
            Assert.That(Index("dialog-layer"), Is.LessThan(Index("toast-layer")));
            Assert.That(Index("toast-layer"), Is.LessThan(Index("popup-layer")), "el menú, encima de todo (salvo el simulador)");
            Assert.That(Index("popup-layer"), Is.EqualTo(root.hierarchy.childCount - 1), "sin el simulador, la capa de arriba");
            yield return Tap("routine-menu-0");
            var menu = H.Shell.Popups.Menu!;
            Assert.That(H.Find("popup-layer")!.Contains(menu), Is.True);
        }

        [UnityTest, Category("AC-APP-03-05")]
        public IEnumerator ToastFourSecondsAndReplaces()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Tap("editor-save");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("El nombre no puede estar vacío"));
            H.Advance(3.0);
            yield return Type("editor-name", "Algo");
            yield return Tap("editor-save");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Agregá al menos un paso"), "uno nuevo reemplaza al visible");
            H.Advance(3.5);
            Assert.That(H.Shell.Toasts.IsVisible, Is.True, "cuenta sus 4 s desde que apareció");
            H.Advance(0.7);
            yield return Settle();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "a los 4 s se va solo");
            var bottom = H.Root.worldBound.yMax - H.Shell.Toasts.Toast.worldBound.yMax;
            Assert.That(bottom, Is.GreaterThanOrEqualTo(0));
        }

        [UnityTest, Category("AC-APP-03-08")]
        public IEnumerator TransitionsFollowTheBible()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Frames();
            H.Tap("routines-add");
            Assert.That(H.Shell.Pages.IsTransitioning, Is.True, "la página entra animada");
            H.Advance((MotionSpecs.PageInMs - 60) / 1000.0);
            Assert.That(H.Shell.Pages.IsTransitioning, Is.True);
            H.Advance(0.1);
            yield return Frames();
            Assert.That(H.Shell.Pages.IsTransitioning, Is.False, "240 ms (ANIMATION_BIBLE)");
            H.Tap("editor-add-series");
            yield return Settle();
            yield return ScrollAndTap("series-repeat-0");
            yield return Back(BackSource.SystemBack);
            Assert.That(H.Shell.Dialogs.IsTransitioning, Is.False);
            yield return Frames();
            H.Tap("editor-back");
            Assert.That(H.Shell.Pages.IsTransitioning, Is.True, "y sale animada");
            H.Advance((MotionSpecs.PageOutMs + 20) / 1000.0);
            yield return Frames();
            Assert.That(H.Shell.Pages.IsTransitioning, Is.False, "200 ms");
            Assert.That(H.Editor, Is.Null);
        }

        [UnityTest, Category("AC-APP-03-08"), Category("AC-APP-05-08")]
        public IEnumerator ReducedMotionFades()
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.ReduceMotion = true);
            yield return GoToRoutines();
            yield return Frames();
            H.Tap("routines-add");
            H.Advance(0.06);
            yield return Frames();
            var page = H.Find("routine-editor")!;
            Assert.That(page.resolvedStyle.translate.x, Is.EqualTo(0).Within(0.01f), "sin desplazamiento");
            Assert.That(page.resolvedStyle.translate.y, Is.EqualTo(0).Within(0.01f));
            H.Advance(MotionSpecs.ReducedPageFadeMs / 1000.0);
            yield return Frames();
            Assert.That(H.Shell.Pages.IsTransitioning, Is.False, "solo un fundido de 120 ms");
            Assert.That(page.resolvedStyle.opacity, Is.EqualTo(1).Within(0.01f));
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator TabOrderAndActivation()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            Screen.List.CardAt(0)!.Focus();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Screen.List.CardAt(0)), "las tarjetas reciben el foco");
            H.PressEnter();
            yield return Settle();
            Assert.That(H.Editor, Is.Not.Null, "Enter activa la tarjeta");
            Assert.That(Editor.NameField.Value, Is.EqualTo("Escalera SPM"));
            yield return Tap("editor-back");
            Screen.List.CardAt(1)!.Focus();
            yield return Frames();
            H.PressKey(KeyCode.Space, ' ');
            yield return Settle();
            Assert.That(H.Editor, Is.Not.Null, "Espacio también");
            Assert.That(Editor.NameField.Value, Is.EqualTo("Pirámide 500"));
            Assert.That(Editor.Back.focusable && Editor.Save.focusable, Is.True, "la flecha y Guardar están en el recorrido");
        }

        [UnityTest, Category("AC-APP-05-07"), Category("AC-RUT-13-13"), Category("AC-RUT-13-15")]
        public IEnumerator TabScrollsTheFocusedIntoView()
        {
            CreateRoutines(RoutineSeeds.Sixty, DeviceProfile.L640);
            yield return OpenEditorOf(0);
            yield return Frames(2);
            Editor.NameField.FocusInput(true);
            yield return Frames();
            var viewport = Editor.Scroll.contentViewport.worldBound;
            for (var i = 0; i < 40; i++)
            {
                H.PressTab();
                yield return Frames();
                H.Settle();
                yield return Frames();
            }

            var focused = H.Focused as VisualElement;
            Assert.That(focused, Is.Not.Null);
            Assert.That(Editor.Scroll.scrollOffset.y, Is.GreaterThan(100), "el contenido se desplazó");
            Assert.That(focused!.worldBound.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1f), "el elemento con foco queda a la vista");
            Assert.That(focused.worldBound.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1f));
        }
    }
}
