using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.Export
{
    // El archivo TCX de una sesión (F-STR-09; It. 12, 02-tech-design §2.1; legacy: TcxBuilder.build, tcx_builder.dart:7-80), línea
    // por línea y con las mismas cadenas (cada writeln = "\n", nunca AppendLine). Cambios decididos por la spec (§5, §8):
    // - sin los metadatos de Strava (F-STR-10 descartado): ni comentario <!-- ROWMATE:… --> ni descripción (DV-116);
    // - <Notes> con el nombre de la rutina DESPUÉS de la última vuelta (orden del esquema TCX v2, DV-117), con [SIM] en el
    //   simulador (DV-123), y escapado y limpio (TcxXml, DV-120);
    // - vueltas sin la rutina (TcxLaps, DV-118) y calorías con la base reparada en memoria y tiempo 0 sin error (DV-119).
    // It. 15 (#73): una vuelta con distancia negativa (el monitor reinició la distancia) sale en 0, los Trackpoint no cambian
    // (AC-D15-14, DV-144); y la actividad cierra con <Creator> RowMate y la versión de la app (AC-D15-15, DT15-07).
    // Determinista: mismo resultado para la misma sesión, puntos y zona (AC-STR-09-11). Sin puntos → ArgumentException (el
    // servicio nunca lo llama así: TcxExportRules.IsExportable).
    public static class TcxBuilder
    {
        public const string SimulatorMark = "[SIM]";

        // El nombre del dispositivo en <Creator> (AC-D15-15).
        public const string CreatorName = "RowMate";

        private const int MaxVersionPart = 65535;

        private const string Header = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

        private const string Root = "<TrainingCenterDatabase xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\""
            + " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\""
            + " xmlns:ns3=\"http://www.garmin.com/xmlschemas/ActivityExtension/v2\">";

        public static string Build(WorkoutSession session, IReadOnlyList<DataPoint> points, TcxOptions options)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (options == null || options.Zone == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (points.Count == 0)
            {
                throw new ArgumentException("TcxBuilder: la sesión no tiene puntos.", nameof(points));
            }

            var zone = options.Zone;
            var start = session.StartedAt;
            var basis = TcxExportRules.CalorieBasis(session,
                TcxExportRules.NeedsRepair(session) ? SessionStats.Compute(points) : SessionStats.Empty);
            var buf = new StringBuilder(512 + 260 * points.Count);

            // tcx:18-24
            Line(buf, Header);
            Line(buf, Root);
            Line(buf, "  <Activities>");
            Line(buf, "    <Activity Sport=\"Other\">");
            buf.Append("      <Id>");
            TcxTime.Append(buf, start, zone);
            Line(buf, "</Id>");

            // tcx:35-73
            var laps = TcxLaps.Group(points);
            for (var l = 0; l < laps.Count; l++)
            {
                var lap = laps[l];
                var first = points[lap.Start];
                var last = points[lap.Last];
                buf.Append("      <Lap StartTime=\"");
                TcxTime.Append(buf, start.AddSeconds(first.ElapsedSeconds), zone);
                Line(buf, "\">");
                Element(buf, "        <TotalTimeSeconds>", last.ElapsedSeconds - first.ElapsedSeconds, "</TotalTimeSeconds>");
                Element(buf, "        <DistanceMeters>", Math.Max(0, last.DistanceMeters - first.DistanceMeters), "</DistanceMeters>");
                Element(buf, "        <Calories>", TcxExportRules.LapCalories(basis.TotalCalories, lap.Count, basis.TotalSeconds), "</Calories>");
                Line(buf, "        <Intensity>Active</Intensity>");
                Line(buf, "        <TriggerMethod>Manual</TriggerMethod>");
                Line(buf, "        <Track>");
                for (var i = lap.Start; i <= lap.Last; i++)
                {
                    AppendPoint(buf, points[i], start, zone);
                }

                Line(buf, "        </Track>");
                Line(buf, "      </Lap>");
            }

            var notes = NotesText(session.RoutineName, options.Simulator);
            if (notes != null)
            {
                buf.Append("      <Notes>");
                TcxXml.AppendEscaped(buf, notes);
                Line(buf, "</Notes>");
            }

            AppendCreator(buf, options.AppVersion);

            // tcx:75-77
            Line(buf, "    </Activity>");
            Line(buf, "  </Activities>");
            Line(buf, "</TrainingCenterDatabase>");
            return buf.ToString();
        }

        // El texto de <Notes> sin escapar, o null si no va (DV-116, DV-123): el nombre de la rutina si no está vacío ni es solo
        // espacios; en el simulador, "[SIM] {nombre}" o "[SIM]" solo en una libre (o con un nombre en blanco).
        public static string? NotesText(string? routineName, bool simulator)
        {
            var hasName = !string.IsNullOrWhiteSpace(TcxXml.Clean(routineName ?? ""));
            if (simulator)
            {
                return hasName ? SimulatorMark + " " + routineName : SimulatorMark;
            }

            return hasName ? routineName : null;
        }

        // <Creator> (esquema TCX v2: después de Notes), con la sangría del golden.
        private static void AppendCreator(StringBuilder buf, string? appVersion)
        {
            var version = VersionParts(appVersion);
            Line(buf, "      <Creator xsi:type=\"Device_t\">");
            Line(buf, "        <Name>" + CreatorName + "</Name>");
            Line(buf, "        <UnitId>0</UnitId>");
            Line(buf, "        <ProductID>0</ProductID>");
            Line(buf, "        <Version>");
            Element(buf, "          <VersionMajor>", version[0], "</VersionMajor>");
            Element(buf, "          <VersionMinor>", version[1], "</VersionMinor>");
            Element(buf, "          <BuildMajor>", version[2], "</BuildMajor>");
            Element(buf, "          <BuildMinor>", version[3], "</BuildMinor>");
            Line(buf, "        </Version>");
            Line(buf, "      </Creator>");
        }

        // Los 4 números de la versión ("1.0.0" → 1, 0, 0, 0; "2.3" → 2, 3, 0, 0). Cada parte toma sus cifras del principio
        // ("1-beta" → 1); lo que falta o no tiene cifras, 0. Nunca negativos ni más de 65 535 (UInt16 en el esquema).
        public static int[] VersionParts(string? appVersion)
        {
            var result = new int[4];
            var parts = (appVersion ?? "").Trim().Split('.');
            for (var i = 0; i < result.Length && i < parts.Length; i++)
            {
                var value = 0;
                var text = parts[i];
                for (var c = 0; c < text.Length && text[c] >= '0' && text[c] <= '9'; c++)
                {
                    value = Math.Min(MaxVersionPart, value * 10 + (text[c] - '0'));
                }

                result[i] = value;
            }

            return result;
        }

        // tcx:44-69
        private static void AppendPoint(StringBuilder buf, DataPoint p, DateTimeOffset start, TimeZoneInfo zone)
        {
            Line(buf, "          <Trackpoint>");
            buf.Append("            <Time>");
            TcxTime.Append(buf, start.AddSeconds(p.ElapsedSeconds), zone);
            Line(buf, "</Time>");
            Element(buf, "            <DistanceMeters>", p.DistanceMeters, "</DistanceMeters>");
            if (p.HeartRate > 0)
            {
                Element(buf, "            <HeartRateBpm><Value>", p.HeartRate, "</Value></HeartRateBpm>");
            }

            // La condición es sobre el SPM crudo (0.3 da <Cadence>0</Cadence>, como el legacy; m-04).
            if (p.StrokeRate > 0)
            {
                Element(buf, "            <Cadence>", DartMath.Round(p.StrokeRate), "</Cadence>");
            }

            if (p.PowerWatts > 0)
            {
                Line(buf, "            <Extensions>");
                Line(buf, "              <ns3:TPX>");
                Element(buf, "                <ns3:Watts>", p.PowerWatts, "</ns3:Watts>");
                Line(buf, "              </ns3:TPX>");
                Line(buf, "            </Extensions>");
            }

            Line(buf, "          </Trackpoint>");
        }

        private static void Line(StringBuilder buf, string text)
        {
            buf.Append(text).Append('\n');
        }

        private static void Element(StringBuilder buf, string open, long value, string close)
        {
            buf.Append(open).Append(value.ToString(CultureInfo.InvariantCulture)).Append(close).Append('\n');
        }
    }
}
