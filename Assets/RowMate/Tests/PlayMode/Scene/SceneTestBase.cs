using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Scene;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Tests.PlayMode.Scene
{
    // Base de las pruebas de la escena (06a-escena-lago/02-tech-design §6.3, PlayMode B1 y B2): un anfitrión con SceneMode.Real (la
    // escena de verdad, la compilación de shaders sincrónica mientras dura, la orilla en el acto y la escena en pasos de 1/60 s), el
    // remo de prueba con R-4 y el reloj de prueba de la It. 4a. Las que dibujan y leen píxeles van con Category("GPU") y suponen un
    // dispositivo gráfico (en -nographics no hay: Inconclusive). Con ROWMATE_SCENE_DUMP, las lecturas se guardan como PNG en esa
    // carpeta (solo para revisar a mano; nunca se versionan).
    public abstract class SceneTestBase : WorkoutUiTestBase
    {
        protected SceneHost SceneHost => H.Scene ?? throw new InvalidOperationException("sin escena (SceneMode.Real)");

        protected UiTestHost CreateScene(DeviceProfile profile, Action<UiHostOptions>? configure = null)
        {
            return CreateWorkout(null, o =>
            {
                o.Scene = SceneMode.Real;
                o.Profile = profile;
                o.IsDesktop = !profile.IsPhone;
                configure?.Invoke(o);
            });
        }

        protected static void AssumeGpu()
        {
            Assume.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "sin GPU (-nographics)");
            // QA-V6a-REN-15: con un rasterizador por software (el servidor X virtual del CI en Linux) las sondas de color y los conteos de
            // SetPass no valen: la prueba no corre (el CI además las saca con -testCategory "!GPU").
            var name = SystemInfo.graphicsDeviceName ?? "";
            foreach (var software in new[] { "llvmpipe", "softpipe", "SwiftShader", "Microsoft Basic Render" })
            {
                if (name.IndexOf(software, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Assert.Ignore("GPU por software (" + software + "): las sondas de color y los conteos de la escena no valen");
                }
            }
        }

        // Entra al inmersivo libre y espera el vuelo entero (la escena a la vista, en la pose del contrato).
        protected IEnumerator EnterFree()
        {
            yield return StartFree();
            yield return AdvanceTo(2.5);
            yield return Settle();
        }

        // La escena sola de las tomas (§5.1): HUD oculto, sin degradés, respiración en 0, tiempo de escena fijo, distancia y hora.
        protected IEnumerator SceneAlone(double distance, double hour, double sceneTime = 3.2)
        {
            var host = SceneHost;
            host.ForceDistanceForTest(distance);
            host.ForceHourForTest(hour);
            host.FreezeSceneTimeForTest(sceneTime);
            host.BreathingOffForTest(true);
            host.SetScrimsForTest(false);
            H.Immersive?.View.HideForCapture();
            H.Step();
            H.Step();
            yield return Frames(3);
        }

        protected Texture2D Read()
        {
            return H.ReadPixels();
        }

        protected static void Dump(Texture2D frame, string name)
        {
            var folder = Environment.GetEnvironmentVariable("ROWMATE_SCENE_DUMP");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), frame.EncodeToPNG());
        }

        protected static Color32 At(Texture2D frame, int xTopLeft, int yTopLeft)
        {
            return RtReader.PixelTopLeft(frame, xTopLeft, yTopLeft);
        }

        protected static void Destroy(UnityEngine.Object? obj)
        {
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }
}
