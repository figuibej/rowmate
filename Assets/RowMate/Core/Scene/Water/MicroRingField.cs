using System;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Core.Scene.Water
{
    // Un micro-anillo: x y z de MUNDO (fijos), edad (s) y tipo (0 = gota que cae al agua; 1 = el ataque de la pala).
    public struct MicroRing
    {
        public double X;
        public double WorldZ;
        public double Age;
        public int Kind;
    }

    // Los micro-anillos (#13; AC-6B-16; 00-arte-agua §6.2): cada gota de la salpicadura o de la recuperación que toca el agua deja un
    // anillo (radio 0,05 + 0,35·a, ancho 0,02 + 0,03·a, 1,2 s, alfa 0,5·(1 − a/1,2)²) y cada ataque deja dos por pala (0,15 → 0,9 m en
    // 1,0 s). Se llama ANTES de StrokeSpray.Step (que borra las gotas que tocan el agua): predice cuáles caen en este paso con la
    // misma balística. Como mucho 24 vivos (se pisa el más viejo). Sin asignar. En la degradada y en Bajo no se dibujan (lo decide
    // WP-B); la regla corre igual (es barata).
    public sealed class MicroRingField
    {
        public const int Capacity = 24;
        public const int CatchRingsPerBlade = 2;
        public const double CatchSecondRingDelay = 0.15;   // el segundo anillo del ataque nace corrido (dos ondas)

        private readonly MicroRing[] _items = new MicroRing[Capacity];

        public int Count { get; private set; }

        public ref readonly MicroRing this[int i]
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

        // Un paso (dt acotado como el resto de la palada): envejece, borra los vencidos y suma los nuevos: las gotas (tipo 0 y 2 de
        // StrokeSpray) que cruzan el agua bajando en este paso y, con BladeEntry, dos por pala en el lugar de la corona que acaba de nacer.
        public void Step(double dt, StrokeSpray spray, StrokeEvents events)
        {
            if (spray == null)
            {
                throw new ArgumentNullException(nameof(spray));
            }

            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            var w = 0;
            for (var i = 0; i < Count; i++)
            {
                var r = _items[i];
                r.Age += step;
                if (r.Age >= LifeOf(r.Kind))
                {
                    continue;
                }

                _items[w++] = r;
            }

            Count = w;

            var items = spray.Items;
            for (var i = 0; i < spray.Count; i++)
            {
                var p = items[i];
                if (p.Kind == 1)
                {
                    // La corona del ataque (nace con edad 0 en BladeEntry): dos anillos en el centro de la pala.
                    if ((events & StrokeEvents.BladeEntry) != 0 && p.Age == 0)
                    {
                        Add(p.X, p.Z, 0, 1);
                        Add(p.X, p.Z, -CatchSecondRingDelay, 1);
                    }

                    continue;
                }

                if (step <= 0 || p.Age + step >= p.Life)
                {
                    continue;
                }

                var vy = p.Vy - StrokeSpray.Gravity * step;
                var y = p.Y + vy * step;
                if (y < 0 && vy < 0)
                {
                    Add(p.X + p.Vx * step, p.Z + p.Vz * step, 0, 0);
                }
            }
        }

        // Las tomas con la palada congelada (CAP-32, QA-V6b-03): un anillo a esa edad (tipo 0 gota, 1 ataque).
        internal void InjectForTest(double x, double worldZ, double age, int kind)
        {
            Add(x, worldZ, age, kind);
        }

        public static double LifeOf(int kind)
        {
            return kind == 1 ? WaterLookArt.CatchRingLife : WaterLookArt.DropRingLife;
        }

        // El radio (m) a esa edad: gota 0,05 + 0,35·a; ataque 0,15 → 0,9 en 1,0 s. Con edad negativa (el segundo del ataque), 0.
        public static double Radius(int kind, double age)
        {
            if (age < 0)
            {
                return 0;
            }

            if (kind == 1)
            {
                return SceneMath.Lerp(WaterLookArt.CatchRingRadius0, WaterLookArt.CatchRingRadius1, SceneMath.Clamp01(age / WaterLookArt.CatchRingLife));
            }

            return WaterLookArt.DropRingRadius0 + WaterLookArt.DropRingGrowth * age;
        }

        public static double Width(double age)
        {
            return WaterLookArt.DropRingWidth0 + WaterLookArt.DropRingWidthGrowth * Math.Max(0, age);
        }

        // 0,5·(1 − a/vida)².
        public static double Alpha(int kind, double age)
        {
            if (age < 0)
            {
                return 0;
            }

            var t = 1 - SceneMath.Clamp01(age / LifeOf(kind));
            return WaterLookArt.DropRingAlpha * t * t;
        }

        private void Add(double x, double worldZ, double age, int kind)
        {
            if (Count == Capacity)
            {
                Array.Copy(_items, 1, _items, 0, Capacity - 1);
                Count--;
            }

            _items[Count++] = new MicroRing { X = x, WorldZ = worldZ, Age = age, Kind = kind };
        }
    }
}
