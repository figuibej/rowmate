using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // El sonar de la búsqueda del remo (P1; 00-arte §1.2; D10-02; A10-03): una banda fija de 152 dp entre la ayuda y la lista, solo
    // mientras busca y sin alto compacto (< 560 dp de contenido: L640 y TH quedan con el spinner del botón, como el legacy). DOS elementos:
    //   #device-sonar .rm-sonar (la banda: malla propia con el disco de 56 en accent.tonalBg y los anillos en accent.primary; overflow hidden)
    //   └── #device-sonar-icon (bluetooth_searching 28 en accent.onTonal)
    // Anillos: 3, radio 28 → 68 (ease.enter), alfa 0,45 → 0, período 1 600 y desfase 533, escalonados desde el cuadro 0. Eco: un 4.º anillo
    // en accent.onTonal (0,70 → 0 en 900) con cada remo nuevo. El disco respira 1 → 1,03. Entra con la regla del empuje (alto 0-160,
    // opacidad 60-260) y sale (opacidad 0-120, alto 80-240). Se repinta una vez por cuadro solo mientras busca y a la vista (otra pestaña
    // o la vista oculta: no se mueve). Sin asignaciones por cuadro (marcador RowMate.Device.Sonar; BleAllocationTests).
    // Reducir movimiento: los anillos quietos (u = 0,25, 0,55 y 0,85), sin respiración ni eco, y la banda entra y sale solo con opacidad.
    public sealed class SonarView : VisualElement, IMotion
    {
        public static readonly ProfilerMarker Marker = new ProfilerMarker("RowMate.Device.Sonar");

        private static readonly double[] StillPhases = { 0.25, 0.55, 0.85 };

        private readonly MotionDriver? _motion;
        private double _start = double.NaN;
        private double _echoStart = double.NaN;
        private double _bandStart = double.NaN;
        private bool _searching;
        private bool _compact;
        private bool _reduced;
        private double _elapsedMs;
        private double _band;    // 0 a 1 (alto)
        private double _opacity; // 0 a 1

        public SonarView(MotionDriver? motion)
        {
            _motion = motion;
            name = "device-sonar";
            AddToClassList("rm-sonar");
            pickingMode = PickingMode.Ignore;
            style.flexShrink = 0;
            style.overflow = Overflow.Hidden;
            Icon = new IconView(Icons.BluetoothSearching, false, (float)BleMotionSpecs.SonarIconDp) { name = "device-sonar-icon" };
            Icon.AddToClassList("rm-sonar__icon");
            Add(Icon);
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(_ => UpdateRegistration());
            RegisterCallback<DetachFromPanelEvent>(_ => _motion?.Remove(this));
            Apply();
        }

        public IconView Icon { get; }

        // Lo continuo (anillos) no cuenta para Settle; la entrada y la salida de la banda sí.
        public bool IsFinite => !double.IsNaN(_bandStart);

        public bool IsSearching => _searching;

        public bool IsCompact => _compact;

        // La banda ocupa lugar (entrando, quieta o saliendo).
        public bool IsShown => _band > 0;

        // Milisegundos desde que empezó la búsqueda (pruebas y arnés).
        public double ElapsedMs => _elapsedMs;

        public void SetCompact(bool compact)
        {
            if (compact == _compact)
            {
                return;
            }

            _compact = compact;
            if (compact)
            {
                _bandStart = double.NaN;
                _band = 0;
                _opacity = 0;
                Apply();
            }
            else if (_searching)
            {
                _band = 1;
                _opacity = 1;
                Apply();
            }

            UpdateRegistration();
        }

        // Empieza o termina la búsqueda del remo.
        public void SetSearching(bool searching)
        {
            if (searching == _searching)
            {
                return;
            }

            _searching = searching;
            var now = _motion?.Now ?? 0;
            _reduced = _motion?.Settings.ReduceMotion ?? false;
            if (searching)
            {
                _start = now;
                _echoStart = double.NaN;
                _elapsedMs = 0;
            }

            if (_compact || _motion == null || !_motion.Armed)
            {
                _bandStart = double.NaN;
                _band = searching && !_compact ? 1 : 0;
                _opacity = _band;
                Apply();
            }
            else
            {
                _bandStart = now;
            }

            UpdateRegistration();
            MarkDirtyRepaint();
        }

        // Apareció un remo nuevo en la lista: un eco (a lo sumo uno a la vez; el siguiente lo reinicia).
        public void Echo()
        {
            if (!_searching || _compact || _reduced || _motion == null)
            {
                return;
            }

            _echoStart = _motion.Now;
        }

        public bool Tick(double nowSeconds)
        {
            using (Marker.Auto())
            {
                if (panel == null)
                {
                    return false;
                }

                var live = _searching && !_compact;
                if (!double.IsNaN(_bandStart))
                {
                    var t = (nowSeconds - _bandStart) * 1000.0;
                    if (_searching)
                    {
                        _band = _reduced ? 1 : Easing.Evaluate(Tokens.EaseMove, BleMotionSpecs.Segment(t, 0, BleMotionSpecs.SonarInHeightEndMs));
                        _opacity = _reduced
                            ? Easing.Evaluate(Tokens.EaseMove, BleMotionSpecs.Segment(t, 0, Tokens.DurFast.Milliseconds))
                            : Easing.Evaluate(Tokens.EaseMove, BleMotionSpecs.Segment(t, BleMotionSpecs.SonarInFadeStartMs, BleMotionSpecs.SonarInFadeEndMs));
                        if (t >= (_reduced ? Tokens.DurFast.Milliseconds : BleMotionSpecs.SonarInFadeEndMs))
                        {
                            _bandStart = double.NaN;
                        }
                    }
                    else
                    {
                        _opacity = 1 - Easing.Evaluate(Tokens.EaseExit, BleMotionSpecs.Segment(t, 0, _reduced ? Tokens.DurFast.Milliseconds : BleMotionSpecs.SonarOutFadeEndMs));
                        _band = _reduced ? (_opacity > 0 ? 1 : 0)
                            : 1 - Easing.Evaluate(Tokens.EaseMove, BleMotionSpecs.Segment(t, BleMotionSpecs.SonarOutHeightStartMs, BleMotionSpecs.SonarOutHeightEndMs));
                        if (t >= (_reduced ? Tokens.DurFast.Milliseconds : BleMotionSpecs.SonarOutHeightEndMs))
                        {
                            _bandStart = double.NaN;
                            _band = 0;
                            _opacity = 0;
                        }
                    }

                    Apply();
                }

                if (!double.IsNaN(_start))
                {
                    _elapsedMs = (nowSeconds - _start) * 1000.0;
                }

                if (_band > 0 && BleMotion.IsDisplayed(this))
                {
                    MarkDirtyRepaint();
                }

                var keep = live || !double.IsNaN(_bandStart);
                return keep;
            }
        }

        private void Apply()
        {
            style.height = (float)(BleMotionSpecs.SonarBandDp * _band);
            style.opacity = (float)_opacity;
            style.display = _band > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateRegistration()
        {
            if (_motion == null)
            {
                return;
            }

            if (panel != null && (_searching && !_compact || !double.IsNaN(_bandStart)))
            {
                _motion.Add(this);
            }
            else
            {
                _motion.Remove(this);
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            using (Marker.Auto())
            {
                var rect = contentRect;
                if (rect.width <= 0 || _band <= 0)
                {
                    return;
                }

                // El centro de la caja de 136 queda fijo a 8 + 68 del borde de arriba (la banda crece hacia abajo y la recorta).
                var center = new Vector2(rect.center.x, (float)(8 + BleMotionSpecs.SonarBoxDp / 2));
                var accent = TokenColors.ToColor(Tokens.AccentPrimary);
                var stroke = (float)BleMotionSpecs.SonarRingStrokeDp;
                for (var i = 0; i < BleMotionSpecs.SonarRings; i++)
                {
                    double u;
                    if (_reduced)
                    {
                        u = StillPhases[i];
                    }
                    else if (!BleMotionSpecs.SonarRingPhase(_elapsedMs, i, out u))
                    {
                        continue;
                    }

                    var c = accent;
                    c.a = (float)(c.a * BleMotionSpecs.SonarRingAlphaAt(u));
                    MeshShapes.Ring(context, center, (float)BleMotionSpecs.SonarRingRadius(u), stroke, c, BleMotionSpecs.SonarSegments);
                }

                if (!double.IsNaN(_echoStart) && _motion != null)
                {
                    var e = (_motion.Now - _echoStart) * 1000.0 / BleMotionSpecs.SonarEchoMs;
                    if (e >= 0 && e < 1)
                    {
                        var echo = TokenColors.ToColor(Tokens.AccentOnTonal);
                        echo.a = (float)(echo.a * BleMotionSpecs.SonarEchoAlpha * (1 - e));
                        MeshShapes.Ring(context, center, (float)BleMotionSpecs.SonarRingRadius(e), stroke, echo, BleMotionSpecs.SonarSegments);
                    }
                }

                var scale = _reduced ? 1 : BleMotionSpecs.SonarDiscScale(_elapsedMs);
                MeshShapes.Disc(context, center, (float)(BleMotionSpecs.SonarDiscDp / 2 * scale), TokenColors.ToColor(Tokens.AccentTonalBg),
                    BleMotionSpecs.SonarSegments);
            }
        }
    }
}
