using System;

namespace RowMate.Core.Scene.Geometry
{
    // La malla del agua avanzada (PA6b-02; 06b-agua-avanzada/02-tech-design §3.4, §4.1): centrada en la cámara del contrato (cinta), un
    // cuadrado interior de 40 × 40 m con paso de 0,5 m (1 m en Bajo) y anillos cuadrados que duplican el paso (4 celdas de ancho cada
    // uno) hasta el borde a 1,5 km. ≤ 20 k triángulos en todos los niveles. Plana en y = 0 (B0 de la costa la mueve en el vértice);
    // queda QUIETA en cinta (ronda del salto, DT6b-27: antes WP-B la movía con −Mod(D; 16) y cada 16 m los anillos re-muestreaban la ola).
    // Entre anillos quedan juntas en T: con el agua plana no se ven. Determinista.
    // Color de vértice: r = el anillo (0 el interior) / 16, g = el paso en m / 256, b = el paso MÁXIMO del anillo en ese lugar / 256
    // (MaxStep: continuo entre anillos, lo usa el vértice para apagar las ondas que el paso no alcanza), a libre. UV = (x; z) en metros.
    public static class WaterRingMesh
    {
        public const double InnerHalf = 20, EdgeHalf = 1500, MaxTriangles = 20000;
        public const double HighStep = 0.5, LowStep = 1.0;
        public const int RingCells = 4;

        public static double InnerStep(QualityTier tier)
        {
            return tier == QualityTier.Low ? LowStep : HighStep;
        }

        // Cuántos vértices e índices hacen falta (lo mismo que arma Build).
        public static void Counts(QualityTier tier, out int vertices, out int indices)
        {
            vertices = 0;
            indices = 0;
            var step = InnerStep(tier);
            AddRect(-InnerHalf, InnerHalf, -InnerHalf, InnerHalf, step, ref vertices, ref indices);
            var inner = InnerHalf;
            var s = step;
            while (inner < EdgeHalf)
            {
                s *= 2;
                var outer = Math.Min(EdgeHalf, inner + RingCells * s);
                Ring(inner, outer, s, ref vertices, ref indices);
                inner = outer;
            }
        }

        // El paso más grueso que puede tener la malla a la distancia de Chebyshev max(|x|; |z|): el anillo de paso s empieza en
        // 20 − 8·s₀ + 4·s (s₀ el paso interior), así que hasta d el paso es como mucho (d − 20 + 8·s₀)/4; nunca menos que s₀ ni más que
        // 256 (el último anillo). Es lineal en d: igual en los vértices que comparten dos anillos.
        public static double MaxStep(double innerStep, double x, double z)
        {
            var d = Math.Max(Math.Abs(x), Math.Abs(z));
            return Math.Min(MaxRingStep, Math.Max(innerStep, (d - InnerHalf + 2 * RingCells * innerStep) / RingCells));
        }

        public const double MaxRingStep = 256;

        public static int Rings(QualityTier tier)
        {
            var n = 0;
            var inner = InnerHalf;
            var s = InnerStep(tier);
            while (inner < EdgeHalf)
            {
                s *= 2;
                inner = Math.Min(EdgeHalf, inner + RingCells * s);
                n++;
            }

            return n;
        }

        public static MeshData Create(QualityTier tier)
        {
            Counts(tier, out var v, out var i);
            var mesh = new MeshData(v, i);
            Build(tier, mesh);
            return mesh;
        }

        public static void Build(QualityTier tier, MeshData into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            into.Clear();
            var step = InnerStep(tier);
            Rect(into, -InnerHalf, InnerHalf, -InnerHalf, InnerHalf, step, 0, step);
            var inner = InnerHalf;
            var s = step;
            var ring = 0;
            while (inner < EdgeHalf)
            {
                s *= 2;
                ring++;
                var outer = Math.Min(EdgeHalf, inner + RingCells * s);
                // El anillo en cuatro tiras: adelante y atrás de borde a borde, izquierda y derecha entre ellas.
                Rect(into, -outer, outer, inner, outer, s, ring, step);
                Rect(into, -outer, outer, -outer, -inner, s, ring, step);
                Rect(into, -outer, -inner, -inner, inner, s, ring, step);
                Rect(into, inner, outer, -inner, inner, s, ring, step);
                inner = outer;
            }
        }

        private static void Ring(double inner, double outer, double s, ref int vertices, ref int indices)
        {
            AddRect(-outer, outer, inner, outer, s, ref vertices, ref indices);
            AddRect(-outer, outer, -outer, -inner, s, ref vertices, ref indices);
            AddRect(-outer, -inner, -inner, inner, s, ref vertices, ref indices);
            AddRect(inner, outer, -inner, inner, s, ref vertices, ref indices);
        }

        private static int Cells(double from, double to, double step)
        {
            return Math.Max(1, (int)Math.Ceiling((to - from) / step - 1e-9));
        }

        private static void AddRect(double x0, double x1, double z0, double z1, double step, ref int vertices, ref int indices)
        {
            var nx = Cells(x0, x1, step);
            var nz = Cells(z0, z1, step);
            vertices += (nx + 1) * (nz + 1);
            indices += 6 * nx * nz;
        }

        private static void Rect(MeshData mesh, double x0, double x1, double z0, double z1, double step, int ring, double innerStep)
        {
            var nx = Cells(x0, x1, step);
            var nz = Cells(z0, z1, step);
            var first = mesh.VertexCount;
            for (var j = 0; j <= nz; j++)
            {
                var z = z0 + (z1 - z0) * j / nz;
                for (var i = 0; i <= nx; i++)
                {
                    var x = x0 + (x1 - x0) * i / nx;
                    mesh.AddVertex(x, 0, z, 0, 1, 0, ring / 16.0, step / 256.0, MaxStep(innerStep, x, z) / MaxRingStep, 0, x, z);
                }
            }

            for (var j = 0; j < nz; j++)
            {
                for (var i = 0; i < nx; i++)
                {
                    var a = first + j * (nx + 1) + i;
                    var b = a + 1;
                    var c = a + nx + 1;
                    var d = c + 1;
                    // De frente hacia arriba (y+) con la convención de las demás mallas del agua.
                    mesh.AddTriangle(a, c, b);
                    mesh.AddTriangle(b, c, d);
                }
            }
        }
    }
}
