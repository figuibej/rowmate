using System;
using System.IO;
using UnityEditor;

namespace RowMate.EditorTools
{
    // Qué build pidió tools/build/build.ps1 (ARCHITECTURE §19, 02-tech-design §1.1): -rowmateTarget Windows|Android,
    // -rowmateSimulator, -rowmateBackend Mono|IL2CPP, -rowmateOutput <carpeta>, -rowmateSmokeTest. Con el simulador el build
    // es SIEMPRE Development (R-01, AC-BLE-13-01). Mono es temporal (D-16: no está el módulo IL2CPP de Windows).
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

        public string Target { get; }

        public bool Simulator { get; }

        public string Backend { get; }

        public string OutputFolder { get; }

        public bool SmokeTest { get; }

        // Development siempre con el simulador; también para la prueba de humo (lee el registro).
        public bool Development => Simulator || SmokeTest;

        public bool IsWindows => string.Equals(Target, "Windows", StringComparison.OrdinalIgnoreCase);

        public BuildTarget BuildTarget => IsWindows ? BuildTarget.StandaloneWindows64 : BuildTarget.Android;

        public BuildTargetGroup BuildTargetGroup => IsWindows ? BuildTargetGroup.Standalone : BuildTargetGroup.Android;

        public ScriptingImplementation ScriptingBackend =>
            string.Equals(Backend, "IL2CPP", StringComparison.OrdinalIgnoreCase) ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x;

        public string Variant => Simulator ? "simulator" : "normal";

        // Builds/<Windows|Android>/<normal|simulator>/RowMate.exe (o .apk).
        public string LocationPath => Path.Combine(OutputFolder, IsWindows ? "RowMate.exe" : "RowMate.apk");

        public static BuildRequest FromCommandLine(string[] args)
        {
            var target = Value(args, "-rowmateTarget") ?? "Windows";
            var simulator = Has(args, "-rowmateSimulator");
            var backend = Value(args, "-rowmateBackend") ?? "Mono";
            var smoke = Has(args, "-rowmateSmokeTest");
            var output = Value(args, "-rowmateOutput") ?? Path.Combine("Builds", target, simulator ? "simulator" : "normal");
            return new BuildRequest(target, simulator, backend, output, smoke);
        }

        private static bool Has(string[] args, string name)
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

        private static string? Value(string[] args, string name)
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
