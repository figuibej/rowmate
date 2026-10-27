using RowMate.Core.Design;
using RowMate.Core.Models;

namespace RowMate.App.Device
{
    // Una tarjeta del tablero de Dispositivo, con los textos ya resueltos (02-tech-design §2.5).
    // LabelKey/Label: clave del catálogo y su texto; ValueText: el valor como lo muestra el legacy (AC-BLE-01-05);
    // NumericValue: el sentido del rodillo (segundos del split, spm, W, m, bpm); Color: el del valor (MetricPalette.For;
    // ícono y etiqueta usan MetricPalette.LabelFor); IconCodepoint/IconFilled: el glifo; ElementName: nombre de contrato
    // del elemento (metric-tile-split|spm|watts|distance|hr).
    public sealed record MetricTileSpec(MetricKind Kind, string LabelKey, string Label, string ValueText, double NumericValue,
                                        ColorToken Color, string IconCodepoint, bool IconFilled, string ElementName);
}
