using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // SPV · Los paneles montados (05-series-parciales/02-tech-design §3.1, §6.3, §6.4; 04-design-notes §4, §5.3, §9.2): LT-59 a LT-61
    // con el tracker armado como el legacy (ComposeSeries) y montado con OverrideSeries; las ranuras con sus rectángulos y la
    // anatomía; los vacíos parejos; la escala de título por idioma; lo largo que se achica sin cortarse; las reglas de alfa por la
    // tinta; el cambio por pieza; un elemento por panel; el panel que vuelve a un panel de UI Toolkit.
    public sealed class SeriesPanelViewTests : SeriesUiTestBase
    {
        // ── LT-59 a LT-61 (test/series_panels_test.dart) ──

        private HudSeries LegacyTracker(bool inSeries)
        {
            // test/series_panels_test.dart:12-22: Begin(0) y una sola lectura de 300 m a los 60 s, que suma (300 ≤ 10 × máx(60; 1)).
            var tracker = new SeriesTracker();
            tracker.Begin(0, 0);
            var step = inSeries ? RoutineSeeds.Lt59Work : RoutineSeeds.Lt59LooseWork;
            var position = inSeries ? new StepPosition("g", 1, 3) : new StepPosition(null, 1, 1);
            tracker.Update(0, step, position, 300, 60);
            var series = WorkoutHudStore.ComposeSeries(tracker.State, new[] { step }, new[] { position }, H.App.Texts.Value, H.HudState!.Session);
            Assert.That(series, Is.Not.Null);
            return series!;
        }

        // legacy: test/series_panels_test.dart:39 'arrancan colapsados y al tocar se expanden y se recuerda'
        [UnityTest, Category("LT-59"), Category("AC-WKT-18-02"), Category("AC-WKT-18-03"), Category("AC-WKT-18-06"), Category("AC-WKT-18-07"),
         Category("AC-WKT-18-09"), Category("AC-WKT-18-10"), Category("AC-WKT-18-11")]
        public IEnumerator StartCollapsedTapExpandsAndRemembers()
        {
            CreateWorkout(RoutineSeeds.Lt59Seed, o => o.PreferencesFile = true);
            yield return StartRoutine("LT-59");
            H.OverrideSeries(LegacyTracker(true));
            yield return Settle();
            Assert.That(H.IsTextVisible("Parciales 500 m"), Is.False, "find.text('Parciales 500 m'): nada");
            Assert.That(H.IsTextVisible("Series anteriores"), Is.False, "find.text('Series anteriores'): nada");
            Assert.That(H.IsTextVisible("500 m"), Is.True, "find.text('500 m'): uno");
            Assert.That(H.IsTextVisible("Reps"), Is.True, "find.text('Reps'): uno");

            H.TapText("500 m");
            yield return SettlePanels();
            Assert.That(H.IsTextVisible("Parciales 500 m"), Is.True, "Parciales 500 m: uno");
            Assert.That(H.IsTextVisible("Rep 1/3 · 300 m"), Is.True, "Rep 1/3 · 300 m: uno");
            Assert.That(H.IsTextVisible("500 m"), Is.False, "el rótulo del chip abierto tiene alfa 0");
            H.FlushPreferences();
            Assert.That(H.PreferencesPath, Is.Not.Null);
            var file = File.ReadAllText(H.PreferencesPath!);
            Assert.That(file.Replace(" ", ""), Does.Contain("\"" + SeriesPanelPrefs.LapsKey + "\":true"), "prefs.getBool(lapsKey) == true: " + file);

            H.TapText("Reps");
            yield return SettlePanels();
            Assert.That(H.IsTextVisible("Sin repeticiones completas"), Is.True, "find.text('Sin repeticiones completas'): uno");
            var empty = SlotNamed("hud-reps-empty");
            Assert.That(empty.Line0 + " / " + empty.Line1, Is.EqualTo("Sin repeticiones / completas"), "en dos renglones parejos (DD5-19)");
        }

        // legacy: test/series_panels_test.dart:62 'respeta la preferencia guardada'
        [UnityTest, Category("LT-60"), Category("AC-WKT-18-10")]
        public IEnumerator RespectsTheSavedPreference()
        {
            CreateWorkout(RoutineSeeds.Lt59Seed, o => o.Preferences = Prefs(true, false));
            yield return OpenStartDialog("LT-59");
            yield return Frames();
            H.Tap("dialog-confirm");
            var sawClosed = false;
            var sawPanel = false;
            for (var i = 0; i < 70; i++)
            {
                H.Step();
                yield return null;
                var column = H.SeriesColumn;
                if (column == null)
                {
                    continue;
                }

                sawPanel = true;
                sawClosed |= !column.Laps.Motion.Expanded || column.Laps.VisualHeightBase < column.Laps.FinalHeightBase - 0.01;
            }

            Assert.That(sawPanel, Is.True, "el panel existe desde que se arma el HUD");
            Assert.That(sawClosed, Is.False, "abierto desde el primer cuadro, sin pasar por cerrado");
            H.OverrideSeries(LegacyTracker(true));
            yield return Settle();
            Assert.That(H.IsTextVisible("Parciales 500 m"), Is.True, "Parciales 500 m: uno");
        }

        // legacy: test/series_panels_test.dart:68 'sin serie no aparece el panel de repeticiones'
        [UnityTest, Category("LT-61"), Category("AC-WKT-18-01")]
        public IEnumerator WithoutASeriesThereIsNoRepsPanel()
        {
            CreateWorkout(RoutineSeeds.Lt59Seed);
            yield return StartRoutine("LT-61");
            H.OverrideSeries(LegacyTracker(false));
            yield return Settle();
            Assert.That(H.IsTextVisible("500 m"), Is.True, "find.text('500 m'): uno");
            Assert.That(H.IsTextVisible("Reps"), Is.False, "find.text('Reps'): nada");
            Assert.That(Reps.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "#hud-reps-panel fuera del árbol visible");
        }

        // ── Las ranuras, los rectángulos y la anatomía (04-design-notes §9.2; DD5-23, DD5-25) ──

        [UnityTest, Category("AC-WKT-18-02"), Category("AC-WKT-18-03"), Category("AC-WKT-18-04"), Category("AC-WKT-18-06"), Category("AC-WKT-17-01")]
        public IEnumerator SlotsAndRectanglesLikeTheDesign()
        {
            // CAP-01: los dos chips cerrados en TV (Series 600 no tiene objetivos en SPM: la columna desde 210,9).
            CreateSeries(DeviceProfile.TV);
            yield return StartAt("Series 600", 60.2);
            var laps = VisualRect(Laps);
            var reps = VisualRect(Reps);
            AssertSpan("500 m x", laps.xMin, laps.xMax, 14.8, 159.1);
            AssertSpan("500 m y", laps.yMin, laps.yMax, 210.9, 246.7);
            AssertSpan("Reps y", reps.yMin, reps.yMax, 255.1, 291.0);
            Assert.That(H.TextOf("hud-laps-chip"), Is.EqualTo("500 m"));
            Assert.That(H.TextOf("hud-reps-chip"), Is.EqualTo("Reps"));
            var chip = SlotNamed("hud-laps-chip");
            Assert.That(chip.WorldRect.xMin, Is.EqualTo(14.8 + 28 * 1.054).Within(0.5), "el rótulo desde x 28 base");

            // CAP-02: parciales abierto a los 170,2 s.
            CreateSeries(DeviceProfile.TV, laps: true);
            yield return StartAt("Series 600", 170.2);
            AssertSpan("parciales y", VisualRect(Laps).yMin, VisualRect(Laps).yMax, 210.9, 331.0);
            Assert.That(H.TextOf("hud-laps-title"), Is.EqualTo("Parciales 500 m"));
            Assert.That(H.TextOf("hud-laps-current"), Is.EqualTo("0:15.0"));
            Assert.That(H.TextOf("hud-laps-current-meters"), Is.EqualTo("60 m"));
            Assert.That(H.TextOf("hud-laps-row-0"), Is.EqualTo("#1 2:05.0"));
            Assert.That(H.TextOf("hud-rep-progress"), Is.EqualTo("Rep 1/3 · 560 m"));
            Assert.That(Laps.TitleScale, Is.EqualTo(0.949).Within(0.01), "la escala de título del español (DD5-20; R5-02: lo medido)");
            var hero = SlotNamed("hud-laps-current");
            var meters = SlotNamed("hud-laps-current-meters");
            Assert.That(meters.WorldRect.yMax, Is.EqualTo(hero.WorldRect.yMax).Within(0.25f), "la misma línea de base (SD-16)");
            Assert.That(meters.WorldRect.xMax, Is.EqualTo(14.8 + 127 * 1.054).Within(0.5), "los metros pegados a x 127");
            AssertSpan("Reps y", VisualRect(Reps).yMin, VisualRect(Reps).yMax, 339.4, 375.3);

            // CAP-03: los dos abiertos a los 250,2 s; los metros de repeticiones pegados a x 127, como los de la vuelta en curso.
            CreateSeries(DeviceProfile.TV, laps: true, reps: true);
            yield return StartAt("Series 600", 250.2);
            AssertSpan("parciales y", VisualRect(Laps).yMin, VisualRect(Laps).yMax, 210.9, 305.7);
            AssertSpan("repeticiones y", VisualRect(Reps).yMin, VisualRect(Reps).yMax, 314.1, 377.4);
            Assert.That(H.TextOf("hud-reps-row-0"), Is.EqualTo("Rep 1 2:05 680 m"));
            Assert.That(SlotNamed("hud-reps-row-0").WorldRect.xMax, Is.EqualTo(14.8 + 127 * 1.054).Within(0.5));
            Assert.That(SeriesPanelGeometry.RespectsBoatZone(new DpRect(Reps.worldBound.xMin, Reps.worldBound.yMin, Reps.worldBound.width,
                Reps.worldBound.height), true, Immersive.Layout!), Is.True, "repeticiones entra en la zona del bote solo por su franja de afuera");
            Assert.That(Laps.childCount, Is.EqualTo(7), "un elemento por panel: 6 holders y el hijo de toque, ocultos");
            foreach (var child in Laps.Children())
            {
                Assert.That(UiQuery.IsShown(child), Is.False, "sus hijos ocultos no cuentan: " + child.GetType().Name);
            }
        }

        [UnityTest, Category("AC-WKT-18-05"), Category("AC-WKT-18-07"), Category("AC-WKT-18-22")]
        public IEnumerator EmptyPanelsInTwoEvenLinesInBothLanguages()
        {
            CreateSeries(DeviceProfile.TV, laps: true, reps: true);
            yield return StartAt("Series 600", 190.2);
            AssertSpan("parciales y", VisualRect(Laps).yMin, VisualRect(Laps).yMax, 210.9, 322.6);
            Debug.Log("[SPV] CAP-05: " + Describe(Laps) + " / " + Describe(Reps) + " · top de repeticiones " + Column.RepsPlacement.TopDp.ToString("0.000"));
            AssertSpan("repeticiones y", VisualRect(Reps).yMin, VisualRect(Reps).yMax, 331.0, 419.5);
            var laps = SlotNamed("hud-laps-empty");
            Assert.That(laps.Text, Is.EqualTo("Sin parciales en este paso"));
            Assert.That(laps.Line0 + " / " + laps.Line1, Is.EqualTo("Sin parciales / en este paso"));
            Assert.That(SlotNamed("hud-reps-empty").Line0 + " / " + SlotNamed("hud-reps-empty").Line1, Is.EqualTo("Sin repeticiones / completas"));
            Assert.That(H.TextOf("hud-rep-progress"), Is.EqualTo("Rep 1/3 · 640 m"));

            H.SetLanguage("en-US");
            yield return Settle();
            Assert.That(SlotNamed("hud-laps-empty").Line0 + " / " + SlotNamed("hud-laps-empty").Line1, Is.EqualTo("No splits / in this step"));
            Assert.That(SlotNamed("hud-reps-empty").Line0 + " / " + SlotNamed("hud-reps-empty").Line1, Is.EqualTo("No completed / reps"));
            Assert.That(H.TextOf("hud-laps-title"), Is.EqualTo("500 m splits"));
            Assert.That(H.TextOf("hud-reps-title"), Is.EqualTo("Previous reps"));
            Assert.That(Laps.TitleScale, Is.EqualTo(1).Within(1e-6), "en inglés, los dos títulos a escala 1 (DD5-20)");
            Assert.That(Reps.TitleScale, Is.EqualTo(1).Within(1e-6));
            AssertSpan("parciales y (los mismos rectángulos)", VisualRect(Laps).yMin, VisualRect(Laps).yMax, 210.9, 322.6);
        }

        // O-5 de la lente de motion de la 5 (06a-escena-lago/00-deuda-5 §3): el corte de los vacíos en dos renglones se rehace solo
        // si cambia el texto (otro idioma); los Apply de cada segundo con el mismo vacío no lo vuelven a cortar (antes, Split y Join
        // una vez por segundo). Los renglones siguen siendo los de DD5-19 en los dos idiomas.
        [UnityTest, Category("AC-WKT-18-05"), Category("AC-WKT-18-21")]
        public IEnumerator TheEmptySplitIsRedoneOnlyWhenTheTextChanges()
        {
            CreateSeries(DeviceProfile.TV, laps: true, reps: true);
            yield return StartAt("Series 600", 190.2);
            var laps = Laps.EmptySplits;
            var reps = Reps.EmptySplits;
            Assert.That(laps, Is.GreaterThanOrEqualTo(1), "parciales ya cortó su vacío");
            Assert.That(reps, Is.GreaterThanOrEqualTo(1), "repeticiones ya cortó su vacío");
            var draws = Laps.DrawCount;
            yield return AdvanceTo(196.2);
            yield return Settle();
            Assert.That(Laps.DrawCount, Is.GreaterThan(draws), "los paneles se volvieron a dibujar con los segundos del descanso");
            Assert.That(Laps.EmptySplits, Is.EqualTo(laps), "6 s del descanso con el mismo vacío: no se rehace el corte");
            Assert.That(Reps.EmptySplits, Is.EqualTo(reps));
            Assert.That(SlotNamed("hud-laps-empty").Line0 + " / " + SlotNamed("hud-laps-empty").Line1, Is.EqualTo("Sin parciales / en este paso"));
            Assert.That(SlotNamed("hud-reps-empty").Line0 + " / " + SlotNamed("hud-reps-empty").Line1, Is.EqualTo("Sin repeticiones / completas"));

            H.SetLanguage("en-US");
            yield return Settle();
            Assert.That(Laps.EmptySplits, Is.GreaterThan(laps), "otro idioma: otro corte");
            Assert.That(SlotNamed("hud-laps-empty").Line0 + " / " + SlotNamed("hud-laps-empty").Line1, Is.EqualTo("No splits / in this step"));
            Assert.That(SlotNamed("hud-reps-empty").Line0 + " / " + SlotNamed("hud-reps-empty").Line1, Is.EqualTo("No completed / reps"));
            var english = Laps.EmptySplits;
            yield return AdvanceTo(199.2);
            yield return Settle();
            Assert.That(Laps.EmptySplits, Is.EqualTo(english), "y después, otra vez quieto");
        }

        // ── Lo largo se achica y nunca se corta (DV-49, AC-WKT-18-17; DT5-16) ──

        [UnityTest, Category("AC-WKT-18-17"), Category("AC-WKT-17-10")]
        public IEnumerator LongValuesShrinkAndAreNeverCut()
        {
            CreateSeries(DeviceProfile.TV, laps: true, reps: true);
            yield return StartAt("Series 600", 170.2);
            H.OverrideSeries(SeriesSeeds.Largo(H.App.Texts.Value, H.HudState!.Session));
            yield return Settle();
            AssertSpan("parciales y", VisualRect(Laps).yMin, VisualRect(Laps).yMax, 210.9, 400.6);
            AssertSpan("repeticiones y", VisualRect(Reps).yMin, VisualRect(Reps).yMax, 409.0, 518.6);
            Assert.That(Laps.HeroScale, Is.EqualTo(0.652).Within(0.02), "la cifra de la vuelta en curso");
            Assert.That(Laps.RowsScaleNow, Is.EqualTo(0.932).Within(0.02), "las filas de vueltas");
            Assert.That(Laps.RepLineScale, Is.EqualTo(0.712).Within(0.02), "la línea Rep");
            Assert.That(Reps.RowsScaleNow, Is.EqualTo(0.638).Within(0.02), "las filas de repeticiones");
            AssertInsideGlass(Laps);
            AssertInsideGlass(Reps);
            foreach (var (_, slot) in UiQuery.Slots(H.Root))
            {
                Assert.That(slot.Text, Does.Not.Contain("…"), "nada cortado con S-LARGO: " + slot);
            }

            // ExtremeSplit: un split más largo que 5999:59 (86 400 s y 1 m de trabajo): la escala en su piso y el split con "…".
            H.OverrideSeries(SeriesSeeds.ExtremeSplit(H.App.Texts.Value, H.HudState!.Session));
            yield return Settle();
            Assert.That(Reps.RowsScaleNow, Is.EqualTo(SeriesPanelGeometry.SeriesNumberMinScale).Within(1e-4), "la escala común en su piso");
            var extreme = SlotNamed("hud-reps-row-0");
            Assert.That(extreme.Text, Does.Contain("…"), "el split recortado con … al final: " + extreme.Text);
            Assert.That(extreme.Text, Does.StartWith("Rep 999 ").And.EndWith(" m"), "el rótulo y los metros enteros: " + extreme.Text);
            AssertInsideGlass(Reps);
        }

        private static void AssertInsideGlass(SeriesPanelView panel)
        {
            var glass = VisualRect(panel);
            for (var i = 0; i < panel.SlotCount; i++)
            {
                var slot = panel.SlotAt(i);
                if (!slot.IsVisible)
                {
                    continue;
                }

                var r = slot.WorldRect;
                Assert.That(r.xMin >= glass.xMin - 0.01f && r.xMax <= glass.xMax + 0.01f && r.yMin >= glass.yMin - 0.01f && r.yMax <= glass.yMax + 0.01f,
                    Is.True, panel.name + ": " + slot + " fuera del vidrio " + glass);
            }
        }

        // ── Las reglas de alfa por la tinta (DD5-13, DD5-14) ──

        [UnityTest, Category("AC-WKT-18-18"), Category("AC-WKT-18-19")]
        public IEnumerator NoInkOutsideTheGlassWhileOpening()
        {
            CreateSeries(DeviceProfile.TV, reps: true);
            yield return StartAt("Series 600", 250.0);
            // En reposo, todo renglón al 100 %.
            for (var i = 0; i < Reps.SlotCount; i++)
            {
                var slot = Reps.SlotAt(i);
                if (slot.Name != "hud-reps-chip")
                {
                    Assert.That(slot.Alpha, Is.EqualTo(1).Within(1e-4), "en reposo: " + slot);
                }
            }

            H.TapText("500 m");
            var sawPartial = false;
            for (var frame = 0; frame < 50; frame++)
            {
                H.Step();
                yield return null;
                var g = Laps.VisualHeightBase;
                var glassBottom = Laps.worldBound.yMin + (float)(g * Laps.Scale);
                for (var i = 0; i < Laps.SlotCount; i++)
                {
                    var slot = Laps.SlotAt(i);
                    if (slot.Alpha > 0.01f)
                    {
                        Assert.That(slot.WorldRect.yMax, Is.LessThanOrEqualTo(glassBottom + 0.05f), "cuadro " + frame + ": tinta fuera del vidrio: " + slot);
                    }

                    sawPartial |= slot.Alpha > 0.01f && slot.Alpha < 0.99f;
                }
            }

            Assert.That(sawPartial, Is.True, "algún renglón apareció con el revelar (no de golpe)");
            yield return Settle();
            Assert.That(Laps.VisualHeightBase, Is.EqualTo(90).Within(0.1), "de 34 a 90 (VID-01)");
        }

        // ── El cambio de contenido, por pieza (DD5-18) ──

        [UnityTest, Category("AC-WKT-18-08"), Category("AC-WKT-18-19")]
        public IEnumerator TheContentChangesPieceByPiece()
        {
            // Dos series a los 61 s: de un trabajo a otro, la vuelta en curso no funde (rueda).
            CreateSeries(DeviceProfile.TV, laps: true, reps: true);
            yield return StartRoutine("Dos series");
            yield return AdvanceTo(60.5);
            H.Advance(0.5);
            var minHero = 1f;
            for (var frame = 0; frame < 20; frame++)
            {
                H.Step();
                yield return null;
                var hero = H.SlotOf("hud-laps-current");
                if (hero.HasValue)
                {
                    minHero = Math.Min(minHero, hero.Value.Slot.Alpha);
                }
            }

            Assert.That(minHero, Is.EqualTo(1).Within(1e-4), "trabajo → trabajo: la vuelta en curso no funde");

            // Series 600 a los 541 s (descanso → enfriamiento): el vacío no se mueve.
            CreateSeries(DeviceProfile.TV, laps: true, reps: true);
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(540.5);
            var before = SlotNamed("hud-laps-empty").WorldRect;
            H.Advance(0.5);
            var moved = 0f;
            var faded = 1f;
            for (var frame = 0; frame < 20; frame++)
            {
                H.Step();
                yield return null;
                var empty = SlotNamed("hud-laps-empty");
                moved = Math.Max(moved, Math.Abs(empty.WorldRect.yMin - before.yMin));
                faded = Math.Min(faded, empty.Alpha);
            }

            Assert.That(moved, Is.LessThan(0.01f), "el vacío no se mueve");
            Assert.That(faded, Is.EqualTo(1).Within(1e-4), "ni funde");
            yield return Settle();
            Assert.That(Reps.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "repeticiones se fue con la serie");
        }

        // ── El panel que vuelve a un panel de UI Toolkit (QA-G5T-12) ──

        [UnityTest, Category("AC-WKT-18-18")]
        public IEnumerator ReattachGoesToTheFinalState()
        {
            CreateSeries(DeviceProfile.TV);
            yield return StartAt("Series 600", 250.0);
            H.TapText("500 m");
            for (var i = 0; i < 8; i++)
            {
                H.Step();
                yield return null;
            }

            Assert.That(Laps.Motion.IsRunning, Is.True, "a mitad del resorte");
            var parent = Laps.parent;
            var index = parent.IndexOf(Laps);
            Laps.RemoveFromHierarchy();
            yield return null;
            Assert.That(Laps.Motion.IsRunning, Is.False, "sin panel, nada sigue");
            H.Resize(DeviceProfile.TH);
            yield return Frames();
            parent.Insert(index, Laps);
            yield return Frames();
            Assert.That(Laps.Motion.IsRunning, Is.False, "nada anima lo que pasó sin panel");
            Assert.That(Laps.VisualHeightBase, Is.EqualTo(Laps.FinalHeightBase).Within(1e-6), "el alto en su estado final");
            Assert.That(Laps.Visual.MarkerAlpha0, Is.EqualTo(0), "sin marca");
            Assert.That(H.IsTextVisible("Parciales 500 m"), Is.True, "abierto, medido y dibujado");
        }

        // QA-V5-02 (verificación de la 5; H5-D2-01; ANIMATION_BIBLE §2.4): la línea Rep, anclada a la izquierda con su rótulo, rueda
        // por tramos. A los 203 s de Series 600 pasa de "Rep 2/3 · 8 m" a "Rep 2/3 · 12 m": ruedan el 8 → 2 y entra el 1; "Rep 2/3 · "
        // y " m" no ruedan (antes rodaba la línea entera, alineada a la derecha de toda la cadena). Lo mismo al empezar la
        // repetición: "Rep 1/3 · 680 m" → "Rep 2/3 · 4 m" solo rueda el número de la repetición y los metros.
        [UnityTest, Category("AC-WKT-18-19")]
        public IEnumerator TheRepLineRollsOnlyWhatChanges()
        {
            CreateSeries(DeviceProfile.TV, laps: true);
            yield return StartAt("Series 600", 200.2);
            var cells = Laps.RepLineCells;
            foreach (var step in new[] { ("Rep 1/3 · 680 m", "Rep 2/3 · 4 m"), ("Rep 2/3 · 8 m", "Rep 2/3 · 12 m") })
            {
                var seen = false;
                for (var i = 0; i < 200 && !seen; i++)
                {
                    var before = cells.Text;
                    H.Step();
                    yield return null;
                    if (before != step.Item1 || cells.Text != step.Item2)
                    {
                        continue;
                    }

                    seen = true;
                    Assert.That(cells.IsRolling, Is.True, step.Item2 + ": rueda");
                    Assert.That(cells.RollsByRuns, Is.True, step.Item2 + ": por tramos");
                    var rolling = "";
                    for (var c = 0; c < cells.Text.Length; c++)
                    {
                        rolling += cells.RollsCell(c) ? "^" : "_";
                    }

                    var expected = step.Item2 == "Rep 2/3 · 4 m" ? "____^_____^__" : "__________^^__";
                    Assert.That(rolling, Is.EqualTo(expected), step.Item1 + " → " + step.Item2 + ": solo ruedan las cifras que cambian");
                }

                Assert.That(seen, Is.True, "llegó " + step.Item2 + " (está " + cells.Text + ")");
            }
        }
    }
}
