using System.IO;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.EditorTools;
using RowMate.Presentation.Controls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Tests.UnityEditMode
{
    // Bootstrapper v3 (02-tech-design §1.3) con el estado real del proyecto abierto en Unity.
    public sealed class BootstrapV3Tests
    {
        [Test, Category("AC-BASE-10")]
        public void Version3AppliedOnce()
        {
            var state = BootstrapState.Load(ProjectBootstrapper.StatePath);
            Assert.That(state, Is.Not.Null);
            Assert.That(state!.version, Is.EqualTo(3));
            Assert.That(ProjectBootstrapper.CurrentVersion, Is.EqualTo(3));
            var ids = new string[state.steps.Count];
            for (var i = 0; i < ids.Length; i++)
            {
                ids[i] = state.steps[i].id;
                Assert.That(state.steps[i].status, Is.EqualTo(BootstrapState.StatusOk).Or.EqualTo(BootstrapState.StatusSkipped), ids[i]);
            }

            Assert.That(ids, Is.EqualTo(new[]
            {
                "player.identity", "player.scripting", "player.android", "player.ios", "player.rendering", "player.runtime", "player.input",
                "urp.provisional", "urp.tidy", "player.window", "ui.fonts", "ui.panel", "scene.boot", "defines.check",
            }));
        }

        [Test, Category("AC-BASE-10")]
        public void UrpAssetsLiveUnderRowMate()
        {
            foreach (var name in UrpProvisioner.RootAssets)
            {
                Assert.That(File.Exists("Assets/" + name), Is.False, name + " ya no está en la raíz");
                Assert.That(File.Exists(UrpProvisioner.Folder + "/" + name), Is.True, name);
            }
        }

        [Test, Category("AC-APP-01-07")]
        public void WindowAndSplash()
        {
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(1280));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(720));
            Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(PlayerSettings.resizableWindow, Is.True);
        }

        [Test, Category("AC-APP-01-07")]
        public void SplashOffAndBackgroundIsBgApp()
        {
            Assert.That(PlayerSettings.SplashScreen.show, Is.False);
            Assert.That((Color32)PlayerSettings.SplashScreen.backgroundColor, Is.EqualTo((Color32)TokenColors.ToColor(Tokens.BgApp)));
        }

        [Test, Category("QA-G2-06")]
        public void AndroidSystemBarsLikeLegacy()
        {
            Assert.That(PlayerSettings.Android.startInFullscreen, Is.False, "barras del sistema visibles (styles.xml windowFullscreen=false)");
            Assert.That(PlayerSettings.Android.renderOutsideSafeArea, Is.True);
        }

        [Test, Category("AC-BASE-10")]
        public void PanelSettingsAsset()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiAssets.PanelPath);
            Assert.That(panel, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(panel.themeStyleSheet), Is.EqualTo(UiAssets.ThemePath));
            Assert.That(panel.scaleMode, Is.EqualTo(PanelScaleMode.ConstantPixelSize));
            Assert.That(panel.scale, Is.EqualTo(1f));
            Assert.That(panel.targetTexture, Is.Null, "la RT la pone la copia");
            Assert.That((Color32)panel.colorClearValue, Is.EqualTo((Color32)TokenColors.ToColor(Tokens.BgApp)));
            var assets = AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            Assert.That(assets, Is.Not.Null);
            Assert.That(assets.IsComplete, Is.True);
            Assert.That(assets.Panel, Is.SameAs(panel));
        }

        [Test, Category("AC-BASE-10")]
        public void BootSceneIsTheOnlyBuildScene()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.That(scenes.Length, Is.EqualTo(1));
            Assert.That(scenes[0].path, Is.EqualTo(BootSceneBuilder.ScenePath));
            Assert.That(scenes[0].enabled, Is.True);
        }

        [Test, Category("QA-G2-01")]
        public void BootSceneDocumentHasNoPanelAsset()
        {
            var text = File.ReadAllText(BootSceneBuilder.ScenePath);
            Assert.That(text, Does.Contain("m_PanelSettings: {fileID: 0}"), "el documento de la escena no tiene panel");
            var guid = AssetDatabase.AssetPathToGUID(UiAssets.AssetPath);
            Assert.That(text, Does.Contain("_assets: {fileID: 11400000, guid: " + guid), "AppRoot._assets = UiAssets");
            Assert.That(text, Does.Contain("m_Name: UiCamera"));
        }

        [Test, Category("AC-APP-01-06"), Category("AC-APP-16-01")]
        public void ProductIsRowMate()
        {
            Assert.That(PlayerSettings.productName, Is.EqualTo(AppIdentity.ProductName));
            Assert.That(PlayerSettings.companyName, Is.EqualTo(AppIdentity.CompanyName));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo(AppIdentity.Version));
        }
    }
}
