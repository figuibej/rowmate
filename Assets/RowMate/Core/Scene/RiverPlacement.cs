using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // La rampa de un club del río (DT8-19, R5): paralela al muro en |x| 8,5–10,0, del lado del club, de ZStart a ZEnd (6,6 m),
    // con el arranque a 1,20 (el malecón) en el extremo del portón y el pie a −0,2 en el otro.
    public readonly struct RampCut
    {
        public RampCut(int side, double zStart, double zEnd, bool topAtStart, double clubZ)
        {
            Side = side;
            ZStart = zStart;
            ZEnd = zEnd;
            TopAtStart = topAtStart;
            ClubZ = clubZ;
        }

        public int Side { get; }
        public double ZStart { get; }
        public double ZEnd { get; }
        public bool TopAtStart { get; }

        // El ancla del club (para la regla de las rampas que se pisan: se quita la del club más lejano).
        public double ClubZ { get; }

        public double SurfaceAt(double zWorld)
        {
            var t = SceneMath.Clamp01((zWorld - ZStart) / (ZEnd - ZStart));
            if (!TopAtStart)
            {
                t = 1 - t;
            }

            return RiverArt.QuayHeight + (RiverArt.RampBottomY - RiverArt.QuayHeight) * t;
        }
    }

    // Qué es un volumen del río (para la prueba de cruces y la escala por eje de la instancia).
    public enum PropVolumeKind
    {
        Building,
        Boathouse,
        Lamp,
        BridgeDeck,
        BridgeAbutment,
        BridgePier,
        Ramp,
        Buoy,
    }

    // Una caja alineada a los ejes (m de mundo).
    public readonly struct PropVolume
    {
        public PropVolume(PropVolumeKind kind, double minX, double maxX, double minY, double maxY, double minZ, double maxZ)
        {
            Kind = kind;
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        public PropVolumeKind Kind { get; }
        public double MinX { get; }
        public double MaxX { get; }
        public double MinY { get; }
        public double MaxY { get; }
        public double MinZ { get; }
        public double MaxZ { get; }

        // ¿Se cruzan con más de `tolerance` de solape en los tres ejes? (el contacto no cuenta).
        public bool Overlaps(in PropVolume o, double tolerance)
        {
            return Math.Min(MaxX, o.MaxX) - Math.Max(MinX, o.MinX) > tolerance
                && Math.Min(MaxY, o.MaxY) - Math.Max(MinY, o.MinY) > tolerance
                && Math.Min(MaxZ, o.MaxZ) - Math.Max(MinZ, o.MinZ) > tolerance;
        }
    }

    // Cómo apoya una casa del río (edificio o club; DT8-19 R1 a R4): el ancla del legacy (x, z) es el centro de la cara a la
    // cámara; el volumen crece hacia +z con el fondo recortado; del lado del agua, recortado a InnerAbsX. Las medidas del
    // catálogo (6 o 8 de frente × escala) no cambian: el recorte solo achica el fondo o el ancho interior.
    public readonly struct HouseLayout
    {
        public HouseLayout(in ShoreProp prop, double depth, double innerAbsX, double outerAbsX, double height)
        {
            Prop = prop;
            Depth = depth;
            InnerAbsX = innerAbsX;
            OuterAbsX = outerAbsX;
            Height = height;
        }

        public ShoreProp Prop { get; }
        public double Depth { get; }
        public double InnerAbsX { get; }
        public double OuterAbsX { get; }
        public double Height { get; }

        public int Side => Prop.X < 0 ? -1 : 1;

        public PropVolume Volume => new PropVolume(Prop.Kind == PropKind.Boathouse ? PropVolumeKind.Boathouse : PropVolumeKind.Building,
            Side < 0 ? -OuterAbsX : InnerAbsX, Side < 0 ? -InnerAbsX : OuterAbsX, RiverArt.QuayHeight, RiverArt.QuayHeight + Height, Prop.Z,
            Prop.Z + Depth);
    }

    // La colocación 3D del río sin cruces (DT8-19, 02-tech-design §2.4.1; gate QA-G8-01). Reglas R1 a R6 en orden; dependen solo
    // de los segmentos k − 1 a k + 1 (los pide a su ShoreSegments, sin asignar): el resultado es el mismo en cualquier orden de
    // armado. Ni el generador ni la lista visible cambian. `Rules = false` (solo pruebas) devuelve las medidas nominales sin
    // recortes ni rampas: el control positivo de la prueba de cruces.
    public sealed class RiverPlacement
    {
        public const int MaxHousesPerSegment = 8;
        public const int MaxVolumesPerSegment = 40;
        public const int MaxRampsPerSegment = 8;

        private readonly ShoreSegments _segments;
        private readonly HouseLayout[] _scratchHouses = new HouseLayout[MaxHousesPerSegment];
        private readonly RampCut[] _scratchRamps = new RampCut[3 * MaxRampsPerSegment];

        public RiverPlacement(ShoreSegments segments)
        {
            _segments = segments ?? throw new ArgumentNullException(nameof(segments));
            if (segments.Environment.Id != EnvironmentId.River)
            {
                throw new ArgumentException("RiverPlacement: solo el río.", nameof(segments));
            }
        }

        public bool Rules { get; set; } = true;

        // R1 a R4: las casas del segmento k en el orden del generador.
        public int Houses(long k, HouseLayout[] into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            var n = 0;
            var props = _segments.PropsForSegment(k);
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (p.Kind == PropKind.Building || p.Kind == PropKind.Boathouse)
                {
                    into[n++] = House(k, p);
                }
            }

            return n;
        }

        // R5: las rampas de los clubes del segmento k (sin las que pisan a la de un club más cercano en z).
        public int Ramps(long k, RampCut[] into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            if (!Rules || k < 0)
            {
                return 0;
            }

            // Las candidatas de k − 1 a k + 1 (cada una decide su sentido sola), y después la regla de las que se pisan.
            var all = 0;
            for (var j = k - 1; j <= k + 1; j++)
            {
                all += Candidates(j, _scratchRamps, all);
            }

            var n = 0;
            for (var i = 0; i < all; i++)
            {
                var r = _scratchRamps[i];
                if ((long)Math.Floor(r.ClubZ / ShoreEnvironment.SegmentLength) != k)
                {
                    continue;
                }

                var removed = false;
                for (var j = 0; j < all && !removed; j++)
                {
                    var o = _scratchRamps[j];
                    if (j != i && o.Side == r.Side && o.ClubZ < r.ClubZ && o.ZStart < r.ZEnd && r.ZStart < o.ZEnd)
                    {
                        removed = true;
                    }
                }

                if (!removed)
                {
                    into[n++] = r;
                }
            }

            return n;
        }

        // Las rampas que tocan la z de mundo (de los segmentos vecinos): para TerrainProfile.RiverHeight.
        public int RampsAround(double zWorld, RampCut[] into)
        {
            var k = (long)Math.Floor(zWorld / ShoreEnvironment.SegmentLength);
            var n = 0;
            for (var j = k - 1; j <= k + 1; j++)
            {
                if (j < 0)
                {
                    continue;
                }

                var count = Ramps(j, _scratchRamps2);
                for (var i = 0; i < count; i++)
                {
                    into[n++] = _scratchRamps2[i];
                }
            }

            return n;
        }

        private readonly RampCut[] _scratchRamps2 = new RampCut[MaxRampsPerSegment];

        // Todos los volúmenes del segmento k: casas, faroles, boyas, el puente por piezas y las rampas.
        public int Volumes(long k, PropVolume[] into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            var n = 0;
            var houses = Houses(k, _scratchHouses);
            for (var i = 0; i < houses; i++)
            {
                into[n++] = _scratchHouses[i].Volume;
            }

            var props = _segments.PropsForSegment(k);
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                switch (p.Kind)
                {
                    case PropKind.Lamp:
                        // Poste y globo (r 0,30) sobre el malecón.
                        into[n++] = new PropVolume(PropVolumeKind.Lamp, p.X - RiverArt.LampGlobeRadius, p.X + RiverArt.LampGlobeRadius,
                            RiverArt.QuayHeight, RiverArt.QuayHeight + RiverArt.LampGlobeY + RiverArt.LampGlobeRadius, p.Z - RiverArt.LampGlobeRadius,
                            p.Z + RiverArt.LampGlobeRadius);
                        break;
                    case PropKind.LaneBuoy:
                        into[n++] = new PropVolume(PropVolumeKind.Buoy, p.X - RiverArt.BuoyRadius, p.X + RiverArt.BuoyRadius, -0.1,
                            RiverArt.BuoyCenterY + RiverArt.BuoyRadius, p.Z - RiverArt.BuoyRadius, p.Z + RiverArt.BuoyRadius);
                        break;
                    case PropKind.Bridge:
                        n = Bridge(p, into, n);
                        break;
                }
            }

            var ramps = Ramps(k, _scratchRamps2);
            for (var i = 0; i < ramps; i++)
            {
                var r = _scratchRamps2[i];
                var minX = r.Side < 0 ? -RiverArt.RampMaxX : RiverArt.RampMinX;
                var maxX = r.Side < 0 ? -RiverArt.RampMinX : RiverArt.RampMaxX;
                into[n++] = new PropVolume(PropVolumeKind.Ramp, minX, maxX, RiverArt.RampBottomY, RiverArt.QuayHeight, r.ZStart, r.ZEnd);
            }

            return n;
        }

        // El puente por piezas (DT8-19 R4; AC-SCN-16-03): el tablero de 24 m (±12) de 6,0 a 7,7; los estribos de |x| 9,5 a 12,5
        // sobre el arco lateral de 3 m del paseo (de 4,2 a 7,2); los pilares de 8,5 a 9,5 de 0 a 1,5 (apoyan en el muro).
        private static int Bridge(in ShoreProp p, PropVolume[] into, int n)
        {
            var z0 = p.Z;
            var z1 = p.Z + RiverArt.BridgeDepth;
            into[n++] = new PropVolume(PropVolumeKind.BridgeDeck, -RiverArt.DeckHalfWidth, RiverArt.DeckHalfWidth, RiverArt.DeckBottom,
                RiverArt.ParapetTop, z0, z1);
            var archTop = RiverArt.QuayHeight + RiverArt.BridgeSideArch;
            for (var s = -1; s <= 1; s += 2)
            {
                // El estribo, por fuera del tablero (de 12 a 12,5) y sobre el arco del paseo (9,5 a 12,5, de 4,2 a 6,0).
                into[n++] = new PropVolume(PropVolumeKind.BridgeAbutment, s < 0 ? -RiverArt.BridgeEndX : RiverArt.DeckHalfWidth,
                    s < 0 ? -RiverArt.DeckHalfWidth : RiverArt.BridgeEndX, RiverArt.QuayHeight, RiverArt.DeckTop, z0, z1);
                into[n++] = new PropVolume(PropVolumeKind.BridgeAbutment, s < 0 ? -RiverArt.DeckHalfWidth : RiverArt.BridgePierOuterX,
                    s < 0 ? -RiverArt.BridgePierOuterX : RiverArt.DeckHalfWidth, archTop, RiverArt.DeckBottom, z0, z1);
                into[n++] = new PropVolume(PropVolumeKind.BridgePier, s < 0 ? -RiverArt.BridgePierOuterX : RiverArt.BridgeSpringX,
                    s < 0 ? -RiverArt.BridgeSpringX : RiverArt.BridgePierOuterX, -RiverArt.WallUnderwater, RiverArt.BridgeSpringY, z0, z1);
            }

            return n;
        }

        private HouseLayout House(long k, in ShoreProp p)
        {
            var club = p.Kind == PropKind.Boathouse;
            var s = p.Scale;
            var ax = Math.Abs(p.X);
            var half = club ? (RiverArt.BoathouseWidth / 2 + RiverArt.BoathouseEave) * s : (RiverArt.BuildingWidth / 2 + RiverArt.CorniceOut) * s;
            var nominal = (club ? RiverArt.BoathouseDepth : RiverArt.BuildingDepth) * s;
            var height = club ? RiverArt.BoathouseRidge * s : FacadeWindows.Floors(p.Seed) * RiverArt.FloorHeight * s + RiverArt.ParapetHeight * s;
            var inner = ax - half;
            var outer = ax + half;
            if (!Rules)
            {
                return new HouseLayout(p, nominal, inner, outer, height);
            }

            // R2: del lado del agua, nunca más adentro de 10,1.
            inner = Math.Max(inner, RiverArt.FaceMinX);

            // R3: la casa siguiente de la misma orilla.
            var depth = nominal;
            var next = NextHouseZ(k, p);
            if (!double.IsNaN(next))
            {
                var g = next - p.Z;
                depth = g < 0.05 ? 0 : (g >= 3 ? Math.Min(nominal, g - RiverArt.HouseGap) : Math.Min(nominal, g));
            }

            // R4: los puentes de k − 1 a k + 1.
            for (var j = k - 1; j <= k + 1; j++)
            {
                var props = _segments.PropsForSegment(j);
                for (var i = 0; i < props.Length; i++)
                {
                    if (props[i].Kind != PropKind.Bridge)
                    {
                        continue;
                    }

                    var zB = props[i].Z;
                    if (p.Z < zB - RiverArt.BridgeClearZ)
                    {
                        depth = Math.Min(depth, zB - RiverArt.BridgeClearZ - p.Z);
                    }
                    else if (p.Z <= zB + RiverArt.BridgeDepth + RiverArt.BridgeClearZ)
                    {
                        inner = Math.Max(inner, RiverArt.BridgeClearX);
                    }
                }
            }

            return new HouseLayout(p, Math.Max(0, depth), inner, outer, height);
        }

        // La z de la primera casa de la misma orilla con z mayor (de este segmento o de los dos siguientes); NaN si no hay.
        private double NextHouseZ(long k, in ShoreProp p)
        {
            var best = double.NaN;
            for (var j = k; j <= k + 2; j++)
            {
                var props = _segments.PropsForSegment(j);
                for (var i = 0; i < props.Length; i++)
                {
                    var o = props[i];
                    if ((o.Kind == PropKind.Building || o.Kind == PropKind.Boathouse) && Math.Sign(o.X) == Math.Sign(p.X) && o.Z > p.Z
                        && (double.IsNaN(best) || o.Z < best))
                    {
                        best = o.Z;
                    }
                }

                if (!double.IsNaN(best))
                {
                    return best;
                }
            }

            return best;
        }

        // Las rampas candidatas de los clubes del segmento j, con su sentido (R5), escritas desde `at`.
        private int Candidates(long j, RampCut[] into, int at)
        {
            if (j < 0)
            {
                return 0;
            }

            var n = 0;
            var props = _segments.PropsForSegment(j);
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (p.Kind != PropKind.Boathouse)
                {
                    continue;
                }

                if (TryRamp(j, p, true, out var ramp))
                {
                    into[at + n++] = ramp;
                }
            }

            return n;
        }

        // El sentido de la rampa de un club (R5): hacia +z desde el centro del portón, si no hacia −z, salvo que el portón de otro
        // club de esa orilla esté más cerca hacia +z (entonces primero −z: dos clubes vecinos tiran sus rampas hacia lados
        // opuestos); si un farol cae justo frente al portón (bloquea los dos sentidos), el arranque se corre hasta medio portón
        // (1,5 m: sigue frente al portón). Con `avoidNeighbours`, además esquiva la rampa que eligen los clubes más cercanos en z
        // de esa orilla en el primer nivel (sin mirar a sus vecinos): dos niveles fijos, sin cadenas, así el resultado depende solo
        // de los segmentos cercanos.
        private bool TryRamp(long j, in ShoreProp p, bool avoidNeighbours, out RampCut ramp)
        {
            var house = House(j, p);
            var side = p.X < 0 ? -1 : 1;
            var gate = p.Z + house.Depth / 2;
            var slide = RiverArt.GateWidth / 2;
            var backwardFirst = NearestGateIsAhead(j, side, p.Z, gate);
            for (var c = 0; c < 4; c++)
            {
                var forward = (c % 2 == 0) != backwardFirst;
                var start = gate + (c < 2 ? 0 : (forward ? slide : -slide));
                var z0 = forward ? start : start - RiverArt.RampLength;
                var z1 = z0 + RiverArt.RampLength;
                if (Blocked(j, side, p.Z, z0, z1) || avoidNeighbours && HitsNeighbourRamp(j, side, p.Z, z0, z1))
                {
                    continue;
                }

                ramp = new RampCut(side, z0, z1, forward, p.Z);
                return true;
            }

            ramp = default;
            return false;
        }

        // ¿El portón de otro club de esa orilla más cercano a este queda hacia +z? (segmentos j − 1 a j + 1; con la colocación de
        // R1 a R4, no con rampas: sin cadenas).
        private bool NearestGateIsAhead(long j, int side, double clubZ, double gate)
        {
            var best = double.MaxValue;
            var ahead = false;
            for (var s = j - 1; s <= j + 1; s++)
            {
                var props = _segments.PropsForSegment(s);
                for (var i = 0; i < props.Length; i++)
                {
                    var o = props[i];
                    if (o.Kind != PropKind.Boathouse || Math.Sign(o.X) != side || o.Z == clubZ)
                    {
                        continue;
                    }

                    var other = o.Z + House(s, o).Depth / 2;
                    var d = Math.Abs(other - gate);
                    if (d < best)
                    {
                        best = d;
                        ahead = other > gate;
                    }
                }
            }

            return ahead;
        }

        // ¿Pisa [z0; z1] la rampa de primer nivel de un club más cercano en z de esa orilla (segmentos j − 1 a j + 1)? Solo los
        // más cercanos: tienen prioridad, como en la regla de las que se pisan.
        private bool HitsNeighbourRamp(long j, int side, double clubZ, double z0, double z1)
        {
            for (var s = j - 1; s <= j + 1; s++)
            {
                var props = _segments.PropsForSegment(s);
                for (var i = 0; i < props.Length; i++)
                {
                    var o = props[i];
                    if (o.Kind == PropKind.Boathouse && Math.Sign(o.X) == side && o.Z < clubZ && TryRamp(s, o, false, out var other)
                        && other.ZStart < z1 && z0 < other.ZEnd)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ¿Cae un farol de esa orilla (± 0,5), un puente (± 0,5) o el portón de otro club de esa orilla (± 0,5) en [z0; z1]? El
        // portón del vecino sale de su colocación (R1 a R4), no de su rampa: cada club decide sin mirar la decisión del otro (sin
        // cadenas), y dos clubes cercanos tiran sus rampas hacia lados opuestos en vez de pisarse.
        private bool Blocked(long j, int side, double clubZ, double z0, double z1)
        {
            for (var s = j - 1; s <= j + 1; s++)
            {
                var props = _segments.PropsForSegment(s);
                for (var i = 0; i < props.Length; i++)
                {
                    var o = props[i];
                    if (o.Kind == PropKind.Lamp && Math.Sign(o.X) == side && o.Z + RiverArt.LampClear > z0 && o.Z - RiverArt.LampClear < z1)
                    {
                        return true;
                    }

                    if (o.Kind == PropKind.Bridge && o.Z + RiverArt.BridgeDepth + RiverArt.BridgeClearZ > z0 && o.Z - RiverArt.BridgeClearZ < z1)
                    {
                        return true;
                    }

                    if (o.Kind == PropKind.Boathouse && Math.Sign(o.X) == side && o.Z != clubZ)
                    {
                        var gate = o.Z + House(s, o).Depth / 2;
                        if (gate + RiverArt.LampClear > z0 && gate - RiverArt.LampClear < z1)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
