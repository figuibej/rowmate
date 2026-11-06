using UnityEngine;

namespace RowMate.Scene
{
    // Las dos cordilleras del lago (06a-escena-lago/02-tech-design §2.9, §3.6 "Cordilleras"; Q-65, DV-55, DV-64, DV-65): dos tiras de
    // HorizonStripMesh (260 columnas de −65° a +65°) centradas en la cámara (sin paralaje lateral), en la cola 1003. Por cuadro, el color
    // de la hora (HorizonModel, exacto en Core) y el alto: tan Θ (mín(0,12·alto/focal; tan 9°)) × k(d) (se acercan sin correrse) × el
    // alto relativo (1 la lejana, 0,7 la cercana).
    public sealed class HorizonRenderer
    {
        private readonly Material _far;
        private readonly Material _near;

        public HorizonRenderer(Transform parent, int layer, Mesh farMesh, Mesh nearMesh, Material far, Material near)
        {
            _far = far;
            _near = near;
            Far = SceneObjects.Create("scene-horizon-far", parent, layer, farMesh, far, false, false);
            Near = SceneObjects.Create("scene-horizon-near", parent, layer, nearMesh, near, false, false);
        }

        public MeshRenderer Far { get; }

        public MeshRenderer Near { get; }

        public void Apply(in RowMate.Core.Scene.SceneFrame frame)
        {
            _far.SetColor(SceneShaderIds.HorizonTone, SceneLighting.Srgb(frame.HorizonFar));
            _near.SetColor(SceneShaderIds.HorizonTone, SceneLighting.Srgb(frame.HorizonNear));
            var tan = (float)frame.HorizonPeakTan;
            _far.SetVector(SceneShaderIds.HorizonScale, new Vector4(tan, (float)frame.HorizonFarScale, 1f, 0));
            _near.SetVector(SceneShaderIds.HorizonScale,
                new Vector4(tan, (float)frame.HorizonNearScale, (float)RowMate.Core.Scene.Art.LakeArt.NearHeightScale, 0));
        }
    }

    // El agua degradada (06a-escena-lago/02-tech-design §2.12, §3.6 "Agua"; AC-SCN-13-02, -03; DT6a-07; DV-59): un plano de
    // 1,6 × 1,5 km centrado en la cámara (cinta), la grilla de WaterArt (los agregados se pesan por vértice); por cuadro, los tres tramos
    // y las 10 líneas del legacy (la fase en double desde Core, la x del disco, la fuerza del legacy y la visibilidad del astro).
    public sealed class WaterSurface
    {
        private readonly Material _material;

        public WaterSurface(Transform parent, int layer, Mesh grid, Material material)
        {
            _material = material;
            Renderer = SceneObjects.Create("scene-water", parent, layer, grid, material, false, true);
        }

        public MeshRenderer Renderer { get; }

        public void Apply(in RowMate.Core.Scene.SceneFrame frame)
        {
            _material.SetColor(SceneShaderIds.WaterBandTop, SceneLighting.Srgb(frame.WaterTop));
            _material.SetColor(SceneShaderIds.WaterBandMid, SceneLighting.Srgb(frame.WaterMid));
            _material.SetColor(SceneShaderIds.WaterBandBottom, SceneLighting.Srgb(frame.WaterBottom));
            _material.SetVector(SceneShaderIds.Shimmer,
                new Vector4((float)frame.WaterPhase, (float)frame.ShimmerCenterX, (float)frame.Celestial.LegacyStrength, (float)frame.ShimmerVisibility));
            _material.SetColor(SceneShaderIds.ShimmerColor, SceneLighting.Srgb(frame.Palette.SunColor));
        }

        // La grilla del plano (WaterArt.GridX × GridZ) en metros de cinta, con la normal hacia arriba.
        public static Mesh BuildGrid()
        {
            var xs = RowMate.Core.Scene.Art.WaterArt.GridX;
            var zs = RowMate.Core.Scene.Art.WaterArt.GridZ;
            var vertices = new Vector3[xs.Length * zs.Length];
            var normals = new Vector3[vertices.Length];
            for (var j = 0; j < zs.Length; j++)
            {
                for (var i = 0; i < xs.Length; i++)
                {
                    vertices[j * xs.Length + i] = new Vector3((float)xs[i], 0, (float)zs[j]);
                    normals[j * xs.Length + i] = Vector3.up;
                }
            }

            var triangles = new int[(xs.Length - 1) * (zs.Length - 1) * 6];
            var t = 0;
            for (var j = 0; j < zs.Length - 1; j++)
            {
                for (var i = 0; i < xs.Length - 1; i++)
                {
                    var a = j * xs.Length + i;
                    var b = a + 1;
                    var c = a + xs.Length;
                    var d = c + 1;
                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            var mesh = new Mesh { name = "scene-water-grid", hideFlags = HideFlags.HideAndDontSave, vertices = vertices, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // El bote provisorio (06a-escena-lago/02-tech-design §3.5, §3.6 "Bote"; AC-SCN-01-08; 04-design-notes §9, §8.3): el casco de
    // HullMesh en la cinta (z 5,3 a 13,5, quieto respecto de la cámara del contrato) y su sombra ovalada sobre el agua en el centro del
    // casco, del lado opuesto al astro (la arma el shader BoatShadow).
    public sealed class BoatVisual
    {
        public const float HullCenterZ = (float)((RowMate.Core.Scene.Art.LakeArt.HullStern + RowMate.Core.Scene.Art.LakeArt.HullBow) / 2);

        public BoatVisual(Transform parent, int layer, Mesh hull, Material hullMaterial, Mesh quad, Material shadowMaterial)
        {
            Hull = SceneObjects.Create("scene-boat", parent, layer, hull, hullMaterial, true, true);
            Shadow = SceneObjects.Create("scene-boat-shadow", parent, layer, quad, shadowMaterial, false, false);
            Shadow.transform.localPosition = new Vector3(0, 0.004f, HullCenterZ);
        }

        public MeshRenderer Hull { get; }

        public MeshRenderer Shadow { get; }
    }

    // Los degradés de pantalla (06a-escena-lago/02-tech-design §3.3, §3.6; DV-67, C-18): de la capa del HUD, dibujados por la cámara de
    // la escena en la cola 3500, antes del post (así los alcanza el velo), con 0 elementos de UI. Apagados en la escena sola
    // (SetScrimsForTest(false): las tomas CAP-01 a -12 y la pasada de contrato).
    public sealed class ScreenOverlay
    {
        public ScreenOverlay(Transform parent, int layer, Mesh quad, Material material)
        {
            Renderer = SceneObjects.Create("scene-overlay", parent, layer, quad, material, false, false);
        }

        public MeshRenderer Renderer { get; }

        public bool Enabled
        {
            get => Renderer.enabled;
            set => Renderer.enabled = value;
        }
    }
}
