using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // EGT · El glifo de "Exportar TCX" (12-exportar-tcx/02-tech-design §1.3; D12-03; 04-design-notes §2): download e ios_share de
    // Material Symbols Rounded con la receta de ASSET_SLOTS §4.4 (trazo 2,25, remates redondos, esquinas de radio 2), en la caja de
    // 24, como contornos cerrados, simples, sin encimarse entre sí y con el mismo sentido de giro.
    [TestFixture]
    public sealed class ExportGlyphTests
    {
        private static readonly ExportGlyph.Kind[] Kinds = { ExportGlyph.Kind.Download, ExportGlyph.Kind.Share };

        [Test, Category("D12-03")]
        public void CoordinatesInsideTheBoxAndContoursClosed()
        {
            foreach (var kind in Kinds)
            {
                var contours = ExportGlyph.Contours(kind);
                Assert.That(contours.Count, Is.EqualTo(2), kind + ": flecha y base");
                foreach (var points in contours)
                {
                    Assert.That(points.Length % 2, Is.EqualTo(0), kind + ": pares x, y");
                    Assert.That(points.Length / 2, Is.GreaterThanOrEqualTo(3), kind + ": un polígono");
                    foreach (var value in points)
                    {
                        Assert.That(value, Is.InRange(0, ExportGlyph.ViewBox), kind + ": dentro de la caja de 24");
                    }

                    Assert.That(points[0] != points[points.Length - 2] || points[1] != points[points.Length - 1], Is.True,
                        kind + ": el cierre lo pone el dibujo (sin repetir el primer vértice)");
                }
            }
        }

        [Test, Category("D12-03")]
        public void DownloadPointsDownShareUp()
        {
            // La punta de la flecha: abajo en download (el punto más bajo de la flecha), arriba en share (el más alto).
            MinMaxY(ExportGlyph.DownloadArrow, out _, out var downMaxY, out var downAtX);
            Assert.That(downMaxY, Is.EqualTo(ExportGlyph.DownloadTipY + ExportGlyph.Stroke / 2).Within(1e-4), "download: punta abajo");
            Assert.That(downAtX, Is.EqualTo(ExportGlyph.DownloadTipX).Within(1e-4));
            MinMaxY(ExportGlyph.ShareArrow, out var shareMinY, out _, out _);
            Assert.That(shareMinY, Is.EqualTo(ExportGlyph.ShareTipY - ExportGlyph.Stroke / 2).Within(1e-4), "share: punta arriba");
            Assert.That(ExportGlyph.Contours(ExportGlyph.Kind.Download), Is.Not.SameAs(ExportGlyph.Contours(ExportGlyph.Kind.Share)));
        }

        [Test, Category("D12-03")]
        public void ShaftsHaveTheRecipeStroke()
        {
            // ASSET_SLOTS §4.4: trazo de 2,25 (peso 500, opsz 24), centrado en x = 12.
            foreach (var arrow in new[] { ExportGlyph.DownloadArrow, ExportGlyph.ShareArrow })
            {
                Assert.That(HasX(arrow, 12 - ExportGlyph.Stroke / 2), Is.True, "borde izquierdo del eje");
                Assert.That(HasX(arrow, 12 + ExportGlyph.Stroke / 2), Is.True, "borde derecho del eje");
            }
        }

        [Test, Category("D12-03")]
        public void ContoursAreSimpleSameWindingAndDoNotOverlap()
        {
            foreach (var kind in Kinds)
            {
                var contours = ExportGlyph.Contours(kind);
                for (var a = 0; a < contours.Count; a++)
                {
                    Assert.That(SignedArea(contours[a]), Is.GreaterThan(0), kind + " #" + a + ": horario en pantalla (y hacia abajo)");
                    Assert.That(SelfIntersects(contours[a]), Is.False, kind + " #" + a + ": contorno simple");
                    for (var b = a + 1; b < contours.Count; b++)
                    {
                        Assert.That(Cross(contours[a], contours[b]), Is.False, kind + ": las piezas no se cruzan");
                        Assert.That(Inside(contours[a], contours[b][0], contours[b][1]), Is.False, kind + ": ni una dentro de otra");
                        Assert.That(Inside(contours[b], contours[a][0], contours[a][1]), Is.False, kind + ": ni una dentro de otra");
                    }
                }
            }
        }

        [Test, Category("D12-03")]
        public void ArraysAreBuiltOnce()
        {
            // Dibujar no asigna: Contours devuelve siempre los mismos arreglos.
            foreach (var kind in Kinds)
            {
                Assert.That(ExportGlyph.Contours(kind), Is.SameAs(ExportGlyph.Contours(kind)));
            }

            Assert.That(ExportGlyph.Contours(ExportGlyph.Kind.Download)[0], Is.SameAs(ExportGlyph.DownloadArrow));
            Assert.That(ExportGlyph.Contours(ExportGlyph.Kind.Share)[1], Is.SameAs(ExportGlyph.ShareBox));
        }

        private static void MinMaxY(double[] p, out double minY, out double maxY, out double xAtMax)
        {
            minY = double.MaxValue;
            maxY = double.MinValue;
            xAtMax = 0;
            for (var i = 0; i < p.Length; i += 2)
            {
                minY = Math.Min(minY, p[i + 1]);
                if (p[i + 1] > maxY)
                {
                    maxY = p[i + 1];
                    xAtMax = p[i];
                }
            }
        }

        private static bool HasX(double[] p, double x)
        {
            for (var i = 0; i < p.Length; i += 2)
            {
                if (Math.Abs(p[i] - x) < 1e-4)
                {
                    return true;
                }
            }

            return false;
        }

        private static double SignedArea(double[] p)
        {
            var n = p.Length / 2;
            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                sum += p[2 * i] * p[2 * j + 1] - p[2 * j] * p[2 * i + 1];
            }

            return sum / 2;
        }

        private static IEnumerable<(double, double, double, double)> Segments(double[] p)
        {
            var n = p.Length / 2;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                yield return (p[2 * i], p[2 * i + 1], p[2 * j], p[2 * j + 1]);
            }
        }

        private static bool SelfIntersects(double[] p)
        {
            var segs = new List<(double, double, double, double)>(Segments(p));
            for (var i = 0; i < segs.Count; i++)
            {
                for (var j = i + 1; j < segs.Count; j++)
                {
                    var adjacent = j == i + 1 || (i == 0 && j == segs.Count - 1);
                    if (!adjacent && ProperCross(segs[i], segs[j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Cross(double[] a, double[] b)
        {
            foreach (var s in Segments(a))
            {
                foreach (var t in Segments(b))
                {
                    if (ProperCross(s, t))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ProperCross((double, double, double, double) s, (double, double, double, double) t)
        {
            var d1 = Orient(t.Item1, t.Item2, t.Item3, t.Item4, s.Item1, s.Item2);
            var d2 = Orient(t.Item1, t.Item2, t.Item3, t.Item4, s.Item3, s.Item4);
            var d3 = Orient(s.Item1, s.Item2, s.Item3, s.Item4, t.Item1, t.Item2);
            var d4 = Orient(s.Item1, s.Item2, s.Item3, s.Item4, t.Item3, t.Item4);
            return d1 * d2 < 0 && d3 * d4 < 0;
        }

        private static double Orient(double ax, double ay, double bx, double by, double cx, double cy)
        {
            return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        }

        private static bool Inside(double[] p, double x, double y)
        {
            var inside = false;
            foreach (var (x1, y1, x2, y2) in Segments(p))
            {
                if ((y1 > y) != (y2 > y) && x < (x2 - x1) * (y - y1) / (y2 - y1) + x1)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
