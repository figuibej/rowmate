using System;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Routines;

namespace RowMate.App.Routines
{
    // Lo que dibuja la fila de un paso en el editor y en la vista previa del diálogo (AC-RUT-12-02, AC-RUT-13-16;
    // legacy: _buildStepTile, lib/features/routines/routine_editor_screen.dart:316-369, :447-458):
    // - Title: el nombre del tipo del catálogo; Subtitle: StepSubtitle (el texto del legacy, con el 📈 para el ícono).
    // - Icon: el glifo del tipo (AC-RUT-01-04: whatshot, fitness_center, pause_circle en contorno, ac_unit).
    // - Colores (DV-12 ampliada, DN3-03): título e ícono en la variante legible step.<tipo>.hud; el borde de 3 dp y el fondo
    //   del cuadrado (que la vista pone al 20 %) en el color base del tipo.
    public sealed record StepRowModel(StepType Type, string Title, StepSubtitle Subtitle, string Icon, ColorToken TitleColor,
                                      ColorToken IconColor, ColorToken BorderColor, ColorToken BadgeColor)
    {
        public static StepRowModel For(IntervalStep step, Localizer l10n)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            var hud = HudColor(step.Type);
            var color = StepTypeInfo.Color(step.Type);
            return new StepRowModel(step.Type, l10n.Get(StepTypeInfo.NameKey(step.Type)), StepSubtitle.For(step, l10n), IconFor(step.Type),
                hud, hud, color, color);
        }

        // :447-458 con los glifos de Icons (descanso en contorno, P-07 de la It. 1).
        public static string IconFor(StepType type)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return Icons.Whatshot;
                case StepType.Work:
                    return Icons.FitnessCenter;
                case StepType.Rest:
                    return Icons.PauseCircle;
                case StepType.Cooldown:
                    return Icons.AcUnit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de paso desconocido.");
            }
        }

        // La variante legible del color del tipo (step.<tipo>.hud; DN3-03).
        public static ColorToken HudColor(StepType type)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return Tokens.StepWarmupHud;
                case StepType.Work:
                    return Tokens.StepWorkHud;
                case StepType.Rest:
                    return Tokens.StepRestHud;
                case StepType.Cooldown:
                    return Tokens.StepCooldownHud;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de paso desconocido.");
            }
        }
    }
}
