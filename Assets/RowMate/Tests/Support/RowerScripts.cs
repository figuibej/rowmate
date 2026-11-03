using System;
using RowMate.Core.Models;

namespace RowMate.Tests.Support
{
    // Las lecturas del remo de prueba (spec §0.4; 02-tech-design §4.5 de la It. 4a). L(k) llega en el instante k − 0,5 s
    // (ScriptedRower): L(0) medio segundo antes de iniciar. El monitor ya traía 1000 m, 300 s y 40 kcal (Q-15).
    public static class RowerScripts
    {
        // R-4: tiempo del monitor 300 + k, paladas 100 + k, distancia 1000 + 4k, split 125, 150 W, 24.0 spm,
        // kcal 40 + ⌊k / 10⌋, pulso 0.
        public static RowingData R4(int k)
        {
            return new RowingData
            {
                ElapsedSeconds = 300 + k,
                StrokeCount = 100 + k,
                DistanceMeters = 1000 + 4 * k,
                Pace500mSeconds = 125,
                PowerWatts = 150,
                StrokeRate = 24.0,
                TotalCalories = 40 + k / 10,
                HeartRate = 0,
            };
        }

        // R-4(s): R-4 con SPM s (por ejemplo R-4(19)).
        public static Func<int, RowingData> R4Spm(double spm)
        {
            return k => R4(k).CopyWith(strokeRate: spm);
        }

        // It. 4b (spec §0.4): R-4(s; w), R-4 con SPM s y vatios w (por ejemplo R-4(23; 170)).
        public static Func<int, RowingData> R4With(double spm, int watts)
        {
            return k => R4(k).CopyWith(strokeRate: spm, powerWatts: watts);
        }

        // It. 4b: R-4X, los extremos FTMS de AC-WKT-17-10 de la 4a (= Extremes).
        public static RowingData R4X(int k)
        {
            return Extremes(k);
        }

        // R-4R, reinicio del monitor: desde L(10) la distancia es 4 (k − 10).
        public static RowingData R4R(int k)
        {
            return k >= 10 ? R4(k).CopyWith(distanceMeters: 4 * (k - 10)) : R4(k);
        }

        // R-4Z, un paquete sin distancia: L(10) llega con distancia 0 y las demás como R-4.
        public static RowingData R4Z(int k)
        {
            return k == 10 ? R4(k).CopyWith(distanceMeters: 0) : R4(k);
        }

        // It. 5 (spec §0.4): R-3, R-4 con la distancia 1000 + 3k (3 m/s) y split 167.
        public static RowingData R3(int k)
        {
            return R4(k).CopyWith(distanceMeters: 1000 + 3 * k, pace500mSeconds: 167);
        }

        // It. 5: R-4A (acelera), R-4 hasta L(250); desde L(251), distancia 2000 + 5 (k − 250) (5 m/s) y split 100.
        public static RowingData R4A(int k)
        {
            return k <= 250 ? R4(k) : R4(k).CopyWith(distanceMeters: 2000 + 5 * (k - 250), pace500mSeconds: 100);
        }

        // It. 5: R-4D (corte), R-4 sin L(101), L(102) ni L(103): esas lecturas no llegan (null; ScriptedRower no manda nada) y
        // el motor sigue con L(100).
        public static RowingData? R4D(int k)
        {
            return k >= 101 && k <= 103 ? null : R4(k);
        }

        // It. 5: el remo de prueba por nombre (los guiones del oráculo de series, Tests/Golden/oracle/series_tracker.json).
        // R-4S es R-4 con el índice de la lectura del tiempo real (ScriptedRower con LifecycleHarness.RealReadingIndex).
        public static Func<int, RowingData?> ByName(string name)
        {
            switch (name)
            {
                case "R-4":
                case "R-4S":
                    return k => R4(k);
                case "R-3":
                    return k => R3(k);
                case "R-4A":
                    return k => R4A(k);
                case "R-4R":
                    return k => R4R(k);
                case "R-4Z":
                    return k => R4Z(k);
                case "R-4D":
                    return R4D;
                case "R-4P":
                    return k => R4P(k);
                case "R-4X":
                    return k => R4X(k);
                default:
                    throw new ArgumentException("remo de prueba desconocido: " + name, nameof(name));
            }
        }

        // R-4P, parado: SPM 0, 0 W, split 0; la distancia, el tiempo y las kcal quietos (los de L(0)).
        public static RowingData R4P(int k)
        {
            return new RowingData
            {
                ElapsedSeconds = 300,
                StrokeCount = 100,
                DistanceMeters = 1000,
                Pace500mSeconds = 0,
                PowerWatts = 0,
                StrokeRate = 0,
                TotalCalories = 40,
                HeartRate = 0,
            };
        }

        // CAP-16 (§7.2): los valores largos; el reloj del HUD llega a 1:02:05 con 3725 s de entrenamiento.
        public static RowingData Long(int k)
        {
            return new RowingData
            {
                ElapsedSeconds = 3725,
                StrokeCount = 4000,
                DistanceMeters = 123456,
                Pace500mSeconds = 0,
                PowerWatts = -999,
                StrokeRate = 40.5,
                TotalCalories = 9999,
                HeartRate = 188,
            };
        }

        // AC-WKT-17-10: los extremos que puede mandar un monitor FTMS (Q-38): SPM 127.5, −32768 W, split 1092:15, distancia
        // 16 777 215, kcal 65535, pulso 255 y el tiempo del monitor en 9:59:59.
        public static RowingData Extremes(int k)
        {
            return new RowingData
            {
                ElapsedSeconds = 35999,
                StrokeCount = 65535,
                DistanceMeters = 16777215,
                Pace500mSeconds = 65535,
                PowerWatts = -32768,
                StrokeRate = 127.5,
                TotalCalories = 65535,
                HeartRate = 255,
            };
        }
    }
}
