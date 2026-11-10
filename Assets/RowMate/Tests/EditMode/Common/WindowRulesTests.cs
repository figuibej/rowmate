using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;

namespace RowMate.Tests.EditMode.Common
{
    // WG · La geometría de la ventana de escritorio (It. 14, F-APP-15; DT14-13, DT14-16).
    [TestFixture]
    public sealed class WindowGeometryTests
    {
        private static readonly WindowRect FullHd = new WindowRect(0, 0, 1920, 1040);

        [Test, Category("AC-APP-15-05")]
        public void InitialIsCenteredAndScaled()
        {
            Assert.That(WindowGeometry.Initial(FullHd, 1.0), Is.EqualTo(new WindowRect(320, 160, 1280, 720)));
            Assert.That(WindowGeometry.Initial(new WindowRect(0, 0, 2560, 1400), 1.5), Is.EqualTo(new WindowRect(320, 160, 1920, 1080)));
            // Otro monitor como principal (origen distinto de 0).
            Assert.That(WindowGeometry.Initial(new WindowRect(-1920, 40, 1920, 1040), 1.0), Is.EqualTo(new WindowRect(-1600, 200, 1280, 720)));
        }

        [Test, Category("AC-APP-15-05")]
        public void InitialIsClippedToTheWorkArea()
        {
            Assert.That(WindowGeometry.Initial(new WindowRect(0, 0, 1366, 728), 1.25), Is.EqualTo(new WindowRect(0, 0, 1366, 728)));
            Assert.That(WindowGeometry.Initial(new WindowRect(0, 0, 1280, 680), 1.0), Is.EqualTo(new WindowRect(0, 0, 1280, 680)));
        }

        // QA-V14-02: se recorta el rectángulo EXTERIOR (contenido + marco con el DPI) al área de trabajo y se centra el exterior; antes se
        // recortaba el contenido y el marco quedaba afuera (la barra de título en y = −31 en un monitor de 1024 × 768).
        private static readonly WindowFrameEdges Frame96 = new WindowFrameEdges(8, 31, 8, 8);

        [Test, Category("AC-APP-15-05")]
        public void TheOuterWindowFitsTheWorkArea1024()
        {
            var content = WindowGeometry.Initial(new WindowRect(0, 0, 1024, 720), 1.0, Frame96);
            Assert.That(content, Is.EqualTo(new WindowRect(8, 31, 1008, 681)));
            AssertOuterInside(content, Frame96, new WindowRect(0, 0, 1024, 720));
        }

        [Test, Category("AC-APP-15-05")]
        public void TheOuterWindowFitsTheWorkArea1366()
        {
            var content = WindowGeometry.Initial(new WindowRect(0, 0, 1366, 728), 1.0, Frame96);
            Assert.That(content, Is.EqualTo(new WindowRect(43, 31, 1280, 689)), "el ancho entra entero; el alto se achica por el marco");
            AssertOuterInside(content, Frame96, new WindowRect(0, 0, 1366, 728));
        }

        [Test, Category("AC-APP-15-05")]
        public void TheOuterWindowFitsTheWorkAreaAt150()
        {
            var frame = new WindowFrameEdges(12, 46, 12, 12);
            var work = new WindowRect(0, 0, 1920, 1032);
            var content = WindowGeometry.Initial(work, 1.5, frame);
            Assert.That(content, Is.EqualTo(new WindowRect(12, 46, 1896, 974)));
            AssertOuterInside(content, frame, work);
        }

        [Test, Category("AC-APP-15-05")]
        public void WhenItFitsTheOuterIsCentered()
        {
            var content = WindowGeometry.Initial(FullHd, 1.0, Frame96);
            Assert.That(content, Is.EqualTo(new WindowRect(320, 171, 1280, 720)), "exterior 1296 × 759 centrado: (312, 140)");
            AssertOuterInside(content, Frame96, FullHd);
            var other = new WindowRect(-1920, 40, 1920, 1040);
            AssertOuterInside(WindowGeometry.Initial(other, 1.0, Frame96), Frame96, other);
        }

        private static void AssertOuterInside(WindowRect content, WindowFrameEdges frame, WindowRect work)
        {
            var left = content.X - frame.Left;
            var top = content.Y - frame.Top;
            var right = content.X + content.Width + frame.Right;
            var bottom = content.Y + content.Height + frame.Bottom;
            Assert.That(left, Is.GreaterThanOrEqualTo(work.X), "borde izquierdo");
            Assert.That(top, Is.GreaterThanOrEqualTo(work.Y), "barra de título");
            Assert.That(right, Is.LessThanOrEqualTo(work.X + work.Width), "borde derecho");
            Assert.That(bottom, Is.LessThanOrEqualTo(work.Y + work.Height), "borde inferior");
        }

        [Test, Category("AC-APP-15-05")]
        public void ShouldPlaceInitial()
        {
            Assert.That(WindowGeometry.ShouldPlaceInitial(new string[0]), Is.True);
            Assert.That(WindowGeometry.ShouldPlaceInitial(new[] { "RowMate.exe", "-rowmate-perf-boot" }), Is.True);
            Assert.That(WindowGeometry.ShouldPlaceInitial(new[] { "RowMate.exe", "-screen-width", "800" }), Is.False);
            Assert.That(WindowGeometry.ShouldPlaceInitial(new[] { "-screen-height", "480" }), Is.False);
            Assert.That(WindowGeometry.ShouldPlaceInitial(new[] { "-Screen-Fullscreen", "0" }), Is.False, "sin distinguir mayúsculas");
            Assert.That(WindowGeometry.ShouldPlaceInitial(new[] { "-popupwindow" }), Is.False);
            Assert.That(WindowGeometry.ShouldPlaceInitial(null!), Is.True);
        }

        [Test, Category("AC-APP-15-05")]
        public void CorrectsFullscreenOrRememberedSize()
        {
            Assert.That(WindowGeometry.NeedsCorrection(true, 1280, 720, 1.0, FullHd), Is.False);
            Assert.That(WindowGeometry.NeedsCorrection(false, 1280, 720, 1.0, FullHd), Is.True, "se cerró en pantalla completa");
            Assert.That(WindowGeometry.NeedsCorrection(true, 1000, 700, 1.0, FullHd), Is.True, "Unity recordó otro tamaño");
        }

        [Test, Category("AC-APP-15-06")]
        public void MinTrackIsContentPlusFrame()
        {
            WindowGeometry.MinTrack(96, 16, 39, out var w, out var h);
            Assert.That(w, Is.EqualTo(656));
            Assert.That(h, Is.EqualTo(399));
            WindowGeometry.MinTrack(144, 22, 52, out w, out h);
            Assert.That(w, Is.EqualTo(960 + 22));
            Assert.That(h, Is.EqualTo(540 + 52));
            WindowGeometry.MinTrack(120, 0, 0, out w, out h);
            Assert.That(w, Is.EqualTo(800));
            Assert.That(h, Is.EqualTo(450));
            WindowGeometry.MinTrack(0, -5, -5, out w, out h);
            Assert.That(w, Is.EqualTo(640), "sin DPI: escala 1; inserciones negativas no restan");
            Assert.That(h, Is.EqualTo(360));
        }

        [Test, Category("AC-APP-15-09")]
        public void Mac800x600()
        {
            Assert.That(WindowGeometry.MacInitialWidth, Is.EqualTo(800));
            Assert.That(WindowGeometry.MacInitialHeight, Is.EqualTo(600));
            Assert.That(WindowGeometry.Initial(new WindowRect(0, 25, 1440, 875), 1.0, WindowGeometry.MacInitialWidth, WindowGeometry.MacInitialHeight),
                Is.EqualTo(new WindowRect(320, 162, 800, 600)));
        }
    }

    // WM · Ventana ↔ pantalla completa (It. 14, AC-APP-15-07, DV-129).
    [TestFixture]
    public sealed class WindowModeStateTests
    {
        private static readonly WindowRect Window = new WindowRect(100, 80, 1280, 720);

        [Test, Category("AC-APP-15-07")]
        public void F11TogglesAndRestores()
        {
            var state = new WindowModeState();
            Assert.That(state.OnKey(WindowKey.F11, false, Window), Is.EqualTo(WindowModeAction.EnterFullscreen));
            Assert.That(state.IsFullscreen, Is.True);
            Assert.That(state.Restore, Is.EqualTo(Window));
            Assert.That(state.OnKey(WindowKey.F11, false, new WindowRect(0, 0, 1920, 1080)), Is.EqualTo(WindowModeAction.ExitFullscreen));
            Assert.That(state.IsFullscreen, Is.False);
            Assert.That(state.Restore, Is.EqualTo(Window), "vuelve al tamaño y la posición de antes");
        }

        [Test, Category("AC-APP-15-07")]
        public void AltEnterTogglesEnterAloneAndEscDoNothing()
        {
            var state = new WindowModeState();
            Assert.That(state.OnKey(WindowKey.Enter, false, Window), Is.EqualTo(WindowModeAction.None));
            Assert.That(state.OnKey(WindowKey.Escape, false, Window), Is.EqualTo(WindowModeAction.None));
            Assert.That(state.OnKey(WindowKey.Enter, true, Window), Is.EqualTo(WindowModeAction.EnterFullscreen));
            Assert.That(state.OnKey(WindowKey.Escape, false, Window), Is.EqualTo(WindowModeAction.None), "Esc no sale (sigue siendo Atrás)");
            Assert.That(state.IsFullscreen, Is.True);
            Assert.That(state.OnKey(WindowKey.Enter, true, Window), Is.EqualTo(WindowModeAction.ExitFullscreen));
        }

        [Test, Category("AC-APP-15-07")]
        public void StartsWindowedEveryTime()
        {
            Assert.That(new WindowModeState().IsFullscreen, Is.False, "no se recuerda entre arranques");
        }
    }

    // VI · El VS_VERSIONINFO del .exe (It. 14, AC-APP-16-06; DT14-31).
    [TestFixture]
    public sealed class VersionInfoBlockTests
    {
        [Test, Category("AC-APP-16-06")]
        public void ReadsBackNameCompanyAndVersion()
        {
            var block = VersionInfoBlock.Build("RowMate", "iguisoft", "1.0.0");
            Assert.That(block.Length % 4, Is.EqualTo(0), "alineado a 4");
            Assert.That(block[0] | (block[1] << 8), Is.EqualTo(block.Length), "wLength = todo el bloque");
            var strings = VersionInfoBlock.ReadStrings(block, out var fixedVersion);
            Assert.That(fixedVersion, Is.EqualTo("1.0.0.0"));
            Assert.That(strings, Is.EquivalentTo(new Dictionary<string, string>
            {
                { "CompanyName", "iguisoft" },
                { "FileDescription", "RowMate" },
                { "FileVersion", "1.0.0" },
                { "InternalName", "RowMate" },
                { "LegalCopyright", "© iguisoft" },
                { "OriginalFilename", "RowMate.exe" },
                { "ProductName", "RowMate" },
                { "ProductVersion", "1.0.0" },
            }));
        }

        [Test, Category("AC-APP-16-06")]
        public void HeaderKeysAndTranslation()
        {
            var block = VersionInfoBlock.Build("RowMate", "iguisoft", "1.2.3");
            var text = System.Text.Encoding.Unicode.GetString(block);
            Assert.That(text, Does.Contain("VS_VERSION_INFO"));
            Assert.That(text, Does.Contain("StringFileInfo"));
            Assert.That(text, Does.Contain(VersionInfoBlock.StringTableKey));
            Assert.That(text, Does.Contain("VarFileInfo"));
            Assert.That(text, Does.Contain("Translation"));
            // VS_FIXEDFILEINFO a continuación de la clave (6 + 32 bytes → 40 alineado): firma 0xFEEF04BD y versión 1.2.3.0.
            Assert.That(block[2] | (block[3] << 8), Is.EqualTo(52));
            Assert.That(System.BitConverter.ToUInt32(block, 40), Is.EqualTo(0xFEEF04BDu));
            Assert.That(System.BitConverter.ToUInt32(block, 48), Is.EqualTo(0x00010002u), "MS = 1.2");
            Assert.That(System.BitConverter.ToUInt32(block, 52), Is.EqualTo(0x00030000u), "LS = 3.0");
            var translation = System.BitConverter.ToUInt32(block, block.Length - 4);
            Assert.That(translation, Is.EqualTo(0x04B00409u), "0x0409 / 1200");
            VersionInfoBlock.ReadStrings(block, out var fixedVersion);
            Assert.That(fixedVersion, Is.EqualTo("1.2.3.0"));
        }

        [Test, Category("AC-APP-16-06")]
        public void RejectsBadVersions()
        {
            Assert.Throws<System.FormatException>(() => VersionInfoBlock.Build("RowMate", "iguisoft", "1.0.x"));
            Assert.Throws<System.FormatException>(() => VersionInfoBlock.Build("RowMate", "iguisoft", "1.0.0.0.0"));
            Assert.Throws<System.ArgumentException>(() => VersionInfoBlock.Build("", "iguisoft", "1.0.0"));
        }
    }
}
