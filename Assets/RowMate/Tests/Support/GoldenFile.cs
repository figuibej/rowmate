using System;
using System.IO;
using System.Text.RegularExpressions;
using RowMate.Core.Common.Json;

namespace RowMate.Tests.Support
{
    // Encabezado común de los vectores dorados (02-tech-design §6.2). Clase con propiedades de solo lectura:
    // las fuentes de test no declaran records (QA-03).
    public sealed class GoldenHeader
    {
        public GoldenHeader(int schema, string generator, string dartVersion, string legacyCommit, string generatedAt)
        {
            Schema = schema;
            Generator = generator;
            DartVersion = dartVersion;
            LegacyCommit = legacyCommit;
            GeneratedAt = generatedAt;
        }

        public int Schema { get; }

        public string Generator { get; }

        public string DartVersion { get; }

        public string LegacyCommit { get; }

        public string GeneratedAt { get; }
    }

    // Un archivo de Assets/RowMate/Tests/Golden/oracle/ ya leído y validado.
    public sealed class GoldenFile
    {
        private static readonly Regex CommitPattern = new Regex("^[0-9a-f]{40}$", RegexOptions.CultureInvariant);

        public GoldenFile(string fileName, GoldenHeader header, JsonNode body, long sizeBytes)
        {
            FileName = fileName;
            Header = header;
            Body = body;
            SizeBytes = sizeBytes;
        }

        public string FileName { get; }

        public GoldenHeader Header { get; }

        // El documento completo (incluye "header").
        public JsonNode Body { get; }

        // Para el tope de 1 MiB por archivo (02-tech-design §6.2, R-T11).
        public long SizeBytes { get; }

        // Lee y valida un archivo: schema == 1, legacyCommit de 40 hex, dartVersion "3.…".
        public static GoldenFile Read(string path)
        {
            var fileName = Path.GetFileName(path);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Golden: falta " + RepoPaths.Relative(path)
                    + ". Se genera con tools/oracle/run-oracle.ps1 (necesita Dart y el legacy al lado).", path);
            }

            var text = File.ReadAllText(path);
            JsonNode body;
            try
            {
                body = JsonParser.Parse(text);
            }
            catch (FormatException e)
            {
                throw new FormatException("Golden " + fileName + ": " + e.Message, e);
            }

            if (body.Kind != JsonKind.Object || !body.TryGet("header", out var headerNode) || headerNode.Kind != JsonKind.Object)
            {
                throw new FormatException("Golden " + fileName + ": falta el objeto \"header\".");
            }

            var header = new GoldenHeader(
                (int)headerNode["schema"].AsLong(),
                headerNode["generator"].AsString(),
                headerNode["dartVersion"].AsString(),
                headerNode["legacyCommit"].AsString(),
                headerNode["generatedAt"].AsString());

            if (header.Schema != 1)
            {
                throw new FormatException("Golden " + fileName + ": schema " + header.Schema + " (se esperaba 1).");
            }

            if (!CommitPattern.IsMatch(header.LegacyCommit))
            {
                throw new FormatException("Golden " + fileName + ": legacyCommit \"" + header.LegacyCommit + "\" no es un sha de 40 hex.");
            }

            if (!header.DartVersion.StartsWith("3.", StringComparison.Ordinal))
            {
                throw new FormatException("Golden " + fileName + ": dartVersion \"" + header.DartVersion + "\" no es Dart 3.");
            }

            if (header.Generator.Length == 0)
            {
                throw new FormatException("Golden " + fileName + ": generator vacío.");
            }

            return new GoldenFile(fileName, header, body, new FileInfo(path).Length);
        }

        // Una sección; falla con un mensaje claro si no existe.
        public JsonNode Section(string name)
        {
            if (Body.TryGet(name, out var section))
            {
                return section;
            }

            throw new InvalidOperationException("Golden " + FileName + ": no existe la sección \"" + name
                + "\". Regenerá con tools/oracle/run-oracle.ps1.");
        }

        // Sección en formato de tabla ({ "columns": [...], "rows": [[...], ...] }).
        public GoldenTable Table(string name)
        {
            return GoldenTable.From(FileName + "/" + name, Section(name));
        }
    }
}
