using System;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Un rectángulo en dp de ventana con la convención de 04-design-notes §3.8 de la It. 4a: x desde la izquierda, y desde
    // ARRIBA. Lo usan HudGeometry (las piezas del HUD, la zona del bote y el lugar de la llave) y OverlaySurface (el lugar
    // que declara el inmersivo para la llave, DN4a-14). Se llama DpRect y no Rect para no chocar con UnityEngine.Rect en las
    // vistas que importan los dos namespaces (Presentation usa los dos).
    public readonly struct DpRect : IEquatable<DpRect>
    {
        public DpRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }

        public double Left => X;

        public double Top => Y;

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public double CenterX => X + Width / 2;

        public double CenterY => Y + Height / 2;

        public static DpRect FromEdges(double left, double top, double right, double bottom)
        {
            return new DpRect(left, top, right - left, bottom - top);
        }

        // Cruce estricto (rozarse en un borde no cuenta), con el otro agrandado `gap` dp de cada lado: la regla de "tapar" de
        // 00-deuda-diseno §1.2 (la llave agrandada 8) y la de hud_layout.py.
        public bool Intersects(DpRect other, double gap = 0)
        {
            return Left - gap < other.Right && other.Left < Right + gap && Top - gap < other.Bottom && other.Top < Bottom + gap;
        }

        // Solo en alto (las reglas T2 y T3 de 04-design-notes §3.7: "si se cruza en alto con la zona del bote").
        public bool OverlapsVertically(DpRect other)
        {
            return Top < other.Bottom && other.Top < Bottom;
        }

        public bool IsInside(double windowWidth, double windowHeight, double tolerance = 1e-6)
        {
            return Left >= -tolerance && Top >= -tolerance && Right <= windowWidth + tolerance && Bottom <= windowHeight + tolerance;
        }

        public bool Equals(DpRect other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object? obj)
        {
            return obj is DpRect other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public static bool operator ==(DpRect left, DpRect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DpRect left, DpRect right)
        {
            return !left.Equals(right);
        }

        // Como las tablas de 04-design-notes §3.8: "x 355.0-395.0 · y 575.9-615.9".
        public override string ToString()
        {
            return "x " + Fmt.Fixed(Left, 1) + "-" + Fmt.Fixed(Right, 1) + " · y " + Fmt.Fixed(Top, 1) + "-" + Fmt.Fixed(Bottom, 1);
        }
    }
}
