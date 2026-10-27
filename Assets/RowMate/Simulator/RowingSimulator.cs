using System;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Simulator
{
    // Física simplificada de un remo para el modo simulador (F-BLE-14), port 1 a 1 del legacy.
    // Cada Tick avanza 1 segundo y devuelve la lectura que emitiría el monitor. Fórmulas de Concept2 para el split y
    // las calorías; pulso que se acerca un 10 % por tick a su objetivo. El azar sale de un IRandom inyectable
    // (AC-BASE-07): sin ruido no se consume. Solo compila con ROWMATE_SIMULATOR (asmdef RowMate.Simulator).
    // legacy: lib/core/dev/rowing_simulator.dart:1-83
    public sealed class RowingSimulator
    {
        // Topes de los objetivos (:7-10).
        public const int MinWatts = 30;
        public const int MaxWatts = 500;
        public const int MinSpm = 14;
        public const int MaxSpm = 40;

        // Pulso de reposo y tope del objetivo de pulso (:37, :56).
        internal const double RestingHeartRate = 70.0;
        internal const double MaxHeartRateTarget = 190.0;

        private readonly IRandom _random;
        private int _targetWatts = 150; // :20
        private int _targetSpm = 24; // :24

        private double _distance;
        private double _strokes;
        private double _calories;
        private double _heartRate = RestingHeartRate; // :37
        private int _elapsed;

        // :12-13. Sin random usa el azar real (SystemRandom); se crea aunque noise sea false, como el legacy.
        public RowingSimulator(IRandom? random = null, bool noise = true)
        {
            _random = random ?? new SystemRandom();
            Noise = noise;
        }

        // false = los valores emitidos son exactamente los objetivos (para tests) (:17-18).
        public bool Noise { get; }

        // 150 W; se recorta a [30; 500] (:20-22).
        public int TargetWatts
        {
            get => _targetWatts;
            set => _targetWatts = Clamp(value, MinWatts, MaxWatts);
        }

        // 24 spm; se recorta a [14; 40] (:24-26).
        public int TargetSpm
        {
            get => _targetSpm;
            set => _targetSpm = Clamp(value, MinSpm, MaxSpm);
        }

        // false = el remero dejó de remar: spm, vatios y split en 0 y totales congelados (:28-29).
        public bool Rowing { get; set; } = true;

        // Último pulso calculado por Tick, redondeado como Dart (la mitad se aleja del cero). Lo emite el pulsómetro
        // simulado de la It. 2 (:31-32).
        public int HeartRate => (int)DartMath.Round(_heartRate);

        // Pulso interno sin redondear (para los tests de AC-BLE-14-07 y AC-BLE-14-10).
        internal double ExactHeartRate => _heartRate;

        // 1 tick = 1 s (:40-70).
        public RowingData Tick()
        {
            var watts = 0;
            var spm = 0.0;
            var pace = 0;

            if (Rowing)
            {
                // Orden de llamadas al azar idéntico al legacy: primero vatios, después spm (:46-47).
                watts = Math.Max(1, (int)DartMath.Round(TargetWatts * (1 + Jitter(0.05))));
                spm = Math.Max(1.0, TargetSpm + Jitter(1.0));
                var paceExact = PaceForWatts(watts);
                pace = (int)DartMath.Round(paceExact);
                _distance += 500 / paceExact;
                _strokes += spm / 60;
                _calories += CaloriesPerSecond(watts);
                _elapsed++;
            }

            // El pulso avanza el 10 % de la distancia a su objetivo (:56-57).
            var targetHr = HeartRateTarget(Rowing, watts);
            _heartRate += (targetHr - _heartRate) * 0.1;

            return new RowingData
            {
                // = double.parse(spm.toStringAsFixed(1)) (:60): 24.25 → 24.3, 23.95 → 23.9.
                StrokeRate = DartMath.RoundToFixed(spm, 1),
                // + 1e-6 evita que errores de punto flotante (23.9999…) resten una unidad (:61-66).
                StrokeCount = (int)Math.Floor(_strokes + 1e-6),
                DistanceMeters = (int)Math.Floor(_distance + 1e-6),
                Pace500mSeconds = pace,
                PowerWatts = watts,
                TotalCalories = (int)Math.Floor(_calories + 1e-6),
                HeartRate = (int)DartMath.Round(_heartRate),
                ElapsedSeconds = _elapsed,
            };
        }

        // Segundos por 500 m para una potencia dada (fórmula de Concept2) (:77-78).
        // Math.Pow puede diferir en el último bit del pow de la VM de Dart (R-T4): los tests usan Within(1e-9).
        public static double PaceForWatts(int watts)
        {
            return 500 * Math.Pow(2.80 / watts, 1.0 / 3.0);
        }

        // kcal por segundo para una potencia dada (fórmula de Concept2) (:81-82). (watts * 4) es entero, como en Dart.
        public static double CaloriesPerSecond(int watts)
        {
            return (watts * 4 * 0.8604 + 300) / 3600;
        }

        // Objetivo de pulso: mín(190; 90 + 0,35 · W) remando (W = la potencia de ese tick, con ruido) y 70 sin remar (:56).
        internal static double HeartRateTarget(bool rowing, int watts)
        {
            return rowing ? Math.Min(MaxHeartRateTarget, 90 + watts * 0.35) : RestingHeartRate;
        }

        // Valor uniforme en [-amplitud; amplitud], o 0 sin ruido: sin ruido NO consume el IRandom (:73-74).
        private double Jitter(double amplitude)
        {
            return Noise ? (_random.NextDouble() * 2 - 1) * amplitude : 0;
        }

        // = int.clamp(min, max).
        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
