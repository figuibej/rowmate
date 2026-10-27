using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using RowMate.Core.Common;

namespace RowMate.EditorTools
{
    // Pasos de PlayerSettings del bootstrapper mínimo (02-tech-design §1.5, ARCHITECTURE §16). NO VERIFICABLE hasta la It. 2.
    // Cada paso asigna valores fijos (idempotente) y devuelve su resultado; una excepción la registra ProjectBootstrapper.
    internal static class PlayerSettingsStep
    {
        private const string SimulatorDefine = "ROWMATE_SIMULATOR";

        // Valor de activeInputHandler en ProjectSettings.asset: 0 = Input Manager, 1 = Input System, 2 = ambos.
        private const int InputSystemHandler = 1;

        private static readonly NamedBuildTarget[] PlayerTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone
        };

        // Empresa, producto, versión y compilación; ids provisionales (R-14, Q-85, D-02).
        // Windows no tiene identificador propio en Unity: el de Standalone es uno solo para Windows y macOS.
        public static BootstrapState.Step ApplyIdentity(string id)
        {
            PlayerSettings.companyName = AppIdentity.CompanyName;
            PlayerSettings.productName = AppIdentity.ProductName;
            PlayerSettings.bundleVersion = AppIdentity.Version;
            PlayerSettings.Android.bundleVersionCode = AppIdentity.BuildNumber;
            PlayerSettings.iOS.buildNumber = AppIdentity.BuildNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlayerSettings.macOS.buildNumber = AppIdentity.BuildNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppIdentity.AndroidApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppIdentity.AppleBundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppIdentity.AppleBundleId);
            return BootstrapState.Step.Ok(id, AppIdentity.CompanyName + "/" + AppIdentity.ProductName + " " + AppIdentity.Version
                + " (" + AppIdentity.BuildNumber + "); Android " + AppIdentity.AndroidApplicationId
                + "; iOS y Standalone " + AppIdentity.AppleBundleId);
        }

        // IL2CPP y .NET Standard 2.1 en Android, iOS y Standalone.
        public static BootstrapState.Step ApplyScripting(string id)
        {
            for (var i = 0; i < PlayerTargets.Length; i++)
            {
                PlayerSettings.SetScriptingBackend(PlayerTargets[i], ScriptingImplementation.IL2CPP);
                PlayerSettings.SetApiCompatibilityLevel(PlayerTargets[i], ApiCompatibilityLevel.NET_Standard);
            }

            return BootstrapState.Step.Ok(id, "IL2CPP + .NET Standard 2.1 en Android, iOS y Standalone");
        }

        // Android 8.0 (API 26) o más nuevo, target el más alto instalado, solo ARM64, Internet obligatorio (Q-90).
        // Los valores de AndroidSdkVersions son el nivel de API: (AndroidSdkVersions)26 = AndroidApiLevel26.
        public static BootstrapState.Step ApplyAndroid(string id)
        {
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)AppIdentity.AndroidMinSdk;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.forceInternetPermission = true;
            return BootstrapState.Step.Ok(id, "minSdk " + AppIdentity.AndroidMinSdk + ", target automático, ARM64, Internet obligatorio");
        }

        // iOS 15.0, como el legacy.
        public static BootstrapState.Step ApplyIos(string id)
        {
            PlayerSettings.iOS.targetOSVersionString = AppIdentity.IosMinimumVersion;
            return BootstrapState.Step.Ok(id, "iOS " + AppIdentity.IosMinimumVersion);
        }

        // Linear; Android Vulkan + GLES3; Windows DX11; Apple con Metal (el valor por defecto, no se toca).
        public static BootstrapState.Step ApplyRendering(string id)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            return BootstrapState.Step.Ok(id, "Linear; Android Vulkan+GLES3; Windows DX11; Apple Metal");
        }

        // runInBackground (ARCHITECTURE §6.1) y Frame Timing Stats (adaptación térmica, §20).
        public static BootstrapState.Step ApplyRuntime(string id)
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.enableFrameTimingStats = true;
            return BootstrapState.Step.Ok(id, "runInBackground y Frame Timing Stats");
        }

        // Paso player.window (v3, 02-tech-design §1.3): ventana de Windows 1280 × 720 redimensionable (Q-88), sin el splash
        // del motor (Unity 6 lo permite en Personal; P-01) y con el fondo bg.app desde el primer cuadro (AC-APP-01-07).
        // Android: barras del sistema visibles como el legacy (styles.xml windowFullscreen=false; Unity arranca en true) y
        // dibujo detrás del recorte (= windowLayoutInDisplayCutoutMode=shortEdges; QA-G2-06). No necesita el módulo de Android.
        public static BootstrapState.Step ApplyWindow(string id)
        {
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = RowMate.Presentation.Controls.TokenColors.ToColor(RowMate.Core.Design.Tokens.BgApp);
            PlayerSettings.Android.startInFullscreen = false;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            return BootstrapState.Step.Ok(id, "ventana 1280×720 redimensionable; sin splash; fondo bg.app; Android con barras del sistema visibles");
        }

        // Manejador de entrada = Input System. No hay API pública: SerializedObject sobre ProjectSettings.asset.
        public static BootstrapState.Step ApplyInputHandler(string id)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                throw new InvalidOperationException("no se pudo abrir ProjectSettings/ProjectSettings.asset");
            }

            var serialized = new SerializedObject(assets[0]);
            var property = serialized.FindProperty("activeInputHandler");
            if (property == null)
            {
                throw new InvalidOperationException("ProjectSettings.asset no tiene activeInputHandler");
            }

            if (property.intValue == InputSystemHandler)
            {
                return BootstrapState.Step.Skipped(id, "ya era Input System");
            }

            property.intValue = InputSystemHandler;
            serialized.ApplyModifiedProperties();
            const string Message = "Manejador de entrada cambiado a Input System: reiniciá el Editor.";
            Debug.LogWarning(ProjectBootstrapper.LogPrefix + Message);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("RowMate", Message, "Entendido");
            }

            return BootstrapState.Step.Ok(id, "Input System (reiniciar el Editor)");
        }

        // R-01: verifica que el modo simulador NO esté en Standalone, Android ni iOS. Si está, advierte;
        // el bootstrapper nunca lo agrega ni lo saca: eso lo hace solo SimulatorDefine (menú RowMate/Modo simulador y la
        // corrida de tests del simulador, QA-V-05), y SimulatorBuildGuard frena los builds de distribución.
        public static BootstrapState.Step CheckSimulatorDefine(string id)
        {
            var found = "";
            for (var i = 0; i < PlayerTargets.Length; i++)
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbols(PlayerTargets[i]);
                var parts = symbols.Split(';');
                for (var j = 0; j < parts.Length; j++)
                {
                    if (parts[j].Trim() == SimulatorDefine)
                    {
                        found += (found.Length > 0 ? ", " : "") + PlayerTargets[i].TargetName;
                    }
                }
            }

            if (found.Length == 0)
            {
                return BootstrapState.Step.Ok(id, SimulatorDefine + " ausente en Standalone, Android e iOS");
            }

            Debug.LogWarning(ProjectBootstrapper.LogPrefix + SimulatorDefine + " está definido en: " + found
                + ". Ningún build de distribución puede llevarlo (R-01).");
            return BootstrapState.Step.Ok(id, "ADVERTENCIA: " + SimulatorDefine + " en " + found);
        }
    }
}
