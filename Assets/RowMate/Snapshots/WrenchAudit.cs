using System.Collections.Generic;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // La auditoría de la llave del simulador en cada toma con simulador (DD4a-04; 00-deuda-diseno §1.2; 02-tech-design §6.10 de
    // la It. 4a): "tapar" es que la caja de la llave, agrandada 8 dp, se cruce con el área táctil de un control. Si se cruza, la
    // toma FALLA (no es una sonda de aspecto).
    // - Controles: .rm-button, .rm-icon-button, .rm-chip__pill, .rm-fab, las pestañas (.rm-tab), .rm-segmented__option,
    //   .rm-switch, .rm-menu__item, el chevron de las tarjetas y de las filas (.rm-routine-card__chevron,
    //   .rm-hrm-device-row__chevron) y los controles del entrenamiento (#hud-controls).
    // - Campos (.rm-field__box): sin superposición, sin aire. Las superficies grandes que se tocan enteras (la tarjeta de rutina,
    //   la fila de un dispositivo encontrado) cuentan solo en sus controles propios (el ⋮) y en su chevron: la llave puede quedar
    //   sobre el resto, el ícono del principio o el fondo (00-deuda-diseno §1.2 y §1.4: en CAP-09 de la It. 2, sobre el corazón
    //   con que empieza la fila; en CAP-03, sobre la tarjeta de SPLIT, que no se toca).
    // - En el entrenamiento, además, ninguna pieza del HUD (la barra, las tarjetas y la banda: HudOverlap).
    // No cuenta: lo que no se ve (display none, opacidad 0, fuera de la ventana), lo que está debajo del velo de un diálogo, lo
    // del propio panel del simulador, y nada si el panel está abierto (la llave no se ve).
    public static class WrenchAudit
    {
        public const float Margin = 8;

        // El redondeo de cada caja al píxel: la geometría deja la llave justo a 8 dp de Pausa en el caso (b) de L640 y, redondeadas
        // la llave y la zona escalada de los controles, quedan a 7,3 (04-design-notes §3.9).
        public const float RoundingTolerance = 1;

        private static readonly string[] Controls =
        {
            "rm-button", "rm-icon-button", "rm-chip__pill", "rm-fab", "rm-tab", "rm-segmented__option", "rm-switch-row", "rm-switch__track", "rm-menu__item",
            "rm-routine-card__chevron", "rm-hrm-device-row__chevron",
        };

        private static readonly string[] Surfaces = { "rm-field__box" };

        private static readonly string[] HudPieces = { "hud-topbar", "hud-spm", "hud-split", "hud-watts", "hud-kcal", "hud-bpm", "hud-band" };

        public static IEnumerable<string> Run(UiTestHost host)
        {
            var problems = new List<string>();
            var wrench = host.Find("sim-open");
            if (wrench == null || !UiQuery.IsShown(wrench) || wrench.resolvedStyle.opacity < 0.01f)
            {
                return problems;
            }

            var box = wrench.worldBound;
            var air = Margin - RoundingTolerance;
            var grown = new Rect(box.xMin - air, box.yMin - air, box.width + 2 * air, box.height + 2 * air);
            var window = host.Root.worldBound;
            if (box.xMin < window.xMin - 0.5f || box.yMin < window.yMin - 0.5f || box.xMax > window.xMax + 0.5f || box.yMax > window.yMax + 0.5f)
            {
                problems.Add("llave: fuera de la ventana (" + box + ")");
            }

            var dialog = host.TopDialog;
            var layer = host.Find("simulator-layer");
            foreach (var element in host.Root.Query<VisualElement>().Build())
            {
                if (ReferenceEquals(element, wrench) || layer != null && (ReferenceEquals(element, layer) || layer.Contains(element)))
                {
                    continue;
                }

                if (dialog != null && !dialog.Host.Contains(element))
                {
                    // Lo de atrás del velo no se toca mientras hay un diálogo.
                    continue;
                }

                var control = HasAnyClass(element, Controls) || element.name == "hud-finish" || element.name == "hud-pause"
                                                             || element.name == "hud-resume";
                var surface = !control && HasAnyClass(element, Surfaces);
                if (!control && !surface)
                {
                    continue;
                }

                if (!Visible(element, window))
                {
                    continue;
                }

                // Lo que está fuera de la vista de su ScrollView no se ve (ni se toca): cuenta solo la parte a la vista.
                var bounds = Clipped(element);
                if (bounds.width <= 0.5f || bounds.height <= 0.5f)
                {
                    continue;
                }

                var crosses = control ? Overlap(bounds, grown) : Overlap(bounds, box);
                if (crosses)
                {
                    problems.Add("la llave tapa " + UiQuery.Describe(element) + " (" + Fmt(bounds) + "; llave " + Fmt(box) + ")");
                }
            }

            // En el entrenamiento: ninguna pieza del HUD.
            if (host.Immersive != null)
            {
                foreach (var name in HudPieces)
                {
                    var piece = host.Find(name);
                    if (piece != null && Visible(piece, window) && Overlap(piece.worldBound, box))
                    {
                        problems.Add("la llave tapa #" + name + " (" + Fmt(piece.worldBound) + "; llave " + Fmt(box) + ")");
                    }
                }
            }

            return problems;
        }

        // HudOverlap (02-tech-design §7.1): en toda toma del inmersivo, ningún par entre la barra, las tarjetas, la banda, los
        // controles y la llave se cruza, y todo queda dentro de la ventana (AC-WKT-17-08).
        public static IEnumerable<string> HudOverlap(UiTestHost host)
        {
            var problems = new List<string>();
            if (host.Immersive == null)
            {
                return problems;
            }

            var window = host.Root.worldBound;
            var rects = new List<(string Name, Rect Box)>();
            foreach (var name in HudPieces)
            {
                var piece = host.Find(name);
                if (piece != null && Visible(piece, window))
                {
                    rects.Add((name, piece.worldBound));
                }
            }

            var controls = host.Immersive.View.Controls;
            rects.Add(("hud-finish", controls.Finish.worldBound));
            var pause = controls.ShowsResume ? controls.Resume : controls.Pause;
            rects.Add((pause.name, pause.worldBound));
            var wrench = host.Find("sim-open");
            if (wrench != null && UiQuery.IsShown(wrench) && wrench.resolvedStyle.opacity > 0.01f)
            {
                rects.Add(("sim-open", wrench.worldBound));
            }

            foreach (var (name, box) in rects)
            {
                if (box.xMin < window.xMin - 0.5f || box.yMin < window.yMin - 0.5f || box.xMax > window.xMax + 0.5f || box.yMax > window.yMax + 0.5f)
                {
                    problems.Add("#" + name + " sale de la ventana (" + Fmt(box) + ")");
                }
            }

            for (var i = 0; i < rects.Count; i++)
            {
                for (var j = i + 1; j < rects.Count; j++)
                {
                    if (Overlap(rects[i].Box, rects[j].Box))
                    {
                        problems.Add("#" + rects[i].Name + " se encima con #" + rects[j].Name + " (" + Fmt(rects[i].Box) + " / " + Fmt(rects[j].Box) + ")");
                    }
                }
            }

            return problems;
        }

        private static bool HasAnyClass(VisualElement element, string[] classes)
        {
            for (var i = 0; i < classes.Length; i++)
            {
                if (element.ClassListContains(classes[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Visible(VisualElement element, Rect window)
        {
            if (!UiQuery.IsShown(element))
            {
                return false;
            }

            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.resolvedStyle.opacity < 0.01f)
                {
                    return false;
                }
            }

            var bounds = element.worldBound;
            return bounds.width > 0.5f && bounds.height > 0.5f && bounds.xMax > window.xMin && bounds.xMin < window.xMax && bounds.yMax > window.yMin
                   && bounds.yMin < window.yMax;
        }

        // La caja recortada por la vista de cada ScrollView que la contiene.
        private static Rect Clipped(VisualElement element)
        {
            var box = element.worldBound;
            for (var e = element.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e is ScrollView scroll)
                {
                    var view = scroll.contentViewport.worldBound;
                    var xMin = Mathf.Max(box.xMin, view.xMin);
                    var yMin = Mathf.Max(box.yMin, view.yMin);
                    var xMax = Mathf.Min(box.xMax, view.xMax);
                    var yMax = Mathf.Min(box.yMax, view.yMax);
                    box = new Rect(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
                }
            }

            return box;
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        private static string Fmt(Rect r)
        {
            return "x " + r.xMin.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "-" + r.xMax.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                   + " · y " + r.yMin.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "-" + r.yMax.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
