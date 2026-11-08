using System;

namespace RowMate.Core.Scene
{
    // El atlas del público de la regata (DA8-12; 02-tech-design §2.7): 512 × 256 RGBA8, procedural y determinista, 9 cuadros de
    // 64 × 128 (una persona sentada de 0,5 × 1,0 m) en una grilla de 8 × 2 (los 7 lugares que sobran, transparentes): cuadro
    // = ropa·3 + pose, ropa 0 remera, 1 manga larga, 2 campera con cierre; pose 0 sentado, 1 brazos arriba, 2 aplauso. La ropa
    // va en blanco puro (255) para que el shader la tiña con el color de la instancia; piel y pantalón con su color. Fila 0 del
    // arreglo = abajo de la imagen (convención de Texture2D.SetPixels32).
    public static class CrowdAtlas
    {
        public const int Width = 512, Height = 256, CellWidth = 64, CellHeight = 128, Columns = 8, Cells = 9;

        public static int ByteCount => Width * Height * 4;

        public static void CellOrigin(int cell, out int x, out int y)
        {
            if (cell < 0 || cell >= Cells)
            {
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "CrowdAtlas: 9 cuadros.");
            }

            x = cell % Columns * CellWidth;
            y = cell / Columns * CellHeight;
        }

        public static void Bake(byte[] rgba)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }

            if (rgba.Length != ByteCount)
            {
                throw new ArgumentException("CrowdAtlas: hacen falta " + ByteCount + " bytes.", nameof(rgba));
            }

            Array.Clear(rgba, 0, rgba.Length);
            for (var cell = 0; cell < Cells; cell++)
            {
                CellOrigin(cell, out var ox, out var oy);
                var clothes = cell / 3;
                var pose = cell % 3;
                for (var py = 0; py < CellHeight; py++)
                {
                    for (var px = 0; px < CellWidth; px++)
                    {
                        // u ∈ [−0,5; 0,5] de ancho, v ∈ [0; 1] de alto (en metros de la persona: 0,5 × 1,0).
                        var u = (px + 0.5) / CellWidth - 0.5;
                        var v = (py + 0.5) / CellHeight;
                        var k = Classify(u * 0.5, v, clothes, pose);
                        if (k == 0)
                        {
                            continue;
                        }

                        var i = ((oy + py) * Width + ox + px) * 4;
                        Color(k, out rgba[i], out rgba[i + 1], out rgba[i + 2]);
                        rgba[i + 3] = 255;
                    }
                }
            }
        }

        // 0 = vacío, 1 = ropa (blanco), 2 = piel, 3 = pantalón, 4 = pelo, 5 = cierre de la campera.
        private static int Classify(double x, double y, int clothes, int pose)
        {
            // Cabeza: círculo de r 0,075 m a 0,84 m, pelo arriba.
            var hx = x;
            var hy = y - 0.84;
            if (hx * hx + hy * hy < 0.075 * 0.075)
            {
                return hy > 0.03 ? 4 : 2;
            }

            // Cuello.
            if (Math.Abs(x) < 0.03 && y >= 0.74 && y < 0.78)
            {
                return 2;
            }

            // Torso: trapecio de 0,30 m (hombros) a 0,26 m (cintura), de 0,42 a 0,76 m.
            var half = 0.13 + 0.02 * (y - 0.42) / 0.34;
            if (y >= 0.42 && y < 0.76 && Math.Abs(x) < half)
            {
                return clothes == 2 && Math.Abs(x) < 0.008 ? 5 : 1;
            }

            // Piernas sentadas: muslos de frente (0,30 a 0,42) y pantorrillas (0 a 0,30), dos de 0,09 m.
            if (y < 0.42 && (Math.Abs(x - 0.06) < 0.045 || Math.Abs(x + 0.06) < 0.045))
            {
                return 3;
            }

            // Brazos según la pose.
            if (Arm(x, y, pose, clothes, -1, out var leftKind) || Arm(x, y, pose, clothes, 1, out leftKind))
            {
                return leftKind;
            }

            return 0;
        }

        private static bool Arm(double x, double y, int pose, int clothes, int side, out int kind)
        {
            kind = 0;
            var sx = side * x;   // del lado del brazo, positivo hacia afuera
            double ax, ay, bx, by;
            switch (pose)
            {
                case 1:
                    // Brazos arriba: del hombro (0,13; 0,74) a la mano (0,18; 1,0).
                    ax = 0.13; ay = 0.74; bx = 0.19; by = 0.99;
                    break;
                case 2:
                    // Aplauso: el codo afuera y las manos juntas al frente del pecho.
                    ax = 0.13; ay = 0.74; bx = 0.03; by = 0.60;
                    break;
                default:
                    // Sentado: brazos al costado, manos sobre los muslos.
                    ax = 0.14; ay = 0.74; bx = 0.12; by = 0.44;
                    break;
            }

            // Distancia al segmento hombro-mano, grosor 0,035 m.
            var dx = bx - ax;
            var dy = by - ay;
            var t = SceneMath.Clamp01(((sx - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
            var px = ax + dx * t - sx;
            var py = ay + dy * t - y;
            if (px * px + py * py >= 0.035 * 0.035)
            {
                return false;
            }

            // La remera deja el antebrazo descubierto (la mitad de abajo del brazo); la mano siempre es piel.
            kind = t > 0.85 || (clothes == 0 && t > 0.45) ? 2 : 1;
            return true;
        }

        private static void Color(int kind, out byte r, out byte g, out byte b)
        {
            switch (kind)
            {
                case 1:
                    r = 255; g = 255; b = 255;
                    return;
                case 2:
                    r = 214; g = 168; b = 134;
                    return;
                case 3:
                    r = 52; g = 58; b = 72;
                    return;
                case 4:
                    r = 58; g = 40; b = 30;
                    return;
                default:
                    r = 90; g = 90; b = 90;
                    return;
            }
        }
    }
}
