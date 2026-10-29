using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Localization
{
    // F-APP-10: los catálogos nuevos contra la instantánea de los .arb del legacy (l10n_legacy.json, generada por el
    // oráculo sin tocar el legacy) y contra los textos y tipos de la spec (01-po-spec §4.17, 02-tech-design §3.3).
    [TestFixture]
    public sealed class CatalogParityTests
    {
        // 01-po-spec §4.17.1 (Q-02).
        private static readonly string[] ChangedKeys = { "workoutStartTitle", "workoutStartContent", "profileSyncResult", "profileUploadResult" };

        // 01-po-spec §4.17.2: clave, es, en (en el orden del catálogo, al final).
        private static readonly string[][] NewKeys =
        {
            new[] { "routineSummarySteps", "{count, plural, one{# paso} other{# pasos}}", "{count, plural, one{# step} other{# steps}}" },
            new[] { "routineSummaryWorkRest", "{work, plural, one{# serie} other{# series}} · {rest, plural, one{# descanso} other{# descansos}}",
                "{work, plural, one{# interval} other{# intervals}} · {rest, plural, one{# rest} other{# rests}}" },
            new[] { "stepDurationMinSec", "{m}m {s}s", "{m}m {s}s" },
            new[] { "stepDurationMin", "{m}m", "{m}m" },
            new[] { "stepDurationSec", "{s}s", "{s}s" },
            new[] { "stepDurationMeters", "{meters}m", "{meters}m" },
            new[] { "stepDurationNone", "?", "?" },
            new[] { "stepTargetWattsRange", "{min}-{max}W", "{min}-{max}W" },
            new[] { "stepTargetWattsMin", ">{min}W", ">{min}W" },
            new[] { "stepTargetSpm", "{spm} spm", "{spm} spm" },
            new[] { "stepTargetSplit", "{split}/500m", "{split}/500m" },
            new[] { "stepTargetProgressions", "📈 {list}", "📈 {list}" },
            new[] { "stepProgressionSpm", "SPM {delta}/rep", "SPM {delta}/rep" },
            new[] { "stepProgressionWatts", "W {delta}/rep", "W {delta}/rep" },
            new[] { "stepProgressionSplit", "Split {delta}s/rep", "Split {delta}s/rep" },
        };

        // 03-rutinas/01-po-spec §11.2 (Q-01): las 16 claves de la It. 3, en el orden del catálogo, al final.
        private static readonly string[][] ItThreeKeys =
        {
            new[] { "navBack", "Atrás", "Back" },
            new[] { "menuShow", "Mostrar menú", "Show menu" },
            new[] { "textCut", "Cortar", "Cut" },
            new[] { "textCopy", "Copiar", "Copy" },
            new[] { "textPaste", "Pegar", "Paste" },
            new[] { "textSelectAll", "Seleccionar todo", "Select all" },
            new[] { "editorRepeatCount", "×{count}", "×{count}" },
            new[] { "editorErrorRange", "Fuera de rango: hasta ±{max}", "Out of range: up to ±{max}" },
            new[] { "editorErrorRangeRep", "Fuera de rango en la repetición {rep}: hasta ±{max}", "Out of range in repetition {rep}: up to ±{max}" },
            new[] { "editorRepetitionsMax", "Máximo {max}", "Maximum {max}" },
            new[] { "editorRepetitionsOutOfRange", "Con ×{count}, un objetivo queda fuera de rango", "With ×{count}, a target goes out of range" },
            new[] { "editorStepOutOfRange", "Hay un paso con valores fuera de rango", "A step has out-of-range values" },
            new[] { "editorSaveError", "No se pudo guardar la rutina", "Couldn't save the routine" },
            new[] { "routineDeleteError", "No se pudo eliminar la rutina", "Couldn't delete the routine" },
            new[] { "routinesLoadError", "No se pudieron cargar las rutinas", "Couldn't load routines" },
            new[] { "retry", "Reintentar", "Retry" },
        };

        // 04a-motor-hud-funcional/01-po-spec §11.2 (Q-01): las 12 claves de la It. 4a, en el orden del catálogo, al final.
        private static readonly string[][] ItFourAKeys =
        {
            new[] { "hudSpm", "SPM", "SPM" },
            new[] { "hudSplit", "Split /500m", "Split /500m" },
            new[] { "hudWatts", "Watts", "Watts" },
            new[] { "hudUnitWatts", "W", "W" },
            new[] { "hudKcal", "kcal", "kcal" },
            new[] { "hudBpm", "BPM", "BPM" },
            new[] { "hudUnitMeters", "m", "m" },
            new[] { "hudFreeTitle", "Entrenamiento Libre", "Free Workout" },
            new[] { "hudUpNext", "Próximo:", "Up next:" },
            new[] { "hudTarget", "obj. {value}", "target {value}" },
            new[] { "workoutSaveError", "No se pudo guardar la sesión", "Couldn't save the session" },
            new[] { "workoutRoutineCannotStart", "Esta rutina no se puede iniciar: revisá sus pasos en Rutinas",
                "This routine can't be started: check its steps in Routines" },
        };

        // 02-tech-design §3.3 (QA-07) + 03-rutinas §11.2: las 26 claves con parámetros, nombre y tipo. Ninguna otra tiene metadatos.
        private static readonly Dictionary<string, string> ExpectedPlaceholders = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Las 7 del legacy (AC-APP-10-04).
            { "hrmConnectingTo", "name:String" },
            { "hrmConnectError", "error:String" },
            { "workoutStartContent", "summary:String" },
            { "routineDeleteContent", "name:String" },
            { "editorStepsCount", "count:int" },
            { "profileSyncResult", "count:int" },
            { "profileUploadResult", "count:int" },
            // Las 14 nuevas con parámetros (stepDurationNone no tiene).
            { "routineSummarySteps", "count:int" },
            { "routineSummaryWorkRest", "work:int, rest:int" },
            { "stepDurationMinSec", "m:int, s:int" },
            { "stepDurationMin", "m:int" },
            { "stepDurationSec", "s:int" },
            { "stepDurationMeters", "meters:int" },
            { "stepTargetWattsRange", "min:int, max:int" },
            { "stepTargetWattsMin", "min:int" },
            { "stepTargetSpm", "spm:int" },
            { "stepTargetSplit", "split:String" },
            { "stepTargetProgressions", "list:String" },
            { "stepProgressionSpm", "delta:String" },
            { "stepProgressionWatts", "delta:String" },
            { "stepProgressionSplit", "delta:String" },
            // Las 5 de la It. 3 con parámetros.
            { "editorRepeatCount", "count:int" },
            { "editorErrorRange", "max:String" },
            { "editorErrorRangeRep", "rep:int, max:String" },
            { "editorRepetitionsMax", "max:int" },
            { "editorRepetitionsOutOfRange", "count:int" },
            // La de la It. 4a con parámetro (04a §11.2).
            { "hudTarget", "value:String" },
        };

        private static LocalizationCatalog Es => LocalizationFixture.Catalog("es");

        private static LocalizationCatalog En => LocalizationFixture.Catalog("en");

        // AC-APP-10-01, AC-APP-10-10: 202 claves en es y las mismas 202 en en (159 del legacy + 15 de la It. 1 + 16 de la It. 3
        // + 12 de la It. 4a).
        [Test, Category("AC-APP-10-01"), Category("AC-APP-10-10")]
        public void SameKeysInBothLanguages()
        {
            var es = Es;
            var en = En;
            Assert.That(es.Locale, Is.EqualTo("es"));
            Assert.That(en.Locale, Is.EqualTo("en"));
            Assert.That(es.Keys.Count, Is.EqualTo(202));
            Assert.That(en.Keys.Count, Is.EqualTo(202));
            Assert.That(new HashSet<string>(es.Keys).Count, Is.EqualTo(202), "claves repetidas en es");
            Assert.That(en.Keys, Is.EquivalentTo(es.Keys), "una clave existe en un solo idioma");
            Assert.That(en.Keys, Is.EqualTo(es.Keys), "mismo orden en los dos archivos");
            Assert.That(LocalizationFixture.LegacyTexts("es").Count, Is.EqualTo(159));
            Assert.That(LocalizationFixture.LegacyTexts("en").Count, Is.EqualTo(159));
        }

        // AC-APP-10-02: las 155 claves del legacy que no cambian tienen el texto del .arb carácter por carácter.
        [TestCase("es")]
        [TestCase("en")]
        [Category("AC-APP-10-02")]
        public void UnchangedLegacyTextsAreIdentical(string language)
        {
            var catalog = LocalizationFixture.Catalog(language);
            var changed = new HashSet<string>(ChangedKeys, StringComparer.Ordinal);
            var compared = 0;
            var different = new List<string>();
            foreach (var entry in LocalizationFixture.LegacyTexts(language))
            {
                if (changed.Contains(entry.Key))
                {
                    continue;
                }

                compared++;
                if (!catalog.Contains(entry.Key) || catalog.Message(entry.Key) != entry.Value)
                {
                    different.Add(entry.Key);
                }
            }

            Assert.That(compared, Is.EqualTo(155));
            Assert.That(different, Is.Empty, language + ": textos distintos del legacy: " + string.Join(", ", different));
        }

        // AC-APP-10-02 (QA-V-06): lo que VE el usuario, no solo el patrón guardado. Así un cambio del formateador (escape con
        // apóstrofos, `#` o llaves) que cambie lo visible hace fallar el test aunque el catálogo no cambie.
        // - Sin parámetros (151): Localizer.Get(clave) = texto del .arb. Flutter lo muestra tal cual (gen-l10n sin
        //   use-escaping: sin parámetros no hay sintaxis ICU que interpretar).
        // - Con parámetros (las 4 sin cambios, todas simples y sin plural): Localizer.Format con valores de prueba = texto
        //   del .arb con cada {parámetro} reemplazado, como la interpolación del código que genera gen-l10n
        //   ('Conectando a $name...'). Los valores llevan apóstrofo, llaves y # para ver que se insertan literales.
        [TestCase("es")]
        [TestCase("en")]
        [Category("AC-APP-10-02"), Category("QA-V-06")]
        public void UnchangedLegacyTextsLookIdenticalOnScreen(string language)
        {
            var catalog = LocalizationFixture.Catalog(language);
            var localizer = LocalizationFixture.Set.For(language == "es" ? AppLanguage.Es : AppLanguage.En);
            Assert.That(localizer.LanguageCode, Is.EqualTo(language));
            var changed = new HashSet<string>(ChangedKeys, StringComparer.Ordinal);
            var plain = 0;
            var withParameters = 0;
            var different = new List<string>();
            foreach (var entry in LocalizationFixture.LegacyTexts(language))
            {
                if (changed.Contains(entry.Key))
                {
                    continue;
                }

                var placeholders = catalog.Placeholders(entry.Key);
                string visible;
                var expected = entry.Value;
                if (placeholders.Count == 0)
                {
                    plain++;
                    visible = localizer.Get(entry.Key);
                }
                else
                {
                    withParameters++;
                    var args = new MessageArg[placeholders.Count];
                    for (var i = 0; i < placeholders.Count; i++)
                    {
                        var placeholder = placeholders[i];
                        string rendered;
                        if (placeholder.Kind == MessageArgKind.Number)
                        {
                            var number = 1234 + i;
                            args[i] = MessageArg.Number(placeholder.Name, number);
                            rendered = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            rendered = "Polar H10 'A' {x} #" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            args[i] = MessageArg.Text(placeholder.Name, rendered);
                        }

                        expected = expected.Replace("{" + placeholder.Name + "}", rendered);
                    }

                    visible = localizer.Format(entry.Key, args);
                }

                if (visible != expected)
                {
                    different.Add(entry.Key + ": \"" + visible + "\" en lugar de \"" + expected + "\"");
                }
            }

            Assert.That(plain, Is.EqualTo(151), language + ": claves sin parámetros");
            Assert.That(withParameters, Is.EqualTo(4), language + ": claves con parámetros sin cambios");
            Assert.That(different, Is.Empty, language + ": texto visible distinto del legacy:\n" + string.Join("\n", different));
        }

        // AC-APP-10-02: los casos especiales que nombra la spec.
        [Test, Category("AC-APP-10-02")]
        public void SpecialCharacters()
        {
            var es = LocalizationFixture.Es;
            var en = LocalizationFixture.En;
            Assert.That(es.Get(L10nKeys.DeviceNoResultsHint), Is.EqualTo("Asegurate que el rower esté encendido\ny el Bluetooth activado"));
            Assert.That(en.Get(L10nKeys.DeviceNoResultsHint), Is.EqualTo("Make sure the rower is on\nand Bluetooth is enabled"));
            foreach (var key in new[] { L10nKeys.DeviceNoResultsHint, L10nKeys.DeviceBtOffHint, L10nKeys.DeviceBtDeniedHint, L10nKeys.EditorEmptySteps })
            {
                foreach (var l10n in LocalizationFixture.Both)
                {
                    var text = l10n.Get(key);
                    Assert.That(text.Split('\n').Length, Is.EqualTo(2), l10n.LanguageCode + "/" + key + ": un salto de línea");
                    Assert.That(text.Contains(" \n") || text.Contains("\n "), Is.False, l10n.LanguageCode + "/" + key + ": sin espacios alrededor del salto");
                    Assert.That(text.Contains("\r"), Is.False);
                }
            }

            Assert.That(es.Get(L10nKeys.WorkoutOrPickRoutine), Is.EqualTo("  O elegí una rutina:"));
            Assert.That(en.Get(L10nKeys.WorkoutOrPickRoutine), Is.EqualTo("  Or pick a routine:"));
            Assert.That(es.Get(L10nKeys.NavWorkout), Is.EqualTo("Workout"), "Q-08");
            Assert.That(es.Format(L10nKeys.RoutineDeleteContent, MessageArg.Text("name", "x")), Is.EqualTo("¿Eliminar \"x\"?"));
            Assert.That(en.Format(L10nKeys.RoutineDeleteContent, MessageArg.Text("name", "x")), Is.EqualTo("Delete \"x\"?"));
            Assert.That(es.Get(L10nKeys.MetricSpm), Is.EqualTo("SPM"));
            Assert.That(es.Get(L10nKeys.MetricSplit), Is.EqualTo("SPLIT 500m"));
            Assert.That(en.Get(L10nKeys.MetricSplit), Is.EqualTo("SPLIT 500m"));
            Assert.That(es.Get(L10nKeys.SessionChartSplit), Is.EqualTo("Split 500m"));
            Assert.That(es.Get(L10nKeys.HistoryStatCalories), Is.EqualTo("kcal"));
            Assert.That(es.Get(L10nKeys.HrmBpm), Is.EqualTo("bpm"));
            Assert.That(es.Get(L10nKeys.Ok), Is.EqualTo("OK"));
            Assert.That(es.Get(L10nKeys.WorkoutCompleted), Is.EqualTo("¡Entrenamiento completado!"));
            Assert.That(es.Get(L10nKeys.ProfileAskUploadDialog), Does.StartWith("¿"));
            Assert.That(en.Get(L10nKeys.ProfileDisconnectContent), Does.Contain("won't"));
        }

        // AC-APP-10-03: las 4 claves que cambian (01-po-spec §4.17.1).
        [Test, Category("AC-APP-10-03")]
        public void FourChangedKeys()
        {
            var es = Es;
            var en = En;
            Assert.That(es.Message("workoutStartTitle"), Is.EqualTo("¿Iniciar entrenamiento?"));
            Assert.That(en.Message("workoutStartTitle"), Is.EqualTo("Start workout?"));
            Assert.That(es.Message("workoutStartContent"), Is.EqualTo("{summary}\n\n¿Iniciar el entrenamiento?"));
            Assert.That(en.Message("workoutStartContent"), Is.EqualTo("{summary}\n\nStart the workout?"));
            Assert.That(es.Message("profileSyncResult"), Is.EqualTo("{count, plural, one{# sesión sincronizada} other{# sesiones sincronizadas}}"));
            Assert.That(en.Message("profileSyncResult"), Is.EqualTo("{count, plural, one{# session synced} other{# sessions synced}}"));
            Assert.That(es.Message("profileUploadResult"), Is.EqualTo("{count, plural, one{# sesión subida} other{# sesiones subidas}}"));
            Assert.That(en.Message("profileUploadResult"), Is.EqualTo("{count, plural, one{# session uploaded} other{# sessions uploaded}}"));

            // Y el legacy decía lo de la columna "legacy" de la spec (la instantánea lo confirma).
            var legacyEs = ToDictionary(LocalizationFixture.LegacyTexts("es"));
            var legacyEn = ToDictionary(LocalizationFixture.LegacyTexts("en"));
            Assert.That(legacyEs["workoutStartTitle"], Is.EqualTo("Iniciar entrenamiento?"));
            Assert.That(legacyEs["workoutStartContent"], Is.EqualTo("{summary}\n\nIniciar el entrenamiento?"));
            Assert.That(legacyEs["profileSyncResult"], Is.EqualTo("{count} sesiones sincronizadas"));
            Assert.That(legacyEs["profileUploadResult"], Is.EqualTo("{count} sesiones subidas"));
            Assert.That(legacyEn["workoutStartContent"], Is.EqualTo("{summary}\n\nStart the workout?"));
            Assert.That(legacyEn["profileSyncResult"], Is.EqualTo("{count} sessions synced"));
            Assert.That(legacyEn["profileUploadResult"], Is.EqualTo("{count} sessions uploaded"));
        }

        // AC-APP-10-04: mismos parámetros (nombre y tipo) en es y en; las 26 claves (§3.3 y la It. 3) y ninguna otra.
        [Test, Category("AC-APP-10-04")]
        public void SameParametersNamesAndTypes()
        {
            var problems = new List<string>();
            var withParameters = 0;
            foreach (var language in new[] { "es", "en" })
            {
                var catalog = LocalizationFixture.Catalog(language);
                foreach (var key in catalog.Keys)
                {
                    var actual = LocalizationFixture.Describe(catalog.Placeholders(key));
                    ExpectedPlaceholders.TryGetValue(key, out var expected);
                    if (actual != (expected ?? ""))
                    {
                        problems.Add(language + "/" + key + ": \"" + actual + "\", se esperaba \"" + (expected ?? "") + "\"");
                    }

                    if (actual.Length > 0)
                    {
                        withParameters++;
                    }
                }

                // Las 7 del legacy conservan nombre y tipo de los .arb.
                foreach (var legacy in LocalizationFixture.LegacyPlaceholders(language))
                {
                    var actual = LocalizationFixture.Describe(catalog.Placeholders(legacy.Key));
                    if (actual != legacy.Value)
                    {
                        problems.Add(language + "/" + legacy.Key + ": legacy \"" + legacy.Value + "\", catálogo \"" + actual + "\"");
                    }
                }

                Assert.That(LocalizationFixture.LegacyPlaceholders(language).Count, Is.EqualTo(7), language + ": metadatos del legacy");
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(withParameters, Is.EqualTo(2 * 27));
            Assert.That(ExpectedPlaceholders.Count, Is.EqualTo(27));
        }

        // AC-APP-10-07: las 15 claves nuevas con los textos de la spec y los tipos de §3.3, al final del catálogo.
        [Test, Category("AC-APP-10-07")]
        public void FifteenNewKeys()
        {
            var es = Es;
            var en = En;
            var legacy = ToDictionary(LocalizationFixture.LegacyTexts("es"));
            Assert.That(NewKeys.Length, Is.EqualTo(15));
            for (var i = 0; i < NewKeys.Length; i++)
            {
                var key = NewKeys[i][0];
                Assert.That(legacy.ContainsKey(key), Is.False, key + " no existía en el legacy");
                Assert.That(es.Message(key), Is.EqualTo(NewKeys[i][1]), "es/" + key);
                Assert.That(en.Message(key), Is.EqualTo(NewKeys[i][2]), "en/" + key);
                Assert.That(es.Keys[159 + i], Is.EqualTo(key), "las nuevas van al final, en el orden de la spec");
                ExpectedPlaceholders.TryGetValue(key, out var expected);
                Assert.That(LocalizationFixture.Describe(es.Placeholders(key)), Is.EqualTo(expected ?? ""), "es/" + key);
                Assert.That(LocalizationFixture.Describe(en.Placeholders(key)), Is.EqualTo(expected ?? ""), "en/" + key);
            }
        }

        // It. 3 (Q-01; 03-rutinas/01-po-spec §11.2): las 16 claves nuevas con sus textos es/en y sus tipos, al final del catálogo.
        [Test, Category("AC-APP-10-07")]
        public void SixteenItThreeKeys()
        {
            var es = Es;
            var en = En;
            var legacy = ToDictionary(LocalizationFixture.LegacyTexts("es"));
            Assert.That(ItThreeKeys.Length, Is.EqualTo(16));
            for (var i = 0; i < ItThreeKeys.Length; i++)
            {
                var key = ItThreeKeys[i][0];
                Assert.That(legacy.ContainsKey(key), Is.False, key + " no existía en el legacy");
                Assert.That(es.Message(key), Is.EqualTo(ItThreeKeys[i][1]), "es/" + key);
                Assert.That(en.Message(key), Is.EqualTo(ItThreeKeys[i][2]), "en/" + key);
                Assert.That(es.Keys[174 + i], Is.EqualTo(key), "las de la It. 3 van al final, en el orden de la spec");
                Assert.That(en.Keys[174 + i], Is.EqualTo(key));
                ExpectedPlaceholders.TryGetValue(key, out var expected);
                Assert.That(LocalizationFixture.Describe(es.Placeholders(key)), Is.EqualTo(expected ?? ""), "es/" + key);
                Assert.That(LocalizationFixture.Describe(en.Placeholders(key)), Is.EqualTo(expected ?? ""), "en/" + key);
            }

            var l10n = LocalizationFixture.Es;
            Assert.That(l10n.Format(L10nKeys.EditorRepeatCount, MessageArg.Number("count", 2000000000)), Is.EqualTo("×2000000000"));
            Assert.That(l10n.Format(L10nKeys.EditorErrorRangeRep, MessageArg.Number("rep", 6), MessageArg.Text("max", "999")),
                Is.EqualTo("Fuera de rango en la repetición 6: hasta ±999"));
            Assert.That(LocalizationFixture.En.Get(L10nKeys.Retry), Is.EqualTo("Retry"));
        }

        // AC-APP-10-10 (04a §11.2): las 12 claves de la It. 4a con sus textos es/en y sus tipos, al final del catálogo. Los
        // textos del HUD que el legacy tiene fijos salen del catálogo con el español idéntico (Q-01); Up next: se traduce
        // (DV-24).
        [Test, Category("AC-APP-10-10")]
        public void TwelveItFourAKeys()
        {
            var es = Es;
            var en = En;
            var legacy = ToDictionary(LocalizationFixture.LegacyTexts("es"));
            Assert.That(ItFourAKeys.Length, Is.EqualTo(12));
            for (var i = 0; i < ItFourAKeys.Length; i++)
            {
                var key = ItFourAKeys[i][0];
                Assert.That(legacy.ContainsKey(key), Is.False, key + " no existía en el legacy");
                Assert.That(es.Message(key), Is.EqualTo(ItFourAKeys[i][1]), "es/" + key);
                Assert.That(en.Message(key), Is.EqualTo(ItFourAKeys[i][2]), "en/" + key);
                Assert.That(es.Keys[190 + i], Is.EqualTo(key), "las de la It. 4a van al final, en el orden de la spec");
                Assert.That(en.Keys[190 + i], Is.EqualTo(key));
                ExpectedPlaceholders.TryGetValue(key, out var expected);
                Assert.That(LocalizationFixture.Describe(es.Placeholders(key)), Is.EqualTo(expected ?? ""), "es/" + key);
                Assert.That(LocalizationFixture.Describe(en.Placeholders(key)), Is.EqualTo(expected ?? ""), "en/" + key);
            }

            Assert.That(LocalizationFixture.Es.Format(L10nKeys.HudTarget, MessageArg.Text("value", "02:05")), Is.EqualTo("obj. 02:05"));
            Assert.That(LocalizationFixture.En.Format(L10nKeys.HudTarget, MessageArg.Text("value", "160")), Is.EqualTo("target 160"));
            Assert.That(LocalizationFixture.Es.Get(L10nKeys.WorkoutCompleted), Is.EqualTo("\u00a1Entrenamiento completado!"), "se reusa (spec §11.2)");
        }

        // AC-APP-10-08: no se pierde ninguna de las 159 claves del legacy (incluidas las sin uso y las de código muerto).
        [Test, Category("AC-APP-10-08")]
        public void NoLegacyKeyLost()
        {
            foreach (var language in new[] { "es", "en" })
            {
                var catalog = LocalizationFixture.Catalog(language);
                var legacyKeys = new List<string>();
                foreach (var entry in LocalizationFixture.LegacyTexts(language))
                {
                    legacyKeys.Add(entry.Key);
                    Assert.That(catalog.Contains(entry.Key), Is.True, language + ": se perdió " + entry.Key);
                }

                Assert.That(legacyKeys.Count, Is.EqualTo(159));
                var head = new List<string>();
                for (var i = 0; i < 159; i++)
                {
                    head.Add(catalog.Keys[i]);
                }

                Assert.That(head, Is.EqualTo(legacyKeys), language + ": las del legacy conservan su orden");
            }

            foreach (var key in new[]
            {
                "workoutStartTitle", "historyStatWatts", "historyStatCalories",
                "workoutFinishTitle", "workoutFinishContent", "workoutPause", "workoutResume", "metricTime", "metricCalories", "metricFreeLabel",
            })
            {
                Assert.That(Es.Contains(key) && En.Contains(key), Is.True, key);
            }
        }

        // Formato de los archivos (02-tech-design §3.3): UTF-8 sin BOM, LF y salto final.
        [TestCase("es")]
        [TestCase("en")]
        [Category("AC-APP-10-01")]
        public void FilesAreUtf8LfWithoutBom(string language)
        {
            var bytes = System.IO.File.ReadAllBytes(RepoPaths.Catalog(language));
            Assert.That(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, Is.False, "sin BOM");
            var text = LocalizationFixture.ReadCatalog(language);
            Assert.That(text.Contains("\r"), Is.False, "LF");
            Assert.That(text.EndsWith("}\n", StringComparison.Ordinal), Is.True, "salto de línea final");
        }

        private static Dictionary<string, string> ToDictionary(IReadOnlyList<KeyValuePair<string, string>> entries)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }
    }
}
