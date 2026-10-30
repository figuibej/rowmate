using System;
using System.Text;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.Workout
{
    // Los 15 formatos de display de F-WKT-11 (+ F-APP-12; AC-WKT-11-01 a -16; 02-tech-design §2.12 de la It. 4a). Donde ya
    // hay una regla de la It. 1, la llama. Todo con Fmt, DartMath y TimeFormats: punto decimal, miles con punto solo en los
    // paneles y 24 h, sin la cultura del equipo (AC-WKT-11-16, Q-06). Iguales en es y en salvo los que pasan por el catálogo.
    public static class WorkoutFormats
    {
        // U+2014: el split de una repetición sin metros de trabajo (series_panels.dart:340).
        public const string NoRepSplit = "—";

        // 1 · Split en vivo: MM:SS con los minutos a dos dígitos; ≤ 0 → --:-- (rowing_data.dart:24-29; AC-BLE-01-02).
        // 133 → 02:13; 6000 → 100:00; 0, −5 → --:--.
        public static string LiveSplit(int seconds)
        {
            return TimeFormats.SplitOrDashes(seconds);
        }

        // 2 · SPM: un decimal con punto (immersive_workout_screen.dart:625). 24.25 → 24.3; 0 → 0.0.
        public static string Spm(double strokeRate)
        {
            return Fmt.Fixed(strokeRate, 1);
        }

        // 3 · Watts: entero crudo (Q-38), la unidad W aparte (:655-656). −10 → -10.
        public static string Watts(int watts)
        {
            return Fmt.Integer(watts);
        }

        // 4 · Distancia del HUD: entero sin separador de miles, la unidad m aparte (:698-699). 123456 → 123456.
        public static string HudDistance(int meters)
        {
            return Fmt.Integer(meters);
        }

        // 5 · kcal y pulso: enteros sin unidad (:717, :725). 142 → 142.
        public static string Count(int value)
        {
            return Fmt.Integer(value);
        }

        // 6 · Reloj de la sesión: H:MM:SS desde 1 h, MM:SS por debajo (:569-577; AC-BLE-01-03). 175 → 02:55; 3725 → 1:02:05.
        public static string SessionClock(long seconds)
        {
            return TimeFormats.Elapsed(seconds);
        }

        // 6 · lo mismo, en buffer y sin asignar (el conteo del diálogo de completado, QA-V4b-MOT-01). Devuelve cuántos escribió.
        public static int WriteSessionClock(long seconds, char[] buffer, int offset)
        {
            return TimeFormats.WriteElapsed(seconds, buffer, offset);
        }

        // 7 · Cuenta regresiva de un paso por tiempo: M:SS con los minutos sin cero a la izquierda (:456-457):
        // '${r ~/ 60}:${(r % 60).toString().padLeft(2, '0')}'. 65 → 1:05; 0 → 0:00; 359 999 → 5999:59.
        public static string Countdown(long remaining)
        {
            return Fmt.Integer(DartMath.TruncDiv(remaining, 60)) + ":" + TimeFormats.Pad2(DartMath.Mod(remaining, 60));
        }

        // 8 · Cuenta de un paso por distancia: el largo total, {metros}m (stepDurationMeters, :458; Q-51). 500 → 500m.
        public static string StepDistance(int meters, Localizer l10n)
        {
            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            return l10n.Format(L10nKeys.StepDurationMeters, MessageArg.Number("meters", meters));
        }

        // 8 · Cuenta de un paso sin meta: ? (stepDurationNone; DV-25: el legacy muestra "nullm").
        public static string StepNoGoal(Localizer l10n)
        {
            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            return l10n.Get(L10nKeys.StepDurationNone);
        }

        // 9 · Tiempo de vuelta: m:ss.d redondeado a las décimas totales, la mitad hacia arriba (series_panels.dart:9-14):
        // tenths = (s · 10).round(); '${tenths ~/ 600}:${((tenths % 600) / 10).toStringAsFixed(1).padLeft(4, '0')}'.
        // 166.667 → 2:46.7; 65 → 1:05.0; 59.96 → 1:00.0. Lo usan los parciales de la It. 5.
        public static string LapTime(double seconds)
        {
            var tenths = DartMath.Round(seconds * 10);
            var minutes = DartMath.TruncDiv(tenths, 600);
            var rest = DartMath.Mod(tenths, 600) / 10.0;
            return Fmt.Integer(minutes) + ":" + Fmt.Fixed(rest, 1).PadLeft(4, '0');
        }

        // 10 · Split de una repetición: m:ss redondeado al segundo (series_panels.dart:16-20); sin metros de trabajo, —
        // (:340). 114.4 → 1:54; 133.5 → 2:14.
        public static string RepSplit(double? seconds)
        {
            if (!seconds.HasValue)
            {
                return NoRepSplit;
            }

            var total = DartMath.Round(seconds.Value);
            return Fmt.Integer(DartMath.TruncDiv(total, 60)) + ":" + TimeFormats.Pad2(DartMath.Mod(total, 60));
        }

        // 11 · Metros de los paneles: miles con punto SIEMPRE, al estilo español aunque la app esté en inglés
        // (NumberFormat('#,##0', 'es'), series_panels.dart:22-24), y " m". 1245 → 1.245 m; 1 000 000 → 1.000.000 m.
        public static string PanelMeters(long meters)
        {
            var digits = Fmt.Integer(meters);
            var negative = digits.StartsWith("-", StringComparison.Ordinal);
            if (negative)
            {
                digits = digits.Substring(1);
            }

            var builder = new StringBuilder(digits.Length + digits.Length / 3 + 3);
            if (negative)
            {
                builder.Append('-');
            }

            for (var i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0)
                {
                    builder.Append('.');
                }

                builder.Append(digits[i]);
            }

            builder.Append(" m");
            return builder.ToString();
        }

        // 12 · Hora: HH:mm de 24 h (DateFormat('HH:mm'), series_panels.dart:98), de la hora que se le pasa (la local del
        // programador). 09:05 → 09:05; 23:59 → 23:59. Lo usa el reloj de pared de la 4b.
        public static string WallClock(DateTimeOffset localTime)
        {
            return TimeFormats.Pad2(localTime.Hour) + ":" + TimeFormats.Pad2(localTime.Minute);
        }

        // 13 · Etiqueta de objetivo del paso (expandido, con el objetivo ya progresado): la regla de AC-RUT-04-01 a -05
        // (interval_step.dart:79-109), con su 📈. ">160W · 24 spm · 📈 SPM +2/rep".
        public static string TargetLabel(IntervalStep step, Localizer l10n)
        {
            return StepLabels.Targets(step, l10n);
        }

        // 14 · Resumen de una rutina (AC-RUT-05-02; routine.dart:100-105): "1 serie · 1 descanso".
        public static string RoutineSummaryText(Routine routine, Localizer l10n)
        {
            return RoutineSummary.Text(routine, l10n);
        }

        // 15 · Resumen final: la distancia como {n} m sin separador de miles y las calorías como número
        // (immersive_workout_screen.dart:986, :991). 1484 → 1484 m. Lo usa el diálogo de rutina completada de la 4b.
        public static string FinalDistance(int meters)
        {
            return Fmt.Integer(meters) + " m";
        }

        public static string FinalCalories(int calories)
        {
            return Fmt.Integer(calories);
        }

        // El valor de hudTarget ("obj. {value}"): SPM y vatios mín. con su entero, el split con el formato de su objetivo
        // (TimeFormats.MinSec, AC-WKT-11-13: 125 → 02:05) (AC-WKT-10-05; D-ART-09).
        public static string TargetValue(int target)
        {
            return Fmt.Integer(target);
        }

        public static string SplitTargetValue(int targetSeconds)
        {
            return TimeFormats.MinSec(targetSeconds);
        }
    }
}
