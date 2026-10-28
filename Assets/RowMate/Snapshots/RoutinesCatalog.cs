using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RowMate.Core.Design;
using RowMate.Core.Routines;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Snapshots
{
    // Catálogo it3 (03-rutinas/01-po-spec §7.3-§7.5 → 02-tech-design §5.4; aspecto: 04-design-notes §7.2, DN3-23): 42 IDs =
    // 47 archivos (CAP-01…28 sin CAP-27, con a/b en CAP-12, -15, -20, -24 y -25; DEMO-01…12 en una sola secuencia; VID-01…03).
    // Salvo que diga otra cosa: modo simulador (sin ruido, capa completa), español, reloj de prueba, fechas de creación del
    // SequenceClock, FakeClipboard, teléfono (IsDesktop = false) en TV y L640 y escritorio en W1280, y la base de prueba de cada
    // toma (TempDatabase: nunca las del usuario, AC-HIS-20-04). Las "comprobaciones" hacen fallar la toma; las sondas de aspecto
    // (aspect: true) se anotan para WP-D sin hacerla fallar.
    // CAP-27 (AC-APP-05-10, AC-HIS-20-03 en el build normal) NO es del arnés: tools/unity/capture-cap27.ps1, a pedido (§5.5).
    // IDs que estas tomas muestran (además de sus pruebas): AC-RUT-08-09 (DEMO-10), AC-RUT-10-01 … -11, AC-RUT-11-01, -02, -04,
    // -05, -09 (CAP-26), AC-RUT-12-02, -06, AC-RUT-13-01, -02, -10 … -12, -15, -16, AC-RUT-14-01 … -04, -06, -11, AC-RUT-01-04,
    // AC-RUT-04-06, AC-RUT-09-04, -05, AC-HIS-20-03 (cinta y llave sobre las mismas pantallas), AC-APP-02-08, AC-APP-03-01,
    // -03, -04, -05, -07, -08 (VID-02), AC-APP-05-05 (CAP-25), AC-APP-05-11, AC-BLE-16-01, AC-BLE-16-02.
    public static class RoutinesCatalog
    {
        public static IReadOnlyList<SnapshotScenario> All
        {
            get
            {
                var all = new List<SnapshotScenario>();
#if ROWMATE_SIMULATOR
                AddStills(all);
                all.Add(Demo());
                AddVideos(all);
#endif
                all.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return all;
            }
        }

        // Archivos que salen (las secuencias cuentan una por toma).
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

#if ROWMATE_SIMULATOR
        private static void AddStills(List<SnapshotScenario> all)
        {
            var bgApp = Tokens.BgApp.Rgb.ToHex();

            // CAP-01: vacío; el + entero y la llave 16 dp encima, sin tocarlo (AC-RUT-10-01, -02, -04; AC-BLE-16-02; AC-APP-02-08).
            var cap01 = Rut("CAP-01", "CAP-01-rutinas-vacia", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty),
                "Rutinas con F-VACIA (cinta y llave del simulador)", s => s.Tap("tab-routines"));
            cap01.Checks = h => Expect(
                ("#routines-empty visible", () => h.IsShown("routines-empty")),
                ("No hay rutinas todavía", () => h.IsTextVisible("No hay rutinas todavía")),
                ("Presioná + para crear una", () => h.IsTextVisible("Presioná + para crear una")),
                ("#routines-add entero en la ventana", () => Inside(h, "routines-add")),
                ("#routines-add sin intersección con #sim-open", () => !Overlap(h.BoundsOf("routines-add"), h.BoundsOf("sim-open"))),
                ("#sim-open.yMax + 16 = #routines-add.yMin ± 1", () => Near(h.BoundsOf("sim-open").yMax + 16, h.BoundsOf("routines-add").yMin, 1)),
                ("mismo xMax ± 1", () => Near(h.BoundsOf("sim-open").xMax, h.BoundsOf("routines-add").xMax, 1)),
                ("#sim-ribbon", () => h.IsShown("sim-ribbon")),
                ("pestaña 2 elegida", () => h.App.Shell.SelectedIndex.Value == 2),
                ("barra sin acciones", () => h.RoutinesScreen!.AppBar.Actions.childCount == 0));
            cap01.WithProbe(PixelProbe.Point("bg.app", "routines-body", new Vector2(0.05f, 0.1f), bgApp), true)
                .WithProbe(PixelProbe.Point("+ accent.primary", "routines-add", new Vector2(0.25f, 0.5f), Tokens.AccentPrimary.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("llave surface.overlay", "sim-open", new Vector2(0.5f, 0.15f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("ícono del vacío al 12 %", "routines-empty .rm-empty__icon", Over(Tokens.TextPrimary, 0.12, Tokens.BgApp), 3), true)
                .WithProbe(PixelProbe.Dominant("título text.secondary", "routines-empty .rm-empty__title", PixelProbe.Composite(Tokens.TextSecondary, Tokens.BgApp), 3), true)
                .WithProbe(PixelProbe.Dominant("ayuda text.tertiary", "routines-empty .rm-empty__hint", PixelProbe.Composite(Tokens.TextTertiary, Tokens.BgApp), 3), true);
            all.Add(cap01);

            // CAP-02: cargando (gracia 120 + fundido 160): solo el indicador de 32 (AC-RUT-10-02).
            var cap02 = Rut("CAP-02", "CAP-02-cargando", DeviceProfile.TV, HostRoutines.Scripted(r => r.LoadDelay = ScriptedRoutineRepository.Never),
                "Rutinas con la base lenta, después de la gracia y el fundido", s => Steps(s.Tap("tab-routines"), s.Advance(0.3)));
            cap02.Checks = h => Expect(
                ("solo #routines-loading", () => h.IsShown("routines-loading") && !h.IsShown("routines-empty") && !h.IsShown("routines-error")),
                ("indicador entero", () => h.Find("routines-loading")!.resolvedStyle.opacity > 0.99f),
                ("indicador de 32", () => h.RoutinesScreen!.Spinner.SizeInDp == 32),
                ("ningún texto en #routines-body", () => UiQuery.VisibleTexts(h.Find("routines-body")!).Count == 0),
                ("+ visible", () => h.IsShown("routines-add")));
            cap02.WithProbe(PixelProbe.Dominant("indicador accent.primary", "routines-loading", Tokens.AccentPrimary.Rgb.ToHex(), 3), true);
            all.Add(cap02);

            // CAP-03: F-ROTA → error de carga y Reintentar; el archivo no se toca (AC-RUT-09-04, AC-HIS-17-05).
            var cap03 = Rut("CAP-03", "CAP-03-error-de-carga", DeviceProfile.TV, HostRoutines.Rota(), "Rutinas con F-ROTA", s => s.Tap("tab-routines"));
            cap03.Checks = h => Expect(
                ("No se pudieron cargar las rutinas", () => h.IsTextVisible("No se pudieron cargar las rutinas")),
                ("#routines-retry", () => h.IsShown("routines-retry")),
                ("sin indicador", () => !h.IsShown("routines-loading")),
                ("+ visible", () => h.IsShown("routines-add")),
                ("F-ROTA sin cambios", () => File.ReadAllText(h.Routines.FilePath!) == "esto no es una base de SQLite\n"));
            cap03.WithProbe(PixelProbe.Dominant("ícono state.dangerText", "routines-error-icon", Tokens.StateDangerText.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("Reintentar tonal", "routines-retry", new Vector2(0.06f, 0.5f), PixelProbe.Composite(Tokens.AccentTonalBg, Tokens.BgApp), 2), true);
            all.Add(cap03);

            // CAP-04 y CAP-05: la lista (AC-RUT-10-03, -10, -11; DN3-07).
            all.Add(ListShot("CAP-04", "CAP-04-lista", "es-AR", new[] { "1 serie · 1 descanso", "2 series · 1 descanso", "1 paso", "4 series · 3 descansos" }));
            all.Add(ListShot("CAP-05", "CAP-05-lista-en", "en-US", new[] { "1 interval · 1 rest", "2 intervals · 1 rest", "1 step", "4 intervals · 3 rests" }));

            // CAP-06: L640, 30 rutinas, al final: la última entera sobre la llave, que está sobre el + (AC-RUT-10-09, AC-BLE-16-02).
            var cap06 = Rut("CAP-06", "CAP-06-lista-l640-final", DeviceProfile.L640, HostRoutines.Seeded(RoutineSeeds.ListaPlus(26)),
                "F-LISTA + 26 rutinas, desplazada al final", s => Steps(s.Tap("tab-routines"), s.ScrollToEnd("routines-list"), s.Settle()));
            cap06.Checks = h => Expect(
                ("última tarjeta entera", () => Inside(h, "routine-card-29")),
                ("yMax ≤ #sim-open.yMin − 16 ± 1", () => h.BoundsOf("routine-card-29").yMax <= h.BoundsOf("sim-open").yMin - 16 + 1),
                ("llave sin intersección con +", () => !Overlap(h.BoundsOf("sim-open"), h.BoundsOf("routines-add"))));
            all.Add(cap06);

            // CAP-07: menú de la tarjeta (AC-RUT-10-06, AC-APP-03-04).
            var cap07 = Rut("CAP-07", "CAP-07-menu-tarjeta", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "⋮ de Escalera SPM abierto",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-menu-0")));
            cap07.Checks = h => Expect(
                ("Editar y Eliminar", () => h.IsShown("routine-menu-edit") && h.IsShown("routine-menu-delete")),
                ("debajo del ⋮", () => h.Shell.Popups.Menu!.worldBound.yMin >= h.BoundsOf("routine-menu-0").yMax - 1),
                ("xMax del menú = xMax del ⋮ ± 1", () => Near(h.Shell.Popups.Menu!.worldBound.xMax, h.BoundsOf("routine-menu-0").xMax, 1)),
                ("#popup-layer antes que #simulator-layer", () => Index(h, "popup-layer") < Index(h, "simulator-layer")));
            cap07.WithProbe(PixelProbe.Point("menú surface.overlay", "popup-layer .rm-menu", new Vector2(0.5f, 0.05f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Dominant("Eliminar state.dangerText", "routine-menu-delete .rm-menu__label", Tokens.StateDangerText.Rgb.ToHex(), 3), true);
            all.Add(cap07);

            // CAP-08: diálogo de eliminar (AC-RUT-10-07, AC-APP-03-03, AC-APP-03-07).
            var cap08 = Rut("CAP-08", "CAP-08-eliminar", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "⋮ → Eliminar en Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-menu-0"), s.Tap("routine-menu-delete")));
            cap08.Checks = h => Expect(
                ("Eliminar rutina", () => h.TextOf("dialog-title") == "Eliminar rutina"),
                ("¿Eliminar \"Escalera SPM\"?", () => h.TextOf("dialog-message") == "¿Eliminar \"Escalera SPM\"?"),
                ("Eliminar relleno", () => h.Find("dialog-confirm")!.ClassListContains("rm-button--danger-fill")),
                ("diálogo de 331", () => Near(h.TopDialog!.Box.worldBound.width, 331, 1)),
                ("#sim-open tocable con el velo", () => h.WhyNotTappable("sim-open") == null));
            cap08.WithProbe(PixelProbe.Point("diálogo surface.overlay", "dialog-delete-routine", new Vector2(0.5f, 0.06f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("velo sobre fondo", "dialog-scrim", new Vector2(0.5f, 0.02f), Over(Tokens.OverlayScrim, MotionSpecs.DialogScrimOpacity, Tokens.BgApp), 3), true)
                .WithProbe(PixelProbe.Point("velo sobre la tarjeta", "routine-card-0", new Vector2(0.6f, 0.08f),
                    Over(Tokens.OverlayScrim, MotionSpecs.DialogScrimOpacity, Tokens.SurfaceCard), 2), true)
                .WithProbe(PixelProbe.Point("Eliminar state.dangerFill", "dialog-confirm", new Vector2(0.08f, 0.5f), Tokens.StateDangerFill.Rgb.ToHex(), 1), true)
                // La cinta y la llave están encima del velo: sin oscurecer. (27; 27) dp de la ventana, en la franja de la cinta.
                .WithProbe(PixelProbe.Point("cinta sin oscurecer", "sim-ribbon", new Vector2(0.546f, 0.546f), Tokens.DevSimulator.Rgb.ToHex(), 2), true)
                .WithProbe(PixelProbe.Point("llave sin oscurecer", "sim-open", new Vector2(0.5f, 0.15f), Tokens.SurfaceOverlay.Rgb.ToHex(), 1), true);
            all.Add(cap08);

            // CAP-09: editor nuevo (AC-RUT-11-01, -02, -04; AC-APP-03-01; AC-BLE-16-02).
            var cap09 = Rut("CAP-09", "CAP-09-editor-nuevo", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "+ con F-VACIA",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add")));
            cap09.Checks = h => Expect(
                ("Nueva rutina", () => h.IsTextVisible("Nueva rutina")),
                ("#editor-save", () => h.IsShown("editor-save")),
                ("campos vacíos", () => h.Editor!.NameField.Value.Length == 0 && h.Editor.DescriptionField.Value.Length == 0),
                ("Pasos (0)", () => h.TextOf("editor-steps-count") == "Pasos (0)"),
                ("editorEmptySteps con \\n", () => h.TextOf("editor-empty") == "Todavía no hay pasos.\nPresioná \"Paso\" o \"Serie\"."),
                ("#tab-bar oculta", () => !h.IsShown("tab-bar")),
                ("la flecha se toca", () => h.WhyNotTappable("editor-back") == null),
                ("#sim-open a 80 ± 1 del borde inferior", () => Near(h.Root.worldBound.yMax - h.BoundsOf("sim-open").yMax, 80, 1)));
            cap09.WithProbe(PixelProbe.Point("caja del campo bg.sunken", "editor-name .rm-field__box", new Vector2(0.5f, 0.5f), Tokens.BgSunken.Rgb.ToHex(), 3), true)
                // El borde de 1 dp en reposo (border.input sobre bg.sunken): medio dp adentro del borde izquierdo de la caja de 379.
                .WithProbe(PixelProbe.Point("borde del campo border.input", "editor-name .rm-field__box", new Vector2(0.0013f, 0.5f),
                    PixelProbe.Composite(Tokens.BorderInput, Tokens.BgSunken), 3), true)
                .WithProbe(PixelProbe.Dominant("vacío del editor text.tertiary", "editor-empty", PixelProbe.Composite(Tokens.TextTertiary, Tokens.BgApp), 3), true);
            all.Add(cap09);

            // CAP-10 y CAP-11: el editor de Escalera SPM (AC-RUT-12-02, AC-RUT-14-02, -03; AC-RUT-01-04).
            all.Add(EditorShot("CAP-10", "CAP-10-editor-escalera", DeviceProfile.TV));
            all.Add(EditorShot("CAP-11", "CAP-11-editor-w1280", DeviceProfile.W1280));

            // CAP-12a/b: F-BORDE, los 11 subtítulos (AC-RUT-12-06, AC-RUT-04-06).
            all.Add(BorderShot("CAP-12a", "CAP-12a-casos-borde", false));
            all.Add(BorderShot("CAP-12b", "CAP-12b-casos-borde-final", true));

            // CAP-13: dos series contiguas, 16 ± 1 entre el pie y el encabezado (AC-RUT-14-11, QA-G3-04).
            var cap13 = Rut("CAP-13", "CAP-13-series-contiguas", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.PiramideContiguous),
                "Editor de Pirámide 500 con una serie ×2 justo después de la ×4", s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0")));
            cap13.Checks = h => Expect(
                ("dos .rm-series", () => h.Find("series-0") != null && h.Find("series-1") != null),
                ("×4 y ×2", () => Series(h, 0).Count.Text == "×4" && Series(h, 1).Count.Text == "×2"),
                ("16 ± 1 entre pie y encabezado", () => Near(h.BoundsOf("series-header-1").yMin - h.BoundsOf("series-footer-0").yMax, 16, 1)));
            all.Add(cap13);

            // CAP-14: nuevo paso (AC-RUT-13-01, -02, -03, -16; AC-APP-03-03).
            var cap14 = Rut("CAP-14", "CAP-14-nuevo-paso", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "+ Paso",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add"), s.TapInView("editor-add-step")));
            cap14.Checks = h => Expect(
                ("Nuevo paso", () => h.TextOf("dialog-title") == "Nuevo paso"),
                ("Trabajo elegido", () => h.Find("step-type-work")!.ClassListContains(SegmentedControl.SelectedClass)),
                ("Por tiempo elegido", () => h.Find("step-mode-time")!.ClassListContains(SegmentedControl.SelectedClass)),
                ("mosaicos 2 × 2", () => Step(h).Types.Columns == 2),
                ("vista previa fija bajo el título", () => Step(h).PreviewIsPinned),
                ("vista previa Trabajo 0s", () => Step(h).Preview.Title.text == "Trabajo" && Step(h).Preview.Subtitle.Text == "0s"),
                ("sin progresiones", () => h.Find("step-prog-spm") == null));
            cap14.WithProbe(PixelProbe.Point("vista previa surface.card", "step-preview", new Vector2(0.97f, 0.5f), Tokens.SurfaceCard.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("insignia de la vista previa", "step-preview .rm-step-row__badge-bg", new Vector2(0.5f, 0.12f),
                    Over(Tokens.StepWork, 0.2, Tokens.SurfaceCard), 2), true)
                .WithProbe(PixelProbe.Point("mosaico elegido step.work @20 %", "step-type-work", new Vector2(0.06f, 0.5f),
                    Over(Tokens.StepWork, 0.2, Tokens.SurfaceOverlay), 2), true)
                .WithProbe(PixelProbe.Point("segmento elegido brand.seed", "step-mode-time", new Vector2(0.08f, 0.5f), Tokens.BrandSeed.Rgb.ToHex(), 1), true);
            all.Add(cap14);

            // CAP-15a/b: editar el Trabajo de Escalera SPM (AC-RUT-13-02, -10, DV-19).
            var cap15a = Rut("CAP-15a", "CAP-15a-editar-paso", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Lápiz de Trabajo en Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("step-edit-1")));
            cap15a.Checks = h => Expect(
                ("Editar paso", () => h.TextOf("dialog-title") == "Editar paso"),
                ("5 / 0 / 18", () => FieldValue(h, "step-min") == "5" && FieldValue(h, "step-sec") == "0" && FieldValue(h, "step-spm") == "18"),
                ("vista previa con tendencia", () => Step(h).Preview.Subtitle.HasTrend
                                                   && Step(h).Preview.Subtitle.Text == "5m  ·  18 spm · " + StepSubtitle.TrendEmoji + " SPM +2/rep"));
            all.Add(cap15a);
            var cap15b = Rut("CAP-15b", "CAP-15b-editar-paso-progresiones", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista),
                "Lápiz de Trabajo en Escalera SPM, desplazado al final",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("step-edit-1"), s.Do(h => h.ScrollDialogToEnd())));
            cap15b.Checks = h => Expect(
                ("Progresiones (solo aplica en series)", () => h.IsTextVisible("Progresiones (solo aplica en series)")),
                ("Incremento SPM 2", () => FieldValue(h, "step-prog-spm") == "2"),
                ("ayudas Ej: 5 y Ej: -2 (más rápido)", () => h.Find<RmTextField>("step-prog-watts")!.Placeholder == "Ej: 5"
                                                             && h.Find<RmTextField>("step-prog-split")!.Placeholder == "Ej: -2 (más rápido)"),
                ("botones dentro de la ventana", () => Inside(h, "dialog-confirm") && Inside(h, "dialog-cancel")));
            // La ayuda (Ej: 5) con el campo vacío, en text.tertiary: el TextElement interno es solo el renglón del texto.
            cap15b.WithProbe(PixelProbe.Dominant("ayuda text.tertiary", "step-prog-watts .unity-text-element--inner-input-field-component",
                PixelProbe.Composite(Tokens.TextTertiary, Tokens.BgSunken), 3), true);
            all.Add(cap15b);

            // CAP-16: por distancia (AC-RUT-13-04, -07).
            var cap16 = Rut("CAP-16", "CAP-16-por-distancia", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "+ Paso, Por distancia, Metros 500",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add"), s.TapInView("editor-add-step"), s.TapInView("step-mode-distance"),
                    s.Type("step-meters", "500")));
            cap16.Checks = h => Expect(
                ("Metros 500", () => FieldValue(h, "step-meters") == "500"),
                ("sin Min", () => !h.IsShown("step-min")),
                ("vista previa 500m", () => Step(h).Preview.Subtitle.Text == "500m"));
            all.Add(cap16);

            // CAP-17: repeticiones (AC-RUT-14-04).
            var cap17 = Rut("CAP-17", "CAP-17-repeticiones", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Chip ×3 de Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("series-repeat-0")));
            cap17.Checks = h => Expect(
                ("Cantidad 3 con foco", () => FieldValue(h, "repeat-amount") == "3" && h.Find<RmTextField>("repeat-amount")!.IsFocused),
                ("teléfono: cursor al final", () => h.Find<RmTextField>("repeat-amount")!.Selection == (1, 1)),
                ("diálogo de 331", () => Near(h.TopDialog!.Box.worldBound.width, 331, 1)));
            cap17.WithProbe(PixelProbe.Dominant("borde con foco accent.primary", "repeat-amount .rm-field__box", Tokens.AccentPrimary.Rgb.ToHex(), 3), true);
            all.Add(cap17);

            // CAP-18, CAP-19: avisos de validación (AC-RUT-11-05, AC-APP-03-05).
            var cap18 = Rut("CAP-18", "CAP-18-aviso-nombre", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "Editor nuevo con nombre ␣␣␣, Guardar",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add"), s.Type("editor-name", "   "), s.Tap("editor-save")));
            cap18.Checks = h => ToastChecks(h, "El nombre no puede estar vacío", true);
            cap18.WithProbe(PixelProbe.Point("aviso surface.overlay", "toast", new Vector2(0.02f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true);
            all.Add(cap18);
            var cap19 = Rut("CAP-19", "CAP-19-aviso-sin-pasos", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "Editor nuevo con nombre Prueba y sin pasos, Guardar",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add"), s.Type("editor-name", "Prueba"), s.Tap("editor-save")));
            cap19.Checks = h => ToastChecks(h, "Agregá al menos un paso", true);
            cap19.WithProbe(PixelProbe.Point("aviso surface.overlay", "toast", new Vector2(0.02f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true);
            all.Add(cap19);

            // CAP-20a/b: errores de rango (AC-RUT-13-11, -12).
            var cap20a = Rut("CAP-20a", "CAP-20a-error-vatios", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty), "+ Paso con W mín 10000, Confirmar",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add"), s.TapInView("editor-add-step"), s.Type("step-wmin", "10000"), s.TapInView("dialog-confirm")));
            cap20a.Checks = h => Expect(
                ("#step-wmin en error", () => h.Find("step-wmin")!.ClassListContains("rm-field--error")),
                ("Fuera de rango: hasta ±9999", () => h.TextOf("step-wmin-error") == "Fuera de rango: hasta ±9999"),
                ("diálogo abierto", () => h.TopDialog is StepDialog));
            cap20a.WithProbe(ErrorBorder("step-wmin", 137.5f), true).WithProbe(ErrorText("step-wmin-error"), true);
            all.Add(cap20a);
            var cap20b = Rut("CAP-20b", "CAP-20b-error-repeticion", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.SeriesTimesTen),
                "Paso de una serie ×10 con SPM 18 e Incremento SPM 200, Confirmar",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("step-edit-0"), s.Type("step-prog-spm", "200"), s.TapInView("dialog-confirm")));
            cap20b.Checks = h => Expect(
                ("Fuera de rango en la repetición 6: hasta ±999", () => h.TextOf("step-prog-spm-error") == "Fuera de rango en la repetición 6: hasta ±999"),
                ("diálogo abierto", () => h.TopDialog is StepDialog));
            cap20b.WithProbe(ErrorBorder("step-prog-spm", 283f), true).WithProbe(ErrorText("step-prog-spm-error"), true);
            all.Add(cap20b);

            // CAP-21: repeticiones fuera de rango (AC-RUT-14-06).
            var cap21 = Rut("CAP-21", "CAP-21-repeticiones-maximo", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Chip ×3, Cantidad 1000, OK",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("series-repeat-0"), s.Type("repeat-amount", "1000"), s.Tap("dialog-confirm")));
            cap21.Checks = h => Expect(
                ("Máximo 999", () => h.TextOf("repeat-error") == "Máximo 999"),
                ("diálogo abierto", () => h.TopDialog is RepeatDialog));
            cap21.WithProbe(ErrorBorder("repeat-amount", 283f), true).WithProbe(ErrorText("repeat-error"), true);
            all.Add(cap21);

            // CAP-22, CAP-23: ventana baja (AC-RUT-13-15).
            var cap22 = Rut("CAP-22", "CAP-22-editor-l640", DeviceProfile.L640, HostRoutines.Seeded(RoutineSeeds.Lista), "Editor de Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0")));
            cap22.Checks = h => Expect(
                ("barra superior entera", () => Inside(h, "editor-appbar")),
                ("campos enteros", () => Inside(h, "editor-name") && Inside(h, "editor-description")),
                ("#tab-bar oculta", () => !h.IsShown("tab-bar")));
            all.Add(cap22);
            var cap23 = Rut("CAP-23", "CAP-23-dialogo-l640", DeviceProfile.L640, HostRoutines.Seeded(RoutineSeeds.Lista), "Lápiz de Trabajo en Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("step-edit-1")));
            cap23.Checks = h => Expect(
                ("título y botones en la ventana", () => Inside(h, "dialog-title") && Inside(h, "dialog-cancel") && Inside(h, "dialog-confirm")),
                (".rm-dialog--compact", () => h.TopDialog!.Box.ClassListContains("rm-dialog--compact")),
                ("vista previa dentro del desplazamiento", () => !Step(h).PreviewIsPinned && Step(h).Preview.parent == Step(h).Content && ChildIndex(Step(h).Preview) == 0
                                                                && h.IsShown("step-preview")),
                ("mosaicos en una fila de 4", () => Step(h).Types.Columns == 4));
            all.Add(cap23);

            // CAP-24a/b: inglés.
            var cap24a = Rut("CAP-24a", "CAP-24a-editor-en", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "En inglés: editor de Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0")), "en-US");
            cap24a.Checks = h => Expect(
                ("Edit routine", () => h.IsTextVisible("Edit routine")),
                ("Save", () => h.TextOf("editor-save") == "Save"),
                ("Routine name / Description (optional)", () => h.Editor!.NameField.LabelElement.text == "Routine name"
                                                                && h.Editor.DescriptionField.LabelElement.text == "Description (optional)"),
                ("Steps (4)", () => h.TextOf("editor-steps-count") == "Steps (4)"),
                ("Step / Series", () => h.TextOf("editor-add-step") == "Step" && h.TextOf("editor-add-series") == "Series"),
                ("Warmup Work Rest Cooldown", () => Row(h, 0).Title.text == "Warmup" && Row(h, 1).Title.text == "Work" && Row(h, 2).Title.text == "Rest"
                                                    && Row(h, 3).Title.text == "Cooldown"),
                ("×3", () => Series(h, 0).Count.Text == "×3"),
                ("Add step to series", () => h.TextOf("series-add-step-0") == "Add step to series"));
            all.Add(cap24a);
            var cap24b = Rut("CAP-24b", "CAP-24b-dialogo-en", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "En inglés: diálogo de Trabajo desplazado al final",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.TapInView("step-edit-1"), s.Do(h => h.ScrollDialogToEnd())), "en-US");
            cap24b.Checks = h => Expect(
                ("Edit step", () => h.TextOf("dialog-title") == "Edit step"),
                ("Progressions (only applies to series)", () => h.IsTextVisible("Progressions (only applies to series)")),
                ("e.g: 5 / e.g: -2 (faster)", () => h.Find<RmTextField>("step-prog-watts")!.Placeholder == "e.g: 5"
                                                    && h.Find<RmTextField>("step-prog-split")!.Placeholder == "e.g: -2 (faster)"),
                ("Cancel / Confirm", () => h.TextOf("dialog-cancel") == "Cancel" && h.TextOf("dialog-confirm") == "Confirm"));
            all.Add(cap24b);

            // CAP-25a/b: letra 1,3 (AC-APP-05-05).
            all.Add(Rut("CAP-25a", "CAP-25a-lista-letra-130", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Letra 1,3×: F-LISTA",
                s => s.Tap("tab-routines"), "es-AR", 1.3));
            all.Add(Rut("CAP-25b", "CAP-25b-editor-letra-130", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Letra 1,3×: editor de Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0")), "es-AR", 1.3));

            // CAP-26: menú del clic derecho en escritorio (AC-RUT-11-09, AC-APP-09-07).
            var cap26 = Rut("CAP-26", "CAP-26-menu-texto", DeviceProfile.W1280, HostRoutines.Seeded(RoutineSeeds.Lista),
                "Editor con Escalera SPM seleccionado, portapapeles con texto, clic derecho",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.Do(h =>
                {
                    h.TestClipboard!.Text = "texto";
                    h.Editor!.NameField.FocusInput(true);
                }), s.RightClick("editor-name")));
            cap26.Checks = h => Expect(
                ("menú denso", () => h.Shell.Popups.Menu != null && h.Shell.Popups.Menu.ClassListContains("rm-menu--dense")),
                ("Cortar, Copiar y Pegar", () => h.TextOf(TextContextMenu.CutName) == "Cortar" && h.TextOf(TextContextMenu.CopyName) == "Copiar"
                                                 && h.TextOf(TextContextMenu.PasteName) == "Pegar"),
                ("sin Seleccionar todo", () => h.Find(TextContextMenu.SelectAllName) == null));
            // La selección (text.selection sobre bg.sunken), en el renglón del nombre: 1 dp debajo del borde de arriba del TextElement
            // (las letras empiezan más abajo) y a 24 dp de su comienzo, dentro de "Escalera SPM" y lejos del menú.
            cap26.WithProbe(PixelProbe.Point("selección text.selection", "editor-name .unity-text-element--inner-input-field-component", new Vector2(0.02f, 0.06f),
                PixelProbe.Composite(Tokens.TextSelection, Tokens.BgSunken), 3), true);
            all.Add(cap26);

            // CAP-28: el guardado falla (AC-RUT-09-05).
            var cap28 = Rut("CAP-28", "CAP-28-aviso-guardado", DeviceProfile.TV, HostRoutines.Scripted(r => r.FailSave = true),
                "Base que falla al guardar; editor válido, Guardar",
                s => Steps(s.Tap("tab-routines"), s.Tap("routines-add"), s.Type("editor-name", "Prueba"), s.Tap("editor-add-series"), s.Tap("editor-save")));
            cap28.Checks = h => Concat(ToastChecks(h, "No se pudo guardar la rutina", true), Expect(
                ("editor abierto con Prueba", () => h.Editor != null && h.Editor.NameField.Value == "Prueba"),
                ("Pasos (2)", () => h.TextOf("editor-steps-count") == "Pasos (2)")));
            cap28.WithProbe(PixelProbe.Point("aviso surface.overlay", "toast", new Vector2(0.02f, 0.5f), Tokens.SurfaceOverlay.Rgb.ToHex(), 3), true);
            all.Add(cap28);
        }

        private static SnapshotScenario ListShot(string id, string stem, string language, string[] summaries)
        {
            var scenario = Rut(id, stem, DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista), "Rutinas con F-LISTA" + (language == "es-AR" ? "" : " (" + language + ")"),
                s => s.Tap("tab-routines"), language);
            var names = new[] { "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" };
            scenario.Checks = h => Expect(
                ("nombres en orden", () => Card(h, 0).NameLabel.text == names[0] && Card(h, 1).NameLabel.text == names[1] && Card(h, 2).NameLabel.text == names[2]
                                           && Card(h, 3).NameLabel.text == names[3]),
                ("descripción solo en la tarjeta 1", () => Card(h, 1).DescriptionLabel.resolvedStyle.display == DisplayStyle.Flex
                                                           && Card(h, 0).DescriptionLabel.resolvedStyle.display == DisplayStyle.None),
                ("resúmenes", () => Card(h, 0).SummaryLabel.text == summaries[0] && Card(h, 1).SummaryLabel.text == summaries[1]
                                    && Card(h, 2).SummaryLabel.text == summaries[2] && Card(h, 3).SummaryLabel.text == summaries[3]),
                ("tramos 8/14/1/7", () => Card(h, 0).Bar.SegmentCount == 8 && Card(h, 1).Bar.SegmentCount == 14 && Card(h, 2).Bar.SegmentCount == 1
                                          && Card(h, 3).Bar.SegmentCount == 7),
                ("sin desborde", () => !Card(h, 0).Bar.Overflow && !Card(h, 1).Bar.Overflow),
                // DN3-07 (02-tech-design §5.4): tramos de 24 en Escalera SPM (8 entran) y de 16,4 en Pirámide 500 (14 a todo el ancho).
                ("tramos de 24 y de 16,4 ± 0,5", () => Near((float)Card(h, 0).Bar.SegmentWidth, 24, 0.5f) && Near((float)Card(h, 1).Bar.SegmentWidth, 16.4f, 0.5f)),
                ("⋮ en las 4", () => h.Find("routine-menu-3") != null),
                ("barra vertical oculta (teléfono)", () => h.Find<ScrollView>("routines-list")!.verticalScrollerVisibility == ScrollerVisibility.Hidden));
            scenario.WithProbe(PixelProbe.Point("tarjeta surface.card", "routine-card-0", new Vector2(0.6f, 0.08f), Tokens.SurfaceCard.Rgb.ToHex(), 3), true)
                .WithProbe(PixelProbe.Point("chip del ícono accent.tonalBg", "routine-card-0 .rm-routine-card__icon", new Vector2(0.12f, 0.12f),
                    PixelProbe.Composite(Tokens.AccentTonalBg, Tokens.SurfaceCard), 2), true)
                // Tramos 1, 2, 3 y 8 de Escalera SPM en su color base: el centro del tramo i está en i·26 + 12 dp de la columna de 255.
                .WithProbe(Segment("tramo 1 step.warmup", 0, Tokens.StepWarmup), true)
                .WithProbe(Segment("tramo 2 step.work", 1, Tokens.StepWork), true)
                .WithProbe(Segment("tramo 3 step.rest", 2, Tokens.StepRest), true)
                .WithProbe(Segment("tramo 8 step.cooldown", 7, Tokens.StepCooldown), true);
            return scenario;
        }

        private static PixelProbe Segment(string name, int index, ColorToken color)
        {
            return PixelProbe.Point(name, "routine-card-0 .rm-structure-bar", new Vector2((index * 26 + 12) / 255f, 0.5f), color.Rgb.ToHex(), 2);
        }

        private static SnapshotScenario EditorShot(string id, string stem, DeviceProfile profile)
        {
            var scenario = Rut(id, stem, profile, HostRoutines.Seeded(RoutineSeeds.Lista), "Editor de Escalera SPM",
                s => Steps(s.Tap("tab-routines"), s.Tap("routine-card-0")));
            scenario.Checks = h => Expect(
                ("Editar rutina", () => h.IsTextVisible("Editar rutina")),
                ("Pasos (4)", () => h.TextOf("editor-steps-count") == "Pasos (4)"),
                ("chip ×3 con caja ≥ 48 × 48", () => Series(h, 0).Count.Text == "×3" && Series(h, 0).Chip.layout.width >= 47.5f && Series(h, 0).Chip.layout.height >= 47.5f),
                ("subtítulos", () => Row(h, 0).Subtitle.Text == "5m" && Row(h, 1).Subtitle.Text == "5m  ·  18 spm · " + StepSubtitle.TrendEmoji + " SPM +2/rep"
                                     && Row(h, 2).Subtitle.Text == "2m" && Row(h, 3).Subtitle.Text == "5m"),
                ("tendencia en step-row-1", () => Row(h, 1).Subtitle.HasTrend),
                ("step-row-3 después del pie", () => h.BoundsOf("step-row-3").yMin >= h.BoundsOf("series-footer-0").yMax - 0.5f),
                ("sangría 16 ± 1", () => Near(h.BoundsOf("step-row-1").xMin - h.BoundsOf("series-header-0").xMin, 16, 1)),
                ("dos hojas (×3)", () => Series(h, 0).VisibleLayers == 2));
            var header = Over(Tokens.BrandSeed, 0.12, Tokens.BgApp);
            scenario.WithProbe(PixelProbe.Point("encabezado de la serie", "series-header-0", new Vector2(0.97f, 0.1f), header, 3), true)
                .WithProbe(PixelProbe.Point("cuerpo de la serie", "series-footer-0", new Vector2(0.97f, 0.9f), Over(Tokens.BrandSeed, 0.06, Tokens.BgApp), 3), true)
                // La línea de tiempo (04-design-notes §2.3): insignia en el color base @20 % y título en la variante .hud de su tipo.
                .WithProbe(Badge(0, Tokens.StepWarmup), true).WithProbe(Badge(1, Tokens.StepWork), true)
                .WithProbe(Badge(2, Tokens.StepRest), true).WithProbe(Badge(3, Tokens.StepCooldown), true)
                .WithProbe(Title(0, Tokens.StepWarmupHud), true).WithProbe(Title(1, Tokens.StepWorkHud), true)
                .WithProbe(Title(2, Tokens.StepRestHud), true).WithProbe(Title(3, Tokens.StepCooldownHud), true)
                // Riel continuo: a 1,5 dp del borde izquierdo del cuerpo (la columna de 387 en TV, 1 256 en W1280).
                .WithProbe(PixelProbe.Point("riel brand.seed", "series-0 .rm-series__steps", new Vector2(1.5f / (profile.Code == DeviceProfile.W1280.Code ? 1256f : 387f), 0.5f),
                    Tokens.BrandSeed.Rgb.ToHex(), 1), true)
                .WithProbe(PixelProbe.Point("cápsula accent.primary @20 %", "series-repeat-0 .rm-chip__pill", new Vector2(0.12f, 0.5f), Over(Tokens.AccentPrimary, 0.2, header), 2), true)
                .WithProbe(PixelProbe.Dominant("×3 accent.onTonal", "series-repeat-0 .rm-chip__count", Tokens.AccentOnTonal.Rgb.ToHex(), 3), true)
                // Hojas (×3: las dos) en los 8 dp de aire de abajo: la 1 ocupa 0-3 dp de la pila y la 2, 3-6.
                .WithProbe(PixelProbe.Point("hoja 1 brand.seed @20 %", "series-0 .rm-series__stack", new Vector2(0.5f, 0.25f), Over(Tokens.BrandSeed, 0.2, Tokens.BgApp), 2), true)
                .WithProbe(PixelProbe.Point("hoja 2 brand.seed @12 %", "series-0 .rm-series__stack", new Vector2(0.5f, 0.75f), header, 2), true);
            return scenario;
        }

        private static PixelProbe Badge(int row, ColorToken type)
        {
            return PixelProbe.Point("insignia " + type.Name + " @20 %", "step-row-" + row + " .rm-step-row__badge-bg", new Vector2(0.5f, 0.12f),
                Over(type, 0.2, Tokens.SurfaceCard), 2);
        }

        private static PixelProbe Title(int row, ColorToken hud)
        {
            return PixelProbe.Dominant("título " + hud.Name, "step-row-" + row + " .rm-step-row__title", hud.Rgb.ToHex(), 3);
        }

        // Borde de error de 2 dp (state.danger) a 1 dp del borde izquierdo de la caja, y su texto (state.dangerText) debajo.
        private static PixelProbe ErrorBorder(string field, float boxWidthDp)
        {
            return PixelProbe.Point("borde de error state.danger", field + " .rm-field__box", new Vector2(1f / boxWidthDp, 0.5f), Tokens.StateDanger.Rgb.ToHex(), 2);
        }

        private static PixelProbe ErrorText(string error)
        {
            return PixelProbe.Dominant("error state.dangerText", error, Tokens.StateDangerText.Rgb.ToHex(), 3);
        }

        private static SnapshotScenario BorderShot(string id, string stem, bool scrolled)
        {
            var expected = new[]
            {
                "0s", "?", "1m 30s  ·  >150W", "60m", "62m 5s", "2000m  ·  0 spm", "30s", "1m  ·  00:59/500m", "2m  ·  120-180W · 22 spm · 02:05/500m",
                "5m  ·  20 spm · " + StepSubtitle.TrendEmoji + " SPM -1/rep", "1m  ·  " + StepSubtitle.TrendEmoji + " Split +3s/rep",
            };
            var scenario = Rut(id, stem, DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Borde), "Editor de F-BORDE" + (scrolled ? ", desplazado al final" : ""),
                s => scrolled ? Steps(s.Tap("tab-routines"), s.Tap("routine-card-0"), s.ScrollToEnd("editor-scroll"), s.Settle())
                    : Steps(s.Tap("tab-routines"), s.Tap("routine-card-0")));
            scenario.Checks = h =>
            {
                var checks = new List<(string, Func<bool>)>
                {
                    ("Pasos (11)", () => h.TextOf("editor-steps-count") == "Pasos (11)"),
                    ("×1 sin hojas y ×2 con una", () => Series(h, 0).Count.Text == "×1" && Series(h, 0).VisibleLayers == 0 && Series(h, 1).Count.Text == "×2"
                                                        && Series(h, 1).VisibleLayers == 1),
                };
                for (var i = 0; i < expected.Length; i++)
                {
                    var index = i;
                    checks.Add(("subtítulo " + (index + 1), () => Row(h, index).Subtitle.Text == expected[index]));
                }

                return Expect(checks.ToArray());
            };
            return scenario;
        }

        // ── DEMO-01…12: una sola secuencia en W1280, F-VACIA (spec §7.4) ──
        private static SnapshotScenario Demo()
        {
            var demo = Rut("DEMO", "DEMO", DeviceProfile.W1280, HostRoutines.Seeded(RoutineSeeds.Empty), "Secuencia de la demo (spec §7.4)", DemoScript);
            demo.Kind = SnapshotKind.Sequence;
            demo.Shot("DEMO-01", "DEMO-01-rutinas-vacia", "Rutinas: estado vacío")
                .Shot("DEMO-02", "DEMO-02-nueva-rutina", "+, nombre Escalera SPM")
                .Shot("DEMO-03", "DEMO-03-paso-calentamiento", "+ Paso: Calentamiento, Por tiempo, Min 5")
                .Shot("DEMO-04", "DEMO-04-serie", "Confirmar y Serie")
                .Shot("DEMO-05", "DEMO-05-editar-trabajo", "Lápiz de Trabajo: Min 5, SPM 18, Incremento SPM 2")
                .Shot("DEMO-06", "DEMO-06-enfriamiento", "Confirmar; Descanso Min 2; + Paso Enfriamiento Min 5")
                .Shot("DEMO-07", "DEMO-07-guardada", "Guardar")
                .Shot("DEMO-08", "DEMO-08-descripcion-x4", "Tarjeta; descripción; ×3 → 4 → OK")
                .Shot("DEMO-09", "DEMO-09-actualizada", "Guardar")
                .Shot("DEMO-10", "DEMO-10-reabierta", "Se cierra la app y se vuelve a abrir sobre la misma base (AC-RUT-08-09)")
                .Shot("DEMO-11", "DEMO-11-eliminar", "⋮ → Eliminar")
                .Shot("DEMO-12", "DEMO-12-vacia-otra-vez", "Eliminar: vuelve el estado vacío");
            return demo;
        }

        private static IEnumerator DemoScript(ScenarioScript s)
        {
            yield return s.Tap("tab-routines");
            yield return s.Shot("DEMO-01", h => Expect(("estado vacío", () => h.IsShown("routines-empty"))));

            yield return s.Tap("routines-add");
            yield return s.Type("editor-name", "Escalera SPM");
            yield return s.Shot("DEMO-02", h => Expect(("Nueva rutina", () => h.IsTextVisible("Nueva rutina")), ("Pasos (0)", () => h.TextOf("editor-steps-count") == "Pasos (0)"),
                ("el nombre", () => h.Editor!.NameField.Value == "Escalera SPM")));

            yield return s.TapInView("editor-add-step");
            yield return s.TapInView("step-type-warmup");
            yield return s.Type("step-min", "5");
            yield return s.Shot("DEMO-03", h => Expect(("Calentamiento elegido", () => h.Find("step-type-warmup")!.ClassListContains(SegmentedControl.SelectedClass)),
                ("vista previa Calentamiento 5m", () => Step(h).Preview.Title.text == "Calentamiento" && Step(h).Preview.Subtitle.Text == "5m")));

            yield return s.TapInView("dialog-confirm");
            yield return s.TapInView("editor-add-series");
            yield return s.Shot("DEMO-04", h => Expect(("Pasos (3)", () => h.TextOf("editor-steps-count") == "Pasos (3)"), ("×3", () => Series(h, 0).Count.Text == "×3"),
                ("Trabajo 2m y Descanso 1m", () => Row(h, 1).Subtitle.Text == "2m" && Row(h, 2).Subtitle.Text == "1m")));

            yield return s.TapInView("step-edit-1");
            yield return s.Type("step-min", "5");
            yield return s.Type("step-spm", "18");
            yield return s.Type("step-prog-spm", "2");
            yield return s.Shot("DEMO-05", h => Expect(("Editar paso", () => h.TextOf("dialog-title") == "Editar paso"),
                ("vista previa con SPM +2/rep", () => Step(h).Preview.Subtitle.Text == "5m  ·  18 spm · " + StepSubtitle.TrendEmoji + " SPM +2/rep")));

            yield return s.TapInView("dialog-confirm");
            yield return s.TapInView("step-edit-2");
            yield return s.Type("step-min", "2");
            yield return s.TapInView("dialog-confirm");
            yield return s.TapInView("editor-add-step");
            yield return s.TapInView("step-type-cooldown");
            yield return s.Type("step-min", "5");
            yield return s.TapInView("dialog-confirm");
            yield return s.Shot("DEMO-06", h => Expect(("Pasos (4)", () => h.TextOf("editor-steps-count") == "Pasos (4)"),
                ("Enfriamiento después del pie", () => Row(h, 3).Title.text == "Enfriamiento" && h.BoundsOf("step-row-3").yMin >= h.BoundsOf("series-footer-0").yMax - 0.5f)));

            yield return s.Tap("editor-save");
            yield return s.Shot("DEMO-07", h => Expect(("Escalera SPM", () => Card(h, 0).NameLabel.text == "Escalera SPM"),
                ("1 serie · 1 descanso", () => Card(h, 0).SummaryLabel.text == "1 serie · 1 descanso"), ("8 tramos", () => Card(h, 0).Bar.SegmentCount == 8)));

            yield return s.Tap("routine-card-0");
            yield return s.Type("editor-description", "Sube 2 spm por repetición");
            yield return s.TapInView("series-repeat-0");
            yield return s.Type("repeat-amount", "4");
            yield return s.Tap("dialog-confirm");
            yield return s.Shot("DEMO-08", h => Expect(("Editar rutina", () => h.IsTextVisible("Editar rutina")),
                ("la descripción", () => h.Editor!.DescriptionField.Value == "Sube 2 spm por repetición"), ("×4", () => Series(h, 0).Count.Text == "×4")));

            yield return s.Tap("editor-save");
            yield return s.Shot("DEMO-09", DemoUpdated);

            yield return s.Reopen();
            yield return s.Tap("tab-routines");
            yield return s.Shot("DEMO-10", DemoUpdated);

            yield return s.Tap("routine-menu-0");
            yield return s.Tap("routine-menu-delete");
            yield return s.Shot("DEMO-11", h => Expect(("Eliminar rutina", () => h.TextOf("dialog-title") == "Eliminar rutina"),
                ("¿Eliminar \"Escalera SPM\"?", () => h.TextOf("dialog-message") == "¿Eliminar \"Escalera SPM\"?"),
                ("cinta y llave encima del velo", () => Index(h, "simulator-layer") > Index(h, "dialog-layer"))));

            yield return s.Tap("dialog-confirm");
            yield return s.Settle();
            yield return s.Shot("DEMO-12", h => Expect(("vuelve el estado vacío", () => h.IsShown("routines-empty"))));
        }

        private static IEnumerable<string> DemoUpdated(UiTestHost h)
        {
            return Expect(("la tarjeta en su lugar", () => Card(h, 0).NameLabel.text == "Escalera SPM"),
                ("la descripción arriba del resumen", () => Card(h, 0).DescriptionLabel.text == "Sube 2 spm por repetición"
                                                            && Card(h, 0).DescriptionLabel.worldBound.yMax <= Card(h, 0).SummaryLabel.worldBound.yMin + 0.5f),
                ("10 tramos", () => Card(h, 0).Bar.SegmentCount == 10));
        }

        // ── VID-01…03 (spec §7.5) ──
        private static void AddVideos(List<SnapshotScenario> all)
        {
            var vid01 = Rut("VID-01", "VID-01-editor-pasos-series", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty),
                "Editor: + Paso y Confirmar; Serie; cruz de un paso; tacho de la serie (10 s)", s => Steps(s.Tap("tab-routines"), s.Tap("routines-add")));
            vid01.Kind = SnapshotKind.Video;
            vid01.Video = new VideoSpec(10).At(0.5, h => h.Tap("editor-add-step")).At(1.5, h => h.Tap("dialog-confirm")).At(3.5, h => h.Tap("editor-add-series"))
                .At(6, h => h.Tap("step-delete-0")).At(8, h => h.Tap("series-delete-0"));
            all.Add(vid01);

            var vid02 = Rut("VID-02", "VID-02-editar-y-eliminar", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Lista),
                "Lista → Escalera SPM → volver → ⋮ → Eliminar → Eliminar (8 s)", s => s.Tap("tab-routines"));
            vid02.Kind = SnapshotKind.Video;
            vid02.Video = new VideoSpec(8).At(0.5, h => h.Tap("routine-card-0")).At(2.5, h => h.Tap("editor-back")).At(4, h => h.Tap("routine-menu-0"))
                .At(5, h => h.Tap("routine-menu-delete")).At(6.5, h => h.Tap("dialog-confirm"));
            all.Add(vid02);

            var vid03 = Rut("VID-03", "VID-03-aviso-reemplazado", DeviceProfile.TV, HostRoutines.Seeded(RoutineSeeds.Empty),
                "Editor nuevo vacío: Guardar tres veces, una por segundo (8 s)", s => Steps(s.Tap("tab-routines"), s.Tap("routines-add")));
            vid03.Kind = SnapshotKind.Video;
            vid03.Video = new VideoSpec(8).At(0.5, h => h.Tap("editor-save")).At(1.5, h => h.Tap("editor-save")).At(2.5, h => h.Tap("editor-save"));
            all.Add(vid03);
        }

        // ── Ayudas ──

        private static SnapshotScenario Rut(string id, string stem, DeviceProfile profile, HostRoutineFactory routines, string state,
                                            Func<ScenarioScript, IEnumerator>? arrange, string language = "es-AR", double textScale = 1)
        {
            var options = new UiHostOptions
            {
                Profile = profile,
                IsDesktop = profile.Code == DeviceProfile.W1280.Code,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = SimulatorLayerMode.Full,
                LanguageTags = new[] { language },
                TextScale = textScale,
                Routines = routines,
            };
            return new SnapshotScenario(id, stem, options, state, arrange) { BackendName = "Sim" };
        }
#endif

        // El color esperado de un token con alfa sobre otro opaco, compuesto en sRGB como la tubería GammaRt (sin literales: ADR
        // 016). Velo: overlay.scrim al 60 % sobre bg.app; serie: brand.seed al 12 % (encabezado) y al 6 % (cuerpo y pie).
        private static string Over(ColorToken top, double alpha, ColorToken under)
        {
            byte Mix(byte a, byte b)
            {
                return (byte)Math.Floor(a * alpha + b * (1 - alpha) + 0.5);
            }

            var t = top.Rgb;
            var u = under.Rgb;
            return new Rgb(Mix(t.R, u.R), Mix(t.G, u.G), Mix(t.B, u.B)).ToHex();
        }

        // Lo mismo sobre un color ya compuesto (la cápsula del chip sobre el encabezado de la serie, que ya es seed @12 %).
        private static string Over(ColorToken top, double alpha, string underHex)
        {
            byte Mix(byte a, byte b)
            {
                return (byte)Math.Floor(a * alpha + b * (1 - alpha) + 0.5);
            }

            var t = top.Rgb;
            var u = Rgb.Parse(underHex);
            return new Rgb(Mix(t.R, u.R), Mix(t.G, u.G), Mix(t.B, u.B)).ToHex();
        }

        private static IEnumerator Steps(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                yield return step;
            }
        }

        private static IEnumerable<string> Expect(params (string What, Func<bool> Ok)[] checks)
        {
            var problems = new List<string>();
            foreach (var check in checks)
            {
                bool ok;
                try
                {
                    ok = check.Ok();
                }
                catch (Exception error)
                {
                    problems.Add("no se cumple: " + check.What + " (" + error.GetType().Name + ": " + error.Message + ")");
                    continue;
                }

                if (!ok)
                {
                    problems.Add("no se cumple: " + check.What);
                }
            }

            return problems;
        }

        private static IEnumerable<string> Concat(IEnumerable<string> a, IEnumerable<string> b)
        {
            var list = new List<string>(a);
            list.AddRange(b);
            return list;
        }

        // Aviso entero, de 16 a 16, abajo, sin intersección con la llave (el aviso queda debajo: DN3-13) y el editor abierto.
        private static IEnumerable<string> ToastChecks(UiTestHost h, string text, bool inEditor)
        {
            return Expect(
                (text, () => h.Shell.Toasts.CurrentText == text),
                ("aviso entero en la ventana", () => Inside(h, "toast")),
                ("de 16 a 16", () => Near(h.BoundsOf("toast").xMin - h.Root.worldBound.xMin, 16, 1) && Near(h.Root.worldBound.xMax - h.BoundsOf("toast").xMax, 16, 1)),
                ("a 16 ± 1 del borde inferior", () => !inEditor || Near(h.Root.worldBound.yMax - h.BoundsOf("toast").yMax, 16, 1)),
                ("#toast.yMin ≥ #sim-open.yMax + 8", () => h.BoundsOf("toast").yMin >= h.BoundsOf("sim-open").yMax + 8 - 0.5f),
                ("el editor sigue abierto", () => !inEditor || h.Editor != null));
        }

        private static bool Inside(UiTestHost h, string name)
        {
            var window = h.Root.worldBound;
            var bounds = h.BoundsOf(name);
            return h.IsShown(name) && bounds.xMin >= window.xMin - 0.5f && bounds.yMin >= window.yMin - 0.5f && bounds.xMax <= window.xMax + 0.5f
                   && bounds.yMax <= window.yMax + 0.5f;
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return a.xMin < b.xMax && b.xMin < a.xMax && a.yMin < b.yMax && b.yMin < a.yMax;
        }

        private static bool Near(float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        private static int Index(UiTestHost h, string name)
        {
            var root = h.Find("app-root")!;
            return root.hierarchy.IndexOf(h.Find(name)!);
        }

        private static int ChildIndex(VisualElement element)
        {
            return element.parent == null ? -1 : element.parent.IndexOf(element);
        }

        private static RoutineCardView Card(UiTestHost h, int index)
        {
            return h.RoutinesScreen!.List.CardAt(index) ?? throw new InvalidOperationException("no existe la tarjeta " + index);
        }

        private static StepRowView Row(UiTestHost h, int index)
        {
            return h.Find("step-row-" + index) as StepRowView ?? throw new InvalidOperationException("no existe #step-row-" + index);
        }

        private static SeriesBlockView Series(UiTestHost h, int ordinal)
        {
            return h.Find("series-" + ordinal) as SeriesBlockView ?? throw new InvalidOperationException("no existe #series-" + ordinal);
        }

        private static StepDialog Step(UiTestHost h)
        {
            return h.TopDialog as StepDialog ?? throw new InvalidOperationException("no hay un diálogo de paso");
        }

        private static string FieldValue(UiTestHost h, string name)
        {
            return h.Find<RmTextField>(name)?.Value ?? "(no existe)";
        }
    }
}
