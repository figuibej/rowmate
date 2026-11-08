using System;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Las luces de la noche de río, costa y regata (08-entornos-selector/02-tech-design §3.3 "Luces"; 04-design-notes §5; AC-SCN-20-04;
    // DT8-11; WP-D1): un dibujo instanciado del shader Glow sobre el quad de la escena, con los objetos visibles del cuadro y su nivel de
    // luz (NightLights, con su minuto y su fundido de 0,4 s, DV-60):
    //   farol → el halo del globo (r 1,5 m, #FFE082 al 35 %), torre de llegada → el halo de la sala de control (r 2,2 m al 25 %),
    //   faro → el halo de la linterna (r 3 m, #FFF59D al 35 %, con el destello) y, a menos de 400 m, los dos medios haces de 60 m que
    //   giran cada 12 s de escena (el shader los gira con _RmSceneTime: siguen en pausa).
    // Lo de día (nivel 0) no se dibuja. El lago no tiene: el lote queda vacío y no hay dibujo. Sin asignar: el lote se reserva una vez.
    public sealed class GlowRenderer
    {
        public const int Capacity = 96;
        private const double MinLevel = 1e-3;
        private const double TowerHaloRadius = 2.2, TowerHaloAlpha = 0.25, TowerHaloY = 8.4;

        private readonly InstanceBatch _batch = new InstanceBatch(SceneShaderIds.GlowInstance, -1, -1, -1, Capacity);
        private readonly Mesh _quad;
        private readonly Material _material;
        private readonly Camera _camera;
        private readonly int _layer;
        private readonly Bounds _bounds = new Bounds(new Vector3(0, 20, 250), new Vector3(400, 80, 1000));
        private EnvironmentId _environment = EnvironmentId.Lake;

        public GlowRenderer(Mesh quad, Material material, Camera camera, int layer)
        {
            _quad = quad ?? throw new ArgumentNullException(nameof(quad));
            _material = material ?? throw new ArgumentNullException(nameof(material));
            _camera = camera;
            _layer = layer;
        }

        // Cuántas luces van en este cuadro (pruebas y presupuesto).
        public int Count => _batch.Count;

        // La miniatura (12 h) y las tomas sin luces no las necesitan; las pruebas pueden apagarlas.
        public bool Enabled { get; set; } = true;

        // El haz del faro (en Bajo se apaga primero si falta margen, 02-tech-design §5): lo pone SceneHost por el nivel.
        public bool Beam { get; set; } = true;

        public void SetEnvironment(EnvironmentId environment)
        {
            _environment = environment;
            _batch.Clear();
        }

        public void Apply(in SceneFrame frame)
        {
            _batch.Clear();
            if (!Enabled || _environment == EnvironmentId.Lake)
            {
                return;
            }

            var props = frame.VisibleProps;
            var light = frame.PropLight;
            var d = frame.Distance;
            for (var i = 0; i < props.Count && i < light.Length; i++)
            {
                var level = light[i];
                if (level < MinLevel)
                {
                    continue;
                }

                var p = props[i];
                var relZ = p.Z - d;
                switch (p.Kind)
                {
                    case PropKind.Lamp:
                        Add(p.X, PropPlacement.FootY(_environment, p) + RiverArt.LampGlobeY, relZ, RiverArt.LampHaloRadius,
                            RiverArt.LampHaloAlpha * level, 0, 0);
                        break;
                    case PropKind.FinishTower:
                        Add(p.X, PropPlacement.FootY(_environment, p) + TowerHaloY, relZ, TowerHaloRadius, TowerHaloAlpha * level, 0, 0);
                        break;
                    case PropKind.Lighthouse:
                    {
                        var y = CoastArt.Promontory + CoastArt.LighthouseHaloY;
                        Add(p.X, y, relZ, CoastArt.LighthouseHaloRadius, CoastArt.LighthouseHaloAlpha * level, 1, 0);
                        if (Beam && relZ < CoastArt.BeamRange)
                        {
                            var beam = CoastArt.BeamAlpha * level;
                            Add(p.X, y, relZ, CoastArt.BeamLength, beam, 2, 0);
                            Add(p.X, y, relZ, CoastArt.BeamLength, beam, 2, Math.PI);
                        }

                        break;
                    }
                }
            }
        }

        // Las sondas del issue #9 (Snapshots/EnvironmentMarks): la pasada sin post mide la linterna sin el halo aditivo encima.
        internal bool SuppressDrawForTest { get; set; }

        public void Draw()
        {
            if (SuppressDrawForTest || _batch.Count == 0)
            {
                return;
            }

            _batch.Draw(_quad, 0, _material, _camera, _layer, ShadowCastingMode.Off, _bounds);
        }

        private void Add(double x, double y, double relZ, double size, double alpha, int type, double phase)
        {
            if (relZ < -size)
            {
                return;
            }

            var matrix = Matrix4x4.Translate(new Vector3((float)x, (float)y, (float)relZ));
            _batch.Add(matrix, new Vector4((float)size, (float)alpha, type, (float)phase), Vector4.zero, 0, 0);
        }
    }
}
