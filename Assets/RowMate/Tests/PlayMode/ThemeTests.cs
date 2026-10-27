using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Surface;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Tema oscuro, cifras tabulares, escala de letra, foco de teclado, reducir movimiento y densidad (F-APP-05).
    public sealed class ThemeTests : UiTestBase
    {
        private static readonly RowingData Sample = new RowingData { StrokeRate = 23.5, PowerWatts = 149, DistanceMeters = 652, Pace500mSeconds = 133 };

        private IEnumerator Connected(UiHostOptions options)
        {
            Create(options);
            H.TestRower!.Emit(BleStatus.Connected);
            H.TestRower.EmitData(Sample);
            yield return Settle();
            yield return Frames(3);
        }

        [UnityTest, Category("AC-APP-05-02")]
        public IEnumerator BaseColorsReadFromPixels()
        {
            yield return Connected(new UiHostOptions());
            var frame = H.ReadPixels();
            try
            {
                var probes = new[]
                {
                    PixelProbe.Point("bg.app", "device-views", new Vector2(0.03f, 0.85f), Tokens.BgApp.Rgb.ToHex()),
                    PixelProbe.Point("surface.card", "metric-tile-split", new Vector2(0.5f, 0.94f), Tokens.SurfaceCard.Rgb.ToHex()),
                    PixelProbe.Point("appbar", "device-appbar", new Vector2(0.55f, 0.12f), Tokens.BgApp.Rgb.ToHex()),
                };
                foreach (var probe in probes)
                {
                    var result = probe.Measure(H.Root, frame, H.Profile.Density);
                    Assert.That(result.Pass, Is.True, result.ToString());
                }
            }
            finally
            {
                Object.Destroy(frame);
            }
        }

        [UnityTest, Category("AC-APP-05-01")]
        public IEnumerator ThemeHasNoLightVariantAndIgnoresSystemTheme()
        {
            var theme = File.ReadAllText("Assets/RowMate/Presentation/Styles/RowMateTheme.tss");
            Assert.That(theme, Does.Not.Contain("light"), "un solo tema, oscuro");
            Assert.That(Directory.GetFiles("Assets/RowMate/Presentation/Styles", "*.tss").Length, Is.EqualTo(1));
            Create(new UiHostOptions());
            yield return Frames();
            var color = H.Shell.resolvedStyle.backgroundColor;
            Assert.That((Color32)color, Is.EqualTo((Color32)TokenColors.ToColor(Tokens.BgApp)));
        }

        [UnityTest, Category("AC-APP-05-04")]
        public IEnumerator TabularCellsHaveFixedWidth()
        {
            yield return Connected(new UiHostOptions());
            var number = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Watts)!.Value;
            number.SetValue("111", 111);
            var narrow = number.NaturalWidth;
            number.SetValue("888", 888);
            Assert.That(number.NaturalWidth, Is.EqualTo(narrow).Within(0.01f), "111 = 888");
            number.SetValue("11:11", 0);
            var colon = number.NaturalWidth;
            number.SetValue("00:00", 0);
            Assert.That(number.NaturalWidth, Is.EqualTo(colon).Within(0.01f), "11:11 = 00:00");
            yield return Frames();
        }

        [UnityTest, Category("AC-APP-05-04")]
        public IEnumerator UnchangedDigitsDoNotMove()
        {
            yield return Connected(new UiHostOptions());
            var number = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Watts)!.Value;
            var before = number.GetCellRect(0);
            H.TestRower!.EmitData(Sample with { PowerWatts = 151 });
            yield return Settle();
            Assert.That(number.Text, Is.EqualTo("151"));
            Assert.That(number.GetCellRect(0).x, Is.EqualTo(before.x).Within(0.01f), "el 1 de 149 → 151 no se mueve");
            Assert.That(number.GetCellRect(0).width, Is.EqualTo(before.width).Within(0.01f));
        }

        [UnityTest, Category("AC-APP-05-05")]
        public IEnumerator TextScale130HasNoOverflow()
        {
            foreach (var profile in new[] { DeviceProfile.TV, DeviceProfile.L640 })
            {
                yield return Connected(new UiHostOptions { Profile = profile, TextScale = 1.3 });
                Assert.That(H.Shell.ClassListContains("rm-text-scale-130"), Is.True);
                var label = H.Find("metric-tile-spm")!.Q<Label>(className: "rm-metric-tile__label");
                Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(12 * 1.3f).Within(0.05f), profile.Code);
                AssertClean(H);
            }
        }

        [UnityTest, Category("AC-APP-05-05")]
        public IEnumerator LabelTrackingIsEightHundredthsEm()
        {
            foreach (var scale in new[] { 1.0, 1.3 })
            {
                Create(new UiHostOptions { TextScale = scale });
                var tracked = new Label("IIIIIIIIII");
                tracked.AddToClassList("rm-type-label");
                tracked.style.position = Position.Absolute;
                var plain = new Label("IIIIIIIIII");
                plain.AddToClassList("rm-type-label");
                plain.style.position = Position.Absolute;
                plain.style.letterSpacing = 0;
                H.Shell.Add(tracked);
                H.Shell.Add(plain);
                yield return Frames(3);
                var size = tracked.resolvedStyle.fontSize;
                var difference = tracked.layout.width - plain.layout.width;
                Assert.That(difference, Is.EqualTo(9 * 0.08 * size).Within(1.0), "a " + scale + "×: 9 × 0,08 em");
            }
        }

        [UnityTest, Category("AC-APP-05-06")]
        public IEnumerator TertiaryTextsUseTextTertiary()
        {
            Create(new UiHostOptions());
            H.TestRower!.Emit(BleStatus.Scanning);
            yield return Settle();
            var expected = TokenColors.ToColor(Tokens.TextTertiary);
            foreach (var element in new[] { H.Find("device-search-hint")!, H.Find("hrm-watch-hint")! })
            {
                var color = element.resolvedStyle.color;
                Assert.That(color.a, Is.EqualTo(expected.a).Within(0.005f), element.name + ": 54 %, no 38 %");
            }
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator TabWalksActionableControlsWithFocusRing()
        {
            Create(new UiHostOptions());
            yield return Frames();
            foreach (var name in new[] { "device-search", "hrm-search", "tab-device", "tab-workout", "tab-routines", "tab-history" })
            {
                Assert.That(H.Find(name)!.focusable, Is.True, name + " se alcanza con Tab");
            }

            var search = H.Find("device-search")!;
            search.Focus();
            using (var tab = KeyDownEvent.GetPooled('\t', KeyCode.Tab, EventModifiers.None))
            {
                tab.target = search;
                search.SendEvent(tab);
            }

            yield return Frames(2);
            Assert.That(H.Shell.ClassListContains(FocusHighlight.KeyboardNavClass), Is.True, "navegando con teclado: anillo visible");
            Assert.That(search.Q(className: "rm-focus-ring").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            using (var pointer = PointerDownEvent.GetPooled())
            {
                pointer.target = search;
                search.SendEvent(pointer);
            }

            yield return Frames(2);
            Assert.That(H.Shell.ClassListContains(FocusHighlight.KeyboardNavClass), Is.False, "el primer toque lo saca");
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator EnterAndSpaceActivate()
        {
            Create(new UiHostOptions());
            yield return Frames();
            var search = H.Find("device-search")!;
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = search;
                search.SendEvent(submit);
            }

            yield return Frames();
            Assert.That(H.TestRower!.StartScanCalls, Is.EqualTo(1), "Enter");
            using (var space = KeyDownEvent.GetPooled(' ', KeyCode.Space, EventModifiers.None))
            {
                space.target = search;
                search.SendEvent(space);
            }

            yield return Frames();
            Assert.That(H.TestRower.StartScanCalls, Is.EqualTo(2), "Espacio");
        }

        [UnityTest, Category("AC-APP-05-08")]
        public IEnumerator ReduceMotionChangesRollHeartAndTabs()
        {
            yield return Connected(new UiHostOptions { ReduceMotion = true });
            Assert.That(H.Shell.ClassListContains("rm-reduce-motion"), Is.True);
            var watts = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Watts)!.Value;
            H.TestRower!.EmitData(Sample with { PowerWatts = 151 });
            H.Handle.Tick();
            H.Advance(0.085);
            Assert.That(watts.IsRolling, Is.False, "fundido de 80 ms");
            yield return Frames();
            H.Tap("tab-history");
            H.Handle.Tick();
            var pill = H.Find("tab-bar-pill")!;
            yield return Frames(2);
            var box = H.Find("tab-history")!.Q(className: "rm-tab__icon-box");
            Assert.That(Mathf.Abs(pill.worldBound.center.x - box.worldBound.center.x), Is.LessThan(1f), "la píldora en el destino en el cuadro 0");
            Hrm().SetStatus(HrmStatus.Connected);
            Hrm().EmitBpm(120);
            H.Advance(0.1);
            var heart = H.Find("hrm-heart")!;
            Assert.That(heart.style.scale.value.value.x, Is.EqualTo(1f).Within(1e-4f), "corazón quieto");
            yield return Frames();
        }

        [UnityTest, Category("AC-APP-05-09")]
        public IEnumerator PanelScaleEqualsDensity()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV });
            yield return Frames(2);
            Assert.That(H.Root.worldBound.width, Is.EqualTo(411.43f).Within(0.05f));
            Assert.That(H.Root.worldBound.height, Is.EqualTo(914.29f).Within(0.05f));
            Assert.That(H.Panel.scale, Is.EqualTo(2.625f).Within(1e-4f));
        }

        [UnityTest, Category("AC-APP-01-07")]
        public IEnumerator SurfaceRtsStartCleared()
        {
            foreach (var format in new[] { GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8B8A8_SRGB })
            {
                var rt = UiSurface.CreateTarget(64, 64, format, "test");
                var read = RtReader.Read(rt);
                Color32 pixel = read.GetPixel(10, 10);
                var hex = new Rgb(pixel.r, pixel.g, pixel.b).ToHex();
                Assert.That(PixelProbe.Distance(hex, Tokens.BgApp.Rgb.ToHex()), Is.LessThanOrEqualTo(1), format + ": una RT recién creada sale limpia a bg.app (" + hex + ")");
                Object.Destroy(read);
                rt.Release();
                Object.Destroy(rt);
            }

            yield return null;
        }

        private RowMate.Tests.Support.ScriptedHeartRateService Hrm()
        {
            return H.TestHeartRate!;
        }
    }
}
