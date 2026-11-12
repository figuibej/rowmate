using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;
using RowMate.Scene.Quality;
using RowMate.Scene.Slots;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace RowMate.Scene
{
    // El mundo armado de la escena (06a-escena-lago/02-tech-design §3.1, §3.4): la raíz (capa RowMateScene), la cámara, la luz, el
    // Volume, las texturas y mallas procedurales, las copias de los materiales y las piezas que aplican el cuadro. NO es de la sesión
    // (P-14): queda armado entre inmersivos y se desarma con Application.lowMemory o al cerrar la app.
    public sealed class SceneWorld : IDisposable
    {
        private readonly List<Object> _owned = new List<Object>();

        internal SceneWorld(GameObject root, int layer)
        {
            Root = root;
            Layer = layer;
        }

        public GameObject Root { get; }

        public int Layer { get; }

        public ChaseCameraRig? Rig { get; internal set; }

        public SceneRenderer? Renderer { get; internal set; }

        public SceneLighting? Lighting { get; internal set; }

        public SceneDirector? Director { get; internal set; }

        public SegmentStore? Store { get; internal set; }

        public ShoreMeshes Meshes { get; } = new ShoreMeshes();

        public Mesh? Quad { get; internal set; }

        public Mesh? HorizonFar { get; internal set; }

        public Mesh? HorizonNear { get; internal set; }

        public Mesh? Hull { get; internal set; }

        public Mesh? WaterGrid { get; internal set; }

        public ProceduralTexture? CloudAtlas { get; internal set; }

        public ProceduralTexture? Moon { get; internal set; }

        public ProceduralTexture? Noise { get; internal set; }

        public ProceduralTexture? CloudLayer { get; internal set; }

        public Texture2DArray? LayerAlbedo { get; internal set; }

        public Texture2DArray? LayerNormal { get; internal set; }

        // Las copias de los materiales (las de los .mat del proyecto nunca cambian en Play).
        public Material? Sky { get; internal set; }
        public Material? Stars { get; internal set; }
        public Material? Clouds { get; internal set; }
        public Material? HorizonFarMaterial { get; internal set; }
        public Material? HorizonNearMaterial { get; internal set; }
        public Material? Water { get; internal set; }
        public Material? Terrain { get; internal set; }
        public Material? Prop { get; internal set; }
        public Material? PropWindow { get; internal set; }
        public Material? Foliage { get; internal set; }
        public Material? Grass { get; internal set; }
        public Material? Reeds { get; internal set; }
        public Material? HullMaterial { get; internal set; }
        public Material? BoatShadow { get; internal set; }
        public Material? ScreenOverlayMaterial { get; internal set; }

        // It. 7 (07-remero-palada/02-tech-design §3.1): las mallas y los materiales del remero, los remos, el carro, los herrajes, el agua
        // que responde y la boya; el remero armado (se rehace con SceneHost.ForceCharacterForTest) y el bote.
        public Mesh? OarShaft { get; internal set; }
        public Mesh? OarBlade { get; internal set; }
        public Mesh? Seat { get; internal set; }
        public Mesh? Buoy { get; internal set; }
        public Mesh? Fittings { get; internal set; }
        public Material? RowerMaterial { get; internal set; }
        public Material? OarMaterial { get; internal set; }
        public Material? OarMaterialRight { get; internal set; }
        public Material? SprayMaterial { get; internal set; }
        public Material? WaterMarksMaterial { get; internal set; }
        public Material? BuoyMaterial { get; internal set; }
        public Material? GlowMaterial { get; internal set; }
        public Material? FoamMaterial { get; internal set; }
        public RowerCharacter? Rower { get; internal set; }
        public BoatVisual? Boat { get; internal set; }

        // It. 6b (06b-agua-avanzada/02-tech-design §4, §5): el agua avanzada (su material, la malla en anillos del nivel y el mapa de
        // normales), las dos aguas, la estela, el reflejo planar (solo con un mundo armado en Alto) y el vidrio desenfocado.
        public Material? WaterAdvancedMaterial { get; internal set; }
        public Material? HudBlurMaterial { get; internal set; }
        public Mesh? WaterRing { get; internal set; }
        public Mesh? WakeRibbon { get; internal set; }
        public Texture2D? WaterNormal { get; internal set; }
        public AdvancedWaterSurface? AdvancedWater { get; internal set; }
        public PlanarReflection? Reflection { get; internal set; }
        public HudBlurPass? Blur { get; internal set; }
        public QualityTier WaterRingTier { get; internal set; }

        // It. 8 (02-tech-design §3.1, DT8-01): el escenario del kit armado (uno en memoria: el almacén de la orilla, sus mallas por
        // segmento y la silueta lejana) y la tira de la silueta de río, costa o regata (null en el lago).
        public RowMate.Core.Scene.EnvironmentId Environment { get; private set; } = RowMate.Core.Scene.EnvironmentId.Lake;

        public Mesh? HorizonStrip { get; private set; }

        // Cambia el kit (en reposo, SceneHost.SwitchKit): descarta el almacén del escenario anterior (sus mallas por segmento) y arma el
        // del nuevo, vacío (la orilla se arma después por tramos); la base común (cielo, agua, bote, remero, materiales) no cambia.
        internal void SetEnvironment(RowMate.Core.Scene.EnvironmentId id, SegmentStore? built = null)
        {
            var store = built ?? new SegmentStore(RowMate.Core.Scene.EnvironmentCatalog.Create(id));
            var old = ReferenceEquals(Store, store) ? null : Store;
            Store = store;
            Environment = id;
            if (Director != null)
            {
                Director.Terrain.Rebind(store);
                Director.Shore.Rebind(store);
                Director.Fill.Rebind(store);
                Director.Kit?.Rebind(store);
                Director.Foam?.Rebind(store);
            }

            old?.Dispose();
            MeshUpload.Destroy(HorizonStrip);
            HorizonStrip = null;
            if (id != RowMate.Core.Scene.EnvironmentId.Lake)
            {
                var data = new RowMate.Core.Scene.Geometry.MeshData(RowMate.Core.Scene.Geometry.EnvironmentHorizonStrip.VertexCapacity,
                    RowMate.Core.Scene.Geometry.EnvironmentHorizonStrip.IndexCapacity);
                RowMate.Core.Scene.Geometry.EnvironmentHorizonStrip.Build(id, data);
                HorizonStrip = MeshUpload.NewMesh("scene-horizon-" + id);
                using (var upload = new MeshUpload())
                {
                    upload.Upload(data, HorizonStrip, false, SceneObjects.Everywhere);
                }
            }

            Director?.Horizon.SetEnvironment(id, HorizonStrip);
            Director?.Glow?.SetEnvironment(id);
            // WP-D1: el suelo del escenario (adoquín, arena, césped con franjas; 0 = el lago de la 6a) en la copia del material.
            Terrain?.SetFloat(SceneShaderIds.TerrainKind, (float)(int)id);
        }

        // Las resoluciones de los slots (SlotValidator, SSL).
        public List<SlotResolution> Slots { get; } = new List<SlotResolution>();

        internal T Own<T>(T obj) where T : Object
        {
            _owned.Add(obj);
            return obj;
        }

        public void Dispose()
        {
            Reflection?.Dispose();
            Reflection = null;
            Blur?.Dispose();
            Blur = null;
            AdvancedWater?.Destroy();
            AdvancedWater = null;
            Store?.Dispose();
            MeshUpload.Destroy(HorizonStrip);
            HorizonStrip = null;
            Rower?.Dispose();
            Rower = null;
            Lighting?.Dispose();
            Meshes.Destroy();
            for (var i = _owned.Count - 1; i >= 0; i--)
            {
                MeshUpload.Destroy(_owned[i]);
            }

            _owned.Clear();
            MeshUpload.Destroy(Root);
        }
    }

    // Arma el mundo por tramos (06a-escena-lago/02-tech-design §2.16, §3.4; QA-G6aT-03): cada MoveNext es una unidad de costo acotado
    // (una parte de la raíz, la resolución de los slots, unas filas de una textura, una malla, los materiales, una variante, un Tick
    // sintético o el único dibujo de calentamiento). Quien llama corre unidades mientras le quede tramo (SceneWarmupPolicy) y al menos
    // una por cuadro con tramo > 0. Sin pasos indivisibles.
    public sealed class SceneWorldBuilder
    {
        // Los texeles de una textura procedural por unidad (QA-V6a-REN-06: la capa de nubes de Alto, 1 024 de ancho, hacía 24 576 por
        // unidad, ≈ 6 ms en el Editor; con 4 096, el ruido costaba ≈ 3,8 ms en el player): las filas que entran en ese número, al menos una.
        private const int TexelsPerUnit = 2048;

        private static int RowsPerUnit(ProceduralTexture texture)
        {
            return Math.Max(1, TexelsPerUnit / Math.Max(1, texture.Texture.width));
        }

        private readonly SceneWorldDefinition _definition;
        private readonly QualityTierProfile _profile;
        private readonly SlotLog _slotLog;
        private readonly IAppLogger _logger;

        public SceneWorldBuilder(SceneWorldDefinition definition, QualityTierProfile profile, SlotLog slotLog, IAppLogger logger)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _slotLog = slotLog ?? throw new ArgumentNullException(nameof(slotLog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SceneWorld? World { get; private set; }

        // It. 7: qué remero se arma (SceneHostOptions.Character) y, para las pruebas, un binding en lugar del de Resources.
        public RowerCharacterKind Character { get; set; } = RowerCharacterKind.Auto;

        public RowerCharacterBinding? Binding { get; set; }

        // It. 8: el escenario del kit que se arma con el mundo (SceneHost lo pone con el elegido).
        public EnvironmentId Environment { get; set; } = EnvironmentId.Lake;

        public int Units { get; private set; }

        // Dentro del tramo de las mallas (el MeshUpload con sus búferes nativos abierto): la prueba de que un armado cortado ahí no deja
        // memoria nativa colgada (QA-V6a-REN-08).
        internal bool UploadOpen { get; private set; }

        // Qué arma la unidad en curso (la peor unidad de [PERF] y de SET dice cuál fue; QA-V6a-REN-06).
        internal string Step { get; private set; } = "";

        // La capa de la escena (la crea UrpProvisioner; sin ella, la 31: la cámara de la UI no ve ninguna capa).
        public static int SceneLayer()
        {
            var layer = LayerMask.NameToLayer(SceneShaderIds.LayerName);
            return layer >= 0 ? layer : 31;
        }

        // Las unidades del armado hasta los materiales y las piezas (las variantes, los Tick sintéticos y el dibujo de calentamiento los
        // corre SceneHost, que tiene la sesión y la RT).
        public IEnumerator<bool> Build()
        {
            var layer = SceneLayer();
            var root = new GameObject("RowMate.Scene") { hideFlags = HideFlags.DontSave, layer = layer };
            root.SetActive(false);
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(root);
            }

            Step = "la raíz y la cámara";
            var world = new SceneWorld(root, layer);
            World = world;
            world.Rig = new ChaseCameraRig(root.transform, layer);
            world.Renderer = new SceneRenderer(world.Rig.Camera);
            Units++;
            yield return false;

            Step = "la luz y el Volume";
            world.Lighting = new SceneLighting(root.transform, layer);
            Units++;
            yield return false;

            // 2. Los slots (la resolución y su validación: un aviso por slot que falta o no valida, nunca por uno procedural).
            Step = "los slots";
            var cloudSlot = Resolve(SlotIds.SkyClouds);
            var moonSlot = Resolve(SlotIds.SkyMoon);
            foreach (var id in LayerSlotIds)
            {
                Resolve(id);
            }

            foreach (var slot in _definition.Slots)
            {
                if (slot != null && !Resolved(slot.SlotId))
                {
                    Resolve(slot.SlotId);
                }
            }

            if (_definition.Lake != null)
            {
                foreach (var slot in _definition.Lake.Slots)
                {
                    if (slot != null && !Resolved(slot.SlotId))
                    {
                        Resolve(slot.SlotId);
                    }
                }
            }

            // It. 8: los slots de río, costa y regata (todos en Procedural: 0 avisos; uno con un asset que falta, un aviso y el procedural).
            foreach (var environment in _definition.OtherEnvironments)
            {
                if (environment == null)
                {
                    continue;
                }

                foreach (var slot in environment.Slots)
                {
                    if (slot != null && !Resolved(slot.SlotId))
                    {
                        Resolve(slot.SlotId);
                    }
                }
            }

            Units++;
            yield return false;

            // 3. Las texturas procedurales, por filas (la última, con la subida, en su propia unidad).
            if (cloudSlot.UsesProcedural)
            {
                Step = "el atlas de nubes";
                world.CloudAtlas = new ProceduralTexture(ProceduralTexture.Kind.CloudAtlas, "scene-cloud-atlas");
                world.Own(world.CloudAtlas.Texture);
                while (!world.CloudAtlas.BuildRows(RowsPerUnit(world.CloudAtlas)))
                {
                    Units++;
                    yield return false;
                }

                Units++;
                yield return false;
            }

            if (moonSlot.UsesProcedural)
            {
                Step = "la luna";
                world.Moon = new ProceduralTexture(ProceduralTexture.Kind.Moon, "scene-moon");
                world.Own(world.Moon.Texture);
                while (!world.Moon.BuildRows(RowsPerUnit(world.Moon)))
                {
                    Units++;
                    yield return false;
                }

                Units++;
                yield return false;
            }

            Step = "el ruido";
            world.Noise = new ProceduralTexture(ProceduralTexture.Kind.Noise, "scene-noise");
            world.Own(world.Noise.Texture);
            while (!world.Noise.BuildRows(RowsPerUnit(world.Noise)))
            {
                Units++;
                yield return false;
            }

            Units++;
            yield return false;
            if (_profile.CloudLayer)
            {
                Step = "la capa de nubes";
                world.CloudLayer = new ProceduralTexture(ProceduralTexture.Kind.CloudLayer, "scene-cloud-layer");
                world.Own(world.CloudLayer.Texture);
                while (!world.CloudLayer.BuildRows(RowsPerUnit(world.CloudLayer)))
                {
                    Units++;
                    yield return false;
                }

                Units++;
                yield return false;
            }

            // Las capas del terreno de un pack (DN6a-15): a dos Texture2DArray, si entran todas; si no, el procedural.
            Step = "las capas del terreno";
            BuildTerrainLayers(world);
            Units++;
            yield return false;

            // 4. Las mallas, de a una.
            Step = "el quad y la grilla del agua";
            world.Quad = world.Own(MeshUpload.Quad("scene-quad", 100000f));
            world.WaterGrid = world.Own(WaterSurface.BuildGrid());
            Units++;
            yield return false;

            // El MeshUpload vive durante varias unidades: si el armado se corta acá (memoria baja, la app que se cierra, la escena
            // demorada), SceneHost descarta el iterador con Dispose y el finally del using suelta los búferes nativos (QA-V6a-REN-08).
            using (var upload = new MeshUpload())
            {
                UploadOpen = true;
                try
                {
                    Step = "las cordilleras";
                    var data = new MeshData(HorizonStripMesh.VertexCapacity, HorizonStripMesh.IndexCapacity);
                    HorizonStripMesh.Build(true, data);
                    world.HorizonFar = world.Own(MeshUpload.NewMesh("scene-horizon-far"));
                    upload.Upload(data, world.HorizonFar, false, SceneObjects.Everywhere);
                    HorizonStripMesh.Build(false, data);
                    world.HorizonNear = world.Own(MeshUpload.NewMesh("scene-horizon-near"));
                    upload.Upload(data, world.HorizonNear, false, SceneObjects.Everywhere);
                    Units++;
                    yield return false;

                    Step = "el casco";
                    data = new MeshData(HullMesh.VertexCapacity, HullMesh.IndexCapacity);
                    HullMesh.Build(data);
                    world.Hull = world.Own(MeshUpload.NewMesh("scene-hull"));
                    upload.Upload(data, world.Hull, false, null, true);
                    Units++;
                    yield return false;

                    for (var form = 0; form < 3; form++)
                    {
                        for (var lod = 0; lod < 3; lod++)
                        {
                            Step = "un pino";
                            data = new MeshData(PineMesh.VertexCapacity(lod), PineMesh.IndexCapacity(lod));
                            PineMesh.Build(form, lod, data);
                            world.Meshes.Pine[form, lod] = MeshUpload.NewMesh("pine-" + form + "-" + lod);
                            upload.Upload(data, world.Meshes.Pine[form, lod], false, null, true);
                            Units++;
                            yield return false;

                            Step = "un pino podado";
                            PineMesh.Build(form, lod, data, LakeArt.CabinPrunedTierBase);
                            world.Meshes.PrunedPine[form, lod] = MeshUpload.NewMesh("pine-pruned-" + form + "-" + lod);
                            upload.Upload(data, world.Meshes.PrunedPine[form, lod], false, null, true);
                            Units++;
                            yield return false;
                        }

                        for (var lod = 0; lod < 2; lod++)
                        {
                            Step = "el bosque";
                            data = new MeshData(PineMesh.ForestVertexCapacity(lod), PineMesh.ForestIndexCapacity(lod));
                            PineMesh.BuildForest(form, lod, data);
                            world.Meshes.Forest[form, lod] = MeshUpload.NewMesh("forest-" + form + "-" + lod);
                            upload.Upload(data, world.Meshes.Forest[form, lod], false, null, true);
                            Units++;
                            yield return false;
                        }
                    }

                    // Las rocas, los juncos y el pasto: una variante (sus dos LOD) por unidad (QA-V6a-REN-06: antes iban todas juntas).
                    data = new MeshData(512, 1024);
                    for (var v = 0; v < LakeArt.RockVariants; v++)
                    {
                        for (var lod = 0; lod < 2; lod++)
                        {
                            Step = "una roca";
                            SmallMeshes.Rock(v, lod, data);
                            world.Meshes.Rock[v, lod] = MeshUpload.NewMesh("rock-" + v + "-" + lod);
                            upload.Upload(data, world.Meshes.Rock[v, lod], false, null, true);
                        }

                        Units++;
                        yield return false;
                    }

                    for (var v = 0; v < ShoreMeshes.ReedVariants; v++)
                    {
                        for (var lod = 0; lod < 2; lod++)
                        {
                            Step = "un junco";
                            SmallMeshes.ReedTuft(v, lod, data);
                            world.Meshes.Reed[v, lod] = MeshUpload.NewMesh("reed-" + v + "-" + lod);
                            upload.Upload(data, world.Meshes.Reed[v, lod], false, null, true, true);
                        }

                        Units++;
                        yield return false;
                    }

                    for (var lod = 0; lod < 2; lod++)
                    {
                        Step = "el pasto";
                        SmallMeshes.GrassTuft(0, lod, data);
                        world.Meshes.Grass[lod] = MeshUpload.NewMesh("grass-" + lod);
                        upload.Upload(data, world.Meshes.Grass[lod], false, null, true, true);
                    }

                    Units++;
                    yield return false;

                    // It. 7: los remos (la caña y la pala en mallas separadas: solo la pala proyecta sombra, DA7-06), el carro y la boya.
                    Step = "los remos, el carro y la boya";
                    data = new MeshData(OarMesh.VertexCapacity, OarMesh.IndexCapacity);
                    OarMesh.Build(RowMate.Core.Scene.Stroke.BoatSpec.Real, data);
                    var oar = MeshUpload.NewMesh("scene-oar");
                    upload.Upload(data, oar, false, null, true);
                    world.OarShaft = world.Own(Submesh(oar, 0, "scene-oar-shaft"));
                    world.OarBlade = world.Own(Submesh(oar, 1, "scene-oar-blade"));
                    MeshUpload.Destroy(oar);
                    data = new MeshData(SeatMesh.VertexCapacity, SeatMesh.IndexCapacity);
                    SeatMesh.Build(data);
                    world.Seat = world.Own(MeshUpload.NewMesh("scene-boat-seat"));
                    upload.Upload(data, world.Seat, false, null, true);
                    data = new MeshData(BuoyMesh.VertexCapacity, BuoyMesh.IndexCapacity);
                    BuoyMesh.Build(data);
                    world.Buoy = world.Own(MeshUpload.NewMesh("scene-buoy"));
                    upload.Upload(data, world.Buoy, false, null, true);
                    Units++;
                    yield return false;
                }
                finally
                {
                    UploadOpen = false;
                }
            }

            // 5. Las copias de los materiales, con las texturas y el nivel (en tres unidades: QA-V6a-REN-06).
            Step = "los materiales del cielo y el agua";
            var m = _definition.Materials;
            world.Sky = Copy(world, m.Sky, "Sky");
            world.Stars = Copy(world, m.Stars, "Stars");
            world.Clouds = Copy(world, m.Clouds, "Clouds");
            world.HorizonFarMaterial = Copy(world, m.HorizonFar, "HorizonFar");
            world.HorizonNearMaterial = Copy(world, m.HorizonNear, "HorizonNear");
            world.Water = Copy(world, m.Water, "WaterDegraded");
            Units++;
            yield return false;

            Step = "los materiales de la orilla y el bote";
            world.Terrain = Copy(world, m.Terrain, "Terrain");
            world.Prop = Copy(world, m.Prop, "Prop");
            world.PropWindow = Copy(world, m.PropWindow, "PropWindow");
            world.Foliage = Copy(world, m.Foliage, "Foliage");
            world.Grass = Copy(world, m.Grass, "Grass");
            world.Reeds = Copy(world, m.Reeds, "Reeds");
            world.HullMaterial = Copy(world, m.Hull, "Hull");
            world.BoatShadow = Copy(world, m.BoatShadow, "BoatShadow");
            world.ScreenOverlayMaterial = Copy(world, m.ScreenOverlay, "ScreenOverlay");
            Units++;
            yield return false;

            Step = "los materiales del remero y del agua que responde";
            world.RowerMaterial = Copy(world, m.Rower, "Rower");
            world.OarMaterial = Copy(world, m.Oar, "Oar");
            world.OarMaterialRight = Copy(world, m.Oar, "Oar");
            world.OarMaterialRight.name = "scene-Oar-right";
            world.SprayMaterial = Copy(world, m.Spray, "Spray");
            world.WaterMarksMaterial = Copy(world, m.WaterMarks, "WaterMarks");
            world.BuoyMaterial = Copy(world, m.Buoy, "Buoy");
            world.GlowMaterial = Copy(world, m.Glow, "Glow");
            world.GlowMaterial.enableInstancing = true;
            world.FoamMaterial = Copy(world, m.Foam, "Foam");
            world.FoamMaterial.enableInstancing = true;
            // It. 6b: el agua avanzada (sin el asset, el shader por nombre) y el vidrio. Si el shader no está, el mundo sigue con la degradada.
            world.WaterAdvancedMaterial = CopyOptional(world, m.WaterAdvanced, SceneShaderIds.WaterAdvanced, "WaterAdvanced");
            world.HudBlurMaterial = CopyOptional(world, m.HudBlur, SceneShaderIds.HudBlur, "HudBlur");
            ConfigureRowerMaterials(world);
            Units++;
            yield return false;

            Step = "las texturas de los materiales";
            world.Sky.SetTexture(SceneShaderIds.MoonTex, moonSlot.As<Texture2D>() ?? world.Moon!.Texture);
            world.Clouds.SetTexture(SceneShaderIds.CloudAtlas, cloudSlot.As<Texture2D>() ?? world.CloudAtlas!.Texture);
            if (world.CloudLayer != null)
            {
                world.Clouds.SetTexture(SceneShaderIds.CloudLayer, world.CloudLayer.Texture);
            }

            world.Terrain.SetTexture(SceneShaderIds.NoiseTex, world.Noise.Texture);
            world.FoamMaterial!.SetTexture(SceneShaderIds.NoiseTex, world.Noise.Texture);
            if (world.LayerAlbedo != null && world.LayerNormal != null)
            {
                world.Terrain.SetTexture(SceneShaderIds.LayerAlbedo, world.LayerAlbedo);
                world.Terrain.SetTexture(SceneShaderIds.LayerNormal, world.LayerNormal);
                world.Terrain.EnableKeyword(SceneShaderIds.TerrainLayersKeyword);
            }
            else
            {
                world.Terrain.DisableKeyword(SceneShaderIds.TerrainLayersKeyword);
            }

            Units++;
            yield return false;

            // 6. Las piezas (en varias unidades: QA-V6a-REN-06).
            // It. 6b: la malla en anillos del nivel (WaterRingMesh, ≤ 20 k triángulos) y el mapa de normales (el slot water.normalDetail,
            // horneado por WaterNormalBaker; sin él, uno procedural chico).
            Step = "la malla del agua avanzada";
            if (world.WaterAdvancedMaterial != null)
            {
                world.WaterRing = world.Own(MeshUpload.NewMesh("scene-water-rings"));
                world.WaterRingTier = _profile.Tier;
                using (var upload = new MeshUpload())
                {
                    upload.Upload(ForwardOnly(WaterRingMesh.Create(WaterMeshTier(_profile.Tier))), world.WaterRing, false, SceneObjects.Everywhere);
                }

                // El slot sigue en Procedural (AC-6B-37): el procedural es el PNG horneado por WaterNormalBaker, que el material ya trae.
                // Un asset asignado al slot gana; sin ninguno de los dos, uno procedural chico.
                world.WaterNormal = Resolved(SlotIds.WaterNormalDetail) ? Resolve(SlotIds.WaterNormalDetail).As<Texture2D>() : null;
                if (world.WaterNormal == null)
                {
                    world.WaterNormal = world.WaterAdvancedMaterial.GetTexture(SceneShaderIds.WaterNormal) as Texture2D;
                }

                if (world.WaterNormal == null)
                {
                    world.WaterNormal = world.Own(ProceduralWaterNormal(FallbackNormalSize));
                }
            }

            world.WakeRibbon = world.Own(WakeRenderer.BuildCombined());
            Units++;
            yield return false;

            Step = "el cielo, las cordilleras, el agua, el bote y los degradés";
            var root2 = root.transform;
            var lighting = world.Lighting!;
            var sky = new SkyRenderer(root2, layer, world.Rig.Camera, world.Quad, world.Sky, world.Stars, world.Clouds);
            var horizon = new HorizonRenderer(root2, layer, world.HorizonFar!, world.HorizonNear!, world.HorizonFarMaterial, world.HorizonNearMaterial);
            var water = new WaterSurface(root2, layer, world.WaterGrid, world.Water);
            if (world.WaterAdvancedMaterial != null && world.WaterRing != null)
            {
                world.AdvancedWater = new AdvancedWaterSurface(root2, layer, world.WaterRing, world.WaterAdvancedMaterial, world.WaterNormal, _profile.Tier);
            }
            world.Fittings = world.Own(MeshUpload.NewMesh("scene-boat-fittings"));
            var boat = new BoatVisual(root2, layer, world.Hull!, world.HullMaterial, world.Quad, world.BoatShadow, world.Fittings, world.Seat!);
            world.Boat = boat;
            var overlay = new ScreenOverlay(root2, layer, world.Quad, world.ScreenOverlayMaterial);
            Units++;
            yield return false;

            // It. 7 (07-remero-palada/02-tech-design §3.1, §3.2): el remero (el personaje del slot se instancia en su unidad y se viste
            // en otra), los herrajes a su medida (RiggingFit), los remos, el agua que responde, la salpicadura y la boya.
            Step = "el remero";
            world.Rower = RowerCharacterResolver.Resolve(Character, Binding, boat.Pitch, layer, world.RowerMaterial!, _slotLog);
            Units++;
            yield return false;

            if (world.Rower.FromSlot)
            {
                Step = "la ropa del remero";
                RowerCharacterResolver.Dress(world.Rower, world.RowerMaterial!);
                Units++;
                yield return false;
            }

            Step = "los herrajes, los remos y el agua que responde";
            BuildFittings(world.Fittings, world.Rower.Spec);
            var rightOar = world.OarMaterialRight!;
            var oars = new OarRig(boat.Pitch, layer, world.OarShaft!, world.OarBlade!, world.OarMaterial!, rightOar, RowMate.Core.Scene.Stroke.BoatSpec.Real);
            // It. 6b: el lote de marcas dibuja la malla que junta el quad y la cinta de la estela (la estela es una instancia más).
            var marks = new WaterInteractionRenderer(world.WakeRibbon ?? world.Quad, world.WaterMarksMaterial!, world.Rig.Camera, layer);
            var vfx = new StrokeVfx(world.Quad, world.SprayMaterial!, world.Rig.Camera, layer);
            var buoy = new MarkerBuoy(root2, layer, world.Buoy!, world.BuoyMaterial!);
            var wake = new WakeRenderer();
            marks.Wake = wake;
            Units++;
            yield return false;

            Step = "el almacén de la orilla";
            // It. 8: el almacén del escenario elegido (el kit; la silueta va al final, con el director).
            world.Store = new SegmentStore(EnvironmentCatalog.Create(Environment));
            Units++;
            yield return false;

            Step = "el terreno";
            var terrain = new TerrainStreamer(root2, layer, world.Store, world.Terrain);
            Units++;
            yield return false;

            Step = "los objetos de la orilla";
            var shore = new ShoreStreamer(world.Store, world.Meshes, world.Foliage, world.Prop, world.PropWindow, world.Rig.Camera, layer);
            var kit = new KitPropRenderer(world.Store, world.Prop, world.PropWindow, world.Rig.Camera, layer);
            var glow = new GlowRenderer(world.Quad!, world.GlowMaterial!, world.Rig.Camera, layer);
            Units++;
            yield return false;

            Step = "el relleno";
            var fill = new FillStreamer(world.Store, world.Meshes, shore, world.Foliage, world.Prop, world.Grass, world.Reeds, world.Rig.Camera, layer);
            Units++;
            yield return false;

            Step = "el director y el nivel";
            world.Director = new SceneDirector(world.Rig, lighting, sky, horizon, water, terrain, shore, fill, boat, overlay, root)
            {
                Rower = world.Rower,
                Oars = oars,
                WaterMarks = marks,
                Spray = vfx,
                Buoy = buoy,
            };
            world.Director.Kit = kit;
            world.Director.WaterSurfaces = new WaterSurfaceController(water, world.AdvancedWater);
            world.Director.Wake = wake;
            if (world.HudBlurMaterial != null)
            {
                world.Blur = new HudBlurPass(world.HudBlurMaterial);
            }


            world.Director.Glow = glow;
            world.Director.Foam = new FoamRenderer(world.Store, world.Own(FoamRenderer.BuildStrip()), world.FoamMaterial!, world.Rig.Camera, layer);
            var seagulls = new SeagullRenderer(world.Prop, world.Rig.Camera, layer);
            world.Own(seagulls.Mesh);
            world.Director.Seagulls = seagulls;
            glow.Beam = _profile.Tier != QualityTier.Low;   // §5: en Bajo, sin el haz del faro
            world.SetEnvironment(Environment, world.Store);

            marks.BoilsEnabled = _profile.Tier != QualityTier.Low;
            SceneQualityApplier.Apply(_profile, world.Rig.Camera, world.Rig.Data, lighting, world.Sky, world.Clouds, world.Grass, sky, shore, fill, world.Store);
            kit.Shadows = _profile.Shadows;
            Units++;
            yield return true;
        }

        // Una submalla como malla propia (los mismos vértices; solo sus índices).
        internal static Mesh Submesh(Mesh source, int submesh, string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = source.vertices;
            mesh.normals = source.normals;
            mesh.colors = source.colors;
            mesh.uv = source.uv;
            mesh.SetTriangles(source.GetTriangles(submesh), 0, true);
            return mesh;
        }

        // Los colores de las regiones del remero y de las partes del remo (RowerArt, en lineal, con la suavidad en w) y los tamaños mínimos.
        internal static void ConfigureRowerMaterials(SceneWorld world)
        {
            var regions = new Vector4[RowerArt.RegionCount];
            for (var i = 0; i < regions.Length; i++)
            {
                regions[i] = Linear(RowerArt.RegionColors[i], RowerArt.RegionSmoothness[i]);
            }

            world.RowerMaterial!.SetVectorArray(SceneShaderIds.RowerColors, regions);
            world.RowerMaterial.SetFloat(SceneShaderIds.RimFill, (float)RowerArt.RimFill);
            var parts = new Vector4[RowerArt.OarColors.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = Linear(RowerArt.OarColors[i], RowerArt.OarSmoothness[i]);
            }

            world.OarMaterial!.SetVectorArray(SceneShaderIds.OarColors, parts);
            world.OarMaterial.SetFloat(SceneShaderIds.MinWidthDp, (float)RowerArt.MinShaftDp);
            world.OarMaterialRight!.SetVectorArray(SceneShaderIds.OarColors, parts);
            world.OarMaterialRight.SetFloat(SceneShaderIds.MinWidthDp, (float)RowerArt.MinShaftDp);
            world.SprayMaterial!.SetFloat(SceneShaderIds.MinDropDp, (float)RowerArt.MinDropDp);
            world.SprayMaterial.enableInstancing = true;
            world.WaterMarksMaterial!.enableInstancing = true;
            world.BuoyMaterial!.SetColor(SceneShaderIds.BuoyColor, SceneLighting.Srgb(SceneColor.FromArgb(RowerArt.BuoyOrange)));
        }

        private static Vector4 Linear(uint argb, double smoothness)
        {
            var c = SceneLighting.Srgb(SceneColor.FromArgb(argb)).linear;
            return new Vector4(c.r, c.g, c.b, (float)smoothness);
        }

        // Los herrajes a la medida del remero (la pedalera y los rieles de RiggingFit).
        internal static void BuildFittings(Mesh mesh, in RowMate.Core.Scene.Stroke.RowerSpec spec)
        {
            var boat = RowMate.Core.Scene.Stroke.BoatSpec.Real;
            var fit = RowMate.Core.Scene.Stroke.RiggingFit.Solve(boat, spec);
            var data = new MeshData(BoatFittingsMesh.VertexCapacity, BoatFittingsMesh.IndexCapacity);
            BoatFittingsMesh.Build(boat, spec, fit, data);
            using (var upload = new MeshUpload())
            {
                upload.Upload(data, mesh, false, null, true);
            }
        }

        private static readonly string[] LayerSlotIds = { SlotIds.TerrainPebbles, SlotIds.TerrainGrass, SlotIds.TerrainSoil, SlotIds.TerrainRock };

        private readonly Dictionary<string, SlotResolution> _resolved = new Dictionary<string, SlotResolution>(StringComparer.Ordinal);

        private bool Resolved(string id) => _resolved.ContainsKey(id);

        private SlotResolution Resolve(string id)
        {
            if (_resolved.TryGetValue(id, out var existing))
            {
                return existing;
            }

            var resolution = SlotResolver.Resolve(_definition.Find(id), id, _slotLog);
            _resolved[id] = resolution;
            World?.Slots.Add(resolution);
            return resolution;
        }

        // Las capas del pack: las cuatro asignadas, del mismo tamaño y formato, entran en el Texture2DArray (DN6a-15). Una que no entra
        // es un asset asignado que no valida: el procedural (sin la palabra clave) y un aviso.
        private void BuildTerrainLayers(SceneWorld world)
        {
            var layers = new TerrainLayer?[LayerSlotIds.Length];
            var any = false;
            for (var i = 0; i < LayerSlotIds.Length; i++)
            {
                layers[i] = Resolve(LayerSlotIds[i]).As<TerrainLayer>();
                any |= layers[i] != null;
            }

            if (!any)
            {
                return;
            }

            Texture2D? first = null;
            for (var i = 0; i < layers.Length; i++)
            {
                var albedo = layers[i]?.diffuseTexture;
                if (albedo == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = albedo;
                }
                else if (albedo.width != first.width || albedo.height != first.height || albedo.format != first.format)
                {
                    _slotLog.Warn(LayerSlotIds[i], "la capa no entra en el Texture2DArray (" + albedo.width + " × " + albedo.height + " " + albedo.format
                        + " contra " + first.width + " × " + first.height + " " + first.format + "): se usa el procedural");
                    return;
                }
            }

            if (first == null)
            {
                return;
            }

            var albedoArray = new Texture2DArray(first.width, first.height, LayerSlotIds.Length, first.format, first.mipmapCount > 1)
            {
                name = "scene-terrain-layers",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
            };
            var normalArray = new Texture2DArray(first.width, first.height, LayerSlotIds.Length, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
            {
                name = "scene-terrain-layers-normal",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
            };
            try
            {
                var white = new Color32[first.width * first.height];
                for (var p = 0; p < white.Length; p++)
                {
                    white[p] = new Color32(128, 128, 255, 255);
                }

                for (var i = 0; i < layers.Length; i++)
                {
                    var albedo = layers[i]?.diffuseTexture ?? first;
                    for (var mip = 0; mip < albedoArray.mipmapCount; mip++)
                    {
                        Graphics.CopyTexture(albedo, 0, mip, albedoArray, i, mip);
                    }

                    normalArray.SetPixels32(white, i, 0);
                }

                normalArray.Apply(false, false);
                world.LayerAlbedo = world.Own(albedoArray);
                world.LayerNormal = world.Own(normalArray);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, SlotResolver.Tag, "las capas del terreno no se pudieron copiar: se usa el procedural", error);
                MeshUpload.Destroy(albedoArray);
                MeshUpload.Destroy(normalArray);
            }
        }

        // La malla en anillos sin los triángulos que quedan enteros detrás de la cámara (z < −10 m): la cámara del contrato mira siempre hacia
        // adelante (el vuelo arranca a z −8,5 mirando al remero y la toma aérea retrocede 1,7 m), así esa mitad nunca se ve y solo sumaba
        // triángulos al presupuesto (SceneAdaptationTests). La malla se corre con el ancla de la grilla hacia atrás (z del mundo ≤ z de la
        // malla), así un triángulo con z de malla < −10 está siempre detrás.
        public const double WaterCullBehindZ = -10;

        // Medio usa la malla de 1 m de Bajo (IB6b-14): con la de 0,5 m el presupuesto de triángulos de Medio con el maniquí (170 k,
        // SceneAdaptationTests) no entraba. El detalle de cerca lo pone el fragmento (el mapa de normales); el vértice lleva lo suave.
        public static QualityTier WaterMeshTier(QualityTier tier)
        {
            return tier == QualityTier.High ? QualityTier.High : QualityTier.Low;
        }

        internal static MeshData ForwardOnly(MeshData source)
        {
            var kept = new MeshData(source.VertexCount, source.IndexCount);
            var p = source.Positions;
            var n = source.Normals;
            var c = source.Colors;
            var uv = source.Uv;
            for (var i = 0; i < source.VertexCount; i++)
            {
                kept.AddVertex(p[i * 3], p[i * 3 + 1], p[i * 3 + 2], n[i * 3], n[i * 3 + 1], n[i * 3 + 2], c[i * 4], c[i * 4 + 1], c[i * 4 + 2], c[i * 4 + 3],
                    uv[i * 2], uv[i * 2 + 1]);
            }

            var idx = source.Indices;
            for (var t = 0; t + 2 < source.IndexCount; t += 3)
            {
                var a = idx[t];
                var b = idx[t + 1];
                var d = idx[t + 2];
                if (p[a * 3 + 2] < WaterCullBehindZ && p[b * 3 + 2] < WaterCullBehindZ && p[d * 3 + 2] < WaterCullBehindZ)
                {
                    continue;
                }

                kept.AddTriangle(a, b, d);
            }

            return kept;
        }

        // El mapa de normales procedural de respaldo (sin el slot horneado): chico, para que la unidad sea liviana.
        public const int FallbackNormalSize = 64;

        internal static Texture2D ProceduralWaterNormal(int size)
        {
            var bytes = new byte[WaterNormalMap.ByteCount(size)];
            WaterNormalMap.Fill(size, WaterNormalMap.DefaultSeed, bytes);
            var texture = new Texture2D(size, size, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.MipChain)
            {
                name = "scene-water-normal",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixelData(bytes, 0);
            texture.Apply(true, false);
            return texture;
        }

        // Una copia de un material que puede faltar (un SceneWorld.asset de antes de la 6b: el shader por nombre; sin shader, null).
        private static Material? CopyOptional(SceneWorld world, Material? source, string shaderName, string name)
        {
            Material copy;
            if (source != null)
            {
                copy = new Material(source);
            }
            else
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    return null;
                }

                copy = new Material(shader);
            }

            copy.name = "scene-" + name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            return world.Own(copy);
        }

        private static Material Copy(SceneWorld world, Material? source, string name)
        {
            Material copy;
            if (source != null)
            {
                copy = new Material(source);
            }
            else
            {
                var shader = Shader.Find("RowMate/Scene/" + name.Replace("HorizonFar", "Horizon").Replace("HorizonNear", "Horizon").Replace("PropWindow", "Prop"))
                             ?? throw new InvalidOperationException("SceneWorldBuilder: falta el material " + name + " (correr RowMate/Bootstrap/Reaplicar)");
                copy = new Material(shader);
            }

            copy.name = "scene-" + name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            return world.Own(copy);
        }
    }
}
