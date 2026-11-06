using RowMate.Core.Scene;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // Un objeto de la escena con una malla y un material, en la capa de la escena y sin sombras salvo que se pidan.
    internal static class SceneObjects
    {
        // Las cosas del cielo y de la pantalla las pone el shader en otro lado: unos bounds enormes para que el culling nunca las corte.
        public static readonly Bounds Everywhere = new Bounds(Vector3.zero, Vector3.one * 100000f);

        public static MeshRenderer Create(string name, Transform parent, int layer, Mesh mesh, Material material, bool castShadows,
                                          bool receiveShadows)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = layer };
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
            return renderer;
        }
    }

    // El cielo (06a-escena-lago/02-tech-design §3.6 "Cielo", "Estrellas", "Nubes"; DV-63, DV-66, DN6a-09, QA-G6aT-08): el telón con el
    // disco y el halo (un quad de pantalla completa en la cola 1000), las 80 estrellas del legacy instanciadas en sus direcciones
    // (cola 1001, aditivas, enmascaradas por el astro en su shader) y las 7 nubes (cola 1002, con WrapFade) más la capa lejana en
    // Alto (una octava instancia con la variante −1). Las direcciones y los alfas los da Core (SceneFrame).
    public sealed class SkyRenderer
    {
        private readonly InstanceBatch _stars;
        private readonly InstanceBatch _clouds;
        private readonly Mesh _quad;
        private readonly Material _starsMaterial;
        private readonly Material _cloudsMaterial;
        private readonly Camera _camera;
        private readonly int _layer;

        public SkyRenderer(Transform parent, int layer, Camera camera, Mesh quad, Material sky, Material stars, Material clouds)
        {
            _camera = camera;
            _layer = layer;
            _quad = quad;
            _starsMaterial = stars;
            _cloudsMaterial = clouds;
            SkyObject = SceneObjects.Create("scene-sky", parent, layer, quad, sky, false, false);
            _stars = new InstanceBatch(SceneShaderIds.StarDir, -1, SceneShaderIds.StarAlpha);
            _clouds = new InstanceBatch(SceneShaderIds.CloudDir, SceneShaderIds.CloudAlpha, SceneShaderIds.CloudVariant);
        }

        public MeshRenderer SkyObject { get; }

        // Las estrellas a la vista (SetStarsForTest).
        public bool StarsEnabled { get; set; } = true;

        // La capa lejana de nubes (solo Alto).
        public bool CloudLayer { get; set; }

        public int StarsDrawn => _stars.Count;

        public void Apply(in SceneFrame frame)
        {
            _stars.Clear();
            if (StarsEnabled)
            {
                var stars = frame.Stars;
                for (var i = 0; i < stars.Length; i++)
                {
                    var s = stars[i];
                    if (s.Alpha <= 0)
                    {
                        continue;
                    }

                    _stars.Add(Matrix4x4.identity, new Vector4((float)s.DirX, (float)s.DirY, (float)s.DirZ, (float)s.RadiusRad), Vector4.zero, (float)s.Alpha, 0);
                }
            }

            _clouds.Clear();
            var clouds = frame.Clouds;
            for (var i = 0; i < clouds.Length; i++)
            {
                var c = clouds[i];
                _clouds.Add(Matrix4x4.identity, new Vector4((float)c.DirX, (float)c.DirY, (float)c.DirZ, (float)c.SizeRad),
                    new Vector4((float)c.BodyAlpha, (float)c.ShadeAlpha, 0, 0), c.Variant, 0);
            }

            if (CloudLayer && clouds.Length > 0)
            {
                // La capa lejana: la variante −1 (el vértice arma el plano de ± 65°), con el alfa del cuerpo del legacy.
                _clouds.Add(Matrix4x4.identity, new Vector4(0, 0, 1, 0), new Vector4((float)CloudModel.BodyAlpha(frame.Palette.Ambient), 0, 0, 0), -1, 0);
            }
        }

        public void Draw()
        {
            var bounds = SceneObjects.Everywhere;
            _stars.Draw(_quad, 0, _starsMaterial, _camera, _layer, ShadowCastingMode.Off, bounds);
            _clouds.Draw(_quad, 0, _cloudsMaterial, _camera, _layer, ShadowCastingMode.Off, bounds);
        }
    }
}
