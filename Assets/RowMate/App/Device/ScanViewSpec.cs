namespace RowMate.App.Device
{
    // Estado de la vista de búsqueda (AC-BLE-05-01, -02, AC-HRM-13-08): "Buscar dispositivos" habilitado, buscando (spinner,
    // "Buscando..."), la ayuda de la búsqueda y el estado vacío.
    // legacy: lib/features/device/device_screen.dart:344-348, :396-442
    public sealed record ScanViewSpec(bool SearchEnabled, bool Searching, bool ShowSearchHint, bool ShowEmptyState);
}
