using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Los 6 casos de valores de 04-design-notes §3.7 y §3.8 (hud_layout.py del Designer) y sus anchos naturales a tamaño base,
    // con las medidas del Designer (fontTools con kerning): cifras tabulares de RowMate Numeric SC (0,521 em en 700, 0,535 en
    // 800), ':' y '.' de 0,32 em, '-' de 0,370 / 0,376 em, rótulos y `obj.` medidos en Inter. Es lo que la vista mide con
    // FitBox.NaturalWidth en la app; acá lo usan HudGeometryTests, WrenchPlacementTests y SimulatorPanelLayoutTests.
    public static class HudGeometryCases
    {
        public static readonly HudCase Free = new HudCase("libre", false, false, false, "24.0", "02:13", "150", "89", null, "00:42", "1048");
        public static readonly HudCase FreeWithBpm = new HudCase("libre+BPM", false, false, false, "24.0", "02:13", "150", "89", "142", "00:42", "1048");
        public static readonly HudCase RoutineWithBpm = new HudCase("rutina SPM+W+BPM", true, false, true, "24.0", "02:13", "150", "89", "142", "00:50", "1200");
        public static readonly HudCase PiramideWattsSplit = new HudCase("Pirámide W+Split", false, true, true, "24.0", "02:13", "150", "89", null, "10:05", "3420");
        public static readonly HudCase Long = new HudCase("largos (CAP-16)", false, false, false, "40.5", "--:--", "-999", "9999", "188", "1:02:05", "123456");
        public static readonly HudCase Extremes = new HudCase("extremos FTMS", false, false, false, "127.5", "1092:15", "-32768", "65535", "255", "9:59:59", "16777215");

        public static IReadOnlyList<HudCase> All => new[] { Free, FreeWithBpm, RoutineWithBpm, PiramideWattsSplit, Long, Extremes };

        // Los 5 perfiles de la geometría (04-design-notes §3.8), con márgenes seguros en 0.
        public static IReadOnlyList<KeyValuePair<string, double[]>> Profiles => new[]
        {
            new KeyValuePair<string, double[]>("TV", new double[] { 411, 914 }),
            new KeyValuePair<string, double[]>("P360", new double[] { 360, 800 }),
            new KeyValuePair<string, double[]>("TH", new double[] { 914, 411 }),
            new KeyValuePair<string, double[]>("L640", new double[] { 640, 360 }),
            new KeyValuePair<string, double[]>("W1280", new double[] { 1280, 720 }),
        };

        public static double[] Profile(string name)
        {
            foreach (var p in Profiles)
            {
                if (p.Key == name)
                {
                    return p.Value;
                }
            }

            throw new ArgumentException("perfil desconocido: " + name, nameof(name));
        }

        // Rótulos (Inter 600 con tracking 0,08 em) y `obj.` (Inter 600 12), en dp base (hud_layout.py: LABEL y OBJ).
        public const double SpmLabel = 30.9, SplitLabel = 77.0, WattsLabel = 37.2, KcalLabel = 24.4, BpmLabel = 26.3 + 11 + 4;
        public const double SpmTarget = 39.8, WattsTarget = 45.2, SplitTarget = 59.0;

        // "W" y "m" (Inter 500 13) con su aire de 4 y 3.
        public const double UnitW = 13.0, UnitM = 11.5;

        public static HudLayoutInput Input(double width, double height, SafeInsets insets, bool simulator, HudCase c)
        {
            return new HudLayoutInput(width, height, insets, simulator, c.Bpm != null, c.SpmTarget, c.SplitTarget, c.WattsTarget, Widths(c));
        }

        public static HudNaturalWidths Widths(HudCase c)
        {
            return new HudNaturalWidths(
                NumberWidth(c.Spm, 64, 800),
                NumberWidth(c.Split, 44, 700),
                NumberWidth(c.Watts, 44, 700) + 4 + UnitW,
                NumberWidth(c.Kcal, 32, 700),
                c.Bpm == null ? 0 : NumberWidth(c.Bpm, 32, 700),
                NumberWidth(c.Time, 34, 700),
                NumberWidth(c.Distance, 34, 700) + 3 + UnitM,
                SpmLabel, SplitLabel, WattsLabel, KcalLabel, BpmLabel, SpmTarget, SplitTarget, WattsTarget);
        }

        // num_w de hud_layout.py: el ancho de un texto de cifras en RowMate Numeric SC.
        public static double NumberWidth(string text, double size, int weight)
        {
            var digit = weight == 800 ? 0.535 : 0.521;
            var hyphen = weight == 800 ? 0.376 : 0.370;
            var width = 0.0;
            foreach (var c in text)
            {
                if (c >= '0' && c <= '9')
                {
                    width += digit * size;
                }
                else if (c == ':' || c == '.')
                {
                    width += 0.32 * size;
                }
                else if (c == '-')
                {
                    width += hyphen * size;
                }
                else
                {
                    throw new ArgumentException("carácter no numérico: " + c, nameof(text));
                }
            }

            return width;
        }
    }

    // Un caso de valores del HUD: qué tarjetas tienen objetivo y los textos de cada valor.
    public sealed class HudCase
    {
        public HudCase(string name, bool spmTarget, bool splitTarget, bool wattsTarget, string spm, string split, string watts, string kcal,
                       string? bpm, string time, string distance)
        {
            Name = name;
            SpmTarget = spmTarget;
            SplitTarget = splitTarget;
            WattsTarget = wattsTarget;
            Spm = spm;
            Split = split;
            Watts = watts;
            Kcal = kcal;
            Bpm = bpm;
            Time = time;
            Distance = distance;
        }

        public string Name { get; }

        public bool SpmTarget { get; }

        public bool SplitTarget { get; }

        public bool WattsTarget { get; }

        public string Spm { get; }

        public string Split { get; }

        public string Watts { get; }

        public string Kcal { get; }

        public string? Bpm { get; }

        public string Time { get; }

        public string Distance { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
