using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Cómo apoya la cabaña (DT6a-08): el ancla del legacy (13; z₀ + 10) es el centro del frente, que mira a la cámara, y nunca
    // se mueve; la profundidad crece hacia +z.
    public readonly struct CabinLayout
    {
        public CabinLayout(double anchorX, double anchorZ, double depth, double floorY, double plinthBottomY, bool collided)
        {
            AnchorX = anchorX;
            AnchorZ = anchorZ;
            Depth = depth;
            FloorY = floorY;
            PlinthBottomY = plinthBottomY;
            Collided = collided;
        }

        public double AnchorX { get; }
        public double AnchorZ { get; }

        // 4, 3 o 2,5 m: la primera que no choca con un tronco (huella + 0,3 m).
        public double Depth { get; }

        // El piso: 0,15 m sobre la esquina más alta de la huella; el zócalo de piedra baja hasta la más baja.
        public double FloorY { get; }
        public double PlinthBottomY { get; }

        // Ni con 2,5 m se libró de los troncos: los pinos que la tocan se podan (PineTierBase).
        public bool Collided { get; }

        public double MinX => AnchorX - LakeArt.CabinFrontWidth / 2;
        public double MaxX => AnchorX + LakeArt.CabinFrontWidth / 2;
        public double MinZ => AnchorZ;
        public double MaxZ => AnchorZ + Depth;
    }

    // Cómo apoya el muelle: el tablero a 0,40 m de x −6 hasta donde la playa llega a 0,30 m; los pilotes en pares (los del legacy)
    // hasta el fondo; el ancla (−9; z₀ + 25) igual al legacy.
    public readonly struct PierLayout
    {
        public PierLayout(double anchorX, double anchorZ, double deckFromX, double deckToX, double deckY, double pileBottom0, double pileBottom1,
                          double pileBottom2)
        {
            AnchorX = anchorX;
            AnchorZ = anchorZ;
            DeckFromX = deckFromX;
            DeckToX = deckToX;
            DeckY = deckY;
            PileBottom0 = pileBottom0;
            PileBottom1 = pileBottom1;
            PileBottom2 = pileBottom2;
        }

        public double AnchorX { get; }
        public double AnchorZ { get; }

        // De la tierra (≈ −10,1) a la punta (−6).
        public double DeckFromX { get; }
        public double DeckToX { get; }
        public double DeckY { get; }

        // La y del fondo bajo cada par de pilotes (LakeArt.PierPileX), la más baja de los dos.
        public double PileBottom0 { get; }
        public double PileBottom1 { get; }
        public double PileBottom2 { get; }

        public double PileBottom(int i)
        {
            return i == 0 ? PileBottom0 : (i == 1 ? PileBottom1 : PileBottom2);
        }
    }

    // La colocación de los objetos del legacy sin tocar su x, z, escala ni variante (06a-escena-lago/02-tech-design §2.13;
    // AC-SCN-15-05, -07; 04-design-notes §7.2): nada flota y nada atraviesa nada.
    public static class PropPlacement
    {
        // It. 8 (02-tech-design §2.4): el pie de los objetos de río, costa y regata. Boyas y puente apoyan en el agua (0; el puente
        // baja a sus pilares); el faro, sobre el promontorio de 2,5 m (DT8-10); el acantilado, sobre las rocas de su pie (el suelo
        // apenas detrás de la pared); el resto, sobre el terreno en su (x, z): el muro del río (1,20), la arena, el talud o el
        // césped. Nunca más de 2,5 m sobre el agua (AC-SCN-14-10).
        public static double FootY(EnvironmentId environment, in ShoreProp prop)
        {
            switch (prop.Kind)
            {
                case PropKind.LaneBuoy:
                case PropKind.Bridge:
                    return 0;
                case PropKind.Lighthouse:
                    return CoastArt.Promontory;
                case PropKind.Cliff:
                    return TerrainProfile.Height(environment, prop.X - 0.01, prop.Z + 1);
                default:
                    return TerrainProfile.Height(environment, prop.X, prop.Z);
            }
        }

        // El pie de un pino: el mínimo del terreno bajo el tronco (radio 0,15·escala) menos 0,20 m.
        public static double PineFootY(EnvironmentId environment, in ShoreProp pine)
        {
            return TerrainProfile.MinUnder(environment, pine.X, pine.Z, LakeArt.PineTrunkRadius * pine.Scale) - LakeArt.PineSink;
        }

        // La cabaña del segmento (con los objetos del mismo segmento: la huella cabe entera adentro de él).
        public static CabinLayout Cabin(EnvironmentId environment, in ShoreProp cabin, ReadOnlySpan<ShoreProp> segmentProps)
        {
            var depths = LakeArt.CabinDepths;
            var chosen = depths[depths.Length - 1];
            var collided = true;
            for (var d = 0; d < depths.Length; d++)
            {
                if (!AnyTrunkInside(cabin, depths[d], LakeArt.CabinClearance, segmentProps))
                {
                    chosen = depths[d];
                    collided = false;
                    break;
                }
            }

            var minX = cabin.X - LakeArt.CabinFrontWidth / 2;
            var maxX = cabin.X + LakeArt.CabinFrontWidth / 2;
            var maxZ = cabin.Z + chosen;
            var c0 = TerrainProfile.Height(environment, minX, cabin.Z);
            var c1 = TerrainProfile.Height(environment, maxX, cabin.Z);
            var c2 = TerrainProfile.Height(environment, minX, maxZ);
            var c3 = TerrainProfile.Height(environment, maxX, maxZ);
            var high = Math.Max(Math.Max(c0, c1), Math.Max(c2, c3));
            var low = Math.Min(Math.Min(c0, c1), Math.Min(c2, c3));
            return new CabinLayout(cabin.X, cabin.Z, chosen, high + LakeArt.CabinFloorLift, low, collided);
        }

        // ¿Hay que podar este pino por la cabaña? Su tronco o su copa (radio 1,3·escala) tocan la huella (+ 0,3 m el tronco,
        // + 0,2 m la copa): su grada de abajo arranca a 3,0 m, sobre el alero de 2,6 m; posición, escala y variante no cambian.
        public static bool IsPrunedBy(in CabinLayout cabin, in ShoreProp pine)
        {
            if (pine.Kind != PropKind.Pine)
            {
                return false;
            }

            var trunk = LakeArt.PineTrunkRadius * pine.Scale;
            var crown = LakeArt.PineMaxHalfWidth * pine.Scale;
            return CircleTouchesRect(pine.X, pine.Z, trunk + LakeArt.CabinClearance, cabin.MinX, cabin.MaxX, cabin.MinZ, cabin.MaxZ)
                || CircleTouchesRect(pine.X, pine.Z, crown + 0.2, cabin.MinX, cabin.MaxX, cabin.MinZ, cabin.MaxZ);
        }

        // ¿El tronco de un pino queda adentro de la huella (sin margen)?
        public static bool TrunkInside(in CabinLayout cabin, in ShoreProp pine)
        {
            return pine.Kind == PropKind.Pine
                && CircleTouchesRect(pine.X, pine.Z, LakeArt.PineTrunkRadius * pine.Scale, cabin.MinX, cabin.MaxX, cabin.MinZ, cabin.MaxZ);
        }

        public static PierLayout Pier(EnvironmentId environment, in ShoreProp pier)
        {
            var landX = -TerrainProfile.AbsXAtHeight(environment, -1, pier.Z, LakeArt.PierBeachHeight);
            var piles = LakeArt.PierPileX;
            var b0 = PileBottom(environment, piles[0], pier.Z);
            var b1 = PileBottom(environment, piles[1], pier.Z);
            var b2 = PileBottom(environment, piles[2], pier.Z);
            return new PierLayout(pier.X, pier.Z, landX, LakeArt.PierTipX, LakeArt.PierDeckHeight, b0, b1, b2);
        }

        private static double PileBottom(EnvironmentId environment, double x, double z)
        {
            return Math.Min(TerrainProfile.Height(environment, x, z - LakeArt.PierPileZOffset),
                TerrainProfile.Height(environment, x, z + LakeArt.PierPileZOffset));
        }

        private static bool AnyTrunkInside(in ShoreProp cabin, double depth, double clearance, ReadOnlySpan<ShoreProp> props)
        {
            var minX = cabin.X - LakeArt.CabinFrontWidth / 2;
            var maxX = cabin.X + LakeArt.CabinFrontWidth / 2;
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (p.Kind == PropKind.Pine
                    && CircleTouchesRect(p.X, p.Z, LakeArt.PineTrunkRadius * p.Scale + clearance, minX, maxX, cabin.Z, cabin.Z + depth))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CircleTouchesRect(double cx, double cz, double r, double minX, double maxX, double minZ, double maxZ)
        {
            var dx = cx < minX ? minX - cx : (cx > maxX ? cx - maxX : 0);
            var dz = cz < minZ ? minZ - cz : (cz > maxZ ? cz - maxZ : 0);
            return dx * dx + dz * dz < r * r;
        }
    }
}
