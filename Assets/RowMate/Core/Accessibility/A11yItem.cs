using System;

namespace RowMate.Core.Accessibility
{
    // Un elemento que el lector alcanza (It. 14, DT14-17): su clave estable (en la app, el VisualElement; en las pruebas, lo que
    // sea), rol, nombre, valor, ayuda, estado y su caja en dp (para el orden visual; el marco en px lo calcula el puente).
    public sealed class A11yItem
    {
        public A11yItem(object key, A11yRole role, string label, double x, double y, double width, double height)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Role = role;
            Label = label ?? "";
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public object Key { get; }

        public A11yRole Role { get; }

        public string Label { get; set; }

        public string Value { get; set; } = "";

        public string Hint { get; set; } = "";

        public A11yState State { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }
    }
}
