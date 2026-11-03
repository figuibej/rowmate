using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it5 (05-series-parciales/01-po-spec §7 → 02-tech-design §5; aspecto: 04-design-notes §9): CAP-01 a CAP-26, DEMO-01 a -12
    // en una sola secuencia y VID-01 a -09. Salvo que diga otra cosa: español (es-AR), F-5, el remo de prueba R-4 (L(k) en los k − 0,5 de
    // la sesión: el toque cae en un k + 0,5 del reloj de prueba) con la cinta y la llave sin panel, sin pulsómetro, una base de prueba
    // propia (TempDatabase) y UNA MemoryPreferences por toma, sembrada con lo que pide la toma (en la secuencia de la demo, la misma
    // para todas sus tomas): ninguna toma lee ni escribe un archivo de preferencias (AC-HIS-20-04).
    // La regla de toma de la 4b (el instante exacto en ms enteros, falla si algo finito bloquea, inFlight en el manifest), con las
    // fases finitas de la 5 (SnapshotTakeRule: el resorte del alto a 0,15 dp; el alto por un dato, los fundidos, la flecha, el
    // empuje, la fila que entra o sale, la marca hasta los 1400 ms, las piezas, la serie que empieza o termina, el corrimiento, la
    // rueda y la capa de hover bloquean siempre). Un panel recién abierto o cerrado se toma ≥ 0,82 s después del toque o la toma
    // arranca con la preferencia en verdadero; los toques del arnés van con NavigationSubmitEvent (sin puntero: sin capa de hover).
    // CAP-11 a los 881,2 s y DEMO-11 a los 31,6 s (DT5-04). El desplazamiento, siempre en 0. El contorno de la zona del bote en CAP-03
    // y CAP-14 a CAP-18. S-LARGO (CAP-25) con UiTestHost.OverrideSeries. En toda toma, HudOverlap y WrenchAudit con los rectángulos
    // visuales de los paneles (SnapshotRunner) y en el manifest las ranuras, los rectángulos y sp (Describe).
    // Los rectángulos de 04-design-notes §9.2 usan s = 1,054; la app usa 1,0549 en TV (411,4 / 390): contra esos números se compara con
    // ± 1 dp, y contra la geometría con la ventana real (SeriesPanelGeometry.Place) con ± 0,5 dp (DD5-25).
    public static class SeriesCatalog
    {
        public const float DesignTolerance = 1.0f;
        public const float GeometryTolerance = 0.5f;

        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddCaptures(all);
                all.Add(Demo());
                AddVideos(all);
                AddAspectProbes(all);
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // Archivos que salen (la secuencia cuenta una por toma).
        public static int FileCount
        {
            get
            {
                var count = 0;
                foreach (var scenario in All)
                {
                    count += scenario.Kind == SnapshotKind.Sequence ? scenario.SequenceShots.Count : 1;
                }

                return count;
            }
        }

        // Lo que una toma deja en el manifest de los paneles: la regla, sp, los rectángulos visuales y de toque, y cada ranura con su
        // texto, su alfa y su escala. Vacío sin paneles (el libre, el reposo).
        public static List<string> Describe(UiTestHost host)
        {
            var lines = new List<string>();
            var column = host.SeriesColumn;
            if (column == null || !column.HasLayout)
            {
                return lines;
            }

            lines.Add("series: regla " + column.Column.Rule + ", sp " + F(column.Column.Scale) + ", desplazamiento " + F(column.ScrollBase) + ", abiertos "
                      + (column.Expanded.Laps ? "parciales" : "-") + "/" + (column.Expanded.Reps ? "repeticiones" : "-"));
            foreach (var panel in new[] { column.Laps, column.Reps })
            {
                if (!UiQuery.IsShown(panel))
                {
                    continue;
                }

                var visual = Visual(panel);
                var hit = ReferenceEquals(panel, column.Laps) ? column.LapsPlacement.HitRect : column.RepsPlacement.HitRect;
                lines.Add(panel.name + ": visual " + R(visual) + ", toque y " + F(hit.Top) + "-" + F(hit.Bottom) + ", G " + F(panel.VisualHeightBase));
                for (var i = 0; i < panel.SlotCount; i++)
                {
                    var slot = panel.SlotAt(i);
                    lines.Add("  " + slot.Name + " \"" + slot.Text + "\" α " + F(slot.Alpha) + " ×" + F(slot.Scale) + " " + R(slot.WorldRect)
                              + (slot.Line0 != null ? " [" + slot.Line0 + " / " + slot.Line1 + "]" : ""));
                }
            }

            return lines;
        }

#if ROWMATE_SIMULATOR
        // ── Las capturas (CAP-01 a CAP-26; §5.2) ──
        private static void AddCaptures(List<SnapshotScenario> all)
        {
            var cap01 = Five("CAP-01", "CAP-01-chips-tv", DeviceProfile.TV, "Series 600 a los 60,2 s, cerrados", s => Routine(s, "Series 600", 60.2));
            cap01.Checks = h => Expect(
                ("500 m y Reps cerrados", () => h.TextOf("hud-laps-chip") == "500 m" && h.TextOf("hud-reps-chip") == "Reps" && !Col(h).Expanded.Laps && !Col(h).Expanded.Reps),
                ("500 m en x 14,8-159,1 · y 210,9-246,7", () => Span(h, Col(h).Laps, 14.8, 159.1, 210.9, 246.7)),
                ("Reps en y 255,1-291,0", () => Span(h, Col(h).Reps, 14.8, 159.1, 255.1, 291.0)),
                ("el aire entre los dos, 8·sp", () => Near(Visual(Col(h).Reps).yMin - Visual(Col(h).Laps).yMax, 8 * Col(h).Column.Scale, 0.5)),
                ("a 32,7 dp de la zona del bote", () => Near(Layout(h).BoatZone.Top - Visual(Col(h).Reps).yMax, 32.7, DesignTolerance)),
                ("8 píldoras, Trabajo, 600m", () => Hud(h).Steps!.PillRow.Count == 8 && h.TextOf("hud-step-type") == "Trabajo" && h.TextOf("hud-countdown") == "600m"),
                ("la geometría", () => Geometry(h)));
            all.Add(cap01);

            var cap02 = Five("CAP-02", "CAP-02-parciales-abierto", DeviceProfile.TV, "170,2 s, parciales abierto", s => Routine(s, "Series 600", 170.2), laps: true);
            cap02.Checks = h => Expect(
                ("parciales en y 210,9-331,0", () => SpanY(Col(h).Laps, 210.9, 331.0)),
                ("Parciales 500 m a la escala de título del español (0,949 ± 0,01)", () => h.TextOf("hud-laps-title") == "Parciales 500 m"
                                                                                           && Near(Col(h).Laps.TitleScale, 0.949, 0.01)),
                ("0:15.0 y 60 m por la misma línea de base", () => h.TextOf("hud-laps-current") == "0:15.0" && h.TextOf("hud-laps-current-meters") == "60 m"
                                                                && Near(Slot(h, "hud-laps-current").WorldRect.yMax, Slot(h, "hud-laps-current-meters").WorldRect.yMax, 0.25)),
                ("los metros pegados a x 127", () => Near(Slot(h, "hud-laps-current-meters").WorldRect.xMax, Col(h).Column.Left + 127 * Col(h).Column.Scale, 0.5)),
                ("#1 2:05.0", () => h.TextOf("hud-laps-row-0") == "#1 2:05.0"),
                ("Rep 1/3 · 560 m", () => h.TextOf("hud-rep-progress") == "Rep 1/3 · 560 m"),
                ("el chip Reps en 339,4-375,3", () => SpanY(Col(h).Reps, 339.4, 375.3)),
                ("la geometría", () => Geometry(h)));
            all.Add(cap02);

            var cap03 = Five("CAP-03", "CAP-03-los-dos-abiertos", DeviceProfile.TV, "250,2 s, los dos abiertos, contorno", s => Routine(s, "Series 600", 250.2),
                laps: true, reps: true, outline: true);
            cap03.Checks = h => Expect(
                ("parciales 210,9-305,7: 0:50.0 200 m, sin filas", () => SpanY(Col(h).Laps, 210.9, 305.7) && h.TextOf("hud-laps-current") == "0:50.0"
                                                                       && h.TextOf("hud-laps-current-meters") == "200 m" && h.TextOf("hud-laps-row-0") == null),
                ("Rep 2/3 · 200 m", () => h.TextOf("hud-rep-progress") == "Rep 2/3 · 200 m"),
                ("repeticiones 314,1-377,4: Rep 1 2:05 680 m", () => SpanY(Col(h).Reps, 314.1, 377.4) && h.TextOf("hud-reps-row-0") == "Rep 1 2:05 680 m"),
                ("los metros pegados a x 127", () => Near(Slot(h, "hud-reps-row-0").WorldRect.xMax, Col(h).Column.Left + 127 * Col(h).Column.Scale, 0.5)),
                ("la zona del bote: solo por su franja de afuera", () => Visual(Col(h).Reps).xMax <= Layout(h).WindowWidth / 2 - 44 * Layout(h).Scale + 0.5),
                ("la geometría", () => Geometry(h)));
            all.Add(cap03);

            var cap04 = Five("CAP-04", "CAP-04-dos-repeticiones", DeviceProfile.TV, "380,2 s, los dos", s => Routine(s, "Series 600", 380.2), laps: true, reps: true);
            cap04.Checks = h => Expect(
                ("parciales 210,9-305,7: 0:10.0 40 m", () => SpanY(Col(h).Laps, 210.9, 305.7) && h.TextOf("hud-laps-current") == "0:10.0"
                                                           && h.TextOf("hud-laps-current-meters") == "40 m"),
                ("Rep 3/3 · 40 m", () => h.TextOf("hud-rep-progress") == "Rep 3/3 · 40 m"),
                ("repeticiones 314,1-400,6: Rep 2 arriba, Rep 1 abajo", () => SpanY(Col(h).Reps, 314.1, 400.6) && h.TextOf("hud-reps-row-0") == "Rep 2 2:05 680 m"
                                                                           && h.TextOf("hud-reps-row-1") == "Rep 1 2:05 680 m"),
                ("las columnas alineadas", () => Near(Slot(h, "hud-reps-row-0").WorldRect.xMax, Slot(h, "hud-reps-row-1").WorldRect.xMax, 0.25)),
                ("la geometría", () => Geometry(h)));
            all.Add(cap04);

            var cap05 = Five("CAP-05", "CAP-05-vacios-parejos", DeviceProfile.TV, "190,2 s, los dos (Descanso)", s => Routine(s, "Series 600", 190.2), laps: true, reps: true);
            cap05.Checks = h => Expect(
                ("parciales 210,9-322,6", () => SpanY(Col(h).Laps, 210.9, 322.6)),
                ("Sin parciales / en este paso", () => Lines(h, "hud-laps-empty") == "Sin parciales / en este paso"),
                ("Rep 1/3 · 640 m", () => h.TextOf("hud-rep-progress") == "Rep 1/3 · 640 m"),
                ("repeticiones 331,0-419,5: Sin repeticiones / completas", () => SpanY(Col(h).Reps, 331.0, 419.5) && Lines(h, "hud-reps-empty") == "Sin repeticiones / completas"),
                ("la barra en Descanso", () => h.TextOf("hud-step-type") == "Descanso"),
                ("la geometría", () => Geometry(h)));
            all.Add(cap05);

            var cap06 = Five("CAP-06", "CAP-06-segundo-de-atraso", DeviceProfile.TV, "180,6 s, los dos", s => Routine(s, "Series 600", 180.6), laps: true, reps: true);
            cap06.Checks = h => Expect(
                ("la barra en Descanso y 0:20", () => h.TextOf("hud-step-type") == "Descanso" && h.TextOf("hud-countdown") == "0:20"),
                ("parciales todavía con el trabajo (P-01): 0:25.0 100 m, #1 2:05.0, Rep 1/3 · 600 m", () => SpanY(Col(h).Laps, 210.9, 331.0)
                    && h.TextOf("hud-laps-current") == "0:25.0" && h.TextOf("hud-laps-current-meters") == "100 m" && h.TextOf("hud-laps-row-0") == "#1 2:05.0"
                    && h.TextOf("hud-rep-progress") == "Rep 1/3 · 600 m"),
                ("repeticiones vacío (339,4-428,0)", () => SpanY(Col(h).Reps, 339.4, 428.0) && h.TextOf("hud-reps-empty") == "Sin repeticiones completas"));
            all.Add(cap06);

            var cap07 = Five("CAP-07", "CAP-07-fuera-de-la-serie", DeviceProfile.TV, "545,2 s, los dos en verdadero", s => Routine(s, "Series 600", 545.2), laps: true, reps: true);
            cap07.Checks = h => Expect(
                ("parciales 210,9-299,4: solo el vacío, sin la línea Rep", () => SpanY(Col(h).Laps, 210.9, 299.4) && h.TextOf("hud-laps-empty") != null
                                                                               && h.TextOf("hud-rep-progress") == null),
                ("sin el panel de repeticiones", () => !h.IsShown("hud-reps-panel")));
            all.Add(cap07);

            var cap08 = Five("CAP-08", "CAP-08-cuatro-vueltas", DeviceProfile.TV, "Fondo 3000 con R-4A a los 600,2 s, parciales", s => Routine(s, "Fondo 3000", 600.2),
                laps: true, reading: RowerScripts.R4A);
            cap08.Checks = h => Expect(
                ("parciales 210,9-377,4: 0:50.0 250 m", () => SpanY(Col(h).Laps, 210.9, 377.4) && h.TextOf("hud-laps-current") == "0:50.0"
                                                            && h.TextOf("hud-laps-current-meters") == "250 m"),
                ("#5 #4 #3 1:40.0 y #2 2:05.0, sin #1", () => h.TextOf("hud-laps-row-0") == "#5 1:40.0" && h.TextOf("hud-laps-row-1") == "#4 1:40.0"
                                                             && h.TextOf("hud-laps-row-2") == "#3 1:40.0" && h.TextOf("hud-laps-row-3") == "#2 2:05.0"),
                ("los tiempos alineados", () => Near(Slot(h, "hud-laps-row-0").WorldRect.xMax, Slot(h, "hud-laps-row-3").WorldRect.xMax, 0.25)),
                ("sin línea Rep ni chip Reps", () => h.TextOf("hud-rep-progress") == null && !h.IsShown("hud-reps-panel")));
            all.Add(cap08);

            var cap09 = Five("CAP-09", "CAP-09-interpolada", DeviceProfile.TV, "Trabajo 1000 con R-3 a los 170,2 s, parciales", s => Routine(s, "Trabajo 1000", 170.2),
                laps: true, reading: RowerScripts.R3);
            cap09.Checks = h => Expect(
                ("parciales 210,9-307,8", () => SpanY(Col(h).Laps, 210.9, 307.8)),
                ("0:03.3 10 m; #1 2:46.7", () => h.TextOf("hud-laps-current") == "0:03.3" && h.TextOf("hud-laps-current-meters") == "10 m"
                                                && h.TextOf("hud-laps-row-0") == "#1 2:46.7"));
            all.Add(cap09);

            var cap10 = Five("CAP-10", "CAP-10-tres-repeticiones", DeviceProfile.TV, "Cinco reps a los 545,2 s, los dos", s => Routine(s, "Cinco reps", 545.2),
                laps: true, reps: true);
            cap10.Checks = h => Expect(
                ("parciales 210,9-305,7: 0:05.0 20 m; Rep 5/5 · 20 m", () => SpanY(Col(h).Laps, 210.9, 305.7) && h.TextOf("hud-laps-current") == "0:05.0"
                                                                           && h.TextOf("hud-laps-current-meters") == "20 m" && h.TextOf("hud-rep-progress") == "Rep 5/5 · 20 m"),
                ("repeticiones 314,1-423,7: Rep 4, Rep 3, Rep 2 a escala 1", () => SpanY(Col(h).Reps, 314.1, 423.7) && h.TextOf("hud-reps-row-0") == "Rep 4 2:05 540 m"
                    && h.TextOf("hud-reps-row-1") == "Rep 3 2:05 540 m" && h.TextOf("hud-reps-row-2") == "Rep 2 2:05 540 m" && Near(Col(h).Reps.RowsScaleNow, 1, 1e-4)));
            all.Add(cap10);

            var cap11 = Five("CAP-11", "CAP-11-escala-comun", DeviceProfile.TV, "Pirámide 500 a los 881,2 s, los dos (DT5-04)", s => Routine(s, "Pirámide 500", 881.2),
                laps: true, reps: true);
            cap11.Checks = h => Expect(
                ("SPM sin objetivo", () => !Hud(h).SpmHasTarget),
                ("parciales 210,9-305,7: 0:03.0 12 m; Rep 2/4 · 12 m", () => SpanY(Col(h).Laps, 210.9, 305.7) && h.TextOf("hud-laps-current") == "0:03.0"
                                                                           && h.TextOf("hud-laps-current-meters") == "12 m" && h.TextOf("hud-rep-progress") == "Rep 2/4 · 12 m"),
                ("repeticiones 314,1-377,4: Rep 1 2:05 1.112 m a 0,969 ± 0,01", () => SpanY(Col(h).Reps, 314.1, 377.4) && h.TextOf("hud-reps-row-0") == "Rep 1 2:05 1.112 m"
                                                                                       && Near(Col(h).Reps.RowsScaleNow, 0.969, 0.012)),
                ("sin sello", () => !Hud(h).Steps!.Seal.IsShowing));
            all.Add(cap11);

            var cap12 = Five("CAP-12", "CAP-12-en-ingles", DeviceProfile.TV, "En inglés, CAP-03", s => Routine(s, "Series 600", 250.2), laps: true, reps: true, language: "en-US");
            cap12.Checks = h => Expect(
                ("500 m splits y Previous reps a escala 1", () => h.TextOf("hud-laps-title") == "500 m splits" && h.TextOf("hud-reps-title") == "Previous reps"
                                                                 && Near(Col(h).Laps.TitleScale, 1, 1e-6)),
                ("0:50.0 200 m; Rep 2/3 · 200 m; Rep 1 2:05 680 m", () => h.TextOf("hud-laps-current") == "0:50.0" && h.TextOf("hud-rep-progress") == "Rep 2/3 · 200 m"
                                                                          && h.TextOf("hud-reps-row-0") == "Rep 1 2:05 680 m"),
                ("los rectángulos de CAP-03", () => SpanY(Col(h).Laps, 210.9, 305.7) && SpanY(Col(h).Reps, 314.1, 377.4)));
            all.Add(cap12);

            var cap13 = Five("CAP-13", "CAP-13-vacios-en-ingles", DeviceProfile.TV, "En inglés, CAP-05", s => Routine(s, "Series 600", 190.2), laps: true, reps: true,
                language: "en-US");
            cap13.Checks = h => Expect(
                ("No splits / in this step", () => Lines(h, "hud-laps-empty") == "No splits / in this step"),
                ("No completed / reps", () => Lines(h, "hud-reps-empty") == "No completed / reps"),
                ("Rep 1/3 · 640 m", () => h.TextOf("hud-rep-progress") == "Rep 1/3 · 640 m"),
                ("los rectángulos de CAP-05", () => SpanY(Col(h).Laps, 210.9, 322.6) && SpanY(Col(h).Reps, 331.0, 419.5)));
            all.Add(cap13);

            var cap14 = Five("CAP-14", "CAP-14-p360", DeviceProfile.P360, "CAP-04 en P360, contorno", s => Routine(s, "Series 600", 380.2), laps: true, reps: true, outline: true);
            cap14.Checks = h => Expect(
                ("parciales x 12,9-139,4, y 184,7-267,8", () => Span(h, Col(h).Laps, 12.9, 139.4, 184.7, 267.8)),
                ("repeticiones 275,2-350,9", () => SpanY(Col(h).Reps, 275.2, 350.9)),
                ("ni Split ni Watts", () => !Overlap(Visual(Col(h).Laps), h.BoundsOf("hud-split")) && !Overlap(Visual(Col(h).Laps), h.BoundsOf("hud-watts"))),
                ("la geometría", () => Geometry(h)));
            all.Add(cap14);

            var cap15 = Five("CAP-15", "CAP-15-th-chips", DeviceProfile.TH, "CAP-01 en TH, contorno", s => Routine(s, "Series 600", 60.2), outline: true);
            cap15.Checks = h => Expect(
                ("ColumnScroll", () => Col(h).Column.Rule == SeriesFitRule.ColumnScroll),
                ("los dos chips enteros, sin desplazar", () => SpanY(Col(h).Laps, 210.9, 246.7) && SpanY(Col(h).Reps, 255.1, 291.0) && Col(h).ScrollBase == 0),
                ("18,8 dp hasta el piso (309,8)", () => Near(Col(h).Column.Floor - Visual(Col(h).Reps).yMax, 18.8, DesignTolerance)),
                ("sin máscara", () => Col(h).ScrollMaxBase <= 0.001),
                ("la zona del bote, a 96,8 dp a la derecha", () => Near(Layout(h).BoatZone.Left - Visual(Col(h).Laps).xMax, 96.8, DesignTolerance)));
            all.Add(cap15);

            var cap16 = Five("CAP-16", "CAP-16-th-mascara", DeviceProfile.TH, "CAP-04 en TH, contorno", s => Routine(s, "Series 600", 380.2), laps: true, reps: true, outline: true);
            cap16.Checks = h => Expect(
                ("ColumnScroll con desplazamiento 0", () => Col(h).Column.Rule == SeriesFitRule.ColumnScroll && Col(h).ScrollBase == 0 && Col(h).ScrollMaxBase > 0),
                ("la cabecera y 0:10.0 40 m al 100 %", () => Slot(h, "hud-laps-title").Alpha > 0.999f && Slot(h, "hud-laps-current").Alpha > 0.999f),
                ("Rep 3/3 · 40 m a 0,925 ± 0,02", () => Near(Slot(h, "hud-rep-progress").Alpha, 0.925, 0.02)),
                ("repeticiones no se ve ni responde (debajo del piso)", () => Visual(Col(h).Reps).yMin >= Col(h).Column.Floor - 0.5),
                ("la geometría", () => Geometry(h)));
            all.Add(cap16);

            var cap17 = Five("CAP-17", "CAP-17-l640-mascara", DeviceProfile.L640, "CAP-04 en L640, contorno", s => Routine(s, "Series 600", 380.2), laps: true, reps: true,
                outline: true);
            cap17.Checks = h => Expect(
                ("ColumnScroll", () => Col(h).Column.Rule == SeriesFitRule.ColumnScroll),
                ("el panel no se angosta: termina en x 139,4", () => Near(Visual(Col(h).Laps).xMax, 139.4, DesignTolerance)),
                ("a 39,8 de la zona", () => Near(Layout(h).BoatZone.Left - Visual(Col(h).Laps).xMax, 39.8, DesignTolerance)),
                ("la geometría", () => Geometry(h)));
            all.Add(cap17);

            var cap18 = Five("CAP-18", "CAP-18-w1280", DeviceProfile.W1280, "W1280 a los 340,2 s, los dos, contorno", s => Routine(s, "Series 600", 340.2), laps: true,
                reps: true, outline: true);
            cap18.Checks = h => Expect(
                ("sp 1,20 ± 0,005", () => Near(Col(h).Column.Scale, 1.20, 0.005)),
                ("parciales x 22,4-186,8, y 320,2-457,0", () => Span(h, Col(h).Laps, 22.4, 186.8, 320.2, 457.0)),
                ("0:15.0 60 m, #1 2:05.0, Rep 2/3 · 560 m", () => h.TextOf("hud-laps-current") == "0:15.0" && h.TextOf("hud-laps-current-meters") == "60 m"
                                                               && h.TextOf("hud-laps-row-0") == "#1 2:05.0" && h.TextOf("hud-rep-progress") == "Rep 2/3 · 560 m"),
                ("repeticiones 466,6-538,6: Rep 1 2:05 680 m", () => SpanY(Col(h).Reps, 466.6, 538.6) && h.TextOf("hud-reps-row-0") == "Rep 1 2:05 680 m"),
                ("todo sin desplazar y al 100 %", () => Col(h).Laps.RowsScrollBase == 0 && Col(h).Reps.RowsScrollBase == 0 && AllOpaque(Col(h).Laps) && AllOpaque(Col(h).Reps)),
                ("la geometría", () => Geometry(h)));
            all.Add(cap18);

            var cap19 = Five("CAP-19", "CAP-19-cuenta-grande-con-chips", DeviceProfile.TV, "197,6 s", s => Routine(s, "Series 600", 197.6));
            cap19.Checks = h => Expect(
                ("la cuenta grande 3 en su disco", () => h.IsShown("hud-big-countdown") && h.TextOf("hud-big-countdown") == "3"),
                ("a 8,4 dp de los chips", () => Near(Hud(h).Callouts.Disc.Left - Visual(Col(h).Laps).xMax, 8.4, 0.5)),
                ("Próximo: Trabajo", () => h.IsShown("hud-up-next") && h.TextOf("hud-up-next-type")?.Trim() == "Trabajo"));
            all.Add(cap19);

            var cap20 = Five("CAP-20", "CAP-20-cuenta-grande-con-parciales", DeviceProfile.TV, "197,6 s, parciales", s => Routine(s, "Series 600", 197.6), laps: true);
            cap20.Checks = h => Expect(
                ("la cuenta grande en su lugar", () => h.IsShown("hud-big-countdown") && !Hud(h).Callouts.BlockedByPanels),
                ("a 8,4 dp de parciales abierto (210,9-322,6)", () => SpanY(Col(h).Laps, 210.9, 322.6) && Near(Hud(h).Callouts.Disc.Left - Visual(Col(h).Laps).xMax, 8.4, 0.5)),
                ("el vacío y Rep 1/3 · 668 m", () => h.TextOf("hud-laps-empty") != null && h.TextOf("hud-rep-progress") == "Rep 1/3 · 668 m"),
                ("el chip Reps en 331,0-366,8", () => SpanY(Col(h).Reps, 331.0, 366.8)),
                ("0:03 de la barra", () => h.TextOf("hud-countdown") == "0:03"));
            all.Add(cap20);

            var cap21 = Five("CAP-21", "CAP-21-th-cuenta-grande", DeviceProfile.TH, "CAP-19 en TH, los dos", s => Routine(s, "Series 600", 197.6), laps: true, reps: true);
            cap21.Checks = h => Expect(
                ("la cuenta grande en su lugar de TH", () => h.IsShown("hud-big-countdown") && !Hud(h).Callouts.BlockedByPanels),
                ("el vacío entero", () => Slot(h, "hud-laps-empty").Alpha > 0.999f),
                ("la línea Rep todavía no se ve (pasa el piso)", () => Slot(h, "hud-rep-progress").Alpha <= 0.01f),
                ("la geometría", () => Geometry(h)));
            all.Add(cap21);

            var cap22 = Five("CAP-22", "CAP-22-pausa", DeviceProfile.TV, "Pausa a los 250,2 s, toma a los 252,2", s => Paused(s), laps: true, reps: true);
            cap22.Checks = h => Expect(
                ("Reanudar y PAUSA en su disco", () => h.IsShown("hud-resume") && h.IsShown("hud-paused-label")),
                ("parciales 210,9-305,7 con 0:50.0 200 m quieto", () => SpanY(Col(h).Laps, 210.9, 305.7) && h.TextOf("hud-laps-current") == "0:50.0"
                                                                      && h.TextOf("hud-laps-current-meters") == "200 m"),
                ("repeticiones 314,1-377,4", () => SpanY(Col(h).Reps, 314.1, 377.4)),
                ("PAUSA a 8,4 dp", () => Near(Hud(h).Callouts.Disc.Left - Visual(Col(h).Laps).xMax, 8.4, 0.5)));
            all.Add(cap22);

            var cap23 = Five("CAP-23", "CAP-23-th-pausa", DeviceProfile.TH, "CAP-22 en TH", s => Paused(s), laps: true, reps: true);
            cap23.Checks = h => Expect(
                ("PAUSA en su lugar", () => h.IsShown("hud-paused-label")),
                ("0:50.0 200 m", () => h.TextOf("hud-laps-current") == "0:50.0" && h.TextOf("hud-laps-current-meters") == "200 m"),
                ("la columna como CAP-16", () => Col(h).Column.Rule == SeriesFitRule.ColumnScroll && Col(h).ScrollBase == 0));
            all.Add(cap23);

            var cap24 = Five("CAP-24", "CAP-24-sello-y-columna", DeviceProfile.TV, "Prueba 4a a los 90,6 s, los dos", s => Routine(s, "Prueba 4a", 90.6), laps: true,
                reps: true);
            cap24.Checks = h => Expect(
                ("el sello No alcanzado · 150 / 160 W · 24.0 / 22 spm", () => Hud(h).Steps!.Seal.IsShowing && Hud(h).Steps!.Seal.Text == "No alcanzado · 150 / 160 W · 24.0 / 22 spm"),
                ("la columna ya subió: parciales 210,9-305,7 con 1:00.0 240 m y Rep 1/2 · 240 m", () => SpanY(Col(h).Laps, 210.9, 305.7)
                    && h.TextOf("hud-laps-current") == "1:00.0" && h.TextOf("hud-laps-current-meters") == "240 m" && h.TextOf("hud-rep-progress") == "Rep 1/2 · 240 m"),
                ("repeticiones vacío, 314,1-402,7", () => SpanY(Col(h).Reps, 314.1, 402.7) && h.TextOf("hud-reps-empty") != null),
                ("la geometría", () => Geometry(h)));
            all.Add(cap24);

            var cap25 = Five("CAP-25", "CAP-25-s-largo", DeviceProfile.TV, "S-LARGO (OverrideSeries), los dos", s => Largo(s), laps: true, reps: true);
            cap25.Checks = h => Expect(
                ("parciales 210,9-400,6 (180)", () => SpanY(Col(h).Laps, 210.9, 400.6)),
                ("repeticiones 409,0-518,6 (104)", () => SpanY(Col(h).Reps, 409.0, 518.6)),
                ("la cifra a 0,652; #1999 a #1996 a 0,932; la línea a 0,712; las repeticiones a 0,638 (± 0,02 de lo medido)",
                    () => Near(Col(h).Laps.HeroScale, 0.652, 0.02) && Near(Col(h).Laps.RowsScaleNow, 0.932, 0.02) && Near(Col(h).Laps.RepLineScale, 0.712, 0.02)
                          && Near(Col(h).Reps.RowsScaleNow, 0.638, 0.02)),
                ("nada cortado", () => !Contains(h, "…")),
                ("nada fuera del vidrio", () => InsideGlass(Col(h).Laps) && InsideGlass(Col(h).Reps)));
            all.Add(cap25);

            var cap26 = Five("CAP-26", "CAP-26-anillo-de-foco", DeviceProfile.W1280, "CAP-18 sin foco y Tab × 3", s => Ring(s), laps: true, reps: true);
            cap26.Checks = h => Expect(
                ("el foco en parciales, con el anillo", () => h.Focused == Col(h).Laps && Col(h).Laps.HasFocusRing),
                ("el anillo blanco de x 18,4-190,8 e y 316,2-461,0", () => Near(Visual(Col(h).Laps).xMin - 4, 18.4, DesignTolerance)
                                                                          && Near(Visual(Col(h).Laps).yMin - 4, 316.2, DesignTolerance)
                                                                          && Near(Visual(Col(h).Laps).yMax + 4, 461.0, DesignTolerance)),
                ("ni el anillo ni el halo tocan SPM ni el panel de abajo", () => Visual(Col(h).Laps).yMin - 8 > h.BoundsOf("hud-spm").yMax
                                                                                && Visual(Col(h).Laps).yMax + 8 < Visual(Col(h).Reps).yMin));
            all.Add(cap26);
        }

        private static IEnumerator Paused(ScenarioScript s)
        {
            yield return Routine(s, "Series 600", 250.2);
            yield return s.Do(h => h.Tap("hud-pause"));
            yield return s.AdvanceTo(252.2);
        }

        private static IEnumerator Largo(ScenarioScript s)
        {
            yield return Routine(s, "Series 600", 170.2);
            yield return s.Do(h => h.OverrideSeries(SeriesSeeds.Largo(h.App.Texts.Value, h.HudState!.Session)));
            yield return s.Frames(3);
        }

        private static IEnumerator Ring(ScenarioScript s)
        {
            yield return Routine(s, "Series 600", 340.2);
            yield return s.Do(h =>
            {
                h.PressTab();
                h.PressTab();
                h.PressTab();
            });
            yield return s.Frames(3);
        }

        // ── La demo (DEMO-01 a -12, una secuencia; §5.3) ──
        private static SnapshotScenario Demo()
        {
            var demo = Five("DEMO", "DEMO", DeviceProfile.W1280, "La demo de la It. 5", DemoSteps, simulator: true);
            demo.Kind = SnapshotKind.Sequence;
            foreach (var (id, stem, state) in new[]
                     {
                         ("DEMO-01", "DEMO-01-workout", "Workout en reposo; Series 600 primera"),
                         ("DEMO-02", "DEMO-02-chip", "Series 600 a los 10,2 s"),
                         ("DEMO-03", "DEMO-03-parciales", "500 m a los 12 s; toma a los 12,8"),
                         ("DEMO-04", "DEMO-04-serie", "A los 60,2 s"),
                         ("DEMO-05", "DEMO-05-repeticiones", "Reps a los 61; toma a los 61,8"),
                         ("DEMO-06", "DEMO-06-primera-vuelta", "A los 170,2 s"),
                         ("DEMO-07", "DEMO-07-repeticion", "A los 230,2 s"),
                         ("DEMO-08", "DEMO-08-desconexion", "Simular desconexión a los 240; toma a los 242,2"),
                         ("DEMO-09", "DEMO-09-sigue", "Panel cerrado; a los 250,2 s"),
                         ("DEMO-10", "DEMO-10-reposo", "Terminar → Terminar; 800 ms de la salida"),
                         ("DEMO-11", "DEMO-11-recordados", "Series 600 otra vez; a los 31,6 s"),
                         ("DEMO-12", "DEMO-12-cerrados", "Cerrar los dos; Terminar → Terminar; 800 ms de la salida"),
                     })
            {
                demo.Shot(id, stem, state);
            }

            return demo;
        }

        private static int _demoMeters;

        private static IEnumerator DemoSteps(ScenarioScript s)
        {
            yield return s.Tap("tab-workout");
            yield return s.Shot("DEMO-01", h => Expect(("Workout en reposo con Series 600 primera", () => h.Immersive == null
                && h.WorkoutScreen?.List.CardAt(0)?.Routine.Name == "Series 600")));

            yield return s.StartRoutine("Series 600");
            yield return s.AdvanceTo(10.2);
            yield return s.Shot("DEMO-02", h => Expect(("el chip 500 m solo, cerrado", () => h.TextOf("hud-laps-chip") == "500 m" && !Col(h).Expanded.Laps
                                                                                             && !h.IsShown("hud-reps-panel")),
                ("en y 320,2-361,0", () => SpanY(Col(h).Laps, 320.2, 361.0))));

            yield return s.AdvanceTo(12);
            yield return s.Do(h => h.TapText("500 m"));
            yield return s.AdvanceTo(12.8);
            yield return s.Shot("DEMO-03", h => Expect(("parciales abierto (84)", () => Col(h).Expanded.Laps && Near(Col(h).Laps.FinalHeightBase, 84, 0.01)),
                ("Parciales 500 m; Sin parciales / en este paso; sin línea Rep", () => h.TextOf("hud-laps-title") == "Parciales 500 m"
                                                                                       && Lines(h, "hud-laps-empty") == "Sin parciales / en este paso" && h.TextOf("hud-rep-progress") == null),
                ("immersive.lapsExpanded en verdadero", () => h.Preferences.TryGetBool(SeriesPanelPrefs.LapsKey, out var laps) && laps)));

            yield return s.AdvanceTo(60.2);
            yield return s.Shot("DEMO-04", h => Expect(("parciales (90): 0:30.0 y la línea Rep", () => Near(Col(h).Laps.FinalHeightBase, 90, 0.01)
                                                                                            && h.TextOf("hud-laps-current") == "0:30.0" && h.TextOf("hud-rep-progress") != null),
                ("el chip Reps en 437,8-478,6", () => SpanY(Col(h).Reps, 437.8, 478.6))));

            yield return s.AdvanceTo(61);
            yield return s.Do(h => h.TapText("Reps"));
            yield return s.AdvanceTo(61.8);
            yield return s.Shot("DEMO-05", h => Expect(("Series anteriores abierto y vacío (437,8-538,6)", () => Col(h).Expanded.Reps && SpanY(Col(h).Reps, 437.8, 538.6)
                && Lines(h, "hud-reps-empty") == "Sin repeticiones / completas")));

            yield return s.AdvanceTo(170.2);
            yield return s.Shot("DEMO-06", h => Expect(("parciales (114) con #1", () => Near(Col(h).Laps.FinalHeightBase, 114, 0.01) && (h.TextOf("hud-laps-row-0") ?? "").StartsWith("#1 ", StringComparison.Ordinal)),
                ("repeticiones con sus dos renglones a la vista (RowsScroll: 83,2 de 84)", () => Near(Col(h).Reps.FinalHeightBase, 83.2, 0.15)
                                                                                                && Slot(h, "hud-reps-empty").Alpha > 0.99f)));

            yield return s.AdvanceTo(230.2);
            yield return s.Shot("DEMO-07", h => Expect(("Rep 1 en repeticiones; Rep 2/3 en la línea", () => (h.TextOf("hud-reps-row-0") ?? "").StartsWith("Rep 1 ", StringComparison.Ordinal)
                                                                                                        && (h.TextOf("hud-rep-progress") ?? "").StartsWith("Rep 2/3 · ", StringComparison.Ordinal))));

            yield return s.AdvanceTo(240);
            yield return s.Do(h => _demoMeters = Meters(h));
            yield return s.Tap("sim-open");
            yield return s.TapInView("sim-drop-rower");
            yield return s.AdvanceTo(242.2);
            yield return s.Shot("DEMO-08", h => Expect(("los metros de la vuelta en curso quietos", () => Meters(h) == _demoMeters),
                ("el panel del simulador sin tapar Split, Watts ni la barra", () => h.IsShown("sim-panel") && !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-split"))
                                                                                    && !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-watts"))
                                                                                    && !Overlap(h.BoundsOf("sim-panel"), h.BoundsOf("hud-topbar"))),
                ("ni los paneles", () => !Overlap(h.BoundsOf("sim-panel"), Visual(Col(h).Laps)))));

            yield return s.Tap("sim-close");
            yield return s.AdvanceTo(250.2);
            yield return s.Shot("DEMO-09", h => Expect(("los metros siguen desde donde estaban (sin salto ni vuelta nueva)", () => Meters(h) >= _demoMeters
                && Meters(h) - _demoMeters < 60)));

            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Shot("DEMO-10", h => Expect(("Workout en reposo, sin diálogo ni aviso", () => h.Immersive == null && h.TopDialog == null && !h.Shell.Toasts.IsVisible)));

            yield return s.StartRoutine("Series 600");
            yield return s.AdvanceTo(31.6);
            yield return s.Shot("DEMO-11", h => Expect(("los dos abiertos desde que se ven", () => Col(h).Expanded.Laps && Col(h).Expanded.Reps && h.IsShown("hud-reps-panel")),
                ("Series anteriores vacío", () => Lines(h, "hud-reps-empty") == "Sin repeticiones / completas"),
                ("0:01.0 y Rep 1/3", () => h.TextOf("hud-laps-current") == "0:01.0" && (h.TextOf("hud-rep-progress") ?? "").StartsWith("Rep 1/3 · ", StringComparison.Ordinal))));

            yield return s.Do(h => h.TapText("Parciales 500 m"));
            yield return s.Settle();
            yield return s.Do(h => h.TapText("Series anteriores"));
            yield return s.Settle();
            yield return s.Tap("hud-finish");
            yield return s.Tap("dialog-confirm");
            yield return s.Shot("DEMO-12", h => Expect(("reposo", () => h.Immersive == null),
                ("las dos preferencias en falso", () => h.Preferences.TryGetBool(SeriesPanelPrefs.LapsKey, out var laps) && !laps
                                                        && h.Preferences.TryGetBool(SeriesPanelPrefs.RepsKey, out var reps) && !reps)));
        }

        private static int Meters(UiTestHost h)
        {
            var text = h.TextOf("hud-laps-current-meters") ?? "";
            var digits = text.Replace(" m", "").Replace(".", "");
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : -1;
        }

        // ── Los videos (VID-01 a -09; §5.4; 30 cuadros por segundo; valores por cuadro en el manifest) ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            var vid01 = Five("VID-01", "VID-01-abrir-y-cerrar", DeviceProfile.TV, "Series 600 a los 250 s, repeticiones abierto; 500 m a los 250,3 y a los 251,8 (3 s)",
                s => Routine(s, "Series 600", 250.0), reps: true);
            Video(vid01, 3).At(0.3, h => h.TapText("500 m")).At(1.8, h => h.TapText("Parciales 500 m"));
            vid01.Video!.FrameValues = (h, t) => Values(h);
            vid01.Video.FrameCheck = (h, t) => Concat(OpenCloseCheck(h, t, 0.3, 1.8, 3.0, 34, 90, false), InkCheck(h));
            all.Add(vid01);

            var vid02 = Five("VID-02", "VID-02-vuelta-nueva", DeviceProfile.TV, "Series 600 de 323,5 a 327,5 s, los dos (la vuelta a los 325)",
                s => Routine(s, "Series 600", 323.5), laps: true, reps: true);
            Video(vid02, 4);
            vid02.Video!.FrameValues = (h, t) => Values(h);
            vid02.Video.FrameCheck = (h, t) => Join(InkCheck(h));
            all.Add(vid02);

            var vid03 = Five("VID-03", "VID-03-cambio-a-descanso", DeviceProfile.TV, "Series 600 de 178,5 a 184,5 s, los dos", s => Routine(s, "Series 600", 178.5),
                laps: true, reps: true);
            Video(vid03, 6);
            vid03.Video!.FrameValues = (h, t) => Values(h);
            vid03.Video.FrameCheck = (h, t) => Join(InkCheck(h));
            all.Add(vid03);

            var vid04 = Five("VID-04", "VID-04-cambio-a-trabajo", DeviceProfile.TV, "Series 600 de 198,5 a 203,5 s, los dos", s => Routine(s, "Series 600", 198.5),
                laps: true, reps: true);
            Video(vid04, 5);
            vid04.Video!.FrameValues = (h, t) => Values(h);
            vid04.Video.FrameCheck = (h, t) => Concat(InkCheck(h), DiscCheck(h));
            all.Add(vid04);

            var vid05 = Five("VID-05", "VID-05-desconexion", DeviceProfile.W1280, "Simulador sin ruido de 238 a 248 s, parciales; Simular desconexión a los 240",
                s => Routine(s, "Series 600", 238.0), laps: true, simulator: true);
            Video(vid05, 10).At(1.2, h => h.Tap("sim-open")).At(2.0, h =>
            {
                h.ScrollTo("sim-drop-rower");
                h.Tap("sim-drop-rower");
            }).At(2.6, h => h.Tap("sim-close"));
            vid05.Video!.FrameValues = (h, t) => Values(h);
            all.Add(vid05);

            // QA-V5-03 y -04 (verificación de la 5): 2,5 s como pide la spec (§7.5), y L(0) justo antes del toque como en las demás
            // tomas de it5 (B5-09; ReadingZeroAtStart): la referencia del motor es la de L(0) y al segundo 1 la vuelta en curso
            // muestra 0:01.0 4 m (antes, 0 m: la última lectura del reposo quedaba de referencia y L(1) contaba como reinicio).
            var vid06 = Five("VID-06", "VID-06-entrada-con-paneles", DeviceProfile.TV, "Cinco reps → Iniciar con los dos en verdadero (2,5 s)",
                s => s.TapRoutine("Cinco reps"), laps: true, reps: true);
            Video(vid06, 2.5).At(0.1, h =>
            {
                if (h.ScriptedRower != null)
                {
                    h.ScriptedRower.Next = 1;
                    h.ScriptedRower.EmitIndex(0);
                }

                Start(h, "dialog-confirm");
            });
            vid06.Video!.FrameValues = (h, t) => Values(h);
            all.Add(vid06);

            var vid07 = Five("VID-07", "VID-07-salida-con-paneles", DeviceProfile.TV, "252,2 s, los dos; Terminar → Terminar antes del 253 (2,5 s)",
                s => Routine(s, "Series 600", 252.2), laps: true, reps: true);
            ImmersiveWorkoutPage? leaving = null;
            Video(vid07, 2.5).At(0.05, h =>
            {
                leaving = h.Immersive;
                h.Tap("hud-finish");
            }).At(0.45, h => h.Tap("dialog-confirm"));
            vid07.Video!.FrameValues = (h, t) => LeavingValues(leaving);
            vid07.Video.FrameCheck = (h, t) => Join(LastPhoto(leaving));
            all.Add(vid07);

            var vid08 = Five("VID-08", "VID-08-reducir-movimiento", DeviceProfile.TV, "VID-02 con reducir movimiento; parciales cerrado y repeticiones abierto; 500 m a los 323,8",
                s => Routine(s, "Series 600", 323.5), reps: true);
            vid08.Host.ReduceMotion = true;
            Video(vid08, 4).At(0.3, h => h.TapText("500 m"));
            vid08.Video!.FrameValues = (h, t) => Values(h);
            vid08.Video.FrameCheck = (h, t) => Concat(OpenCloseCheck(h, t, 0.3, double.PositiveInfinity, 1.45, 34, 90, true), InkCheck(h));
            all.Add(vid08);

            var vid09 = Five("VID-09", "VID-09-cuenta-grande-y-panel", DeviceProfile.TV, "196,5 a 199,5 s, cerrados; 500 m a los 197,3", s => Routine(s, "Series 600", 196.5));
            Video(vid09, 3).At(0.8, h => h.TapText("500 m"));
            vid09.Video!.FrameValues = (h, t) => Values(h) + " count=" + h.TextOf("hud-big-countdown") + " shown=" + h.IsShown("hud-big-countdown");
            vid09.Video.FrameCheck = (h, t) => Join(DiscCheck(h));
            all.Add(vid09);
        }

        private static VideoSpec Video(SnapshotScenario scenario, double seconds)
        {
            scenario.Kind = SnapshotKind.Video;
            scenario.Video = new VideoSpec(seconds);
            return scenario.Video;
        }

        private static void Start(UiTestHost h, string element)
        {
            var shift = UiHostOptions.WorkoutStartUtc - h.Scheduler.UtcNow;
            h.Scheduler.ShiftUtc(shift);
            h.Tap(element);
        }

        // Los valores por cuadro de los IMotion de la 5 y de lo que se ve: el alto, la cola, los fundidos, la flecha, el empuje, la
        // marca, el FLIP, la columna y los textos de la vuelta en curso y de la línea.
        private static string? Values(UiTestHost h)
        {
            var column = h.SeriesColumn;
            if (column == null)
            {
                return null;
            }

            var laps = column.Laps.Visual;
            var reps = column.Reps.Visual;
            var col = column.Motion.Visual;
            return "G=" + F(laps.HeightBase) + " tail=" + F(laps.TailShiftBase) + " content=" + F(laps.ContentAlpha) + " label=" + F(laps.ChipLabelAlpha)
                   + " title=" + F(laps.TitleAlpha) + " chevron=" + F(laps.ChevronDegrees) + " push=" + F(laps.RowsPushBase) + " new=" + F(laps.NewRowsAlpha)
                   + "@" + F(laps.NewRowsShiftBase) + " marker=" + F(laps.MarkerAlpha0) + " old=" + F(laps.BodyOldAlpha) + " body=" + F(laps.BodyNewAlpha)
                   + "@" + F(laps.BodyNewRiseBase) + " line=" + F(laps.RepLineAlpha) + " repsG=" + F(reps.HeightBase) + " repsMarker=" + F(reps.MarkerAlpha0)
                   + " flip=" + F(column.Reps.resolvedStyle.translate.y) + " repsAlpha=" + F(col.RepsAlpha) + " follow=" + F(col.FollowBase)
                   + " hero=" + h.TextOf("hud-laps-current") + " " + h.TextOf("hud-laps-current-meters") + " rep=" + h.TextOf("hud-rep-progress")
                   + " band=" + h.TextOf("hud-time-value");
        }

        private static string? LeavingValues(ImmersiveWorkoutPage? page)
        {
            if (page == null || page.Root.panel == null || !page.HasView || page.View.Series == null)
            {
                return null;
            }

            var column = page.View.Series;
            return "hud=" + F(page.IsExiting ? page.Exit.Current.Hud : 1) + " laps=" + F(column.Laps.resolvedStyle.opacity) + " hero=" + Text(column.Laps, "hud-laps-current")
                   + " rep=" + Text(column.Laps, "hud-rep-progress") + " reps=" + Text(column.Reps, "hud-reps-row-0");
        }

        // VID-07: la salida con la última foto: nunca vacíos ni en cero.
        private static IEnumerable<string> LastPhoto(ImmersiveWorkoutPage? page)
        {
            if (page == null || page.Root.panel == null || !page.HasView || page.View.Series == null)
            {
                return Array.Empty<string>();
            }

            var column = page.View.Series;
            return Expect(("0:52.0 en la vuelta en curso", () => Text(column.Laps, "hud-laps-current") == "0:52.0"),
                ("Rep 2/3 · 208 m", () => Text(column.Laps, "hud-rep-progress") == "Rep 2/3 · 208 m"),
                ("Rep 1 2:05 680 m", () => Text(column.Reps, "hud-reps-row-0") == "Rep 1 2:05 680 m"));
        }

        private static string Text(SeriesPanelView panel, string slot)
        {
            return panel.Slot(slot)?.Text ?? "";
        }

        // Abrir (y cerrar) con la línea de tiempo pura en el mismo instante (± 0,15 base) y el FLIP de repeticiones = (G − H)·sp.
        private static IEnumerable<string> OpenCloseCheck(UiTestHost h, double t, double openAt, double closeAt, double endAt, double closed, double open, bool reduced)
        {
            var column = h.SeriesColumn;
            if (column == null || t <= openAt + 1e-6 || t >= endAt)
            {
                return Array.Empty<string>();
            }

            var opening = t < closeAt;
            var ms = (long)Math.Round(((opening ? t - openAt : t - closeAt)) * 1000);
            var expected = opening ? SeriesTimelines.Open(ms, closed, open, 0, reduced) : SeriesTimelines.Close(ms, open, closed, 0, reduced);
            var g = column.Laps.VisualHeightBase;
            var problems = new List<string>();
            if (Math.Abs(g - expected.HeightBase) > 0.15)
            {
                problems.Add("el alto en " + F(g) + " (la línea de tiempo dice " + F(expected.HeightBase) + " a los " + ms + " ms)");
            }

            if (column.RepsShown)
            {
                var flip = (g - column.Laps.FinalHeightBase) * column.Laps.Scale;
                if (Math.Abs(column.Reps.resolvedStyle.translate.y - flip) > 0.05)
                {
                    problems.Add("el FLIP en " + F(column.Reps.resolvedStyle.translate.y) + " (G − H dice " + F(flip) + ")");
                }
            }

            return problems;
        }

        // Ningún renglón con alfa > 0 tiene tinta fuera del vidrio (DD5-13).
        private static IEnumerable<string> InkCheck(UiTestHost h)
        {
            var column = h.SeriesColumn;
            if (column == null)
            {
                return Array.Empty<string>();
            }

            var problems = new List<string>();
            foreach (var panel in new[] { column.Laps, column.Reps })
            {
                if (!UiQuery.IsShown(panel))
                {
                    continue;
                }

                var glass = Visual(panel);
                for (var i = 0; i < panel.SlotCount; i++)
                {
                    var slot = panel.SlotAt(i);
                    if (slot.Alpha > 0.01f && slot.WorldRect.yMax > glass.yMax + 0.05f)
                    {
                        problems.Add(slot.Name + " con α " + F(slot.Alpha) + " fuera del vidrio");
                    }
                }
            }

            return problems;
        }

        // La cuenta grande y PAUSA nunca se enciman con un panel (VID-04, VID-09).
        private static IEnumerable<string> DiscCheck(UiTestHost h)
        {
            var column = h.SeriesColumn;
            if (column == null || !h.IsShown("hud-big-countdown"))
            {
                return Array.Empty<string>();
            }

            var disc = h.Immersive!.View.Callouts.Disc;
            var rect = new Rect((float)disc.Left, (float)disc.Top, (float)disc.Width, (float)disc.Height);
            return Overlap(rect, Visual(column.Laps)) || column.RepsShown && Overlap(rect, Visual(column.Reps))
                ? new[] { "la cuenta grande se encima con un panel" }
                : Array.Empty<string>();
        }

        // ── Las sondas de aspecto (04-design-notes §9.6, con los valores que midió WP-D2 en §15.3 de las notas) ──
        // Los colores esperados salen de las fichas (sin literales, ADR 016); las distancias, de la anatomía (base del panel) o en dp de
        // ventana sobre la caja del elemento (Point: Mathf.RoundToInt((x + ancho·rx)·densidad)). Las tomas de TV usan los anchos del
        // manifest (el panel 144,28 dp; SPM 163,57), las de W1280, 164,4 y 153,6 (kcal).
        private static void AddAspectProbes(List<SnapshotScenario> all)
        {
            var byId = new Dictionary<string, SnapshotScenario>(StringComparer.Ordinal);
            foreach (var scenario in all)
            {
                byId[scenario.Id] = scenario;
            }

            var glassSky = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropSky);
            var glassWater = PixelProbe.Composite(Tokens.GlassFallback, Tokens.BackdropWater);
            var sky = Tokens.BackdropSky.Rgb.ToHex();
            var water = Tokens.BackdropWater.Rgb.ToHex();
            // El borde de 1·sp blanco al 10 % por dentro: el mismo que da la tarjeta en la misma toma (DN5-02, DD5-21).
            var borderSky = Over(Tokens.TextPrimary, SeriesPanelGeometry.BorderAlpha, glassSky);
            var borderWater = Over(Tokens.TextPrimary, SeriesPanelGeometry.BorderAlpha, glassWater);
            const float tvPanel = 144.28f, tvSpm = 163.57f, deskPanel = 164.4f, deskKcal = 153.6f;

            // S5-01 y S5-03: el vidrio de parciales sobre el cielo en CAP-03 (90 base de alto), (5; 30) y (60; 3) contra (60; 30): plano
            // desde el borde, sin reflejo. S5-04: el borde de parciales y el de SPM, a 0,4 dp del lado izquierdo, a la mitad del alto.
            byId["CAP-03"].WithProbe(PixelProbe.Point("S5-01 vidrio sobre el cielo", "hud-laps-panel", new Vector2(5f / 137, 30f / 90), glassSky, 2), true)
                .WithProbe(PixelProbe.Point("S5-03 sin reflejo (60; 3)", "hud-laps-panel", new Vector2(60f / 137, 3f / 90), glassSky, 2), true)
                .WithProbe(PixelProbe.Point("S5-03 sin reflejo (60; 30)", "hud-laps-panel", new Vector2(60f / 137, 30f / 90), glassSky, 2), true)
                .WithProbe(PixelProbe.Point("S5-04 borde de parciales (cielo)", "hud-laps-panel", new Vector2(0.4f / tvPanel, 0.5f), borderSky, 3), true)
                .WithProbe(PixelProbe.Point("S5-04 borde de SPM (la tarjeta, cielo)", "hud-spm", new Vector2(0.4f / tvSpm, 0.5f), borderSky, 3), true);
            // S5-02: repeticiones de CAP-04 (82 base), (5; 60): debajo del horizonte. S5-06 en repeticiones: los metros (el cuarto derecho
            // de la fila) en metric.distance.hud.
            byId["CAP-04"].WithProbe(PixelProbe.Point("S5-02 vidrio sobre el agua", "hud-reps-panel", new Vector2(5f / 137, 60f / 82), glassWater, 2), true)
                .WithProbe(PixelProbe.DominantIn("S5-06 metros de la repetición", "hud-reps-panel", e => SlotPart(e, "hud-reps-row-0", 0.75f, 1f),
                    Tokens.MetricDistanceHud.Rgb.ToHex(), 3), true);
            // S5-05, S5-06: la cifra y los metros de la vuelta en curso (CAP-02). S5-07: el rótulo #1 (el 16 % izquierdo de la fila 0) en
            // text.secondary.
            byId["CAP-02"].WithProbe(PixelProbe.Dominant("S5-05 vuelta en curso", "hud-laps-current", Tokens.MetricSplitHud.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("S5-06 metros de la vuelta", "hud-laps-current-meters", Over(Tokens.MetricSplitHud, SeriesPanelGeometry.HeroMetersAlpha, glassSky), 4), true)
                .WithProbe(PixelProbe.DominantIn("S5-07 rótulo #n", "hud-laps-panel", e => SlotPart(e, "hud-laps-row-0", 0f, 0.16f),
                    Over(Tokens.TextSecondary, 1, glassSky), 4), true);
            // S5-08: el vacío (CAP-05), sobre el cielo (parciales) y sobre el agua (repeticiones).
            byId["CAP-05"].WithProbe(PixelProbe.Dominant("S5-08 vacío", "hud-laps-empty", Over(Tokens.TextTertiary, 1, glassSky), 4), true)
                .WithProbe(PixelProbe.Dominant("S5-08 vacío de repeticiones (agua)", "hud-reps-empty", Over(Tokens.TextTertiary, 1, glassWater), 4), true);
            // S5-01 y S5-02 en los otros perfiles: P360 (cielo), TH (agua) y W1280 (agua), en (5; 30).
            byId["CAP-14"].WithProbe(PixelProbe.Point("S5-01 vidrio sobre el cielo (P360)", "hud-laps-panel", new Vector2(5f / 137, 30f / 90), glassSky, 2), true);
            // S5-12 (WP-D2): la máscara de la columna (DD5-15) en TH, a 6 dp del piso (310,15 = 411,43 − 96·s): el vidrio al 6 / 12·s =
            // 0,474 de su alfa; ± 5 por medio dp de redondeo (el degradé vale ≈ 8 por dp en el azul).
            byId["CAP-16"].WithProbe(PixelProbe.Point("S5-02 vidrio sobre el agua (TH)", "hud-laps-panel", new Vector2(5f / 137, 30f / 90), glassWater, 2), true)
                .WithProbe(PixelProbe.Point("S5-12 la máscara a 6 dp del piso", "hud-laps-panel", new Vector2(0.5f, 0.9817f), Over(Tokens.GlassFallback, 0.474, water), 5), true);
            // S5-12: con la máscara, el vidrio de parciales en 0 pasando el piso (CAP-21, 1,5 dp debajo): solo el agua.
            byId["CAP-21"].WithProbe(PixelProbe.Point("S5-12 sin vidrio pasando el piso", "hud-laps-panel", new Vector2(0.5f, 0.9005f), water, 2), true);
            // S5-04 sobre el agua (W1280, borde de 1·sp = 1,2 px): parciales contra kcal; S5-02 en W1280.
            byId["CAP-18"].WithProbe(PixelProbe.Point("S5-02 vidrio sobre el agua (W1280)", "hud-laps-panel", new Vector2(5f / 137, 30f / 114), glassWater, 2), true)
                .WithProbe(PixelProbe.Point("S5-04 borde de parciales (agua)", "hud-laps-panel", new Vector2(0.4f / deskPanel, 0.5f), borderWater, 3), true)
                .WithProbe(PixelProbe.Point("S5-04 borde de kcal (la tarjeta, agua)", "hud-kcal", new Vector2(0.4f / deskKcal, 0.5f), borderWater, 3), true);
            // S5-10: el aire al disco de la cuenta grande (8,4 dp = 8·s): cielo a 0,5 y a 7,9 dp del borde del panel, el aro (step.rest,
            // el descanso) a 9,2 dp. CAP-19: el chip Reps, a la altura del centro del disco; CAP-20: parciales abierto.
            foreach (var (id, panel) in new[] { ("CAP-19", "hud-reps-panel"), ("CAP-20", "hud-laps-panel") })
            {
                byId[id].WithProbe(PixelProbe.Point("S5-10 aire al disco: cielo a 0,5 dp (" + panel + ")", panel, new Vector2((tvPanel + 0.5f) / tvPanel, 0.5f), sky, 2), true)
                    .WithProbe(PixelProbe.Point("S5-10 aire al disco: cielo a 7,9 dp (" + panel + ")", panel, new Vector2((tvPanel + 7.9f) / tvPanel, 0.5f), sky, 2), true)
                    .WithProbe(PixelProbe.Point("S5-10 el aro a 9,2 dp (" + panel + ")", panel, new Vector2((tvPanel + 9.2f) / tvPanel, 0.5f), Tokens.StepRest.Rgb.ToHex(), 3), true);
            }

            // S5-11: el anillo, a 3 dp por fuera del borde, a la mitad del lado izquierdo, y su halo (overlay.scrim al 50 %) a 6 dp (CAP-26).
            byId["CAP-26"].WithProbe(PixelProbe.Point("S5-11 el anillo", "hud-laps-panel", new Vector2(-3f / deskPanel, 0.5f), Tokens.TextPrimary.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("S5-11 el halo", "hud-laps-panel", new Vector2(-6f / deskPanel, 0.5f), Over(Tokens.OverlayScrim, SeriesPanelGeometry.FocusHaloAlpha, water), 2), true);
        }

        // Una parte horizontal de una ranura del panel (en dp de ventana), para las sondas que miran una pieza de una fila (el rótulo, los
        // metros). Sin la ranura, un rectángulo vacío: la sonda falla con "sin tamaño".
        private static Rect SlotPart(VisualElement element, string slot, float from, float to)
        {
            if (!(element is SeriesPanelView panel) || !(panel.Slot(slot) is DrawnSlot found))
            {
                return Rect.zero;
            }

            var r = found.WorldRect;
            return new Rect(r.x + r.width * from, r.y, r.width * (to - from), r.height);
        }

        // Un color de ficha (con su alfa, por otro alfa) sobre un fondo ya compuesto, en sRGB como la tubería GammaRt (sin literales).
        private static string Over(ColorToken token, double alpha, string backgroundHex)
        {
            var a = token.Alpha * alpha;
            var fg = token.Rgb;
            var bg = Rgb.Parse(backgroundHex);
            return new Rgb(Mix(fg.R, bg.R, a), Mix(fg.G, bg.G, a), Mix(fg.B, bg.B, a)).ToHex();
        }

        private static byte Mix(byte fg, byte bg, double alpha)
        {
            return (byte)Math.Max(0, Math.Min(255, Math.Round(fg * alpha + bg * (1 - alpha))));
        }

        // ── Armado ──

        private static SnapshotScenario Five(string id, string stem, DeviceProfile profile, string state, Func<ScenarioScript, IEnumerator>? arrange, bool laps = false,
                                             bool reps = false, string language = "es-AR", Func<int, RowingData>? reading = null, bool simulator = false,
                                             bool outline = false)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = simulator ? HostBackends.Simulator(false) : HostBackends.TestRower(reading ?? RowerScripts.R4),
                SimulatorLayer = simulator ? SimulatorLayerMode.Full : SimulatorLayerMode.ChromeOnly,
                LanguageTags = new[] { language },
                Routines = HostRoutines.Seeded(RoutineSeeds.F5),
                StartUtc = UiHostOptions.WorkoutStartUtc,
                BoatZoneOutline = outline,
                Preferences = Prefs(laps, reps),
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = simulator ? "Sim" : "R-4", ExactInstant = true };
        }

        // Una MemoryPreferences por toma (§5.1 punto 1), sembrada con lo que pide.
        private static MemoryPreferences Prefs(bool laps, bool reps)
        {
            var prefs = new MemoryPreferences();
            if (laps)
            {
                prefs.SetBool(SeriesPanelPrefs.LapsKey, true);
            }

            if (reps)
            {
                prefs.SetBool(SeriesPanelPrefs.RepsKey, true);
            }

            return prefs;
        }

        private static IEnumerator Routine(ScenarioScript s, string name, double at)
        {
            s.ReadingZeroAtStart = true;
            yield return s.Tap("tab-workout");
            yield return s.StartRoutine(name);
            yield return s.AdvanceTo(at);
        }

        private static SeriesColumn Col(UiTestHost h)
        {
            return h.SeriesColumn ?? throw new InvalidOperationException("sin paneles de series");
        }

        private static HudView Hud(UiTestHost h)
        {
            return h.Immersive!.View;
        }

        private static HudLayout Layout(UiTestHost h)
        {
            return h.Immersive!.Layout ?? throw new InvalidOperationException("sin disposición");
        }

        private static DrawnSlot Slot(UiTestHost h, string name)
        {
            return h.SlotOf(name)?.Slot ?? throw new InvalidOperationException("sin la ranura " + name);
        }

        private static string Lines(UiTestHost h, string name)
        {
            var slot = Slot(h, name);
            return slot.Line0 + " / " + slot.Line1;
        }

        private static bool SpanY(SeriesPanelView panel, double y0, double y1)
        {
            var r = Visual(panel);
            return Near(r.yMin, y0, DesignTolerance) && Near(r.yMax, y1, DesignTolerance);
        }

        private static bool Span(UiTestHost h, SeriesPanelView panel, double x0, double x1, double y0, double y1)
        {
            var r = Visual(panel);
            return Near(r.xMin, x0, DesignTolerance) && Near(r.xMax, x1, DesignTolerance) && SpanY(panel, y0, y1);
        }

        // Los rectángulos contra la geometría con la ventana real (± 0,5 dp; DD5-25): el borde de arriba de cada panel es su TopDp
        // (con el translate de la columna en 0) y su alto, el final × sp.
        private static bool Geometry(UiTestHost h)
        {
            var column = Col(h);
            foreach (var (panel, placement) in new[] { (column.Laps, column.LapsPlacement), (column.Reps, column.RepsPlacement) })
            {
                if (!UiQuery.IsShown(panel))
                {
                    continue;
                }

                var r = Visual(panel);
                if (!Near(r.yMin, placement.TopDp - (column.Column.Rule == SeriesFitRule.ColumnScroll ? column.ScrollBase * column.Column.Scale : 0), GeometryTolerance)
                    || !Near(r.height, placement.HeightBase * column.Column.Scale, GeometryTolerance) || !Near(r.xMin, column.Column.Left, GeometryTolerance))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllOpaque(SeriesPanelView panel)
        {
            for (var i = 0; i < panel.SlotCount; i++)
            {
                var slot = panel.SlotAt(i);
                if (slot.Name.EndsWith("-chip", StringComparison.Ordinal) && panel.Motion.Expanded)
                {
                    continue;
                }

                if (slot.Alpha < 0.999f)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool InsideGlass(SeriesPanelView panel)
        {
            var glass = Visual(panel);
            for (var i = 0; i < panel.SlotCount; i++)
            {
                var slot = panel.SlotAt(i);
                if (!slot.IsVisible)
                {
                    continue;
                }

                var r = slot.WorldRect;
                if (r.xMin < glass.xMin - 0.01f || r.xMax > glass.xMax + 0.01f || r.yMin < glass.yMin - 0.01f || r.yMax > glass.yMax + 0.01f)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Contains(UiTestHost h, string text)
        {
            foreach (var (_, slot) in UiQuery.Slots(h.Root))
            {
                if (slot.Text.Contains(text))
                {
                    return true;
                }
            }

            return false;
        }
#endif

        // El rectángulo visual de un panel (dp de ventana): su caja con el translate y el alto G.
        public static Rect Visual(SeriesPanelView panel)
        {
            var box = panel.worldBound;
            return new Rect(box.x, box.y, box.width, (float)(panel.VisualHeightBase * panel.Scale));
        }

        private static string? Join(IEnumerable<string> problems)
        {
            var all = new List<string>(problems);
            return all.Count == 0 ? null : string.Join("; ", all);
        }

        private static bool Near(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax - 0.01f && b.xMin < a.xMax - 0.01f && a.yMin < b.yMax - 0.01f && b.yMin < a.yMax - 0.01f;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string R(Rect r)
        {
            return "x " + F(r.xMin) + "-" + F(r.xMax) + " · y " + F(r.yMin) + "-" + F(r.yMax);
        }

        private static IEnumerable<string> Expect(params (string What, Func<bool> Ok)[] checks)
        {
            var problems = new List<string>();
            foreach (var (what, ok) in checks)
            {
                bool passed;
                try
                {
                    passed = ok();
                }
                catch (Exception error)
                {
                    problems.Add("no se cumple: " + what + " (" + error.GetType().Name + ": " + error.Message + ")");
                    continue;
                }

                if (!passed)
                {
                    problems.Add("no se cumple: " + what);
                }
            }

            return problems;
        }

        private static string? Concat(IEnumerable<string> a, IEnumerable<string> b)
        {
            var all = new List<string>(a);
            all.AddRange(b);
            return all.Count == 0 ? null : string.Join("; ", all);
        }
    }
}
