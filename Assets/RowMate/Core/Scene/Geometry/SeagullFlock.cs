using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // Las 5 gaviotas de la costa en una sola malla (QA-V8-01; AC-SCN-19-04, -20-06; 00-arte-entornos §3.4): la gaviota low-poly de
    // SeagullMesh copiada 5 veces, con sus índices por las dos caras (se ven desde abajo) y el color del legacy (#2B2B2B, silueta
    // oscura). Por cuadro, Pose pone cada copia a SeagullDistance m de la cámara en su acimut (relativo al rumbo de la cámara) y su
    // elevación sobre el horizonte (SeagullFlight): las alas a lo ancho de la pantalla, el tamaño (5 + i)/5 × el fundido de la vuelta
    // y el aleteo sin(6t + i) en el vértice (la punta ± 0,25 m, en proporción a la distancia al cuerpo). A 90 m, la envergadura de
    // 1,1 m mide 5 px de TV (411·1,1/90), como el legacy. Fuera de la costa (alfa 0) la copia queda en un punto: no se ve.
    // Los arreglos se reservan al crear; Pose no asigna.
    public sealed class SeagullFlock
    {
        public const double Distance = 90;
        public const double FlapTip = 0.25;

        private readonly MeshData _template;

        public SeagullFlock()
        {
            _template = new MeshData(SeagullMesh.VertexCapacity, SeagullMesh.IndexCapacity);
            SeagullMesh.Build(_template);
            PerGull = _template.VertexCount;
            var count = SeagullFlight.Count;
            VertexCount = PerGull * count;
            Positions = new float[VertexCount * 3];
            Normals = new float[VertexCount * 3];
            Colors = new float[VertexCount * 4];
            Indices = new int[_template.IndexCount * 2 * count];
            var tone = SceneColor.FromArgb(CoastArt.Seagull);
            var n = 0;
            for (var g = 0; g < count; g++)
            {
                var b = g * PerGull;
                for (var v = 0; v < PerGull; v++)
                {
                    Normals[(b + v) * 3 + 1] = 1;
                    Colors[(b + v) * 4] = (float)tone.R;
                    Colors[(b + v) * 4 + 1] = (float)tone.G;
                    Colors[(b + v) * 4 + 2] = (float)tone.B;
                    Colors[(b + v) * 4 + 3] = 0.5f;
                }

                for (var i = 0; i < _template.IndexCount; i += 3)
                {
                    int i0 = _template.Indices[i] + b, i1 = _template.Indices[i + 1] + b, i2 = _template.Indices[i + 2] + b;
                    Indices[n++] = i0;
                    Indices[n++] = i1;
                    Indices[n++] = i2;
                    Indices[n++] = i0;
                    Indices[n++] = i2;
                    Indices[n++] = i1;
                }
            }
        }

        public int PerGull { get; }

        public int VertexCount { get; }

        public float[] Positions { get; }

        public float[] Normals { get; }

        public float[] Colors { get; }

        public int[] Indices { get; }

        // Cuántas se ven en el último Pose (alfa y tamaño > 0).
        public int Visible { get; private set; }

        // El centro de la gaviota i en el último Pose (pruebas).
        public void Center(int i, out double x, out double y, out double z)
        {
            x = _cx[i];
            y = _cy[i];
            z = _cz[i];
        }

        private readonly double[] _cx = new double[SeagullFlight.Count];
        private readonly double[] _cy = new double[SeagullFlight.Count];
        private readonly double[] _cz = new double[SeagullFlight.Count];

        public void Pose(SeagullInstance[] gulls, in CameraPose camera)
        {
            if (gulls == null)
            {
                throw new ArgumentNullException(nameof(gulls));
            }

            Visible = 0;
            var yaw = SceneMath.DegToRad(camera.YawDeg);
            var half = CoastArt.SeagullWingspan / 2;
            for (var g = 0; g < SeagullFlight.Count; g++)
            {
                var gull = g < gulls.Length ? gulls[g] : default;
                var az = yaw + gull.AzimuthRad;
                var cosEl = Math.Cos(gull.ElevationRad);
                // La dirección de la mirada hacia la gaviota (y: arriba; z: adelante de la cámara sin girar).
                double fx = Math.Sin(az) * cosEl, fy = Math.Sin(gull.ElevationRad), fz = Math.Cos(az) * cosEl;
                double rx = Math.Cos(az), rz = -Math.Sin(az);
                var cx = camera.X + Distance * fx;
                var cy = camera.Y + Distance * fy;
                var cz = camera.Z + Distance * fz;
                _cx[g] = cx;
                _cy[g] = cy;
                _cz[g] = cz;
                var s = gull.Size * gull.Alpha;
                if (s > 0)
                {
                    Visible++;
                }

                // Hacia adelante en el plano (el cuerpo de la gaviota apunta lejos de la cámara).
                double hx = Math.Sin(az), hz = Math.Cos(az);
                var b = g * PerGull;
                for (var v = 0; v < PerGull; v++)
                {
                    double lx = _template.X(v), ly = _template.Y(v), lz = _template.Z(v);
                    var reach = Math.Max(0, Math.Min(1, (Math.Abs(lx) - 0.05) / (half - 0.05)));
                    ly += FlapTip * gull.Flap * reach;
                    var o = (b + v) * 3;
                    Positions[o] = (float)(cx + s * (lx * rx + lz * hx));
                    Positions[o + 1] = (float)(cy + s * ly);
                    Positions[o + 2] = (float)(cz + s * (lx * rz + lz * hz));
                }
            }
        }
    }
}
