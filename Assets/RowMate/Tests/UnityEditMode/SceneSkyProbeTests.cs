using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Tests.UnityEditMode
{
    // SKP · El shader Sky, dibujado en la GPU, da las sondas de contrato del cielo (06a-escena-lago/04-design-notes §10.4; DN6a-09;
    // AC-SCN-11-02; WP-D1): en V390 a las 12 h, con la cámara del contrato y sin el ajuste de la hora (la RT de la escena, sin post),
    // arriba (195; 8) `#0B4FA8` ± 3 y a media altura (195; 168,8) `#2C70BE` ± 3 por canal: el telón de SkyBackdrop.Gradient mezclado
    // en sRGB como Color.lerp y pasado a lineal exacto (en lineal daría `#4B82C2`). Además, debajo del horizonte la bruma
    // (lerp(skyHorizon; fog; 0,5)). Se dibuja un quad con un CommandBuffer en una RT sRGB del tamaño de la ventana, con las matrices de
    // Core (ChaseCameraProjection) y los globales que pondrá SceneLighting (WP-B1). Necesita GPU.
    [Category("GPU")]
    public sealed class SceneSkyProbeTests
    {
        private const int Width = 390;
        private const int Height = 844;

        [Test]
        public void TheSkyShaderHitsTheContractProbesAtNoon()
        {
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "sin GPU (-nographics)");
            var shader = Shader.Find("RowMate/Scene/Sky");
            Assert.That(shader, Is.Not.Null);
            var cam = new SceneCamera(Width, Height);
            var palette = SceneTimeOfDay.PaletteFor(12);
            var haze = SceneColor.Lerp(palette.SkyHorizon, palette.Fog, 0.5);

            var material = new Material(shader);
            var rt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var mesh = Quad();
            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            var cmd = new CommandBuffer();
            try
            {
                var pose = new CameraPose(0, SceneCamera.CamHeight, 0, 0, 0, 0, cam.Focal, cam.HorizonY);
                var view = ToUnity(ChaseCameraProjection.View(pose));
                var projection = ToUnity(ChaseCameraProjection.Contract(cam));
                cmd.SetRenderTarget(rt);
                cmd.ClearRenderTarget(true, true, Color.magenta);
                cmd.SetGlobalMatrix("unity_MatrixInvVP", (projection * view).inverse);
                cmd.SetGlobalVector("_ProjectionParams", new Vector4(1, 0.5f, 2000, 1f / 2000));
                cmd.SetGlobalVector("_WorldSpaceCameraPos", new Vector4(0, (float)SceneCamera.CamHeight, 0, 0));
                cmd.SetGlobalVector("_RmSkyTopSrgb", Srgb(palette.SkyTop));
                cmd.SetGlobalVector("_RmSkyHorizonSrgb", Srgb(palette.SkyHorizon));
                cmd.SetGlobalVector("_RmHazeSrgb", Srgb(haze));
                cmd.SetGlobalFloat("_RmHorizonY", (float)cam.HorizonY);
                cmd.SetGlobalFloat("_RmFocalDp", (float)cam.Focal);
                cmd.SetGlobalVector("_RmViewDp", new Vector4(Width, Height, 0, 0));
                cmd.SetGlobalFloat("_RmGolden", 0);
                cmd.SetGlobalFloat("_RmAmbient", (float)palette.Ambient);
                // El sol de las 12 h (acimut 0, 12°): lejos de las dos sondas (más allá del halo de 6,4°).
                SceneMath.Direction(0, 12, out var dx, out var dy, out var dz);
                cmd.SetGlobalVector("_RmDiskDir", new Vector4((float)dx, (float)dy, (float)dz, 0));
                cmd.SetGlobalVector("_RmDiskParams", new Vector4(1, 0, 0.94f, 0));
                cmd.SetGlobalVector("_RmDiskShape", new Vector4((float)CelestialPlacement.DiskRadiusRad(false), (float)CelestialPlacement.HaloRadiusRad(false), 1, 8));
                cmd.DrawMesh(mesh, Matrix4x4.identity, material, 0, 0);
                Graphics.ExecuteCommandBuffer(cmd);

                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                read.Apply();
                RenderTexture.active = previous;

                // La fila de la ventana (y desde arriba, en dp = px) en la textura leída: la orientación sale de la imagen (arriba el
                // azul de skyTop, abajo la bruma clara), no de la convención de la plataforma.
                var pixels = read.GetPixels32();
                Color32 Row(int row) => pixels[row * Width + Width / 2];
                var flipped = Luminance(Row(Height - 1)) > Luminance(Row(0));
                Color32 At(int yFromTop) => Row(flipped ? yFromTop : Height - 1 - yFromTop);

                Expect(At(8), SkyBackdrop.Color(palette, Fraction(cam, 8)), "arriba (195; 8)");
                Expect(At(168), SkyBackdrop.Color(palette, Fraction(cam, 168)), "media altura (195; 168,8)");
                Assert.That(SkyBackdrop.Color(palette, Fraction(cam, 168)).ToHex(), Is.EqualTo("#2C70BE"), "la sonda de 04-design-notes §10.4");
                Expect(At(600), haze, "debajo del horizonte, la bruma");
            }
            finally
            {
                cmd.Release();
                UnityEngine.Object.DestroyImmediate(read);
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(material);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        // q del centro del píxel de la fila y (desde arriba) en la columna del centro.
        private static double Fraction(in SceneCamera cam, int y)
        {
            var tau = (cam.HorizonY - (y + 0.5)) / cam.Focal;
            return SkyBackdrop.Fraction(tau, cam.HorizonY / cam.Focal);
        }

        private static void Expect(Color32 got, in SceneColor want, string where)
        {
            var w = new[] { want.R * 255, want.G * 255, want.B * 255 };
            var g = new[] { (double)got.r, got.g, got.b };
            for (var c = 0; c < 3; c++)
            {
                Assert.That(g[c], Is.EqualTo(w[c]).Within(3.0), where + ": canal " + c + " (" + got + " contra " + want.ToHex() + ")");
            }
        }

        private static float Luminance(Color32 c)
        {
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        private static Vector4 Srgb(in SceneColor c)
        {
            return new Vector4((float)c.R, (float)c.G, (float)c.B, 1);
        }

        private static Matrix4x4 ToUnity(in SceneMatrix4 m)
        {
            var u = new Matrix4x4();
            for (var r = 0; r < 4; r++)
            {
                for (var c = 0; c < 4; c++)
                {
                    u[r, c] = (float)m[r, c];
                }
            }

            return u;
        }

        // Un quad con la UV0 de 0 a 1 (el shader arma el clip con la UV).
        private static Mesh Quad()
        {
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(1, 1, 0), new Vector3(-1, 1, 0) },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
                bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f),
            };
            return mesh;
        }
    }
}
