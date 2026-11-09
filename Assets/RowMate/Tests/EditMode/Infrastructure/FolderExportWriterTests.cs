using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Export;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // FW · Escritura del archivo exportado (AC-STR-09-14, -24; 02-tech-design §3.2, DT12-09, QA-G12-02), siempre en una carpeta
    // TEMPORAL propia (nunca Documentos ni Descargas): colisiones sin distinguir mayúsculas, nunca pisa, sin archivos a medias ni
    // temporales, carrera en el Move y la purga de la caché de compartir.
    [TestFixture]
    public sealed class FolderExportWriterTests
    {
        private const string Base = "RowMate_2026-11-05_1840_Escalera";
        private string _root = null!;
        private ExportLocation _location = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "rowmate-export-" + Guid.NewGuid().ToString("N"));
            _location = new ExportLocation(Path.Combine(_root, "exports"), "ROWMATE_DATA_DIR\\exports");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private static byte[] Bytes(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        private string[] Names()
        {
            return Directory.GetFiles(_location.Directory).Select(p => Path.GetFileName(p)!).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        }

        [Test, Category("AC-STR-09-14"), Category("AC-STR-09-16")]
        public void CreatesFolderAndWrites()
        {
            Assert.That(Directory.Exists(_location.Directory), Is.False);
            var file = FolderExportWriter.Write(_location, Base, ".tcx", Bytes("uno"));
            Assert.That(file.FileName, Is.EqualTo(Base + ".tcx"));
            Assert.That(file.FullPath, Is.EqualTo(Path.Combine(_location.Directory, Base + ".tcx")));
            Assert.That(file.Location, Is.SameAs(_location));
            Assert.That(File.ReadAllText(file.FullPath), Is.EqualTo("uno"));
            Assert.That(Names(), Is.EqualTo(new[] { Base + ".tcx" }), "sin temporales");
        }

        [Test, Category("AC-STR-09-14")]
        public void CollisionSuffixCaseInsensitive()
        {
            Directory.CreateDirectory(_location.Directory);
            File.WriteAllText(Path.Combine(_location.Directory, Base.ToLowerInvariant() + ".TCX"), "otro");
            var second = FolderExportWriter.Write(_location, Base, ".tcx", Bytes("dos"));
            Assert.That(second.FileName, Is.EqualTo(Base + "_2.tcx"));
            var third = FolderExportWriter.Write(_location, Base, ".tcx", Bytes("tres"));
            Assert.That(third.FileName, Is.EqualTo(Base + "_3.tcx"));
        }

        [Test, Category("AC-STR-09-14")]
        public void NeverOverwrites()
        {
            var first = FolderExportWriter.Write(_location, Base, ".tcx", Bytes("primero"));
            var second = FolderExportWriter.Write(_location, Base, ".tcx", Bytes("segundo"));
            Assert.That(File.ReadAllText(first.FullPath), Is.EqualTo("primero"), "el anterior no cambia");
            Assert.That(File.ReadAllText(second.FullPath), Is.EqualTo("segundo"));
            Assert.That(Names(), Is.EqualTo(new[] { Base + ".tcx", Base + "_2.tcx" }));
        }

        [Test, Category("AC-STR-09-14"), Category("AC-STR-09-21")]
        public void FailureLeavesNoPartialNorTemp()
        {
            // Disco lleno a mitad: el .part tiene parte del contenido cuando falla.
            var error = Assert.Throws<IOException>(() => FolderExportWriter.Write(_location, Base, ".tcx", Bytes(new string('x', 4096)),
                path => new FailingStream(new FileStream(path, FileMode.CreateNew, FileAccess.Write), 100)));
            Assert.That(error!.Message, Does.Contain("disco lleno"));
            Assert.That(Names(), Is.Empty, "ni .tcx a medias ni .part");
            // Y si falla al abrir.
            Assert.Throws<UnauthorizedAccessException>(() => FolderExportWriter.Write(_location, Base, ".tcx", Bytes("x"),
                path => throw new UnauthorizedAccessException("sin acceso")));
            Assert.That(Names(), Is.Empty);
        }

        [Test, Category("AC-STR-09-14")]
        public void RaceOnMoveTakesNextSuffix()
        {
            // Otro toma el nombre entre la enumeración de la carpeta y el Move (mientras se escribe el .part).
            var file = FolderExportWriter.Write(_location, Base, ".tcx", Bytes("mío"), path =>
            {
                File.WriteAllText(Path.Combine(_location.Directory, Base + ".tcx"), "del otro");
                return new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            });
            Assert.That(file.FileName, Is.EqualTo(Base + "_2.tcx"));
            Assert.That(File.ReadAllText(Path.Combine(_location.Directory, Base + ".tcx")), Is.EqualTo("del otro"), "no lo pisa");
            Assert.That(File.ReadAllText(file.FullPath), Is.EqualTo("mío"));
            Assert.That(Names(), Is.EqualTo(new[] { Base + ".tcx", Base + "_2.tcx" }));
        }

        [Test, Category("AC-STR-09-21")]
        public void NoDirectoryFails()
        {
            Assert.Throws<IOException>(() => FolderExportWriter.Write(new ExportLocation("", null), Base, ".tcx", Bytes("x")));
        }

        [Test, Category("AC-STR-09-24")]
        public void DeleteOlderThanOnlyOldTcx()
        {
            var dir = _location.Directory;
            Directory.CreateDirectory(Path.Combine(dir, "sub"));
            var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
            var limit = now - FolderExportWriter.ShareCacheMaxAge;
            void Make(string name, DateTime written)
            {
                var path = Path.Combine(dir, name);
                File.WriteAllText(path, name);
                File.SetLastWriteTimeUtc(path, written);
            }

            Make("viejo.tcx", limit.AddMinutes(-1));
            Make("VIEJO2.TCX", limit.AddHours(-30));
            Make("nuevo.tcx", limit.AddMinutes(1));
            Make("justo.tcx", limit);
            Make("viejo.fit", limit.AddHours(-30));
            Make("viejo.tcx.part", limit.AddHours(-30));
            Make("~rowmate-abc.part", limit.AddHours(-30));
            var nested = Path.Combine(dir, "sub", "adentro.tcx");
            File.WriteAllText(nested, "x");
            File.SetLastWriteTimeUtc(nested, limit.AddHours(-30));
            Assert.That(FolderExportWriter.DeleteOlderThan(dir, ".tcx", limit), Is.EqualTo(2));
            Assert.That(Names(), Is.EqualTo(new[] { "justo.tcx", "nuevo.tcx", "viejo.fit", "viejo.tcx.part", "~rowmate-abc.part" }));
            Assert.That(File.Exists(nested), Is.True, "sin recursión");
        }

        [Test, Category("AC-STR-09-24")]
        public void DeleteOlderThanMissingFolderIsZero()
        {
            Assert.That(FolderExportWriter.DeleteOlderThan(_location.Directory, ".tcx", DateTime.MaxValue), Is.EqualTo(0));
            Assert.That(Directory.Exists(_location.Directory), Is.False, "nunca la crea (m-02)");
        }

        [Test, Category("AC-STR-09-24")]
        public void ShareCacheMaxAgeIs24h()
        {
            Assert.That(FolderExportWriter.ShareCacheMaxAge, Is.EqualTo(TimeSpan.FromHours(24)));
        }

        // Un flujo que acepta `limit` bytes y después falla como un disco lleno.
        private sealed class FailingStream : Stream
        {
            private readonly Stream _inner;
            private readonly int _limit;

            public FailingStream(Stream inner, int limit)
            {
                _inner = inner;
                _limit = limit;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => _inner.Length;

            public override long Position
            {
                get => _inner.Position;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                _inner.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, Math.Min(count, _limit));
                _inner.Flush();
                throw new IOException("disco lleno");
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
