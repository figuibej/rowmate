using System.Collections;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Ports;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // La regla única de área segura (02-tech-design §3.1, QA-G2-06): la barra superior consume el margen superior y los
    // laterales, la de pestañas el inferior y los laterales, el contenido no consume nada.
    public sealed class SafeAreaTests : UiTestBase
    {
        [UnityTest, Category("AC-APP-05-09"), Category("QA-G2-06")]
        public IEnumerator PhoneWithStatusAndGestureBars()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TV, Insets = new SafeInsets(0, 24, 0, 48) });
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            yield return Frames(3);
            var root = H.Find("app-root")!;
            Assert.That(root.resolvedStyle.paddingTop + root.resolvedStyle.paddingBottom, Is.EqualTo(0), "#app-root sin padding");
            Assert.That(root.worldBound.height, Is.EqualTo(H.Root.worldBound.height).Within(0.5f));
            var appBar = H.BoundsOf("device-appbar");
            Assert.That(appBar.yMin, Is.EqualTo(0).Within(0.5f));
            Assert.That(appBar.height, Is.EqualTo(80).Within(0.5f), "56 + 24");
            Assert.That(H.Find("device-appbar")!.Q<Label>(className: "rm-appbar__title").worldBound.yMin, Is.GreaterThanOrEqualTo(24 - 0.5f));
            var bar = H.BoundsOf("tab-bar");
            Assert.That(bar.height, Is.EqualTo(112).Within(0.5f), "64 + 48");
            Assert.That(bar.yMax, Is.EqualTo(H.Root.worldBound.yMax).Within(0.5f), "llega al borde inferior");
            H.ScrollToEnd("device-connected-scroll");
            yield return Frames(3);
            Assert.That(H.BoundsOf("hrm-card").yMax, Is.LessThanOrEqualTo(bar.yMin + 0.5f), "al final del desplazamiento, por encima de la barra");
        }

        [UnityTest, Category("QA-G2-06")]
        public IEnumerator LandscapeLateralInsetMovesTheBarsNotTheContent()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.TH });
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            var columns = H.DeviceScreen!.Connected.Grid.Spec!.Columns;
            H.SetInsets(new SafeInsets(32, 0, 0, 0));
            yield return Frames(3);
            Assert.That(H.Find("device-appbar")!.resolvedStyle.paddingLeft, Is.EqualTo(32).Within(0.01f));
            Assert.That(H.Find("tab-bar")!.resolvedStyle.paddingLeft, Is.EqualTo(32).Within(0.01f));
            Assert.That(H.DeviceScreen.Connected.Grid.Spec!.Columns, Is.EqualTo(columns), "el contenido no se rellena");
        }
    }
}
