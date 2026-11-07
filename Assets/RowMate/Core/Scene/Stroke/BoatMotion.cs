using System;

namespace RowMate.Core.Scene.Stroke
{
    // Lo que se mueve el bote en un cuadro (rad y m): cabeceo (+ proa arriba), arfada, rolido y surge (+ hacia proa).
    public struct BoatMotionSample
    {
        public BoatMotionSample(double pitchRad, double heaveM, double rollRad, double surgeM)
        {
            PitchRad = pitchRad;
            HeaveM = heaveM;
            RollRad = rollRad;
            SurgeM = surgeM;
        }

        public double PitchRad;
        public double HeaveM;
        public double RollRad;
        public double SurgeM;
    }

    // El cabeceo, la arfada y el rolido del bote (AC-SCN-06-01, -02; ANIMATION_BIBLE §4.6). El cabeceo es el del contrato
    // (scene_state.dart:85) evaluado con la fase visual φ (en régimen φ = p); remero, remos, carro y casco cabecean juntos (DV-69).
    public static class BoatMotion
    {
        // El rolido del lago (k; río 0,8, costa 2,0 y regata 0,4 son de la It. 8).
        public const double LakeRollK = 0.5;

        // 0,02·sin(2πφ) + 0,006·sin(0,8·t) rad.
        public static double Pitch(double phi, double sceneTime)
        {
            return 0.02 * Math.Sin(phi * 2 * Math.PI) + 0.006 * Math.Sin(sceneTime * 0.8);
        }

        // 0,006·sin(2π(φ − 0,08)) m.
        public static double Heave(double phi)
        {
            return 0.006 * Math.Sin(2 * Math.PI * (phi - 0.08));
        }

        // 0,004·sin(0,9t + 1,3)·k rad.
        public static double Roll(double sceneTime, double k)
        {
            return 0.004 * Math.Sin(0.9 * sceneTime + 1.3) * k;
        }
    }
}
