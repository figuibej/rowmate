using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El chip de fase de la barra libre (F-WKT-16; AC-WKT-16-03 a -05; legacy immersive_workout_screen.dart:526-547;
    // 04b-hud-exr/02-tech-design §2.7, §3.7; 04-design-notes §3.5, §4.9; DT4b-11, DN4b-15, DN4b-26):
    //   #hud-phase-chip .rm-hud-phase-chip              ANCHO FIJO (PhaseChip.FixedWidthBase: 101,8 base en es, 74,1 en en), alto
    //   │                                               19,6 (3 + 13,6 + 3); relleno 8 × 3; radio 8; tinte @15 % y borde 1 @50 % del
    //   │                                               color base; contenido centrado; flex-shrink 0; sin eventos ni foco
    //   ├── #hud-phase-dot .rm-hud-phase-chip__dot     6, radio 3, phase.running, 4 de aire; solo en EN MARCHA (late: LiveDotMotion)
    //   └── #hud-phase-label .rm-hud-phase-chip__label 11/700 (Inter 700), tracking 0,08 em; el color del texto por código
    // El texto nuevo está desde el cuadro de la fase (AC-WKT-16-03); el cruce de colores (150) y el de la etiqueta (0-80 / 80-200)
    // los hace PhaseChipMotion. El ancho fijo se mide con MeasureTextSize (solo con panel) al cambiar el idioma: el reloj no se
    // corre cuando cambia la fase. FINALIZADO va en phase.finished.hud #FF7A92 (DV-44). Reposo: sin chip (la salida conserva la
    // última foto, DV-41).
    public sealed class PhaseChipView : VisualElement, IDisposable
    {
        public const string UssClass = "rm-hud-phase-chip";
        public const float LabelSizeDp = 11;

        private readonly UiContext _ui;
        private readonly PhaseChipMotion _chipMotion;
        private readonly LiveDotMotion _dotMotion;
        private HudPhaseChip? _chip;
        private HudPhaseChip? _pending;
        private Color _fromBase;
        private Color _fromText;
        private Color _toBase;
        private Color _toText;
        private bool _hasColors;
        private Localizer? _texts;
        private bool _disposed;

        public PhaseChipView(UiContext ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "hud-phase-chip";
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            focusable = false;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.flexShrink = 0;
            style.marginLeft = (float)HudGeometry.PhaseChipGap;
            style.paddingLeft = (float)HudGeometry.PhaseChipPadH - 1;
            style.paddingRight = (float)HudGeometry.PhaseChipPadH - 1;
            style.paddingTop = (float)HudGeometry.PhaseChipPadV - 1;
            style.paddingBottom = (float)HudGeometry.PhaseChipPadV - 1;
            var radius = (float)HudGeometry.PhaseChipRadius;
            style.borderTopLeftRadius = radius;
            style.borderTopRightRadius = radius;
            style.borderBottomLeftRadius = radius;
            style.borderBottomRightRadius = radius;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;

            Dot = new VisualElement { name = "hud-phase-dot" };
            Dot.AddToClassList(UssClass + "__dot");
            Dot.pickingMode = PickingMode.Ignore;
            var dot = (float)HudGeometry.LiveDotSize;
            Dot.style.width = dot;
            Dot.style.height = dot;
            Dot.style.flexShrink = 0;
            Dot.style.marginRight = (float)HudGeometry.LiveDotGap;
            var dotRadius = dot / 2f;
            Dot.style.borderTopLeftRadius = dotRadius;
            Dot.style.borderTopRightRadius = dotRadius;
            Dot.style.borderBottomLeftRadius = dotRadius;
            Dot.style.borderBottomRightRadius = dotRadius;
            Dot.style.backgroundColor = TokenColors.ToColor(Tokens.PhaseRunning);
            Dot.style.display = DisplayStyle.None;
            Add(Dot);

            Label = HudText.Label("hud-phase-label", "rm-type-header-s", LabelSizeDp);
            Label.AddToClassList(UssClass + "__label");
            // 0,08 em en la unidad de TextCore (8), no en dp (QA-V4b-02).
            Label.style.letterSpacing = (float)TypographyUssWriter.LetterSpacingUnits(HudGeometry.PhaseChipTrackingEm);
            Label.style.whiteSpace = WhiteSpace.NoWrap;
            Label.style.flexShrink = 0;
            Add(Label);
            Label.RegisterCallback<GeometryChangedEvent>(_ => Measure());

            _chipMotion = new PhaseChipMotion(ui.Motion, ApplyVisual);
            _dotMotion = new LiveDotMotion(ui.Motion, o => Dot.style.opacity = (float)o);
            RegisterCallback<DetachFromPanelEvent>(_ => StopMotions());
            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            style.display = DisplayStyle.None;
        }

        public VisualElement Dot { get; }

        public Label Label { get; }

        public HudPhaseChip? Chip => _chip;

        public PhaseChipMotion ChipMotion => _chipMotion;

        public LiveDotMotion DotMotion => _dotMotion;

        // El ancho fijo en base (0 hasta que se mide con panel).
        public float FixedWidth { get; private set; }

        // Los textos del catálogo (para el ancho fijo): EN MARCHA, PAUSADO y FINALIZADO en el idioma de la app.
        public void RenderTexts(Localizer texts)
        {
            _texts = texts;
            Measure();
        }

        public void Apply(HudPhaseChip? chip, bool instant)
        {
            if (_disposed)
            {
                return;
            }

            if (panel == null)
            {
                _pending = chip;
                return;
            }

            if (chip == null)
            {
                // Reposo: no pasa con la última foto (la vista deja de aplicar estados antes).
                return;
            }

            var first = _chip == null;
            var changed = first || chip.Text != _chip!.Text || !chip.Base.Equals(_chip.Base);
            _chip = chip;
            style.display = DisplayStyle.Flex;
            Label.text = chip.Text;
            var toBase = TokenColors.ToColor(chip.Base);
            var toText = TokenColors.ToColor(chip.TextColor);
            if (changed)
            {
                _fromBase = _hasColors ? CurrentBase() : toBase;
                _fromText = _hasColors ? CurrentText() : toText;
                _toBase = toBase;
                _toText = toText;
                _hasColors = true;
                _chipMotion.Change(instant || first);
            }

            Dot.style.display = chip.LiveDot ? DisplayStyle.Flex : DisplayStyle.None;
            _dotMotion.SetActive(chip.LiveDot);
            if (!chip.LiveDot)
            {
                Dot.style.opacity = StyleKeyword.Null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopMotions();
        }

        // Los colores que se ven (la mezcla del cruce en curso).
        private float _mix = 1;

        private Color CurrentBase()
        {
            return TokenColors.Lerp(_fromBase, _toBase, _mix);
        }

        private Color CurrentText()
        {
            return TokenColors.Lerp(_fromText, _toText, _mix);
        }

        private void ApplyVisual(ChipVisual visual)
        {
            _mix = (float)visual.ColorMix;
            var baseColor = CurrentBase();
            style.backgroundColor = HudText.WithAlpha(baseColor, (float)HudGeometry.PhaseChipTintAlpha);
            var border = HudText.WithAlpha(baseColor, (float)HudGeometry.PhaseChipBorderAlpha);
            style.borderLeftColor = border;
            style.borderRightColor = border;
            style.borderTopColor = border;
            style.borderBottomColor = border;
            Label.style.color = CurrentText();
            // Una sola etiqueta: el texto nuevo desde el cuadro de la fase; su opacidad sigue el cruce (0-80 baja, 80-200 sube).
            var opacity = visual.NewLabelOpacity > 0 ? visual.NewLabelOpacity : visual.OldLabelOpacity;
            Label.style.opacity = opacity >= 0.999 ? (StyleFloat)StyleKeyword.Null : (float)opacity;
        }

        private void Measure()
        {
            if (_texts == null || panel == null)
            {
                return;
            }

            var active = TextWidth(_texts.Get(L10nKeys.HudPhaseActive));
            var paused = TextWidth(_texts.Get(L10nKeys.HudPhasePaused));
            var finished = TextWidth(_texts.Get(L10nKeys.HudPhaseFinished));
            var width = (float)PhaseChip.FixedWidthBase(active, paused, finished);
            if (width > 0 && Math.Abs(width - FixedWidth) > 0.01f)
            {
                FixedWidth = width;
                style.width = width;
                style.minWidth = width;
            }
        }

        private double TextWidth(string text)
        {
            return Label.MeasureTextSize(text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
        }

        private void OnAttach()
        {
            Measure();
            if (_pending != null)
            {
                var chip = _pending;
                _pending = null;
                Apply(chip, true);
            }
            else if (_chip != null)
            {
                _dotMotion.SetActive(_chip.LiveDot);
            }
        }

        private void StopMotions()
        {
            _chipMotion.Stop();
            _dotMotion.Stop();
        }
    }
}
