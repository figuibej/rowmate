using System;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Aplica la geometría del HUD (02-tech-design §6.4 de la It. 4a; ART_BIBLE §5.2-§5.3; 04-design-notes §3; DN4a-13): mide
    // los anchos naturales a tamaño BASE de lo que va en cada zona (las cifras, los rótulos y los obj.), llama a la función pura
    // HudGeometry.Layout y pone, por zona, el ancla × s, la escala s con el origen en el ancla (DT4a-03: cada zona se arma a
    // tamaño base y escala entera, también sus bordes de 1 dp) y la escala de sus cifras (los topes T1 a T5). Recalcula SOLO
    // con un cambio de la ventana o de un ancho natural o de lo que hay (BPM, objetivos): los topes no cambian los anchos
    // naturales (achican las cifras a partir de la base), así que no hay vuelta. La decisión de la fila de abajo, de los mínimos
    // de kcal y BPM y del lugar de la llave es por ventana: ninguna tarjeta cambia de modo con un dato (T4).
    // El resultado (Layout) lo usan la página (la llave del simulador, el obstáculo de los controles), las pruebas y el arnés.
    public sealed class HudZones
    {
        private readonly HudView _view;
        private readonly SafeInsetsSource _insets;
        private readonly bool _reserveWrench;
        private bool _applying;
        private bool _dirty = true;
        private double _lastWidth;
        private double _lastHeight;
        private HudNaturalWidths _lastNatural;
        private (bool Bpm, bool Spm, bool Split, bool Watts) _lastContent;

        public HudZones(HudView view, SafeInsetsSource insets, bool reserveWrench)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _insets = insets ?? throw new ArgumentNullException(nameof(insets));
            _reserveWrench = reserveWrench;
        }

        public HudLayout? Layout { get; private set; }

        // Cambió el lugar de la llave, el techo del panel o el rectángulo de los controles (la página vuelve a declararlos).
        public event Action<HudLayout>? Changed;

        public void Invalidate()
        {
            _dirty = true;
        }

        // Vuelve a calcular si algo de lo que entra cambió. Devuelve true si aplicó una disposición nueva.
        public bool Update()
        {
            if (_applying)
            {
                return false;
            }

            var size = _view.WindowSize;
            if (!(size.x > 0) || !(size.y > 0))
            {
                return false;
            }

            var natural = _view.MeasureNatural();
            var content = (_view.HasBpm, _view.SpmHasTarget, _view.SplitHasTarget, _view.WattsHasTarget);
            if (!_dirty && Layout != null && Math.Abs(size.x - _lastWidth) < 0.01 && Math.Abs(size.y - _lastHeight) < 0.01
                && Same(natural, _lastNatural) && content == _lastContent)
            {
                return false;
            }

            _dirty = false;
            _lastWidth = size.x;
            _lastHeight = size.y;
            _lastNatural = natural;
            _lastContent = content;
            var input = new HudLayoutInput(size.x, size.y, _insets(), _reserveWrench, content.Item1, content.Item2, content.Item3, content.Item4,
                natural);
            var layout = HudGeometry.Layout(in input);
            _applying = true;
            try
            {
                Apply(layout);
            }
            finally
            {
                _applying = false;
            }

            Layout = layout;
            Changed?.Invoke(layout);
            return true;
        }

        private void Apply(HudLayout layout)
        {
            var s = layout.Scale;
            var w = layout.WindowWidth;
            var h = layout.WindowHeight;
            _view.Backdrop.SetHorizon(layout.Horizon);

            Place(_view.TopBar, layout.TopBarZone, w, h, s);
            _view.TopBar.style.width = (float)layout.TopBarZone.MaxBaseWidth;

            Place(_view.Spm, layout.SpmZone, w, h, s);
            _view.Spm.ApplyFit(layout.ValueScale(HudValue.Spm));

            Place(_view.RightZone, layout.RightZone, w, h, s);
            _view.Split.ApplyFit(layout.ValueScale(HudValue.Split));
            _view.Watts.ApplyFit(layout.ValueScale(HudValue.Watts));

            Place(_view.VitalsZone, layout.VitalsZone, w, h, s);
            _view.SetVitalsArrangement(layout.VitalsInRow, layout.VitalsInRow ? layout.VitalsMinWidth : true);
            var vitalsFit = layout.ValueScale(HudValue.Kcal);
            _view.Kcal.ApplyFit(vitalsFit);
            _view.Bpm.ApplyFit(vitalsFit);

            Place(_view.Band, layout.BandZone, w, h, s);
            _view.Band.SetCompact(layout.BandCompact);
            _view.Band.ApplyFit(layout.ValueScale(HudValue.Band));

            Place(_view.Controls, layout.ControlsZone, w, h, s);

            // Cada tarjeta y la banda miden lo que calculó HudGeometry (en dp base: el rectángulo / s). Así la vista es la
            // geometría probada en dotnet (zonas sin cruces, la zona del bote libre) y no la suma de redondeos de los textos, que
            // con las cifras achicadas daba hasta 2 dp de más (S-B4a). Lo que adentro mida un poco más cae en el relleno.
            // Las tarjetas lo toman con ApplyBaseSize: en el acto, salvo durante su cambio de paso (crecen con su hueco y su obj.,
            // 04-design-notes §5.5).
            SizeCard(_view.Spm, layout.Spm, s);
            SizeCard(_view.Split, layout.Split, s);
            SizeCard(_view.Watts, layout.Watts, s);
            SizeCard(_view.Kcal, layout.Kcal, s);
            if (layout.Bpm.HasValue)
            {
                SizeCard(_view.Bpm, layout.Bpm.Value, s);
            }

            Size(_view.Band, layout.Band, s);
            // La banda es una cápsula: radio = medio alto (un radio enorme en USS lo recorta UI Toolkit por eje y sale una elipse).
            if (s > 0)
            {
                var radius = (float)(layout.Band.Height / s / 2);
                _view.Band.style.borderTopLeftRadius = radius;
                _view.Band.style.borderTopRightRadius = radius;
                _view.Band.style.borderBottomLeftRadius = radius;
                _view.Band.style.borderBottomRightRadius = radius;
            }

            var boat = _view.BoatZone;
            if (boat != null)
            {
                var zone = layout.BoatZone;
                boat.style.left = (float)zone.Left;
                boat.style.top = (float)zone.Top;
                boat.style.width = (float)zone.Width;
                boat.style.height = (float)zone.Height;
            }
        }

        private static void SizeCard(HudCard card, DpRect rect, double scale)
        {
            if (!(scale > 0))
            {
                return;
            }

            card.ApplyBaseSize((float)(rect.Width / scale), (float)(rect.Height / scale));
        }

        // El tamaño base de una pieza desde su rectángulo de ventana (dp × s).
        private static void Size(VisualElement piece, DpRect rect, double scale)
        {
            if (!(scale > 0))
            {
                return;
            }

            piece.style.width = (float)(rect.Width / scale);
            piece.style.height = (float)(rect.Height / scale);
        }

        // Una zona: el ancla × s en su esquina, la escala s con el origen ahí (DT4a-03).
        public static void Place(VisualElement zone, HudZone z, double windowWidth, double windowHeight, double scale)
        {
            zone.style.position = Position.Absolute;
            var s = (float)scale;
            zone.style.scale = new Scale(new Vector3(s, s, 1f));
            switch (z.Corner)
            {
                case HudCorner.TopLeft:
                    zone.style.left = (float)z.AnchorX;
                    zone.style.top = (float)z.AnchorY;
                    zone.style.right = StyleKeyword.Auto;
                    zone.style.bottom = StyleKeyword.Auto;
                    zone.style.transformOrigin = new TransformOrigin(0, 0);
                    break;
                case HudCorner.TopRight:
                    zone.style.right = (float)(windowWidth - z.AnchorX);
                    zone.style.top = (float)z.AnchorY;
                    zone.style.left = StyleKeyword.Auto;
                    zone.style.bottom = StyleKeyword.Auto;
                    zone.style.transformOrigin = new TransformOrigin(new Length(100, LengthUnit.Percent), 0);
                    break;
                case HudCorner.BottomLeft:
                    zone.style.left = (float)z.AnchorX;
                    zone.style.bottom = (float)(windowHeight - z.AnchorY);
                    zone.style.right = StyleKeyword.Auto;
                    zone.style.top = StyleKeyword.Auto;
                    zone.style.transformOrigin = new TransformOrigin(0, new Length(100, LengthUnit.Percent));
                    break;
                case HudCorner.BottomRight:
                    zone.style.right = (float)(windowWidth - z.AnchorX);
                    zone.style.bottom = (float)(windowHeight - z.AnchorY);
                    zone.style.left = StyleKeyword.Auto;
                    zone.style.top = StyleKeyword.Auto;
                    zone.style.transformOrigin = new TransformOrigin(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
                    break;
                default:
                    // Abajo al centro: una fila con el contenido centrado y el origen en su centro. Mide ventana / s, así escalada
                    // ocupa justo el ancho de la ventana (a lo ancho y escalada, la caja de la fila salía de la ventana).
                    var width = s > 0 ? windowWidth / s : windowWidth;
                    zone.style.left = (float)((windowWidth - width) / 2);
                    zone.style.width = (float)width;
                    zone.style.right = StyleKeyword.Auto;
                    zone.style.bottom = (float)(windowHeight - z.AnchorY);
                    zone.style.top = StyleKeyword.Auto;
                    zone.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
                    break;
            }
        }

        private static bool Same(HudNaturalWidths a, HudNaturalWidths b)
        {
            return Near(a.SpmValue, b.SpmValue) && Near(a.SplitValue, b.SplitValue) && Near(a.WattsValue, b.WattsValue)
                   && Near(a.KcalValue, b.KcalValue) && Near(a.BpmValue, b.BpmValue) && Near(a.BandTime, b.BandTime)
                   && Near(a.BandDistance, b.BandDistance) && Near(a.SpmLabel, b.SpmLabel) && Near(a.SplitLabel, b.SplitLabel)
                   && Near(a.WattsLabel, b.WattsLabel) && Near(a.KcalLabel, b.KcalLabel) && Near(a.BpmLabel, b.BpmLabel)
                   && Near(a.SpmTarget, b.SpmTarget) && Near(a.SplitTarget, b.SplitTarget) && Near(a.WattsTarget, b.WattsTarget);
        }

        private static bool Near(double a, double b)
        {
            return Math.Abs(a - b) < 0.05;
        }
    }

    // Los márgenes seguros vigentes (ShellLayout.Insets).
    public delegate SafeInsets SafeInsetsSource();
}
