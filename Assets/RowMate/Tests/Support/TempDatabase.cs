using System;
using System.IO;
using System.Threading;
using RowMate.Core.Common;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Routines;
using RowMate.Infrastructure.Threading;

namespace RowMate.Tests.Support
{
    // Base de prueba propia y temporal (spec §0.4, AC-HIS-20-04; 02-tech-design §2.4, §2.7): una carpeta por prueba en
    // <temp del sistema>/RowMate.Tests/<guid>-ñá/, que se borra en Dispose. La ñ y la á del nombre hacen que toda la suite
    // pruebe rutas con acentos, como un perfil de Windows con ñ (QA-G3-13 g).
    // Guardia (R3-10): nunca una ruta dentro de la carpeta de datos de la app (…/iguisoft/RowMate, la de
    // Application.persistentDataPath en Windows, macOS y Linux) ni un archivo llamado rower_app.db / rower_app_dev.db fuera de
    // la carpeta de pruebas. El mensaje nombra el archivo, nunca la carpeta (QA-G3-20).
    public sealed class TempDatabase : IDisposable
    {
        public const string DefaultFileName = "rowmate-test.db";
        public const string RootName = "RowMate.Tests";
        public const string AccentSuffix = "-ñá";

        private bool _disposed;

        public TempDatabase(string fileName = DefaultFileName)
        {
            Folder = System.IO.Path.Combine(Root, Guid.NewGuid().ToString("N") + AccentSuffix);
            Directory.CreateDirectory(Folder);
            FilePath = PathOf(fileName);
        }

        // <temp del sistema>/RowMate.Tests
        public static string Root => System.IO.Path.Combine(System.IO.Path.GetTempPath(), RootName);

        // La carpeta de esta prueba.
        public string Folder { get; }

        // El archivo de la base de esta prueba.
        public string FilePath { get; }

        // Otro archivo dentro de la carpeta de esta prueba (por ejemplo rower_app_dev.db para comparar variantes).
        public string PathOf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(new[] { '/', '\\' }) >= 0)
            {
                throw new ArgumentException("TempDatabase: nombre de archivo inválido.", nameof(fileName));
            }

            var path = System.IO.Path.Combine(Folder, fileName);
            Guard(path);
            return path;
        }

        // La base sobre FilePath (o el archivo dado), con el ejecutor dado o uno en línea.
        public RowMateDatabase Open(IAppLogger logger, IDbExecutor? executor = null, string? fileName = null)
        {
            return new RowMateDatabase(fileName == null ? FilePath : PathOf(fileName), executor ?? new InlineDbExecutor(), logger);
        }

        // El repositorio de la app sobre la base de esta prueba.
        public SqliteRoutineRepository Repository(IAppLogger logger, IDbExecutor? executor = null, string? fileName = null)
        {
            return new SqliteRoutineRepository(Open(logger, executor, fileName), logger);
        }

        // La base local de la app (It. 4a, §3.5): rutinas y sesiones sobre la MISMA RowMateDatabase, en el archivo de esta
        // prueba. Se dispone con LocalStore.Dispose (el ejecutor termina lo encolado y cierra la conexión).
        public LocalStore Store(IAppLogger logger, IDbExecutor? executor = null, string? fileName = null)
        {
            return new LocalStore(Open(logger, executor, fileName), logger);
        }

        // Lanza InvalidOperationException si la ruta cae en los datos del usuario (ver arriba). Lo usan también las fábricas
        // de bases de PlayMode y del arnés.
        public static void Guard(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var full = Normalize(System.IO.Path.GetFullPath(path));
            var name = System.IO.Path.GetFileName(full);
            var userData = "/" + Normalize(AppIdentity.CompanyName + "/" + AppIdentity.ProductName) + "/";
            if ((full + "/").IndexOf(userData, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException("TempDatabase: " + name + " está en la carpeta de datos de la app: las pruebas nunca la usan.");
            }

            var root = Normalize(System.IO.Path.GetFullPath(Root)).TrimEnd('/') + "/";
            var isAppName = string.Equals(name, DatabaseFiles.NormalName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, DatabaseFiles.SimulatorName, StringComparison.OrdinalIgnoreCase);
            if (isAppName && !full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("TempDatabase: " + name + " solo se usa dentro de la carpeta temporal de pruebas.");
            }
        }

        // Borra la carpeta de la prueba (con sus -wal y -shm). La base ya se tiene que haber cerrado.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (Directory.Exists(Folder))
                    {
                        Directory.Delete(Folder, true);
                    }

                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(50);
                }
            }
        }

        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
