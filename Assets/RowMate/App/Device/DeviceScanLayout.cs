using System;
using RowMate.Core.Design;

namespace RowMate.App.Device
{
    // Cómo se reparte la vista de búsqueda (It. 11, A11-05, DT11-14; cierra m-06 de la 10a): Stacked = como hasta la 10a (alto normal);
    // TwoColumns = alto compacto y ancho de contenido ≥ 840 (lista a la izquierda, tarjeta del pulsómetro a la derecha); SingleScroll =
    // alto compacto y angosto (lista a su alto natural y la tarjeta debajo, en un único ScrollView).
    public enum DeviceScanLayoutKind
    {
        Stacked,
        SingleScroll,
        TwoColumns
    }

    // La disposición y el ancho de la columna derecha (0 salvo en TwoColumns).
    public readonly struct DeviceScanLayout : IEquatable<DeviceScanLayout>
    {
        public const double TwoColumnsMinWidthDp = 840;
        public const double RightColumnFraction = 0.4;
        public const double RightColumnMinDp = 360;
        public const double RightColumnMaxDp = 440;

        public DeviceScanLayout(DeviceScanLayoutKind kind, double rightColumnDp)
        {
            Kind = kind;
            RightColumnDp = rightColumnDp;
        }

        public DeviceScanLayoutKind Kind { get; }

        public double RightColumnDp { get; }

        // Alto ≥ BleMotionSpecs.CompactHeightDp (560) → Stacked; compacto y ancho ≥ 840 → TwoColumns con clamp(0,4 W; 360; 440);
        // si no → SingleScroll. TV, P360 y W1280 quedan Stacked; TH, TwoColumns; L640, SingleScroll.
        public static DeviceScanLayout For(double contentWidthDp, double contentHeightDp)
        {
            if (contentHeightDp >= BleMotionSpecs.CompactHeightDp)
            {
                return new DeviceScanLayout(DeviceScanLayoutKind.Stacked, 0);
            }

            if (contentWidthDp >= TwoColumnsMinWidthDp)
            {
                var right = Math.Min(RightColumnMaxDp, Math.Max(RightColumnMinDp, RightColumnFraction * contentWidthDp));
                return new DeviceScanLayout(DeviceScanLayoutKind.TwoColumns, right);
            }

            return new DeviceScanLayout(DeviceScanLayoutKind.SingleScroll, 0);
        }

        public bool Equals(DeviceScanLayout other)
        {
            return Kind == other.Kind && RightColumnDp.Equals(other.RightColumnDp);
        }

        public override bool Equals(object? obj)
        {
            return obj is DeviceScanLayout other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Kind * 397) ^ RightColumnDp.GetHashCode();
        }

        public override string ToString()
        {
            return Kind == DeviceScanLayoutKind.TwoColumns ? Kind + "(" + RightColumnDp + ")" : Kind.ToString();
        }
    }
}
