using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // Estado de los archivos de una carpeta de base (nombre → SHA-256): "la base no se toca" se comprueba con los BYTES del
    // archivo principal y de sus -wal / -shm (AC-HIS-17-04, -05; AC-RUT-08-08), no con lo que lee SQLite.
    internal static class DatabaseFileState
    {
        public static IReadOnlyDictionary<string, string> Of(string folder)
        {
            var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(folder))
            {
                result[Path.GetFileName(file)] = Hash(file);
            }

            return result;
        }

        public static string Hash(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            }
        }

        public static string Describe(IReadOnlyDictionary<string, string> state)
        {
            var parts = new List<string>();
            foreach (var entry in state)
            {
                parts.Add(entry.Key + "=" + entry.Value.Substring(0, 12));
            }

            return string.Join(", ", parts);
        }

        // La tarea terminó (ejecutor en línea) y devuelve su resultado o su excepción (sin AggregateException).
        public static T Done<T>(Task<T> task)
        {
            Assert.That(task.IsCompleted, Is.True, "la tarea terminó (ejecutor en línea)");
            return task.GetAwaiter().GetResult();
        }

        public static void Done(Task task)
        {
            Assert.That(task.IsCompleted, Is.True, "la tarea terminó (ejecutor en línea)");
            task.GetAwaiter().GetResult();
        }

        // La excepción de una tarea ya terminada que tenía que fallar.
        public static Exception Failure(Task task)
        {
            Assert.That(task.IsCompleted, Is.True, "la tarea terminó (ejecutor en línea)");
            Assert.That(task.IsFaulted, Is.True, "la tarea tenía que fallar");
            var aggregate = task.Exception!;
            return aggregate.InnerExceptions.Count == 1 ? aggregate.InnerExceptions[0] : aggregate;
        }
    }
}
