using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Core.Text;

namespace RowMate.App.Routines
{
    // El editor de una rutina sin la vista (F-RUT-11, -12, -14; legacy: _RoutineEditorScreenState,
    // lib/features/routines/routine_editor_screen.dart:17-200). Lo crea AppContext.CreateEditor y lo dispone la página.
    // - Name y Description son el texto de los campos SIN recortar (los escribe la vista); se recortan al guardar.
    // - Las operaciones sobre la plantilla son las de RoutineTemplate y reemiten el estado (Pasos (N) en el acto).
    // - SaveAsync (:40-73 + Q-13, Q-19, Q-92), en orden: guardando o cerrado → Ignored; nombre vacío después de recortar →
    //   NameEmpty (aunque tampoco haya pasos); sin pasos → NoSteps; algún paso fuera de los topes (solo con una base tocada
    //   a mano) → StepOutOfRange; si no, guarda con los pasos renumerados 0…n−1 y el nombre y la descripción recortados.
    //   Saved deja el editor CERRADO: todo SaveAsync y toda acción posterior no hacen nada (QA-G3-01; la página ya está
    //   saliendo). Failed lo deja abierto con todo lo escrito (AC-RUT-09-05).
    // - Después de Dispose (el editor cerrado con "Atrás" durante un guardado lento) el guardado ya pedido termina igual en
    //   RoutinesStore, como el `if (mounted) pop()` del legacy, y este store no cambia nada (QA-G3-09).
    public sealed class RoutineEditorStore : IDisposable
    {
        private readonly Routine? _existing;
        private readonly RoutinesStore _routines;
        private readonly SeriesIdGenerator _seriesIds;
        private readonly ObservableValue<RoutineEditorState> _state;
        private IReadOnlyList<IntervalStep> _template;
        private IReadOnlyList<int> _keys;
        private string _name;
        private string _description;
        private int _nextKey;
        private bool _saving;
        private bool _closed;
        private bool _disposed;

        // :17-31. seriesClock = el Scheduler de la app (ids g{ms}_{n}), nunca el reloj de las fechas de creación.
        public RoutineEditorStore(Routine? existing, RoutinesStore routines, IClock seriesClock, IAppLogger logger)
        {
            _routines = routines ?? throw new ArgumentNullException(nameof(routines));
            _seriesIds = new SeriesIdGenerator(seriesClock ?? throw new ArgumentNullException(nameof(seriesClock)));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _existing = existing;
            _name = existing?.Name ?? "";
            _description = existing?.Description ?? "";
            _template = existing?.Steps ?? new IntervalStep[0];
            var keys = new int[_template.Count];
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = _nextKey++;
            }

            _keys = keys;
            _state = new ObservableValue<RoutineEditorState>(BuildState(), logger);
            State = new ReadOnlyObservable<RoutineEditorState>(_state);
        }

        // editorNewRoutine (true) o editorEditRoutine (false): AC-RUT-11-01.
        public bool IsNew => _existing == null;

        // Desde Saved (o Dispose) nada más cambia.
        public bool IsClosed => _closed || _disposed;

        public string Name
        {
            get => _name;
            set
            {
                if (!IsClosed)
                {
                    _name = value ?? "";
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (!IsClosed)
                {
                    _description = value ?? "";
                }
            }
        }

        public IReadOnlyObservable<RoutineEditorState> State { get; }

        // La plantilla actual, en orden.
        public IReadOnlyList<IntervalStep> Template => _template;

        // + Paso: sin progresiones (:75-88).
        public StepDialogForm NewStepForm()
        {
            return StepDialogForm.ForNew(false, 1);
        }

        // + Agregar paso a serie: con progresiones y el ×N de la serie (:135-139).
        public StepDialogForm NewSeriesStepForm(string groupId)
        {
            return StepDialogForm.ForNew(true, RoutineTemplate.RepeatCountOf(_template, groupId));
        }

        // Lápiz: precargado, con progresiones si el paso está en una serie (:90-97).
        public StepDialogForm EditStepForm(int index)
        {
            CheckIndex(index);
            var step = _template[index];
            var inSeries = step.GroupId != null;
            return StepDialogForm.ForEdit(step, inSeries, inSeries ? RoutineTemplate.RepeatCountOf(_template, step.GroupId!) : 1);
        }

        // :80-86: al final, suelto (AC-RUT-12-01).
        public void AddLooseStep(IntervalStep fromDialog)
        {
            if (IsClosed)
            {
                return;
            }

            Apply(RoutineTemplate.AppendLoose(_template, fromDialog), Append(_keys, _nextKey++));
        }

        // :110-133: Trabajo 120 s + Descanso 60 s, ×3, con un id nuevo, al final (AC-RUT-14-01).
        public void AddSeries()
        {
            if (IsClosed)
            {
                return;
            }

            var groupId = _seriesIds.Next(_template);
            var keys = Append(_keys, _nextKey++);
            Apply(RoutineTemplate.AppendDefaultSeries(_template, groupId), Append(keys, _nextKey++));
        }

        // :135-149: después del último paso de la serie, con sus repeticiones (AC-RUT-14-07).
        public void AddStepToSeries(string groupId, IntervalStep fromDialog)
        {
            if (IsClosed)
            {
                return;
            }

            var at = RoutineTemplate.LastIndexOf(_template, groupId) + 1;
            var template = RoutineTemplate.InsertIntoSeries(_template, groupId, fromDialog);
            var keys = new List<int>(_keys);
            keys.Insert(at, _nextKey++);
            Apply(template, keys);
        }

        // :98-106: en su lugar, conservando serie y ×N (AC-RUT-12-03, AC-RUT-14-08). La fila conserva su clave.
        public void ReplaceStep(int index, IntervalStep fromDialog)
        {
            if (IsClosed)
            {
                return;
            }

            Apply(RoutineTemplate.Replace(_template, index, fromDialog), _keys);
        }

        // :190-194: al instante (AC-RUT-12-04). Una serie que se queda sin pasos desaparece (AC-RUT-14-10).
        public void DeleteStep(int index)
        {
            if (IsClosed)
            {
                return;
            }

            CheckIndex(index);
            var keys = new List<int>(_keys);
            keys.RemoveAt(index);
            Apply(RoutineTemplate.RemoveAt(_template, index), keys);
        }

        // :196-200: la serie entera, al instante (AC-RUT-14-09).
        public void DeleteSeries(string groupId)
        {
            if (IsClosed)
            {
                return;
            }

            var keys = new List<int>(_keys.Count);
            for (var i = 0; i < _template.Count; i++)
            {
                if (_template[i].GroupId != groupId)
                {
                    keys.Add(_keys[i]);
                }
            }

            Apply(RoutineTemplate.RemoveSeries(_template, groupId), keys);
        }

        // :153: firstWhere(groupId).groupRepeatCount ?? 1 (lo que precarga el diálogo de repeticiones).
        public int RepeatCountOf(string groupId)
        {
            return RoutineTemplate.RepeatCountOf(_template, groupId);
        }

        // OK del diálogo de repeticiones (:151-188 + Q-92; AC-RUT-14-05, -06), con el texto recortado:
        // no numérico → ×1 (:173 ?? 1); ≤ 0 → sin cambio (:179); > 999 → TooMany; si sube N y con el N nuevo algún objetivo
        // progresado se pasa de su tope → TargetOutOfRange; si no → ×N en todos los pasos de la serie. Bajar N siempre se
        // acepta. Con TooMany y TargetOutOfRange la serie no cambia.
        public RepeatChange ChangeRepeatCount(string groupId, string text)
        {
            var current = RoutineTemplate.RepeatCountOf(_template, groupId);
            if (IsClosed)
            {
                return new RepeatChange(RepeatChangeKind.Unchanged, current);
            }

            var value = DartIntParser.TryParse(DartText.Trim(text ?? "")) ?? BigInteger.One;
            if (value.Sign <= 0)
            {
                return new RepeatChange(RepeatChangeKind.Unchanged, current);
            }

            if (value > StepLimits.MaxRepeatCount)
            {
                return new RepeatChange(RepeatChangeKind.TooMany, StepLimits.MaxRepeatCount);
            }

            var count = (int)value;
            if (count > current && !SeriesFits(groupId, count))
            {
                return new RepeatChange(RepeatChangeKind.TargetOutOfRange, count);
            }

            if (NeedsRepeatCount(groupId, count))
            {
                Apply(RoutineTemplate.SetRepeatCount(_template, groupId, count), _keys);
            }

            return new RepeatChange(RepeatChangeKind.Applied, count);
        }

        public async Task<SaveOutcome> SaveAsync()
        {
            if (IsClosed || _saving)
            {
                return SaveOutcome.Ignored;
            }

            var name = DartText.Trim(_name);
            if (name.Length == 0)
            {
                return SaveOutcome.NameEmpty;
            }

            if (_template.Count == 0)
            {
                return SaveOutcome.NoSteps;
            }

            if (!StepLimitCheck.TemplateIsWithinLimits(_template))
            {
                return SaveOutcome.StepOutOfRange;
            }

            _saving = true;
            Publish();
            var description = DartText.Trim(_description);
            var steps = RoutineTemplate.Renumber(_template, _existing?.Id ?? 0);
            Routine? saved;
            try
            {
                saved = _existing == null
                    ? await _routines.CreateAsync(name, description, steps)
                    : await _routines.UpdateAsync(_existing with { Name = name, Description = description, Steps = steps });
            }
            catch (Exception)
            {
                // Un error que no se espera (RoutinesStore ya captura las fallas de la base): el editor no queda trabado.
                if (!_disposed)
                {
                    _saving = false;
                    Publish();
                }

                throw;
            }

            if (_disposed)
            {
                return saved == null ? SaveOutcome.Failed : SaveOutcome.Saved;
            }

            if (saved == null)
            {
                _saving = false;
                Publish();
                return SaveOutcome.Failed;
            }

            _closed = true;
            return SaveOutcome.Saved;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        // Con ×count, cada paso de la serie dentro de los topes en cada repetición (AC-RUT-14-06).
        private bool SeriesFits(string groupId, int count)
        {
            for (var i = 0; i < _template.Count; i++)
            {
                if (_template[i].GroupId == groupId && !StepLimitCheck.RepetitionsWithinLimits(_template[i], count))
                {
                    return false;
                }
            }

            return true;
        }

        private bool NeedsRepeatCount(string groupId, int count)
        {
            for (var i = 0; i < _template.Count; i++)
            {
                if (_template[i].GroupId == groupId && _template[i].GroupRepeatCount != count)
                {
                    return true;
                }
            }

            return false;
        }

        private void Apply(IReadOnlyList<IntervalStep> template, IReadOnlyList<int> keys)
        {
            _template = template;
            _keys = keys;
            Publish();
        }

        private void Publish()
        {
            if (!_disposed)
            {
                _state.Value = BuildState();
            }
        }

        private RoutineEditorState BuildState()
        {
            var steps = new EditorStep[_template.Count];
            for (var i = 0; i < steps.Length; i++)
            {
                steps[i] = new EditorStep(_keys[i], _template[i]);
            }

            return new RoutineEditorState(steps, EditorLayout.Build(_template), _template.Count, _saving);
        }

        private void CheckIndex(int index)
        {
            if (index < 0 || index >= _template.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "RoutineEditorStore: índice fuera de la plantilla.");
            }
        }

        private static IReadOnlyList<int> Append(IReadOnlyList<int> keys, int key)
        {
            var result = new List<int>(keys.Count + 1);
            result.AddRange(keys);
            result.Add(key);
            return result;
        }
    }
}
