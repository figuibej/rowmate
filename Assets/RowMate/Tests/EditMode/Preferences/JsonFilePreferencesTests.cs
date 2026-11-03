using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Common.Json;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Preferences;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Preferences
{
    // JFP: prefs.json (05-series-parciales/02-tech-design §2.7; ADR 021; AC-WKT-18-11, AC-HIS-20-04): falta, vacío o dañado →
    // vacío sin aviso y el próximo Set guarda uno válido; una clave de otro tipo → solo esa da false y todas se conservan; el
    // guardado que falla deja el valor en memoria sin aviso; un corte entre el temporal y el reemplazo deja el viejo; las claves
    // desconocidas se conservan; las escrituras seguidas se juntan y la última gana; el mismo nombre para los dos builds; Q-11.
    // Todo en una carpeta temporal propia (TempDatabase: <temp del sistema>/RowMate.Tests/<guid>-ñá/), nunca el archivo del
    // usuario: ninguna prueba calcula la ruta real.
    [TestFixture]
    public sealed class JsonFilePreferencesTests
    {
        private const string Laps = "immersive.lapsExpanded";
        private const string Reps = "immersive.repsExpanded";

        [Test, Category("AC-WKT-18-11")]
        public void AMissingFileStartsEmptyAndTheNextSetWritesAValidOne()
        {
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                var logger = new RecordingLogger();
                using (var prefs = Inline(path, logger))
                {
                    Assert.That(prefs.IsLoaded, Is.True);
                    Assert.That(prefs.TryGetBool(Laps, out var value), Is.False);
                    Assert.That(value, Is.False);
                    Assert.That(File.Exists(path), Is.False, "leer no crea el archivo");
                    prefs.SetBool(Laps, true);
                    Assert.That(Read(path), Is.EqualTo("{\"immersive.lapsExpanded\":true}"));
                    Assert.That(File.Exists(path + PreferencesFiles.TempSuffix), Is.False, "el temporal se puso en su lugar");
                    Assert.That(prefs.Writes, Is.EqualTo(1));
                }

                Assert.That(logger.Lines, Is.Empty, "sin aviso");
            }
        }

        [TestCase("", "vacío")]
        [TestCase("   \n", "vacío")]
        [TestCase("{\"immersive.lapsExpanded\": tru", "dañado")]
        [TestCase("[true, false]", "no es un objeto")]
        [TestCase("\"texto\"", "no es un objeto")]
        [Category("AC-WKT-18-11")]
        public void ADamagedFileStartsEmptyWithoutWarningAndTheNextSetRepairsIt(string content, string kind)
        {
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                File.WriteAllText(path, content, new UTF8Encoding(false));
                var logger = new RecordingLogger();
                using (var prefs = Inline(path, logger))
                {
                    Assert.That(prefs.TryGetBool(Laps, out _), Is.False, kind);
                    Assert.That(prefs.Keys, Is.Empty);
                    prefs.SetBool(Reps, true);
                    Assert.That(Read(path), Is.EqualTo("{\"immersive.repsExpanded\":true}"));
                    Assert.That(JsonParser.Parse(Read(path)).Kind, Is.EqualTo(JsonKind.Object));
                }

                Assert.That(logger.Lines.Count, Is.EqualTo(1), "una línea Info");
                Assert.That(logger.Levels[0], Is.EqualTo(LogLevel.Info));
                Assert.That(logger.Lines[0], Does.Not.Contain(temp.Folder).And.Not.Contain(PreferencesFiles.Name), "sin la ruta");
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void AKeyOfAnotherTypeFailsAloneAndEverythingIsKept()
        {
            // QA-G5T-07: solo esa clave da false; las demás se leen; al guardar se conservan todas, esa también, tal cual (con sus
            // números exactos y sus cadenas), hasta que un Set la reemplace.
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                var original = "{\"immersive.lapsExpanded\":\"sí\",\"immersive.repsExpanded\":true,\"hrm.remembered\":{\"name\":\"sensor\",\"n\":1.50,"
                               + "\"list\":[1,null,false]},\"scene.hour\":12}";
                File.WriteAllText(path, original, new UTF8Encoding(false));
                using (var prefs = Inline(path, new RecordingLogger()))
                {
                    Assert.That(prefs.TryGetBool(Laps, out _), Is.False, "una cadena no es un booleano");
                    Assert.That(prefs.TryGetString(Laps, out var text), Is.True);
                    Assert.That(text, Is.EqualTo("sí"));
                    Assert.That(prefs.TryGetBool(Reps, out var reps) && reps, Is.True, "las demás se leen");
                    Assert.That(prefs.TryGetBool("scene.hour", out _), Is.False, "un número no es un booleano");
                    Assert.That(prefs.Keys, Is.EqualTo(new[] { Laps, Reps, "hrm.remembered", "scene.hour" }));
                    prefs.SetBool(Reps, false);
                    Assert.That(Read(path), Is.EqualTo(original.Replace("\"immersive.repsExpanded\":true", "\"immersive.repsExpanded\":false")),
                        "las demás claves se conservan tal cual, en su orden");
                    prefs.SetBool(Laps, true);
                    Assert.That(Read(path), Does.StartWith("{\"immersive.lapsExpanded\":true,"));
                    prefs.Remove("scene.hour");
                    Assert.That(Read(path), Does.Not.Contain("scene.hour"));
                    Assert.That(Read(path), Does.Contain("\"hrm.remembered\":{\"name\":\"sensor\",\"n\":1.50,\"list\":[1,null,false]}"));
                }
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void AFailedSaveKeepsTheValueInMemoryWithoutNotice()
        {
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                File.WriteAllText(path, "{\"immersive.lapsExpanded\":false}", new UTF8Encoding(false));
                var logger = new RecordingLogger();
                var ops = new FaultyOps { FailWrite = true };
                using (var prefs = new JsonFilePreferences(path, logger, new InlineDbExecutor(), ops))
                {
                    Assert.DoesNotThrow(() => prefs.SetBool(Laps, true));
                    Assert.That(prefs.TryGetBool(Laps, out var laps) && laps, Is.True, "la misma ejecución sigue con lo que tocó el usuario");
                    Assert.That(prefs.Writes, Is.EqualTo(0));
                }

                Assert.That(Read(path), Is.EqualTo("{\"immersive.lapsExpanded\":false}"), "después de reiniciar, lo último guardado");
                Assert.That(logger.Levels, Is.EqualTo(new[] { LogLevel.Warning }), "una línea Warning, y nada más");
                Assert.That(logger.Lines[0], Does.Not.Contain(temp.Folder).And.Not.Contain(PreferencesFiles.Name), "sin la ruta");
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        [Category("AC-WKT-18-11")]
        public void ACutBetweenTheTempAndTheReplaceLeavesTheOldFile(bool existed)
        {
            // Entero o nada: el temporal se escribe al lado; si el reemplazo no llega, el archivo que estaba sigue entero (o, si no
            // había, sigue sin haber), nunca uno a medias.
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                if (existed)
                {
                    File.WriteAllText(path, "{\"immersive.repsExpanded\":true}", new UTF8Encoding(false));
                }

                var ops = new FaultyOps { FailReplace = true };
                using (var prefs = new JsonFilePreferences(path, new RecordingLogger(), new InlineDbExecutor(), ops))
                {
                    prefs.SetBool(Laps, true);
                    Assert.That(ops.TempWrites, Is.EqualTo(1), "el temporal sí se escribió");
                }

                if (existed)
                {
                    Assert.That(Read(path), Is.EqualTo("{\"immersive.repsExpanded\":true}"));
                }
                else
                {
                    Assert.That(File.Exists(path), Is.False);
                }

                Assert.That(File.Exists(path + PreferencesFiles.TempSuffix), Is.False, "el temporal que quedó se borra");
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void QueuedWritesCoalesceAndTheLastWins()
        {
            // Las escrituras seguidas se juntan: a lo sumo una en curso y una pendiente; un Set nuevo reemplaza los bytes pendientes.
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                var writer = new QueuedExecutor();
                var ops = new FaultyOps();
                using (var prefs = new JsonFilePreferences(path, new RecordingLogger(), writer, ops))
                {
                    prefs.SetBool(Laps, true);
                    prefs.SetBool(Laps, false);
                    prefs.SetBool(Reps, true);
                    Assert.That(File.Exists(path), Is.False, "nada en el disco hasta que corre el ejecutor");
                    Assert.That(writer.Pending, Is.EqualTo(1), "un solo trabajo encolado");
                    writer.RunAll();
                    Assert.That(prefs.Writes, Is.EqualTo(1), "tres toques seguidos, una escritura");
                    Assert.That(Read(path), Is.EqualTo("{\"immersive.lapsExpanded\":false,\"immersive.repsExpanded\":true}"), "la última gana");
                }
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void TwoSetsDuringAWriteInFlightGiveAtMostTwoWrites()
        {
            // Con una escritura en curso (el hilo de escritura detenido adentro), dos Set más: a lo sumo dos escrituras en total, y
            // la última gana. Los bytes de cada escritura ya están armados (el ejecutor solo escribe).
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                var logger = new RecordingLogger();
                var ops = new FaultyOps { Gate = new ManualResetEventSlim(false) };
                using (var writer = new SerialExecutor("RowMate.Tests.Prefs", logger))
                using (var prefs = new JsonFilePreferences(path, logger, writer, ops))
                {
                    prefs.SetBool(Laps, true);
                    Assert.That(ops.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True, "la primera escritura está en curso");
                    prefs.SetBool(Reps, true);
                    prefs.SetBool(Reps, false);
                    ops.Gate.Set();
                    Assert.That(prefs.FlushAsync().Wait(TimeSpan.FromSeconds(5)), Is.True);
                    Assert.That(prefs.Writes, Is.LessThanOrEqualTo(2));
                    Assert.That(prefs.Writes, Is.EqualTo(2));
                    Assert.That(ops.Written[0], Is.EqualTo("{\"immersive.lapsExpanded\":true}"), "la primera, con lo de su Set");
                    Assert.That(Read(path), Is.EqualTo("{\"immersive.lapsExpanded\":true,\"immersive.repsExpanded\":false}"), "la última gana");
                }
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void TheProductionWriterSavesOffTheMainThreadAndFlushes()
        {
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                using (var prefs = new JsonFilePreferences(path, new RecordingLogger()))
                {
                    prefs.SetString("scene.mode", "día \"claro\"\n");
                    prefs.SetBool(Laps, true);
                    Assert.That(prefs.FlushAsync().Wait(TimeSpan.FromSeconds(5)), Is.True);
                    Assert.That(prefs.Writes, Is.InRange(1, 2));
                }

                Assert.That(Read(path), Is.EqualTo("{\"scene.mode\":\"día \\\"claro\\\"\\n\",\"immersive.lapsExpanded\":true}"));
                using (var again = new JsonFilePreferences(path, new RecordingLogger()))
                {
                    Assert.That(again.TryGetString("scene.mode", out var mode), Is.True);
                    Assert.That(mode, Is.EqualTo("día \"claro\"\n"));
                    Assert.That(again.TryGetBool(Laps, out var laps) && laps, Is.True, "sobrevive a reiniciar");
                    Assert.That(again.FlushAsync().IsCompleted, Is.True, "sin nada pendiente");
                }
            }
        }

        [Test, Category("AC-WKT-18-11")]
        public void AByteOrderMarkIsRead()
        {
            using (var temp = new TempDatabase())
            {
                var path = temp.PathOf(PreferencesFiles.Name);
                File.WriteAllText(path, "{\"immersive.lapsExpanded\":true}", new UTF8Encoding(true));
                using (var prefs = Inline(path, new RecordingLogger()))
                {
                    Assert.That(prefs.TryGetBool(Laps, out var laps) && laps, Is.True);
                }
            }
        }

        [Test, Category("AC-WKT-18-11"), Category("AC-HIS-20-04")]
        public void OneFileNameForBothBuildsAndNeverTheLegacyOnes()
        {
            // P-11: el mismo archivo para el build normal y el de simulador (las bases tienen nombres distintos); Q-11: nunca las
            // preferencias del legacy.
            Assert.That(PreferencesFiles.Name, Is.EqualTo("prefs.json"));
            Assert.That(DatabaseFiles.NameFor(true), Is.Not.EqualTo(DatabaseFiles.NameFor(false)));
            Assert.That(PreferencesFiles.Name, Does.Not.Contain("shared").And.Not.Contain("Flutter"));
            Assert.That(Path.GetFileName(PreferencesFiles.PathIn(TempDatabase.Root)), Is.EqualTo("prefs.json"));
            Assert.That(PreferencesFiles.PathIn(TempDatabase.Root), Does.StartWith(TempDatabase.Root));
        }

        // ── Lo común ──

        private static JsonFilePreferences Inline(string path, IAppLogger logger)
        {
            return new JsonFilePreferences(path, logger, new InlineDbExecutor(), new FaultyOps());
        }

        private static string Read(string path)
        {
            return File.ReadAllText(path, new UTF8Encoding(false));
        }

        // Las operaciones de System.IO, con fallas a pedido y una compuerta para detener la escritura.
        private sealed class FaultyOps : IPreferencesFileOps
        {
            public bool FailWrite { get; set; }

            public bool FailReplace { get; set; }

            public ManualResetEventSlim? Gate { get; set; }

            public ManualResetEventSlim Entered { get; } = new ManualResetEventSlim(false);

            public int TempWrites { get; private set; }

            public List<string> Written { get; } = new List<string>();

            public bool Exists(string path)
            {
                return File.Exists(path);
            }

            public string ReadAllText(string path)
            {
                return File.ReadAllText(path, new UTF8Encoding(false));
            }

            public void WriteAllBytes(string path, byte[] bytes)
            {
                Entered.Set();
                Gate?.Wait(TimeSpan.FromSeconds(10));
                if (FailWrite)
                {
                    throw new IOException("falla de prueba al escribir " + path);
                }

                File.WriteAllBytes(path, bytes);
                TempWrites++;
                lock (Written)
                {
                    Written.Add(new UTF8Encoding(false).GetString(bytes));
                }
            }

            public void Replace(string source, string destination)
            {
                if (FailReplace)
                {
                    throw new IOException("falla de prueba al reemplazar " + destination);
                }

                File.Replace(source, destination, null);
            }

            public void Move(string source, string destination)
            {
                if (FailReplace)
                {
                    throw new IOException("falla de prueba al mover " + destination);
                }

                File.Move(source, destination);
            }

            public void Delete(string path)
            {
                File.Delete(path);
            }
        }

        // Un ejecutor que encola y corre a pedido (en el hilo de la prueba).
        private sealed class QueuedExecutor : IDbExecutor
        {
            private readonly Queue<Action> _work = new Queue<Action>();

            public int Pending => _work.Count;

            public Task<T> Run<T>(Func<T> work)
            {
                var source = new TaskCompletionSource<T>();
                _work.Enqueue(() =>
                {
                    try
                    {
                        source.SetResult(work());
                    }
                    catch (Exception error)
                    {
                        source.SetException(error);
                    }
                });
                return source.Task;
            }

            public Task Run(Action work)
            {
                return Run(() =>
                {
                    work();
                    return true;
                });
            }

            public bool WaitIdle(TimeSpan timeout)
            {
                return _work.Count == 0;
            }

            public void RunAll()
            {
                while (_work.Count > 0)
                {
                    _work.Dequeue()();
                }
            }

            public void Dispose()
            {
            }
        }
    }
}
