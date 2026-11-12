using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Water
{
    // La estela con historia (DV-151; AC-6B-14; 00-arte-agua §6.1): la popa de mundo cada 0,25 s, 48 puntos (12 s). Cada punto guarda
    // su z de mundo (de la D en double), su intensidad al nacer I₀ = clamp(v/4; 0; 1) con la velocidad del casco (con el surge de la
    // palada, así la estela "late" con cada drive) y su edad. Con el bote quieto I₀ = 0: no hay estela; al frenar, los puntos nuevos
    // nacen más débiles y los viejos envejecen y se apagan (alfa 0,35·I₀·e^(−a/4), < 0,02 a los 12 s). Sin asignar después de construir.
    public sealed class WakeTrail
    {
        public const int Capacity = 48;
        public const double Interval = 0.25;
        public const double LifeSeconds = Capacity * Interval;
        public const int PackedStride = 4;

        private readonly double[] _worldZ = new double[Capacity];
        private readonly double[] _intensity = new double[Capacity];
        private readonly double[] _birth = new double[Capacity];
        private int _head = -1;      // el más nuevo
        private int _stored;
        private double _lastEmit = double.NaN;
        private double _lastNow = double.NaN;
        private double _lastDistance;
        private double _lastIntensity;
        private double _lastSternOffset;

        // Los puntos vivos (edad < 12 s).
        public int Count
        {
            get
            {
                var n = 0;
                for (var k = 0; k < _stored; k++)
                {
                    if (AgeOf(k) < LifeSeconds)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        // La intensidad de la popa en el último paso (la del tramo que todavía no es un punto).
        public double HeadIntensity => _lastIntensity;

        public void Clear()
        {
            _head = -1;
            _stored = 0;
            _lastEmit = double.NaN;
            _lastNow = double.NaN;
            _lastDistance = 0;
            _lastIntensity = 0;
            _lastSternOffset = 0;
        }

        // Un cuadro: now (s, el reloj de la escena; corre también en pausa), la distancia de escena, la velocidad del casco (m/s) y el
        // desfase de dibujo del bote (el surge, m): la popa que se dibuja está en distancia + 5,3 + surge.
        public void Step(double now, double distance, double hullSpeedMps, double sternOffsetM = 0)
        {
            if (!double.IsNaN(_lastNow) && now < _lastNow)
            {
                Clear();
            }

            _lastNow = now;
            _lastDistance = distance;
            _lastIntensity = WaterModel.WakeIntensity(hullSpeedMps);
            _lastSternOffset = double.IsNaN(sternOffsetM) ? 0 : sternOffsetM;
            if (double.IsNaN(_lastEmit) || now - _lastEmit >= Interval - 1e-9)
            {
                Push(distance + WaterModel.SternRelZ + sternOffsetM, _lastIntensity, now);
                // Sin arrastrar error: el próximo, un intervalo después (o ahora, si el cuadro fue largo).
                _lastEmit = double.IsNaN(_lastEmit) || now - _lastEmit >= 2 * Interval ? now : _lastEmit + Interval;
            }
        }

        // Las tomas con la distancia forzada (G6b-15): 48 puntos como si el bote viniera a esa velocidad desde hace 12 s.
        public void Prime(double now, double distance, double steadySpeedMps)
        {
            Clear();
            var intensity = WaterModel.WakeIntensity(steadySpeedMps);
            var speed = double.IsNaN(steadySpeedMps) ? 0 : Math.Max(0, steadySpeedMps);
            for (var k = Capacity - 1; k >= 0; k--)
            {
                var age = k * Interval;
                Push(distance - speed * age + WaterModel.SternRelZ, intensity, now - age);
            }

            _lastNow = now;
            _lastEmit = now;
            _lastDistance = distance;
            _lastIntensity = intensity;
            _lastSternOffset = 0;
        }

        // El punto k (0 = el más nuevo) en cinta, con la distancia y el reloj del último paso.
        public void Point(int k, out double relZ, out double intensity, out double age, out double back)
        {
            if ((uint)k >= (uint)_stored)
            {
                throw new ArgumentOutOfRangeException(nameof(k));
            }

            var i = Index(k);
            relZ = _worldZ[i] - _lastDistance;
            intensity = _intensity[i];
            age = _lastNow - _birth[i];
            back = WaterModel.SternRelZ - relZ;
        }

        // Por punto (del más nuevo al más viejo): relZ, I₀, edad y recorrido (back); los que faltan o vencieron van con I₀ 0 y la
        // edad de la vida entera. Lo lee WakeRenderer y lo sube a _RmWake (WP-B), sin asignar.
        // QA-V6b-02: el lugar 0 es la CABEZA VIVA, la popa que se dibuja en este cuadro (distancia + 5,3 + surge, la intensidad de ahora y
        // edad 0); los lugares 1 a 47 son los puntos guardados 0 a 46 (el 47, de 11,75 s, ya vale < 0,02). Sin esto la cinta arrancaba en
        // el último punto emitido, que queda hasta 0,25 s (≈ 1 m a 4 m/s) detrás de la popa: un hueco con un borde recto.
        public void CopyTo(float[] packed)
        {
            if (packed == null || packed.Length < Capacity * PackedStride)
            {
                throw new ArgumentException("WakeTrail: hacen falta " + Capacity * PackedStride + " lugares.", nameof(packed));
            }

            var live = !double.IsNaN(_lastNow);
            var headRelZ = WaterModel.SternRelZ + _lastSternOffset;
            packed[0] = (float)headRelZ;
            packed[1] = live ? (float)_lastIntensity : 0f;
            packed[2] = live ? 0f : (float)LifeSeconds;
            packed[3] = (float)(WaterModel.SternRelZ - headRelZ);
            for (var k = 1; k < Capacity; k++)
            {
                var o = k * PackedStride;
                var j = k - 1;
                if (j < _stored)
                {
                    Point(j, out var relZ, out var intensity, out var age, out var back);
                    var alive = age < LifeSeconds;
                    packed[o] = (float)relZ;
                    packed[o + 1] = alive ? (float)intensity : 0f;
                    packed[o + 2] = (float)Math.Min(age, LifeSeconds);
                    packed[o + 3] = (float)back;
                }
                else
                {
                    packed[o] = (float)WaterModel.SternRelZ;
                    packed[o + 1] = 0f;
                    packed[o + 2] = (float)LifeSeconds;
                    packed[o + 3] = 0f;
                }
            }
        }

        // ── La forma y el alfa de un punto por su edad (00-arte-agua §6.1) ──

        public static double Alpha(double intensityAtBirth, double age)
        {
            return WaterLookArt.WakeAlphaPeak * intensityAtBirth * Math.Exp(-Math.Max(0, age) / WaterLookArt.WakeAlphaTau);
        }

        // QA-V6b-02: la cabeza que nace en la popa (0 en la popa, a pleno a WakeHeadLength detrás).
        public static double HeadRamp(double backM)
        {
            return SceneMath.SmoothStep(0, WaterLookArt.WakeHeadLength, double.IsNaN(backM) ? 0 : backM);
        }

        // El alfa que sube a _RmWake: el de la edad × la cabeza (el apagado hacia la cámara lo pone el vértice, que conoce la cámara).
        public static double ShapedAlpha(double intensityAtBirth, double age, double backM)
        {
            return Alpha(intensityAtBirth, age) * HeadRamp(backM);
        }

        public static double ArmWidth(double age)
        {
            return WaterLookArt.WakeArmWidthBase + WaterLookArt.WakeArmWidthPerSecond * SceneMath.Clamp(age, 0, LifeSeconds);
        }

        public static double CenterWidth(double age)
        {
            return SceneMath.Lerp(WaterLookArt.WakeCenterWidthBase, WaterLookArt.WakeCenterWidthEnd, SceneMath.Clamp01(age / LifeSeconds));
        }

        private void Push(double worldZ, double intensity, double birth)
        {
            _head = (_head + 1) % Capacity;
            _worldZ[_head] = worldZ;
            _intensity[_head] = intensity;
            _birth[_head] = birth;
            if (_stored < Capacity)
            {
                _stored++;
            }
        }

        private int Index(int k)
        {
            return ((_head - k) % Capacity + Capacity) % Capacity;
        }

        private double AgeOf(int k)
        {
            return _lastNow - _birth[Index(k)];
        }
    }
}
