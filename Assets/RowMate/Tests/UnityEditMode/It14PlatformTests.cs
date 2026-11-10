using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.EditorTools;
using RowMate.Platform.Display;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // It. 14 (14-pulido-plataformas/02-tech-design §2.4, §3, DT14-09, DT14-14, DT14-15; AC-APP-16-08, -11, AC-APP-15-05 a -09): el paso
    // identity del bootstrapper, la importación de los íconos, la subclase del WndProc (con un IWndProcApi falso), F11 / Alt+Enter (con un
    // IWindowFrame falso) y el post-proceso de iOS sobre carpetas temporales.
    public sealed class IdentityStepTests
    {
        [Test, Category("AC-APP-16-08")]
        public void StandaloneIconsForEverySize()
        {
            var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Application);
            Assert.That(sizes, Is.Not.Empty);
            Assert.That(icons.Length, Is.EqualTo(sizes.Length));
            for (var i = 0; i < sizes.Length; i++)
            {
                Assert.That(icons[i], Is.Not.Null, "tamaño " + sizes[i]);
                Assert.That(AssetDatabase.GetAssetPath(icons[i]), Is.EqualTo(IdentityStep.StandaloneFolder + "/icon-" + sizes[i] + ".png"));
                Assert.That(icons[i].width, Is.EqualTo(sizes[i]));
            }
        }

        [Test, Category("AC-APP-16-08")]
        public void ImportSettingsAreFixed()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IdentityStep.Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.sRGBTexture, Is.True, path);
                Assert.That(importer.maxTextureSize, Is.EqualTo(IdentityStep.MaxTextureSize), path);
            }
        }

        [Test, Category("AC-APP-16-08")]
        public void ApplyIsIdempotent()
        {
            var before = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            IdentityStep.ApplyImport("identity.import");
            IdentityStep.ApplyStandalone("identity.standalone");
            IdentityStep.ApplyMobile("identity.mobile");
            AssetDatabase.SaveAssets();
            Assert.That(File.ReadAllText("ProjectSettings/ProjectSettings.asset"), Is.EqualTo(before), "aplicar dos veces no cambia ProjectSettings");
        }

        [Test, Category("AC-APP-16-11")]
        public void LaunchScreenAndSplash()
        {
            Assert.That(PlayerSettings.allowFullscreenSwitch, Is.False);
            Assert.That(PlayerSettings.iOS.requiresFullScreen, Is.False);
            Assert.That(PlayerSettings.SplashScreen.show, Is.False);
            var mark = AssetDatabase.LoadAssetAtPath<Texture2D>(RowMate.Bootstrap.UiAssets.BootMarkPath);
            Assert.That(mark, Is.Not.Null);
            Assert.That(mark.width, Is.EqualTo(384), "96 dp × 4");
            var assets = AssetDatabase.LoadAssetAtPath<RowMate.Bootstrap.UiAssets>(RowMate.Bootstrap.UiAssets.AssetPath);
            Assert.That(assets.BootMark, Is.SameAs(mark));
        }
    }

    public sealed class WindowsWindowFrameTests
    {
        private sealed class FakeApi : IWndProcApi
        {
            public IntPtr Current = (IntPtr)0x1000;
            public readonly List<string> Calls = new List<string>();
            public int Dpi = 144;

            public IntPtr GetWndProc(IntPtr window)
            {
                return Current;
            }

            public IntPtr SetWndProc(IntPtr window, IntPtr procedure)
            {
                var previous = Current;
                Current = procedure;
                Calls.Add("set");
                return previous;
            }

            public IntPtr CallOriginal(IntPtr original, IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
            {
                Calls.Add("original " + message.ToString("X4") + (Current == original ? " (restaurado)" : ""));
                if (message == WindowsWindowFrame.WmGetMinMaxInfo)
                {
                    // El original escribe su mínimo y su máximo: el nuestro pisa SOLO el mínimo.
                    Marshal.WriteInt32(lParam, 16, 7777);
                    Marshal.WriteInt32(lParam, WindowsWindowFrame.MinTrackXOffset, 1);
                    Marshal.WriteInt32(lParam, WindowsWindowFrame.MinTrackYOffset, 1);
                }

                return (IntPtr)5;
            }

            public int DpiFor(IntPtr window)
            {
                return Dpi;
            }

            public void FrameInsets(IntPtr window, int dpi, out int width, out int height)
            {
                width = 16;
                height = 39;
            }

            public bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
            {
                Calls.Add("post " + message.ToString("X4"));
                return true;
            }
        }

        // QA-V14-01: cerrar con la X (o Alt+F4) terminaba en 0xC0000005: Unity apagaba el dominio con nuestro thunk todavía en la cadena (o en
        // la pila). Al recibir WM_CLOSE se restaura el original y se vuelve a encolar el mismo WM_CLOSE, que llega directo al de Unity, sin
        // nada administrado en el medio; el nuestro devuelve 0 sin llamar al original.
        [Test, Category("AC-APP-15-05")]
        public void CloseRestoresTheOriginalAndRepostsIt()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            frame.Install(null);
            Assert.That(frame.Handle((IntPtr)42, WindowsWindowFrame.WmClose, IntPtr.Zero, IntPtr.Zero), Is.EqualTo(IntPtr.Zero));
            Assert.That(api.Current, Is.EqualTo((IntPtr)0x1000), "el original otra vez en GWLP_WNDPROC");
            Assert.That(api.Calls, Has.Member("post 0010"), "el mismo WM_CLOSE, encolado de nuevo");
            Assert.That(api.Calls.FindAll(c => c.StartsWith("original 0010", StringComparison.Ordinal)), Is.Empty, "no se llama al original desde el thunk");
            Assert.That(frame.IsInstalled, Is.False);
            Assert.That(frame.LastUninstall, Is.EqualTo(WndProcRestore.Restored));
        }

        [Test, Category("AC-APP-15-05")]
        public void CloseWithAnotherSubclassOnTopDelegatesAndSaysSo()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            frame.Install(null);
            api.Current = (IntPtr)0x2000;
            frame.Handle((IntPtr)42, WindowsWindowFrame.WmClose, IntPtr.Zero, IntPtr.Zero);
            Assert.That(api.Current, Is.EqualTo((IntPtr)0x2000), "la otra subclase no se toca");
            Assert.That(api.Calls, Has.No.Member("post 0010"), "sin volver a encolar (no se cerraría dos veces)");
            Assert.That(api.Calls[api.Calls.Count - 1], Does.StartWith("original 0010"));
            Assert.That(frame.LastUninstall, Is.EqualTo(WndProcRestore.NotOurs), "queda registrado para el log");
        }

        [Test, Category("AC-APP-15-05")]
        public void DestroyRestoresBeforeDelegating()
        {
            foreach (var message in new[] { WindowsWindowFrame.WmDestroy, WindowsWindowFrame.WmNcDestroy })
            {
                var api = new FakeApi();
                var frame = new WindowsWindowFrame(api, (IntPtr)42);
                frame.Install(null);
                frame.Handle((IntPtr)42, message, IntPtr.Zero, IntPtr.Zero);
                Assert.That(api.Calls[api.Calls.Count - 1], Is.EqualTo("original " + message.ToString("X4") + " (restaurado)"), "el original ya está puesto cuando se delega");
                Assert.That(frame.IsInstalled, Is.False);
            }
        }

        // QA-V14-03: hasta el primer cuadro de Unity, el área de contenido se pinta con bg.app (antes se veía blanca ≈ 0,7 s).
        [Test, Category("AC-APP-16-10")]
        public void BootBackgroundIsBgAppAsColorRef()
        {
            Assert.That(WindowsBootBackground.ColorRef(RowMate.Core.Design.Tokens.BgApp), Is.EqualTo(0x002A1B0Du), "#0D1B2A como 0x00BBGGRR");
            Assert.That(WindowsBootBackground.GclpHbrBackground, Is.EqualTo(-10));
            var source = File.ReadAllText(Path.Combine(Application.dataPath, "RowMate/Platform/Display/WindowsBootBackground.cs"));
            Assert.That(source, Does.Contain("RuntimeInitializeLoadType.SubsystemRegistration"), "lo antes posible, antes de la primera escena");
            Assert.That(source, Does.Contain("#if UNITY_STANDALONE_WIN && !UNITY_EDITOR"), "solo en el player de Windows");
        }

        [Test, Category("AC-APP-15-05")]
        public void UninstallSaysWhatHappened()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            Assert.That(frame.LastUninstall, Is.EqualTo(WndProcRestore.None));
            frame.Install(null);
            frame.Uninstall();
            Assert.That(frame.LastUninstall, Is.EqualTo(WndProcRestore.Restored));
            frame.Uninstall();
            Assert.That(frame.LastUninstall, Is.EqualTo(WndProcRestore.Restored), "la segunda no cambia lo registrado");
            Assert.That(WindowsWindowFrame.Describe(WndProcRestore.NotOurs), Does.Contain("otra subclase"));
        }

        [Test, Category("AC-APP-15-05")]
        public void InstallKeepsTheOriginalAndUninstallRestoresOnlyOurs()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            Action? quitting = null;
            Assert.That(frame.Install(handler => quitting = handler), Is.True);
            Assert.That(frame.Original, Is.EqualTo((IntPtr)0x1000));
            Assert.That(api.Current, Is.EqualTo(WindowsWindowFrame.OurProcedure));
            Assert.That(quitting, Is.Not.Null, "se engancha a Application.quitting");
            quitting!();
            Assert.That(api.Current, Is.EqualTo((IntPtr)0x1000), "vuelve el original");
            var sets = api.Calls.Count;
            frame.Uninstall();
            Assert.That(api.Calls.Count, Is.EqualTo(sets), "dos veces no hace nada");
        }

        [Test, Category("AC-APP-15-05")]
        public void UninstallLeavesAnotherSubclassAlone()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            frame.Install(null);
            api.Current = (IntPtr)0x2000;
            frame.Uninstall();
            Assert.That(api.Current, Is.EqualTo((IntPtr)0x2000), "si el actual no es el nuestro, no se toca");
        }

        [Test, Category("AC-APP-15-06")]
        public void MinMaxInfoCallsTheOriginalFirstAndWritesOnlyTheMinimum()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            frame.Install(null);
            var info = Marshal.AllocHGlobal(40);
            try
            {
                frame.Handle((IntPtr)42, WindowsWindowFrame.WmGetMinMaxInfo, IntPtr.Zero, info);
                Assert.That(api.Calls[api.Calls.Count - 1], Is.EqualTo("original 0024"));
                WindowGeometry.MinTrack(144, 16, 39, out var width, out var height);
                Assert.That(Marshal.ReadInt32(info, WindowsWindowFrame.MinTrackXOffset), Is.EqualTo(width));
                Assert.That(Marshal.ReadInt32(info, WindowsWindowFrame.MinTrackYOffset), Is.EqualTo(height));
                Assert.That(width, Is.EqualTo(960 + 16), "640 lógicos al 150 % más el marco");
                Assert.That(Marshal.ReadInt32(info, 16), Is.EqualTo(7777), "el máximo de Unity queda");
            }
            finally
            {
                Marshal.FreeHGlobal(info);
                frame.Uninstall();
            }
        }

        [Test, Category("AC-APP-15-07")]
        public void AltEnterWithoutBeep()
        {
            var api = new FakeApi();
            var frame = new WindowsWindowFrame(api, (IntPtr)42);
            frame.Install(null);
            Assert.That(frame.Handle((IntPtr)42, WindowsWindowFrame.WmSysChar, (IntPtr)WindowsWindowFrame.VkReturn, IntPtr.Zero), Is.EqualTo(IntPtr.Zero));
            Assert.That(api.Calls, Has.No.Member("original 0106"), "no llega al original: sin pitido");
            frame.Handle((IntPtr)42, 0x000F, IntPtr.Zero, IntPtr.Zero);
            Assert.That(api.Calls[api.Calls.Count - 1], Is.EqualTo("original 000F"), "el resto va al original (WM_PAINT; WM_CLOSE tiene su camino, QA-V14-01)");
            frame.Uninstall();
        }
    }

    public sealed class WindowModeTests
    {
        private sealed class FakeFrame : IWindowFrame
        {
            public WindowRect Rect = new WindowRect(100, 80, 1280, 720);
            public readonly List<string> Calls = new List<string>();

            public WindowRect Current => Rect;

            public void EnterFullscreen()
            {
                Calls.Add("entrar");
            }

            public void ExitFullscreen(WindowRect restore)
            {
                Calls.Add("salir " + restore);
            }

            public void Tick()
            {
            }
        }

        [Test, Category("AC-APP-15-07")]
        public void F11AndAltEnterToggleAndRestore()
        {
            var frame = new FakeFrame();
            var controller = new WindowModeController(frame);
            Assert.That(controller.OnKey(WindowKey.F11, false), Is.EqualTo(WindowModeAction.EnterFullscreen));
            frame.Rect = new WindowRect(0, 0, 1920, 1080);
            Assert.That(controller.OnKey(WindowKey.Escape, false), Is.EqualTo(WindowModeAction.None), "Esc no cambia la ventana");
            Assert.That(controller.OnKey(WindowKey.Enter, false), Is.EqualTo(WindowModeAction.None), "Enter sin Alt tampoco");
            Assert.That(controller.OnKey(WindowKey.Enter, true), Is.EqualTo(WindowModeAction.ExitFullscreen));
            Assert.That(frame.Calls, Is.EqualTo(new[] { "entrar", "salir " + new WindowRect(100, 80, 1280, 720) }), "vuelve al rectángulo de antes");
        }
    }

    public sealed class ApplePostprocessTests
    {
        private const string Plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
            + "<plist version=\"1.0\"><dict><key>CFBundleName</key><string>RowMate</string><key>UISupportedInterfaceOrientations</key><array>"
            + "<string>UIInterfaceOrientationPortraitUpsideDown</string></array><key>UIRequiresFullScreen</key><true/></dict></plist>";

        [Test, Category("AC-APP-15-09")]
        public void InfoPlistOrientations()
        {
            var patched = ApplePostprocess.PatchInfoPlist(Plist);
            Assert.That(ApplePostprocess.ReadArray(patched, "UISupportedInterfaceOrientations"), Is.EqualTo(ApplePostprocess.IPhoneOrientations));
            Assert.That(ApplePostprocess.ReadArray(patched, "UISupportedInterfaceOrientations~ipad"), Is.EqualTo(ApplePostprocess.IPadOrientations));
            Assert.That(patched, Does.Match(@"<key>UIRequiresFullScreen</key>\s*<false ?/>"), "multitarea en iPad");
            Assert.That(patched, Does.Contain("<string>RowMate</string>"), "lo demás queda");
            Assert.That(patched, Does.Contain("<!DOCTYPE plist"));
        }

        [Test, Category("AC-APP-16-11"), Category("AC-APP-16-08")]
        public void CopiesTheIconAndTheLaunchMark()
        {
            var project = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "xcode-" + Guid.NewGuid().ToString("N"));
            try
            {
                var catalog = Path.Combine(project, "Unity-iPhone", "Images.xcassets", "AppIcon.appiconset");
                Directory.CreateDirectory(catalog);
                File.WriteAllText(Path.Combine(catalog, "Icon-Unity.png"), "viejo");
                File.WriteAllText(Path.Combine(project, "Info.plist"), Plist);
                ApplePostprocess.Apply(project, Path.GetFullPath(ApplePostprocess.IdentityFolder));
                Assert.That(File.Exists(Path.Combine(catalog, "Icon-Unity.png")), Is.False, "el de Unity sale");
                Assert.That(File.Exists(Path.Combine(catalog, "icon-1024.png")), Is.True);
                Assert.That(File.Exists(Path.Combine(catalog, "Contents.json")), Is.True);
                Assert.That(Directory.GetFiles(catalog, "*.meta"), Is.Empty, "sin los .meta de Unity");
                var mark = Path.Combine(project, "Unity-iPhone", "Images.xcassets", "RowMateLaunchMark.imageset");
                Assert.That(File.Exists(Path.Combine(mark, "mark@3x.png")), Is.True);
                var icon = new Texture2D(2, 2);
                Assert.That(icon.LoadImage(File.ReadAllBytes(Path.Combine(catalog, "icon-1024.png"))), Is.True);
                Assert.That(icon.width, Is.EqualTo(1024));
                Assert.That(File.ReadAllBytes(Path.Combine(catalog, "icon-1024.png"))[25], Is.EqualTo(2), "PNG de color tipo 2 (RGB): opaco, sin alfa");
                UnityEngine.Object.DestroyImmediate(icon);
            }
            finally
            {
                if (Directory.Exists(project))
                {
                    Directory.Delete(project, true);
                }
            }
        }
    }
}
