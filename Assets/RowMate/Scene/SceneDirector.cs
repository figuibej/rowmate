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

        public ShoreStreamer Shore { get; }

        public FillStreamer Fill { get; }

        public BoatVisual Boat { get; }

        public ScreenOverlay Overlay { get; }

        public GameObject Root { get; }

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
        }

        // Los dibujos instanciados de este cuadro (justo antes del pedido de render).
        public void QueueInstancedDraws()
        {
            Sky.Draw();
            Shore.Draw();
            Fill.Draw();
        }
    }
}
