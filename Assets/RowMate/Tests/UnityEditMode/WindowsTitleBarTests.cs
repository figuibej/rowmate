using NUnit.Framework;
using RowMate.Platform.Display;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // Barra de título de Windows en el tema del sistema (QA-V2-08 de la It. 2; legacy windows/runner/win32_window.cpp:275-287).
    // La regla y las constantes; la ventana real la mira QA con tools/unity/capture-window.ps1 sobre un build.
    public sealed class WindowsTitleBarTests
    {
        [Test, Category("AC-APP-05-01"), Category("AC-APP-05-10"), Category("QA-V2-08")]
        public void DarkOnlyWhenTheSystemSaysDark()
        {
            Assert.That(WindowsTitleBar.DarkFor(0), Is.True, "AppsUseLightTheme = 0: oscura");
            Assert.That(WindowsTitleBar.DarkFor(1), Is.False, "AppsUseLightTheme = 1: clara");
            Assert.That(WindowsTitleBar.DarkFor(null), Is.Null, "sin el valor no se toca (RegGetValue != ERROR_SUCCESS)");
        }

        [Test, Category("AC-APP-05-01"), Category("AC-APP-05-10"), Category("QA-V2-08")]
        public void SameRegistryValueAndDwmAttributeAsTheLegacy()
        {
            Assert.That(WindowsTitleBar.PersonalizeKey, Is.EqualTo(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"));
            Assert.That(WindowsTitleBar.LightThemeValue, Is.EqualTo("AppsUseLightTheme"));
            Assert.That(WindowsTitleBar.DwmUseImmersiveDarkMode, Is.EqualTo(20), "DWMWA_USE_IMMERSIVE_DARK_MODE");
        }

        [Test, Category("AC-APP-05-10")]
        public void ThemeChangesArriveWithinTwoSeconds()
        {
            // DD-01 (It. 3): con la app enfocada, el cambio del tema del sistema llega en el sondeo (≤ 2 s); al volver el
            // foco, en el acto (PlatformServices.Refresh). La ventana real: [Device-Win] y CAP-27 (a pedido).
            Assert.That(WindowsTitleBar.PollSeconds, Is.LessThanOrEqualTo(2));
            var bar = new WindowsTitleBar(new RecordingLogger());
            bar.Poll(0);
            bar.Poll(1);
            Assert.That(bar.AppliedDark, Is.Null, "en el Editor el sondeo tampoco toca la ventana");
        }

        [Test, Category("AC-APP-05-01"), Category("AC-APP-05-10"), Category("QA-V2-08")]
        public void TheEditorWindowIsNeverTouched()
        {
            var bar = new WindowsTitleBar(new RecordingLogger());
            bar.Apply();
            Assert.That(bar.AppliedDark, Is.Null, "en el Editor no se cambia la ventana del Editor");
        }
    }
}
