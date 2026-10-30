using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // PRT · La fila de píldoras montada (F-WKT-15; AC-WKT-14-07 a -13, -16, -17; AC-WKT-15-01 a -05; LT-46;
    // 04b-hud-exr/02-tech-design §2.1 a §2.3, §3.4, §6.3): UN elemento que dibuja todo; las pruebas leen cada píldora por índice
    // (DT4b-01). Estados con R-4: a los 40,2 s la 0 cumplida, la 1 no, la 2 (descanso) sin objetivo, la 3 en curso y la 4
    // pendiente: los cuatro aspectos (los cinco casos del legacy) en una sola fila.
    public sealed class PillRowTests : WorkoutUiTestBase
    {
        // legacy: test/step_pill_test.dart:19 'pendiente, en curso, hecho y no cumplido se pintan distinto'
        [UnityTest, Category("LT-46"), Category("AC-WKT-15-02"), Category("AC-WKT-15-03"), Category("AC-WKT-15-04"), Category("AC-WKT-15-05")]
        public IEnumerator PendingActiveDoneAndMissedPaintDifferently()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(40.2);
            var row = Hud.Steps!.PillRow;
            Assert.That(row.Count, Is.EqualTo(5), "una píldora por paso");
            // El mapeo del legacy (:20-26), con LookOf en la app: d = cumplida, m = no cumplida, n = sin resultado, a = en curso,
            // p = pendiente.
            Assert.That(row.LookAt(0), Is.EqualTo(StepPillLook.Solid), "d: cumplida");
            Assert.That(row.LookAt(1), Is.EqualTo(StepPillLook.Hollow), "m: no cumplida");
            Assert.That(row.LookAt(2), Is.EqualTo(StepPillLook.Solid), "n: sin resultado = cumplida");
            Assert.That(row.LookAt(3), Is.EqualTo(StepPillLook.Current), "a: en curso");
            Assert.That(row.LookAt(4), Is.EqualTo(StepPillLook.Pending), "p: pendiente");
            var done = row.TrackColorOf(0).a;
            var missed = row.TrackColorOf(1).a;
            var noResult = row.TrackColorOf(2).a;
            var active = row.TrackColorOf(3).a;
            var pending = row.TrackColorOf(4).a;
            Assert.That(done, Is.GreaterThan(0.8f), ":35 la hecha con alfa > 0,8");
            Assert.That(noResult, Is.EqualTo(done).Within(1e-4), ":36 la hecha sin resultado igual a la cumplida");
            Assert.That(pending, Is.LessThan(0.3f), ":37 la pendiente con alfa < 0,3");
            Assert.That(missed, Is.EqualTo(pending).Within(1e-4), ":40 el interior de la no cumplida con el alfa de la pendiente");
            Assert.That(StepPills.Paint(row.LookAt(1), 0).BorderAlpha, Is.GreaterThan(0.8), ":41-42 el borde de la no cumplida");
            Assert.That(StepPills.Paint(row.LookAt(4), 0).BorderWidthBase, Is.EqualTo(0), ":43 la pendiente sin borde");
            Assert.That(StepPills.Paint(row.LookAt(0), 0).BorderWidthBase, Is.EqualTo(0), ":44 la hecha sin borde");
            Assert.That(active, Is.GreaterThan(pending), ":47 la en curso más que la pendiente");
            Assert.That(active, Is.LessThan(done), ":48 y menos que la hecha");
            Assert.That(row.Fill, Is.EqualTo(0.510).Within(0.005), ":49-50 el relleno de la en curso (0,510 a los 40,2 s, CAP-01)");
            Assert.That(StepPills.Paint(row.LookAt(3), row.Fill).BorderWidthBase, Is.EqualTo(0), ":51 la en curso sin borde");
            Assert.That(StepPills.Paint(row.LookAt(4), 0).HasFill, Is.False, ":52 la pendiente sin relleno");
        }

        [UnityTest, Category("AC-WKT-14-07"), Category("AC-WKT-14-08"), Category("AC-WKT-15-01")]
        public IEnumerator OnePillPerStepSameWidth()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(40.2);
            var row = Hud.Steps!.PillRow;
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(row.layout.height, Is.EqualTo((float)PillRowGeometry.RowHeight).Within(0.5f), "la fila mide 20 base (± el redondeo a píxeles)");
            var ppp = row.scaledPixelsPerPoint * s;
            var first = row.RectOf(0).width * ppp;
            for (var i = 1; i < row.Count; i++)
            {
                Assert.That(row.RectOf(i).width * ppp, Is.EqualTo(first).Within(1.01f), "todas del mismo ancho ± 1 px (" + i + ")");
                Assert.That(row.RectOf(i).height, Is.EqualTo((float)PillRowGeometry.Height).Within(0.01f), "8 base de alto");
            }

            Assert.That(row.RectOf(0).xMin, Is.EqualTo((float)PillRowGeometry.PadLeft).Within(0.5f), "10 a la izquierda");
            Assert.That(row.RectOf(0).yMin, Is.EqualTo((float)PillRowGeometry.PadTop).Within(0.01f), "8 arriba");
        }

        [UnityTest, Category("AC-WKT-14-13"), Category("AC-WKT-14-09")]
        public IEnumerator LastStepStaysCurrent()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(30.25);
            yield return Settle();
            var row = Hud.Steps!.PillRow;
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Finished), "la rutina terminó sola");
            Assert.That(row.LookAt(3), Is.EqualTo(StepPillLook.Current), "la última queda en curso (Q-52)");
            Assert.That(row.Fill, Is.EqualTo(1).Within(0.005), "llena, con su brillo");
            Assert.That(row.LookAt(0), Is.EqualTo(StepPillLook.Solid));
            Assert.That(row.LookAt(1), Is.EqualTo(StepPillLook.Solid), "el descanso sin objetivo, sólido");
        }

        [UnityTest, Category("AC-WKT-14-16")]
        public IEnumerator TwoHundredStepsFitTheBar()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(2.2);
            var row = Hud.Steps!.PillRow;
            var layout = row.CurrentLayout;
            Assert.That(layout.Count, Is.EqualTo(200));
            Assert.That(layout.Strip, Is.True, "en TV, 200 pasos son la tira (DV-40)");
            Assert.That(layout.Narrow, Is.True, "la en curso es un pulgar");
            var thumb = layout.ThumbOf(0);
            Assert.That(thumb.Width, Is.GreaterThanOrEqualTo(PillRowGeometry.ThumbMinWidth - 1e-6), "el pulgar de al menos 4");
            Assert.That(thumb.Height, Is.EqualTo(PillRowGeometry.ThumbHeight).Within(1e-6), "y 10 de alto");
            Assert.That(row.worldBound.xMax, Is.LessThanOrEqualTo(Bounds("hud-topbar").xMax + 0.5f), "nunca sale de la barra");
        }

        [UnityTest, Category("AC-WKT-14-17")]
        public IEnumerator NoInteractionsNorTab()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(2.2);
            var row = Hud.Steps!.PillRow;
            Assert.That(row.pickingMode, Is.EqualTo(PickingMode.Ignore), "sin toques");
            Assert.That(row.focusable, Is.False, "fuera del anillo de Tab");
            Assert.That(row.childCount, Is.EqualTo(0), "un solo elemento: lo dibuja todo su generateVisualContent");
        }

        [UnityTest, Category("AC-WKT-14-11")]
        public IEnumerator HaloOnlyWhileEndingAndKeepsPulsingInPause()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(2.2);
            Assert.That(Hud.Steps!.PillRow.HaloAlpha, Is.EqualTo(0).Within(1e-6), "sin halo fuera de los últimos 5 s");
            yield return AdvanceTo(7.6);
            Assert.That(Hud.Steps.PillRow.HaloAlpha, Is.GreaterThan(0.29), "el halo de los últimos 5 s");
            yield return Tap("hud-pause");
            var before = Hud.Steps.PillRow.HaloAlpha;
            H.Advance(0.4);
            yield return Frames();
            Assert.That(Hud.Steps.PillRow.HaloAlpha, Is.Not.EqualTo(before).Within(1e-4), "en pausa sigue latiendo");
        }

        // QA-V4b-03 (H-D2-01; 04-design-notes §3.2: "la pista, el relleno y el brillo de siempre"): en la tira, la pista del
        // pulgar (step.work al 30 %) queda sobre el vidrio y no sobre los tramos de abajo. A los 15,2 s de Serie larga el pulgar de la
        // 0 (4 de ancho, pegado al borde izquierdo) tapa la 1 y parte de la 2 (descansos pendientes al 18 %); el relleno llega a la
        // mitad y su quinto derecho es pista. Antes se veía la pista sobre el tramo pendiente (CAP-22: diferencia 34).
        [UnityTest, Category("AC-WKT-14-16")]
        public IEnumerator ThumbTrackSitsOnTheGlass()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Serie larga");
            yield return AdvanceTo(15.2);
            yield return Frames(2);
            var row = Hud.Steps!.PillRow;
            Assert.That(row.Fill, Is.EqualTo(0.507).Within(0.01), "el relleno a la mitad del pulgar");
            Assert.That(row.CurrentLayout.Strip, Is.True, "la tira");
            var glass = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky);
            var frame = H.ReadPixels();
            try
            {
                var probes = new[]
                {
                    PixelProbe.Point("vidrio de la fila", "hud-topbar", new Vector2(0.5f, 0.92f), glass, 1),
                    PixelProbe.PointIn("pulgar: pista step.work @30 % sobre el vidrio", "hud-pill-row", e => ThumbRect(e, 0), new Vector2(0.8f, 0.5f),
                        Over(Tokens.StepWork, StepPills.CurrentTrackAlpha, glass), 2),
                    PixelProbe.PointIn("pulgar: relleno step.work", "hud-pill-row", e => ThumbRect(e, 0), new Vector2(0.25f, 0.5f), Tokens.StepWork.Rgb.ToHex(), 3),
                };
                foreach (var probe in probes)
                {
                    var result = probe.Measure(H.Root, frame, H.Profile.Density);
                    Assert.That(result.Pass, Is.True, result.ToString());
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(frame);
            }
        }

        // QA-V4b-04 (H-D2-03): el relleno de la en curso no tiene agujeros. La teselación de Painter2D del polígono recortado dejaba
        // una grieta (un píxel del color de la pista dentro del relleno: CAP-27, la 3 a los 47,6 s con la pausa, en (724, 58) px; con
        // el contorno sin vértices repetidos se corrió a (678, 51), justo debajo del brillo). Se recorren todas las filas del
        // relleno desde la de abajo del brillo, sin los bordes con su suavizado, en marcha (CAP-01) y en pausa (CAP-27).
        [UnityTest, Category("AC-WKT-15-03"), Category("AC-WKT-14-10")]
        public IEnumerator CurrentFillHasNoHoles()
        {
            CreateWorkout(RoutineSeeds.F4B);
            yield return StartRoutine("Estados");
            yield return AdvanceTo(40.2);
            yield return Frames(2);
            var holes = FillHoles(3, "en marcha (CAP-01)");
            yield return AdvanceTo(47.0);
            yield return Tap("hud-pause");
            yield return AdvanceTo(47.6);
            yield return Frames(2);
            holes += FillHoles(3, "en pausa (CAP-27)");
            Assert.That(holes, Is.Empty, "píxeles del relleno que no son step.work");
        }

        // Los píxeles del relleno de la píldora n (sin el brillo de arriba ni los bordes suavizados) que se alejan de step.work.
        private string FillHoles(int index, string label)
        {
            var row = Hud.Steps!.PillRow;
            var density = H.Profile.Density;
            var local = row.RectOf(index);
            var fillWidth = (float)(row.Fill * local.width);
            var highlight = (float)MotionSpecs.PillHighlightDp;
            var inner = new Rect(local.x + (float)PillRowGeometry.Radius, local.y + highlight, fillWidth - (float)PillRowGeometry.Radius, local.height - highlight);
            var world = row.LocalToWorld(inner);
            // Desde el píxel siguiente al suavizado de cada borde (medio píxel a cada lado del borde): debajo del brillo, hasta el
            // corte recto y hasta el borde de abajo.
            int Start(double edgePx) => (int)Math.Floor(edgePx + 0.5) + 1;
            int End(double edgePx) => (int)Math.Floor(edgePx - 0.5);
            var pixels = new RectInt(Start(world.xMin * density), Start(world.yMin * density), 0, 0);
            pixels.xMax = End(world.xMax * density);
            pixels.yMax = End(world.yMax * density);
            var expected = Tokens.StepWork.Rgb;
            var frame = H.ReadPixels();
            var found = new System.Text.StringBuilder();
            var count = 0;
            try
            {
                for (var y = pixels.yMin; y < pixels.yMax; y++)
                {
                    for (var x = pixels.xMin; x < pixels.xMax; x++)
                    {
                        var c = RtReader.PixelTopLeft(frame, x, y);
                        var d = Math.Max(Math.Abs(c.r - expected.R), Math.Max(Math.Abs(c.g - expected.G), Math.Abs(c.b - expected.B)));
                        if (d > 3)
                        {
                            count++;
                            if (count <= 5)
                            {
                                found.Append(label).Append(" (").Append(x).Append(", ").Append(y).Append(") ").Append(new Rgb(c.r, c.g, c.b).ToHex()).Append("; ");
                            }
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(frame);
            }

            Assert.That(pixels.width * pixels.height, Is.GreaterThan(500), label + ": se recorrió el relleno");
            return count == 0 ? "" : count + " " + found;
        }

        // El pulgar de la n en coordenadas del panel.
        private static Rect ThumbRect(VisualElement element, int index)
        {
            if (!(element is RowMate.Presentation.Workout.PillRowView row))
            {
                return Rect.zero;
            }

            var thumb = row.CurrentLayout.ThumbOf(index);
            return row.LocalToWorld(new Rect((float)thumb.Left, (float)thumb.Top, (float)thumb.Width, (float)thumb.Height));
        }

        // Un token con alfa sobre un color ya compuesto, en sRGB (la tubería GammaRt), como las sondas del arnés.
        private static string Over(ColorToken top, double alpha, string underHex)
        {
            var t = top.Rgb;
            var u = Rgb.Parse(underHex);
            return new Rgb(Mix(t.R, u.R, alpha), Mix(t.G, u.G, alpha), Mix(t.B, u.B, alpha)).ToHex();
        }

        private static byte Mix(byte a, byte b, double alpha)
        {
            return (byte)Math.Floor(a * alpha + b * (1 - alpha) + 0.5);
        }
    }
}
