using System;
using System.Collections.Generic;
using System.Numerics;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Core.Text;

namespace RowMate.App.Routines
{
    // El diálogo de paso sin la vista (F-RUT-13; legacy: _StepDialogState, lib/features/routines/routine_editor_screen.dart
    // :474-739). Cada campo es el texto tal cual se escribió; la vista lo escribe en cada cambio y lee Preview() para la vista
    // previa (AC-RUT-13-16) y Confirm() al tocar Confirmar.
    // - Nuevo (:484-485): Trabajo, Por tiempo, todo vacío. Editar (:498-521): ver ForEdit.
    // - _build() (:531-562): cada número con int.tryParse(texto.trim()) de Dart (DartIntParser sobre DartText.Trim; lo que
    //   no es entero cuenta como vacío). Por tiempo: (Min ?? 0) × 60 + (Seg ?? 0), siempre una duración y sin distancia;
    //   por distancia: Metros o null (sin meta) y sin duración; split = (min ?? 0) × 60 + (seg ?? 0) y ≤ 0 → sin split.
    //   Las progresiones se leen siempre, aunque no se vean (un paso suelto conserva las que trae: AC-RUT-12-03).
    // - Topes de Q-92 (StepLimits) solo en Confirm, con cuentas exactas (BigInteger: un número de cualquier largo es "fuera
    //   de rango" y nunca desborda). Solo cuenta el modo elegido; un split ≤ 0 es "sin split" aunque pase el tope; las
    //   progresiones se controlan solo en una serie (ShowsProgressions), primero su campo y después cada repetición.
    // Sin validaciones de sentido (Q-25, AC-RUT-13-09): mín > máx, 0 y negativos dentro del tope pasan tal cual.
    public sealed class StepDialogForm
    {
        private static readonly BigInteger Sixty = new BigInteger(60);

        private string _minutes = "";
        private string _seconds = "";
        private string _meters = "";
        private string _wattsMin = "";
        private string _wattsMax = "";
        private string _spm = "";
        private string _splitMin = "";
        private string _splitSec = "";
        private string _progressionSpm = "";
        private string _progressionWatts = "";
        private string _progressionSplit = "";

        private StepDialogForm(bool isNew, bool inSeries, int seriesRepeatCount)
        {
            IsNew = isNew;
            ShowsProgressions = inSeries;
            SeriesRepeatCount = seriesRepeatCount;
            Type = StepType.Work;
        }

        // editorNewStep (true) o editorEditStep (false): AC-RUT-13-01.
        public bool IsNew { get; }

        // Solo en una serie (:688): + Agregar paso a serie, o el lápiz de un paso de serie.
        public bool ShowsProgressions { get; }

        // El ×N actual de la serie (para el tope en cada repetición, AC-RUT-13-12); 1 en un paso suelto.
        public int SeriesRepeatCount { get; }

        public StepType Type { get; set; }

        // Modo Por distancia (false = Por tiempo). Cambiar de modo no borra lo escrito en el otro (AC-RUT-13-04).
        public bool ByDistance { get; set; }

        public string Minutes { get => _minutes; set => _minutes = value ?? ""; }

        public string Seconds { get => _seconds; set => _seconds = value ?? ""; }

        public string Meters { get => _meters; set => _meters = value ?? ""; }

        public string WattsMin { get => _wattsMin; set => _wattsMin = value ?? ""; }

        public string WattsMax { get => _wattsMax; set => _wattsMax = value ?? ""; }

        public string Spm { get => _spm; set => _spm = value ?? ""; }

        public string SplitMin { get => _splitMin; set => _splitMin = value ?? ""; }

        public string SplitSec { get => _splitSec; set => _splitSec = value ?? ""; }

        public string ProgressionSpm { get => _progressionSpm; set => _progressionSpm = value ?? ""; }

        public string ProgressionWatts { get => _progressionWatts; set => _progressionWatts = value ?? ""; }

        public string ProgressionSplit { get => _progressionSplit; set => _progressionSplit = value ?? ""; }

        // + Paso (sin progresiones) y + Agregar paso a serie (con progresiones y el N de la serie).
        public static StepDialogForm ForNew(bool inSeries, int seriesRepeatCount)
        {
            return new StepDialogForm(true, inSeries, seriesRepeatCount);
        }

        // :498-521: el tipo; distancia si el paso tiene distancia (si no, tiempo, también sin meta); la duración y el split
        // partidos con ~/ y % de Dart (el resto nunca es negativo: −90 → "-1" y "30"), sin ceros a la izquierda; los metros,
        // vatios, SPM y las progresiones (un 0 se ve "0"), aunque el paso sea suelto.
        public static StepDialogForm ForEdit(IntervalStep existing, bool inSeries, int seriesRepeatCount)
        {
            if (existing == null)
            {
                throw new ArgumentNullException(nameof(existing));
            }

            var form = new StepDialogForm(false, inSeries, seriesRepeatCount)
            {
                Type = existing.Type,
                ByDistance = existing.DistanceMeters.HasValue,
            };
            if (existing.DurationSeconds.HasValue)
            {
                form.Minutes = Fmt.Integer(DartMath.TruncDiv((long)existing.DurationSeconds.Value, 60));
                form.Seconds = Fmt.Integer(DartMath.Mod((long)existing.DurationSeconds.Value, 60));
            }

            form.Meters = Text(existing.DistanceMeters);
            form.WattsMin = Text(existing.TargetWattsMin);
            form.WattsMax = Text(existing.TargetWattsMax);
            form.Spm = Text(existing.TargetSpm);
            if (existing.TargetSplitSeconds.HasValue)
            {
                form.SplitMin = Fmt.Integer(DartMath.TruncDiv((long)existing.TargetSplitSeconds.Value, 60));
                form.SplitSec = Fmt.Integer(DartMath.Mod((long)existing.TargetSplitSeconds.Value, 60));
            }

            form.ProgressionSpm = Text(existing.ProgressionSpm);
            form.ProgressionWatts = Text(existing.ProgressionWatts);
            form.ProgressionSplit = Text(existing.ProgressionSplitSeconds);
            return form;
        }

        // _build() sin topes: la fila que se vería al confirmar (AC-RUT-13-16). Lo que no entra en un int se ve vacío.
        // routineId 0, order 0 y sin serie, como el legacy: el editor le pone su lugar y su serie.
        public IntervalStep Preview()
        {
            var values = Read();
            return new IntervalStep(0, 0, Type)
            {
                DurationSeconds = ToInt(values.Duration),
                DistanceMeters = ToInt(values.Meters),
                TargetWattsMin = ToInt(values.WattsMin),
                TargetWattsMax = ToInt(values.WattsMax),
                TargetSpm = ToInt(values.Spm),
                TargetSplitSeconds = ToInt(values.Split),
                ProgressionSpm = ToInt(values.ProgressionSpm),
                ProgressionWatts = ToInt(values.ProgressionWatts),
                ProgressionSplitSeconds = ToInt(values.ProgressionSplit),
            };
        }

        // _build() + Q-92: el paso (el diálogo se cierra) o los errores por campo (queda abierto).
        public StepDialogResult Confirm()
        {
            var values = Read();
            var errors = new List<FieldError>();

            // 1. Cada campo que cuenta, contra su tope (AC-RUT-13-11).
            if (ByDistance)
            {
                CheckField(errors, values.Meters, StepLimits.MaxDistanceMeters, StepField.Meters, StepLimits.DistanceMaxText);
            }
            else
            {
                CheckField(errors, values.Duration, StepLimits.MaxDurationSeconds, StepField.Duration, StepLimits.DurationMaxText);
            }

            CheckField(errors, values.WattsMin, StepLimits.MaxWatts, StepField.WattsMin, StepLimits.WattsMaxText);
            CheckField(errors, values.WattsMax, StepLimits.MaxWatts, StepField.WattsMax, StepLimits.WattsMaxText);
            CheckField(errors, values.Spm, StepLimits.MaxSpm, StepField.Spm, StepLimits.SpmMaxText);
            CheckField(errors, values.Split, StepLimits.MaxSplitSeconds, StepField.Split, StepLimits.SplitMaxText);
            if (ShowsProgressions)
            {
                CheckField(errors, values.ProgressionSpm, StepLimits.MaxProgressionSpm, StepField.ProgressionSpm, StepLimits.ProgressionSpmMaxText);
                CheckField(errors, values.ProgressionWatts, StepLimits.MaxProgressionWatts, StepField.ProgressionWatts, StepLimits.ProgressionWattsMaxText);
                CheckField(errors, values.ProgressionSplit, StepLimits.MaxProgressionSplitSeconds, StepField.ProgressionSplit,
                    StepLimits.ProgressionSplitMaxText);
            }

            // 2. Cada repetición de la serie (AC-RUT-13-12), con los campos que pasaron su tope: el error va debajo de la
            // progresión, con el {max} del objetivo que se pasa. W mín y W máx comparten la progresión de vatios: cuenta la
            // primera repetición entre los dos. Una progresión sin su objetivo no se controla.
            if (ShowsProgressions && SeriesRepeatCount >= 1)
            {
                CheckRepetitions(errors, StepField.ProgressionSpm, values.ProgressionSpm, StepLimits.MaxSpm, StepLimits.SpmMaxText,
                    Target(errors, StepField.Spm, values.Spm));
                CheckRepetitions(errors, StepField.ProgressionWatts, values.ProgressionWatts, StepLimits.MaxWatts, StepLimits.WattsMaxText,
                    Target(errors, StepField.WattsMin, values.WattsMin), Target(errors, StepField.WattsMax, values.WattsMax));
                CheckRepetitions(errors, StepField.ProgressionSplit, values.ProgressionSplit, StepLimits.MaxSplitSeconds, StepLimits.SplitMaxText,
                    Target(errors, StepField.Split, values.Split));
            }

            if (errors.Count > 0)
            {
                errors.Sort((a, b) => ((int)a.Field).CompareTo((int)b.Field));
                return StepDialogResult.Invalid(errors);
            }

            return StepDialogResult.Valid(Preview());
        }

        private Values Read()
        {
            BigInteger? duration = null;
            BigInteger? meters = null;
            if (ByDistance)
            {
                meters = Parse(_meters);
            }
            else
            {
                duration = (Parse(_minutes) ?? BigInteger.Zero) * Sixty + (Parse(_seconds) ?? BigInteger.Zero);
            }

            var splitTotal = (Parse(_splitMin) ?? BigInteger.Zero) * Sixty + (Parse(_splitSec) ?? BigInteger.Zero);
            return new Values(
                duration,
                meters,
                Parse(_wattsMin),
                Parse(_wattsMax),
                Parse(_spm),
                splitTotal.Sign > 0 ? splitTotal : (BigInteger?)null,
                Parse(_progressionSpm),
                Parse(_progressionWatts),
                Parse(_progressionSplit));
        }

        // int.tryParse(texto.trim()).
        private static BigInteger? Parse(string text)
        {
            return DartIntParser.TryParse(DartText.Trim(text));
        }

        private static int? ToInt(BigInteger? value)
        {
            if (!value.HasValue || value.Value > int.MaxValue || value.Value < int.MinValue)
            {
                return null;
            }

            return (int)value.Value;
        }

        private static string Text(int? value)
        {
            return value.HasValue ? Fmt.Integer(value.Value) : "";
        }

        private static void CheckField(List<FieldError> errors, BigInteger? value, int max, StepField field, string maxText)
        {
            if (value.HasValue && BigInteger.Abs(value.Value) > max)
            {
                errors.Add(new FieldError(field, null, maxText));
            }
        }

        // El objetivo para el control por repetición: null si no hay o si su campo ya tiene error.
        private static long? Target(List<FieldError> errors, StepField field, BigInteger? value)
        {
            return value.HasValue && !HasError(errors, field) ? (long)value.Value : (long?)null;
        }

        private void CheckRepetitions(List<FieldError> errors, StepField progressionField, BigInteger? progression, int max, string maxText,
            params long?[] targets)
        {
            if (!progression.HasValue || HasError(errors, progressionField))
            {
                return;
            }

            int? first = null;
            for (var i = 0; i < targets.Length; i++)
            {
                if (!targets[i].HasValue)
                {
                    continue;
                }

                var repetition = StepLimits.FirstRepetitionOutOfRange(targets[i]!.Value, (long)progression.Value, SeriesRepeatCount, max);
                if (repetition.HasValue && (!first.HasValue || repetition.Value < first.Value))
                {
                    first = repetition;
                }
            }

            if (first.HasValue)
            {
                errors.Add(new FieldError(progressionField, first, maxText));
            }
        }

        private static bool HasError(List<FieldError> errors, StepField field)
        {
            for (var i = 0; i < errors.Count; i++)
            {
                if (errors[i].Field == field)
                {
                    return true;
                }
            }

            return false;
        }

        // Los números del formulario, exactos. Duration es null en modo distancia y Meters en modo tiempo (solo cuenta el
        // modo elegido); Split es null si da ≤ 0.
        private readonly struct Values
        {
            public Values(BigInteger? duration, BigInteger? meters, BigInteger? wattsMin, BigInteger? wattsMax, BigInteger? spm,
                BigInteger? split, BigInteger? progressionSpm, BigInteger? progressionWatts, BigInteger? progressionSplit)
            {
                Duration = duration;
                Meters = meters;
                WattsMin = wattsMin;
                WattsMax = wattsMax;
                Spm = spm;
                Split = split;
                ProgressionSpm = progressionSpm;
                ProgressionWatts = progressionWatts;
                ProgressionSplit = progressionSplit;
            }

            public BigInteger? Duration { get; }

            public BigInteger? Meters { get; }

            public BigInteger? WattsMin { get; }

            public BigInteger? WattsMax { get; }

            public BigInteger? Spm { get; }

            public BigInteger? Split { get; }

            public BigInteger? ProgressionSpm { get; }

            public BigInteger? ProgressionWatts { get; }

            public BigInteger? ProgressionSplit { get; }
        }
    }
}
