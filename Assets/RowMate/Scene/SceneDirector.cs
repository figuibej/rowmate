using RowMate.Core.Scene;
using UnityEngine;

namespace RowMate.Scene
{
    // Aplica el SceneFrame de Core a la escena de Unity (06a-escena-lago/02-tech-design §3.1, §4.4): la cámara (pose y matriz), la luz,
    // los globales y el Volume, el cielo, las cordilleras, el agua, el terreno, los objetos del legacy y el relleno, y pide los dibujos
    // instanciados para la cámara de la escena (que SceneRenderer dibuja a pedido en el mismo cuadro). Un solo Apply por cuadro, sin
    // Update en cada objeto, sin asignaciones.
    public sealed class SceneDirector
    {
        public SceneDirector(ChaseCameraRig rig, SceneLighting lighting, SkyRenderer sky, HorizonRenderer horizon, WaterSurface water, TerrainStreamer terrain,
                             ShoreStreamer shore, FillStreamer fill, BoatVisual boat, ScreenOverlay overlay, GameObject root)
        {
            Rig = rig;
            Lighting = lighting;
            Sky = sky;
            Horizon = horizon;
            Water = water;
            Terrain = terrain;
            Shore = shore;
            Fill = fill;
            Boat = boat;
            Overlay = overlay;
            Root = root;
        }

        public ChaseCameraRig Rig { get; }

        public SceneLighting Lighting { get; }

        public SkyRenderer Sky { get; }

        public HorizonRenderer Horizon { get; }

        public WaterSurface Water { get; }

        public TerrainStreamer Terrain { get; }

        // It. 8 (AC-SCN-22-04, ThumbnailRule): la miniatura, sin bote, remero, remos, efectos ni boya.
        public bool HideBoat { get; set; }

        // It. 8: los objetos de río, costa y regata (null en un mundo de antes de la 8: las pruebas viejas).
        public KitPropRenderer? Kit { get; set; }

        // It. 8 (WP-D1): los halos y el haz de la noche de los escenarios y la espuma de la costa (null en un mundo de antes de la 8).
        public GlowRenderer? Glow { get; set; }

        public FoamRenderer? Foam { get; set; }

        // QA-V8-01: las gaviotas de la costa (null en un mundo de antes de la 8).
        public SeagullRenderer? Seagulls { get; set; }

        public ShoreStreamer Shore { get; }

        public FillStreamer Fill { get; }

        public BoatVisual Boat { get; }

        public ScreenOverlay Overlay { get; }

        public GameObject Root { get; }

        // It. 7 (07-remero-palada/02-tech-design §3.1): el remero (se cambia con SceneHost.ForceCharacterForTest), los remos, el agua que
        // responde, la salpicadura y la boya.
        public RowerCharacter? Rower { get; set; }

        public OarRig? Oars { get; set; }

        public WaterInteractionRenderer? WaterMarks { get; set; }

        public StrokeVfx? Spray { get; set; }

        public MarkerBuoy? Buoy { get; set; }

        // Con el óvalo (Bajo, o la luz sin fuerza de sombra: Medio de noche) el remero, los remos, los herrajes y el carro no proyectan: el
        // mapa no se vería y la pasada de sombras cuesta SetPass (B7-04). El casco de la 6a sigue como estaba.
        private bool? _casting;

        public void CastShadows(bool on)
        {
            if (_casting == on && !_rowerChanged)
            {
                return;
            }

            _casting = on;
            _rowerChanged = false;
            var mode = on ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            Boat.Fittings.shadowCastingMode = mode;
            Boat.Seat.shadowCastingMode = mode;
            Oars?.CastShadows(on);
            Rower?.CastShadows(on);
        }

        // El remero cambió (ForceCharacterForTest): el próximo Apply vuelve a poner sus sombras.
        public void RowerChanged()
        {
            _rowerChanged = true;
        }

        private bool _rowerChanged;

        // Cuántas veces se aplicó (pruebas).
        public long Applied { get; private set; }

        public void Apply(in SceneFrame frame)
        {
            Applied++;
            Rig.Apply(frame);
            Lighting.Apply(frame);
            Sky.Apply(frame);
            Horizon.Apply(frame);
            Water.Apply(frame);
            Terrain.Apply(frame.Distance);
            Shore.Apply(frame);
            Fill.Apply(frame.Distance, frame.Pose.Z);
            Kit?.Apply(frame);
            Glow?.Apply(frame);
            Foam?.Apply(frame);
            Seagulls?.Apply(frame);
            Boat.Apply(frame.Rower);
            if (Boat.Root.gameObject.activeSelf == HideBoat)
            {
                Boat.Root.gameObject.SetActive(!HideBoat);
            }

            CastShadows(frame.Rower.ShadowMode == RowMate.Core.Scene.Stroke.BoatShadowMode.Map);
            Oars?.Apply(frame.Rower.Pose);
            Rower?.Driver.Apply(frame.Rower.Pose);
            WaterMarks?.Apply(frame);
            Spray?.Apply(frame);
            Buoy?.Apply(frame);
            if (HideBoat && Buoy != null)
            {
                Buoy.Renderer.enabled = false;
            }
        }

        // Los dibujos instanciados de este cuadro (justo antes del pedido de render).
        public void QueueInstancedDraws()
        {
            Sky.Draw();
            Shore.Draw();
            Fill.Draw();
            Kit?.Draw();
            Glow?.Draw();
            Foam?.Draw();
            Seagulls?.Draw();
            if (!HideBoat)
            {
                WaterMarks?.Draw();
                Spray?.Draw();
            }
        }
    }
}
