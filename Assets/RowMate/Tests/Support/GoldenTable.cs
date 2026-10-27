using System;
using System.Collections.Generic;
using System.Text;
using RowMate.Core.Common.Json;

namespace RowMate.Tests.Support
{
    // Sección de vectores dorados en formato de tabla (QA-06, 02-tech-design §6.2):
    // { "columns": ["x", "n", "out"], "rows": [["4038400000000000", 1, "24.3"], ...] }.
    // Las celdas son escalares: un double va como "<16 hex>" (bits IEEE-754), un entero como número y un texto como cadena.
    public sealed class GoldenTable
    {
        private readonly Dictionary<string, int> _columnIndex;
        private readonly IReadOnlyList<JsonNode> _rows;

        private GoldenTable(string name, IReadOnlyList<string> columns, IReadOnlyList<JsonNode> rows)
        {
            Name = name;
            Columns = columns;
            _rows = rows;
            _columnIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < columns.Count; i++)
            {
                _columnIndex[columns[i]] = i;
            }
        }

        // "<archivo>/<sección>", para los mensajes.
        public string Name { get; }

        public IReadOnlyList<string> Columns { get; }

        public int Count => _rows.Count;

        public GoldenRow this[int index]
        {
            get
            {
                if (index < 0 || index >= _rows.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return new GoldenRow(this, index);
            }
        }

        // Verdadero si la sección tiene forma de tabla.
        public static bool IsTable(JsonNode section)
        {
            return section.Kind == JsonKind.Object
                && section.TryGet("columns", out var columns) && columns.Kind == JsonKind.Array
                && section.TryGet("rows", out var rows) && rows.Kind == JsonKind.Array;
        }

        public static GoldenTable From(string name, JsonNode section)
        {
            if (!IsTable(section))
            {
                throw new InvalidOperationException("Golden " + name + ": la sección no tiene formato de tabla (columns/rows).");
            }

            var columnNodes = section["columns"].Items;
            var columns = new string[columnNodes.Count];
            for (var i = 0; i < columnNodes.Count; i++)
            {
                columns[i] = columnNodes[i].AsString();
            }

            var rows = section["rows"].Items;
            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Kind != JsonKind.Array || row.Items.Count != columns.Length)
                {
                    throw new InvalidOperationException("Golden " + name + ": la fila " + r + " no tiene " + columns.Length + " celdas.");
                }
            }

            return new GoldenTable(name, columns, rows);
        }

        // Índice de una columna; falla con un mensaje claro si no existe.
        public int ColumnIndex(string column)
        {
            if (_columnIndex.TryGetValue(column, out var index))
            {
                return index;
            }

            throw new InvalidOperationException("Golden " + Name + ": no existe la columna \"" + column + "\" (hay: "
                + string.Join(", ", Columns) + ").");
        }

        internal JsonNode CellAt(int row, int column)
        {
            return _rows[row].Items[column];
        }
    }

    // Una fila de una GoldenTable.
    public readonly struct GoldenRow
    {
        private readonly GoldenTable _table;

        internal GoldenRow(GoldenTable table, int index)
        {
            _table = table;
            Index = index;
        }

        public int Index { get; }

        public JsonNode Cell(string column)
        {
            return _table.CellAt(Index, _table.ColumnIndex(column));
        }

        // Celda "<16 hex>" con los bits IEEE-754 de un double.
        public double F64(string column)
        {
            return Cell(column).AsF64();
        }

        public long Int(string column)
        {
            return Cell(column).AsLong();
        }

        public string Text(string column)
        {
            return Cell(column).AsString();
        }

        // "[x=4038400000000000, n=1, out="24.3"]", para los mensajes de diferencias.
        public override string ToString()
        {
            if (_table == null)
            {
                return "[]";
            }

            var sb = new StringBuilder("[");
            for (var i = 0; i < _table.Columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(_table.Columns[i]).Append('=').Append(_table.CellAt(Index, i).ToString());
            }

            return sb.Append(']').ToString();
        }
    }
}
