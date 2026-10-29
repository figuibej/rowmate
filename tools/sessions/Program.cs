using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Sessions;
using RowMate.Infrastructure.Threading;

namespace RowMate.SessionList
{
    // Herramienta de la demo (c) y de QA (spec §12.9; 02-tech-design §3.6 de la It. 4a): lista las sesiones de la base local,
    // la más nueva arriba, SIN tocar la base del usuario.
    //
    //   dotnet run --project tools/dotnet/RowMate.SessionList -- [--variant simulator|normal | --db <archivo>] [--verbose]
    //
    // 1. Nunca abre el archivo original con SQLite: copia la base (y su -wal, si está) a una carpeta temporal propia, con
    //    FileShare.ReadWrite (no bloquea a la app), abre la copia y la borra al terminar. Ni el contenido ni la fecha de la
    //    base cambian, y el WAL de una sesión cortada se lee igual. Si la copia no se puede abrir o leer (la app escribía en ese
    //    instante), copia de nuevo, hasta dos veces.
    // 2. Lista con SqliteSessionRepository.ListAsync(): id · inicio (hora local) · fin (o —) · rutina (o libre) · puntos.
    // 3. Ninguna ruta en la salida ni en los errores: solo el nombre del archivo (el repo es público y los informes copian la
    //    salida). La carpeta por defecto es la de la app (LocalLow/iguisoft/RowMate en Windows), calculada en el momento.
    // 4. Los errores son tres textos fijos con el nombre del archivo y el código 2 (QA-G4aT-18): los mensajes de las
    //    excepciones de E/S y de SQLite traen la ruta completa, así que nunca se imprimen. Con --verbose, el tipo de la
    //    excepción (sin su mensaje).
    public static class Program
    {
        public const int ExitOk = 0;
        public const int ExitUsage = 1;
        public const int ExitError = 2;

        private const int CopyAttempts = 3;

        // Con la salida redirigida (un informe, una prueba) escribe UTF-8; en la consola, la de la consola en UTF-8 si se puede
        // (el "—" de una sesión cortada).
        public static int Main(string[] args)
        {
            var utf8 = new UTF8Encoding(false);
            if (!Console.IsOutputRedirected || !Console.IsErrorRedirected)
            {
                try
                {
                    Console.OutputEncoding = utf8;
                }
                catch (IOException)
                {
                    // sin consola: queda la que hay
                }
            }

            var output = Console.IsOutputRedirected ? new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true } : Console.Out;
            var error = Console.IsErrorRedirected ? new StreamWriter(Console.OpenStandardError(), utf8) { AutoFlush = true } : Console.Error;
            try
            {
                return Run(args ?? new string[0], output, error);
            }
            finally
            {
                output.Flush();
                error.Flush();
            }
        }

        public static int Run(string[] args, TextWriter output, TextWriter error)
        {
            if (!TryParse(args, out var source, out var verbose))
            {
                error.WriteLine("[sessions] uso: RowMate.SessionList [--variant simulator|normal | --db <archivo>] [--verbose]");
                return ExitUsage;
            }

            var name = Path.GetFileName(source);
            if (!File.Exists(source))
            {
                error.WriteLine("[sessions] no se encontró " + name);
                return ExitError;
            }

            var work = Path.Combine(Path.GetTempPath(), "RowMate.SessionList", Guid.NewGuid().ToString("N"));
            try
            {
                for (var attempt = 1; attempt <= CopyAttempts; attempt++)
                {
                    try
                    {
                        var listings = ListCopy(source, work, name, verbose ? new VerboseLogger(error) : (IAppLogger)NullAppLogger.Instance);
                        Print(output, name, listings);
                        return ExitOk;
                    }
                    catch (DatabaseOpenException problem) when (problem.Problem != DatabaseProblem.Io)
                    {
                        Detail(error, verbose, problem);
                        error.WriteLine("[sessions] " + name + " no es una base de RowMate");
                        return ExitError;
                    }
                    catch (NotARowMateDatabaseException problem)
                    {
                        Detail(error, verbose, problem);
                        error.WriteLine("[sessions] " + name + " no es una base de RowMate");
                        return ExitError;
                    }
                    catch (FileNotFoundException problem)
                    {
                        Detail(error, verbose, problem);
                        error.WriteLine("[sessions] no se encontró " + name);
                        return ExitError;
                    }
                    catch (DirectoryNotFoundException problem)
                    {
                        Detail(error, verbose, problem);
                        error.WriteLine("[sessions] no se encontró " + name);
                        return ExitError;
                    }
                    catch (Exception problem)
                    {
                        // E/S (en uso, sin permiso) o SQLite al abrir o leer la copia: se copia de nuevo.
                        Detail(error, verbose, problem);
                        Delete(work);
                        Thread.Sleep(200);
                    }
                }

                error.WriteLine("[sessions] " + name + " está en uso o no se puede leer");
                return ExitError;
            }
            finally
            {
                Delete(work);
            }
        }

        // La carpeta de datos de la app (Application.persistentDataPath de Unity con empresa y producto de AppIdentity), en el
        // momento: nunca escrita en el código.
        public static string DefaultFolder()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var company = AppIdentity.CompanyName;
            var product = AppIdentity.ProductName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(home, "AppData", "LocalLow", company, product);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(home, "Library", "Application Support", company, product);
            }

            return Path.Combine(home, ".config", "unity3d", company, product);
        }

        private static bool TryParse(string[] args, out string source, out bool verbose)
        {
            source = "";
            verbose = false;
            string? variant = null;
            string? db = null;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--variant":
                        if (i + 1 >= args.Length || variant != null)
                        {
                            return false;
                        }

                        variant = args[++i];
                        break;
                    case "--db":
                        if (i + 1 >= args.Length || db != null)
                        {
                            return false;
                        }

                        db = args[++i];
                        break;
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        return false;
                }
            }

            if (db != null && variant != null)
            {
                return false;
            }

            if (db != null)
            {
                source = db;
                return db.Length > 0;
            }

            if (variant != null && variant != "simulator" && variant != "normal")
            {
                return false;
            }

            source = Path.Combine(DefaultFolder(), DatabaseFiles.NameFor(variant != "normal"));
            return true;
        }

        // Copia la base (y su -wal) a la carpeta de trabajo, la abre y lista. La base de origen se lee con FileShare.ReadWrite
        // y FileShare.Delete: la app puede seguir escribiendo.
        private static IReadOnlyList<SessionListing> ListCopy(string source, string work, string name, IAppLogger logger)
        {
            Directory.CreateDirectory(work);
            var copy = Path.Combine(work, name);
            CopyShared(source, copy);
            var wal = source + "-wal";
            if (File.Exists(wal))
            {
                CopyShared(wal, copy + "-wal");
            }

            if (new FileInfo(copy).Length == 0)
            {
                throw new NotARowMateDatabaseException();
            }

            using (var database = new RowMateDatabase(copy, new SerialExecutor("RowMate.SessionList", logger), logger))
            {
                var repository = new SqliteSessionRepository(database, logger);
                return repository.ListAsync().GetAwaiter().GetResult();
            }
        }

        private static void CopyShared(string from, string to)
        {
            using (var input = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var output = new FileStream(to, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                input.CopyTo(output);
            }
        }

        private static void Print(TextWriter output, string name, IReadOnlyList<SessionListing> listings)
        {
            output.WriteLine("[sessions] " + name + ": " + listings.Count.ToString(CultureInfo.InvariantCulture)
                + (listings.Count == 1 ? " sesión" : " sesiones"));
            foreach (var listing in listings)
            {
                var finished = listing.FinishedAt.HasValue ? Local(listing.FinishedAt.Value) : "—";
                var routine = listing.RoutineName ?? "libre";
                var points = listing.PointCount.ToString(CultureInfo.InvariantCulture) + (listing.PointCount == 1 ? " punto" : " puntos");
                output.WriteLine("  " + listing.Id.ToString(CultureInfo.InvariantCulture) + " · " + Local(listing.StartedAt) + " · " + finished + " · "
                    + routine + " · " + points);
            }
        }

        private static string Local(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        // Con --verbose, el tipo de la excepción: su mensaje puede traer la ruta y nunca se imprime.
        private static void Detail(TextWriter error, bool verbose, Exception problem)
        {
            if (verbose)
            {
                error.WriteLine("[sessions] detalle: " + problem.GetType().Name
                    + (problem is DatabaseOpenException open ? " (" + open.Problem + ")" : ""));
            }
        }

        private static void Delete(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
            catch (IOException)
            {
                // En uso un instante (el -shm recién cerrado): queda en la carpeta temporal del sistema.
            }
            catch (UnauthorizedAccessException)
            {
                // Ídem.
            }
        }

        // El archivo está vacío: la app nunca deja una base así (la crea con su esquema al primer uso).
        private sealed class NotARowMateDatabaseException : Exception
        {
        }

        // --verbose: el registro de la base (que nunca escribe rutas, QA-G3-20) va al error estándar.
        private sealed class VerboseLogger : IAppLogger
        {
            private readonly TextWriter _error;

            public VerboseLogger(TextWriter error)
            {
                _error = error;
            }

            public void Log(LogLevel level, string tag, string message, Exception? error = null)
            {
                _error.WriteLine("[" + tag + "] " + level + ": " + message + (error == null ? "" : " (" + error.GetType().Name + ")"));
            }
        }
    }
}
