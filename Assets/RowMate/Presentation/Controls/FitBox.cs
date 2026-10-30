using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // = FittedBox(fit: BoxFit.scaleDown) del legacy (device_screen.dart:321-323, heart_rate_card.dart:163-166): el contenido
    // se mide a tamaño natural (hijo en posición absoluta) y, si no entra, se escala con style.scale (sin relayout) hasta
    // entrar, centrado o alineado al inicio. Sin tope inferior (D-10). FitScale = la escala destino (tests, AC-BLE-11-06).
    // - Cuando un dato nuevo cambia la escala necesaria (el dueño llama a ExpectDataChange antes de cambiar el contenido), la
    //   escala sigue un CriticalSpring ω 12 (DN-15). El primer cálculo, un cambio de tamaño de la caja, de letra y reducir
    //   movimiento: en el acto.
    // - SizeToContentHeight: la caja toma el alto del contenido escalado (la del pulso, en una fila); si no, la caja es la
    //   que manda (la del tablero, que crece con flex-grow) y el contenido se centra en los dos ejes.
    // - It. 4b (02-tech-design §5.2, §7.4): ISettlingMotion dice cuánto le falta a la escala, en dp por el ancho del contenido,
    //   así la regla de toma no la cuenta a menos de 0,15 dp de su destino. La conducta no cambia.
    public sealed class FitBox : VisualElement, IMotion, ISettlingMotion
    {
        public const string UssClass = "rm-fitbox";

        private readonly MotionDriver? _motion;
        private readonly bool _alignStart;
        private readonly bool _sizeToContentHeight;
        private CriticalSpring _spring = new CriticalSpring(1);
        private double _target = 1;
        private bool _expectDataChange;
        private bool _hasLayout;
        private float _boxWidth = -1;
        private float _boxHeight = -1;
        private float _appliedScale = 1;
        private float _left = float.NaN;
        private float _top = float.NaN;

        // A la grilla de píxeles físicos: con la posición en un píxel exacto, el redondeo del layout no cambia el tamaño del
        // contenido de un cuadro a otro (sin bucle de layout).
        private float Snap(float value)
        {
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            return Mathf.Round(value * ppp) / ppp;
        }

        private static bool Changed(ref float current, float value)
        {
            if (!float.IsNaN(current) && Mathf.Abs(current - value) < 0.05f)
            {
                return false;
            }

            current = value;
            return true;
        }

        public FitBox(bool alignStart, bool sizeToContentHeight, MotionDriver? motion)
        {
            _alignStart = alignStart;
            _sizeToContentHeight = sizeToContentHeight;
            _motion = motion;
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            Content = new VisualElement();
            Content.AddToClassList(UssClass + "__content");
            Content.pickingMode = PickingMode.Ignore;
            Content.style.position = Position.Absolute;
            Content.style.left = 0;
            Content.style.top = 0;
            Content.style.transformOrigin = new TransformOrigin(0, 0);
            hierarchy.Add(Content);
            RegisterCallback<GeometryChangedEvent>(OnBoxGeometry);
            Content.RegisterCallback<GeometryChangedEvent>(OnContentGeometry);
        }

        public VisualElement Content { get; }

        public override VisualElement contentContainer => Content;

        public bool IsFinite => true;

        // Escala destino (1 = entra a tamaño natural).
        public float FitScale => (float)_target;

        // Escala aplicada ahora (distinta de FitScale solo mientras el resorte se mueve).
        public float AppliedScale => (float)_spring.Position;

        // Lo que le falta al resorte de la escala, en dp: |destino − escala| × el ancho natural del contenido (0 quieto).
        public double RemainingDp
        {
            get
            {
                if (_motion == null || !_motion.Contains(this))
                {
                    return 0;
                }

                var width = Content.layout.width;
                return float.IsNaN(width) || width <= 0 ? 0 : Math.Abs(_target - _spring.Position) * width;
            }
        }

        // El contenido, con la escala aplicada, entra en la caja (± 0,5 dp).
        public bool Fits
        {
            get
            {
                var w = Content.layout.width * AppliedScale;
                var h = Content.layout.height * AppliedScale;
                return w <= contentRect.width + 0.5f && (_sizeToContentHeight || h <= contentRect.height + 0.5f);
            }
        }

        // El próximo cambio de tamaño del contenido viene de un dato nuevo: anima (si está armado y sin reducir movimiento).
        public void ExpectDataChange()
        {
            _expectDataChange = true;
        }

        // El resorte se evalúa en forma cerrada desde el instante en que arrancó (CriticalSpring.Step con dt = ahora − inicio,
        // sobre el estado inicial): no depende de cuántos Tick hubo ni de su dt. Tope de 1,5 s (a ω 12 ya está quieto).
        public bool Tick(double nowSeconds)
        {
            var elapsed = nowSeconds - _animStart;
            var state = _animFrom;
            state.Step(elapsed, _target, MotionSpecs.FitScaleOmega);
            _spring = state;
            if (elapsed >= 1.5 || _spring.IsSettled(_target, 2e-3))
            {
                _spring.SnapTo(_target);
                ApplyScale();
                return false;
            }

            ApplyScale();
            return true;
        }

        private CriticalSpring _animFrom;
        private double _animStart;
        private double _animTarget = double.NaN;

        // Arranca (o reencauza hacia un destino nuevo) desde la posición y la velocidad actuales.
        private void StartSpring()
        {
            _animFrom = _spring;
            _animStart = _motion != null ? _motion.Now : 0;
            _motion?.Add(this);
        }

        // Solo un cambio de TAMAÑO del contenido recalcula: mover el contenido (left/top que pone ApplyScale) no, así no hay
        // un bucle de layout (en PlayMode, "Layout update is struggling…").
        private void OnContentGeometry(GeometryChangedEvent evt)
        {
            var pixel = 1.01f / (scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f);
            if (Mathf.Abs(evt.newRect.width - evt.oldRect.width) < pixel && Mathf.Abs(evt.newRect.height - evt.oldRect.height) < pixel && _hasLayout)
            {
                return;
            }

            Recompute(false);
        }

        private void OnBoxGeometry(GeometryChangedEvent evt)
        {
            var widthChanged = Mathf.Abs(evt.newRect.width - _boxWidth) > 0.01f;
            var heightChanged = !_sizeToContentHeight && Mathf.Abs(evt.newRect.height - _boxHeight) > 0.01f;
            if (!widthChanged && !heightChanged)
            {
                return;
            }

            _boxWidth = evt.newRect.width;
            _boxHeight = evt.newRect.height;
            Recompute(true);
        }

        private void Recompute(bool boxChanged)
        {
            var natural = Content.layout;
            var available = contentRect;
            if (!(natural.width > 0) || !(natural.height > 0) || !(available.width > 0))
            {
                return;
            }

            var scale = Math.Min(1.0, available.width / natural.width);
            if (!_sizeToContentHeight && available.height > 0)
            {
                scale = Math.Min(scale, available.height / natural.height);
            }

            var animate = _expectDataChange && !boxChanged && _hasLayout && _motion != null && _motion.Armed
                          && !_motion.Settings.ReduceMotion && Math.Abs(scale - _target) > 1e-4;
            _expectDataChange = false;
            _hasLayout = true;
            _target = scale;
            if (animate)
            {
                // Si ya se movía hacia (casi) el mismo destino, sigue (no se reinicia: un recálculo por un cambio de layout no
                // frena el resorte). Si el destino cambió, sigue desde donde está con su velocidad hacia el nuevo.
                if (!_motion!.Contains(this) || Math.Abs(scale - _animTarget) > 0.005)
                {
                    _animTarget = scale;
                    StartSpring();
                }
            }
            else
            {
                _motion?.Remove(this);
                _spring.SnapTo(scale);
                ApplyScale();
            }
        }

        private void ApplyScale()
        {
            var s = (float)_spring.Position;
            if (Mathf.Abs(_appliedScale - s) > 1e-5f)
            {
                _appliedScale = s;
                Content.style.scale = new Scale(new Vector2(s, s));
            }

            var natural = Content.layout;
            var available = contentRect;
            if (Changed(ref _left, Snap(_alignStart ? 0 : (available.width - natural.width * s) / 2)))
            {
                Content.style.left = _left;
            }

            var top = _sizeToContentHeight ? 0 : Snap((available.height - natural.height * s) / 2);
            if (Changed(ref _top, top))
            {
                Content.style.top = _top;
            }

            if (_sizeToContentHeight)
            {
                var height = natural.height * s;
                if (Mathf.Abs(resolvedStyle.height - height) > 0.05f)
                {
                    style.height = height;
                }
            }
        }
    }
}
