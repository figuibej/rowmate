using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RowMate.Core.Design;
using RowMate.Core.Localization;

namespace RowMate.Core.Models
{
    // Datos de presentación de cada tipo de paso (F-RUT-01 + F-APP-07), sin UI:
    // - orden del selector (AC-RUT-01-01);
    // - clave del nombre en el catálogo (AC-RUT-01-02; legacy: lib/shared/theme.dart:42-53, stepTypeLocalized);
    // - nombre del ícono de Material (AC-RUT-01-04, dato; legacy: routine_editor_screen.dart:447-458). El codepoint
    //   llega con Icons.cs en la It. 2 (D-09); el de descanso es la variante sin relleno (P-07);
    // - color de la fuente única (AC-RUT-01-03, AC-BASE-04; legacy: theme.dart:56-69, stepColor).
    public static class StepTypeInfo
    {
        private static readonly ReadOnlyCollection<StepType> Order = new ReadOnlyCollection<StepType>(new[]
        {
            StepType.Warmup, StepType.Work, StepType.Rest, StepType.Cooldown,
        });

        public static IReadOnlyList<StepType> SelectorOrder => Order;

        public static string NameKey(StepType type)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return L10nKeys.StepTypeWarmup;
                case StepType.Work:
                    return L10nKeys.StepTypeWork;
                case StepType.Rest:
                    return L10nKeys.StepTypeRest;
                case StepType.Cooldown:
                    return L10nKeys.StepTypeCooldown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de paso desconocido.");
            }
        }

        public static string IconName(StepType type)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return "whatshot";
                case StepType.Work:
                    return "fitness_center";
                case StepType.Rest:
                    return "pause_circle_outline";
                case StepType.Cooldown:
                    return "ac_unit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de paso desconocido.");
            }
        }

        // La misma instancia de Tokens (nunca una copia ni un hex suelto).
        public static ColorToken Color(StepType type)
        {
            return ColorForStorageName(StepTypeNames.ToStorage(type));
        }

        // Como stepColor(String) del legacy: por nombre guardado; cualquier otro valor (vacío, "sprint", un punto de
        // telemetría sin tipo) → step.unknown.
        public static ColorToken ColorForStorageName(string? name)
        {
            switch (name)
            {
                case StepTypeNames.Work:
                    return Tokens.StepWork;
                case StepTypeNames.Rest:
                    return Tokens.StepRest;
                case StepTypeNames.Warmup:
                    return Tokens.StepWarmup;
                case StepTypeNames.Cooldown:
                    return Tokens.StepCooldown;
                default:
                    return Tokens.StepUnknown;
            }
        }
    }
}
