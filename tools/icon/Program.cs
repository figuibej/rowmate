using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using RowMate.Core.Design;

namespace RowMate.Icon
{
    // Genera o chequea el ícono de RowMate (It. 14, DT14-06; 02-tech-design §2.2) desde Core/Design/AppIcon* (geometría propia y
    // procedural, PNG con deflate propio: los mismos bytes en cada corrida y en cada plataforma):
    //   Assets/RowMate/Art/Identity/**                                  Windows, Android, arranque, iOS y macOS
    //   docs/migration/iterations/14-pulido-plataformas/snapshots/      CAP-01.png y CAP-02.png
    //   sin argumentos   escribe todos los archivos (solo los que cambian)
    //   --check          no escribe; sale con 1 si alguno difiere de lo generado (o falta)
    //   --root <ruta>    raíz del repositorio (por defecto: sube desde el directorio actual hasta ProjectSettings/ProjectVersion.txt)
    // Uso: "C:/Program Files/dotnet/dotnet.exe" run --project tools/dotnet/RowMate.Icon --artifacts-path tools/dotnet/.artifacts
    // En el CI lo comprueba AppIconAssetsAreCurrentTests (ci-core).
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            var check = false;
            string? root = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--check")
                {
                    check = true;
                }
                else if (args[i] == "--root" && i + 1 < args.Length)
                {
                    root = args[++i];
                }
                else
                {
                    Console.Error.WriteLine("[icon] argumento desconocido: " + args[i]);
                    Console.Error.WriteLine("[icon] uso: RowMate.Icon [--check] [--root <ruta>]");
                    return 2;
                }
            }

            root ??= FindRoot(Directory.GetCurrentDirectory()) ?? FindRoot(AppContext.BaseDirectory);
            if (root == null)
            {
                Console.Error.WriteLine("[icon] no encontré la raíz del repositorio (ProjectSettings/ProjectVersion.txt). Usá --root.");
                return 2;
            }

            var watch = Stopwatch.StartNew();
            var outputs = AppIconOutputs.All();
            var stale = 0;
            var written = 0;
            foreach (var output in outputs)
            {
                var path = Path.Combine(root, output.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var current = File.Exists(path) ? File.ReadAllBytes(path) : null;
                if (current != null && Same(current, output.Bytes))
                {
                    continue;
                }

                if (check)
                {
                    stale++;
                    Console.Error.WriteLine("[icon] " + output.RelativePath + (current == null ? " no existe." : " no coincide con Core/Design/AppIcon*."));
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, output.Bytes);
                written++;
                Console.WriteLine("[icon] escrito " + output.RelativePath + " (" + output.Bytes.Length + " bytes).");
            }

            if (check && stale > 0)
            {
                Console.Error.WriteLine("[icon] Regenerá con:");
                Console.Error.WriteLine("[icon]   \"C:/Program Files/dotnet/dotnet.exe\" run --project tools/dotnet/RowMate.Icon --artifacts-path tools/dotnet/.artifacts");
                return 1;
            }

            Console.WriteLine("[icon] " + outputs.Count + " archivos, " + (check ? "al día" : written + " escritos") + " (" + watch.ElapsedMilliseconds + " ms).");
            return 0;
        }

        private static bool Same(byte[] a, byte[] b)
        {
            return a.AsSpan().SequenceEqual(b);
        }

        private static string? FindRoot(string start)
        {
            var directory = new DirectoryInfo(Path.GetFullPath(start));
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
