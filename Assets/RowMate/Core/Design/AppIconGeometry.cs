using System;
using System.Collections.Generic;

namespace RowMate.Core.Design
{
    // Forma de una pieza del ícono (It. 14, 00-arte §1.3; DT14-01).
    public enum IconShape
    {
        // Segmento A–B con extremos redondos de radio R (caña, pala, empuñadura y sus recortes).
        Stadium,

        // Disco de centro C y radio R recortado a y ≤ YMax (el sol), con degradé vertical de su borde de arriba a la base.
        HalfDisc,

        // Trazo y = Y0 + Sign·Amp·sin(2π·(x − X0)/Period) para x ∈ [X0; X1], de grosor 2R y puntas redondas (las olas).
        SineStroke,

        // Disco radial de centro C y radio R: alfa Alpha0 en el centro → 0 en el borde con smoothstep (el resplandor).
        RadialGlow,
    }

    // Una pieza de la marca en la caja de 100 × 100 (origen arriba a la izquierda, y hacia abajo). KnockOut = borra (alfa → 0)
    // lo dibujado antes donde la pieza cubre; si no, pinta con su color (o su degradé). Inmutable.
    public sealed class IconPiece
    {
        private const double SineStep = 0.25;

        private IconPiece(string name, IconShape shape, bool knockOut, Rgb color, Rgb color2)
        {
            Name = name;
            Shape = shape;
            KnockOut = knockOut;
            Color = color;
            Color2 = color2;
            PolylineX = new double[0];
            PolylineY = new double[0];
        }

        public string Name { get; }

        public IconShape Shape { get; }

        public bool KnockOut { get; }

        public Rgb Color { get; }

        // El color de la base del degradé del sol (= Color si no hay degradé).
        public Rgb Color2 { get; }

        public double Ax { get; private set; }

        public double Ay { get; private set; }

        public double Bx { get; private set; }

        public double By { get; private set; }

        // Stadium y SineStroke: la mitad del grosor; HalfDisc y RadialGlow: el radio.
        public double R { get; private set; }

        public double YMax { get; private set; }

        public double Alpha0 { get; private set; }

        public double Y0 { get; private set; }

        public double Amp { get; private set; }

        public double X0 { get; private set; }

        public double X1 { get; private set; }

        public double Period { get; private set; }

        public double Sign { get; private set; }

        // La línea quebrada del trazo (pasos de 0,25 en x), calculada con IconMath.Sin.
        public double[] PolylineX { get; private set; }

        public double[] PolylineY { get; private set; }

        public double MinX { get; private set; }

        public double MinY { get; private set; }

        public double MaxX { get; private set; }

        public double MaxY { get; private set; }

        // Grosor (2R) del trazo, la caña o la pala, en unidades de la caja.
        public double Thickness => 2 * R;

        public static IconPiece Stadium(string name, double ax, double ay, double bx, double by, double r, Rgb color, bool knockOut)
        {
            var piece = new IconPiece(name, IconShape.Stadium, knockOut, color, color)
            {
                Ax = ax,
                Ay = ay,
                Bx = bx,
                By = by,
                R = r,
            };
            piece.MinX = Math.Min(ax, bx) - r;
            piece.MaxX = Math.Max(ax, bx) + r;
            piece.MinY = Math.Min(ay, by) - r;
            piece.MaxY = Math.Max(ay, by) + r;
            return piece;
        }

        public static IconPiece HalfDisc(string name, double cx, double cy, double r, double yMax, Rgb top, Rgb bottom)
        {
            var piece = new IconPiece(name, IconShape.HalfDisc, false, top, bottom)
            {
                Ax = cx,
                Ay = cy,
                R = r,
                YMax = yMax,
            };
            piece.MinX = cx - r;
            piece.MaxX = cx + r;
            piece.MinY = cy - r;
            piece.MaxY = Math.Min(cy + r, yMax);
            return piece;
        }

        public static IconPiece Sine(string name, double y0, double amp, double x0, double x1, double period, double sign, double width, Rgb color)
        {
            var piece = new IconPiece(name, IconShape.SineStroke, false, color, color)
            {
                Y0 = y0,
                Amp = amp,
                X0 = x0,
                X1 = x1,
                Period = period,
                Sign = sign,
                R = width / 2,
            };
            var segments = (int)Math.Floor((x1 - x0) / SineStep + 0.5);
            var xs = new double[segments + 1];
            var ys = new double[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var x = i == segments ? x1 : x0 + i * SineStep;
                xs[i] = x;
                ys[i] = y0 + sign * amp * IconMath.Sin(IconMath.TwoPi * (x - x0) / period);
            }

            piece.PolylineX = xs;
            piece.PolylineY = ys;
            piece.MinX = x0 - piece.R;
            piece.MaxX = x1 + piece.R;
            piece.MinY = y0 - amp - piece.R;
            piece.MaxY = y0 + amp + piece.R;
            return piece;
        }

        public static IconPiece Glow(string name, double cx, double cy, double r, double alpha0, Rgb color)
        {
            var piece = new IconPiece(name, IconShape.RadialGlow, false, color, color)
            {
                Ax = cx,
                Ay = cy,
                R = r,
                Alpha0 = alpha0,
            };
            piece.MinX = cx - r;
            piece.MaxX = cx + r;
            piece.MinY = cy - r;
            piece.MaxY = cy + r;
            return piece;
        }

        // Distancia con signo a la forma (negativa adentro), en unidades de la caja. Si el punto está a más de `far` de la caja
        // que la contiene, devuelve un valor > far sin calcular la distancia exacta. El resplandor no tiene borde: su distancia
        // al disco (para el recorte por caja).
        public double SignedDistance(double x, double y, double far)
        {
            if (x < MinX - far || x > MaxX + far || y < MinY - far || y > MaxY + far)
            {
                return far + 1;
            }

            switch (Shape)
            {
                case IconShape.Stadium:
                    return SegmentDistance(x, y, Ax, Ay, Bx, By) - R;
                case IconShape.HalfDisc:
                    return Math.Max(Length(x - Ax, y - Ay) - R, y - YMax);
                case IconShape.RadialGlow:
                    return Length(x - Ax, y - Ay) - R;
                default:
                    return SineDistance(x, y, far) - R;
            }
        }

        // El color en la altura y (el degradé del sol de su borde de arriba a la base; las demás, su color).
        public void ColorAt(double y, out double r, out double g, out double b)
        {
            if (Shape != IconShape.HalfDisc)
            {
                r = Color.R;
                g = Color.G;
                b = Color.B;
                return;
            }

            var top = Ay - R;
            var t = IconMath.Clamp01((y - top) / (YMax - top));
            r = Color.R + (Color2.R - Color.R) * t;
            g = Color.G + (Color2.G - Color.G) * t;
            b = Color.B + (Color2.B - Color.B) * t;
        }

        // El alfa del resplandor en (x, y): Alpha0 · (1 − smoothstep(d / R)).
        public double GlowAlpha(double x, double y)
        {
            var d = Length(x - Ax, y - Ay);
            return d >= R ? 0 : Alpha0 * (1 - IconMath.SmoothStep(d / R));
        }

        internal static double Length(double dx, double dy)
        {
            return Math.Sqrt(dx * dx + dy * dy);
        }

        internal static double SegmentDistance(double px, double py, double ax, double ay, double bx, double by)
        {
            var abx = bx - ax;
            var aby = by - ay;
            var len2 = abx * abx + aby * aby;
            var t = len2 > 0 ? ((px - ax) * abx + (py - ay) * aby) / len2 : 0;
            t = IconMath.Clamp01(t);
            return Length(px - (ax + t * abx), py - (ay + t * aby));
        }

        private double SineDistance(double x, double y, double far)
        {
            // Solo los segmentos cuya x cae a menos de R + far del punto (la curva es función de x).
            var reach = R + far;
            var first = (int)Math.Floor((x - reach - X0) / SineStep) - 1;
            var last = (int)Math.Floor((x + reach - X0) / SineStep) + 1;
            var segments = PolylineX.Length - 1;
            if (first < 0)
            {
                first = 0;
            }

            if (last > segments - 1)
            {
                last = segments - 1;
            }

            var best = reach + 1;
            for (var i = first; i <= last; i++)
            {
                var d = SegmentDistance(x, y, PolylineX[i], PolylineY[i], PolylineX[i + 1], PolylineY[i + 1]);
                if (d < best)
                {
                    best = d;
                }
            }

            return best;
        }
    }

    // La marca "amanecer de regata" como datos (It. 14, 00-arte §1.3 y §1.4; DT14-03, DT14-04): dos remos de scull cruzados en
    // X delante de un sol que asoma sobre el horizonte, con las palas en el agua. Fuente propia y procedural (AC-APP-16-08): ni
    // el JPEG del legacy ni assets de terceros.
    // Colores (DT14-03, manda la spec): remos y ola 1 en accent.primary, ola 2 en brand.seed, el sol con el degradé
    // step.warmup → metric.split y el resplandor en metric.split. El fondo de las placas es bg.app liso (AppIconRaster).
    public static class AppIconGeometry
    {
        // El centro de la caja (para el radio máximo) y el cruce de los remos (00-arte §1.3).
        public const double Center = 50;
        public const double CrossX = 50;
        public const double CrossY = 47.757;

        // Remo derecho (OR): empuñadura H y punta de la pala T. El izquierdo (OL) es su espejo en x = 50.
        public const double HandleX = 18;
        public const double HandleY = 8;
        public const double TipX = 84;
        public const double TipY = 90;

        // Largo del remo = |T − H| = √(66² + 82²).
        public static readonly double OarLength = Math.Sqrt((TipX - HandleX) * (TipX - HandleX) + (TipY - HandleY) * (TipY - HandleY));

        // La marca de un nivel, en orden de dibujo (cada pieza tapa a las anteriores). glow = false la deja sin resplandor (el
        // frente adaptativo de Android).
        public static IReadOnlyList<IconPiece> Mark(AppIconLevel level, bool glow = true)
        {
            var oar = Tokens.AccentPrimary.Rgb;
            var pieces = new List<IconPiece>();
            switch (level)
            {
                case AppIconLevel.M:
                    if (glow)
                    {
                        pieces.Add(IconPiece.Glow("G", 50, 58, 46, 0.20, Tokens.MetricSplit.Rgb));
                    }

                    pieces.Add(IconPiece.HalfDisc("S", 50, 58, 22, 58, Tokens.StepWarmup.Rgb, Tokens.MetricSplit.Rgb));
                    pieces.Add(IconPiece.Sine("W1", 66, 2.2, 10, 90, 20, 1, 4.4, Tokens.AccentPrimary.Rgb));
                    pieces.Add(IconPiece.Sine("W2", 76, 2.2, 25, 75, 20, -1, 4.4, Tokens.BrandSeed.Rgb));
                    AddOar(pieces, "OL", false, true, 3.5, 2.5, 80, 98.762, 6.5, 3.0, oar);
                    AddOar(pieces, "OR", true, true, 3.5, 2.5, 80, 98.762, 6.5, 3.0, oar);
                    break;
                case AppIconLevel.S:
                    pieces.Add(IconPiece.HalfDisc("S", 50, 58, 23, 58, Tokens.StepWarmup.Rgb, Tokens.MetricSplit.Rgb));
                    pieces.Add(IconPiece.Sine("W1", 66, 2.6, 12, 88, 19, 1, 6, Tokens.AccentPrimary.Rgb));
                    AddOar(pieces, "OL", false, false, 3.5, 3.5, 80, 98.762, 8, 3.5, oar);
                    AddOar(pieces, "OR", true, false, 3.5, 3.5, 80, 98.762, 8, 3.5, oar);
                    break;
                default:
                    // DT14-04: solo los remos, sin recorte (la cruz en una pieza); caña r 7,5 y pala r 11.
                    AddOar(pieces, "OL", false, false, 3.5, 7.5, 78, 95.762, 11, 0, oar);
                    AddOar(pieces, "OR", true, false, 3.5, 7.5, 78, 95.762, 11, 0, oar);
                    break;
            }

            return pieces;
        }

        // El punto del remo a la distancia s de la empuñadura: P(s) = H + s·d.
        public static void OarPoint(bool right, double s, out double x, out double y)
        {
            var hx = right ? HandleX : 100 - HandleX;
            var tx = right ? TipX : 100 - TipX;
            x = hx + s * (tx - hx) / OarLength;
            y = HandleY + s * (TipY - HandleY) / OarLength;
        }

        // El radio máximo de lo que se pinta (sin el resplandor, que se desvanece a 0, ni los recortes) desde (50; 50).
        public static double MaxRadius(IReadOnlyList<IconPiece> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            var max = 0.0;
            foreach (var piece in pieces)
            {
                if (piece.KnockOut || piece.Shape == IconShape.RadialGlow)
                {
                    continue;
                }

                double reach;
                switch (piece.Shape)
                {
                    case IconShape.Stadium:
                        reach = Math.Max(IconPiece.Length(piece.Ax - Center, piece.Ay - Center), IconPiece.Length(piece.Bx - Center, piece.By - Center)) + piece.R;
                        break;
                    case IconShape.HalfDisc:
                        reach = IconPiece.Length(piece.Ax - Center, piece.Ay - Center) + piece.R;
                        break;
                    default:
                        reach = 0;
                        for (var i = 0; i < piece.PolylineX.Length; i++)
                        {
                            reach = Math.Max(reach, IconPiece.Length(piece.PolylineX[i] - Center, piece.PolylineY[i] - Center) + piece.R);
                        }

                        break;
                }

                max = Math.Max(max, reach);
            }

            return max;
        }

        // Un remo: primero el recorte (la unión de sus piezas con r + knock; ninguno si knock = 0) y después la empuñadura (si
        // va), la caña y la pala.
        private static void AddOar(List<IconPiece> pieces, string name, bool right, bool handle, double shaftStart, double shaftR,
                                   double bladeStart, double bladeEnd, double bladeR, double knock, Rgb color)
        {
            const double HandleEnd = 12;
            const double HandleR = 3.5;
            if (knock > 0)
            {
                if (handle)
                {
                    pieces.Add(Segment(name + ".handle.ko", right, shaftStart, HandleEnd, HandleR + knock, color, true));
                }

                pieces.Add(Segment(name + ".shaft.ko", right, shaftStart, bladeStart, shaftR + knock, color, true));
                pieces.Add(Segment(name + ".blade.ko", right, bladeStart, bladeEnd, bladeR + knock, color, true));
            }

            if (handle)
            {
                pieces.Add(Segment(name + ".handle", right, shaftStart, HandleEnd, HandleR, color, false));
            }

            pieces.Add(Segment(name + ".shaft", right, shaftStart, bladeStart, shaftR, color, false));
            pieces.Add(Segment(name + ".blade", right, bladeStart, bladeEnd, bladeR, color, false));
        }

        private static IconPiece Segment(string name, bool right, double s0, double s1, double r, Rgb color, bool knockOut)
        {
            OarPoint(right, s0, out var ax, out var ay);
            OarPoint(right, s1, out var bx, out var by);
            return IconPiece.Stadium(name, ax, ay, bx, by, r, color, knockOut);
        }
    }
}
