using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // SCT · La columna de los paneles (05-series-parciales/02-tech-design §3.2 a §3.4; 04-design-notes §5): el toque POR LA SELECCIÓN
    // REAL (panel.Pick; QA-G5T-04): fuera de la caja de layout del chip, en un panel corrido por el FLIP, en la columna desplazada y bajo
    // la máscara; el arrastre que no abre; cuándo no responde; el desplazamiento (RowsScroll en W1280 y ColumnScroll en TH) con la
    // rueda, sin inercia y sin que lo que se ve salte; la zona del bote; la cuenta grande y PAUSA en su lugar; el único escritor del
    // translate.
    public sealed class SeriesColumnTests : SeriesUiTestBase
    {
        private static Vector2 Center(Rect rect)
        {
            return rect.center;
        }

        [UnityTest, Category("AC-WKT-18-09"), Category("AC-WKT-13-07")]
        public IEnumerator TapsGoThroughTheRealSelection()
        {
            CreateSeries(DeviceProfile.TV);
            yield return StartAt("Series 600", 60.2);
            var chip = Laps.worldBound;
            // Un toque a 3 dp del borde de abajo de `500 m`, fuera de su caja de layout y dentro de su área de 56 dp.
            var picked = H.TapAt(new Vector2(chip.center.x, chip.yMax + 3));
            yield return Frames();
            Assert.That(picked, Is.SameAs(Laps), "lo elige parciales por su área de toque (no " + UiQuery.Describe(picked) + ")");
            Assert.That(Column.Expanded.Laps, Is.True, "abre 500 m");
            Assert.That(Column.Expanded.Reps, Is.False, "y no Reps");
            Assert.That(H.Preferences.TryGetBool(SeriesPanelPrefs.LapsKey, out var saved) && saved, Is.True, "y lo guarda en el acto");

            // A mitad de abrir parciales, repeticiones corrido por el FLIP se elige donde se ve.
            yield return SettlePanels();
            H.TapText("Parciales 500 m");
            for (var i = 0; i < 6; i++)
            {
                H.Step();
                yield return null;
            }

            var reps = Reps.worldBound;
            Assert.That(Reps.resolvedStyle.translate.y, Is.Not.EqualTo(0).Within(0.5f), "corrido por el FLIP");
            picked = H.TapAt(reps.center);
            Assert.That(picked, Is.SameAs(Reps), "repeticiones donde se ve (no " + UiQuery.Describe(picked) + ")");
            Assert.That(Column.Expanded.Reps, Is.True, "abre Reps");
        }

        [UnityTest, Category("AC-WKT-18-09"), Category("AC-WKT-18-13")]
        public IEnumerator ADragDoesNotToggleAndATapDoes()
        {
            CreateSeries(DeviceProfile.TV);
            yield return StartAt("Series 600", 60.2);
            var center = Laps.worldBound.center;
            H.DragAt(center, center + new Vector2(0, 20), 6);
            yield return Settle();
            Assert.That(Column.Expanded.Laps, Is.False, "un arrastre de 20 dp no abre");
            H.PointerTapAt(center);
            yield return Settle();
            Assert.That(Column.Expanded.Laps, Is.True, "un toque con el puntero (menos de 8 dp) abre");
            Assert.That(Laps.Visual.HoverAlpha, Is.LessThan(0.05), "la capa de estado vuelve (0,04 con el mouse encima, 0 sin él)");
        }

        [UnityTest, Category("AC-WKT-18-20"), Category("AC-WKT-13-07"), Category("AC-WKT-13-12")]
        public IEnumerator WhenThePanelsRespond()
        {
            // Antes de los 520 ms de la cascada: no.
            CreateSeries(DeviceProfile.TV);
            yield return OpenStartDialog("Series 600");
            yield return Frames();
            H.Tap("dialog-confirm");
            H.Advance(0.4);
            yield return Frames();
            Assert.That(Immersive.HasView, Is.True, "el HUD ya se armó (a los 230)");
            Assert.That(Column.IsInteractive, Is.False, "antes de su tramo");
            H.TapAt(Laps.worldBound.center);
            Assert.That(Column.Expanded.Laps, Is.False, "un toque a los 400 ms no abre");
            H.Advance(0.2);
            yield return Frames();
            Assert.That(Column.IsInteractive, Is.True, "desde los 520 ms");
            yield return Settle();

            // En pausa sí.
            yield return AdvanceTo(60.2);
            yield return Tap("hud-pause");
            H.TapAt(Laps.worldBound.center);
            yield return SettlePanels();
            Assert.That(Column.Expanded.Laps, Is.True, "en pausa se abre");
            Assert.That(H.IsShown("hud-paused-label"), Is.True, "PAUSA en su lugar con el panel abierto");
            Assert.That(Overlap(VisualRect(Laps), H.BoundsOf("hud-paused-label")), Is.False, "sin encimarse");

            // Con un diálogo abierto, no.
            yield return Tap("hud-finish");
            Assert.That(H.TopDialog, Is.Not.Null);
            H.TapAt(Laps.worldBound.center);
            Assert.That(Column.Expanded.Laps, Is.True, "con el diálogo, el toque no llega (lo recibe el velo)");
            yield return Settle();
            if (H.TopDialog != null)
            {
                yield return Tap("dialog-cancel");
            }

            // Durante la salida, no (la página que sale ya no es la de arriba: la columna se guarda antes).
            var column = Column;
            yield return Tap("hud-finish");
            H.Tap("dialog-confirm");
            H.Advance(0.05);
            yield return Frames();
            Assert.That(column.IsInteractive, Is.False, "inerte en la salida");
        }

        [UnityTest, Category("AC-WKT-18-13"), Category("AC-WKT-18-14"), Category("AC-WKT-17-09")]
        public IEnumerator ColumnScrollInLandscape()
        {
            CreateSeries(DeviceProfile.TH, laps: true, reps: true, configure: o => o.BoatZoneOutline = true);
            yield return StartAt("Series 600", 380.2);
            Assert.That(Column.Column.Rule, Is.EqualTo(SeriesFitRule.ColumnScroll));
            Assert.That(Column.ScrollBase, Is.EqualTo(0), "empieza arriba");
            Assert.That(Column.ScrollMaxBase, Is.GreaterThan(0), "algo escondido debajo del piso");
            var floor = (float)Column.Column.Floor;
            // Repeticiones empieza debajo del piso: un punto de su caja bajo la máscara no lo elige.
            var reps = Reps.worldBound;
            Assert.That(reps.yMin, Is.GreaterThan(floor - 1), "repeticiones debajo del piso");
            Assert.That(H.Root.panel.Pick(new Vector2(reps.center.x, Math.Min(reps.yMin + 5, H.Root.worldBound.yMax - 1))), Is.Not.SameAs(Reps),
                "lo que queda bajo la máscara no responde");

            // La rueda de a una fila, con spring.snappy.
            H.Wheel(Laps.worldBound.center, 1);
            yield return Settle();
            Assert.That(Column.ScrollBase, Is.EqualTo(Math.Min(22, Column.ScrollMaxBase)).Within(0.05), "una fila (22 base)");
            // El arrastre 1:1 hacia arriba lleva al tope, sin inercia.
            var center = Laps.worldBound.center;
            H.DragAt(center, center + new Vector2(0, -300), 6);
            yield return Frames();
            var after = Column.ScrollBase;
            Assert.That(after, Is.EqualTo(Column.ScrollMaxBase).Within(0.05), "dentro de sus topes");
            H.Advance(0.5);
            yield return Frames();
            Assert.That(Column.ScrollBase, Is.EqualTo(after).Within(1e-6), "sin inercia: queda donde se soltó");
            // Ahora repeticiones se ve y responde donde se ve.
            var repsNow = Reps.worldBound;
            var picked = H.Root.panel.Pick(new Vector2(repsNow.center.x, Math.Max(repsNow.yMin + 4, (float)Column.Column.Top(false) + 4)));
            Assert.That(picked, Is.SameAs(Reps), "repeticiones desplazado se elige donde se ve (no " + UiQuery.Describe(picked) + ")");
            // En horizontal, los paneles nunca entran en la zona del bote.
            foreach (var panel in new[] { Laps, Reps })
            {
                var r = VisualRect(panel);
                Assert.That(SeriesPanelGeometry.RespectsBoatZone(new DpRect(r.x, r.y, r.width, r.height), true, Immersive.Layout!), Is.True, panel.name);
            }

            // Al rotar, a 0 y en el acto.
            H.Resize(DeviceProfile.TV);
            yield return Frames(3);
            Assert.That(Column.ScrollBase, Is.EqualTo(0), "al rotar vuelve a 0");
            Assert.That(Column.Column.Rule, Is.EqualTo(SeriesFitRule.FitAll));
            Assert.That(Laps.Motion.IsRunning, Is.False, "en el acto, sin animación");
        }

        // LM5-01 (verificación de la 5, ronda 1): en ColumnScroll el desplazamiento se recorta cuando el contenido se achica por un
        // dato. TH y L640 con los dos abiertos a los 380,2 s (CAP-16), la columna arrastrada al tope (para ver Series anteriores) y
        // la serie que termina a los 541 s: repeticiones se va y parciales pasa al vacío (84 base, el tope a 0). Antes el
        // desplazamiento quedaba en 86,1 y parciales entero arriba del techo, sin toque, sin arrastre y sin rueda.
        [UnityTest, Category("AC-WKT-18-13"), Category("AC-WKT-18-20")]
        public IEnumerator TheColumnScrollFollowsAColumnThatShrinks()
        {
            foreach (var profile in new[] { DeviceProfile.TH, DeviceProfile.L640 })
            {
                CreateSeries(profile, laps: true, reps: true);
                yield return StartAt("Series 600", 380.2);
                Assert.That(Column.Column.Rule, Is.EqualTo(SeriesFitRule.ColumnScroll), profile.Code);
                var center = Laps.worldBound.center;
                H.DragAt(center, center + new Vector2(0, -300), 6);
                yield return Frames();
                Assert.That(Column.ScrollBase, Is.GreaterThan(80), profile.Code + ": al tope (86,1)");

                yield return AdvanceTo(541.2);
                yield return Settle();
                Assert.That(Column.RepsShown, Is.False, profile.Code + ": la serie terminó");
                Assert.That(Column.ScrollMaxBase, Is.EqualTo(0).Within(1e-6), profile.Code + ": todo entra");
                Assert.That(Column.ScrollBase, Is.LessThanOrEqualTo(Column.ScrollMaxBase + 0.05), profile.Code + ": dentro de su tope");
                var sp = (float)Column.Column.Scale;
                var glass = VisualRect(Laps);
                var ceiling = (float)Column.Column.Top(Column.SpmHasTarget);
                Assert.That(glass.yMin, Is.GreaterThanOrEqualTo(ceiling - 0.5f), profile.Code + ": la cabecera de parciales, debajo del techo (" + Describe(Laps) + ")");
                Assert.That(glass.yMin + 34 * sp, Is.LessThanOrEqualTo((float)Column.Column.Floor + 0.5f), profile.Code + ": y arriba del piso");
                var header = new Vector2(glass.center.x, glass.yMin + 17 * sp);
                var picked = H.Root.panel.Pick(header);
                Assert.That(picked, Is.SameAs(Laps), profile.Code + ": la cabecera responde donde se ve (no " + UiQuery.Describe(picked) + ")");
                Assert.That(SlotNamed("hud-laps-title").Alpha, Is.GreaterThan(0.99f), profile.Code + ": y se ve (la máscara no la apaga)");
                DisposeHost();
            }
        }

        // LM5-01 (b): cerrar repeticiones con la columna al tope achica la columna (el tope pasa de 86,1 a 38,1). El recorte no salta
        // en el cuadro del toque (antes, 48 base = 50,6 dp en TH): va con spring.snappy, como la rueda.
        [UnityTest, Category("AC-WKT-18-13"), Category("AC-WKT-18-18")]
        public IEnumerator ClosingAPanelWithTheColumnAtTheTopDoesNotJump()
        {
            CreateSeries(DeviceProfile.TH, laps: true, reps: true);
            yield return StartAt("Series 600", 380.2);
            var center = Laps.worldBound.center;
            H.DragAt(center, center + new Vector2(0, -300), 6);
            yield return Frames();
            var top = Column.ScrollBase;
            Assert.That(top, Is.GreaterThan(80), "al tope");
            // El desplazamiento que se dibuja (la traslación de los dos paneles es −ScrollBase·sp), leído en el acto: el cuadro del
            // toque cuenta.
            var sp = Column.Column.Scale;
            var before = Column.ScrollBase * sp;
            var repsNow = Reps.worldBound;
            var picked = H.TapAt(new Vector2(repsNow.center.x, Math.Max(repsNow.yMin + 17 * (float)Reps.Scale, (float)Column.Column.Top(false) + 4)));
            Assert.That(picked, Is.SameAs(Reps), "el toque en la cabecera de repeticiones");
            Assert.That(Column.Expanded.Reps, Is.False, "cierra");
            Assert.That(Column.ScrollMaxBase, Is.LessThan(top - 40), "el tope bajó");
            var worst = Math.Abs(Column.ScrollBase * sp - before);
            before = Column.ScrollBase * sp;
            for (var i = 0; i < 50; i++)
            {
                H.Step();
                yield return null;
                var now = Column.ScrollBase * sp;
                worst = Math.Max(worst, Math.Abs(now - before));
                before = now;
            }

            yield return Settle();
            Assert.That(worst, Is.LessThan(12), "ningún cuadro salta (el peor, " + worst.ToString("0.0") + " dp)");
            Assert.That(Column.ScrollBase, Is.EqualTo(Column.ScrollMaxBase).Within(0.05), "y termina en el tope nuevo");
            Assert.That(Laps.resolvedStyle.translate.y, Is.EqualTo((float)(-Column.ScrollBase * sp)).Within(0.05f), "lo que se dibuja es ese desplazamiento");
        }

        // LM5-01 (b) en RowsScroll (W1280): las filas de un panel se recortan sin saltar. (1) Con las filas al tope y 4 vueltas a la
        // vista, una vuelta nueva no puede sumar 22 al desplazamiento (DD5-27 no tiene lugar: la más vieja sale por abajo): las filas
        // bajan con el empuje, sin el salto de 22·sp de compensar y recortar en el mismo cuadro. (2) Cerrar el panel con las filas
        // desplazadas las lleva a 0 con spring.snappy mientras el contenido se apaga, no en el cuadro del toque.
        [UnityTest, Category("AC-WKT-18-13"), Category("AC-WKT-18-18"), Category("AC-WKT-18-19")]
        public IEnumerator TheRowsScrollIsClampedWithoutJumps()
        {
            CreateSeries(DeviceProfile.W1280, laps: true, reps: true);
            yield return StartAt("Series 600", 340.2);
            var largo = SeriesSeeds.Largo(H.App.Texts.Value, H.HudState!.Session);
            H.OverrideSeries(largo);
            yield return Settle();
            for (var i = 0; i < 6; i++)
            {
                H.Wheel(Laps.worldBound.center, 1);
            }

            yield return Settle();
            var max = Laps.RowsScrollMaxBase;
            Assert.That(max, Is.GreaterThan(20), "las filas se desplazan");
            Assert.That(Laps.RowsScrollBase, Is.EqualTo(max).Within(0.05), "al tope");

            // (1) La vuelta 2000 con las filas al tope: el #1997 (la fila 2) pasa a ser la fila 3 y baja con el empuje.
            var watched = SlotNamed("hud-laps-row-2");
            var newer = new HudLapRow[4];
            newer[0] = SeriesSeeds.Lap(H.App.Texts.Value, 2000, SeriesSeeds.LongLapSeconds);
            for (var i = 1; i < 4; i++)
            {
                newer[i] = largo.Laps[i - 1];
            }

            H.OverrideSeries(largo with { Laps = newer, LapCount = 2000 }, instant: false);
            var y = watched.WorldRect.center.y;
            var worst = 0f;
            for (var i = 0; i < 30; i++)
            {
                H.Step();
                yield return null;
                var now = SlotNamed("hud-laps-row-3").WorldRect.center.y;
                worst = Math.Max(worst, Math.Abs(now - y));
                y = now;
            }

            yield return Settle();
            Assert.That(worst, Is.LessThan(8f), "la fila que se veía no salta (el peor cuadro, " + worst.ToString("0.0") + " dp)");
            Assert.That(Laps.RowsScrollBase, Is.EqualTo(Laps.RowsScrollMaxBase).Within(0.05), "sigue al tope");

            // (2) Cerrar parciales con las filas al tope.
            Assert.That(Laps.RowsScrollBase, Is.GreaterThan(20));
            var row = SlotNamed("hud-laps-row-3").WorldRect.center.y;
            H.TapText("Parciales 500 m");
            Assert.That(Column.Expanded.Laps, Is.False, "cierra");
            worst = 0f;
            for (var i = 0; i < 8; i++)
            {
                H.Step();
                yield return null;
                var now = SlotNamed("hud-laps-row-3").WorldRect.center.y;
                worst = Math.Max(worst, Math.Abs(now - row));
                row = now;
            }

            yield return SettlePanels();
            Assert.That(worst, Is.LessThan(12f), "las filas no saltan a 0 en el cuadro del toque (el peor cuadro, " + worst.ToString("0.0") + " dp)");
            Assert.That(Laps.RowsScrollBase, Is.EqualTo(0).Within(0.05), "cerrado, las filas vuelven a 0");
        }

        [UnityTest, Category("AC-WKT-18-13")]
        public IEnumerator RowsScrollOnTheDesktop()
        {
            CreateSeries(DeviceProfile.W1280, laps: true, reps: true);
            yield return StartAt("Series 600", 340.2);
            Assert.That(Column.Column.Rule, Is.EqualTo(SeriesFitRule.RowsScroll));
            Assert.That(Column.Column.Scale, Is.EqualTo(1.20).Within(0.005), "sp 1,20 (DD5-03)");
            var largo = SeriesSeeds.Largo(H.App.Texts.Value, H.HudState!.Session);
            H.OverrideSeries(largo);
            yield return Settle();
            // DD5-26: 4 vueltas + 3 repeticiones sin objetivo: parciales 137,2 (2 filas) y repeticiones 60 (1 fila).
            Assert.That(Laps.FinalHeightBase, Is.EqualTo(137.2).Within(0.1));
            Assert.That(Reps.FinalHeightBase, Is.EqualTo(60).Within(0.1));
            Assert.That(Laps.RowsScrollMaxBase, Is.GreaterThan(0), "las filas de parciales se desplazan");
            H.Wheel(Laps.worldBound.center, 1);
            yield return Settle();
            Assert.That(Laps.RowsScrollBase, Is.EqualTo(22).Within(0.05), "la rueda de a una fila");

            // DD5-27: con la vista desplazada, una vuelta nueva entra arriba, fuera de la vista, y lo que se ve no se mueve. Con 3
            // vueltas a la vista (la lista crece a 4: hay lugar para sumar 22 al desplazamiento sin pasar el tope).
            var three = new HudLapRow[3];
            for (var i = 0; i < 3; i++)
            {
                three[i] = largo.Laps[i];
            }

            H.OverrideSeries(largo with { Laps = three, LapCount = 1999 });
            yield return Settle();
            H.Wheel(Laps.worldBound.center, 1);
            yield return Settle();
            var scrolled = Laps.RowsScrollBase;
            Assert.That(scrolled, Is.GreaterThan(1), "desplazada");
            var visibleBefore = SlotNamed("hud-laps-row-1").WorldRect.center.y;
            var newer = new HudLapRow[4];
            newer[0] = SeriesSeeds.Lap(H.App.Texts.Value, 2000, SeriesSeeds.LongLapSeconds);
            for (var i = 1; i < 4; i++)
            {
                newer[i] = largo.Laps[i - 1];
            }

            H.OverrideSeries(largo with { Laps = newer, LapCount = 2000 });
            var maxMove = 0f;
            for (var i = 0; i < 20; i++)
            {
                H.Step();
                yield return null;
                maxMove = Math.Max(maxMove, Math.Abs(SlotNamed("hud-laps-row-2").WorldRect.center.y - visibleBefore));
            }

            Assert.That(maxMove, Is.LessThan(0.6f), "la fila que se veía no salta (DD5-27)");
            Assert.That(Laps.RowsScrollBase, Is.EqualTo(scrolled + 22).Within(0.05), "el desplazamiento sumó 22");
        }

        [UnityTest, Category("AC-WKT-18-15"), Category("AC-WKT-14-18"), Category("AC-WKT-13-12")]
        public IEnumerator TheBigCountStaysInItsPlace()
        {
            CreateSeries(DeviceProfile.TV, laps: true);
            yield return StartAt("Series 600", 197.6);
            Assert.That(H.IsShown("hud-big-countdown"), Is.True, "la cuenta grande con parciales abierto (P-04 sin efecto con 137)");
            var disc = Hud.Callouts.Disc;
            var panel = VisualRect(Laps);
            Assert.That(disc.Left - panel.xMax, Is.EqualTo(8.4).Within(0.5), "a 8,4 dp del panel (S5-10)");
            Assert.That(Hud.Callouts.BlockedByPanels, Is.False);
            // Con la regla forzada (los 200 de la spec): el disco no se muestra.
            Hud.Callouts.SetBlockedByPanels(true, true);
            yield return Settle();
            Assert.That(H.IsShown("hud-big-countdown"), Is.False, "con un panel encima, la cuenta grande no se muestra");
            Hud.Callouts.SetBlockedByPanels(false, true);
            yield return Settle();
            Assert.That(H.IsShown("hud-big-countdown"), Is.True);
        }

        [UnityTest, Category("AC-WKT-18-18")]
        public IEnumerator TheLowerPanelFollowsTheUpperOneByTranslate()
        {
            CreateSeries(DeviceProfile.TV, reps: true);
            yield return StartAt("Series 600", 250.0);
            H.TapText("500 m");
            var sawFlip = false;
            for (var i = 0; i < 40; i++)
            {
                H.Step();
                yield return null;
                var expected = (float)((Laps.VisualHeightBase - Laps.FinalHeightBase) * Laps.Scale);
                var actual = Reps.resolvedStyle.translate.y;
                Assert.That(actual, Is.EqualTo(expected).Within(0.05f), "cuadro " + i + ": el FLIP = (G − H) · sp");
                sawFlip |= expected < -10;
                // El aire de 8·sp entre los dos, en cada cuadro.
                var gap = Reps.worldBound.yMin - (Laps.worldBound.yMin + (float)(Laps.VisualHeightBase * Laps.Scale));
                Assert.That(gap, Is.EqualTo(8 * Laps.Scale).Within(0.5), "cuadro " + i + ": el aire (± 0,5: el layout va al píxel)");
            }

            Assert.That(sawFlip, Is.True, "llegó a correrlo (hasta −59 dp en VID-01)");
        }
    }
}
