using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Water
{
    // Una columna de luz sobre el agua: el pie de la luz (x de mundo y relZ en cinta), ancho y largo (m, hacia la cámara), alfa (ya con
    // el nivel de su luz) y tono. La dibuja WaterInteractionRenderer como WaterMarks tipo 4 (DT6b-23).
    public struct LightColumn
    {
        public double X;
        public double RelZ;
        public double Width;
        public double Length;
        public double Alpha;
        public uint Tone;
    }

    // Las columnas de luz de la noche (#31; AC-6B-18; 00-arte-agua §7.1-§7.3): cada farol, ventana encendida a menos de 60 m (las 16
    // más cercanas), cabaña, torre y el faro cuando su haz apunta a ± 20° de la cámara dejan una columna sobre el agua debajo de su
    // x. El alfa sigue al nivel de su luz (PropLight, con su fundido de 0,4 s: se enciende sin saltos); de día no hay ninguna. Como
    // mucho 32 (las más cercanas); en Lite (Bajo), las 8 más cercanas. Sin asignar después de construir.
    public sealed class LightColumns
    {
        public const int Capacity = 32;
        private const int CandidateCapacity = 96;
        private const double MinLevel = 1e-3;

        private readonly LightColumn[] _items = new LightColumn[Capacity];
        private readonly LightColumn[] _candidates = new LightColumn[CandidateCapacity];
        private readonly LightColumn[] _windows = new LightColumn[CandidateCapacity];
        private readonly DartRandom _random = new DartRandom(0);
        private int _candidateCount;
        private int _windowCount;

        public int Count { get; private set; }

        public ref readonly LightColumn this[int i]
        {
            get
            {
                if ((uint)i >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i));
                }

                return ref _items[i];
            }
        }

        public void Clear()
        {
            Count = 0;
        }

        // Con los objetos visibles del cuadro y su nivel de luz (frame.VisibleProps y frame.PropLight, en el mismo orden).
        public void Fill(in SceneFrame frame, QualityTier tier)
        {
            Count = 0;
            _candidateCount = 0;
            _windowCount = 0;
            var props = frame.VisibleProps;
            var light = frame.PropLight;
            if (props == null || light == null)
            {
                return;
            }

            var d = frame.Distance;
            for (var i = 0; i < props.Count && i < light.Length; i++)
            {
                var level = light[i];
                if (level < MinLevel)
                {
                    continue;
                }

                var p = props[i];
                var relZ = p.Z - d;
                if (relZ <= SceneCamera.MinZ)
                {
                    continue;
                }

                switch (p.Kind)
                {
                    case PropKind.Lamp:
                        Candidate(p.X, relZ, WaterLookArt.LampColumnWidth, WaterLookArt.LampColumnLength, WaterLookArt.LampColumnAlpha * level,
                            WaterLookArt.LampColumnTone);
                        break;
                    case PropKind.Cabin:
                        Candidate(p.X, relZ, WaterLookArt.CabinColumnWidth, WaterLookArt.CabinColumnLength, WaterLookArt.CabinColumnAlpha * level,
                            WaterLookArt.LampColumnTone);
                        break;
                    case PropKind.FinishTower:
                        Candidate(p.X, relZ, WaterLookArt.TowerColumnWidth, WaterLookArt.TowerColumnLength, WaterLookArt.TowerColumnAlpha * level,
                            WaterLookArt.LampColumnTone);
                        break;
                    case PropKind.Boathouse:
                        Window(p.X, relZ, level);
                        break;
                    case PropKind.Building:
                        BuildingWindows(p, relZ, level);
                        break;
                    case PropKind.Lighthouse:
                    {
                        var facing = BeaconFacing(frame.SceneTime, p.X, relZ);
                        if (facing > 0)
                        {
                            Candidate(p.X, relZ, WaterLookArt.BeaconColumnWidth, WaterLookArt.BeaconColumnLength,
                                WaterLookArt.BeaconColumnAlpha * level * facing, WaterLookArt.BeaconColumnTone);
                        }

                        break;
                    }
                }
            }

            // Las ventanas: solo a menos de 60 m y las 16 más cercanas.
            SortByDistance(_windows, _windowCount);
            for (var i = 0; i < _windowCount && i < WaterLookArt.WindowColumnMax; i++)
            {
                if (_candidateCount < CandidateCapacity)
                {
                    _candidates[_candidateCount++] = _windows[i];
                }
            }

            SortByDistance(_candidates, _candidateCount);
            var max = tier == QualityTier.Low ? WaterLookArt.LiteColumnMax : Capacity;
            for (var i = 0; i < _candidateCount && Count < max; i++)
            {
                _items[Count++] = _candidates[i];
            }
        }

        // El peso del faro (0-1): cualquiera de los dos medios haces (2π·frac(t/12) y + π, como el shader Glow) a ± 20° de la dirección
        // del faro a la cámara, con un borde suave de 10° (sin saltos).
        public static double BeaconFacing(double sceneTime, double x, double relZ)
        {
            var angle = 2 * Math.PI * Text.DartMath.Mod(sceneTime / CoastArt.BeamPeriodSeconds, 1.0);
            var len = Math.Sqrt(x * x + relZ * relZ);
            if (len < 1e-9)
            {
                return 1;
            }

            var hx = -x / len;
            var hz = -relZ / len;
            var facing = Math.Abs(hx * Math.Cos(angle) + hz * Math.Sin(angle));
            var edge = WaterLookArt.BeaconFacingDeg;
            return SceneMath.SmoothStep(Math.Cos(SceneMath.DegToRad(edge)), Math.Cos(SceneMath.DegToRad(edge / 2)), facing);
        }

        // Las ventanas de la cara del río de un edificio (5 por piso, FacadeWindows): una columna por cada columna de ventanas con alguna
        // encendida, repartidas a lo largo del edificio (aprox.: centradas en su z), en la cara que mira al agua.
        private void BuildingWindows(in ShoreProp p, double relZ, double level)
        {
            if (relZ >= WaterLookArt.WindowColumnRange)
            {
                return;
            }

            FacadeWindows.Masks(p.Seed, _random, out _, out var river);
            var floors = FacadeWindows.Floors(p.Seed);
            var per = RiverArt.RiverWindowsPerFloor;
            var depth = RiverArt.BuildingDepth * p.Scale;
            var face = p.X - Math.Sign(p.X) * RiverArt.BuildingWidth / 2 * p.Scale;
            for (var k = 0; k < per; k++)
            {
                var lit = false;
                for (var f = 0; f < floors && !lit; f++)
                {
                    lit = (river & (1UL << (f * per + k))) != 0;
                }

                if (lit)
                {
                    Window(face, relZ + ((k + 0.5) / per - 0.5) * depth, level);
                }
            }
        }

        private void Window(double x, double relZ, double level)
        {
            if (relZ <= SceneCamera.MinZ || relZ >= WaterLookArt.WindowColumnRange || _windowCount >= CandidateCapacity)
            {
                return;
            }

            _windows[_windowCount++] = new LightColumn
            {
                X = x, RelZ = relZ, Width = WaterLookArt.WindowColumnWidth, Length = WaterLookArt.WindowColumnLength,
                Alpha = WaterLookArt.WindowColumnAlpha * level, Tone = WaterLookArt.LampColumnTone,
            };
        }

        private void Candidate(double x, double relZ, double width, double length, double alpha, uint tone)
        {
            if (_candidateCount >= CandidateCapacity)
            {
                return;
            }

            _candidates[_candidateCount++] = new LightColumn { X = x, RelZ = relZ, Width = width, Length = length, Alpha = alpha, Tone = tone };
        }

        // Inserción estable por relZ (pocas: sin asignar).
        private static void SortByDistance(LightColumn[] items, int count)
        {
            for (var i = 1; i < count; i++)
            {
                var item = items[i];
                var j = i - 1;
                while (j >= 0 && items[j].RelZ > item.RelZ)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = item;
            }
        }
    }
}
