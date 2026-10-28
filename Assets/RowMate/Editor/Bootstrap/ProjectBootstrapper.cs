using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RowMate.EditorTools
{
    // Preparación mínima del proyecto sin Editor (ADR 014, ARCHITECTURE §16, 02-tech-design §1.5).
    // Verificado en el primer open real (It. 2, 00-primer-open.md). Idempotente: cada paso ASIGNA valores (no suma ni
    // alterna), así correrlo dos veces deja el mismo estado. Toda constante de identidad sale de RowMate.Core.Common.AppIdentity.
    // - Un solo intento automático por sesión del Editor (QA-V-03 de la It. 1): [InitializeOnLoad] corre el constructor
    //   estático en CADA recarga de dominio (cada recompilación), así que el intento se marca en SessionState, que
    //   sobrevive a las recargas y se borra al cerrar Unity. El constructor solo agenda RunOncePerSession con
    //   EditorApplication.delayCall si la sesión todavía no lo intentó.
    // - En batchmode (-quit, -executeMethod, -runTests) el delayCall no llega a correr antes de salir o de empezar: el
    //   primer open real terminó con código 0 sin aplicar nada. Ahí dispara BootstrapPostprocessor al final del refresh
    //   de cada recarga de dominio (RunInBatchMode), con la misma marca de sesión.
    // - Estado en ProjectSettings/RowMateBootstrap.json: si la versión ya está aplicada, no hace nada.
    // - Cada paso corre en su propio try/catch: uno que falla queda "failed" y los demás siguen; si falla alguno,
    //   la versión no se marca como aplicada (se reintenta en el PRÓXIMO OPEN, no en cada recompilación) y la consola
    //   muestra los pasos manuales. El menú RowMate/Bootstrap/Reaplicar fuerza una corrida en cualquier momento.
    // - No crea niveles de calidad definitivos (QA-P-10) y nunca activa el modo simulador (R-01).
    // - En batchmode no abre diálogos: solo registra en la consola.
    [InitializeOnLoad]
    public static class ProjectBootstrapper
    {
        // Versiones (subirla hace que el próximo open reaplique todos los pasos, que son idempotentes):
        // 1 = bootstrapper mínimo de la It. 1. 2 = primer open real (It. 2): el renderer de URP lleva PostProcessData.
        // 3 = It. 2 (WP-B, 02-tech-design §1.3): assets de URP ordenados, ventana y splash, FontAsset, PanelSettings y la
        // escena Boot (urp.tidy, player.window, ui.fonts, ui.panel, scene.boot), siempre antes de defines.check.
        public const int CurrentVersion = 3;
        public const string StatePath = "ProjectSettings/RowMateBootstrap.json";
        public const string LogPrefix = "[BOOTSTRAP] ";

        // Clave de SessionState: "ya hubo un intento automático en esta sesión del Editor".
        private const string AttemptedThisSessionKey = "RowMate.Bootstrap.AttemptedThisSession";

        static ProjectBootstrapper()
        {
            if (SessionState.GetBool(AttemptedThisSessionKey, false))
            {
                return;
            }

            EditorApplication.delayCall += RunOncePerSession;
        }

        [MenuItem("RowMate/Bootstrap/Reaplicar")]
        private static void Reapply()
        {
            Run(true);
        }

        // -executeMethod RowMate.EditorTools.ProjectBootstrapper.ApplyFromCommandLine -quit: aplica la versión actual fuera del
        // postprocesado de importación (sin forzar si ya está aplicada) y sale con 1 si algún paso falló.
        public static void ApplyFromCommandLine()
        {
            Apply(false);
            var state = BootstrapState.Load(StatePath);
            if (state == null || state.version < CurrentVersion)
            {
                EditorApplication.Exit(1);
            }
        }

        // -executeMethod RowMate.EditorTools.ProjectBootstrapper.ReapplyFromCommandLine -quit: vuelve a aplicar TODOS los pasos
        // aunque la versión ya esté aplicada (son idempotentes), como el menú Reaplicar. Lo usó la It. 3 (B2) para crear el
        // FontAsset de Noto Emoji, asignar los respaldos de UiAssets y apagar getFontFeatures (DN3-24) sin subir la versión.
        // Sale con 1 si algún paso falló.
        public static void ReapplyFromCommandLine()
        {
            Apply(true);
            var state = BootstrapState.Load(StatePath);
            if (state == null || state.version < CurrentVersion)
            {
                EditorApplication.Exit(1);
            }
        }

        // Intento automático: se marca en SessionState recién cuando de verdad corre (si el Editor está ocupado se reagenda;
        // si en el medio hay una recarga de dominio, el constructor estático lo vuelve a agendar porque la marca no está).
        private static void RunOncePerSession()
        {
            if (IsBusy())
            {
                EditorApplication.delayCall += RunOncePerSession;
                return;
            }

            if (SessionState.GetBool(AttemptedThisSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(AttemptedThisSessionKey, true);
            Run(false);
        }

        private static bool IsBusy()
        {
            return EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;
        }

        // Intento automático en batchmode: lo llama BootstrapPostprocessor después de cada recarga de dominio. Corre en el
        // acto, porque dentro de OnPostprocessAllAssets el Editor figura ocupado (isUpdating) y reagendar sería no correr.
        internal static void RunInBatchMode()
        {
            if (SessionState.GetBool(AttemptedThisSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(AttemptedThisSessionKey, true);
            Apply(false);
        }

        public static void Run(bool force)
        {
            // Si el Editor está compilando, importando o entrando en play mode, se reagenda.
            if (IsBusy())
            {
                EditorApplication.delayCall += () => Run(force);
                return;
            }

            Apply(force);
        }

        private static void Apply(bool force)
        {
            var previous = BootstrapState.Load(StatePath);
            if (!force && previous != null && previous.version >= CurrentVersion)
            {
                // Una línea por sesión del Editor: deja ver en el log que el intento automático corrió y no tuvo nada que hacer.
                Debug.Log(LogPrefix + "versión " + CurrentVersion + " ya aplicada (" + StatePath + "): sin cambios.");
                return;
            }

            var results = new List<BootstrapState.Step>();

            // El bootstrapper es de los pocos que escriben los FontAsset generados y los assets de URP (QA-V2-07).
            using (GeneratedAssetGuard.AllowWrites())
            {
                RunStep(results, "player.identity", PlayerSettingsStep.ApplyIdentity);
                RunStep(results, "player.scripting", PlayerSettingsStep.ApplyScripting);
                RunStep(results, "player.android", PlayerSettingsStep.ApplyAndroid);
                RunStep(results, "player.ios", PlayerSettingsStep.ApplyIos);
                RunStep(results, "player.rendering", PlayerSettingsStep.ApplyRendering);
                RunStep(results, "player.runtime", PlayerSettingsStep.ApplyRuntime);
                RunStep(results, "player.input", PlayerSettingsStep.ApplyInputHandler);
                RunStep(results, "urp.provisional", UrpProvisioner.Apply);
                RunStep(results, "urp.tidy", UrpProvisioner.Tidy);
                RunStep(results, "player.window", PlayerSettingsStep.ApplyWindow);
                RunStep(results, "ui.fonts", FontAssetBuilder.Apply);
                RunStep(results, "ui.panel", UiAssetsBuilder.Apply);
                RunStep(results, "scene.boot", BootSceneBuilder.Apply);
                RunStep(results, "defines.check", PlayerSettingsStep.CheckSimulatorDefine);

                AssetDatabase.SaveAssets();
            }

            var failed = 0;
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].status == BootstrapState.StatusFailed)
                {
                    failed++;
                }
            }

            var state = new BootstrapState
            {
                version = failed == 0 ? CurrentVersion : (previous != null ? previous.version : 0),
                editorVersion = Application.unityVersion,
                appliedAtUtc = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                steps = results
            };
            state.Save(StatePath);

            var summary = new StringBuilder(LogPrefix);
            summary.Append("versión ").Append(CurrentVersion).Append(failed == 0 ? " aplicada" : " con " + failed + " paso(s) fallido(s)");
            summary.Append(" (").Append(StatePath).Append(")");
            for (var i = 0; i < results.Count; i++)
            {
                summary.Append("\n  ").Append(results[i].id).Append(": ").Append(results[i].status);
                if (results[i].detail.Length > 0)
                {
                    summary.Append(" - ").Append(results[i].detail);
                }
            }

            if (failed == 0)
            {
                Debug.Log(summary.ToString());
            }
            else
            {
                Debug.LogWarning(summary.ToString());
            }
        }

        private static void RunStep(List<BootstrapState.Step> results, string id, Func<string, BootstrapState.Step> step)
        {
            try
            {
                results.Add(step(id));
            }
            catch (Exception e)
            {
                results.Add(BootstrapState.Step.Failed(id, e.GetType().Name + ": " + e.Message));
                Debug.LogError(LogPrefix + id + " falló: " + e.Message + "\n" + ManualInstructions(id));
            }
        }

        // Respaldo manual de cada paso si la API falla (R-T2).
        private static string ManualInstructions(string id)
        {
            switch (id)
            {
                case "urp.provisional":
                    return "Pasos manuales: 1) Assets > Create > Rendering > URP Asset (with Universal Renderer) en "
                        + UrpProvisioner.Folder + "; 2) Project Settings > Graphics > Default Render Pipeline = ese asset; "
                        + "3) Project Settings > Quality: asignarlo en cada nivel existente (sin crear ni borrar niveles).";
                case "player.input":
                    return "Paso manual: Project Settings > Player > Other Settings > Active Input Handling = Input System Package (New), y reiniciar el Editor.";
                case "urp.tidy":
                    return "Paso manual: mover (arrastrar en el Project) Assets/UniversalRenderPipelineGlobalSettings.asset y Assets/DefaultVolumeProfile.asset a "
                        + UrpProvisioner.Folder + " (el GUID se conserva).";
                case "player.window":
                    return "Paso manual: Project Settings > Player > Resolution: Windowed 1280 × 720, Resizable Window; Splash Image: Show Splash Screen apagado, "
                        + "fondo #0D1B2A; Android: Start in fullscreen apagado y Render outside safe area encendido.";
                case "ui.fonts":
                    return "Paso manual: por cada .ttf de Assets/RowMate/UI/Fonts e Icons, Create > Text Core > Font Asset (SDFAA, dinámico) con los tamaños de "
                        + "02-tech-design §1.3, guardado en la carpeta Generated con el nombre de FontAssetPaths; después Reimport de Typography.uss y del tema.";
                case "ui.panel":
                    return "Paso manual: Create > UI Toolkit > Panel Settings Asset en " + UiAssetsBuilder.Folder + "/RowMatePanel.asset (tema RowMateTheme.tss, "
                        + "Constant Pixel Size, escala 1, Clear Color #0D1B2A) y Create > RowMate > UI Assets con el panel y los dos strings.*.json.";
                case "scene.boot":
                    return "Paso manual: escena nueva vacía " + BootSceneBuilder.ScenePath + " con un GameObject AppRoot (AppRoot + UIDocument sin panel; "
                        + "_assets = UiAssets) y una cámara UiCamera (Solid Color #0D1B2A, Culling Mask Nothing); File > Build Profiles: única escena.";
                default:
                    return "Paso manual: Project Settings > Player, con los valores de ARCHITECTURE §16 (punto 1).";
            }
        }
    }
}
