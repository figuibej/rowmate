using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Export;

namespace RowMate.Tests.EditMode.Export
{
    // LG · El registro sin rutas del equipo (AC-STR-09-21; DT12-19).
    [TestFixture]
    public sealed class ExportLogTests
    {
        private static KeyValuePair<string, string> Pair(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        [Test, Category("AC-STR-09-21")]
        public void NoUserPathInMessage()
        {
            // Una ruta inventada (como la de un equipo con Documentos en OneDrive): nunca sale en el registro.
            const string User = "X:\\Perfiles\\persona";
            const string Folder = User + "\\OneDrive\\Documentos\\RowMate\\TCX";
            var replacements = new[] { Pair(User, ExportLog.UserProfileLabel), Pair(Folder, "Documentos\\RowMate\\TCX") };
            var error = new UnauthorizedAccessException("Access to the path 'x:\\perfiles\\PERSONA\\onedrive\\documentos\\rowmate\\tcx\\~rowmate-1.part' is denied.");
            var message = ExportLog.Failed(error, replacements);
            Assert.That(message, Is.EqualTo("no se pudo exportar: UnauthorizedAccessException: Access to the path 'Documentos\\RowMate\\TCX\\~rowmate-1.part' is denied."),
                "la carpeta (la más larga) primero y sin distinguir mayúsculas");
            Assert.That(ExportLog.Scrub("en X:/Perfiles/persona/AppData/Temp", replacements), Is.EqualTo("en %USERPROFILE%/AppData/Temp"), "también con la otra barra");
            Assert.That(ExportLog.Scrub("sin rutas", replacements), Is.EqualTo("sin rutas"));
            Assert.That(ExportLog.Scrub("x", new[] { Pair("", "nada") }), Is.EqualTo("x"), "una clave vacía no se usa");
        }

        [Test, Category("AC-STR-09-21")]
        public void SavedAndFailedFormats()
        {
            Assert.That(ExportLog.Saved("RowMate_2026-11-05_1840_Escalera.tcx", "Documentos\\RowMate\\TCX"),
                Is.EqualTo("TCX guardado: RowMate_2026-11-05_1840_Escalera.tcx en Documentos\\RowMate\\TCX"));
            Assert.That(ExportLog.Failed(new IOException("disco lleno"), new KeyValuePair<string, string>[0]),
                Is.EqualTo("no se pudo exportar: IOException: disco lleno"));
            Assert.That(ExportLog.Tag, Is.EqualTo("EXPORT"));
        }
    }
}
