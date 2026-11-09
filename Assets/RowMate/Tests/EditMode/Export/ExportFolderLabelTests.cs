using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Core.Localization;
using RowMate.Core.Ports;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Export
{
    // FL · La carpeta como se muestra (AC-STR-09-16, -19; 02-tech-design §2.5): Documentos en el idioma de la app o la etiqueta
    // fija; nunca la unidad, el usuario ni la ruta real.
    [TestFixture]
    public sealed class ExportFolderLabelTests
    {
        private static readonly string UserFolder = Path.Combine(Path.GetTempPath(), "rowmate-label-user", "Documents", "RowMate", "TCX");

        [Test, Category("AC-STR-09-16"), Category("AC-STR-09-19")]
        public void DocumentsAndFixedLabels()
        {
            var documents = new ExportLocation(UserFolder, null);
            Assert.That(ExportFolderLabel.For(documents, LocalizationFixture.Es), Is.EqualTo("Documentos\\RowMate\\TCX"));
            Assert.That(ExportFolderLabel.For(documents, LocalizationFixture.En), Is.EqualTo("Documents\\RowMate\\TCX"));
            var dataDir = new ExportLocation(Path.Combine(Path.GetTempPath(), "rowmate-data", "exports"), "ROWMATE_DATA_DIR\\exports");
            Assert.That(ExportFolderLabel.For(dataDir, LocalizationFixture.Es), Is.EqualTo("ROWMATE_DATA_DIR\\exports"));
            Assert.That(LocalizationFixture.Es.Format(L10nKeys.ExportTcxSaved, MessageArg.Text("folder", ExportFolderLabel.For(documents, LocalizationFixture.Es))),
                Is.EqualTo("Guardado en Documentos\\RowMate\\TCX"));
            Assert.That(LocalizationFixture.En.Format(L10nKeys.ExportTcxSaved, MessageArg.Text("folder", ExportFolderLabel.For(documents, LocalizationFixture.En))),
                Is.EqualTo("Saved to Documents\\RowMate\\TCX"));
        }

        [Test, Category("AC-STR-09-19")]
        public void NeverDriveUserOrRealPath()
        {
            var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var locations = new List<ExportLocation>
            {
                new ExportLocation(UserFolder, null),
                new ExportLocation(Path.Combine(user, "Documents", "RowMate", "TCX"), null),
            };
            foreach (var location in locations)
            {
                foreach (var l10n in LocalizationFixture.Both)
                {
                    var label = ExportFolderLabel.For(location, l10n);
                    Assert.That(label, Is.EqualTo(l10n.Get(L10nKeys.ExportTcxDocuments) + "\\RowMate\\TCX"), "solo el texto fijo");
                    Assert.That(label, Does.Not.Contain(":").And.Not.Contain("Users").And.Not.Contain(location.Directory));
                }
            }
        }
    }
}
