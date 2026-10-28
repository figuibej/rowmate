using System.Collections;
using System.IO;
using NUnit.Framework;
using RowMate.Infrastructure.Database;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // RPS · Lo guardado sobrevive a cerrar y reabrir la app, con SQLite real en una carpeta temporal (AC-RUT-08-06, -09,
    // AC-HIS-17-04, AC-HIS-20-02; 03-rutinas/02-tech-design §2, §6.3). Nunca la carpeta de datos del usuario (AC-HIS-20-04).
    public sealed class RoutinesPersistenceTests : RoutinesUiTestBase
    {
        // Los bytes del archivo principal con la base abierta (SQLite lo tiene abierto para escribir: hay que compartirlo).
        private static byte[] ReadShared(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                return copy.ToArray();
            }
        }

        [UnityTest, Category("AC-RUT-08-06")]
        public IEnumerator SeriesBlocksSurviveReopen()
        {
            CreateRoutines(RoutineSeeds.Empty);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Dos series");
            H.Tap("editor-add-series");
            H.Tap("editor-add-series");
            yield return Settle();
            var groups = (Editor.Store.Template[0].GroupId, Editor.Store.Template[2].GroupId);
            Assert.That(groups.Item1, Is.Not.EqualTo(groups.Item2), "dos toques en el mismo milisegundo: dos ids");
            yield return Tap("editor-save");
            H.Reopen();
            yield return Frames(2);
            yield return OpenEditorOf(0);
            Assert.That(H.Find("series-0"), Is.Not.Null);
            Assert.That(H.Find("series-1"), Is.Not.Null, "las dos series vuelven separadas");
            Assert.That(Editor.Store.Template[0].GroupId, Is.EqualTo(groups.Item1));
            Assert.That(Editor.Store.Template[2].GroupId, Is.EqualTo(groups.Item2));
        }

        [UnityTest, Category("AC-RUT-08-09")]
        public IEnumerator ReopenShowsTheSame()
        {
            CreateRoutines(RoutineSeeds.Lista);
            yield return OpenNewEditor();
            yield return Type("editor-name", "Guardada");
            yield return Type("editor-description", "Con descripción\ny dos líneas");
            yield return Tap("editor-add-series");
            yield return Tap("editor-save");
            var before = H.App.Routines.State.Value.Routines;
            H.Reopen();
            Assert.That(H.Generation, Is.EqualTo(1));
            yield return GoToRoutines();
            var after = H.App.Routines.State.Value.Routines;
            Assert.That(after.Count, Is.EqualTo(5));
            for (var i = 0; i < after.Count; i++)
            {
                Assert.That(after[i].Id, Is.EqualTo(before[i].Id), "id de la rutina " + i);
                Assert.That(after[i].Name, Is.EqualTo(before[i].Name));
                Assert.That(after[i].Description, Is.EqualTo(before[i].Description));
                Assert.That(after[i].CreatedAt, Is.EqualTo(before[i].CreatedAt));
                Assert.That(after[i].Steps, Is.EqualTo(before[i].Steps), "los pasos de la rutina " + i + " iguales después de reabrir");
            }

            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Guardada"));
            yield return Tap("routine-card-0");
            Assert.That(Editor.DescriptionField.Value, Is.EqualTo("Con descripción\ny dos líneas"));
            Assert.That(Editor.CountLabel.text, Is.EqualTo("Pasos (2)"));
        }

        [UnityTest, Category("AC-HIS-17-04")]
        public IEnumerator OpensAnExistingBase()
        {
            CreateRoutines(RoutineSeeds.Lista);
            var path = H.Routines.FilePath!;
            H.Reopen();
            var bytes = ReadShared(path);
            H.Reopen();
            Assert.That(ReadShared(path), Is.EqualTo(bytes), "abrir una v8 existente no la cambia");
            yield return GoToRoutines();
            Assert.That(Screen.List.Count, Is.EqualTo(4), "se usa tal cual");
        }

        [UnityTest, Category("AC-HIS-20-02")]
        public IEnumerator VariantsDoNotMix()
        {
            using (var folder = new TempDatabase())
            {
                var simulator = DatabaseFiles.NameFor(true);
                var normal = DatabaseFiles.NameFor(false);
                Assert.That(simulator, Is.Not.EqualTo(normal), "el mismo nombre que §2.5");
                Create(new UiHostOptions { Routines = HostRoutines.InFolder(folder, simulator) });
                yield return OpenNewEditor();
                yield return Type("editor-name", "Del simulador");
                yield return Tap("editor-add-series");
                yield return Tap("editor-save");
                Assert.That(Listed.Count, Is.EqualTo(1));
                Create(new UiHostOptions { Routines = HostRoutines.InFolder(folder, normal) });
                yield return GoToRoutines();
                Assert.That(Listed.Count, Is.EqualTo(0), "la base normal no ve la del simulador");
                Assert.That(H.IsShown("routines-empty"), Is.True);
                Create(new UiHostOptions { Routines = HostRoutines.InFolder(folder, simulator) });
                yield return GoToRoutines();
                Assert.That(Listed.Count, Is.EqualTo(1), "y la del simulador sigue con lo suyo");
                Assert.That(File.Exists(folder.PathOf(simulator)) && File.Exists(folder.PathOf(normal)), Is.True, "dos archivos en la misma carpeta");
                DisposeHost();
            }
        }
    }
}
