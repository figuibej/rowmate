using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.Scene
{
    // SCP · Las sondas de WP-B0 como regresión (06a-escena-lago/02-tech-design §3.2; DT6a-05; ADR 022; QA-G6aT-09):
    // S6a-1 el color de la RT sRGB de la escena en la RT gamma de la UI (y el vidrio del HUD sobre blanco, #2A2F37); S6a-2 el dibujo a
    // pedido con post, sombras e instanciado; S6a-5 la RT escalada sin bloques; S6a-7 cero cuadros de atraso, también sin cambios del HUD.
    [Category("GPU")]
    public sealed class SceneCompositionTests : SceneTestBase
    {
        private static readonly string[] Probes = { "#0B4FA8", "#FFFFFF", "#7F7F7F", "#06122A" };

        // S6a-1: cuatro quads sin luz (URP Unlit) delante de la cámara, sin post: el hex en la RT de la escena y en la RT de la UI ± 1.
        [UnityTest, Category("AC-WKT-17-13"), Category("AC-SCN-01-02")]
        public IEnumerator SceneColorReachesTheUiUnchanged()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.V390);
            yield return EnterFree();
            yield return SceneAlone(920, 12);
            var host = SceneHost;
            var data = host.World!.Rig!.Data;
            data.renderPostProcessing = false;
            var quads = new GameObject[Probes.Length];
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(shader, Is.Not.Null);
            try
            {
                for (var i = 0; i < Probes.Length; i++)
                {
                    ColorUtility.TryParseHtmlString(Probes[i], out var color);
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.layer = host.World.Layer;
                    quad.transform.SetParent(host.World.Root.transform, false);
                    // En la cinta, delante de la cámara del contrato (2,2 m de alto), a 3 m: una columna por color.
                    quad.transform.localPosition = new Vector3(-0.75f + i * 0.5f, 2.2f, 3f);
                    quad.transform.localScale = new Vector3(0.45f, 1.2f, 1f);
                    var material = new Material(shader);
                    material.SetColor("_BaseColor", color);
                    quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                    quads[i] = quad;
                }

                H.Step();
                yield return Frames(3);
                var ui = Read();
                var scene = RtReader.Read(host.Output!);
                Dump(ui, "s6a1-ui");
                for (var i = 0; i < Probes.Length; i++)
                {
                    // El centro de la columna i en dp (V390, densidad 1): x = 195 + (−0,75 + 0,5·i)·390/3, y = el horizonte.
                    var x = (int)Math.Round(195 + (-0.75 + 0.5 * i) * 390 / 3.0);
                    var y = (int)Math.Round(new SceneCamera(390, 844).HorizonY);
                    var want = Hex(Probes[i]);
                    ExpectClose(At(ui, x, y), want, 1, "UI " + Probes[i]);
                    ExpectClose(RtReader.PixelTopLeft(scene, x * scene.width / 390, y * scene.height / 844), want, 1, "escena " + Probes[i]);
                }

                Destroy(ui);
                Destroy(scene);
            }
            finally
            {
                data.renderPostProcessing = true;
                foreach (var quad in quads)
                {
                    if (quad != null)
                    {
                        Destroy(quad.GetComponent<MeshRenderer>().sharedMaterial);
                        Destroy(quad);
                    }
                }
            }
        }

        // S6a-2: el pedido de render (SubmitRenderRequest) dibuja con el Volume (el post cambia la imagen), con el instanciado (las 80
        // estrellas de las 22 h) y sin el respaldo de Camera.Render.
        [UnityTest, Category("AC-SCN-11-03")]
        public IEnumerator TheRenderRequestDrawsPostAndInstances()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.V390);
            yield return EnterFree();
            yield return SceneAlone(920, 22);
            var host = SceneHost;
            Assert.That(host.World!.Renderer!.LastUsedSubmit, Is.True, "SubmitRenderRequest, no el respaldo");
            Assert.That(host.World.Director!.Sky.StarsDrawn, Is.EqualTo(StarField.Count), "las 80 estrellas a las 22 h");
            var withPost = RtReader.Read(host.Output!);
            var raw = new RenderTexture(host.Output!.descriptor);
            host.RenderWithoutPostForTest(raw);
            var without = RtReader.Read(raw);
            var differ = 0;
            for (var y = 0; y < withPost.height; y += 8)
            {
                for (var x = 0; x < withPost.width; x += 8)
                {
                    var a = withPost.GetPixel(x, y);
                    var b = without.GetPixel(x, y);
                    if (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 0.02f)
                    {
                        differ++;
                    }
                }
            }

            Assert.That(differ, Is.GreaterThan(10), "el Volume de la hora llega al dibujo a pedido");
            // Una estrella brilla: algún píxel del cielo bastante más claro que su vecino de 3 px.
            var star = 0;
            for (var y = 4; y < withPost.height / 3; y++)
            {
                for (var x = 4; x < withPost.width - 4; x++)
                {
                    var c = withPost.GetPixel(x, withPost.height - 1 - y);
                    var n = withPost.GetPixel(x - 3, withPost.height - 1 - y);
                    if (c.grayscale - n.grayscale > 0.2f)
                    {
                        star++;
                    }
                }
            }

            Assert.That(star, Is.GreaterThan(0), "las estrellas instanciadas se dibujan");
            Destroy(withPost);
            Destroy(without);
            raw.Release();
            Destroy(raw);
        }

        // S6a-5: con 0,85 la RT mide 0,85 × la ventana con filtro bilineal; con 1, punto.
        [UnityTest, Category("AC-SCN-01-13")]
        public IEnumerator TheScaledTargetUsesBilinear()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.TV);
            yield return EnterFree();
            var host = SceneHost;
            Assert.That(host.Output!.filterMode, Is.EqualTo(FilterMode.Point), "Alto: a tamaño completo, de punto");
            Assert.That(host.Output.width, Is.EqualTo(DeviceProfile.TV.WidthPx));
            var generation = host.Target.Generation;
            host.SetResolution(0.85);
            H.Step();
            yield return Frames();
            Assert.That(host.Target.Generation, Is.EqualTo(generation + 1), "la RT se rehace una vez");
            Assert.That(host.Output!.width, Is.EqualTo((int)Math.Round(DeviceProfile.TV.WidthPx * 0.85)));
            Assert.That(host.Output.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(H.Immersive!.View.SceneSurface!.Shown, Is.SameAs(host.Output), "scene-surface muestra la RT nueva");
            H.Step();
            Assert.That(host.Target.Generation, Is.EqualTo(generation + 1), "no se rehace en cada cuadro");
        }

        // S6a-7: el número de cuadro pintado en la esquina de la RT de la escena se lee en la RT de la UI en el mismo cuadro, 120 cuadros
        // seguidos, también en pausa (el HUD quieto).
        [UnityTest, Category("AC-SCN-01-02")]
        public IEnumerator TheUiShowsTheSceneOfTheSameFrame()
        {
            AssumeGpu();
            CreateScene(DeviceProfile.V390);
            yield return EnterFree();
            yield return Tap("hud-pause");
            yield return Settle();
            var host = SceneHost;
            H.Immersive!.View.HideForCapture();
            host.StampFrameForTest(true);
            var mismatches = 0;
            var checkedFrames = 0;
            for (var i = 0; i < 120; i++)
            {
                H.Step();
                yield return null;
                var stamp = host.StampedFrameForTest;
                var top = H.ReadPixels(new Rect(0, 0, 4, 4));
                var bottom = H.ReadPixels(new Rect(0, DeviceProfile.V390.HeightPx - 4, 4, 4));
                var a = (Color32)top.GetPixel(1, 1);
                var b = (Color32)bottom.GetPixel(1, 1);
                var want = (byte)(stamp & 0xFF);
                checkedFrames++;
                if (a.r != want && b.r != want)
                {
                    mismatches++;
                }

                Destroy(top);
                Destroy(bottom);
            }

            host.StampFrameForTest(false);
            Assert.That(checkedFrames, Is.EqualTo(120));
            Assert.That(mismatches, Is.EqualTo(0), "cuadros en que la UI mostró una escena vieja");
        }

        private static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        private static void ExpectClose(Color32 got, Color32 want, int tolerance, string what)
        {
            Assert.That(Math.Abs(got.r - want.r), Is.LessThanOrEqualTo(tolerance), what + ": " + got + " contra " + want);
            Assert.That(Math.Abs(got.g - want.g), Is.LessThanOrEqualTo(tolerance), what + ": " + got + " contra " + want);
            Assert.That(Math.Abs(got.b - want.b), Is.LessThanOrEqualTo(tolerance), what + ": " + got + " contra " + want);
        }
    }
}
