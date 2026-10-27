using System;
using System.IO;
using System.Text;
using RowMate.Core.Design;

namespace RowMate.Tokens
{
    // Genera o chequea los USS que salen de Assets/RowMate/Core/Design/Tokens.cs (ADR 016; 02-tech-design de la It. 1 §4.5
    // y de la It. 2 §2.9):
    //   Assets/RowMate/Presentation/Styles/Tokens.uss       variables (UssTokenWriter)
    //   Assets/RowMate/Presentation/Styles/Typography.uss   clases .rm-type-*, .rm-icon y escalas de letra (TypographyUssWriter)
    //   sin argumentos   escribe los dos archivos (LF, UTF-8 sin BOM)
    //   --check          no escribe; sale con 1 si alguno difiere de lo generado (o falta)
    //   --root <ruta>    raíz del repositorio (por defecto: sube desde el directorio actual hasta ProjectSettings/ProjectVersion.txt)
    // Uso: "C:/Program Files/dotnet/dotnet.exe" run --project tools/dotnet/RowMate.Tokens --artifacts-path tools/dotnet/.artifacts
    internal static class Program
    {
        private const string TokensUssRelative = "Assets/RowMate/Presentation/Styles/Tokens.uss";
        private const string TypographyUssRelative = "Assets/RowMate/Presentation/Styles/Typography.uss";

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
                    Console.Error.WriteLine("[tokens] argumento desconocido: " + args[i]);
                    Console.Error.WriteLine("[tokens] uso: RowMate.Tokens [--check] [--root <ruta>]");
                    return 2;
                }
            }

            root ??= FindRoot(Directory.GetCurrentDirectory()) ?? FindRoot(AppContext.BaseDirectory);
            if (root == null)
            {
                Console.Error.WriteLine("[tokens] no encontré la raíz del repositorio (ProjectSettings/ProjectVersion.txt). Usá --root.");
                return 2;
            }

            var outputs = new[]
            {
                (Relative: TokensUssRelative, Text: UssTokenWriter.Write(TokenCatalog.All)),
                (Relative: TypographyUssRelative, Text: TypographyUssWriter.Write(TokenCatalog.All)),
            };

            if (check)
            {
                var stale = 0;
                foreach (var output in outputs)
                {
                    var path = PathOf(root, output.Relative);
                    var current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n") : null;
                    if (current == output.Text)
                    {
                        Console.WriteLine("[tokens] " + output.Relative + " al día (" + TokenCatalog.All.Count + " tokens).");
                        continue;
                    }

                    stale++;
                    Console.Error.WriteLine("[tokens] " + output.Relative + (current == null ? " no existe." : " no coincide con Tokens.cs."));
                }

                if (stale == 0)
                {
                    return 0;
                }

                Console.Error.WriteLine("[tokens] Regenerá con:");
                Console.Error.WriteLine("[tokens]   \"C:/Program Files/dotnet/dotnet.exe\" run --project tools/dotnet/RowMate.Tokens --artifacts-path tools/dotnet/.artifacts");
                return 1;
            }

            foreach (var output in outputs)
            {
                var path = PathOf(root, output.Relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, output.Text, new UTF8Encoding(false));
                Console.WriteLine("[tokens] escrito " + output.Relative + " (" + TokenCatalog.All.Count + " tokens).");
            }

            return 0;
        }

        private static string PathOf(string root, string relative)
        {
            return Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
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
