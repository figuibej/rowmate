using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene.Stroke
{
    // Una partícula: posición (x de mundo, y sobre el agua, z de MUNDO, con la distancia), velocidad (m/s), tamaño (m), edad y vida (s)
    // y tipo: 0 gota del ataque, 1 corona (Vy = su alto, sin moverse), 2 gota de la recuperación (trazo de 0,06 m).
    public struct SprayParticle
    {
        public double X, Y, Z, Vx, Vy, Vz, Size, Age, Life;
        public int Kind;
    }

    // La salpicadura del ataque y las gotas de la recuperación (AC-SCN-08-04, -05; ANIMATION_BIBLE §4.1, §4.2; DT7-05): un pool
    // propio de 128 partículas balísticas, determinista (semilla DartRandom(palada·2 + lado)) y sin asignar, que Unity dibuja en un
    // solo pedido instanciado. Nunca nace nada en pausa (solo la llaman los eventos y la recuperación con la pala afuera).
    public sealed class StrokeSpray
    {
        public const int Capacity = 128;
        public const double Gravity = 9.8;
        public const int MinDropsPerBlade = 6;
        public const double DripStrokeLength = 0.06;

        private readonly SprayParticle[] _items = new SprayParticle[Capacity];
        private readonly DartRandom _random = new DartRandom(0);
        private readonly double[] _dripCarry = new double[2];
        private int _strokeIndex;

        public int Count { get; private set; }

        // Compacto: [0; Count).
        public SprayParticle[] Items => _items;

        // Las gotas del ataque de una pala: 8 + 14·e (al menos 6), por la escala de partículas del nivel (1 en Alto y Medio, 0,5 en
        // Bajo, sin bajar de 6).
        public static int DropsPerBlade(double effort, double particleScale)
        {
            return Math.Max(MinDropsPerBlade, (int)DartMath.Round((8 + 14 * effort) * particleScale));
        }

        // El alto máximo de la salpicadura: 0,12 + 0,18·e (la corona).
        public static double CrownHeight(double effort)
        {
            return 0.12 + 0.18 * effort;
        }

        public void Clear()
        {
            Count = 0;
            _strokeIndex = 0;
            _dripCarry[0] = 0;
            _dripCarry[1] = 0;
        }

        // BladeEntry: por pala, las gotas (arriba 1,4 + 1,0·e m/s, a proa 0,6, afuera 0,3, cono de 35°, 0,02–0,05 m, vida 0,35–0,6 s)
        // y la corona (0,25 × (0,12 + 0,18·e) m, 0,18 s).
        public void OnBladeEntry(in ScullingPose pose, double boatWorldZ, double effort, double particleScale)
        {
            Burst(pose.Right.BladeCenter, -1, boatWorldZ, effort, particleScale);
            Burst(pose.Left.BladeCenter, 1, boatWorldZ, effort, particleScale);
            _strokeIndex++;
            _dripCarry[0] = 0;
            _dripCarry[1] = 0;
        }

        // De BladeExit a c = 0,55: 50·(1 − u) gotas por segundo y por pala (u = avance del tramo), de 0,008–0,015 m, desde el borde
        // de abajo y la punta de la pala.
        public void EmitDrips(in ScullingPose pose, double boatWorldZ, double u, double dt, double particleScale)
        {
            var rate = 50 * (1 - SceneMath.Clamp01(u)) * particleScale;
            for (var k = 0; k < 2; k++)
            {
                var side = k == 0 ? -1 : 1;
                var oar = side < 0 ? pose.Right : pose.Left;
                _dripCarry[k] += rate * dt;
                while (_dripCarry[k] >= 1)
                {
                    _dripCarry[k] -= 1;
                    _random.Reseed(_strokeIndex * 2 + (side < 0 ? 0 : 1) + 1000 + Count);
                    var along = (_random.NextDouble() - 0.5) * 0.4;
                    Spawn(new SprayParticle
                    {
                        X = oar.BladeCenter.X + side * along,
                        Y = Math.Max(0.01, oar.BladeCenter.Y - 0.05),
                        Z = boatWorldZ + oar.BladeCenter.Z + (_random.NextDouble() - 0.5) * 0.06,
                        Vx = 0,
                        Vy = -0.2,
                        Vz = 0,
                        Size = 0.008 + 0.007 * _random.NextDouble(),
                        Age = 0,
                        Life = 1.0,
                        Kind = 2,
                    });
                }
            }
        }

        // Gravedad 9,8; se borran al tocar el agua o al vencer (conservando el orden).
        public void Step(double dt)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            var w = 0;
            for (var i = 0; i < Count; i++)
            {
                var p = _items[i];
                p.Age += step;
                if (p.Kind != 1)
                {
                    p.Vy -= Gravity * step;
                    p.X += p.Vx * step;
                    p.Y += p.Vy * step;
                    p.Z += p.Vz * step;
                }

                if (p.Age >= p.Life || p.Kind != 1 && p.Y < 0 && p.Vy < 0)
                {
                    continue;
                }

                _items[w++] = p;
            }

            Count = w;
        }

        // El alfa de una partícula: 0,8 → 0 en su vida.
        public static double AlphaOf(in SprayParticle p)
        {
            return 0.8 * (1 - SceneMath.Clamp01(p.Age / p.Life));
        }

        // Cuántas hay de un tipo (las sondas del arnés).
        public int CountOf(int kind)
        {
            var n = 0;
            for (var i = 0; i < Count; i++)
            {
                if (_items[i].Kind == kind)
                {
                    n++;
                }
            }

            return n;
        }

        private void Burst(in SceneVec3 blade, int side, double boatWorldZ, double effort, double particleScale)
        {
            _random.Reseed(_strokeIndex * 2 + (side < 0 ? 0 : 1));
            var up = 1.4 + 1.0 * effort;
            var cone = Math.Tan(35 * Math.PI / 180) * up;
            var drops = DropsPerBlade(effort, particleScale);
            for (var i = 0; i < drops; i++)
            {
                var jx = (_random.NextDouble() * 2 - 1) * cone * 0.5;
                var jz = (_random.NextDouble() * 2 - 1) * cone * 0.5;
                var speed = 0.85 + 0.15 * _random.NextDouble();
                Spawn(new SprayParticle
                {
                    X = blade.X + side * (_random.NextDouble() - 0.5) * 0.3,
                    Y = 0.01,
                    Z = boatWorldZ + blade.Z + (_random.NextDouble() - 0.5) * 0.1,
                    Vx = (side * 0.3 + jx) * speed,
                    Vy = up * speed,
                    Vz = (0.6 + jz) * speed,
                    Size = 0.02 + 0.03 * _random.NextDouble(),
                    Age = 0,
                    Life = 0.35 + 0.25 * _random.NextDouble(),
                    Kind = 0,
                });
            }

            Spawn(new SprayParticle
            {
                X = blade.X,
                Y = 0,
                Z = boatWorldZ + blade.Z,
                Vx = 0,
                Vy = CrownHeight(effort),
                Vz = 0,
                Size = 0.25,
                Age = 0,
                Life = 0.18,
                Kind = 1,
            });
        }

        private void Spawn(in SprayParticle p)
        {
            if (Count == Capacity)
            {
                Array.Copy(_items, 1, _items, 0, Capacity - 1);
                Count--;
            }

            _items[Count++] = p;
        }
    }
}
