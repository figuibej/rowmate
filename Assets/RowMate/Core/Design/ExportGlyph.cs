using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // El glifo del botón "Exportar TCX" (It. 12; 12-exportar-tcx/02-tech-design §1.3, §9.2, DT12-17; D12-03; 04-design-notes §2):
    // download (Windows: guarda en una carpeta) y ios_share (Android: la hoja de compartir) de Material Symbols Rounded, en la caja de
    // 24 × 24 (y hacia abajo). No están en el recorte de la fuente de íconos (y agregarlos pide red: issue), así que los dibuja
    // ExportGlyphView con Painter2D, como el corazón de la It. 11 (HeartGlyph).
    // Receta de los íconos dibujados (ASSET_SLOTS §4.4): trazo de 2,25 unidades (= peso 500 con opsz 24, el de la fuente de la app),
    // extremos y uniones redondeados, esquinas exteriores con radio 2 y cada pieza como UN contorno cerrado y simple (pares x, y; el
    // dibujo cierra el último tramo): dos formas encimadas se verían más claras donde se cruzan con un color con alfa.
    // Los contornos se arman una sola vez (constructor estático) y quedan en arreglos: dibujar no asigna.
    public static class ExportGlyph
    {
        public enum Kind
        {
            Download,
            Share,
        }

        public const double ViewBox = 24;

        // Trazo de la receta (§4.4) y su mitad.
        public const double Stroke = 2.25;
        private const double H = Stroke / 2;

        // Radio exterior de las esquinas de la bandeja y de la caja (el de Material Symbols Rounded).
        public const double CornerRadius = 2;

        // Tramos por cada 90° de arco: 4 alcanzan para que no se vean facetas a 24-36 px.
        private const int StepsPerQuarter = 4;

        // download: la flecha (punta abajo, en (12; 14,5) + medio trazo) y la bandeja en U abierta arriba.
        public const double DownloadTipX = 12, DownloadTipY = 14.5;
        public static readonly double[] DownloadArrow;
        public static readonly double[] DownloadTray;

        // ios_share: la flecha (punta arriba, en (12; 3,25) − medio trazo) que sale de la caja abierta arriba.
        public const double ShareTipX = 12, ShareTipY = 3.25;
        public static readonly double[] ShareArrow;
        public static readonly double[] ShareBox;

        private static readonly double[][] DownloadContours;
        private static readonly double[][] ShareContours;

        static ExportGlyph()
        {
            DownloadArrow = BuildDownloadArrow();
            DownloadTray = BuildDownloadTray();
            ShareArrow = BuildShareArrow();
            ShareBox = BuildShareBox();
            DownloadContours = new[] { DownloadArrow, DownloadTray };
            ShareContours = new[] { ShareArrow, ShareBox };
        }

        // Los contornos (pares x, y; cada uno se cierra solo) del glifo.
        public static IReadOnlyList<double[]> Contours(Kind kind)
        {
            switch (kind)
            {
                case Kind.Download:
                    return DownloadContours;
                case Kind.Share:
                    return ShareContours;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        // Flecha hacia abajo: eje de (12; 4,5) a la punta y brazos a 45° hasta (8; 10,5) y (16; 10,5), todo con trazo 2,25.
        // Sentido horario en pantalla (y hacia abajo): ángulos crecientes.
        private static double[] BuildDownloadArrow()
        {
            const double cx = DownloadTipX, tipY = DownloadTipY, topY = 4.5, armX = 4, armY = 10.5;
            var inner = InnerCorner(tipY, cx + H, up: true);
            var p = new List<double>(64);
            Arc(p, cx, topY, H, 180, 360, true);                 // remate redondo arriba del eje
            Add(p, cx + H, inner);                               // el eje se junta con el brazo derecho
            Arc(p, cx + armX, armY, H, 225, 405, true);          // remate del brazo derecho
            Arc(p, cx, tipY, H, 45, 135, true);                  // la punta, redondeada
            Arc(p, cx - armX, armY, H, 135, 315, true);          // remate del brazo izquierdo
            Add(p, cx - H, inner);                               // el brazo izquierdo se junta con el eje
            return p.ToArray();
        }

        // Bandeja: U de trazo 2,25 entre x 4 y 20, fondo en y 20, laterales con remate redondo arriba (centro en y 15,875), esquinas
        // exteriores de radio 2 e interiores vivas (como el original).
        private static double[] BuildDownloadTray()
        {
            const double left = 4, right = 20, bottom = 20, capY = 15.875, r = CornerRadius;
            var p = new List<double>(64);
            Arc(p, left + H, capY, H, 180, 360, true);
            Add(p, left + Stroke, bottom - Stroke);
            Add(p, right - Stroke, bottom - Stroke);
            Arc(p, right - H, capY, H, 180, 360, true);
            Arc(p, right - r, bottom - r, r, 0, 90, true);
            Arc(p, left + r, bottom - r, r, 90, 180, true);
            return p.ToArray();
        }

        // Flecha hacia arriba: punta en (12; 3,25), brazos a 45° hasta (9; 6,25) y (15; 6,25), eje hasta (12; 15,75).
        private static double[] BuildShareArrow()
        {
            const double cx = ShareTipX, tipY = ShareTipY, bottomY = 15.75, armX = 3, armY = 6.25;
            var inner = InnerCorner(tipY, cx + H, up: false);
            var p = new List<double>(64);
            Arc(p, cx, bottomY, H, 0, 180, true);                // remate redondo abajo del eje
            Add(p, cx - H, inner);                               // el eje se junta con el brazo izquierdo
            Arc(p, cx - armX, armY, H, 45, 225, true);           // remate del brazo izquierdo
            Arc(p, cx, tipY, H, 225, 315, true);                 // la punta, redondeada
            Arc(p, cx + armX, armY, H, 315, 495, true);          // remate del brazo derecho
            Add(p, cx + H, inner);                               // el brazo derecho se junta con el eje
            return p.ToArray();
        }

        // Caja abierta arriba: x 4 a 20, de y 8,75 a 23, trazo 2,25, esquinas exteriores de radio 2 e interiores vivas; el borde de
        // arriba solo en los costados, con remate redondo en x 8 y 16 (el hueco deja pasar la flecha).
        private static double[] BuildShareBox()
        {
            const double left = 4, right = 20, top = 8.75, bottom = 23, stubLeft = 8, stubRight = 16, r = CornerRadius;
            const double capY = top + H, innerTop = top + Stroke;
            var p = new List<double>(96);
            Arc(p, stubLeft, capY, H, 270, 450, true);           // remate del borde izquierdo
            Add(p, left + Stroke, innerTop);
            Add(p, left + Stroke, bottom - Stroke);
            Add(p, right - Stroke, bottom - Stroke);
            Add(p, right - Stroke, innerTop);
            Arc(p, stubRight, capY, H, 90, 270, true);           // remate del borde derecho
            Arc(p, right - r, top + r, r, 270, 360, true);
            Arc(p, right - r, bottom - r, r, 0, 90, true);
            Arc(p, left + r, bottom - r, r, 90, 180, true);
            Arc(p, left + r, top + r, r, 180, 270, true);
            return p.ToArray();
        }

        // Donde el borde del eje (x = edgeX) corta el borde interior de un brazo a 45° que sale de la punta (cx; tipY): ese borde pasa
        // por la punta corrida medio trazo hacia el eje. Para la flecha hacia abajo el corte queda arriba de la punta (up).
        private static double InnerCorner(double tipY, double edgeX, bool up)
        {
            var s = H / Math.Sqrt(2);
            var dx = edgeX - 12 + s;
            return up ? tipY - s - dx : tipY + s + dx;
        }

        private static void Add(List<double> p, double x, double y)
        {
            p.Add(Round(x));
            p.Add(Round(y));
        }

        // Arco de from a to grados (0 = derecha, 90 = abajo en pantalla), con los dos extremos incluidos.
        private static void Arc(List<double> p, double cx, double cy, double r, double from, double to, bool includeStart)
        {
            var steps = Math.Max(1, (int)DartMath.Round(Math.Abs(to - from) / 90 * StepsPerQuarter));
            for (var i = includeStart ? 0 : 1; i <= steps; i++)
            {
                var a = (from + (to - from) * i / steps) * Math.PI / 180;
                Add(p, cx + r * Math.Cos(a), cy + r * Math.Sin(a));
            }
        }

        // Cuatro decimales (DartMath, ADR 012): los mismos números en .NET 8 y en Mono, y nada de -0.
        private static double Round(double v)
        {
            var rounded = DartMath.Round(v * 10000) / 10000.0;
            return rounded == 0 ? 0 : rounded;
        }
    }
}
