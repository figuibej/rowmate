using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace RowMate.Tests.Support
{
    // Ubica los datos del repositorio igual en `dotnet test` y en el Editor de Unity (ARCHITECTURE §15.1).
    // La raíz es la carpeta que contiene ProjectSettings/ProjectVersion.txt. Orden de búsqueda:
    // variable ROWMATE_REPO_ROOT → AssemblyMetadata "RowMateRepoRoot" (solo dotnet) → subir desde
    // TestContext.CurrentContext.TestDirectory → desde Directory.GetCurrentDirectory() → desde AppContext.BaseDirectory.
    public static class RepoPaths
    {
        private const string MarkerRelative = "ProjectSettings/ProjectVersion.txt";
        private static readonly object Gate = new object();
        private static string? _root;

        public static string Root
        {
            get
            {
                if (_root != null)
                {
                    return _root;
                }

                lock (Gate)
                {
                    if (_root == null)
                    {
                        _root = FindRoot();
                    }

                    return _root;
                }
            }
        }

        // <root>/Assets/RowMate
        public static string RowMateAssets => Combine("Assets", "RowMate");

        // <root>/Assets/RowMate/Tests/Golden/oracle/<file>
        public static string Golden(string fileName)
        {
            return Combine("Assets", "RowMate", "Tests", "Golden", "oracle", fileName);
        }

        // <root>/Assets/RowMate/Localization/strings.<lang>.json
        public static string Catalog(string languageCode)
        {
            return Combine("Assets", "RowMate", "Localization", "strings." + languageCode + ".json");
        }

        // Ruta relativa a Root. Cada parte puede traer '/' (se normaliza al separador del sistema).
        public static string Combine(params string[] relativeParts)
        {
            var path = Root;
            for (var i = 0; i < relativeParts.Length; i++)
            {
                var part = relativeParts[i].Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                path = Path.Combine(path, part);
            }

            return path;
        }

        // Ruta relativa a Root con '/' (para mensajes y comparaciones estables entre sistemas).
        public static string Relative(string fullPath)
        {
            var root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(fullPath);
            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                full = full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return full.Replace('\\', '/');
        }

        // Archivos bajo una carpeta relativa a Root (vacío si la carpeta no existe), en orden ordinal.
        public static IReadOnlyList<string> Files(string relativeFolder, string searchPattern)
        {
            var folder = Combine(relativeFolder);
            if (!Directory.Exists(folder))
            {
                return new string[0];
            }

            var files = Directory.GetFiles(folder, searchPattern, SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            return files;
        }

        private static string FindRoot()
        {
            var tried = new List<string>();

            var fromEnv = Environment.GetEnvironmentVariable("ROWMATE_REPO_ROOT");
            if (!string.IsNullOrEmpty(fromEnv))
            {
                tried.Add("ROWMATE_REPO_ROOT=" + fromEnv);
                if (IsRoot(fromEnv!))
                {
                    return Path.GetFullPath(fromEnv!);
                }
            }

            var fromMetadata = MetadataRoot();
            if (fromMetadata != null)
            {
                tried.Add("AssemblyMetadata=" + fromMetadata);
                if (IsRoot(fromMetadata))
                {
                    return Path.GetFullPath(fromMetadata);
                }
            }

            var starts = new List<string>();
            try
            {
                var testDirectory = TestContext.CurrentContext.TestDirectory;
                if (!string.IsNullOrEmpty(testDirectory))
                {
                    starts.Add(testDirectory);
                }
            }
            catch (Exception)
            {
                // Fuera de un contexto de NUnit no hay TestDirectory.
            }

            starts.Add(Directory.GetCurrentDirectory());
            starts.Add(AppContext.BaseDirectory);

            for (var i = 0; i < starts.Count; i++)
            {
                tried.Add(starts[i]);
                var found = WalkUp(starts[i]);
                if (found != null)
                {
                    return found;
                }
            }

            throw new DirectoryNotFoundException("RepoPaths: no encontré la raíz del repositorio (la carpeta con "
                + MarkerRelative + "). Probé: " + string.Join(" | ", tried) + ". Definí ROWMATE_REPO_ROOT.");
        }

        private static string? MetadataRoot()
        {
            var attributes = typeof(RepoPaths).Assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
            for (var i = 0; i < attributes.Length; i++)
            {
                var metadata = (AssemblyMetadataAttribute)attributes[i];
                if (metadata.Key == "RowMateRepoRoot" && !string.IsNullOrEmpty(metadata.Value))
                {
                    return metadata.Value;
                }
            }

            return null;
        }

        private static string? WalkUp(string start)
        {
            DirectoryInfo? directory;
            try
            {
                directory = new DirectoryInfo(Path.GetFullPath(start));
            }
            catch (Exception)
            {
                return null;
            }

            while (directory != null)
            {
                if (IsRoot(directory.FullName))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static bool IsRoot(string candidate)
        {
            try
            {
                return File.Exists(Path.Combine(candidate, "ProjectSettings", "ProjectVersion.txt"));
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
