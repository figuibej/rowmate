namespace RowMate.Core.Scene.Stroke
{
    // Las medidas reales del single y del remo de sculling, en el marco del bote (origen en el punto medio entre los pernos al nivel
    // del agua, +z proa, +y arriba; el remero mira a −z): D-ART-04, ART_BIBLE §7.8, 00-arte-remero §5 (AC-SCN-05-06). El casco es el
    // de la 6a (LakeArt). La pedalera y el recorrido del carro no son del bote: los da RiggingFit para el cuerpo del remero (DT7-02).
    public readonly struct BoatSpec
    {
        public BoatSpec(double pinX, double pinY, double oarLength, double inboard, double gripFromPin, double bladeFromPin, double bladeLength,
                        double bladeWidth, double seatTopY, double railLength, double stretcherAngleDeg, double heelBelowSeat)
        {
            PinX = pinX;
            PinY = pinY;
            OarLength = oarLength;
            Inboard = inboard;
            GripFromPin = gripFromPin;
            BladeFromPin = bladeFromPin;
            BladeLength = bladeLength;
            BladeWidth = bladeWidth;
            SeatTopY = seatTopY;
            RailLength = railLength;
            StretcherAngleDeg = stretcherAngleDeg;
            HeelBelowSeat = heelBelowSeat;
        }

        public static BoatSpec Real => new BoatSpec(0.80, 0.31, 2.88, 0.88, 0.82, 1.76, 0.47, 0.22, 0.14, 0.72, 42, 0.17);

        // Los pernos a ± 0,80 m y el eje del remo a 0,31 m sobre el agua.
        public double PinX { get; }
        public double PinY { get; }

        // 2,88 m con 0,88 de interior (del extremo del puño al collarín).
        public double OarLength { get; }
        public double Inboard { get; }

        // Del perno al centro del puño (0,82) y al centro de la pala (1,76).
        public double GripFromPin { get; }
        public double BladeFromPin { get; }

        // La pala "hacha" de 0,47 × 0,22 m.
        public double BladeLength { get; }
        public double BladeWidth { get; }

        // La cara de arriba del asiento sobre el agua.
        public double SeatTopY { get; }

        // Los rieles del carro.
        public double RailLength { get; }

        // La placa de la pedalera y el talón bajo el asiento (arte §5.2).
        public double StretcherAngleDeg { get; }
        public double HeelBelowSeat { get; }

        // El casco de la 6a (mundo): popa, proa y mangas.
        public double HullStern => Art.LakeArt.HullStern;
        public double HullBow => Art.LakeArt.HullBow;
        public double BeamWaterline => Art.LakeArt.HullBeamWaterline;
        public double BeamGunwale => Art.LakeArt.HullBeamGunwale;
    }
}
