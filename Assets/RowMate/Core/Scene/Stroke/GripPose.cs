using System.Collections.Generic;

namespace RowMate.Core.Scene.Stroke
{
    // Un dedo del agarre: el sufijo del hueso de Mixamo (sin "mixamorig:Left" / "Right") y el giro relativo a la pose de reposo medida
    // (grados): flexión alrededor del eje del doblez y apertura (spread) solo en la primera falange.
    public readonly struct FingerCurl
    {
        public FingerCurl(string boneSuffix, double curlDeg, double spreadDeg)
        {
            BoneSuffix = boneSuffix;
            CurlDeg = curlDeg;
            SpreadDeg = spreadDeg;
        }

        public string BoneSuffix { get; }
        public double CurlDeg { get; }
        public double SpreadDeg { get; }
    }

    // El agarre del puño del remo para un personaje con dedos (Y Bot; DT7-07, DA7-12): una tabla de ángulos por falange que
    // RowerRigDriver aplica una vez al armar, como rotación local relativa al reposo. Sale de los músculos del arte §4.6 (índice a
    // meñique −0,85 / −0,90 / −0,80; pulgar −0,20 / −0,60 / −0,60 con apertura 0,40) a razón de ≈ 90° por unidad de músculo. El
    // maniquí no tiene dedos (puño rígido).
    public static class GripPose
    {
        public static IReadOnlyList<FingerCurl> Fingers { get; } = new[]
        {
            new FingerCurl("HandIndex1", 76, 0), new FingerCurl("HandIndex2", 81, 0), new FingerCurl("HandIndex3", 72, 0),
            new FingerCurl("HandMiddle1", 76, 0), new FingerCurl("HandMiddle2", 81, 0), new FingerCurl("HandMiddle3", 72, 0),
            new FingerCurl("HandRing1", 76, 0), new FingerCurl("HandRing2", 81, 0), new FingerCurl("HandRing3", 72, 0),
            new FingerCurl("HandPinky1", 76, 0), new FingerCurl("HandPinky2", 81, 0), new FingerCurl("HandPinky3", 72, 0),
            new FingerCurl("HandThumb1", 18, 36), new FingerCurl("HandThumb2", 54, 0), new FingerCurl("HandThumb3", 54, 0),
        };

        // "mixamorig:LeftHandIndex1"…
        public static string BoneName(bool left, in FingerCurl finger)
        {
            return "mixamorig:" + (left ? "Left" : "Right") + finger.BoneSuffix;
        }
    }
}
