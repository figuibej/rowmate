using System;
using System.Collections.Generic;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // La malla de un segmento de 50 m de río, costa o regata (It. 8, WP-B1; desvío del instanciado con float4 de §3.3 mientras
    // falten los shaders de WP-D1): los objetos del legacy del segmento con su colocación (RiverPlacement en el río, PropPlacement en
    // los otros), el muro del río, el relleno (EnvironmentFill) y, de respaldo, las ventanas de las fachadas (máscara de
    // FacadeWindows) y las cifras del cartel en 7 segmentos (DT8-15). Todo en coordenadas de cinta del segmento (z − z₀), en DOS
    // submallas: 0 = el cuerpo (material Prop), 1 = lo que se enciende de noche (globos de faroles, portón del club, linterna del
    // faro, sala de la torre y las ventanas encendidas de la máscara; material de ventana de noche, Prop de día).
    // Las mallas de cada tipo y variante se arman una vez (caché) y se copian con su transformación (posición, escala por eje y
    // espejo en x: los recortes de colocación viajan como escala, §2.4.1). Sin asignar después del calentamiento salvo el
    // acantilado (su borde depende de la semilla), que se arma en un búfer reutilizado.
    public sealed class KitSegmentMesh
    {
        // 0 = cercano (LOD0 de las mallas), 1 = lejano (LOD1); el cercano hasta NearRange de la cámara al borde del segmento.
        public const int Lods = 2;
        public const double NearRange = 120;
        public const int BodyVertexCapacity = 20000, BodyIndexCapacity = 60000;
        public const int GlowVertexCapacity = 4000, GlowIndexCapacity = 8000;
        public const int VertexCapacity = BodyVertexCapacity + GlowVertexCapacity;
        public const int IndexCapacity = BodyIndexCapacity + GlowIndexCapacity;

        // El vidrio de las ventanas de día y el color de piel del público (respaldo sin Facade ni Crowd).
        public const uint SpectatorSkin = 0xFFD9A77A;
        public const double WindowInset = 0.03;

        private readonly EnvironmentId _environment;
        private readonly Dictionary<long, MeshData> _cache = new Dictionary<long, MeshData>();
        private readonly MeshData _body = new MeshData(BodyVertexCapacity, BodyIndexCapacity);
        private readonly MeshData _glow = new MeshData(GlowVertexCapacity, GlowIndexCapacity);
        private readonly MeshData _cliff = new MeshData(CliffMesh.VertexCapacity(0), CliffMesh.IndexCapacity(0));
        private readonly DartRandom _rnd = new DartRandom(1);
        private readonly char[] _text = new char[DistanceMarkerText.MaxChars];

        public KitSegmentMesh(EnvironmentId environment)
        {
            if (environment == EnvironmentId.Lake)
            {
                throw new ArgumentException("KitSegmentMesh: el lago tiene sus streamers de la 6a.", nameof(environment));
            }

            _environment = environment;
        }

        public EnvironmentId Environment => _environment;

        // Las mallas de las variantes fijas del escenario, armadas de antemano (el armado del kit; después Build no asigna).
        public void WarmUp()
        {
            for (var lod = 0; lod < Lods; lod++)
            {
                switch (_environment)
                {
                    case EnvironmentId.River:
                        for (var floors = 3; floors <= 6; floors++)
                        {
                            for (var tone = 0; tone < 3; tone++)
                            {
                                Building(floors, false, tone, lod);
                                Building(floors, true, tone, lod);
                            }
                        }

                        Get(PropKind.Boathouse, 0, lod);
                        Get(PropKind.Lamp, 0, lod);
                        Get(PropKind.LaneBuoy, 0, lod);
                        Get(PropKind.Bridge, 0, lod);
                        Wall(lod);
                        CityBlock();
                        break;
                    case EnvironmentId.Coast:
                        Get(PropKind.Lighthouse, 0, lod);
                        Get(PropKind.BeachUmbrella, 0, lod);
                        for (var v = 0; v < LakeArt.RockVariants; v++)
                        {
                            Rock(v, lod);
                        }

                        break;
                    case EnvironmentId.Regatta:
                        Get(PropKind.LaneBuoy, 1, lod);
                        Get(PropKind.Grandstand, 0, lod);
                        Get(PropKind.FinishTower, 0, lod);
                        Get(PropKind.Flag, 0, lod);
                        Get(PropKind.DistanceMarker, 0, lod);
                        Poplar(lod);
                        break;
                }
            }
        }

        // Arma el segmento k en `into` (lo vacía): props = los objetos del legacy del segmento (PropsForSegment), houses = las casas
        // del río con su recorte (RiverPlacement.Houses; vacío en los otros), fill = el relleno (EnvironmentFill.Segment), footY = el
        // pie de cada objeto (PropPlacement.FootY, en el orden de props).
        public void Build(long k, ReadOnlySpan<ShoreProp> props, ReadOnlySpan<double> footY, ReadOnlySpan<HouseLayout> houses, ReadOnlySpan<FillItem> fill,
                          int lod, MeshData into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            lod = lod <= 0 ? 0 : 1;
            _body.Clear();
            _glow.Clear();
            var z0 = k * ShoreEnvironment.SegmentLength;

            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                var y = i < footY.Length ? footY[i] : PropPlacement.FootY(_environment, p);
                var rz = p.Z - z0;
                switch (p.Kind)
                {
                    case PropKind.Building:
                    case PropKind.Boathouse:
                        var found = false;
                        for (var h = 0; h < houses.Length; h++)
                        {
                            var house = houses[h];
                            if (house.Prop.Kind == p.Kind && house.Prop.Z == p.Z && house.Prop.X == p.X)
                            {
                                AddHouse(house, y, rz, lod);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            // Sin colocación (fuera del río): la medida nominal.
                            var half = p.Kind == PropKind.Boathouse ? (RiverArt.BoathouseWidth / 2 + RiverArt.BoathouseEave) * p.Scale
                                : (RiverArt.BuildingWidth / 2 + RiverArt.CorniceOut) * p.Scale;
                            var depth = (p.Kind == PropKind.Boathouse ? RiverArt.BoathouseDepth : RiverArt.BuildingDepth) * p.Scale;
                            AddHouse(new HouseLayout(p, depth, Math.Abs(p.X) - half, Math.Abs(p.X) + half, 0), y, rz, lod);
                        }

                        break;
                    case PropKind.Lamp:
                        Append(Get(PropKind.Lamp, 0, lod), new Xf(p.X, y, rz, 1, 1, 1), 0);
                        break;
                    case PropKind.LaneBuoy:
                        Append(Get(PropKind.LaneBuoy, _environment == EnvironmentId.Regatta ? 1 : 0, lod), new Xf(p.X, 0, rz, 1, 1, 1),
                            RiverArt.BuoyTones[Mod(p.Seed, 3)], false, MotionBuoy, LifePhase(i, p));
                        break;
                    case PropKind.Bridge:
                        Append(Get(PropKind.Bridge, 0, lod), new Xf(0, 0, rz, 1, 1, 1), 0);
                        break;
                    case PropKind.Cliff:
                        // El bloque del faro (z₀ + 25 en el segmento del faro) baja a 2,5 m (DT8-10).
                        var lighthouseBlock = k % 20 == 10 && Math.Abs(rz - 25) < 1e-6;
                        var sy = lighthouseBlock ? CoastArt.Promontory / CoastArt.CliffHeight : p.Scale;
                        CliffMesh.Build(p.Seed, lod, _cliff, _rnd);
                        Append(_cliff, new Xf(p.X, y, rz, 1, sy, 1), 0);
                        break;
                    case PropKind.Lighthouse:
                        Append(Get(PropKind.Lighthouse, 0, lod), new Xf(p.X, CoastArt.Promontory, rz, 1, 1, 1), 0);
                        break;
                    case PropKind.BeachUmbrella:
                        Append(Get(PropKind.BeachUmbrella, 0, lod), new Xf(p.X, y, rz, 1, 1, 1), CoastArt.UmbrellaTones[Mod(p.Seed, 3)], false,
                            MotionUmbrella, LifePhase(i, p));
                        break;
                    case PropKind.Grandstand:
                        Append(Get(PropKind.Grandstand, 0, lod), new Xf(p.X, y, rz, p.X < 0 ? -1 : 1, 1, 1), 0);
                        break;
                    case PropKind.FinishTower:
                        Append(Get(PropKind.FinishTower, 0, lod), new Xf(p.X, y, rz, p.X > 0 ? -1 : 1, 1, 1), 0);
                        break;
                    case PropKind.Flag:
                        Append(Get(PropKind.Flag, 0, lod), new Xf(p.X, y, rz, 1, 1, 1), RegattaArt.FlagTones[Mod(p.Seed, 4)], false, MotionFlag,
                            LifePhase(i, p));
                        break;
                    case PropKind.DistanceMarker:
                        Append(Get(PropKind.DistanceMarker, 0, lod), new Xf(p.X, y, rz, 1, 1, 1), 0);
                        AddMarkerDigits(p, y, rz);
                        break;
                }
            }

            if (_environment == EnvironmentId.River)
            {
                // El muro y el malecón de las dos orillas (la malla es la de la derecha; la izquierda, en espejo).
                var wall = Wall(lod);
                Append(wall, new Xf(0, 0, 0, 1, 1, 1), 0);
                Append(wall, new Xf(0, 0, 0, -1, 1, 1), 0);
            }

            for (var i = 0; i < fill.Length; i++)
            {
                AddFill(fill[i], z0, lod);
            }

            Concat(into);
        }

        // ── Las piezas ──

        private void AddHouse(in HouseLayout house, double footY, double rz, int lod)
        {
            var p = house.Prop;
            var club = p.Kind == PropKind.Boathouse;
            var s = p.Scale;
            var halfLocal = club ? RiverArt.BoathouseWidth / 2 + RiverArt.BoathouseEave : RiverArt.BuildingWidth / 2 + RiverArt.CorniceOut;
            var depthLocal = club ? RiverArt.BoathouseDepth : RiverArt.BuildingDepth;
            var inner = Math.Min(house.InnerAbsX, house.OuterAbsX - 0.1);
            var sx = (house.OuterAbsX - inner) / (2 * halfLocal);
            var cx = (inner + house.OuterAbsX) / 2;
            var sz = Math.Max(house.Depth, 0.05) / depthLocal;
            var side = p.X < 0 ? -1 : 1;
            var xf = new Xf(side * cx, footY, rz, side * sx, s, sz);
            if (club)
            {
                Append(Get(PropKind.Boathouse, 0, lod), xf, 0);
                return;
            }

            var floors = FacadeWindows.Floors(p.Seed);
            var tone = FacadeWindows.Tone(p.Seed);
            Append(Building(floors, HasMansard(p.Seed), tone, lod), xf, 0);
            AddWindows(p.Seed, floors, xf, house.Depth > 0.5);
        }

        // Las ventanas del legacy (shore_painter.dart:141-152), de respaldo mientras falte Facade: 3 por piso al frente y 5 al río; las
        // encendidas de la máscara van a la submalla de la noche.
        private void AddWindows(int seed, int floors, in Xf xf, bool withRiverSide)
        {
            FacadeWindows.Masks(seed, _rnd, out var front, out var river);
            var h = floors * RiverArt.FloorHeight;
            var w = RiverArt.BuildingWidth / 2;
            var d = RiverArt.BuildingDepth;
            for (var f = 0; f < floors; f++)
            {
                var y0 = h - RiverArt.WindowSill - f * RiverArt.FloorHeight - RiverArt.WindowHeight;
                var y1 = y0 + RiverArt.WindowHeight;
                for (var i = 0; i < RiverArt.WindowsPerFloor; i++)
                {
                    var x0 = RiverArt.WindowX0 + i * RiverArt.WindowStepX;
                    var target = FacadeWindows.IsLit(front, f, i) ? _glow : _body;
                    QuadXf(target, xf, x0, y0, -WindowInset, x0 + RiverArt.WindowWidth, y0, -WindowInset, x0 + RiverArt.WindowWidth, y1, -WindowInset,
                        x0, y1, -WindowInset, 0, 0, -1, RiverArt.WindowGlass);
                }

                if (!withRiverSide)
                {
                    continue;
                }

                for (var i = 0; i < RiverArt.RiverWindowsPerFloor; i++)
                {
                    var zc = d * (i + 0.5) / RiverArt.RiverWindowsPerFloor;
                    var lit = (river & (1UL << (f * RiverArt.RiverWindowsPerFloor + i))) != 0;
                    var target = lit ? _glow : _body;
                    var x = -w - WindowInset;
                    QuadXf(target, xf, x, y0, zc + 0.5, x, y0, zc - 0.5, x, y1, zc - 0.5, x, y1, zc + 0.5, -1, 0, 0, RiverArt.WindowGlass);
                }
            }
        }

        // Las cifras del cartel en 7 segmentos (DT8-15, respaldo del atlas): 0,55 m de alto centradas a 2,0 m, comprimidas a 1,60 m.
        private void AddMarkerDigits(in ShoreProp p, double footY, double rz)
        {
            var n = DistanceMarkerText.Format(Math.Max(0, p.Seed), _text);
            var text = new ReadOnlySpan<char>(_text, 0, n);
            var em = MarkerGlyphs.EmMeters() * MarkerGlyphs.HorizontalScale(text);
            var height = MarkerGlyphs.DigitHeightMeters(text);
            var width = MarkerGlyphs.WidthMeters(text);
            var x = p.X - width / 2;
            var yb = footY + RegattaArt.MarkerTextCenterY - height / 2;
            var z = rz - 0.012;   // el frente del tablero está en z = 0 (MarkerMesh); el borde negro, a −0,005
            var stroke = Math.Max(0.05, height * 0.13);
            for (var i = 0; i < n; i++)
            {
                var c = _text[i];
                var advance = MarkerGlyphs.AdvanceEm(c) * em;
                var segs = MarkerGlyphs.SevenSegments(c);
                var gw = advance * 0.78;
                var gx = x + (advance - gw) / 2;
                if (c == 'm')
                {
                    // e, c, g y el trazo central, en la mitad de abajo.
                    var mid = yb + height / 2;
                    Bar(gx, yb, gx + stroke, mid, z);
                    Bar(gx + gw - stroke, yb, gx + gw, mid, z);
                    Bar(gx + gw / 2 - stroke / 2, yb, gx + gw / 2 + stroke / 2, mid, z);
                    Bar(gx, mid - stroke, gx + gw, mid, z);
                }
                else if (segs != 0)
                {
                    var top = yb + height;
                    var mid = yb + height / 2;
                    if ((segs & 1) != 0) Bar(gx, top - stroke, gx + gw, top, z);
                    if ((segs & 2) != 0) Bar(gx + gw - stroke, mid, gx + gw, top, z);
                    if ((segs & 4) != 0) Bar(gx + gw - stroke, yb, gx + gw, mid, z);
                    if ((segs & 8) != 0) Bar(gx, yb, gx + gw, yb + stroke, z);
                    if ((segs & 16) != 0) Bar(gx, yb, gx + stroke, mid, z);
                    if ((segs & 32) != 0) Bar(gx, mid, gx + stroke, top, z);
                    if ((segs & 64) != 0) Bar(gx, mid - stroke / 2, gx + gw, mid + stroke / 2, z);
                }

                x += advance;
            }
        }

        private void Bar(double x0, double y0, double x1, double y1, double z)
        {
            MeshShapes.Quad(_body, x0, y0, z, x1, y0, z, x1, y1, z, x0, y1, z, 0, 0, -1, 0xFF1A1A1A, 0.5);
        }

        private void AddFill(in FillItem item, double z0, int lod)
        {
            var rz = item.Z - z0;
            switch (item.Kind)
            {
                case FillKind.CityBlock:
                    var tone = BuildingMesh.Scale(RiverArt.BuildingTones[Mod(item.Variant, 3)], RiverArt.BlockToneScale);
                    Append(CityBlock(), new Xf(item.X, item.BaseY, rz, item.SizeX, item.Height, item.SizeZ), tone);
                    break;
                case FillKind.CoastRock:
                    Append(Rock(Mod(item.Variant, LakeArt.RockVariants), lod), new Xf(item.X, item.BaseY, rz, item.Height, item.Height, item.Height), 0);
                    break;
                case FillKind.CoastShrub:
                    var h = item.Height;
                    var green = BuildingMesh.Scale(CoastArt.CliffGrass, 1 + 0.15 * item.Tint);
                    Append(Rock(Mod(item.Variant, LakeArt.RockVariants), 1), new Xf(item.X, item.BaseY, rz, 1.4 * h, h, 1.4 * h), green, true);
                    break;
                case FillKind.Poplar:
                    var sy = item.Height / PoplarMesh.Height;
                    Append(Poplar(lod), new Xf(item.X, item.BaseY, rz, 1, sy, 1), 0);
                    break;
                case FillKind.Spectator:
                    var clothes = RegattaArt.CrowdClothes[Mod(item.Variant / 10, RegattaArt.CrowdClothes.Length)];
                    var hw = RegattaArt.CrowdWidth / 2 * 0.9;
                    var first = _body.VertexCount;
                    MeshShapes.Box(_body, item.X - hw, item.BaseY, rz - 0.15, item.X + hw, item.BaseY + 0.72, rz + 0.15, clothes, 0.5);
                    if (lod == 0)
                    {
                        MeshShapes.Box(_body, item.X - 0.11, item.BaseY + 0.74, rz - 0.11, item.X + 0.11, item.BaseY + RegattaArt.CrowdHeight, rz + 0.11,
                            SpectatorSkin, 0.5);
                    }

                    MarkCrowd(first, item.BaseY, item.Rank, item.Tint);
                    break;
            }
        }

        // ── La caché de mallas ──

        private static long Key(int kind, int variant, int lod) => ((long)kind << 32) | ((long)(variant & 0xFFFFFF) << 4) | (uint)lod;

        private MeshData Get(PropKind kind, int variant, int lod)
        {
            var key = Key((int)kind, variant, lod);
            if (_cache.TryGetValue(key, out var mesh))
            {
                return mesh;
            }

            switch (kind)
            {
                case PropKind.Boathouse:
                    mesh = new MeshData(BoathouseMesh.VertexCapacity(lod), BoathouseMesh.IndexCapacity(lod));
                    BoathouseMesh.Build(lod, mesh);
                    break;
                case PropKind.Lamp:
                    mesh = new MeshData(LampMesh.VertexCapacity(lod), LampMesh.IndexCapacity(lod));
                    LampMesh.Build(lod, mesh);
                    break;
                case PropKind.LaneBuoy:
                    mesh = new MeshData(LaneBuoyMesh.VertexCapacity(lod), LaneBuoyMesh.IndexCapacity(lod) + 64 * 3);
                    LaneBuoyMesh.Build(lod, variant == 1, mesh);
                    break;
                case PropKind.Bridge:
                    mesh = new MeshData(BridgeMesh.VertexCapacity(lod), BridgeMesh.IndexCapacity(lod));
                    BridgeMesh.Build(lod, mesh);
                    break;
                case PropKind.Lighthouse:
                    mesh = new MeshData(LighthouseMesh.VertexCapacity(lod), LighthouseMesh.IndexCapacity(lod));
                    LighthouseMesh.Build(lod, mesh);
                    break;
                case PropKind.BeachUmbrella:
                    mesh = new MeshData(UmbrellaMesh.VertexCapacity(lod), UmbrellaMesh.IndexCapacity(lod));
                    UmbrellaMesh.Build(lod, mesh);
                    break;
                case PropKind.Grandstand:
                    mesh = new MeshData(GrandstandMesh.VertexCapacity(lod), GrandstandMesh.IndexCapacity(lod));
                    GrandstandMesh.Build(lod, mesh);
                    break;
                case PropKind.FinishTower:
                    mesh = new MeshData(FinishTowerMesh.VertexCapacity(lod), FinishTowerMesh.IndexCapacity(lod));
                    FinishTowerMesh.Build(lod, mesh);
                    break;
                case PropKind.Flag:
                    mesh = new MeshData(FlagMesh.VertexCapacity(lod), FlagMesh.IndexCapacity(lod));
                    FlagMesh.Build(lod, mesh);
                    break;
                case PropKind.DistanceMarker:
                    mesh = new MeshData(MarkerMesh.VertexCapacity(lod), MarkerMesh.IndexCapacity(lod));
                    MarkerMesh.Build(lod, mesh);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "KitSegmentMesh: tipo sin malla de kit.");
            }

            _cache[key] = mesh;
            return mesh;
        }

        private MeshData Building(int floors, bool mansard, int tone, int lod)
        {
            var key = Key(100, floors * 10 + (mansard ? 5 : 0) + tone, lod);
            if (!_cache.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(BuildingMesh.VertexCapacity(lod), BuildingMesh.IndexCapacity(lod));
                BuildingMesh.Build(floors, mansard, tone, lod, mesh);
                _cache[key] = mesh;
            }

            return mesh;
        }

        private MeshData Wall(int lod)
        {
            var key = Key(101, 0, lod);
            if (!_cache.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(QuayWallMesh.VertexCapacity, QuayWallMesh.IndexCapacity);
                QuayWallMesh.Build(lod, mesh);
                _cache[key] = mesh;
            }

            return mesh;
        }

        private MeshData CityBlock()
        {
            var key = Key(102, 0, 0);
            if (!_cache.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(CityBlockMesh.VertexCapacity, CityBlockMesh.IndexCapacity);
                CityBlockMesh.Build(mesh);
                _cache[key] = mesh;
            }

            return mesh;
        }

        private MeshData Rock(int variant, int lod)
        {
            var key = Key(103, variant, lod);
            if (!_cache.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(512, 1024);
                SmallMeshes.Rock(variant, lod, mesh);
                _cache[key] = mesh;
            }

            return mesh;
        }

        private MeshData Poplar(int lod)
        {
            var key = Key(104, 0, lod);
            if (!_cache.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(PoplarMesh.VertexCapacity(lod), PoplarMesh.IndexCapacity(lod));
                PoplarMesh.Build(lod, mesh);
                _cache[key] = mesh;
            }

            return mesh;
        }

        // 40 % de los techos con mansarda (RiverArt.MansardShare), por la semilla (determinista).
        public static bool HasMansard(int seed)
        {
            return Mod(seed * 7 + 3, 10) < (int)(RiverArt.MansardShare * 10 + 0.5);
        }

        // ── Copia con transformación ──

        private readonly struct Xf
        {
            public Xf(double x, double y, double z, double sx, double sy, double sz)
            {
                X = x;
                Y = y;
                Z = z;
                Sx = sx;
                Sy = sy;
                Sz = sz;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }
            public double Sx { get; }
            public double Sy { get; }
            public double Sz { get; }
        }

        // Copia src (todas sus submallas: la 1 va a la de la noche, el resto al cuerpo) con la transformación. tint ≠ 0: los vértices
        // blancos (la tela, la boya: "el color va por instancia") toman ese color; recolor: todos. El material rugoso (a = 0) pasa a
        // liso: sin Facade, el Prop dibujaría tablas de madera en las fachadas y las rocas.
        // WP-D1 (Prop.shader, "vida"): lo teñido de una bandera, una sombrilla o una boya lleva en uv su marca y su fase: uv.x =
        // −(código + peso) con el peso en [0; 1) (el largo del paño desde el mástil; el radio de la tela desde la punta), uv.y = la fase
        // en rad. Lo demás conserva su uv (en [0; 1]: quieto).
        // QA-V8-01 (AC-SCN-18-04, -20-06): el público lleva −(7 + w) con w = la altura sobre su asiento (0 los pies, 1 la cabeza) y en
        // uv.y su período y su fase juntos: ⌊uv.y⌋ = el período en centésimas de segundo (1,5 a 4 s, EnvironmentFill) y la parte
        // fraccionaria = la fase en vueltas. El shader lo mece y lo levanta apenas (RegattaArt.CrowdSway, CrowdBob).
        public const int MotionNone = 0, MotionFlag = 1, MotionUmbrella = 3, MotionBuoy = 5, MotionCrowd = 7;

        // La marca del público (arriba) en los vértices [first; VertexCount) del cuerpo.
        private void MarkCrowd(int first, double baseY, double period, double phase)
        {
            var packed = Math.Floor(period * 100 + 0.5) + Math.Min(0.999, Math.Max(0, phase));
            for (var v = first; v < _body.VertexCount; v++)
            {
                var w = Math.Min(0.999, Math.Max(0, (_body.Positions[v * 3 + 1] - baseY) / RegattaArt.CrowdHeight));
                _body.Uv[v * 2] = (float)-(MotionCrowd + w);
                _body.Uv[v * 2 + 1] = (float)packed;
            }
        }

        // La fase de la vida de un objeto: por su lugar en el segmento y su semilla (determinista, sin asignar).
        private static double LifePhase(int index, in ShoreProp p)
        {
            var h = (index * 0.618034 + Mod(p.Seed, 97) * 0.137 + Math.Abs(p.X) * 0.071) % 1.0;
            return h * 2 * Math.PI;
        }

        private static double MotionWeight(int motion, MeshData src, int v)
        {
            switch (motion)
            {
                case MotionFlag:
                    return Math.Min(0.999, Math.Max(0, src.Uv[v * 2]));
                case MotionUmbrella:
                    var px = src.Positions[v * 3];
                    var pz = src.Positions[v * 3 + 2];
                    return Math.Min(0.999, Math.Sqrt(px * px + pz * pz) / CoastArt.UmbrellaRadius);
                default:
                    return 0;
            }
        }

        private void Append(MeshData src, in Xf xf, uint tint, bool recolor = false, int motion = MotionNone, double phase = 0)
        {
            for (var s = 0; s < src.SubmeshCount; s++)
            {
                src.Submesh(s, out var start, out var count);
                if (count == 0)
                {
                    continue;
                }

                var target = s == 1 ? _glow : _body;
                // Los vértices que usa esta submalla: el rango [min; max] de sus índices (las mallas se arman en orden).
                var min = int.MaxValue;
                var max = -1;
                for (var i = start; i < start + count; i++)
                {
                    var v = src.Indices[i];
                    min = Math.Min(min, v);
                    max = Math.Max(max, v);
                }

                var baseIndex = target.VertexCount - min;
                var tc = SceneColor.FromArgb(tint);
                for (var v = min; v <= max; v++)
                {
                    double px = src.Positions[v * 3], py = src.Positions[v * 3 + 1], pz = src.Positions[v * 3 + 2];
                    double nx = src.Normals[v * 3], ny = src.Normals[v * 3 + 1], nz = src.Normals[v * 3 + 2];
                    double r = src.Colors[v * 4], g = src.Colors[v * 4 + 1], b = src.Colors[v * 4 + 2], a = src.Colors[v * 4 + 3];
                    double u = src.Uv[v * 2], uv = src.Uv[v * 2 + 1];
                    if (tint != 0 && (recolor || (r >= 0.999 && g >= 0.999 && b >= 0.999)))
                    {
                        r = tc.R;
                        g = tc.G;
                        b = tc.B;
                        if (motion != MotionNone)
                        {
                            u = -(motion + MotionWeight(motion, src, v));
                            uv = phase;
                        }
                    }

                    if (a < 0.25)
                    {
                        a = 0.5;
                    }

                    target.AddVertex(xf.X + px * xf.Sx, xf.Y + py * xf.Sy, xf.Z + pz * xf.Sz, nx / xf.Sx, ny / xf.Sy, nz / xf.Sz, r, g, b, a,
                        u, uv);
                }

                for (var i = start; i < start + count; i += 3)
                {
                    target.AddTriangle(src.Indices[i] + baseIndex, src.Indices[i + 1] + baseIndex, src.Indices[i + 2] + baseIndex);
                }
            }
        }

        private static void QuadXf(MeshData target, in Xf xf, double x0, double y0, double z0, double x1, double y1, double z1, double x2, double y2,
                                   double z2, double x3, double y3, double z3, double nx, double ny, double nz, uint color)
        {
            MeshShapes.Quad(target, xf.X + x0 * xf.Sx, xf.Y + y0 * xf.Sy, xf.Z + z0 * xf.Sz, xf.X + x1 * xf.Sx, xf.Y + y1 * xf.Sy, xf.Z + z1 * xf.Sz,
                xf.X + x2 * xf.Sx, xf.Y + y2 * xf.Sy, xf.Z + z2 * xf.Sz, xf.X + x3 * xf.Sx, xf.Y + y3 * xf.Sy, xf.Z + z3 * xf.Sz, nx / xf.Sx, ny / xf.Sy,
                nz / xf.Sz, color, 0.5);
        }

        // El cuerpo y, en la submalla 1, lo que se enciende.
        private void Concat(MeshData into)
        {
            into.Clear();
            Copy(_body, into, 0);
            into.BeginSubmesh();
            Copy(_glow, into, into.VertexCount);
        }

        private static void Copy(MeshData src, MeshData into, int offset)
        {
            for (var v = 0; v < src.VertexCount; v++)
            {
                into.AddVertex(src.Positions[v * 3], src.Positions[v * 3 + 1], src.Positions[v * 3 + 2], src.Normals[v * 3], src.Normals[v * 3 + 1],
                    src.Normals[v * 3 + 2], src.Colors[v * 4], src.Colors[v * 4 + 1], src.Colors[v * 4 + 2], src.Colors[v * 4 + 3], src.Uv[v * 2],
                    src.Uv[v * 2 + 1]);
            }

            for (var i = 0; i < src.IndexCount; i += 3)
            {
                into.AddTriangle(src.Indices[i] + offset, src.Indices[i + 1] + offset, src.Indices[i + 2] + offset);
            }
        }

        private static int Mod(int a, int b)
        {
            var m = a % b;
            return m < 0 ? m + b : m;
        }
    }
}
