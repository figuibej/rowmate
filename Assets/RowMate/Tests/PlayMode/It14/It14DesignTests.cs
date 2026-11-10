using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.History;
using RowMate.Tests.PlayMode.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.It14
{
    // It. 14, WP-D (14-pulido-plataformas/02-tech-design §3, §6.1, §6.2 y §7; 00-arte-pulido §3; AC-POL-01, -02, -04, AC-APP-15-10,
    // AC-DEBT-06, -07, -08): el inventario de transiciones muestreado cuadro a cuadro, la auditoría de cifras tabulares y de fuentes, el
    // diálogo de completado a 1,3×, el Historial a 1,3× y en TV, los ejes sin etiquetas encimadas, el arte del Historial y el ancla de
    // desplazamiento.
    internal static class DesignProbe
    {
        // Un texto hecho solo de un número formateado (TimeFormats, HistoryFormats, Fmt): cifras con separadores y, a lo sumo, la unidad
        // pegada o separada. "1 serie · 1 descanso" no entra (hay palabras): es un número dentro de una frase (exento por la regla).
        private static readonly Regex Numeric = new Regex(@"^[-+]?[\d:.,/ ·]*\d[\d:.,/ ·]*(\s?(m|W|spm|/500m|s|bpm|kcal|%))*$", RegexOptions.CultureInvariant);

        public static bool IsNumberText(string text)
        {
            return text.Length > 0 && Numeric.IsMatch(text.Trim());
        }

        // Los TextElement a la vista (display, visibility y opacidad heredadas; cruzando la ventana), salvo los subárboles exentos.
        public static List<TextElement> VisibleTexts(VisualElement root)
        {
            var list = new List<TextElement>();
            Walk(root, root.worldBound, list);
            return list;
        }

        private static void Walk(VisualElement element, Rect window, List<TextElement> list)
        {
            if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.visibility == Visibility.Hidden
                || element.resolvedStyle.opacity <= 0.001f || element.ClassListContains("rm-audit-exempt") || element is TabularNumber)
            {
                return;
            }

            if (element is TextElement text && !string.IsNullOrEmpty(text.text) && text.worldBound.Overlaps(window) && text.worldBound.width > 0)
            {
                list.Add(text);
            }

            for (var i = 0; i < element.hierarchy.childCount; i++)
            {
                Walk(element.hierarchy[i], window, list);
            }
        }

        public static string FontPath(TextElement text)
        {
            var asset = text.resolvedStyle.unityFontDefinition.fontAsset;
            return asset == null ? "(ninguna)" : asset.name;
        }

        public static bool InsideTextField(VisualElement element)
        {
            for (var e = element.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e is TextField || e.ClassListContains("rm-text-field"))
                {
                    return true;
                }
            }

            return false;
        }

        public static string Describe(TextElement text)
        {
            return "#" + text.name + " ." + string.Join(".", text.GetClasses()) + " \"" + text.text + "\" (" + FontPath(text) + ")";
        }

        public static float OpacityOf(VisualElement element)
        {
            var style = element.style.opacity;
            return style.keyword == StyleKeyword.Undefined ? style.value : 1f;
        }
    }

    // AC-POL-01 (T-02, T-03, T-04): cada transición sigue su token cuadro a cuadro (duración y curva), con su variante de reducir
    // movimiento, y ningún cuadro muestra la pantalla sin estilos (texto con la fuente del motor).
    public sealed class TransitionInventoryTests : RoutinesUiTestBase
    {
        private const float Tolerance = 0.02f;

        [UnityTest, Category("AC-POL-01")]
        public IEnumerator TabSwitchFollowsTheTokensFrameByFrame()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return Frames(3);
            var tabs = H.Shell.Tabs;
            var outgoing = tabs.Screens[tabs.Current];
            H.Tap("tab-routines");
            var incoming = tabs.Screens[tabs.Current];
            Assert.That(incoming, Is.Not.SameAs(outgoing));
            var frames = (int)Math.Ceiling(MotionSpecs.TabInEndMs / (UiTestHost.FrameSeconds * 1000)) + 2;
            for (var k = 0; k <= frames; k++)
            {
                var ms = k * UiTestHost.FrameSeconds * 1000;
                var expectedIn = Easing.Evaluate(Tokens.EaseEnter, Progress(ms, MotionSpecs.TabInStartMs, MotionSpecs.TabInEndMs));
                var expectedOut = 1 - Easing.Evaluate(Tokens.EaseExit, Progress(ms, 0, MotionSpecs.TabOutMs));
                Assert.That(DesignProbe.OpacityOf(incoming), Is.EqualTo((float)expectedIn).Within(Tolerance), "la entrante a los " + ms.ToString("0") + " ms");
                if (ms < MotionSpecs.TabOutMs)
                {
                    Assert.That(DesignProbe.OpacityOf(outgoing), Is.EqualTo((float)expectedOut).Within(Tolerance), "la saliente a los " + ms.ToString("0") + " ms");
                }

                yield return null;
                AssertStyled(incoming, "cuadro " + k);
                H.Step();
            }

            Assert.That(outgoing.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "la saliente terminó");
            Assert.That(DesignProbe.OpacityOf(incoming), Is.EqualTo(1f).Within(0.001f), "la entrante a pleno");
        }

        [UnityTest, Category("AC-POL-01")]
        public IEnumerator PushAndPopKeepTheirDurationsAndEveryFrameIsStyled()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return GoToRoutines();
            yield return Frames();
            H.Tap("routines-add");
            var page = H.Shell.Pages.Top!.Root;
            var t = 0.0;
            var cap24Checked = false;
            while (H.Shell.Pages.IsTransitioning && t < 1000)
            {
                yield return null;
                AssertStyled(page, "push a los " + t.ToString("0") + " ms");
                if (!cap24Checked && t >= 120)
                {
                    // CAP-24: a mitad del push, la página ya se ve (no un cuadro vacío) y el shell sigue detrás.
                    cap24Checked = true;
                    Assert.That(page.resolvedStyle.opacity, Is.GreaterThan(0.5f), "a los 120 ms la página se ve");
                    Assert.That(H.Shell.Tabs.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "el shell sigue detrás (sin hueco)");
                }

                H.Step();
                t += UiTestHost.FrameSeconds * 1000;
            }

            Assert.That(t, Is.EqualTo(MotionSpecs.PageInMs).Within(UiTestHost.FrameSeconds * 1000 + 1), "push de 240");
            yield return Settle();
            H.Tap("editor-back");
            t = 0;
            while (H.Shell.Pages.IsTransitioning && t < 1000)
            {
                H.Step();
                t += UiTestHost.FrameSeconds * 1000;
                yield return null;
            }

            Assert.That(t, Is.EqualTo(MotionSpecs.PageOutMs).Within(UiTestHost.FrameSeconds * 1000 + 1), "pop de 200");
        }

        [UnityTest, Category("AC-POL-01")]
        public IEnumerator ReducedMotionIsAFade()
        {
            CreateRoutines(RoutineSeeds.Lista, null, o => o.ReduceMotion = true);
            yield return Frames(3);
            var tabs = H.Shell.Tabs;
            H.Tap("tab-routines");
            var incoming = tabs.Screens[tabs.Current];
            yield return null;
            Assert.That(incoming.style.translate.keyword == StyleKeyword.Undefined ? incoming.style.translate.value.x.value : 0f, Is.EqualTo(0f),
                "pestaña: solo opacidad");
            yield return Settle();
            H.Tap("routines-add");
            var t = 0.0;
            while (H.Shell.Pages.IsTransitioning && t < 1000)
            {
                H.Step();
                t += UiTestHost.FrameSeconds * 1000;
                yield return null;
            }

            Assert.That(t, Is.LessThanOrEqualTo(MotionSpecs.ReducedPageFadeMs + UiTestHost.FrameSeconds * 1000 + 1), "fundido de 120");
        }

        private static double Progress(double ms, double start, double end)
        {
            var u = (ms - start) / (end - start);
            return u <= 0 ? 0 : u >= 1 ? 1 : u;
        }

        // Ningún texto con la fuente por defecto del motor (un cuadro sin estilos).
        private static void AssertStyled(VisualElement root, string when)
        {
            foreach (var text in DesignProbe.VisibleTexts(root))
            {
                Assert.That(text.resolvedStyle.unityFontDefinition.fontAsset, Is.Not.Null, when + ": " + DesignProbe.Describe(text));
            }
        }
    }

    // AC-POL-02: ningún texto con la fuente del motor y todo número formateado en cifras tabulares (RowMate Numeric SC o TabularNumber).
    public sealed class TypographyAuditTests : HistoryUiTestBase
    {
        // Exentos de la regla (00-arte-pulido §3.2): identificadores y números dentro de una frase o de una insignia de índice.
        private static readonly string[] ExemptNames = { "session-step-", "routine-card-", "step-row-" };

        private static readonly HashSet<string> NumericFonts = new HashSet<string>
        {
            "RowMateNumericSC-SemiBold-SDF", "RowMateNumericSC-Bold-SDF", "RowMateNumericSC-ExtraBold-SDF",
        };

        [UnityTest, Category("AC-POL-02")]
        public IEnumerator HistoryAndDetailUseTabularFiguresAndProjectFonts()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return Frames(3);
            var problems = new List<string>();
            Audit("Historial", problems);
            yield return OpenCard(0);
            Audit("Detalle", problems);
            H.ScrollToEnd("session-scroll");
            yield return Frames(3);
            Audit("Detalle abajo", problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest, Category("AC-POL-02")]
        public IEnumerator DeviceAndRoutinesUseTabularFiguresAndProjectFonts()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV });
            H.TestRower!.ConnectedName = "Remo de prueba";
            H.TestRower.Emit(BleStatus.Connected);
            H.TestRower.EmitData(new RowingData { StrokeRate = 23.5, PowerWatts = 149, DistanceMeters = 652, Pace500mSeconds = 133 });
            yield return Settle();
            var problems = new List<string>();
            Audit("Dispositivo", problems);
            yield return Tap("tab-workout");
            Audit("Workout", problems);
            yield return Tap("tab-routines");
            Audit("Rutinas", problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [UnityTest, Category("AC-POL-02")]
        public IEnumerator StepRowsAreColumnsOfFixedWidth()
        {
            // TY-01: las tres columnas miden lo mismo en todas las filas; las unidades caen en el mismo lugar.
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return OpenCard(0);
            yield return Frames(3);
            var rows = Detail.StepRows;
            Assert.That(rows.Count, Is.GreaterThan(2));
            for (var c = 0; c < 3; c++)
            {
                Assert.That(rows.ColumnWidth(c), Is.GreaterThan(0), "columna " + c);
                var right = rows.ValueAt(0, c).worldBound.xMax;
                for (var i = 1; i < rows.Count; i++)
                {
                    Assert.That(rows.ValueAt(i, c).worldBound.xMax, Is.EqualTo(right).Within(0.6f), "columna " + c + ", fila " + i + ": el borde derecho no baila");
                }
            }

            Assert.That(DesignProbe.FontPath(rows.ValueAt(0, 0)), Is.EqualTo("RowMateNumericSC-Bold-SDF"), "num.xs");
            Assert.That(DesignProbe.FontPath(rows.Line1At(0)), Is.EqualTo("RowMateNumericSC-SemiBold-SDF"));
            Assert.That(DesignProbe.FontPath(Card0Date()), Is.EqualTo("RowMateNumericSC-SemiBold-SDF"), "TY-02: la fecha");
        }

        private Label Card0Date()
        {
            return Screen.List.CardAt(0)!.DateLabel;
        }

        private void Audit(string screen, List<string> problems)
        {
            var allowed = new HashSet<string>();
            foreach (var path in FontAssetPaths.All)
            {
                allowed.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }

            foreach (var text in DesignProbe.VisibleTexts(H.Root))
            {
                var font = DesignProbe.FontPath(text);
                if (!allowed.Contains(font))
                {
                    problems.Add(screen + " · fuente fuera del proyecto · " + DesignProbe.Describe(text));
                }

                if (!DesignProbe.IsNumberText(text.text) || DesignProbe.InsideTextField(text) || text.ClassListContains("rm-icon") || IsExempt(text))
                {
                    continue;
                }

                if (!NumericFonts.Contains(font))
                {
                    problems.Add(screen + " · número sin cifras tabulares · " + DesignProbe.Describe(text));
                }
            }
        }

        private static bool IsExempt(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.ClassListContains("rm-step-perf__number") || e.ClassListContains("rm-step-perf__badge"))
                {
                    return true;
                }

                foreach (var prefix in ExemptNames)
                {
                    if (e.name != null && e.name.StartsWith(prefix, StringComparison.Ordinal) && e.name.EndsWith("-number", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    // AC-DEBT-06 (#36): P360 a 1,3× en inglés, cada columna se achica ENTERA (valor y etiqueta con la misma escala) y entre columnas
    // quedan 4 dp; en TV el gráfico del Detalle se ve entero (el 0 del eje Y y el eje X) al desplazarse.
    public sealed class HistoryScaleTests : HistoryUiTestBase
    {
        [UnityTest, Category("AC-DEBT-06")]
        public IEnumerator ColumnsShrinkWholeWithFourDpOfAir()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.P360, o =>
            {
                o.LanguageTags = new[] { "en-US" };
                o.TextScale = 1.3;
            });
            yield return GoToHistory();
            yield return Frames(3);
            var shrunk = 0;
            for (var i = 0; i < Screen.List.Count; i++)
            {
                var stats = Card(i).Stats;
                for (var c = 0; c < stats.Count; c++)
                {
                    var fit = stats.FitAt(c);
                    Assert.That(fit.Fits, Is.True, "tarjeta " + i + ", columna " + c);
                    Assert.That(stats.LabelAt(c).worldBound.height / stats.LabelAt(c).layout.height, Is.EqualTo(fit.AppliedScale).Within(0.02f),
                        "la etiqueta con la escala de su columna");
                    if (fit.FitScale < 0.999f)
                    {
                        shrunk++;
                    }

                    if (c > 0)
                    {
                        var left = Content(stats, c - 1);
                        var right = Content(stats, c);
                        Assert.That(right.xMin - left.xMax, Is.GreaterThanOrEqualTo(4f - 0.6f), "tarjeta " + i + ": 4 dp entre las columnas " + (c - 1) + " y " + c);
                    }
                }
            }

            Assert.That(shrunk, Is.GreaterThan(0), "en P360 a 1,3× alguna columna se achica");
            AssertClean(H);
        }

        [UnityTest, Category("AC-DEBT-06")]
        public IEnumerator TvChartShowsZeroAndTheXAxis()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.TV);
            yield return GoToHistory();
            yield return OpenCard(0);
            H.ScrollToEnd("session-scroll");
            yield return Frames(3);
            var viewport = Detail.Scroll.contentViewport.worldBound;
            var chart = Detail.Chart;
            Assert.That(chart.worldBound.yMax, Is.LessThanOrEqualTo(viewport.yMax + 0.5f), "el gráfico entero a la vista");
            var labels = chart.Labels;
            Assert.That(labels.YAt(0).text, Is.EqualTo("0"));
            Assert.That(viewport.Contains(labels.YAt(0).worldBound.center), Is.True, "el 0 del eje Y a la vista");
            Assert.That(viewport.Contains(labels.XAt(0).worldBound.center), Is.True, "el eje X a la vista");
        }

        // La caja que ocupa la columna c (el contenido escalado de su FitBox).
        private static Rect Content(StatRowView stats, int c)
        {
            var value = stats.ValueAt(c).worldBound;
            var label = stats.LabelAt(c).worldBound;
            return Rect.MinMaxRect(Math.Min(value.xMin, label.xMin), Math.Min(value.yMin, label.yMin), Math.Max(value.xMax, label.xMax),
                Math.Max(value.yMax, label.yMax));
        }
    }

    // AC-DEBT-07 (#32) y AC-DEBT-08 (#37): los ejes sin etiquetas encimadas (una marca intermedia que choca con un borde no se dibuja) y
    // el error del Historial con su ícono de 56 al 12 %.
    public sealed class ChartArtTests : HistoryUiTestBase
    {
        [UnityTest, Category("AC-DEBT-07")]
        public IEnumerator NoAxisLabelTouchesAnother()
        {
            foreach (var profile in new[] { DeviceProfile.W1280, DeviceProfile.TV })
            {
                CreateHistory(HistorySeeds.F9, profile);
                yield return GoToHistory();
                yield return OpenCard(0);
                H.ScrollToEnd("session-scroll");
                yield return Frames(3);
                var metrics = new[] { "watts", "spm", "split", "distance", "hr" };
                for (var metric = 0; metric < metrics.Length; metric++)
                {
                    if (metric > 0)
                    {
                        yield return Tap("session-metric-" + metrics[metric]);
                        Detail.Chart.FinishEntry();
                        yield return Frames(3);
                    }

                    var labels = Detail.Chart.Labels;
                    AssertApart(VisibleX(labels), true, profile.Code + " métrica " + metric + " X");
                    AssertApart(VisibleY(labels), false, profile.Code + " métrica " + metric + " Y");
                    Assert.That(labels.XDropped(0) || labels.XDropped(labels.XCount - 1), Is.False, "los bordes quedan siempre");
                }
            }
        }

        [UnityTest, Category("AC-DEBT-08")]
        public IEnumerator HistoryErrorHasTheEmptyIcon()
        {
            CreateScriptedHistory(r => r.HistoryFailures = 2, DeviceProfile.TV);
            yield return GoToHistory();
            Assert.That(H.IsShown("history-error"), Is.True);
            var icon = H.Find("history-error-icon")!;
            Assert.That(icon.resolvedStyle.width, Is.EqualTo(56).Within(0.5f), "56 dp");
            Assert.That(icon.resolvedStyle.opacity, Is.EqualTo(0.12f).Within(0.005f), "al 12 %");
            var gap = H.BoundsOf("history-error-text").yMin - H.BoundsOf("history-error-icon").yMax;
            Assert.That(gap, Is.EqualTo(12).Within(0.6f), "12 encima del texto");
            AssertClean(H);
        }

        private static List<Rect> VisibleX(ChartAxisLabels labels)
        {
            var list = new List<Rect>();
            for (var i = 0; i < labels.XCount; i++)
            {
                if (labels.XAt(i).resolvedStyle.display != DisplayStyle.None)
                {
                    list.Add(labels.XAt(i).worldBound);
                }
            }

            return list;
        }

        private static List<Rect> VisibleY(ChartAxisLabels labels)
        {
            var list = new List<Rect>();
            for (var i = 0; i < labels.YCount; i++)
            {
                if (labels.YAt(i).resolvedStyle.display != DisplayStyle.None)
                {
                    // La caja de 14 de la etiqueta Y: lo que mide AxisCrowding.
                    list.Add(labels.YAt(i).worldBound);
                }
            }

            return list;
        }

        private static void AssertApart(List<Rect> rects, bool horizontal, string what)
        {
            for (var a = 0; a < rects.Count; a++)
            {
                for (var b = a + 1; b < rects.Count; b++)
                {
                    var gap = horizontal
                        ? Math.Max(rects[b].xMin - rects[a].xMax, rects[a].xMin - rects[b].xMax)
                        : Math.Max(rects[b].yMin - rects[a].yMax, rects[a].yMin - rects[b].yMax);
                    var limit = a == 0 || b == rects.Count - 1 ? 4f - 0.6f : -0.6f;
                    Assert.That(gap, Is.GreaterThanOrEqualTo(limit), what + ": " + a + " y " + b);
                }
            }
        }
    }

    // AC-POL-04 (O-01 de la 4b): en L640 con letra 1,3×, el diálogo de rutina completada queda centrado, entero y quieto.
    public sealed class CompletionDialogScaleTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-POL-04")]
        public IEnumerator CenteredWholeAndStillAtL640WithBigText()
        {
            CreateWorkout(RoutineSeeds.F4A, o =>
            {
                o.Profile = DeviceProfile.L640;
                o.TextScale = 1.3;
            });
            yield return StartRoutine("Corta");
            yield return AdvanceTo(31);
            yield return Settle();
            var dialog = H.TopDialog;
            Assert.That(dialog, Is.Not.Null, "el diálogo de completado");
            var window = H.Root.worldBound;
            var box = dialog!.Box.worldBound;
            Assert.That(window.Contains(box.min) && window.Contains(box.max - Vector2.one * 0.5f), Is.True, "entero en la ventana: " + box);
            Assert.That(box.center.x, Is.EqualTo(window.center.x).Within(1f), "centrado en x");
            Assert.That(box.center.y, Is.EqualTo(window.center.y).Within(1f), "centrado en y");
            Assert.That(dialog.ContentScroll.scrollOffset.y, Is.EqualTo(0).Within(0.01f), "sin desplazarse");
            var finish = H.Find("completion-finish");
            Assert.That(finish, Is.Not.Null);
            Assert.That(window.Contains(finish!.worldBound.center), Is.True, "la acción a la vista");
            H.Advance(2);
            yield return Frames(3);
            var later = dialog.Box.worldBound;
            Assert.That(Vector2.Distance(later.position, box.position) + Vector2.Distance(later.size, box.size), Is.LessThan(0.5f), "quieto");
        }
    }

    // AC-APP-15-10 (T-07, fuera del HUD): al rotar, la primera tarjeta que se veía queda arriba, a la misma distancia del borde.
    public sealed class ScrollAnchorTests : RoutinesUiTestBase
    {
        [UnityTest, Category("AC-APP-15-10")]
        public IEnumerator RotationKeepsTheFirstVisibleCard()
        {
            CreateRoutines(RoutineSeeds.ListaPlus(20), DeviceProfile.TV);
            yield return GoToRoutines();
            yield return Frames(3);
            var list = Screen.List;
            list.Scroll.scrollOffset = new Vector2(0, 600);
            yield return Frames(3);
            var anchor = list.Anchor;
            Assert.That(anchor.AnchorIndex, Is.GreaterThan(0), "desplazada");
            var index = anchor.AnchorIndex;
            var delta = anchor.AnchorDelta;
            H.Resize(DeviceProfile.TH);
            yield return Frames(4);
            var child = list.Content[index];
            var top = child.layout.y - list.Scroll.scrollOffset.y;
            var max = Math.Max(0, list.Content.layout.height - list.Scroll.contentViewport.layout.height);
            if (list.Scroll.scrollOffset.y < max - 0.5f)
            {
                Assert.That(top, Is.EqualTo(delta).Within(1f), "la misma tarjeta a la misma distancia del borde");
            }

            H.Resize(DeviceProfile.TV);
            yield return Frames(4);
            top = list.Content[index].layout.y - list.Scroll.scrollOffset.y;
            Assert.That(top, Is.EqualTo(delta).Within(1f), "y de vuelta");
            AssertClean(H);
        }
    }
}
