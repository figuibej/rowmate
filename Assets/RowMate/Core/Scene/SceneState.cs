using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // Charco que deja la pala al salir del agua (scene/scene_state.dart:5-11): queda fijo en el mundo y envejece.
    public struct Puddle
    {
        public Puddle(double x, double worldZ, double age)
        {
            X = x;
            WorldZ = worldZ;
            Age = age;
        }

        public double X;
        public double WorldZ;
        public double Age;
    }

    // La lista de charcos del contrato (scene_state.dart:30) sin asignar: 16 lugares en orden de llegada; el borrado conserva el orden
    // (como removeWhere). Con más de 16 vivos (imposible por debajo de 120 spm) se pisa el más viejo. No se dibuja (PuddleField).
    public sealed class PuddleRing
    {
        public const int Capacity = 16;

        private readonly Puddle[] _items = new Puddle[Capacity];

        public int Count { get; private set; }

        public ref readonly Puddle this[int i]
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

        public void Add(in Puddle puddle)
        {
            if (Count == Capacity)
            {
                Array.Copy(_items, 1, _items, 0, Capacity - 1);
                Count--;
            }

            _items[Count++] = puddle;
        }

        public void Clear()
        {
            Count = 0;
        }

        // :79-81: todos envejecen el paso; se borran los de edad > vida, conservando el orden.
        internal void AgeAndPrune(double step, double life)
        {
            var w = 0;
            for (var i = 0; i < Count; i++)
            {
                _items[i].Age += step;
                if (!(_items[i].Age > life))
                {
                    _items[w++] = _items[i];
                }
            }

            Count = w;
        }
    }

    // El estado dinámico de la escena (scene/scene_state.dart:13-86; F-SCN-03, F-SCN-04, F-SCN-06, F-SCN-07): velocidad, distancia y
    // tiempo (6a) y, desde la It. 7, la fase de palada del contrato p, los charcos del contrato y el cabeceo, en el orden exacto del
    // legacy (LT-88 a LT-91). La distancia de escena es propia (no la del monitor), en double (Q-68), y no toca nada fuera de la
    // escena (AC-SCN-03-05). Lo que se ve usa la fase visual (VisualStrokeClock) y los charcos visuales (PuddleField).
    public sealed class SceneState
    {
        public const double SpeedTau = 1.5;       // scene_state.dart:16
        public const double MaxStep = 0.1;        // :52 (scene_view.dart:63)
        public const double PuddleLife = 4.0;     // :17
        public const double OarLength = 2.9;      // :18
        public const double RiggerX = 0.85;       // :19
        public const double IdleSpm = 12;         // :23

        private bool _bladeWasIn = true;          // :31: al empezar la pala cuenta como dentro

        public double Speed { get; private set; }

        public double Distance { get; private set; }

        public double Time { get; private set; }

        // :27: la fase del contrato p ∈ [0; 1).
        public double StrokePhase { get; private set; }

        // :28: rad, + = proa arriba (con p; lo visual usa φ, BoatMotion).
        public double Pitch { get; private set; }

        // :30: la lista del contrato.
        public PuddleRing Puddles { get; } = new PuddleRing();

        // :33-34: la distancia acotada para el shader del legacy. Contrato (LT-68); el agua de la app no la usa (QA-P-28).
        public double ShaderDistance => DartMath.Mod(Distance, 4096.0);

        // :38-43: el parser repone el ritmo medio cuando el instantáneo es 0, así que la señal de remar es el SPM.
        public static double TargetSpeedFor(double pace500m, double spm, bool isActive)
        {
            return isActive && spm > 0 && pace500m > 0 ? 500 / pace500m : 0;
        }

        // :45-86: paso acotado a [0; 0,1], inercia con τ = 1,5 s, corte a 0 bajo 0,01 m/s y la distancia con la velocidad nueva; la
        // fase (:59-65), los charcos al soltar la pala (:67-81) y el cabeceo (:85), en ese orden.
        public void Update(double dt, double pace500m, double spm, bool isActive)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > MaxStep ? MaxStep : dt);
            Time += step;
            var target = TargetSpeedFor(pace500m, spm, isActive);
            Speed += (target - Speed) * Math.Min(1.0, step / SpeedTau);
            if (target == 0 && Speed < 0.01)
            {
                Speed = 0;
            }

            Distance += Speed * step;

            if (isActive && spm > 0)
            {
                StrokePhase = DartMath.Mod(StrokePhase + spm / 60 * step, 1.0);
            }
            else if (isActive && StrokePhase > 0)
            {
                // Sin paladas: termina el ciclo despacio y queda en el catch.
                var next = StrokePhase + IdleSpm / 60 * step;
                StrokePhase = next >= 1.0 ? 0.0 : next;
            }

            var bladeIn = StrokeCycle.BladeInWater(StrokePhase);
            if (_bladeWasIn && !bladeIn)
            {
                // Soltó la pala: un charco a cada lado, a la altura de las palas (primero el izquierdo, x < 0).
                var sweep = StrokeCycle.OarSweep(StrokePhase);
                var bladeZ = SceneCamera.BoatZ - Math.Sin(sweep) * OarLength;
                Puddles.Add(new Puddle(-1.0 * (RiggerX + Math.Cos(sweep) * OarLength), Distance + bladeZ, 0));
                Puddles.Add(new Puddle(1.0 * (RiggerX + Math.Cos(sweep) * OarLength), Distance + bladeZ, 0));
            }

            _bladeWasIn = bladeIn;
            Puddles.AgeAndPrune(step, PuddleLife);
            Pitch = 0.02 * Math.Sin(StrokePhase * 2 * Math.PI) + 0.006 * Math.Sin(Time * 0.8);
        }

        // Vuelve a cero (al abrir una sesión de escena, AC-SCN-01-03).
        public void Reset()
        {
            Speed = 0;
            Distance = 0;
            Time = 0;
            StrokePhase = 0;
            Pitch = 0;
            Puddles.Clear();
            _bladeWasIn = true;
        }

        // E-0, E-920, E-1234, E-42K (§0.4 de la spec): solo las pruebas y el arnés (InternalsVisibleTo).
        internal void ForceForTest(double distance, double speed, double time)
        {
            Distance = distance;
            Speed = speed;
            Time = time;
        }

        // Los tests legacy fijan strokePhase (boat_painter_test.dart:35) y el arnés fija la fase del contrato.
        internal void ForceStrokeForTest(double phase)
        {
            StrokePhase = phase;
        }

        // boat_painter_test.dart:29-33: charcos armados a mano.
        internal void AddPuddleForTest(double x, double worldZ, double age)
        {
            Puddles.Add(new Puddle(x, worldZ, age));
        }
    }
}
