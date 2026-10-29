using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Routines;
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
    // RED · Editor de rutina en pantalla (F-RUT-11, -12, -14; AC-RUT-09-03, -05; 03-rutinas/02-tech-design §4.4, §6.3).
    public sealed class RoutineEditorTests : RoutinesUiTestBase
    {
        private const string Trend = StepSubtitle.TrendEmoji;

        [UnityTest, Category("AC-RUT-11-01")]
        public IEnumerator AppBarNewAndEdit()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            Assert.That(H.IsTextVisible("Nueva rutina"), Is.True);
            Assert.That(Editor.Back.Icon.Codepoint, Is.EqualTo(Icons.ArrowBack));
            Assert.That(Editor.Back.AccessibleName, Is.EqualTo("Atrás"), "ayuda navBack");
            Assert.That(Editor.Save.Label.text, Is.EqualTo("Guardar"));
            Assert.That(H.WhyNotTappable("editor-back"), Is.Null);
            Assert.That(H.WhyNotTappable("editor-save"), Is.Null);
            yield return Tap("editor-back");
            yield return Tap("routine-card-0");
            Assert.That(H.IsTextVisible("Editar rutina"), Is.True);
            Assert.That(H.IsTextVisible("Nueva rutina"), Is.False);
        }

        [UnityTest, Category("AC-RUT-11-02")]
        public IEnumerator FieldsAndPrefill()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            Assert.That(Editor.NameField.IsMultiline, Is.False, "nombre de una línea");
            Assert.That(Editor.DescriptionField.IsMultiline, Is.True, "descripción multilínea");
            Assert.That(Editor.NameField.LabelElement.text, Is.EqualTo("Nombre de la rutina"));
            Assert.That(Editor.DescriptionField.LabelElement.text, Is.EqualTo("Descripción (opcional)"));
            Assert.That(Editor.NameField.Value, Is.Empty);
            var name = H.BoundsOf("editor-name");
            var description = H.BoundsOf("editor-description");
            Assert.That(description.yMin, Is.GreaterThan(name.yMax), "la descripción va debajo");
            Assert.That(name.xMin - H.Root.worldBound.xMin, Is.EqualTo(16).Within(1f), "márgenes de 16");
            yield return Type("editor-name", "Uno\nDos");
            Assert.That(Editor.NameField.Value, Is.EqualTo("UnoDos"), "un salto de línea pegado se quita");
            yield return Tap("editor-back");
            yield return Tap("routine-card-1");
            Assert.That(Editor.NameField.Value, Is.EqualTo("Pirámide 500"), "al editar vienen precargados");
            Assert.That(Editor.DescriptionField.Value, Is.EqualTo("Bajar 2 s el split en cada vuelta"));
        }

        [UnityTest, Category("AC-RUT-11-03")]
        public IEnumerator StepsCountUpdatesAtOnce()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (0)"));
            H.Tap("editor-add-series");
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (2)"), "la plantilla, en el acto");
            yield return Settle();
            H.Tap("step-delete-1");
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (1)"));
            yield return Settle();
        }

        [UnityTest, Category("AC-RUT-11-04")]
        public IEnumerator EmptyStepsText()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            Assert.That(H.IsShown("editor-empty"), Is.True);
            Assert.That(Editor.EmptyLabel.text, Is.EqualTo("Todavía no hay pasos.\nPresioná \"Paso\" o \"Serie\"."));
            AssertColor(Editor.EmptyLabel.resolvedStyle.color, Tokens.TextTertiary, "text.tertiary");
            yield return Tap("editor-add-series");
            Assert.That(H.IsShown("editor-empty"), Is.False);
        }

        [UnityTest, Category("AC-RUT-11-05")]
        public IEnumerator ValidationToasts()
        {
            CreateRoutines(RoutineSeeds.RaraWatts);
            yield return OpenNewEditor();
            yield return Tap("editor-save");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("El nombre no puede estar vacío"));
            Assert.That(H.Editor, Is.Not.Null, "el editor sigue abierto");
            yield return Type("editor-name", "Algo");
            yield return Tap("editor-save");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Agregá al menos un paso"));
            yield return Tap("editor-back");
            yield return Tap("routine-card-0");
            yield return Tap("editor-save");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Hay un paso con valores fuera de rango"), "vatios 50 000");
            Assert.That(H.Editor, Is.Not.Null);
        }

        [UnityTest, Category("AC-RUT-11-06")]
        public IEnumerator SaveClosesAndTheListShowsIt()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "  Nueva  ");
            yield return Type("editor-description", "  ");
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            Assert.That(H.Editor, Is.Null, "guardar cierra el editor");
            Assert.That(Listed[0].Name, Is.EqualTo("Nueva"), "nombre recortado");
            Assert.That(Listed[0].Description, Is.Null.Or.Empty, "descripción vacía");
            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Nueva"));
        }

        [UnityTest, Category("AC-RUT-11-07")]
        public IEnumerator SameNameTwiceAndFiveHundredChars()
        {
            CreateRoutines(RoutineSeeds.Lista);
            var longName = new string('a', 500);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Escalera SPM");
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            yield return Tap("routines-add");
            yield return Type("editor-name", longName);
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            Assert.That(Listed.Count, Is.EqualTo(6));
            Assert.That(Listed[0].Name, Is.EqualTo(longName), "sin largo máximo");
            Assert.That(Listed[1].Name, Is.EqualTo("Escalera SPM"), "sin unicidad");
        }

        [UnityTest, Category("AC-RUT-09-03")]
        public IEnumerator DoubleTapSaveCreatesOne()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Una");
            yield return Tap("editor-add-series");
            yield return Frames();
            H.Tap("editor-save");
            H.TapThrough("editor-save");
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(5), "dos toques seguidos crean una sola");
            Assert.That(H.Editor, Is.Null);
        }

        [UnityTest, Category("AC-RUT-09-03")]
        public IEnumerator TapAndEnterCreateOne()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Una");
            yield return Tap("editor-add-series");
            Editor.Save.Focus();
            yield return Frames();
            H.Tap("editor-save");
            H.PressEnter();
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(5), "toque y Enter crean una sola");
        }

        [UnityTest, Category("AC-RUT-09-03")]
        public IEnumerator EditSavesOnce()
        {
            CreateScripted(_ => { }, new[] { RoutineSeeds.EscaleraSpm() });
            yield return OpenEditorOf(0);
            yield return Type("editor-name", "Escalera 2");
            yield return Frames();
            H.Tap("editor-save");
            H.TapThrough("editor-save");
            yield return Settle();
            Assert.That(H.ScriptedRoutines!.UpdateCalls, Is.EqualTo(1), "un solo UPDATE");
            Assert.That(H.ScriptedRoutines.InsertCalls, Is.EqualTo(0));
            Assert.That(Listed[0].Name, Is.EqualTo("Escalera 2"));
        }

        [UnityTest, Category("AC-RUT-09-03"), Category("AC-APP-03-01")]
        public IEnumerator TapDuringTheExitCreatesNothing()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Una");
            yield return Tap("editor-add-series");
            yield return Frames();
            var save = H.BoundsOf("editor-save").center;
            H.Tap("editor-save");
            H.Advance(0.05);
            yield return Frames();
            Assert.That(H.Shell.Pages.IsTransitioning, Is.True, "el editor está saliendo");
            H.TapAt(save);
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(5), "el segundo toque, 50 ms después, no crea nada (lo que sale no responde)");
            Assert.That(H.Editor, Is.Null, "y no abre otro editor");
        }

        [UnityTest, Category("AC-RUT-09-03"), Category("AC-APP-03-01")]
        public IEnumerator EnterDuringTheExitDoesNothing()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Una");
            yield return Tap("editor-add-series");
            Editor.Save.Focus();
            yield return Frames();
            var exiting = Editor;
            H.Tap("editor-save");
            H.Advance(0.05);
            H.PressEnter();
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(5), "Enter durante la salida no guarda otra vez");
            // La página que sale no recibe el Enter. Desde QA-V3-04 (It. 4a), Tap mueve el foco como un clic de verdad: el + que
            // abrió el editor tenía el foco, ExitLock se lo devuelve al salir y el Enter le llega a él, que abre un editor nuevo y
            // vacío. En el legacy el clic no le da el foco al botón (00-deuda-it3 §11, para el diseño de la 4a).
            var top = H.Editor;
            Assert.That(top == null || (!ReferenceEquals(top, exiting) && top.Store.IsNew && top.NameField.Value.Length == 0), Is.True,
                "el Enter no llegó a la página que sale");
        }

        [UnityTest, Category("AC-RUT-09-05")]
        public IEnumerator SaveErrorKeepsTheEditorOpen()
        {
            CreateScripted(r => r.FailSave = true);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Falla");
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            Assert.That(H.Editor, Is.Not.Null, "el editor sigue abierto");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo guardar la rutina"));
            Assert.That(Editor.NameField.Value, Is.EqualTo("Falla"), "todo queda como estaba");
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (2)"));
            Assert.That(H.Logger.Lines, Has.Some.Contains("guardado de rutina"));
        }

        [UnityTest, Category("AC-RUT-09-05"), Category("AC-RUT-11-08")]
        public IEnumerator BackDuringASlowSaveClosesAndTheSaveLands()
        {
            CreateScripted(r => r.SaveDelay = TimeSpan.FromSeconds(5));
            yield return OpenNewEditor();
            yield return Type("editor-name", "Lenta");
            yield return Tap("editor-add-series");
            yield return Frames();
            var start = H.Scheduler.MonotonicSeconds;
            H.Tap("editor-save");
            yield return Back(BackSource.SystemBack);
            Assert.That(H.Editor, Is.Null, "Atrás cierra sin esperar");
            Assert.That(Listed.Count, Is.EqualTo(0), "todavía no terminó (" + (H.Scheduler.MonotonicSeconds - start).ToString("0.00") + " s de reloj; animaciones: "
                + H.Handle.Motion.DescribeFinite() + ")");
            H.Advance(5.1);
            yield return Settle();
            Assert.That(Listed.Count, Is.EqualTo(1), "el guardado llega igual");
            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Lenta"));
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "sin aviso");
            Assert.That(H.HasErrors(), Is.False);
        }

        [UnityTest, Category("AC-RUT-12-01")]
        public IEnumerator AddStepAtTheEnd()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-series");
            yield return AddLooseStep(("step-min", "5"));
            Assert.That(RowCount(), Is.EqualTo(3));
            Assert.That(Row(2).Title.text, Is.EqualTo("Trabajo"));
            Assert.That(Row(2).Subtitle.Text, Is.EqualTo("5m"));
            Assert.That(Row(2).ClassListContains("rm-step-row--in-series"), Is.False, "suelto, después del pie de la serie");
            Assert.That(Editor.Store.Template[2].GroupId, Is.Null);
        }

        [UnityTest, Category("AC-RUT-12-01")]
        public IEnumerator CancelAddsNothing()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Tap("editor-add-step");
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>());
            yield return TypeInDialog("step-min", "5");
            yield return ScrollAndTap("dialog-cancel");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(RowCount(), Is.EqualTo(0));
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (0)"));
        }

        [UnityTest, Category("AC-RUT-12-02")]
        public IEnumerator StepRowContract()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return Frames(2);
            var row = Row(0);
            Assert.That(row.resolvedStyle.borderLeftWidth, Is.EqualTo(3).Within(0.1f), "borde izquierdo 3 (redondeado al píxel)");
            Assert.That(row.layout.height, Is.GreaterThanOrEqualTo(56 - 0.5f), "min-height 56");
            var badge = row.Q(className: "rm-step-row__badge");
            Assert.That(badge.layout.width, Is.EqualTo(32).Within(0.5f));
            Assert.That(badge.layout.height, Is.EqualTo(32).Within(0.5f));
            var expected = TokenColors.WithAlpha(StepTypeInfo.Color(StepType.Warmup), 0.2);
            Assert.That(row.BadgeBackground.resolvedStyle.backgroundColor.a, Is.EqualTo(expected.a).Within(0.01f), "insignia @20 %");
            Assert.That(row.Edit!.Icon.resolvedStyle.width, Is.EqualTo(20).Within(0.5f), "lápiz de 20");
            Assert.That(row.Delete!.Icon.resolvedStyle.width, Is.EqualTo(20).Within(0.5f), "cruz de 20");
            Assert.That(row.Edit.layout.width, Is.GreaterThanOrEqualTo(48 - 0.5f), "caja táctil de 48");
            Assert.That(row.Delete.layout.height, Is.GreaterThanOrEqualTo(48 - 0.5f));
            AssertColor(row.Title.resolvedStyle.color, StepRowModel.HudColor(StepType.Warmup), "título en step.warmup.hud");
            AssertColor(row.Icon.resolvedStyle.color, StepRowModel.HudColor(StepType.Warmup), "ícono en step.warmup.hud");
        }

        [UnityTest, Category("AC-RUT-01-04")]
        public IEnumerator StepRowIcons()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            var types = new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Cooldown };
            for (var i = 0; i < types.Length; i++)
            {
                Assert.That(Row(i).Type, Is.EqualTo(types[i]));
                Assert.That(Row(i).Icon.Codepoint, Is.EqualTo(StepRowModel.IconFor(types[i])), "ícono del tipo " + types[i]);
                Assert.That(Row(i).ClassListContains(StepRowView.TypeClass(types[i])), Is.True);
            }

            Assert.That(Row(0).Title.text, Is.EqualTo("Calentamiento"));
            Assert.That(Row(1).Title.text, Is.EqualTo("Trabajo"));
            Assert.That(Row(2).Title.text, Is.EqualTo("Descanso"));
            Assert.That(Row(3).Title.text, Is.EqualTo("Enfriamiento"));
        }

        [UnityTest, Category("AC-RUT-12-03")]
        public IEnumerator EditInPlace()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-0");
            Assert.That(H.IsTextVisible("Editar paso"), Is.True);
            yield return TypeInDialog("step-min", "10");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(Row(0).Subtitle.Text, Is.EqualTo("10m"));
            Assert.That(RowCount(), Is.EqualTo(4), "no se agrega ni se mueve nada");
            Assert.That(Editor.Store.Template[1].GroupId, Is.Not.Null, "la serie sigue igual");
        }

        [UnityTest, Category("AC-RUT-12-04")]
        public IEnumerator DeleteStepAtOnce()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            H.Tap("step-delete-3");
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (3)"), "sin preguntar");
            Assert.That(H.TopDialog, Is.Null);
            yield return Settle();
            Assert.That(RowCount(), Is.EqualTo(3));
        }

        [UnityTest, Category("AC-RUT-12-05"), Category("AC-RUT-14-12")]
        public IEnumerator NoReorderOrMoveAffordances()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(1);
            var steps = H.Find("editor-steps")!;
            for (var i = 0; i < RowCount(); i++)
            {
                var buttons = Row(i).Query<Button>().ToList();
                Assert.That(buttons.Count, Is.EqualTo(2), "solo lápiz y cruz en la fila " + i);
            }

            var icons = steps.Query<IconView>().ToList();
            foreach (var icon in icons)
            {
                Assert.That(icon.Codepoint, Is.Not.EqualTo(Icons.ArrowBack), "sin flechas para mover");
            }

            Assert.That(steps.Q(className: "rm-drag-handle"), Is.Null, "sin asas de arrastre");
            var header = Block(0).Header.Query<Button>().ToList();
            Assert.That(header.Count, Is.EqualTo(2), "encabezado: chip y tacho, nada para mover la serie");
        }

        [UnityTest, Category("AC-RUT-12-06"), Category("AC-RUT-04-06")]
        public IEnumerator BorderSubtitlesOnScreen()
        {
            CreateRoutines(RoutineSeeds.Borde);
            yield return OpenEditorOf(0);
            var expected = new[]
            {
                ("Trabajo", "0s"),
                ("Descanso", "?"),
                ("Trabajo", "1m 30s  ·  >150W"),
                ("Trabajo", "60m"),
                ("Enfriamiento", "62m 5s"),
                ("Trabajo", "2000m  ·  0 spm"),
                ("Calentamiento", "30s"),
                ("Trabajo", "1m  ·  00:59/500m"),
                ("Trabajo", "2m  ·  120-180W · 22 spm · 02:05/500m"),
                ("Trabajo", "5m  ·  20 spm · " + Trend + " SPM -1/rep"),
                ("Descanso", "1m  ·  " + Trend + " Split +3s/rep"),
            };
            Assert.That(RowCount(), Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(Row(i).Title.text, Is.EqualTo(expected[i].Item1), "título " + (i + 1));
                Assert.That(Row(i).Subtitle.Text, Is.EqualTo(expected[i].Item2), "subtítulo " + (i + 1));
            }

            Assert.That(Row(9).Subtitle.HasTrend, Is.True, "el 📈 se dibuja como el ícono");
            Assert.That(StepSubtitleView.TrendGlyph, Is.EqualTo(Icons.TrendingUp).And.Not.Empty, "el glifo trending_up (U+E8E5)");
            Assert.That(Row(9).Subtitle.text, Does.Contain(StepSubtitleView.TrendGlyph));
            Assert.That(Row(8).Subtitle.text, Does.Not.Contain(StepSubtitleView.TrendGlyph), "sin progresiones, sin ícono");
            Assert.That(Row(9).Subtitle.text, Does.Not.Contain(Trend));
            Assert.That(Row(9).Subtitle.text, Does.Contain(StepSubtitleView.TrendMarkup()), "glifo en metric.distance al 117 %");
            Assert.That(Row(8).Subtitle.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.PreWrap), "los dos espacios del separador");
            AssertClean(H);
        }

        [UnityTest, Category("AC-RUT-14-01")]
        public IEnumerator SeriesButton()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            H.Tap("editor-add-series");
            H.Tap("editor-add-series");
            yield return Settle();
            Assert.That(H.Find("series-0"), Is.Not.Null);
            Assert.That(H.Find("series-1"), Is.Not.Null, "dos toques, dos series");
            var template = Editor.Store.Template;
            Assert.That(template.Count, Is.EqualTo(4));
            Assert.That(template[0].GroupId, Is.Not.EqualTo(template[2].GroupId), "ids distintos (aunque sea el mismo milisegundo)");
            Assert.That(Row(0).Subtitle.Text, Is.EqualTo("2m"));
            Assert.That(Row(1).Subtitle.Text, Is.EqualTo("1m"));
            Assert.That(Block(0).RepeatCount, Is.EqualTo(3));
        }

        [UnityTest, Category("AC-RUT-14-02")]
        public IEnumerator SeriesBlockContract()
        {
            CreateWith(new[]
            {
                RoutineSeeds.Make("Hojas", RoutineSeeds.Day(24),
                    RoutineSeeds.Step(StepType.Work, dur: 60, g: "g1_0", rep: 1),
                    RoutineSeeds.Step(StepType.Work, dur: 60, g: "g1_1", rep: 2),
                    RoutineSeeds.Step(StepType.Work, dur: 60, g: "g1_2", rep: 3)),
            });
            yield return OpenEditorOf(0);
            yield return Frames(2);
            Assert.That(Block(0).VisibleLayers, Is.EqualTo(0), "×1: sin hojas");
            Assert.That(Block(1).VisibleLayers, Is.EqualTo(1), "×2: una hoja");
            Assert.That(Block(2).VisibleLayers, Is.EqualTo(2), "×3 o más: dos hojas");
            var block = Block(1);
            Assert.That(block.resolvedStyle.marginTop, Is.EqualTo(8).Within(0.5f), "8 arriba");
            Assert.That(block.resolvedStyle.marginBottom, Is.GreaterThanOrEqualTo(8 - 0.5f), "8 abajo");
            Assert.That(block.Header.layout.height, Is.GreaterThanOrEqualTo(48 - 0.5f), "encabezado de 48");
            Assert.That(block.Footer.layout.height, Is.GreaterThanOrEqualTo(48 - 0.5f), "pie de 48");
            foreach (var part in new[] { block.Header, block.Steps, block.Footer })
            {
                Assert.That(part.resolvedStyle.borderLeftWidth, Is.EqualTo(3).Within(0.1f), "riel continuo");
                AssertColor(part.resolvedStyle.borderLeftColor, Tokens.BrandSeed, "riel en brand.seed");
            }

            var row = Row(1);
            Assert.That(row.ClassListContains("rm-step-row--in-series"), Is.True);
            Assert.That(row.worldBound.xMin - block.Header.worldBound.xMin, Is.EqualTo(16).Within(1f), "sangría visible 16");
        }

        [UnityTest, Category("AC-RUT-14-03")]
        public IEnumerator ChipTextAndTouchArea()
        {
            CreateWith(new[] { RoutineSeeds.EscaleraSpm(), RoutineSeeds.RaraHuge.Routines[0] with { CreatedAt = RoutineSeeds.Day(26) } });
            yield return OpenEditorOf(1);
            yield return Frames(2);
            var chip = Block(0).Chip;
            var detail = " (min " + chip.resolvedStyle.minWidth + " × " + chip.resolvedStyle.minHeight + ", layout " + chip.layout + ", clases "
                         + string.Join(" ", chip.GetClasses()) + ", encabezado " + Block(0).Header.layout + ")";
            Assert.That(chip.layout.width, Is.GreaterThanOrEqualTo(48 - 0.5f), "caja táctil ≥ 48" + detail);
            Assert.That(chip.layout.height, Is.GreaterThanOrEqualTo(48 - 0.5f), "caja táctil ≥ 48 de alto" + detail);
            Assert.That(Block(0).Count.Text, Is.EqualTo("×3"));
            yield return Tap("editor-back");
            yield return Tap("routine-card-0");
            yield return Frames(2);
            var huge = Block(0);
            Assert.That(huge.Count.Text, Is.EqualTo(RoutineTexts.RepeatChip(2000000000, H.App.Texts.Value)));
            Assert.That(huge.Pill.layout.width, Is.GreaterThanOrEqualTo(huge.Count.NaturalWidth - 0.5f), "la cápsula crece y no corta el número");
            Assert.That(huge.Pill.worldBound.xMax, Is.LessThanOrEqualTo(huge.Header.worldBound.xMax + 0.5f));
        }

        [UnityTest, Category("AC-RUT-14-07")]
        public IEnumerator AddStepToSeries()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("series-add-step-0");
            Assert.That(H.TopDialog, Is.InstanceOf<StepDialog>());
            yield return TypeInDialog("step-min", "1");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(RowCount(), Is.EqualTo(5));
            var template = Editor.Store.Template;
            Assert.That(template[3].GroupId, Is.EqualTo(template[1].GroupId), "después del último de su serie");
            Assert.That(template[3].GroupRepeatCount, Is.EqualTo(3));
            Assert.That(template[4].Type, Is.EqualTo(StepType.Cooldown));
            Assert.That(Row(3).ClassListContains("rm-step-row--in-series"), Is.True);
        }

        [UnityTest, Category("AC-RUT-14-08")]
        public IEnumerator EditSeriesStep()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            yield return ScrollAndTap("step-edit-1");
            Assert.That(H.Find("step-prog-spm"), Is.Not.Null, "en una serie se ven las progresiones");
            yield return TypeInDialog("step-spm", "20");
            yield return ScrollAndTap("dialog-confirm");
            Assert.That(Row(1).Subtitle.Text, Does.StartWith("5m  ·  20 spm"));
            Assert.That(Editor.Store.Template[1].GroupId, Is.Not.Null, "sigue en su serie");
        }

        [UnityTest, Category("AC-RUT-14-09")]
        public IEnumerator DeleteSeriesAtOnce()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            H.Tap("series-delete-0");
            Assert.That(H.TopDialog, Is.Null, "sin preguntar");
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (2)"));
            yield return Settle();
            Assert.That(H.Find("series-0"), Is.Null);
            Assert.That(RowCount(), Is.EqualTo(2));
        }

        [UnityTest, Category("AC-RUT-14-10")]
        public IEnumerator SeriesEmptiesAndDisappears()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenEditorOf(0);
            H.Tap("step-delete-2");
            yield return Settle();
            Assert.That(H.Find("series-0"), Is.Not.Null, "con un paso sigue");
            H.Tap("step-delete-1");
            yield return Settle();
            Assert.That(H.Find("series-0"), Is.Null, "vacía, desaparece");
            Assert.That(RowCount(), Is.EqualTo(2));
        }

        [UnityTest, Category("AC-RUT-14-11")]
        public IEnumerator ContiguousSeries()
        {
            CreateRoutines(RoutineSeeds.PiramideContiguous);
            yield return OpenEditorOf(0);
            yield return Frames(2);
            var first = Block(0);
            var second = Block(1);
            Assert.That(second.Header.worldBound.yMin - first.Footer.worldBound.yMax, Is.EqualTo(16).Within(1f), "16 ± 1 dp entre el pie y el encabezado");
            Assert.That(first.Count.Text, Is.EqualTo("×4"));
            Assert.That(second.Count.Text, Is.EqualTo("×2"));
        }

        [UnityTest, Category("AC-APP-05-11")]
        public IEnumerator NoScrollbarOnPhone()
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.IsDesktop = false);
            yield return OpenEditorOf(1);
            Assert.That(Editor.Scroll.verticalScrollerVisibility, Is.EqualTo(ScrollerVisibility.Hidden), "teléfono: sin barra fija");
        }

        [UnityTest, Category("AC-APP-05-05")]
        public IEnumerator TextScale130()
        {
            CreateRoutines(RoutineSeeds.Borde, DeviceProfile.TV, o => o.TextScale = 1.3);
            yield return OpenEditorOf(0);
            yield return Frames(2);
            Assert.That(H.Shell.TextScaleClass, Is.EqualTo("rm-text-scale-130"));
            for (var i = 0; i < RowCount(); i++)
            {
                Assert.That(Row(i).Subtitle.worldBound.xMax, Is.LessThanOrEqualTo(Row(i).worldBound.xMax + 0.5f), "el subtítulo ajusta en la fila " + i);
            }

            AssertClean(H);
        }

        [UnityTest, Category("AC-APP-05-08")]
        public IEnumerator RowMotionReduced()
        {
            // Reducir movimiento (04-design-notes §5.6): lo nuevo funde 160 ms SIN crecer (tiene su alto desde el primer cuadro) y
            // lo borrado se va con un fundido, sin colapsar.
            CreateRoutines(RoutineSeeds.Empty, null, o => o.ReduceMotion = true);
            yield return OpenNewEditor();
            H.Tap("editor-add-series");
            yield return Frames(2);
            Assert.That(Block(0).layout.height, Is.GreaterThan(48), "ya con su alto: no crece desde 0");
            H.Advance(0.2);
            yield return Frames(2);
            Assert.That(Editor.Steps.IsAnimating, Is.False, "el fundido de 160 ms terminó");
            var before = Block(0).layout.height;
            H.Tap("step-delete-0");
            H.Advance(0.3);
            yield return Frames(2);
            Assert.That(Editor.Steps.IsAnimating, Is.False, "lo borrado se fue sin animar el alto");
            Assert.That(Block(0).layout.height, Is.LessThan(before));
        }
    }
}
