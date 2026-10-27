using System;

namespace RowMate.Core.Models
{
    // Nombres persistidos de los tipos de paso: los de type.name del legacy (interval_step.dart:115), en minúsculas.
    // Parse = StepType.values.firstWhere((e) => e.name == map['type']) (interval_step.dart:133): comparación exacta y
    // SIN tipo por defecto; un nombre desconocido (o vacío, o null) es un error (AC-RUT-01-05, D-14: FormatException).
    public static class StepTypeNames
    {
        public const string Warmup = "warmup";
        public const string Work = "work";
        public const string Rest = "rest";
        public const string Cooldown = "cooldown";

        public static string ToStorage(StepType type)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return Warmup;
                case StepType.Work:
                    return Work;
                case StepType.Rest:
                    return Rest;
                case StepType.Cooldown:
                    return Cooldown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de paso desconocido.");
            }
        }

        public static StepType Parse(string name)
        {
            if (TryParse(name, out var type))
            {
                return type;
            }

            throw new FormatException(name == null
                ? "Tipo de paso vacío (null): no hay tipo por defecto."
                : "Tipo de paso desconocido \"" + name + "\": se esperaba warmup, work, rest o cooldown.");
        }

        public static bool TryParse(string? name, out StepType type)
        {
            switch (name)
            {
                case Warmup:
                    type = StepType.Warmup;
                    return true;
                case Work:
                    type = StepType.Work;
                    return true;
                case Rest:
                    type = StepType.Rest;
                    return true;
                case Cooldown:
                    type = StepType.Cooldown;
                    return true;
                default:
                    type = StepType.Warmup;
                    return false;
            }
        }
    }
}
