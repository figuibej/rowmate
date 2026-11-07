namespace RowMate.Core.Scene.Stroke
{
    // Los largos de hueso del remero (m): los del maniquí procedural o los medidos del rig del slot (RowerRigDriver, WP-B1). La tabla
    // del maniquí es la del arte §2.1 para 1,85 m (DT7-03: WP-B0 mide Y Bot y, si difiere, se cambia acá en una línea, escalado a
    // 1,85 m si mide fuera de 1,80–1,88; QA7-G-11). El tronco se reparte en la pelvis (Hips → Spine) y los tres tramos de la columna
    // hasta la base del cuello; los hombros caen ShoulderDrop debajo de esa base.
    public readonly struct RowerSpec
    {
        public RowerSpec(double hipHalfWidth, double hipAboveSeat, double hipsAboveJoints, double pelvisLength, double spineLength,
                         double spine1Length, double spine2Length, double shoulderDrop, double clavicleHalfWidth, double shoulderHalfWidth,
                         double neckLength, double headTopAbove, double upperArm, double forearm, double handToGrip, double thigh, double shin,
                         double ankleHeight, double footLength, double footHalfSpread)
        {
            HipHalfWidth = hipHalfWidth;
            HipAboveSeat = hipAboveSeat;
            HipsAboveJoints = hipsAboveJoints;
            PelvisLength = pelvisLength;
            SpineLength = spineLength;
            Spine1Length = spine1Length;
            Spine2Length = spine2Length;
            ShoulderDrop = shoulderDrop;
            ClavicleHalfWidth = clavicleHalfWidth;
            ShoulderHalfWidth = shoulderHalfWidth;
            NeckLength = neckLength;
            HeadTopAbove = headTopAbove;
            UpperArm = upperArm;
            Forearm = forearm;
            HandToGrip = handToGrip;
            Thigh = thigh;
            Shin = shin;
            AnkleHeight = ankleHeight;
            FootLength = footLength;
            FootHalfSpread = footHalfSpread;
        }

        // El maniquí de 1,85 m (arte §2.1): articulaciones de la cadera a ± 0,09, tronco 0,53 de la cadera a los hombros, hombros a
        // ± 0,185, brazo 0,344, antebrazo 0,270, centro del agarre a 0,08 de la muñeca, muslo 0,453, pierna 0,455, pie 0,28.
        public static RowerSpec Mannequin => new RowerSpec(
            hipHalfWidth: 0.09, hipAboveSeat: 0.09, hipsAboveJoints: 0.06, pelvisLength: 0.11, spineLength: 0.13, spine1Length: 0.15,
            spine2Length: 0.16, shoulderDrop: 0.02, clavicleHalfWidth: 0.04, shoulderHalfWidth: 0.185, neckLength: 0.10,
            headTopAbove: 0.212, upperArm: 0.344, forearm: 0.270, handToGrip: 0.08, thigh: 0.453, shin: 0.455, ankleHeight: 0.08,
            footLength: 0.28, footHalfSpread: 0.11);

        public double HipHalfWidth { get; }

        // La articulación de la cadera sobre la cara del asiento (parámetro libre del ajuste, 02-tech-design §9).
        public double HipAboveSeat { get; }

        // El hueso Hips sobre el punto medio de las articulaciones de la cadera.
        public double HipsAboveJoints { get; }

        public double PelvisLength { get; }
        public double SpineLength { get; }
        public double Spine1Length { get; }
        public double Spine2Length { get; }
        public double ShoulderDrop { get; }
        public double ClavicleHalfWidth { get; }
        public double ShoulderHalfWidth { get; }
        public double NeckLength { get; }
        public double HeadTopAbove { get; }
        public double UpperArm { get; }
        public double Forearm { get; }
        public double HandToGrip { get; }
        public double Thigh { get; }
        public double Shin { get; }
        public double AnkleHeight { get; }
        public double FootLength { get; }
        public double FootHalfSpread { get; }

        // De las articulaciones de la cadera a la base del cuello.
        public double TrunkLength => PelvisLength + SpineLength + Spine1Length + Spine2Length;

        // El brazo entero hasta el centro del agarre.
        public double ArmReach => UpperArm + Forearm + HandToGrip;

        // De pie: tobillo + pierna + muslo + tronco + cuello + cabeza (1,85 en el maniquí).
        public double Height => AnkleHeight + Shin + Thigh + TrunkLength + NeckLength + HeadTopAbove;

        // El mismo cuerpo × k (un rig de 1,75 m = Mannequin.Scaled(1,75 / 1,85)).
        public RowerSpec Scaled(double k)
        {
            return new RowerSpec(HipHalfWidth * k, HipAboveSeat * k, HipsAboveJoints * k, PelvisLength * k, SpineLength * k, Spine1Length * k,
                Spine2Length * k, ShoulderDrop * k, ClavicleHalfWidth * k, ShoulderHalfWidth * k, NeckLength * k, HeadTopAbove * k, UpperArm * k,
                Forearm * k, HandToGrip * k, Thigh * k, Shin * k, AnkleHeight * k, FootLength * k, FootHalfSpread * k);
        }
    }
}
