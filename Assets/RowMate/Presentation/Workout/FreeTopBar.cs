using System;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La barra superior del entrenamiento libre (AC-WKT-16-01 a -06, AC-WKT-19-01 a -05; legacy immersive_workout_screen.dart:
    // 491-547; 02-tech-design §6.6 de la It. 4a; 04b-hud-exr/02-tech-design §2.15, §3.7; 04-design-notes §3.3 a §3.5;
    // DN4a-04, DN4b-26, DN4b-27; DV-47):
    //   #hud-topbar .rm-hud-topbar .rm-hud-topbar--free .rm-hud-glass   alto 40; relleno 14 × 8; borde 1 border.subtle; en fila
    //   ├── #hud-free-title                rowing 20 en accent.primary, 8, hudFreeTitle 16/600 blanco; flex-shrink 1: el título
    //   │                                  se ACHICA hasta 0,8 (escala pareja desde su borde izquierdo, InfoRowFit.FreeTitleScale)
    //   │                                  y solo por debajo de eso se recorta con "…" (en TV y 360 en español entra a 0,818)
    //   ├── #hud-wall-clock                margin-left: auto (al menos 2 del título); el reloj de pared (WallClockView, el mismo que en la barra de pasos)
    //   └── #hud-phase-chip                margin-left 10; ANCHO FIJO (PhaseChipView)
    // La misma caja de 40 que la de la 4a: SPM queda a la misma distancia. Sin eventos. Nada se encima: el título es lo único que
    // cede; el reloj y el chip tienen flex-shrink 0. La escala se mide solo con panel y cambia en el acto con la ventana o el
    // idioma (nunca layout por cuadro). Es la zona: HudZones la ancla arriba a la izquierda y la escala por s.
    public sealed class FreeTopBar : VisualElement, IDisposable
    {
        public const float IconSizeDp = 20;
        public const float TitleSizeDp = 16;
        public const float IconGapDp = 8;

        // El aire mínimo entre el título y el reloj (InfoRowFit.FreeTitleClockGap, 2): sin él, el título achicado quedaba a ≈ 3 dp
        // del ícono del reloj en TV en español. Con 6 (B-14, calibrado con el chip angosto de QA-V4b-02) el título ya no entra a 0,8
        // con el chip a su ancho y se recortaba.
        public const float TitleClockGapDp = (float)InfoRowFit.FreeTitleClockGap;

        private float _appliedScale = -1;
        private float _appliedWidth = -2;

        public FreeTopBar(UiContext ui, IReadOnlyObservable<WallClockState> clock)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            name = "hud-topbar";
            AddToClassList("rm-hud-topbar");
            AddToClassList("rm-hud-topbar--free");
            AddToClassList("rm-hud-glass");
            pickingMode = PickingMode.Ignore;
            HudTopBars.ApplyFrame(this, HudGeometry.FreeRowPadH);

            Row = new VisualElement { name = "hud-free-title" };
            Row.AddToClassList("rm-hud-free-title");
            Row.pickingMode = PickingMode.Ignore;
            Row.style.flexShrink = 1;
            Row.style.minWidth = 0;
            Icon = new IconView(Icons.Rowing, false, IconSizeDp);
            Icon.AddToClassList("rm-hud-free-title__icon");
            Icon.style.color = TokenColors.ToColor(Tokens.AccentPrimary);
            Row.Add(Icon);
            Title = HudText.Label("hud-free-title-text", "rm-type-title-s", TitleSizeDp);
            Title.AddToClassList("rm-hud-free-title__text");
            Title.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Title.style.whiteSpace = WhiteSpace.NoWrap;
            Title.style.flexShrink = 0;
            Title.style.transformOrigin = new TransformOrigin(0, new Length(50, LengthUnit.Percent));
            Row.Add(Title);
            Add(Row);

            Clock = new WallClockView(ui, clock);
            Clock.Number.style.marginLeft = StyleKeyword.Auto;
            Add(Clock.Number);

            Chip = new PhaseChipView(ui);
            Add(Chip);

            RegisterCallback<GeometryChangedEvent>(_ => Fit());
            Chip.RegisterCallback<GeometryChangedEvent>(_ => Fit());
            Clock.Number.RegisterCallback<GeometryChangedEvent>(_ => Fit());
        }

        public VisualElement Row { get; }

        public IconView Icon { get; }

        public Label Title { get; }

        public WallClockView Clock { get; }

        public PhaseChipView Chip { get; }

        // La escala del título (1 a su tamaño; 0,8 como mínimo) y si se recorta con "…" (solo por debajo de 0,8).
        public float TitleScale => _appliedScale < 0 ? 1 : _appliedScale;

        public bool TitleTruncated { get; private set; }

        public void RenderTexts(Localizer texts)
        {
            Title.text = texts.Get(L10nKeys.HudFreeTitle);
            Chip.RenderTexts(texts);
            Fit();
        }

        public void Apply(WorkoutHudState state, bool instant)
        {
            Chip.Apply(state.Chip, instant);
        }

        public void Dispose()
        {
            Chip.Dispose();
        }

        // AC-WKT-16-06 (DN4b-27): primero se achica el título (escala pareja desde su borde izquierdo, con un margen negativo que
        // le devuelve al layout lo que no se ve); solo por debajo de 0,8 se recorta con "…".
        private void Fit()
        {
            if (panel == null || string.IsNullOrEmpty(Title.text))
            {
                return;
            }

            var available = contentRect.width;
            if (!(available > 0))
            {
                return;
            }

            var title = Title.MeasureTextSize(Title.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
            var others = IconSizeDp + IconGapDp + TitleClockGapDp + Clock.Number.NaturalBoxWidth + (float)HudGeometry.PhaseChipGap + Chip.FixedWidth;
            var scale = (float)InfoRowFit.FreeTitleScale(available, title, others);
            var truncates = InfoRowFit.FreeTitleTruncates(available, title, others);
            var width = truncates ? Math.Max(0, (available - others) / scale) : -1;
            if (Math.Abs(scale - _appliedScale) < 0.0001f && Math.Abs(width - _appliedWidth) < 0.01f && truncates == TitleTruncated)
            {
                return;
            }

            _appliedScale = scale;
            _appliedWidth = width;
            TitleTruncated = truncates;
            Title.style.scale = Math.Abs(scale - 1) < 0.0001f ? (StyleScale)StyleKeyword.Null : new Scale(new Vector3(scale, scale, 1));
            if (truncates)
            {
                Title.style.width = width;
                Title.style.overflow = Overflow.Hidden;
                Title.style.textOverflow = TextOverflow.Ellipsis;
                Title.style.marginRight = -width * (1 - scale);
            }
            else
            {
                Title.style.width = StyleKeyword.Null;
                Title.style.overflow = StyleKeyword.Null;
                Title.style.textOverflow = StyleKeyword.Null;
                Title.style.marginRight = -title * (1 - scale);
            }
        }
    }

    // El marco común de las dos barras superiores (DN4a-04): vidrio, borde 1 border.subtle (dentro del relleno), radio 14 y
    // alto 40 = 1 + 8 + 22 + 8 + 1. It. 4b: la barra de pasos es una columna de tres filas sin relleno (cada fila lleva el suyo),
    // de 52 (74 con el sello).
    internal static class HudTopBars
    {
        public static void ApplyFrame(VisualElement bar, double padH)
        {
            ApplyBorder(bar);
            bar.style.height = (float)HudGeometry.TopBarHeight;
            bar.style.paddingLeft = (float)padH;
            bar.style.paddingRight = (float)padH;
            bar.style.paddingTop = (float)HudGeometry.TopBarPadV;
            bar.style.paddingBottom = (float)HudGeometry.TopBarPadV;
        }

        public static void ApplyBorder(VisualElement bar)
        {
            var border = TokenColors.ToColor(Tokens.BorderSubtle);
            bar.style.borderLeftColor = border;
            bar.style.borderRightColor = border;
            bar.style.borderTopColor = border;
            bar.style.borderBottomColor = border;
            bar.style.borderLeftWidth = 1;
            bar.style.borderRightWidth = 1;
            bar.style.borderTopWidth = 1;
            bar.style.borderBottomWidth = 1;
        }
    }
}
