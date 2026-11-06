using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // Ayudas comunes de las pruebas de la escena contra el oráculo (scene_*.json): leer objetos de la orilla, los bytes canónicos
    // y su resumen FNV-1a de 64 bits (calculado igual en Dart, tools/oracle/engine/test/scene_vectors_test.dart), y la regla de
    // los empates de z (QA-G6aT-15 a: cada tramo de z iguales se ordena por (tipo, x, semilla) en los dos lados).
    internal static class SceneGolden
    {
        public static ShoreProp Prop(GoldenRow row)
        {
            return new ShoreProp(row.F64("x"), row.F64("z"), KindOf(row.Text("kind")), row.F64("scale"), (int)row.Int("seed"));
        }

        // El nombre de Dart (PropKind.name: "pine", "laneBuoy"…) al enum.
        public static PropKind KindOf(string dartName)
        {
            foreach (PropKind kind in Enum.GetValues(typeof(PropKind)))
            {
                var name = kind.ToString();
                if (string.Equals(char.ToLowerInvariant(name[0]) + name.Substring(1), dartName, StringComparison.Ordinal))
                {
                    return kind;
                }
            }

            throw new FormatException("tipo de objeto desconocido: " + dartName);
        }

        // tipo (1 byte), x, z y escala (bits big-endian), semilla (int32 big-endian).
        public static void AppendCanonical(List<byte> into, in ShoreProp p)
        {
            into.Add((byte)p.Kind);
            AppendBigEndian(into, BitConverter.DoubleToInt64Bits(p.X));
            AppendBigEndian(into, BitConverter.DoubleToInt64Bits(p.Z));
            AppendBigEndian(into, BitConverter.DoubleToInt64Bits(p.Scale));
            var s = p.Seed;
            into.Add((byte)(s >> 24));
            into.Add((byte)(s >> 16));
            into.Add((byte)(s >> 8));
            into.Add((byte)s);
        }

        public static string Fnv1a64(ReadOnlySpan<ShoreProp> props)
        {
            var bytes = new List<byte>(props.Length * 29);
            for (var i = 0; i < props.Length; i++)
            {
                AppendCanonical(bytes, props[i]);
            }

            ulong h = 0xcbf29ce484222325UL;
            const ulong Prime = 0x100000001b3UL;
            foreach (var b in bytes)
            {
                h ^= b;
                unchecked
                {
                    h *= Prime;
                }
            }

            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        // Ordena cada tramo de z iguales por (tipo, x, semilla): la regla de comparación de las listas visibles.
        public static List<ShoreProp> CanonicalTies(IEnumerable<ShoreProp> props)
        {
            var list = new List<ShoreProp>(props);
            var i = 0;
            while (i < list.Count)
            {
                var j = i + 1;
                while (j < list.Count && BitConverter.DoubleToInt64Bits(list[j].Z) == BitConverter.DoubleToInt64Bits(list[i].Z))
                {
                    j++;
                }

                if (j - i > 1)
                {
                    var run = list.GetRange(i, j - i);
                    run.Sort((a, b) =>
                    {
                        var c = a.Kind.CompareTo(b.Kind);
                        if (c != 0)
                        {
                            return c;
                        }

                        c = a.X.CompareTo(b.X);
                        return c != 0 ? c : a.Seed.CompareTo(b.Seed);
                    });
                    for (var r = 0; r < run.Count; r++)
                    {
                        list[i + r] = run[r];
                    }
                }

                i = j;
            }

            return list;
        }

        // Las filas del oráculo agrupadas por una columna double (la distancia).
        public static Dictionary<long, List<ShoreProp>> PropsByDistance(string file, string section)
        {
            var result = new Dictionary<long, List<ShoreProp>>();
            foreach (var row in GoldenVectors.Rows(file, section))
            {
                var key = BitConverter.DoubleToInt64Bits(row.F64("distance"));
                if (!result.TryGetValue(key, out var list))
                {
                    list = new List<ShoreProp>();
                    result[key] = list;
                }

                list.Add(Prop(row));
            }

            return result;
        }

        public static Dictionary<long, List<ShoreProp>> PropsBySegment(string file, string section)
        {
            var result = new Dictionary<long, List<ShoreProp>>();
            foreach (var row in GoldenVectors.Rows(file, section))
            {
                var k = row.Int("k");
                if (!result.TryGetValue(k, out var list))
                {
                    list = new List<ShoreProp>();
                    result[k] = list;
                }

                list.Add(Prop(row));
            }

            return result;
        }

        // Dos listas iguales bit a bit (null) o la primera diferencia.
        public static string? Diff(IReadOnlyList<ShoreProp> expected, IReadOnlyList<ShoreProp> actual)
        {
            if (expected.Count != actual.Count)
            {
                return "esperados " + expected.Count + " objetos, obtenidos " + actual.Count;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (!expected[i].Equals(actual[i]))
                {
                    return "objeto " + i + ": esperado " + expected[i] + ", obtenido " + actual[i];
                }
            }

            return null;
        }

        public static string F(double d)
        {
            return d.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void AppendBigEndian(List<byte> into, long bits)
        {
            for (var shift = 56; shift >= 0; shift -= 8)
            {
                into.Add((byte)(bits >> shift));
            }
        }
    }
}
