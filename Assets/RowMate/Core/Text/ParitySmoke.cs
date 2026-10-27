using System.Collections.Generic;

namespace RowMate.Core.Text
{
    // Prueba de humo de la paridad numérica con Dart en un player (AC-BASE-01, D-17; ARCHITECTURE §16). Evalúa la tabla de
    // AC-BASE-01 (01-po-spec de la It. 1, §4.18) con Fmt y DartMath y devuelve las diferencias: vacía = paridad.
    // En dotnet la corre ParitySmokeTests; un player (IL2CPP o Mono) arrancado con -rowmate-parity-smoke la corre, la escribe
    // en el registro y sale (WP-B), así se ve que el AOT de IL2CPP no cambia el redondeo ni el formato.
    // Sin cultura ni formato de .NET: los textos de las diferencias también salen de Fmt.
    public static class ParitySmoke
    {
        // 1 decimal, 0 decimales y un par de casos de borde (el camino rápido y el lento de Fmt.Fixed).
        private static readonly (double Value, int Digits, string Expected)[] FixedCases =
        {
            (0.15, 1, "0.1"),
            (0.25, 1, "0.3"),
            (0.35, 1, "0.3"),
            (24.45, 1, "24.4"),
            (24.25, 1, "24.3"),
            (23.95, 1, "23.9"),
            (1.05, 1, "1.1"),
            (-0.05, 1, "-0.1"),
            (2.5, 0, "3"),
            (0.5, 0, "1"),
            (1.5, 0, "2"),
            (-0.5, 0, "-1"),
            (-0.04, 1, "-0.0"),
            (1.005, 2, "1.00"),
            (24.0, 1, "24.0"),
            (0.1, 20, "0.10000000000000000555"),
            (1e21, 2, "1e+21"),
        };

        // Redondeo a entero (la mitad se aleja del cero; nunca "−0").
        private static readonly (double Value, long Expected)[] RoundCases =
        {
            (0.5, 1),
            (2.5, 3),
            (-0.5, -1),
            (120.5, 121),
            (-0.05, 0),
            (0.49999999999999994, 0),
            (142.4, 142),
        };

        // 1 decimal y de vuelta a número (el valor que emite el simulador).
        private static readonly (double Value, int Digits, double Expected)[] RoundTripCases =
        {
            (24.25, 1, 24.3),
            (23.95, 1, 23.9),
            (23.99999999999998, 1, 24.0),
        };

        // Cantidad de casos que evalúa Run (para informarlo junto con el resultado).
        public static int CaseCount => FixedCases.Length + RoundCases.Length + RoundTripCases.Length + 2;

        public static IReadOnlyList<string> Run()
        {
            var differences = new List<string>();
            for (var i = 0; i < FixedCases.Length; i++)
            {
                var c = FixedCases[i];
                var actual = Fmt.Fixed(c.Value, c.Digits);
                if (actual != c.Expected)
                {
                    differences.Add("Fmt.Fixed(" + Fmt.DartToString(c.Value) + ", " + Fmt.Integer(c.Digits) + ") = \"" + actual
                        + "\", Dart da \"" + c.Expected + "\"");
                }
            }

            for (var i = 0; i < RoundCases.Length; i++)
            {
                var c = RoundCases[i];
                var actual = DartMath.Round(c.Value);
                if (actual != c.Expected)
                {
                    differences.Add("DartMath.Round(" + Fmt.DartToString(c.Value) + ") = " + Fmt.Integer(actual) + ", Dart da "
                        + Fmt.Integer(c.Expected));
                }
            }

            for (var i = 0; i < RoundTripCases.Length; i++)
            {
                var c = RoundTripCases[i];
                var actual = DartMath.RoundToFixed(c.Value, c.Digits);
                if (actual != c.Expected)
                {
                    differences.Add("DartMath.RoundToFixed(" + Fmt.DartToString(c.Value) + ", " + Fmt.Integer(c.Digits) + ") = "
                        + Fmt.DartToString(actual) + ", Dart da " + Fmt.DartToString(c.Expected));
                }
            }

            // Textos de la grilla de Dispositivo con los números de image-01.png (AC-BLE-01-05, AC-BASE-02).
            Check(differences, "Fmt.Fixed(23.5, 1)", Fmt.Fixed(23.5, 1), "23.5");
            Check(differences, "TimeFormats.SplitOrDashes(133)", TimeFormats.SplitOrDashes(133), "02:13");
            return differences;
        }

        private static void Check(List<string> differences, string what, string actual, string expected)
        {
            if (actual != expected)
            {
                differences.Add(what + " = \"" + actual + "\", Dart da \"" + expected + "\"");
            }
        }
    }
}
