using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // HLT · La geometría del HUD en la pantalla real (AC-WKT-17-08, -09, -10; DV-33; 04-design-notes §3.6-§3.8; 02-tech-design
    // §6.4 de la It. 4a). Los valores largos (CAP-16) y los extremos de FTMS llegan del remo de prueba; el reloj de 1:02:05 o de
    // 9:59:59 es un estado del HUD sintético (sin correr horas de motor): se aplica con Instant y se rehace la disposición.
    public sealed class HudLayoutTests : WorkoutUiTestBase
    {
        private static readonly DeviceProfile[] Profiles = { DeviceProfile.TV, DeviceProfile.TH, DeviceProfile.L640, DeviceProfile.W1280 };

        private static readonly string[] Pieces = { "hud-topbar", "hud-spm", "hud-split", "hud-watts", "hud-kcal", "hud-bpm", "hud-band", "hud-finish", "hud-pause" };

        private enum Case
        {
            Free,
            FreeWithPulse,
            RoutineWithTargets,
            Long,
            Extremes,
        }

        private static Func<int, RowingData> ReadingFor(Case c)
        {
            switch (c)
            {
                case Case.Long:
                    return RowerScripts.Long;
                case Case.Extremes:
                    return RowerScripts.Extremes;
                case Case.Free:
                    return RowerScripts.R4;
                default:
                    return k => RowerScripts.R4(k).CopyWith(heartRate: 140);
            }
        }

        private IEnumerator Arrange(DeviceProfile profile, Case c)
        {
            CreateWorkout(RoutineSeeds.F4A, o => o.Profile = profile, ReadingFor(c));
            if (c == Case.RoutineWithTargets)
            {
                yield return StartRoutine("Prueba 4a");
                yield return AdvanceTo(50.05);
            }
            else
            {
                yield return StartFree();
                yield return AdvanceTo(3.05);
            }

            if (c == Case.Long)
            {
                yield return ApplyClock(3725);
            }
            else if (c == Case.Extremes)
            {
                yield return ApplyClock(35999);
            }

            yield return Settle();
        }

        // El reloj de la banda en un estado sintético (Instant: sin rodillos) y la disposición rehecha con sus anchos.
        private IEnumerator ApplyClock(long seconds)
        {
            var state = Immersive.Hud.State.Value with { Clock = WorkoutFormats.SessionClock(seconds), ClockValue = (int)seconds, Instant = true };
            Hud.Apply(state, null);
            yield return Frames(3);
            Immersive.Zones.Invalidate();
            Immersive.Zones.Update();
            yield return Frames(3);
        }

        private List<string> Problems(string label)
        {
            var problems = new List<string>();
            var window = H.Root.worldBound;
            var boxes = new List<(string Name, Rect Box)>();
            foreach (var name in Pieces)
            {
                if (H.IsShown(name))
                {
                    boxes.Add((name, Bounds(name)));
                }
            }

            foreach (var (name, box) in boxes)
            {
                if (box.xMin < window.xMin - 0.5f || box.yMin < window.yMin - 0.5f || box.xMax > window.xMax + 0.5f || box.yMax > window.yMax + 0.5f)
                {
                    problems.Add(label + ": #" + name + " sale de la ventana " + box);
                }
            }

            for (var i = 0; i < boxes.Count; i++)
            {
                for (var j = i + 1; j < boxes.Count; j++)
                {
                    if (Overlap(boxes[i].Box, boxes[j].Box))
                    {
                        problems.Add(label + ": #" + boxes[i].Name + " se encima con #" + boxes[j].Name + " " + boxes[i].Box + " / " + boxes[j].Box);
                    }
                }
            }

            return problems;
        }

        private List<string> BoatProblems(string label)
        {
            var problems = new List<string>();
            var zone = Immersive.Layout!.BoatZone;
            var window = H.Root.worldBound;
            var rect = new Rect(window.xMin + (float)zone.Left, window.yMin + (float)zone.Top, (float)zone.Width, (float)zone.Height);
            foreach (var name in Pieces)
            {
                // ± 0,5 dp: el redondeo de cada caja al píxel (la geometría deja las piezas justo en el borde de la zona, T2 y T3).
                var inner = new Rect(rect.xMin + 0.5f, rect.yMin + 0.5f, rect.width - 1f, rect.height - 1f);
                if (H.IsShown(name) && Overlap(Bounds(name), inner))
                {
                    problems.Add(label + ": #" + name + " entra en la zona del bote (" + Bounds(name) + " / zona " + rect + "; HudGeometry " + Predicted(name) + ")");
                }
            }

            return problems;
        }

        // Lo que calcula HudGeometry para cada pieza (dp de ventana), para comparar con la caja real.
        private string Predicted(string name)
        {
            var layout = Immersive.Layout!;
            DpRect? rect = name switch
            {
                "hud-topbar" => layout.TopBar,
                "hud-spm" => layout.Spm,
                "hud-split" => layout.Split,
                "hud-watts" => layout.Watts,
                "hud-kcal" => layout.Kcal,
                "hud-bpm" => layout.Bpm,
                "hud-band" => layout.Band,
                _ => null,
            };
            return rect?.ToString() ?? "-";
        }

        // S-B4a (02-tech-design §9.0 de la 4a): la caja real de cada tarjeta, de la banda y de la barra contra la que calcula
        // HudGeometry (± 1 dp), en los cuatro perfiles, en libre, con pulso y con rutina.
        [UnityTest, Category("AC-WKT-17-08")]
        public IEnumerator ViewMatchesTheGeometry()
        {
            var problems = new List<string>();
            foreach (var profile in Profiles)
            {
                foreach (var c in new[] { Case.Free, Case.FreeWithPulse, Case.RoutineWithTargets, Case.Long })
                {
                    yield return Arrange(profile, c);
                    var layout = Immersive.Layout!;
                    var window = H.Root.worldBound;
                    var pairs = new List<(string Name, DpRect Rect)>
                    {
                        ("hud-topbar", layout.TopBar), ("hud-spm", layout.Spm), ("hud-split", layout.Split), ("hud-watts", layout.Watts),
                        ("hud-kcal", layout.Kcal), ("hud-band", layout.Band),
                    };
                    if (layout.Bpm.HasValue && H.IsShown("hud-bpm"))
                    {
                        pairs.Add(("hud-bpm", layout.Bpm.Value));
                    }

                    // ± 1 dp más un píxel base escalado: UI Toolkit redondea cada caja al píxel antes de la escala de la zona, que lo
                    // agranda (en W1280, densidad 1 y s = 1,6: hasta 1,6 dp).
                    var tolerance = 1f + (float)(layout.Scale / H.Profile.Density);
                    foreach (var (name, rect) in pairs)
                    {
                        var b = Bounds(name);
                        var x = b.xMin - window.xMin;
                        var y = b.yMin - window.yMin;
                        if (Math.Abs(x - rect.Left) > tolerance || Math.Abs(y - rect.Top) > tolerance || Math.Abs(b.width - rect.Width) > tolerance
                            || Math.Abs(b.height - rect.Height) > tolerance)
                        {
                            problems.Add(profile.Code + " " + c + ": #" + name + " real " + x.ToString("0.0") + "," + y.ToString("0.0") + " " + b.width.ToString("0.0")
                                         + "×" + b.height.ToString("0.0") + " / HudGeometry " + rect);
                        }
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // S-B4a (02-tech-design §9.0 de la 4a; 04-design-notes §3.2): los altos base de las tarjetas (sin y con obj.), la flecha
        // sobre la línea de base de las cifras (0,196 em del valor para ▲), obj. donde empiezan las cifras y el toque de los
        // botones con la escala de la zona.
        [UnityTest, Category("AC-WKT-10-05"), Category("AC-WKT-17-01")]
        public IEnumerator CardsArrowAndTargetMatchTheDesignNotes()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(50.05);
            yield return Settle();
            var s = (float)Immersive.Layout!.Scale;
            Assert.That(Bounds("hud-spm").height / s, Is.EqualTo(119.0f).Within(0.5f), "SPM con obj.: 119,0");
            Assert.That(Bounds("hud-watts").height / s, Is.EqualTo(97.8f).Within(0.5f), "Watts con obj.: 97,8");
            Assert.That(Bounds("hud-split").height / s, Is.EqualTo(80.9f).Within(0.5f), "Split sin obj.: 80,9");
            Assert.That(Bounds("hud-kcal").height / s, Is.EqualTo(63.7f).Within(0.5f), "kcal: 63,7");

            var watts = Hud.Watts;
            Assert.That(watts.IsOff, Is.True, "150 contra 160: fuera, con ▲");
            var number = watts.Number;
            var arrow = watts.ArrowLabel!;
            var numberBaseline = number.worldBound.yMin + number.BaselineFromTop * s;
            var arrowBaseline = arrow.worldBound.yMin + HudText.BaselineFromTop(arrow, arrow.resolvedStyle.fontSize) * s;
            var expected = (float)(HudGeometry.ArrowUpBaselineEm * number.FontSize) * s;
            Assert.That(numberBaseline - arrowBaseline, Is.EqualTo(expected).Within(1f), "la línea de base de ▲, 0,196 em del valor sobre la de las cifras (± 1 dp)");
            Assert.That(watts.Target!.worldBound.xMin, Is.EqualTo(number.worldBound.xMin).Within(1f), "obj. empieza donde empiezan las cifras");
            Assert.That(arrow.worldBound.xMax, Is.LessThanOrEqualTo(number.worldBound.xMin + 0.5f), "la flecha en el hueco, a la izquierda de las cifras");

            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            s = (float)Immersive.Layout!.Scale;
            Assert.That(Bounds("hud-spm").height / s, Is.EqualTo(102.1f).Within(0.5f), "SPM sin obj.: 102,1");
            Assert.That(Bounds("hud-watts").height / s, Is.EqualTo(80.9f).Within(0.5f), "Watts sin obj.: 80,9");
            Assert.That(Responds("hud-pause"), Is.True, "el toque llega a los botones con la escala de la zona");
            yield return Tap("hud-pause");
            Assert.That(Hud.Controls.ShowsResume, Is.True);
        }

        [UnityTest, Category("AC-WKT-17-08")]
        public IEnumerator NothingOverlapsInAnyProfile()
        {
            var problems = new List<string>();
            foreach (var profile in Profiles)
            {
                foreach (var c in new[] { Case.Free, Case.FreeWithPulse, Case.RoutineWithTargets, Case.Long })
                {
                    yield return Arrange(profile, c);
                    problems.AddRange(Problems(profile.Code + " " + c));
                    var audit = H.Audit();
                    if (!audit.IsClean)
                    {
                        problems.Add(profile.Code + " " + c + ": " + audit);
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest, Category("AC-WKT-17-09")]
        public IEnumerator BoatZoneStaysFree()
        {
            var problems = new List<string>();
            foreach (var profile in Profiles)
            {
                foreach (var c in new[] { Case.RoutineWithTargets, Case.Long })
                {
                    yield return Arrange(profile, c);
                    problems.AddRange(BoatProblems(profile.Code + " " + c));
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest, Category("AC-WKT-17-10")]
        public IEnumerator ExtremeValuesFit()
        {
            var problems = new List<string>();
            foreach (var profile in Profiles)
            {
                yield return Arrange(profile, Case.Extremes);
                Assert.That(H.TextOf("hud-spm-value"), Is.EqualTo("127.5"), profile.Code);
                Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("9:59:59"));
                Assert.That(H.TextOf("hud-bpm-value"), Is.EqualTo("255"));
                problems.AddRange(Problems(profile.Code + " extremos"));
                var audit = H.Audit();
                if (!audit.IsClean)
                {
                    problems.Add(profile.Code + " extremos: " + audit);
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // Las escalas de 04-design-notes §3.7 (± 0,005) en la pantalla real (la parte de los valores que se achican). Salen de
        // los anchos de las cifras de RowMate Numeric SC (la derivada con tnum, DD4a-07), que entrega WP-D0: con las cifras de
        // hoy dan un 4 % menos en todos los casos (por ejemplo, L640 banda 0,746 en lugar de 0,776).
        [UnityTest, Category("AC-WKT-17-08"), Category("AC-WKT-17-10")]
        [Ignore("NO VERIFICABLE hasta WP-D0: las escalas de 04-design-notes §3.7 se calcularon con las cifras de RowMate Numeric SC (DD4a-07), que todavía no está")]
        public IEnumerator ScalesOfTheDesignNotes()
        {
            var expected = new (DeviceProfile Profile, Case Case, HudValue Value, double Scale)[]
            {
                (DeviceProfile.L640, Case.Free, HudValue.Band, 0.776),
                (DeviceProfile.L640, Case.RoutineWithTargets, HudValue.Spm, 0.962),
                (DeviceProfile.TV, Case.Long, HudValue.Band, 0.800),
                (DeviceProfile.TH, Case.Long, HudValue.Band, 0.778),
                (DeviceProfile.L640, Case.Long, HudValue.Band, 0.561),
                (DeviceProfile.W1280, Case.Long, HudValue.Band, 0.792),
                (DeviceProfile.TV, Case.Extremes, HudValue.Spm, 0.951),
                (DeviceProfile.TV, Case.Extremes, HudValue.Split, 0.952),
                (DeviceProfile.TV, Case.Extremes, HudValue.Watts, 0.976),
                (DeviceProfile.TV, Case.Extremes, HudValue.Band, 0.651),
                (DeviceProfile.TH, Case.Extremes, HudValue.Band, 0.675),
                (DeviceProfile.L640, Case.Extremes, HudValue.Kcal, 0.915),
                (DeviceProfile.L640, Case.Extremes, HudValue.Band, 0.486),
                (DeviceProfile.W1280, Case.Extremes, HudValue.Band, 0.687),
            };
            var problems = new List<string>();
            foreach (var e in expected)
            {
                yield return Arrange(e.Profile, e.Case);
                var actual = Immersive.Layout!.ValueScale(e.Value);
                if (Math.Abs(actual - e.Scale) > 0.005)
                {
                    problems.Add(e.Profile.Code + " " + e.Case + " " + e.Value + ": " + actual.ToString("0.000") + " (se esperaba " + e.Scale.ToString("0.000") + ")");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
