using System;

namespace RowMate.Core.Scene.Stroke
{
    // Un charco visible: x y z de mundo (fijos), edad (s), giro de los remolinos (rad), alfa y lado (−1 el de x < 0, +1 el de x > 0).
    public struct VisualPuddle
    {
        public double X;
        public double WorldZ;
        public double Age;
        public double Spin;
        public double Alpha;
        public int Side;
    }

    // Los charcos que se ven (D-ANI-02; AC-SCN-07-02, -03; ANIMATION_BIBLE §4.3): las reglas del legacy (scene_state.dart:67-83,
    // boat_painter.dart:100-116) sobre la pala visible. Dos por palada en la salida de la pala (BladeExit), primero el de x < 0, donde
    // está cada pala (su centro); fijos en el mundo; viven 4 s (también en pausa); radio 0,3 → 0,9; alfa (1 − edad/4)^1,5·(0,30 +
    // 0,35·e) con una entrada de 0,15 s; los remolinos giran 1,5 rad/s decayendo con la vida. Los de relZ ≤ 0,5 no se dibujan. Doce
    // lugares (hasta 90 spm); con más se pisa el más viejo. Sin asignar.
    public sealed class PuddleField
    {
        public const int Capacity = 12;
        public const double Life = SceneState.PuddleLife;
        public const double FadeInSeconds = 0.15;
        public const double SpinRadPerSecond = 1.5;

        private readonly VisualPuddle[] _items = new VisualPuddle[Capacity];
        private double _effort;

        public int Count { get; private set; }

        public ref readonly VisualPuddle this[int i]
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

        // La salida de la pala: un charco por pala en su centro (boatWorldZ = distancia de escena + 9,5 + surge del cuadro).
        public void OnBladeExit(in ScullingPose pose, double boatWorldZ)
        {
            Add(pose.Right.BladeCenter.X, boatWorldZ + pose.Right.BladeCenter.Z, pose.Right.BladeCenter.X < 0 ? -1 : 1);
            Add(pose.Left.BladeCenter.X, boatWorldZ + pose.Left.BladeCenter.Z, pose.Left.BladeCenter.X < 0 ? -1 : 1);
        }

        // Envejece (también en pausa) y borra los de más de 4 s, conservando el orden.
        public void Step(double dt, double effort)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            _effort = effort;
            var w = 0;
            for (var i = 0; i < Count; i++)
            {
                var p = _items[i];
                p.Age += step;
                if (p.Age > Life)
                {
                    continue;
                }

                p.Spin = SpinOf(p.Age);
                p.Alpha = AlphaOf(p.Age, effort);
                _items[w++] = p;
            }

            Count = w;
        }

        // 0,3 → 0,9 m (boat_painter.dart:106-107).
        public static double Radius(double age)
        {
            return 0.3 + 0.6 * (age / Life);
        }

        // (1 − edad/4)^1,5·(0,30 + 0,35·e), con la entrada de 0,15 s.
        public static double AlphaOf(double age, double effort)
        {
            var t = SceneMath.Clamp01(age / Life);
            var fadeIn = SceneMath.Clamp01(age / FadeInSeconds);
            return Math.Pow(1 - t, 1.5) * (0.30 + 0.35 * effort) * fadeIn;
        }

        // El giro acumulado: ∫ 1,5·(1 − t/4) dt.
        public static double SpinOf(double age)
        {
            return SpinRadPerSecond * (age - age * age / (2 * Life));
        }

        // Detrás de la cámara no se dibuja (relZ ≤ 0,5, SceneCamera.MinZ).
        public static bool IsBehindCamera(double relZ)
        {
            return relZ <= SceneCamera.MinZ;
        }

        // LT-97/98: los 3 charcos del test legacy (boat_painter_test.dart:29-33).
        internal void InjectForTest(double x, double worldZ, double age)
        {
            Add(x, worldZ, x < 0 ? -1 : 1);
            _items[Count - 1].Age = age;
            _items[Count - 1].Spin = SpinOf(age);
            _items[Count - 1].Alpha = AlphaOf(age, _effort);
        }

        private void Add(double x, double worldZ, int side)
        {
            if (Count == Capacity)
            {
                Array.Copy(_items, 1, _items, 0, Capacity - 1);
                Count--;
            }

            _items[Count++] = new VisualPuddle { X = x, WorldZ = worldZ, Age = 0, Spin = 0, Alpha = 0, Side = side };
        }
    }
}
