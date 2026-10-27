using System;
using System.Collections.Generic;

namespace RowMate.Core.Models
{
    // Rutina de entrenamiento (F-RUT-05; legacy: lib/core/models/routine.dart:8-21): nombre (obligatorio), descripción
    // (vacía por defecto), fecha de creación y la PLANTILLA de pasos sin expandir (ninguno por defecto).
    // La rutina expandida sale de FlattenedSteps() y sus posiciones de FlattenedStepPositions() (RoutineExpansion).
    // QA-05: C# 9 no tiene `required`; Name entra por el constructor y Description/Steps tienen inicializador.
    // Los init no aceptan null (como los campos no anulables del legacy).
    public sealed record Routine
    {
        private string _name;
        private string _description = "";
        private IReadOnlyList<IntervalStep> _steps = Array.Empty<IntervalStep>();

        public Routine(string name, DateTimeOffset createdAt)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            CreatedAt = createdAt;
        }

        public long? Id { get; init; }

        public string Name
        {
            get => _name;
            init => _name = value ?? throw new ArgumentNullException(nameof(Name));
        }

        public string Description
        {
            get => _description;
            init => _description = value ?? throw new ArgumentNullException(nameof(Description));
        }

        public DateTimeOffset CreatedAt { get; init; }

        // Plantilla sin expandir, en orden.
        public IReadOnlyList<IntervalStep> Steps
        {
            get => _steps;
            init => _steps = value ?? throw new ArgumentNullException(nameof(Steps));
        }

        // = flattenedSteps (routine.dart:29-70).
        public IReadOnlyList<IntervalStep> FlattenedSteps()
        {
            return RoutineExpansion.Flatten(Steps);
        }

        // = flattenedStepPositions (routine.dart:74-97): alineadas índice por índice con FlattenedSteps().
        public IReadOnlyList<StepPosition> FlattenedStepPositions()
        {
            return RoutineExpansion.Positions(Steps);
        }
    }
}
