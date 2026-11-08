using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Las siluetas lejanas del río, la costa y la regata (02-tech-design §2.5; AC-SCN-19-03 a -05; DT8-09). Los px del legacy
    // son los de TV (411 × 914 dp, focal 411): un px de ancho es 1/411 rad de acimut. La forma es fija (no se corre de costado,
    // DV-81); el alto se escala con HorizonModel.PeakTan y el acercamiento con HorizonModel.ApproachScale.
    public static class LegacyTv
    {
        public const double Width = 411, Height = 914, Focal = 411;
        public const double HorizonY = Height * 0.40;   // SceneCamera: vertical, 0,40 del alto

        // Un ancho de px del legacy en TV → rad de acimut.
        public static double PxToRad(double px)
        {
            return px / Focal;
        }
    }

    // El skyline del río (river_environment.dart:46-71): columnas de 34 px (4,74° en TV) con DartRandom(i·31 + 7) y, en el orden
    // del legacy, el alto (0,3 + 0,7·r de hMax), el ancho (0,6 + 0,4·r de la columna) y las ventanas de noche (r < 0,5, celda por
    // celda: filas de arriba a abajo cada 7 px, columnas cada 8 px, con la geometría de TV). Bit (fila·cols + col) de la máscara.
    public static class Skyline
    {
        public const double ColumnPx = 34;
        public const double CellWidthPx = 8, CellHeightPx = 7, WindowWidthPx = 3, WindowHeightPx = 4;
        public const int MaxCells = 64;

        public static double ColumnRad => LegacyTv.PxToRad(ColumnPx);

        public static void Column(int i, DartRandom rnd, out double heightFraction, out double widthFraction, out int rows, out int cols,
                                  out ulong windowMask)
        {
            if (rnd == null)
            {
                throw new ArgumentNullException(nameof(rnd));
            }

            rnd.Reseed(i * 31 + 7);
            heightFraction = 0.3 + rnd.NextDouble() * 0.7;
            widthFraction = 0.6 + rnd.NextDouble() * 0.4;
            var hMax = LegacyTv.Height * RiverArt.HorizonFraction;
            var h = hMax * heightFraction;
            var bwidth = ColumnPx * widthFraction;
            rows = 0;
            cols = 0;
            windowMask = 0;
            var bit = 0;
            for (var wy = LegacyTv.HorizonY - h + 6; wy < LegacyTv.HorizonY - 4; wy += 7)
            {
                rows++;
                var c = 0;
                for (var wx = 4.0; wx < bwidth - 4; wx += 8)
                {
                    c++;
                    if (rnd.NextDouble() < RiverArt.SkylineWindowThreshold && bit < MaxCells)
                    {
                        windowMask |= 1UL << bit;
                    }

                    bit++;
                }

                cols = c;
            }
        }
    }

    // El acantilado lejano de la costa (coast_environment.dart:51-66): 20 pasos sobre el 35 % izquierdo del ancho (1,0° por paso
    // en TV), alto hMax·fade·(0,6 + 0,4·sin(1,7·i + 0,5)) con fade = 1 − i/20. Sin la fase distance·0,002 del legacy (DT8-09:
    // "sin cambiar de forma").
    public static class CoastFarCliff
    {
        public static double StepRad => LegacyTv.PxToRad(LegacyTv.Width * CoastArt.FarCliffWidthFraction / CoastArt.FarCliffSteps);

        // Fracción de hMax en el paso i (0 a 20).
        public static double Height(int step)
        {
            var fade = 1 - step / (double)CoastArt.FarCliffSteps;
            return fade * (0.6 + 0.4 * Math.Sin(step * 1.7 + 0.5));
        }

        // El acimut (rad, 0 = centro de TV) del paso i: el borde izquierdo de TV más i pasos.
        public static double AzimuthRad(int step)
        {
            return LegacyTv.PxToRad(-LegacyTv.Width / 2) + step * StepRad;
        }
    }

    // Un rectángulo del fondo de la regata en ángulos: acimut izquierdo y ancho (rad, 0 = centro de TV) y la banda de alto en
    // fracciones de hMax (desde el horizonte).
    public readonly struct BackdropRect
    {
        public BackdropRect(double leftRad, double widthRad, double bottomFraction, double topFraction)
        {
            LeftRad = leftRad;
            WidthRad = widthRad;
            BottomFraction = bottomFraction;
            TopFraction = topFraction;
        }

        public double LeftRad { get; }
        public double WidthRad { get; }
        public double BottomFraction { get; }
        public double TopFraction { get; }
    }

    // El fondo de la regata (regatta_environment.dart:43-71) a distancia 0, sin el corrimiento lateral del legacy (DV-81): la tira
    // de tierra de 0 a 0,25·hMax, y cada medio ancho de TV (205,5 px = 0,5 rad = 28,6°) una tribuna (desde 0,08 del ancho, 0,16 de
    // ancho, de 0,2 a 0,7·hMax), una torre (desde 0,36, 0,025 de ancho, hasta hMax) y 4 banderas (desde 0,42 + 0,015·i, mástil de
    // 0,25 a 0,6·hMax, paño de 6 px).
    public static class RegattaBackdrop
    {
        public const double PeriodFraction = 0.5, StandFrom = 0.08, StandWidth = 0.16, TowerFrom = 0.36, TowerWidth = 0.025;
        public const double FlagFrom = 0.42, FlagStep = 0.015, FlagClothPx = 6;
        public const double LandTop = 0.25, StandBottom = 0.2, StandTop = 0.7, FlagMastBottom = 0.25, FlagMastTop = 0.6;
        public const double FlagTipP = 0.55, FlagClothBottomP = 0.5;

        public static double PeriodRad => LegacyTv.PxToRad(LegacyTv.Width * PeriodFraction);

        // El bloque n (n = 0 empieza en el borde izquierdo de TV): tribuna y torre.
        public static BackdropRect Stand(int n)
        {
            return new BackdropRect(Left(n, StandFrom), LegacyTv.PxToRad(LegacyTv.Width * StandWidth), StandBottom, StandTop);
        }

        public static BackdropRect Tower(int n)
        {
            return new BackdropRect(Left(n, TowerFrom), LegacyTv.PxToRad(LegacyTv.Width * TowerWidth), 0, 1);
        }

        // El mástil de la bandera i del bloque n (acimut del mástil; el paño sale hacia +x 6 px).
        public static double FlagAzimuthRad(int n, int i)
        {
            return Left(n, FlagFrom + i * FlagStep);
        }

        public static double FlagClothRad => LegacyTv.PxToRad(FlagClothPx);

        // La onda de la punta: 2 px de TV en fracción de hMax, a 3 rad/s (los copia el shader Horizon: kRmFarFlagWave, kRmFarFlagRate).
        public const double FlagWavePx = 2, FlagWaveRate = 3;

        public static double FlagWaveP => FlagWavePx / (LegacyTv.Height * RegattaArt.HorizonFraction);

        // sin(3t + i)·2 px del legacy (:63): la punta del paño, en fracción de hMax de TV.
        public static double FlagWave(int i, double sceneTime)
        {
            return Math.Sin(sceneTime * FlagWaveRate + i) * FlagWaveP;
        }

        private static double Left(int n, double fraction)
        {
            return LegacyTv.PxToRad(-LegacyTv.Width / 2 + n * LegacyTv.Width * PeriodFraction + LegacyTv.Width * fraction);
        }
    }

    // Los colores de las siluetas por hora (environment.dart:127-129): HorizonModel.Color con el tono y la profundidad de cada capa.
    public static class EnvironmentHorizonColors
    {
        public static SceneColor Skyline(in ScenePalette p) => HorizonModel.Color(p, SceneColor.FromArgb(RiverArt.SkylineTone), RiverArt.SkylineDepth);

        public static SceneColor CoastCliff(in ScenePalette p) => HorizonModel.Color(p, SceneColor.FromArgb(CoastArt.FarCliffTone), CoastArt.FarCliffDepth);

        public static SceneColor RegattaLand(in ScenePalette p) => HorizonModel.Color(p, SceneColor.FromArgb(RegattaArt.Land), RegattaArt.LandDepth);

        public static SceneColor RegattaStand(in ScenePalette p) => HorizonModel.Color(p, SceneColor.FromArgb(RegattaArt.BackdropStand), RegattaArt.BackdropDepth);

        public static SceneColor RegattaTower(in ScenePalette p) => HorizonModel.Color(p, SceneColor.FromArgb(RegattaArt.BackdropTower), RegattaArt.BackdropDepth);

        public static SceneColor RegattaFlag(in ScenePalette p, int i) => HorizonModel.Color(p, SceneColor.FromArgb(RegattaArt.FlagTones[i & 3]), RegattaArt.FlagDepth);

        // AC-SCN-19-03: las ventanas del skyline con ambiente < 0,6 (las enciende NightLights por bloque).
        public static bool SkylineWindowsOn(double ambient) => NightLights.IsNightLegacy(ambient);
    }
}
