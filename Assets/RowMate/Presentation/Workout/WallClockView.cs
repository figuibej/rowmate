using System;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El reloj de pared del HUD (F-WKT-19; AC-WKT-19-01 a -05; legacy series_panels.dart:57-110; 04b-hud-exr/02-tech-design §2.8,
    // §3.7; 04-design-notes §3.4; DT4b-08, M7):
    //   #hud-wall-clock .rm-hud-wall-clock    UN TabularNumber num.s 20/700 blanco (el tamaño de la cuenta, P-07) con el ícono
    //                                          schedule de 16 al 70 % como PREFIJO (fuente de íconos), a 5, con la tinta centrada en
    //                                          la banda de las mayúsculas de las cifras: 69,1 base, siempre igual (cifras tabulares)
    // Lo mueve el WallClockTicker del store del HUD (su propio ritmo; la hora del equipo, no la de la sesión). Cambia SOLO con el
    // minuto, con el rodillo del HUD (180, hacia arriba; a medianoche, hacia abajo: el valor es el minuto del día); con Instant
    // (cambio de zona o de hora a mano, despertar, vuelta del segundo plano, el primer estado), sin rodillo. Sin marco, sin
    // toques y sin foco. Va en la barra de pasos (a la derecha de la cuenta) y en la libre (antes del chip).
    public sealed class WallClockView
    {
        public const float SizeDp = 20;

        public WallClockView(UiContext ui, IReadOnlyObservable<WallClockState> clock)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            Number = new TabularNumber(ui.Motion) { name = "hud-wall-clock" };
            Number.AddToClassList("rm-type-num-s");
            Number.AddToClassList("rm-hud-wall-clock");
            Number.SetFontSize(SizeDp);
            Number.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Number.style.flexShrink = 0;
            Number.focusable = false;
            ApplyPrefix((float)HudGeometry.WallClockIconGap);
            Number.Bind(clock, Apply);
        }

        public TabularNumber Number { get; }

        // It. 5 (DD5-01): el aire del ícono a sus cifras (5; 3 en el modo compacto de la barra libre).
        public float IconGapDp { get; private set; } = (float)HudGeometry.WallClockIconGap;

        // El modo compacto de la barra libre (InfoRowFit.FreeCompact): el ícono a 3 de sus cifras. Solo lo pide FreeTopBar; el
        // reloj de la barra de pasos sigue a 5 (AC-WKT-19-01).
        public void SetCompact(bool compact)
        {
            var gap = (float)(compact ? InfoRowFit.FreeCompactClockIconGap : HudGeometry.WallClockIconGap);
            if (Math.Abs(gap - IconGapDp) > 0.001f)
            {
                ApplyPrefix(gap);
            }
        }

        private void ApplyPrefix(float gap)
        {
            IconGapDp = gap;
            Number.SetPrefix(Icons.Schedule, "rm-icon", (float)HudGeometry.WallClockIcon,
                HudText.WithAlpha(TokenColors.ToColor(Tokens.TextPrimary), (float)HudGeometry.WallClockIconAlpha), gap, AffixAlign.CapCenter);
        }

        public string Text => Number.Text;

        public string Icon => Number.PrefixText;

        // La última foto (O-01): la salida conserva la hora que mostraba.
        public bool IsFrozen { get; private set; }

        public void Freeze()
        {
            IsFrozen = true;
        }

        private void Apply(WallClockState state)
        {
            if (IsFrozen)
            {
                return;
            }

            Number.SetValue(state.Text, state.MinuteOfDay, !state.Instant);
        }
    }
}
