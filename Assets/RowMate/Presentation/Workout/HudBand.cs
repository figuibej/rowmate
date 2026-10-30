using System;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La banda tiempo │ distancia (AC-WKT-17-04; legacy immersive_workout_screen.dart:669-706, :839-887; 02-tech-design §6.7 de
    // la It. 4a; 04-design-notes §3.4):
    //   #hud-band .rm-hud-band .rm-hud-glass       cápsula (r.full), borde 1 @10 %, relleno vertical 10
    //   ├── #hud-time .rm-hud-band__column          timer 16 (metric.time @70 %) centrado arriba · 2 · num.band 34 metric.time.hud
    //   ├── #hud-band-divider .rm-hud-band__divider 1 × 32, blanco @12 %, flex-shrink 0 (QA-V4a-03)
    //   └── #hud-distance .rm-hud-band__column      straighten 16 (metric.distance @70 %) · 2 · [num.band metric.distance.hud
    //                                               + 3 + hudUnitMeters 13/500 en .hud @80 %, en la línea de base] (DV-31)
    // It. 4b (M2, 04b-hud-exr/02-tech-design §3.11): la m es el SUFIJO de la cifra de la distancia (la dibuja el mismo
    // TabularNumber, Inter 500 en su línea de base): salen la fila .rm-hud-band__row y el Label de la unidad.
    // En vertical como el legacy: relleno 20 a los lados y 16 a cada lado del divisor. En horizontal, COMPACTA (DV-33): 14 y 10.
    // Las dos cifras comparten UNA escala (HudLayout.ValueScale(Band), DN4a-01): la banda se lee pareja; el relleno, el
    // divisor, los íconos y la unidad no se achican. Es la zona: HudZones la ancla abajo a la izquierda y la escala por s.
    public sealed class HudBand : VisualElement
    {
        public const string UssClass = "rm-hud-band";
        public const float ValueSizeDp = 34;
        public const float IconSizeDp = 16;
        public const float IconGapDp = 2;
        public const float UnitGapDp = 3;
        public const float UnitSizeDp = 13;
        public const float DividerHeightDp = 32;
        public const float IconAlpha = 0.70f;
        public const float DividerAlpha = 0.12f;
        public const float BorderAlpha = 0.10f;

        private readonly VisualElement _divider;
        private string _unitText = "";
        private float _fit = 1;
        private bool? _compact;

        public HudBand(UiContext ui)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            name = "hud-band";
            AddToClassList(UssClass);
            AddToClassList("rm-hud-glass");
            pickingMode = PickingMode.Ignore;
            var border = HudText.WithAlpha(TokenColors.ToColor(Tokens.TextPrimary), BorderAlpha);
            style.borderLeftColor = border;
            style.borderRightColor = border;
            style.borderTopColor = border;
            style.borderBottomColor = border;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.paddingTop = (float)HudGeometry.BandPadV - 1;
            style.paddingBottom = (float)HudGeometry.BandPadV - 1;

            TimeColumn = Column("hud-time");
            TimeIcon = Icon(Icons.Timer, Tokens.MetricTime);
            TimeColumn.Add(TimeIcon);
            Time = Number(ui, "hud-time-value", MetricKind.Time);
            TimeColumn.Add(Time);
            Add(TimeColumn);

            _divider = new VisualElement { name = "hud-band-divider" };
            _divider.AddToClassList(UssClass + "__divider");
            _divider.pickingMode = PickingMode.Ignore;
            _divider.style.width = 1;
            _divider.style.height = DividerHeightDp;
            // QA-V4a-03 (AC-WKT-17-04): el divisor no cede su ancho. La banda mide lo que calcula HudGeometry con los anchos
            // naturales de las cifras, y TabularNumber redondea su caja hacia arriba a píxeles: cuando el contenido quedaba
            // apenas más ancho que la banda, el divisor (el único hijo que se podía achicar) se iba a 0 y no se veía en TV,
            // TH y L640. Con flex-shrink 0 esa fracción de píxel cae en el relleno de la derecha (20 o 14 dp).
            _divider.style.flexShrink = 0;
            _divider.style.backgroundColor = HudText.WithAlpha(TokenColors.ToColor(Tokens.TextPrimary), DividerAlpha);
            Add(_divider);

            DistanceColumn = Column("hud-distance");
            DistanceIcon = Icon(Icons.Straighten, Tokens.MetricDistance);
            DistanceColumn.Add(DistanceIcon);
            Distance = Number(ui, "hud-distance-value", MetricKind.Distance);
            DistanceColumn.Add(Distance);
            Add(DistanceColumn);

            SetCompact(false);
            ApplyFit(1);
        }

        public VisualElement TimeColumn { get; }

        public VisualElement DistanceColumn { get; }

        public IconView TimeIcon { get; }

        public IconView DistanceIcon { get; }

        public TabularNumber Time { get; }

        public TabularNumber Distance { get; }

        // M2: la unidad (la m) es el sufijo de la distancia.
        public string UnitText => Distance.SuffixText;

        // El divisor de 1 × 32 entre las dos columnas (para las pruebas de AC-WKT-17-04).
        public VisualElement Divider => _divider;

        public float Fit => _fit;

        public bool IsCompact => _compact == true;

        // Anchos naturales a tamaño base de las cifras (la distancia con 3 + "m"), para HudGeometry.
        public double NaturalTimeWidth => HudText.BaseWidth(Time, _fit);

        public double NaturalDistanceWidth => HudText.BaseBoxWidth(Distance, _fit);

        public void SetUnit(string text)
        {
            if (_unitText != text)
            {
                _unitText = text;
                ApplyUnit();
            }
        }

        // M2: la m como sufijo de la distancia, a la escala de las cifras, en su línea de base.
        private void ApplyUnit()
        {
            var color = HudText.WithAlpha(TokenColors.ToColor(Tokens.MetricDistanceHud), (float)HudGeometry.UnitAlpha);
            Distance.SetSuffix(_unitText, "rm-type-caption", UnitSizeDp * _fit, color, UnitGapDp * _fit);
        }

        public void SetValues(string clock, int clockValue, string distance, int distanceValue, bool instant)
        {
            Time.SetValue(clock, clockValue, !instant);
            Distance.SetValue(distance, distanceValue, !instant);
        }

        // Compacta en horizontal (relleno 14 y 10 a cada lado del divisor) o la del legacy en vertical (20 y 16).
        public void SetCompact(bool compact)
        {
            if (_compact == compact)
            {
                return;
            }

            _compact = compact;
            EnableInClassList(UssClass + "--compact", compact);
            var padH = (float)(compact ? HudGeometry.BandPadHLandscape : HudGeometry.BandPadHPortrait) - 1;
            var gap = (float)(compact ? HudGeometry.BandDividerGapLandscape : HudGeometry.BandDividerGapPortrait);
            style.paddingLeft = padH;
            style.paddingRight = padH;
            _divider.style.marginLeft = gap;
            _divider.style.marginRight = gap;
        }

        // La escala compartida de las dos cifras (y su unidad); el alto de la banda no cambia.
        public void ApplyFit(double fit)
        {
            var f = (float)(fit > 0 ? Math.Min(1.0, fit) : 0.01);
            _fit = f;
            var size = ValueSizeDp * f;
            var spare = (ValueSizeDp - size) / 2f;
            foreach (var number in new[] { Time, Distance })
            {
                number.SetFontSize(size);
                number.style.marginBottom = spare;
            }

            Time.style.marginTop = IconGapDp + spare;
            Distance.style.marginTop = IconGapDp + spare;
            ApplyUnit();
        }

        private static VisualElement Column(string elementName)
        {
            var column = new VisualElement { name = elementName };
            column.AddToClassList(UssClass + "__column");
            column.pickingMode = PickingMode.Ignore;
            return column;
        }

        private static IconView Icon(string codepoint, ColorToken token)
        {
            var icon = new IconView(codepoint, false, IconSizeDp);
            icon.AddToClassList(UssClass + "__icon");
            icon.pickingMode = PickingMode.Ignore;
            icon.style.color = HudText.WithAlpha(TokenColors.ToColor(token), IconAlpha);
            return icon;
        }

        private static TabularNumber Number(UiContext ui, string elementName, MetricKind kind)
        {
            var number = new TabularNumber(ui.Motion) { name = elementName };
            number.AddToClassList("rm-type-num-band");
            number.AddToClassList(UssClass + "__number");
            number.style.fontSize = ValueSizeDp;
            number.style.color = TokenColors.ToColor(MetricPalette.HudFor(kind));
            return number;
        }
    }
}
