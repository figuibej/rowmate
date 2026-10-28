using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Presentation.Surface;
using RowMate.Tests.Support;
using UnityEditor;
using UnityEngine.UIElements;

namespace RowMate.Tests.UnityEditMode
{
    // RPF · La copia del panel con sus respaldos de fuente (QA-G3-12, DN3-21; 03-rutinas/02-tech-design §4.4; 04-design-notes
    // §4.15): una sola fábrica, la copia de PanelTextSettings distinta del asset y con los respaldos en orden (íconos, Noto
    // Emoji), y los tres anfitriones (AppRoot, UiTestHost y el arnés, que usa UiTestHost) la llaman con el MISMO UiAssets.
    public sealed class RuntimePanelFactoryTests
    {
        [Test, Category("QA-G3-12")]
        public void TheCopyHasTheFallbacksInOrder()
        {
            var assets = AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            Assert.That(assets, Is.Not.Null, UiAssets.AssetPath);
            Assert.That(assets!.Panel, Is.Not.Null);
            Assert.That(assets.IconsOutlineFallback, Is.Not.Null, "respaldo 1: RowMateIcons-Outline-SDF");
            Assert.That(assets.EmojiFallback, Is.Not.Null, "respaldo 2: Noto Emoji");
            Assert.That(assets.IconsOutlineFallback!.characterLookupTable.ContainsKey(0xE8E5), Is.True, "trending_up en el respaldo de íconos");
            var originalText = assets.Panel!.textSettings;
            var panel = RuntimePanelFactory.Create(assets.Panel, assets.IconsOutlineFallback, assets.EmojiFallback, "RPF");
            try
            {
                Assert.That(panel, Is.Not.SameAs(assets.Panel), "una copia: el asset no se toca (R2-05)");
                Assert.That(panel.textSettings, Is.Not.Null);
                Assert.That(panel.textSettings, Is.Not.SameAs(originalText), "una copia propia de PanelTextSettings");
                Assert.That(assets.Panel.textSettings, Is.SameAs(originalText), "el asset sigue con lo suyo");
                var fallbacks = panel.textSettings!.fallbackFontAssets;
                Assert.That(fallbacks.Count, Is.EqualTo(2));
                Assert.That(fallbacks[0], Is.SameAs(assets.IconsOutlineFallback), "primero los íconos");
                Assert.That(fallbacks[1], Is.SameAs(assets.EmojiFallback), "después Noto Emoji");
                Assert.That(panel.themeStyleSheet, Is.SameAs(assets.Panel.themeStyleSheet), "el mismo tema");
            }
            finally
            {
                RuntimePanelFactory.Destroy(panel);
            }
        }

        [Test, Category("QA-G3-12")]
        public void TheThreeHostsShareTheFallbacks()
        {
            var appRoot = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Bootstrap/AppRoot.cs"));
            var host = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Tests/UiSupport/UiTestHost.cs"));
            Assert.That(appRoot, Does.Contain("RuntimePanelFactory.Create("), "AppRoot arma el panel con la fábrica");
            Assert.That(Regex.IsMatch(appRoot, @"RuntimePanelFactory\.Create\([^;]*IconsOutlineFallback[^;]*EmojiFallback"), Is.True, "con los respaldos de su UiAssets");
            Assert.That(host, Does.Contain("RuntimePanelFactory.Create(UiAssetsLocator.Panel(), UiAssetsLocator.IconsOutlineFallback(), UiAssetsLocator.EmojiFallback()"),
                "UiTestHost, con el MISMO UiAssets.asset");
            foreach (var file in RepoPaths.Files("Assets/RowMate/Snapshots", "*.cs"))
            {
                // R-T1 (It. 2, CAP-23 de su conjunto) compara dos tuberías de color con su propio panel: no dibuja texto que use
                // los respaldos, y sus bytes tienen que seguir iguales.
                if (Path.GetFileName(file) == "Rt1Measurement.cs")
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.That(text, Does.Not.Contain("Object.Instantiate(UiAssetsLocator.Panel()"), RepoPaths.Relative(file) + ": el arnés no copia el panel por su cuenta");
                Assert.That(text, Does.Not.Contain("new PanelTextSettings"), RepoPaths.Relative(file));
            }

            var factoryUses = 0;
            foreach (var file in RepoPaths.Files("Assets/RowMate", "*.cs"))
            {
                var text = File.ReadAllText(file);
                if (text.Contains("ScriptableObject.CreateInstance<" + "PanelTextSettings>"))
                {
                    factoryUses++;
                    Assert.That(RepoPaths.Relative(file).Replace('\\', '/'), Is.EqualTo("Assets/RowMate/Presentation/Surface/RuntimePanelFactory.cs"), "una sola fábrica");
                }
            }

            Assert.That(factoryUses, Is.EqualTo(1));
            Assert.That(typeof(PanelTextSettings).IsSubclassOf(typeof(UnityEngine.TextCore.Text.TextSettings)), Is.True);
        }
    }
}
