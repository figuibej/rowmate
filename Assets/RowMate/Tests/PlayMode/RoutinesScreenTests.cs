using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Placeholder;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // RLT · Pantalla Rutinas (F-RUT-10, F-RUT-09 en pantalla; 03-rutinas/02-tech-design §4.3, §6.3).
    public sealed class RoutinesScreenTests : RoutinesUiTestBase
    {
        private static readonly string[] ListaOrder = { "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" };

        [UnityTest, Category("AC-RUT-10-01")]
        public IEnumerator TabTitleWithoutActionsAndFab()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return GoToRoutines();
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(2), "Rutinas es el destino 2");
            Assert.That(Screen.AppBar.Title.text, Is.EqualTo("Rutinas"));
            Assert.That(Screen.AppBar.Actions.childCount, Is.EqualTo(0), "la barra superior no tiene acciones");
            Assert.That(H.IsShown("routines-add"), Is.True);
            Assert.That(Screen.AddButton.Icon.Codepoint, Is.EqualTo(Icons.Add));
            Assert.That(H.WhyNotTappable("routines-add"), Is.Null, "el + se toca entero");
            var fab = H.BoundsOf("routines-add");
            Assert.That(fab.width, Is.EqualTo(Fab.SizeDp).Within(0.5f));
            Assert.That(H.Root.worldBound.xMax - fab.xMax, Is.EqualTo(Fab.RightDp).Within(0.6f), "a 16 del borde derecho");
            Assert.That(H.BoundsOf("tab-bar").yMin - fab.yMax, Is.EqualTo(Fab.GapAboveTabBarDp).Within(0.6f), "16 sobre la barra");
        }

        [UnityTest, Category("AC-RUT-10-02")]
        public IEnumerator LoadingShowsOnlyTheIndicator()
        {
            // La gracia corre desde que empieza la carga (al arrancar), se vea o no la pestaña: se mide sin cambiar de pestaña
            // (el cambio asienta su transición y gastaría reloj).
            CreateScripted(r => r.LoadDelay = ScriptedRoutineRepository.Never);
            var loading = H.Find("routines-loading")!;
            Assert.That(loading.style.display.value, Is.EqualTo(DisplayStyle.None), "al empezar, nada");
            H.Advance(0.1);
            Assert.That(loading.style.display.value, Is.EqualTo(DisplayStyle.None), "a los 100 ms todavía nada (gracia de 120)");
            H.Advance(0.2);
            Assert.That(loading.style.display.value, Is.EqualTo(DisplayStyle.Flex), "a los 300 ms, el indicador");
            yield return GoToRoutines();
            Assert.That(loading.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(loading.resolvedStyle.opacity, Is.EqualTo(1).Within(0.01), "entero (gracia 120 + fundido 160)");
            Assert.That(Screen.Spinner.SizeInDp, Is.EqualTo(32), "indicador de 32 (DN3-15)");
            Assert.That(Screen.Spinner.StrokeInDp, Is.EqualTo(3));
            Assert.That(UiQuery.VisibleTexts(H.Find("routines-body")!), Is.Empty, "ningún texto mientras carga");
            Assert.That(H.IsShown("routines-add"), Is.True, "el + se ve en los tres estados");
        }

        [UnityTest, Category("AC-RUT-10-02")]
        public IEnumerator FastLoadNeverShowsTheIndicator()
        {
            CreateScripted(r => r.LoadDelay = TimeSpan.FromMilliseconds(20));
            for (var i = 0; i < 30; i++)
            {
                H.Advance(0.01);
                Assert.That(H.Find("routines-loading")!.style.display.value, Is.EqualTo(DisplayStyle.None), "una carga de 20 ms no deja un parpadeo");
            }

            yield return GoToRoutines();
            Assert.That(H.IsShown("routines-empty"), Is.True);
        }

        [UnityTest, Category("AC-RUT-10-02")]
        public IEnumerator EmptyStateTextsAndColors()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return GoToRoutines();
            Assert.That(H.IsShown("routines-empty"), Is.True);
            Assert.That(H.IsTextVisible("No hay rutinas todavía"), Is.True);
            Assert.That(H.IsTextVisible("Presioná + para crear una"), Is.True);
            var empty = H.Find("routines-empty")!;
            var icon = empty.Q<IconView>();
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.FitnessCenter));
            Assert.That(icon.resolvedStyle.width, Is.EqualTo(56).Within(0.5f));
            var title = empty.Q<Label>(className: "rm-empty__title");
            var hint = empty.Q<Label>(className: "rm-empty__hint");
            AssertColor(title.resolvedStyle.color, Tokens.TextSecondary, "título en text.secondary (DV-11)");
            AssertColor(hint.resolvedStyle.color, Tokens.TextTertiary, "ayuda en text.tertiary");
            Assert.That(hint.worldBound.yMin, Is.GreaterThan(title.worldBound.yMax), "la ayuda va debajo del título");
            Assert.That(title.worldBound.yMin, Is.GreaterThan(icon.worldBound.yMax), "el título va debajo del ícono");
            AssertClean(H);
        }

        [UnityTest, Category("AC-RUT-10-03"), Category("AC-RUT-10-10")]
        public IEnumerator CardContent()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            Assert.That(Screen.List.Count, Is.EqualTo(4));
            for (var i = 0; i < ListaOrder.Length; i++)
            {
                var card = Screen.List.CardAt(i)!;
                Assert.That(card.name, Is.EqualTo("routine-card-" + i));
                Assert.That(card.NameLabel.text, Is.EqualTo(ListaOrder[i]), "orden de AC-RUT-08-05 (la más nueva arriba)");
                Assert.That(card.MenuButton.name, Is.EqualTo("routine-menu-" + i));
                Assert.That(card.MenuButton.Icon.Codepoint, Is.EqualTo(Icons.MoreVert));
                AssertColor(card.resolvedStyle.backgroundColor, Tokens.SurfaceCard, "tarjeta surface.card");
            }

            Assert.That(Screen.List.CardAt(1)!.DescriptionLabel.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Screen.List.CardAt(1)!.DescriptionLabel.text, Is.EqualTo("Bajar 2 s el split en cada vuelta"));
            Assert.That(Screen.List.CardAt(0)!.DescriptionLabel.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "sin descripción, no se muestra");
            Assert.That(Screen.List.CardAt(0)!.SummaryLabel.text, Is.EqualTo("1 serie · 1 descanso"));
            Assert.That(Screen.List.CardAt(1)!.SummaryLabel.text, Is.EqualTo("2 series · 1 descanso"));
            Assert.That(Screen.List.CardAt(2)!.SummaryLabel.text, Is.EqualTo("1 paso"));
            Assert.That(Screen.List.CardAt(3)!.SummaryLabel.text, Is.EqualTo("4 series · 3 descansos"));
            var icon = Screen.List.CardAt(0)!.Q<IconView>();
            Assert.That(icon.Codepoint, Is.EqualTo(Icons.FitnessCenter));
            AssertColor(icon.resolvedStyle.color, Tokens.AccentPrimary, "ícono accent.primary");
            AssertClean(H);
        }

        [UnityTest, Category("AC-RUT-10-10")]
        public IEnumerator SummariesOnScreen()
        {
            CreateWith(new[] { RoutineSeeds.IntervalosCuatro(), RoutineSeeds.SoloCalentamiento(), RoutineSeeds.PiramideQuinientos(), RoutineSeeds.EscaleraSpm(), RoutineSeeds.CasosBorde() },
                o => o.LanguageTags = new[] { "en-US" });
            yield return GoToRoutines();
            Assert.That(Screen.AppBar.Title.text, Is.EqualTo("Routines"));
            Assert.That(Screen.List.CardAt(0)!.SummaryLabel.text, Is.EqualTo("7 intervals · 2 rests"), "Casos borde (la más nueva)");
            Assert.That(Screen.List.CardAt(1)!.SummaryLabel.text, Is.EqualTo("1 interval · 1 rest"));
            Assert.That(Screen.List.CardAt(2)!.SummaryLabel.text, Is.EqualTo("2 intervals · 1 rest"));
            Assert.That(Screen.List.CardAt(3)!.SummaryLabel.text, Is.EqualTo("1 step"));
            Assert.That(Screen.List.CardAt(4)!.SummaryLabel.text, Is.EqualTo("4 intervals · 3 rests"));
            H.SetLanguage("es-AR");
            yield return Settle();
            Assert.That(Screen.List.CardAt(0)!.SummaryLabel.text, Is.EqualTo("7 series · 2 descansos"));
            Assert.That(Screen.AppBar.Title.text, Is.EqualTo("Rutinas"));
        }

        [UnityTest, Category("AC-RUT-10-03"), Category("AC-RUT-10-07")]
        public IEnumerator UserTextIsShownAsWritten()
        {
            // DN3-22, QA-G3-10: "<b>A</b>" no se pone en negrita y "Serie\n2" (la barra y la n escritas a mano) no salta.
            var markup = RoutineSeeds.Make("<b>A</b>", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 60)) with { Description = "Serie\\n2" };
            CreateWith(new[] { markup });
            yield return GoToRoutines();
            var card = Screen.List.CardAt(0)!;
            Assert.That(card.NameLabel.text, Is.EqualTo("<b>A</b>"));
            Assert.That(card.NameLabel.enableRichText, Is.False);
            Assert.That(card.NameLabel.parseEscapeSequences, Is.False);
            var literal = card.NameLabel.MeasureTextSize("<b>A</b>", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            var single = card.NameLabel.MeasureTextSize("A", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.That(literal, Is.GreaterThan(single * 3), "se dibujan las marcas, no una A en negrita");
            Assert.That(card.DescriptionLabel.text, Is.EqualTo("Serie\\n2"));
            Assert.That(card.DescriptionLabel.parseEscapeSequences, Is.False);
            Assert.That(card.DescriptionLabel.layout.height, Is.LessThan(LineHeightOf(card.DescriptionLabel) * 1.6f), "la barra y la n no saltan de línea");

            yield return Tap("routine-menu-0");
            yield return Tap("routine-menu-delete");
            var dialog = (DeleteRoutineDialog)H.TopDialog!;
            Assert.That(dialog.Message.text, Is.EqualTo("¿Eliminar \"<b>A</b>\"?"), "el nombre tal cual, con comillas rectas");
            Assert.That(dialog.Message.enableRichText, Is.False);
            Assert.That(dialog.Message.parseEscapeSequences, Is.False);
            yield return Tap("dialog-cancel");

            yield return Tap("routine-card-0");
            Assert.That(Editor.NameField.Value, Is.EqualTo("<b>A</b>"));
            Assert.That(Editor.DescriptionField.Value, Is.EqualTo("Serie\\n2"));
            var inner = Editor.NameField.Input.Q(TextField.textInputUssName)!.Q<TextElement>();
            Assert.That(inner.enableRichText, Is.False, "el campo tampoco interpreta marcas");
            Assert.That(inner.parseEscapeSequences, Is.False);
        }

        [UnityTest, Category("AC-RUT-10-03")]
        public IEnumerator LongNameAndDescriptionWrap()
        {
            var words = "Rutina larga de prueba con muchas palabras ";
            var name = (words + words + words + words + words + words).Substring(0, 200);
            CreateWith(new[] { RoutineSeeds.Make(name, RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 60)) with { Description = name } });
            yield return GoToRoutines();
            yield return Frames(2);
            var card = Screen.List.CardAt(0)!;
            var column = card.Q(className: "rm-routine-card__text");
            Assert.That(card.NameLabel.worldBound.xMax, Is.LessThanOrEqualTo(column.worldBound.xMax + 0.5f), "el nombre no sale de su columna");
            Assert.That(card.NameLabel.layout.height, Is.GreaterThan(LineHeightOf(card.NameLabel) * 2), "el nombre pasa a varias líneas (nunca …)");
            Assert.That(card.DescriptionLabel.layout.height, Is.GreaterThan(LineHeightOf(card.DescriptionLabel) * 2), "la descripción también");
            Assert.That(card.MenuButton.worldBound.xMax, Is.LessThanOrEqualTo(card.worldBound.xMax + 0.5f), "el ⋮ queda adentro");
            AssertClean(H);
        }

        [UnityTest, Category("AC-RUT-10-03")]
        public IEnumerator LongWordWithoutSpacesWraps()
        {
            // QA-G3-13 f: una sola palabra de 200 caracteres sin espacios se parte donde haga falta (S-B3a, punto 12).
            var word = new string('W', 200);
            CreateWith(new[] { RoutineSeeds.Make(word, RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 60)) });
            yield return GoToRoutines();
            yield return Frames(2);
            var card = Screen.List.CardAt(0)!;
            var column = card.Q(className: "rm-routine-card__text");
            Assert.That(DisplayText.Strip(card.NameLabel.text), Is.EqualTo(word), "el dato no cambia");
            Assert.That(card.NameLabel.worldBound.xMax, Is.LessThanOrEqualTo(column.worldBound.xMax + 0.5f), "no sale de la columna");
            Assert.That(card.NameLabel.layout.height, Is.GreaterThan(LineHeightOf(card.NameLabel) * 2), "se parte en varias líneas");
            AssertClean(H);
        }

        [UnityTest, Category("AC-RUT-10-11")]
        public IEnumerator StructureBarStaysInOneLine()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Frames(3);
            var expected = new[] { 8, 14, 1, 7 };
            for (var i = 0; i < expected.Length; i++)
            {
                var bar = Screen.List.CardAt(i)!.Bar;
                Assert.That(bar.SegmentCount, Is.EqualTo(expected[i]), ListaOrder[i]);
                Assert.That(bar.Overflow, Is.False);
                Assert.That(bar.layout.height, Is.LessThanOrEqualTo(8f), "un solo renglón");
            }

            var escalera = Screen.List.CardAt(0)!.Bar;
            Assert.That(escalera.SegmentTypes, Is.EqualTo(new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Work, StepType.Rest, StepType.Work, StepType.Rest, StepType.Cooldown }));
        }

        [UnityTest, Category("AC-RUT-10-11")]
        public IEnumerator ImpossibleRoutinesLoadAndRespond()
        {
            CreateWith(new[] { RoutineSeeds.RaraHuge.Routines[0], RoutineSeeds.RaraZero.Routines[0] with { CreatedAt = RoutineSeeds.Day(25) } });
            yield return GoToRoutines();
            yield return Frames(3);
            var zero = Screen.List.CardAt(0)!;
            var huge = Screen.List.CardAt(1)!;
            Assert.That(zero.NameLabel.text, Is.EqualTo("Serie vacía"));
            Assert.That(huge.NameLabel.text, Is.EqualTo("Serie enorme"));
            Assert.That(huge.Bar.Overflow, Is.True, "×2 000 000 000: desborde que se apaga");
            Assert.That(huge.Bar.SegmentCount, Is.GreaterThan(0));
            Assert.That(huge.Bar.Total, Is.GreaterThan(1000000000L));
            Assert.That(zero.Bar.EmptyTrack, Is.True, "×0: la pista vacía");
            Assert.That(zero.Bar.SegmentCount, Is.EqualTo(0));
            yield return Tap(huge.name);
            Assert.That(H.Editor, Is.Not.Null, "la app sigue respondiendo");
        }

        [UnityTest, Category("AC-RUT-10-04")]
        public IEnumerator FabOpensANewEditor()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            Assert.That(Editor.Store.IsNew, Is.True);
            Assert.That(H.IsTextVisible("Nueva rutina"), Is.True);
        }

        [UnityTest, Category("AC-RUT-10-05")]
        public IEnumerator TapCardOpensItsEditor()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(1);
            Assert.That(Editor.Store.IsNew, Is.False);
            Assert.That(Editor.NameField.Value, Is.EqualTo("Pirámide 500"));
            Assert.That(Editor.DescriptionField.Value, Is.EqualTo("Bajar 2 s el split en cada vuelta"));
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (5)"));
        }

        [UnityTest, Category("AC-RUT-10-06")]
        public IEnumerator MenuButtonDoesNotOpenTheEditor()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Tap("routine-menu-0");
            Assert.That(H.Editor, Is.Null, "el ⋮ no abre el editor");
            Assert.That(H.Shell.Popups.HasOpenMenu, Is.True);
            Assert.That(H.IsShown("routine-menu-edit"), Is.True);
            Assert.That(H.IsShown("routine-menu-delete"), Is.True);
        }

        [UnityTest, Category("AC-RUT-10-08")]
        public IEnumerator NoOtherActions()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            Assert.That(Screen.AppBar.Actions.childCount, Is.EqualTo(0), "sin búsqueda ni orden");
            var card = Screen.List.CardAt(0)!;
            Assert.That(card.Query<Button>().ToList().Count, Is.EqualTo(1), "la tarjeta solo tiene el ⋮ (sin deslizar, duplicar ni deshacer)");
            yield return Tap("routine-menu-0");
            Assert.That(H.Shell.Popups.Menu!.Items.Count, Is.EqualTo(2), "Editar y Eliminar, nada más");
        }

        [UnityTest, Category("AC-RUT-10-09")]
        public IEnumerator ThirtyRoutinesScrollAboveTheFab()
        {
            CreateRoutines(RoutineSeeds.ListaPlus(26), null, o => o.IsDesktop = false);
            yield return GoToRoutines();
            H.ScrollToEnd("routines-list");
            yield return Frames(3);
            var last = Screen.List.CardAt(29)!;
            Assert.That(last.NameLabel.text, Is.EqualTo("Intervalos 4×"), "la más vieja queda abajo");
            var fab = H.BoundsOf("routines-add");
            Assert.That(last.worldBound.yMax, Is.LessThanOrEqualTo(fab.yMin - 16 + 1), "la última tarjeta termina por encima del +");
            Assert.That(H.ScrollOffsetOf("routines-list"), Is.GreaterThan(100));
        }

        [UnityTest, Category("AC-RUT-10-09")]
        public IEnumerator ScrollPositionSurvivesEditorAndTabs()
        {
            CreateRoutines(RoutineSeeds.ListaPlus(26));
            yield return GoToRoutines();
            H.ScrollToEnd("routines-list");
            yield return Frames(3);
            var offset = H.ScrollOffsetOf("routines-list");
            Assert.That(offset, Is.GreaterThan(100));
            yield return Tap(Screen.List.CardAt(27)!.name);
            Assert.That(H.Editor, Is.Not.Null);
            yield return Tap("editor-back");
            yield return Frames(2);
            Assert.That(H.ScrollOffsetOf("routines-list"), Is.EqualTo(offset).Within(0.5f), "al cerrar el editor, la lista donde estaba");
            yield return Tap("tab-device");
            yield return Tap("tab-routines");
            yield return Frames(2);
            Assert.That(H.ScrollOffsetOf("routines-list"), Is.EqualTo(offset).Within(0.5f), "al cambiar de pestaña también");
        }

        [UnityTest, Category("AC-APP-05-11")]
        public IEnumerator NoScrollbarOnPhone()
        {
            CreateRoutines(RoutineSeeds.ListaPlus(26), null, o => o.IsDesktop = false);
            yield return GoToRoutines();
            var list = H.Find<ScrollView>("routines-list")!;
            Assert.That(list.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden), "teléfono: sin barra fija (QA-V2-13)");
            Assert.That(list.horizontalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden));
        }

        [UnityTest, Category("AC-RUT-09-04")]
        public IEnumerator LoadErrorAndRetry()
        {
            CreateScripted(r => r.FailLoad = true);
            yield return GoToRoutines();
            Assert.That(H.IsShown("routines-error"), Is.True);
            Assert.That(H.IsTextVisible("No se pudieron cargar las rutinas"), Is.True);
            Assert.That(H.TextOf("routines-retry"), Is.EqualTo("Reintentar"));
            Assert.That(H.Find("routines-loading")!.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "nunca un indicador eterno (Q-13)");
            Assert.That(H.IsShown("routines-add"), Is.True);
            Assert.That(H.Logger.Lines, Has.Some.Contains("carga de rutinas"));
            H.ScriptedRoutines!.FailLoad = false;
            H.ScriptedRoutines.InsertAsync(RoutineSeeds.EscaleraSpm());
            yield return Tap("routines-retry");
            yield return Settle();
            Assert.That(H.IsShown("routines-error"), Is.False);
            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Escalera SPM"));
        }

        [UnityTest, Category("AC-RUT-09-04")]
        public IEnumerator DeviceTabStillWorksWithLoadError()
        {
            CreateScripted(r => r.FailLoad = true);
            yield return GoToRoutines();
            Assert.That(H.IsShown("routines-error"), Is.True);
            yield return Tap("tab-device");
            Assert.That(H.IsShown("screen-device"), Is.True, "el resto de la app sigue funcionando");
        }

        [UnityTest, Category("AC-RUT-08-08"), Category("AC-RUT-01-05")]
        public IEnumerator UnknownTypeShowsTheLoadError()
        {
            CreateRoutines(RoutineSeeds.RaraSprint);
            yield return GoToRoutines();
            Assert.That(H.IsShown("routines-error"), Is.True, "tipo sprint: error de carga, no un indicador eterno (P-07)");
            Assert.That(H.App.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.LoadFailed));
        }

        [UnityTest, Category("AC-HIS-17-05")]
        public IEnumerator UnreadableBaseShowsTheError()
        {
            Create(new UiHostOptions { Routines = HostRoutines.Rota() });
            var path = H.Routines.FilePath!;
            var before = File.ReadAllBytes(path);
            yield return GoToRoutines();
            Assert.That(H.IsShown("routines-error"), Is.True);
            yield return Tap("routines-retry");
            Assert.That(H.IsShown("routines-error"), Is.True, "sigue ilegible");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before), "el archivo no se toca");
        }

        [UnityTest, Category("AC-RUT-09-01")]
        public IEnumerator SecondVisitDoesNotShowTheIndicator()
        {
            CreateScripted(r => r.LoadDelay = TimeSpan.FromSeconds(2), new[] { RoutineSeeds.EscaleraSpm() });
            H.Advance(2.1);
            yield return GoToRoutines();
            Assert.That(Screen.List.CardAt(0), Is.Not.Null, "la carga del arranque ya terminó");
            yield return Tap("tab-device");
            yield return Tap("tab-routines");
            Assert.That(H.Find("routines-loading")!.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "la segunda visita no muestra el indicador");
            Assert.That(H.ScriptedRoutines!.LoadCalls, Is.EqualTo(1), "cambiar de pestaña no vuelve a leer la base");
        }

        [UnityTest, Category("AC-APP-01-09")]
        public IEnumerator ListIsReadyBeforeTheFirstVisit()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return Frames();
            Assert.That(H.App.Routines.State.Value.Phase, Is.EqualTo(RoutinesPhase.Ready), "se cargó al arrancar, en Dispositivo");
            Assert.That(H.IsShown("screen-device"), Is.True);
            yield return GoToRoutines();
            Assert.That(H.Find("routines-loading")!.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "sin indicador");
            Assert.That(Screen.List.Count, Is.EqualTo(4));
        }

        [UnityTest, Category("AC-RUT-09-02")]
        public IEnumerator CreateEditDeleteInPlace()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Nueva");
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            Assert.That(H.Editor, Is.Null, "guardar cierra el editor");
            Assert.That(Listed[0].Name, Is.EqualTo("Nueva"), "el alta va primera");
            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Nueva"));
            yield return Tap("routine-card-3");
            yield return Type("editor-name", "Solo calentamiento 2");
            yield return Tap("editor-save");
            Assert.That(Listed[3].Name, Is.EqualTo("Solo calentamiento 2"), "editar la deja en su lugar");
            Assert.That(Screen.List.CardAt(3)!.NameLabel.text, Is.EqualTo("Solo calentamiento 2"));
            yield return Tap("routine-menu-2");
            yield return Tap("routine-menu-delete");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(4));
            Assert.That(new[] { Listed[0].Name, Listed[1].Name, Listed[2].Name, Listed[3].Name },
                Is.EqualTo(new[] { "Nueva", "Escalera SPM", "Solo calentamiento 2", "Intervalos 4×" }), "borrar no mueve a las demás");
            Assert.That(Screen.List.Count, Is.EqualTo(4));
        }

        [UnityTest, Category("AC-RUT-09-06")]
        public IEnumerator DeleteErrorKeepsTheCard()
        {
            CreateScripted(r => r.FailDelete = true, new[] { RoutineSeeds.EscaleraSpm() });
            yield return GoToRoutines();
            yield return Tap("routine-menu-0");
            yield return Tap("routine-menu-delete");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(Screen.List.Count, Is.EqualTo(1), "la tarjeta sigue");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo eliminar la rutina"));
            Assert.That(H.Logger.Lines, Has.Some.Contains("baja de rutina"));
        }

        [UnityTest, Category("AC-RUT-09-06")]
        public IEnumerator DeleteRemovesWhenDone()
        {
            CreateScripted(r => r.DeleteDelay = TimeSpan.FromSeconds(1), new[] { RoutineSeeds.EscaleraSpm() });
            yield return GoToRoutines();
            yield return Tap("routine-menu-0");
            yield return Tap("routine-menu-delete");
            yield return Tap("dialog-confirm");
            Assert.That(H.TopDialog, Is.Null, "el diálogo no espera el borrado (routines_screen.dart:115)");
            Assert.That(Screen.List.Count, Is.EqualTo(1), "hasta que termina el borrado, la tarjeta sigue");
            H.Advance(1.1);
            yield return Settle();
            yield return Settle();
            Assert.That(Screen.List.Count, Is.EqualTo(0));
            Assert.That(H.IsShown("routines-empty"), Is.True, "vuelve el estado vacío");
        }

        [UnityTest, Category("AC-APP-02-08")]
        public IEnumerator RoutinesIsNotAPlaceholder()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return GoToRoutines();
            var screen = H.Find("screen-routines")!;
            Assert.That(screen, Is.InstanceOf<RoutinesScreenView>());
            Assert.That(screen.Q(className: "rm-placeholder__icon"), Is.Null, "Rutinas deja de ser provisoria");
            // Desde la It. 4a Workout tampoco es provisoria (WSC.WorkoutIsNotAPlaceholder).
            Assert.That(H.Find("screen-workout"), Is.InstanceOf<WorkoutScreenView>(), "Workout deja de ser provisoria (It. 4a)");
            Assert.That(H.Find("screen-history"), Is.InstanceOf<PlaceholderScreenView>(), "Historial sigue provisoria");
        }

        [UnityTest, Category("AC-APP-05-05")]
        public IEnumerator TextScale130()
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.TextScale = 1.3);
            yield return GoToRoutines();
            Assert.That(H.Shell.TextScaleClass, Is.EqualTo("rm-text-scale-130"));
            var small = Screen.List.CardAt(0)!.NameLabel.resolvedStyle.fontSize;
            Assert.That(small, Is.GreaterThan(16f), "el nombre crece con la letra");
            AssertClean(H);
        }

        [UnityTest, Category("AC-APP-05-08")]
        public IEnumerator StructureBarStaticWithReducedMotion()
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.ReduceMotion = true);
            yield return GoToRoutines();
            yield return Frames(3);
            Assert.That(Screen.List.CardAt(0)!.Bar.SegmentCount, Is.EqualTo(8), "la mini-barra no depende del movimiento");
            yield return Tap("routines-add");
            yield return Type("editor-name", "Reducida");
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            yield return Frames();
            Assert.That(Screen.List.IsAnimating, Is.False, "con reducir movimiento la tarjeta nueva aparece sin crecer");
            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Reducida"));
        }
    }
}
