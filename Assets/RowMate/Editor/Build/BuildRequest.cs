using System;
using System.IO;
using UnityEditor;

namespace RowMate.EditorTools
{
    // Qué build pidió tools/build/build.ps1 (ARCHITECTURE §19, 02-tech-design §1.1): -rowmateTarget Windows|Android,
    // -rowmateSimulator, -rowmateBackend Mono|IL2CPP, -rowmateOutput <carpeta>, -rowmateSmokeTest. Con el simulador el build
    // es SIEMPRE Development (R-01, AC-BLE-13-01). Mono es temporal (D-16: no está el módulo IL2CPP de Windows).
    // It. 14 (14-pulido-plataformas/02-tech-design §8.1; AC-APP-19-06, -08, -09): también -rowmateTarget iOS|macOS (el job manual de
    // Apple del CI, sin firmar, nunca Development) y la firma de Android por argumentos (-androidKeystoreName, -androidKeystorePass,
    // -androidKeyaliasName, -androidKeyaliasPass, los que pasa GameCI; vacíos = la llave de depuración). Nada se versiona.
    public sealed class BuildRequest
    {
        public BuildRequest(string target, bool simulator, string backend, string outputFolder, bool smokeTest)
        {
            Target = target;
            Simulator = simulator;
            Backend = backend;
            OutputFolder = outputFolder;
            SmokeTest = smokeTest;
        }

        // La firma de Android por argumentos (null o vacía = depuración). Solo en memoria durante el build (BuildCli la restaura).
        public AndroidSigning? Signing { get; set; }

        public string Target { get; }

        public bool Simulator { get; }

        public string Backend { get; }

        public string OutputFolder { get; }

        public bool SmokeTest { get; }

        // Development siempre con el simulador; también para la prueba de humo (lee el registro).
        public bool Development => Simulator || SmokeTest;

        public bool IsWindows => string.Equals(Target, "Windows", StringComparison.OrdinalIgnoreCase);

        public bool IsIos => string.Equals(Target, "iOS", StringComparison.OrdinalIgnoreCase);

        public bool IsMac => string.Equals(Target, "macOS", StringComparison.OrdinalIgnoreCase);

        public bool IsAndroid => !IsWindows && !IsIos && !IsMac;

        public BuildTarget BuildTarget => IsWindows ? BuildTarget.StandaloneWindows64 : IsIos ? BuildTarget.iOS : IsMac ? BuildTarget.StandaloneOSX : BuildTarget.Android;

        public BuildTargetGroup BuildTargetGroup => IsWindows || IsMac ? BuildTargetGroup.Standalone : IsIos ? BuildTargetGroup.iOS : BuildTargetGroup.Android;

        public ScriptingImplementation ScriptingBackend =>
            string.Equals(Backend, "IL2CPP", StringComparison.OrdinalIgnoreCase) ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x;

        public string Variant => Simulator ? "simulator" : "normal";

        // Builds/<Windows|Android>/<normal|simulator|dev-smoke>/RowMate.exe (o .apk).
        // iOS: la carpeta del proyecto de Xcode; macOS: RowMate.app.
        public string LocationPath => IsIos ? OutputFolder : Path.Combine(OutputFolder, IsWindows ? "RowMate.exe" : IsMac ? "RowMate.app" : "RowMate.apk");

        // La carpeta fija de cada variante (QA-V3-08 de la It. 3: un build de desarrollo abre el puerto de PlayerConnection y
        // Windows pregunta por el Firewall una vez por ruta de RowMate.exe). La misma que usa tools/build/build.ps1.
        public static string DefaultOutput(string target, bool simulator, bool smokeTest)
        {
            return Path.Combine("Builds", target, simulator ? "simulator" : smokeTest ? "dev-smoke" : "normal");
        }

        public static BuildRequest FromCommandLine(string[] args)
        {
            var target = Value(args, "-rowmateTarget") ?? "Windows";
            var simulator = Has(args, "-rowmateSimulator");
            var backend = Value(args, "-rowmateBackend") ?? "Mono";
            var smoke = Has(args, "-rowmateSmokeTest");
            var output = Value(args, "-rowmateOutput") ?? DefaultOutput(target, simulator, smoke);
            return new BuildRequest(target, simulator, backend, output, smoke) { Signing = AndroidSigning.FromCommandLine(args) };
        }

        internal static bool Has(string[] args, string name)
        {
            foreach (var arg in args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string? Value(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
