using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // AC-BASE-10: revisión estática del bootstrapper del Editor (02-tech-design §1.5), que dotnet no compila (lo compila
    // Unity). Se lee el texto sin comentarios de Assets/RowMate/Editor/Bootstrap/*.cs, y el estado que dejó el primer open
    // real (It. 2) en ProjectSettings/RowMateBootstrap.json.
    [TestFixture]
    public sealed class BootstrapperStaticTests
    {
        private static readonly string[] Files =
        {
            "ProjectBootstrapper.cs", "BootstrapState.cs", "PlayerSettingsStep.cs", "UrpProvisioner.cs", "BootstrapPostprocessor.cs",
        };

        private static readonly string[] StepIds =
        {
            "player.identity", "player.scripting", "player.android", "player.ios", "player.rendering", "player.runtime",
            "player.input", "urp.provisional", "urp.tidy", "player.window", "ui.fonts", "ui.panel", "scene.boot", "defines.check",
        };

        [Test, Category("AC-BASE-10")]
        public void HasInitializeOnLoadStateFileAndMenu()
        {
            var main = Code("ProjectBootstrapper.cs");
            Assert.That(main, Does.Contain("[InitializeOnLoad]"));
            Assert.That(main, Does.Contain("EditorApplication.delayCall"));
            Assert.That(main, Does.Contain("\"ProjectSettings/RowMateBootstrap.json\""));
            Assert.That(main, Does.Contain("[MenuItem(\"RowMate/Bootstrap/Reaplicar\")]"));
            Assert.That(main, Does.Contain("CurrentVersion = 3"), "It. 2 (WP-B): bootstrapper v3, 02-tech-design §1.3");
        }

        [Test, Category("AC-BASE-10")]
        public void RunsTheStepsEachIsolated()
        {
            var main = Code("ProjectBootstrapper.cs");
            foreach (var id in StepIds)
            {
                Assert.That(main, Does.Contain("RunStep(results, \"" + id + "\""), id);
            }

            Assert.That(Regex.Matches(main, @"RunStep\(results,").Count, Is.EqualTo(StepIds.Length));
            Assert.That(main, Does.Contain("catch (Exception e)"));
        }

        [Test, Category("AC-BASE-10"), Category("AC-APP-16-01")]
        public void UsesAppIdentity()
        {
            // Toda constante de identidad sale de AppIdentity (testeada en dotnet), sin literales repetidos.
            var all = AllCode();
            foreach (var member in new[] { "CompanyName", "ProductName", "Version", "BuildNumber", "AndroidApplicationId", "AppleBundleId",
                "AndroidMinSdk", "IosMinimumVersion" })
            {
                Assert.That(all, Does.Contain("AppIdentity." + member), member);
            }

            foreach (var literal in StringLiterals())
            {
                // Las rutas de paquetes de Unity (Packages/com.unity.…) no son identificadores de la app.
                var withoutPackages = Regex.Replace(literal, @"Packages/com\.unity\.[a-z.-]+/", "Packages/<paquete>/");
                Assert.That(withoutPackages, Does.Not.Contain("com."), "literal de identificador en el bootstrapper: \"" + literal + "\"");
                Assert.That(literal, Does.Not.Contain("iguisoft"), "literal de empresa en el bootstrapper");
            }
        }

        [Test, Category("AC-BASE-10"), Category("AC-APP-16-02")]
        public void DoesNotApplyTheReservedWindowsId()
        {
            // AC-APP-16-02 (QA-02): WindowsApplicationId es solo un dato para el MSIX de la It. 14.
            Assert.That(AllCode(), Does.Not.Contain("WindowsApplicationId"));
            var identity = Code("PlayerSettingsStep.cs");
            Assert.That(identity, Does.Contain("SetApplicationIdentifier(NamedBuildTarget.Android, AppIdentity.AndroidApplicationId)"));
            Assert.That(identity, Does.Contain("SetApplicationIdentifier(NamedBuildTarget.iOS, AppIdentity.AppleBundleId)"));
            Assert.That(identity, Does.Contain("SetApplicationIdentifier(NamedBuildTarget.Standalone, AppIdentity.AppleBundleId)"));
        }

        // QA-V-03 de la It. 1: si un paso falla, se reintenta en el PRÓXIMO OPEN, no en cada recarga de dominio.
        // [InitializeOnLoad] corre el constructor estático en cada recompilación: el intento se marca en SessionState
        // (sobrevive a las recargas, se borra al cerrar Unity) y el constructor no agenda nada si ya está marcado.
        [Test, Category("AC-BASE-10"), Category("QA-V-03")]
        public void RetriesAFailedStepOnlyOnTheNextOpen()
        {
            var main = Code("ProjectBootstrapper.cs");
            var constructor = Body(main, "static ProjectBootstrapper()");
            Assert.That(constructor, Does.Match(@"if\s*\(\s*SessionState\.GetBool\(\s*AttemptedThisSessionKey\s*,\s*false\s*\)\s*\)\s*\{\s*return;\s*\}"),
                "el constructor estático sale si la sesión ya lo intentó");
            Assert.That(constructor, Does.Contain("EditorApplication.delayCall += RunOncePerSession;"));
            Assert.That(constructor, Does.Not.Contain("Run("), "el constructor no corre los pasos directamente");

            var once = Body(main, "private static void RunOncePerSession()");
            var busy = once.IndexOf("IsBusy()", StringComparison.Ordinal);
            var mark = once.IndexOf("SessionState.SetBool(AttemptedThisSessionKey, true);", StringComparison.Ordinal);
            var run = once.IndexOf("Run(false);", StringComparison.Ordinal);
            Assert.That(busy, Is.GreaterThanOrEqualTo(0), "si el Editor está ocupado, reagenda sin marcar");
            Assert.That(mark, Is.GreaterThan(busy), "marca la sesión después de ver que el Editor no está ocupado");
            Assert.That(run, Is.GreaterThan(mark), "corre los pasos después de marcar la sesión");
            Assert.That(main, Does.Match(@"AttemptedThisSessionKey\s*=\s*""RowMate\.Bootstrap\.AttemptedThisSession"""));

            // El menú fuerza la corrida aunque la sesión ya lo haya intentado.
            Assert.That(Body(main, "private static void Reapply()"), Does.Contain("Run(true);"));
        }

        [Test, Category("AC-BASE-10")]
        public void NeverChangesScriptingDefines()
        {
            // R-01: el bootstrapper no activa ni saca el modo simulador. Solo lo hace SimulatorDefine (QA-V-05).
            var all = AllCode();
            Assert.That(all, Does.Not.Contain("SetScriptingDefineSymbols"));
            Assert.That(all, Does.Contain("GetScriptingDefineSymbols"));
        }

        [Test, Category("AC-BASE-10")]
        public void DoesNotFixFinalQualityLevels()
        {
            // QA-P-10: un solo asset provisorio; no crea, borra ni renombra niveles de calidad.
            var all = AllCode();
            Assert.That(all, Does.Not.Match(@"RowMate_URP_(High|Medium|Low)"));
            Assert.That(all, Does.Not.Match(@"QualitySettings\.names\s*="));
            Assert.That(all, Does.Not.Contain("IncreaseLevel"));
            Assert.That(all, Does.Not.Contain("DecreaseLevel"));
            Assert.That(Code("UrpProvisioner.cs"), Does.Contain("/RowMate_URP_Default.asset\""));
            Assert.That(Code("UrpProvisioner.cs"), Does.Contain("UniversalRenderPipelineAsset.Create("));
        }

        [Test, Category("AC-BASE-10"), Category("AC-APP-16-04"), Category("AC-APP-16-05")]
        public void PlatformMinimumsComeFromAppIdentity()
        {
            var player = Code("PlayerSettingsStep.cs");
            Assert.That(player, Does.Contain("minSdkVersion = (AndroidSdkVersions)AppIdentity.AndroidMinSdk"));
            Assert.That(player, Does.Contain("targetArchitectures = AndroidArchitecture.ARM64"));
            Assert.That(player, Does.Contain("targetOSVersionString = AppIdentity.IosMinimumVersion"));
            Assert.That(player, Does.Contain("ScriptingImplementation.IL2CPP"));
            Assert.That(player, Does.Contain("ApiCompatibilityLevel.NET_Standard"));
            Assert.That(player, Does.Contain("ColorSpace.Linear"));
            Assert.That(player, Does.Contain("runInBackground = true"));
        }

        // Primer open real (It. 2, 00-primer-open.md): con -batchmode el delayCall no corre antes de salir, así que el
        // disparo en batchmode es un AssetPostprocessor al final del refresh de cada recarga de dominio, con la misma marca
        // de sesión que el Editor con ventana (QA-V-03) y sin mirar IsBusy (dentro del postprocesado isUpdating es true).
        [Test, Category("AC-BASE-10"), Category("QA-V-03")]
        public void RunsInBatchModeAfterTheDomainReload()
        {
            var post = Code("BootstrapPostprocessor.cs");
            Assert.That(post, Does.Match(@"class\s+BootstrapPostprocessor\s*:\s*AssetPostprocessor"));
            Assert.That(post, Does.Match(@"static\s+void\s+OnPostprocessAllAssets\([^)]*\bbool\s+didDomainReload\s*\)"));
            Assert.That(post, Does.Match(@"if\s*\(\s*didDomainReload\s*&&\s*Application\.isBatchMode\s*\)"),
                "solo en batchmode: el Editor con ventana sigue con delayCall");
            Assert.That(post, Does.Contain("ProjectBootstrapper.RunInBatchMode();"));

            var main = Code("ProjectBootstrapper.cs");
            var batch = Body(main, "internal static void RunInBatchMode()");
            var check = batch.IndexOf("SessionState.GetBool(AttemptedThisSessionKey, false)", StringComparison.Ordinal);
            var mark = batch.IndexOf("SessionState.SetBool(AttemptedThisSessionKey, true);", StringComparison.Ordinal);
            var apply = batch.IndexOf("Apply(false);", StringComparison.Ordinal);
            Assert.That(check, Is.GreaterThanOrEqualTo(0), "sale si la sesión ya lo intentó");
            Assert.That(mark, Is.GreaterThan(check));
            Assert.That(apply, Is.GreaterThan(mark), "marca la sesión antes de aplicar");
            Assert.That(batch, Does.Not.Contain("IsBusy()"), "en el postprocesado el Editor siempre figura ocupado");
            Assert.That(batch, Does.Not.Contain("delayCall"));
        }

        // Versión 2 (primer open): el renderer lleva el PostProcessData por defecto de URP, como el menú de URP. Sin él, URP
        // apaga el postprocesado del renderer (bloom, tonemapping y ajustes de color de ART_BIBLE).
        [Test, Category("AC-BASE-10")]
        public void RendererHasTheDefaultPostProcessData()
        {
            var urp = Code("UrpProvisioner.cs");
            Assert.That(urp, Does.Contain("\"Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset\""));
            Assert.That(Regex.Matches(urp, @"renderer\.postProcessData\s*=\s*LoadDefaultPostProcessData\(\);").Count, Is.EqualTo(2),
                "al crear el renderer y al completar uno de la versión 1");
            Assert.That(urp, Does.Match(@"else\s+if\s*\(\s*renderer\.postProcessData\s*==\s*null\s*\)"));
        }

        // AC-BASE-10 con el estado que dejó Unity (ProjectSettings/RowMateBootstrap.json se versiona). Si alguien sube
        // CurrentVersion, este test queda rojo hasta abrir el proyecto en Unity y versionar el estado nuevo.
        [Test, Category("AC-BASE-10")]
        public void RanOnTheFirstOpenAndLeftItsState()
        {
            var path = RepoPaths.Combine("ProjectSettings/RowMateBootstrap.json");
            Assert.That(File.Exists(path), Is.True, "falta ProjectSettings/RowMateBootstrap.json: abrir el proyecto en Unity 6000.3.27f1");
            var state = JsonParser.Parse(File.ReadAllText(path));
            var current = Regex.Match(Code("ProjectBootstrapper.cs"), @"CurrentVersion\s*=\s*(\d+);");
            Assert.That(current.Success, Is.True);
            Assert.That(state["version"].AsLong(), Is.EqualTo(long.Parse(current.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)),
                "la versión aplicada es la actual: ningún paso falló");
            Assert.That(state["editorVersion"].AsString(), Is.EqualTo("6000.3.27f1"));

            var steps = state["steps"].Items;
            Assert.That(steps.Count, Is.EqualTo(StepIds.Length));
            for (var i = 0; i < steps.Count; i++)
            {
                Assert.That(steps[i]["id"].AsString(), Is.EqualTo(StepIds[i]), "orden de los pasos");
                Assert.That(steps[i]["status"].AsString(), Is.EqualTo("ok").Or.EqualTo("skipped"), StepIds[i] + ": " + steps[i]["detail"].AsString());
            }

            Assert.That(steps[StepIds.Length - 1]["detail"].AsString(), Does.Contain("ausente"), "R-01: el modo simulador no quedó puesto");
        }

        private static string Code(string file)
        {
            var path = RepoPaths.Combine("Assets/RowMate/Editor/Bootstrap", file);
            Assert.That(File.Exists(path), Is.True, "falta " + RepoPaths.Relative(path));
            return SourceScan.StripComments(File.ReadAllText(path));
        }

        // Cuerpo (entre llaves balanceadas) del miembro cuya firma empieza con `signature`.
        internal static string Body(string code, string signature)
        {
            var start = code.IndexOf(signature, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "falta " + signature);
            var open = code.IndexOf('{', start);
            var depth = 0;
            for (var i = open; i < code.Length; i++)
            {
                if (code[i] == '{')
                {
                    depth++;
                }
                else if (code[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return code.Substring(open, i - open + 1);
                    }
                }
            }

            Assert.Fail("llaves sin cerrar en " + signature);
            return "";
        }

        private static string AllCode()
        {
            var all = "";
            foreach (var file in Files)
            {
                all += Code(file) + "\n";
            }

            return all;
        }

        private static IEnumerable<string> StringLiterals()
        {
            var literal = new Regex(@"""((?:[^""\\\n]|\\.)*)""");
            foreach (var file in Files)
            {
                foreach (Match match in literal.Matches(Code(file)))
                {
                    yield return match.Groups[1].Value;
                }
            }
        }
    }
}
