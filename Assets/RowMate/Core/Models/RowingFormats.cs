using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // Textos de la grilla de Dispositivo (la regla; la grilla en pantalla es de la It. 2, F-BLE-11).
    // Siempre con punto y sin separador de miles, sea cual sea la región del equipo (AC-BASE-02): Fmt, no ToString().
    // legacy: lib/features/device/device_screen.dart:253-258
    public static class RowingFormats
    {
        // = d.strokeRate.toStringAsFixed(1): 24.0 → "24.0", 23.5 → "23.5", 24.25 → "24.3".
        public static string Spm(double strokeRate)
        {
            return Fmt.Fixed(strokeRate, 1);
        }

        // = d.powerWatts.toString(): entero sin unidad, con signo ASCII (-10 → "-10").
        public static string Watts(int watts)
        {
            return Fmt.Integer(watts);
        }

        // = '${d.distanceMeters}m': entero con la "m" pegada (10000 → "10000m").
        public static string Meters(int meters)
        {
            return Fmt.Integer(meters) + "m";
        }

        // = d.heartRate.toString(): entero sin unidad (la grilla lo muestra solo si es > 0).
        public static string HeartRate(int bpm)
        {
            return Fmt.Integer(bpm);
        }

        // = d.pace500mFormatted: la regla compartida del split (AC-BLE-01-02, AC-BLE-01-06).
        public static string Split(int seconds)
        {
            return TimeFormats.SplitOrDashes(seconds);
        }

        // = d.elapsedFormatted: la regla compartida del tiempo (AC-BLE-01-03, AC-BLE-01-06).
        public static string Elapsed(int seconds)
        {
            return TimeFormats.Elapsed(seconds);
        }
    }
}
