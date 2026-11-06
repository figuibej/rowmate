namespace RowMate.Core.Scene
{
    // La pose de la cámara en un cuadro (06a-escena-lago/02-tech-design §2.2): posición en metros (la del contrato:
    // (0; 2,2; 0)), giros en grados (guiñada + = mira a la derecha, cabeceo + = mira arriba, rolido como el z de Unity;
    // 0 = nivelada mirando a +z) y la proyección (focal y horizonte en dp: las del contrato fuera del vuelo).
    // En Unity: transform.position = (X; Y; Z) y transform.rotation = Quaternion.Euler(−PitchDeg; YawDeg; RollDeg).
    public readonly struct CameraPose
    {
        public CameraPose(double x, double y, double z, double yawDeg, double pitchDeg, double rollDeg, double focalDp, double horizonDp)
        {
            X = x;
            Y = y;
            Z = z;
            YawDeg = yawDeg;
            PitchDeg = pitchDeg;
            RollDeg = rollDeg;
            FocalDp = focalDp;
            HorizonDp = horizonDp;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public double YawDeg { get; }

        public double PitchDeg { get; }

        public double RollDeg { get; }

        public double FocalDp { get; }

        public double HorizonDp { get; }

        // La pose del contrato para una ventana (AC-SCN-02-01): exacta, sin redondeo.
        public static CameraPose Contract(in SceneCamera cam)
        {
            return new CameraPose(0, SceneCamera.CamHeight, 0, 0, 0, 0, cam.Focal, cam.HorizonY);
        }

        public CameraPose WithPosition(double x, double y, double z)
        {
            return new CameraPose(x, y, z, YawDeg, PitchDeg, RollDeg, FocalDp, HorizonDp);
        }

        public CameraPose WithRotation(double yawDeg, double pitchDeg, double rollDeg)
        {
            return new CameraPose(X, Y, Z, yawDeg, pitchDeg, rollDeg, FocalDp, HorizonDp);
        }

        public CameraPose WithProjection(double focalDp, double horizonDp)
        {
            return new CameraPose(X, Y, Z, YawDeg, PitchDeg, RollDeg, focalDp, horizonDp);
        }
    }
}
