using System;

namespace RowMate.Core.Scene.Geometry
{
    // Piezas comunes de las mallas puras: un cuadrilátero subdividido, una caja y un cilindro, con el color de vértice dado.
    internal static class MeshShapes
    {
        // Un cuadrilátero p0 → p1 → p2 → p3 (en el sentido que mira a la normal), subdividido nu × nv.
        public static void Quad(MeshData mesh, double x0, double y0, double z0, double x1, double y1, double z1, double x2, double y2, double z2,
                                double x3, double y3, double z3, double nx, double ny, double nz, uint color, double material, int nu = 1, int nv = 1)
        {
            var c = SceneColor.FromArgb(color);
            var first = mesh.VertexCount;
            for (var j = 0; j <= nv; j++)
            {
                var v = j / (double)nv;
                for (var i = 0; i <= nu; i++)
                {
                    var u = i / (double)nu;
                    // Bilineal entre los cuatro vértices.
                    var ax = x0 + (x1 - x0) * u;
                    var ay = y0 + (y1 - y0) * u;
                    var az = z0 + (z1 - z0) * u;
                    var bx = x3 + (x2 - x3) * u;
                    var by = y3 + (y2 - y3) * u;
                    var bz = z3 + (z2 - z3) * u;
                    mesh.AddVertex(ax + (bx - ax) * v, ay + (by - ay) * v, az + (bz - az) * v, nx, ny, nz, c.R, c.G, c.B, material, u, v);
                }
            }

            for (var j = 0; j < nv; j++)
            {
                for (var i = 0; i < nu; i++)
                {
                    var a = first + j * (nu + 1) + i;
                    var b = a + 1;
                    var d = a + nu + 1;
                    var e = d + 1;
                    mesh.AddTriangle(a, d, b);
                    mesh.AddTriangle(b, d, e);
                }
            }
        }

        // Una caja alineada a los ejes (sin la cara de abajo si openBottom).
        public static void Box(MeshData mesh, double minX, double minY, double minZ, double maxX, double maxY, double maxZ, uint color, double material,
                               bool openBottom = true, int subdivisions = 1)
        {
            var n = subdivisions;
            // Frente (−z), fondo (+z), izquierda (−x), derecha (+x), arriba (+y), abajo (−y).
            Quad(mesh, minX, minY, minZ, maxX, minY, minZ, maxX, maxY, minZ, minX, maxY, minZ, 0, 0, -1, color, material, n, n);
            Quad(mesh, maxX, minY, maxZ, minX, minY, maxZ, minX, maxY, maxZ, maxX, maxY, maxZ, 0, 0, 1, color, material, n, n);
            Quad(mesh, minX, minY, maxZ, minX, minY, minZ, minX, maxY, minZ, minX, maxY, maxZ, -1, 0, 0, color, material, n, n);
            Quad(mesh, maxX, minY, minZ, maxX, minY, maxZ, maxX, maxY, maxZ, maxX, maxY, minZ, 1, 0, 0, color, material, n, n);
            Quad(mesh, minX, maxY, minZ, maxX, maxY, minZ, maxX, maxY, maxZ, minX, maxY, maxZ, 0, 1, 0, color, material, n, n);
            if (!openBottom)
            {
                Quad(mesh, minX, minY, maxZ, maxX, minY, maxZ, maxX, minY, minZ, minX, minY, minZ, 0, -1, 0, color, material, n, n);
            }
        }

        // Un cilindro vertical (sin tapas) de radio r entre y0 e y1, centrado en (cx; cz).
        public static void Cylinder(MeshData mesh, double cx, double cz, double r, double y0, double y1, int sides, uint color, double material)
        {
            var c = SceneColor.FromArgb(color);
            var first = mesh.VertexCount;
            for (var ring = 0; ring < 2; ring++)
            {
                var y = ring == 0 ? y0 : y1;
                for (var j = 0; j <= sides; j++)
                {
                    var t = 2 * Math.PI * j / sides;
                    mesh.AddVertex(cx + Math.Cos(t) * r, y, cz + Math.Sin(t) * r, Math.Cos(t), 0, Math.Sin(t), c.R, c.G, c.B, material, j / (double)sides, ring);
                }
            }

            for (var j = 0; j < sides; j++)
            {
                var a = first + j;
                var b = first + j + 1;
                var d = first + sides + 1 + j;
                var e = first + sides + 2 + j;
                mesh.AddTriangle(a, d, b);
                mesh.AddTriangle(b, d, e);
            }
        }

        // It. 8: un tronco de cono vertical (sin tapas), de r0 en y0 a r1 en y1: 2·sides triángulos.
        public static void Frustum(MeshData mesh, double cx, double cz, double r0, double r1, double y0, double y1, int sides, uint color, double material)
        {
            var c = SceneColor.FromArgb(color);
            var first = mesh.VertexCount;
            var slope = (r0 - r1) / Math.Max(1e-9, y1 - y0);
            for (var ring = 0; ring < 2; ring++)
            {
                var y = ring == 0 ? y0 : y1;
                var r = ring == 0 ? r0 : r1;
                for (var j = 0; j <= sides; j++)
                {
                    var t = 2 * Math.PI * j / sides;
                    mesh.AddVertex(cx + Math.Cos(t) * r, y, cz + Math.Sin(t) * r, Math.Cos(t), slope, Math.Sin(t), c.R, c.G, c.B, material, j / (double)sides, ring);
                }
            }

            Bands(mesh, first, sides, 1);
        }

        // Un elipsoide (bandas × gajos; los polos son anillos de radio 0): 2·slices·bands triángulos.
        public static void Ellipsoid(MeshData mesh, double cx, double cy, double cz, double rx, double ry, double rz, int bands, int slices, uint color,
                                     double material)
        {
            var c = SceneColor.FromArgb(color);
            var first = mesh.VertexCount;
            for (var s = 0; s <= bands; s++)
            {
                var phi = -Math.PI / 2 + Math.PI * s / bands;
                var cp = Math.Cos(phi);
                var sp = Math.Sin(phi);
                for (var j = 0; j <= slices; j++)
                {
                    var t = 2 * Math.PI * j / slices;
                    var nx = cp * Math.Cos(t);
                    var nz = cp * Math.Sin(t);
                    mesh.AddVertex(cx + rx * nx, cy + ry * sp, cz + rz * nz, nx / rx, sp / ry, nz / rz, c.R, c.G, c.B, material, j / (double)slices,
                        s / (double)bands);
                }
            }

            Bands(mesh, first, slices, bands);
        }

        // Un triángulo con su normal (p0 → p1 → p2 en el sentido del Quad: antihorario visto desde la normal).
        public static void Triangle(MeshData mesh, double x0, double y0, double z0, double x1, double y1, double z1, double x2, double y2, double z2,
                                    double nx, double ny, double nz, uint color, double material)
        {
            var c = SceneColor.FromArgb(color);
            var a = mesh.AddVertex(x0, y0, z0, nx, ny, nz, c.R, c.G, c.B, material, 0, 0);
            var b = mesh.AddVertex(x1, y1, z1, nx, ny, nz, c.R, c.G, c.B, material, 1, 0);
            var d = mesh.AddVertex(x2, y2, z2, nx, ny, nz, c.R, c.G, c.B, material, 0, 1);
            mesh.AddTriangle(a, d, b);
        }

        // Las bandas entre anillos consecutivos de (sides + 1) vértices, con la vuelta del Cylinder.
        private static void Bands(MeshData mesh, int first, int sides, int bands)
        {
            for (var s = 0; s < bands; s++)
            {
                for (var j = 0; j < sides; j++)
                {
                    var a = first + s * (sides + 1) + j;
                    var b = a + 1;
                    var d = a + sides + 1;
                    var e = d + 1;
                    mesh.AddTriangle(a, d, b);
                    mesh.AddTriangle(b, d, e);
                }
            }
        }
    }
}
