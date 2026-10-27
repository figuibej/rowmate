namespace RowMate.App.Device
{
    // Geometría de la grilla de métricas (AC-BLE-11-05): columnas, proporción ancho / alto de cada tarjeta y separación en
    // los dos sentidos. legacy: lib/features/device/device_screen.dart:261-276 (GridView.count)
    public sealed record DeviceGridSpec(int Columns, double AspectRatio, double Spacing)
    {
        // Ancho de cada tarjeta para el ancho de la grilla, en dp.
        public double CellWidth(double gridWidthDp)
        {
            return (gridWidthDp - Spacing * (Columns - 1)) / Columns;
        }

        // Alto de cada tarjeta (= ancho / proporción, childAspectRatio de Flutter).
        public double CellHeight(double gridWidthDp)
        {
            return CellWidth(gridWidthDp) / AspectRatio;
        }

        // Filas para n tarjetas.
        public int Rows(int tileCount)
        {
            return tileCount <= 0 ? 0 : (tileCount + Columns - 1) / Columns;
        }

        // Alto total de la grilla para n tarjetas (filas × alto + separaciones).
        public double Height(double gridWidthDp, int tileCount)
        {
            var rows = Rows(tileCount);
            return rows == 0 ? 0 : rows * CellHeight(gridWidthDp) + (rows - 1) * Spacing;
        }
    }
}
