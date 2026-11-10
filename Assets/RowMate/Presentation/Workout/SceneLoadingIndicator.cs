using System;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El indicador de carga de la escena en el primer entrenamiento (It. 14, #21, AC-DEBT-09; DT14-30, QA-G14-03; ART_BIBLE §3.10):
    // UN SOLO elemento (el HUD está cerca de su tope de 60): un Label con workoutSceneLoading ("Preparando escena…") que dibuja arriba
    // del texto el arco de espera de 32 dp (270°, una vuelta cada 900 ms, como Spinner), centrado en el área de la escena. Es hermano de
    // SceneSurface (justo después: encima del fondo provisorio y de la escena, debajo del contenido del HUD); display: none salvo
    // mientras se ve. La opacidad y cuándo se ve los decide SceneLoadingController (SceneLoadingSpecs).
    //   #scene-loading .rm-scene-loading .rm-type-body-s
    public sealed class SceneLoadingIndicator : Label, IMotion
    {
        public const string UssClass = "rm-scene-loading";
        public const float StrokeDp = 3;
        public const float GapDp = 8;

        private readonly MotionDriver _motion;
        private float _degrees;
        private bool _shown;

        public SceneLoadingIndicator(UiContext ui)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            _motion = ui.Motion;
            name = "scene-loading";
            AddToClassList(UssClass);
            AddToClassList("rm-type-body-s");
            pickingMode = PickingMode.Ignore;
            enableRichText = false;
            text = ui.Texts.Get(L10nKeys.WorkoutSceneLoading);
            style.position = Position.Absolute;
            style.left = new Length(50, LengthUnit.Percent);
            style.top = new Length(50, LengthUnit.Percent);
            style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
            style.paddingTop = SceneLoadingSpecs.IndicatorDp + GapDp;
            style.unityTextAlign = TextAnchor.UpperCenter;
            style.color = TokenColors.ToColor(Tokens.TextSecondary);
            style.display = DisplayStyle.None;
            generateVisualContent += Draw;
            RegisterCallback<DetachFromPanelEvent>(_ => _motion.Remove(this));
        }

        public bool IsShown => _shown;

        // El arco gira mientras se ve (continuo: no lo esperan Settle ni la regla de toma).
        public bool IsFinite => false;

        public void SetText(string value)
        {
            if (text != value)
            {
                text = value;
            }
        }

        // opacity ≤ 0: fuera (display: none, 0 elementos visibles); > 0: a la vista con esa opacidad.
        public void SetOpacity(double opacity)
        {
            var shown = opacity > 0;
            if (shown != _shown)
            {
                _shown = shown;
                style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                if (shown)
                {
                    _motion.Add(this);
                }
                else
                {
                    _motion.Remove(this);
                }
            }

            style.opacity = shown ? (float)Math.Min(1, opacity) : 0f;
        }

        public bool Tick(double nowSeconds)
        {
            if (panel == null || !_shown)
            {
                return false;
            }

            var turns = nowSeconds * 1000.0 / MotionSpecs.SpinnerTurnMs;
            _degrees = (float)((turns - Math.Floor(turns)) * 360.0);
            MarkDirtyRepaint();
            return true;
        }

        private void Draw(MeshGenerationContext context)
        {
            var size = (float)SceneLoadingSpecs.IndicatorDp;
            var rect = contentRect;
            var center = new Vector2(rect.center.x, rect.yMin - GapDp - size / 2);
            var radius = size / 2 - StrokeDp / 2;
            if (radius <= 0)
            {
                return;
            }

            var painter = context.painter2D;
            painter.strokeColor = TokenColors.ToColor(Tokens.AccentPrimary);
            painter.lineWidth = StrokeDp;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            var start = -90 + _degrees;
            painter.Arc(center, radius, new Angle(start, AngleUnit.Degree), new Angle(start + (float)MotionSpecs.SpinnerArcDegrees, AngleUnit.Degree),
                ArcDirection.Clockwise);
            painter.Stroke();
        }
    }

    // Cuándo se ve el indicador (It. 14, #21, AC-DEBT-09; DT14-30): solo si la escena no estaba lista al entrar al inmersivo; a los
    // 120 ms de gracia con un fundido de 160 y, con la escena lista, se va con otro de 160 (SceneLoadingSpecs). Sin elementos propios: lo
    // mueve ImmersiveWorkoutPage (Tick por MotionDriver mientras hace falta). El HUD, el reloj y la sesión no esperan nada.
    public sealed class SceneLoadingController : IMotion
    {
        private readonly SceneLoadingIndicator _indicator;
        private readonly MotionDriver _motion;
        private readonly bool _readyAtEnter;
        private readonly double _enteredAt;
        private double _readyAt = double.NaN;
        private bool _done;

        public SceneLoadingController(SceneLoadingIndicator indicator, MotionDriver motion, bool sceneReadyAtEnter, double enteredAt)
        {
            _indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _readyAtEnter = sceneReadyAtEnter;
            _enteredAt = enteredAt;
            _done = !SceneLoadingSpecs.NeedsIndicator(sceneReadyAtEnter);
            if (!_done)
            {
                _motion.Add(this);
            }
        }

        public bool NeedsIndicator => SceneLoadingSpecs.NeedsIndicator(_readyAtEnter);

        public bool IsDone => _done;

        // Continuo mientras espera la escena (no retiene Settle ni la escena demorada); termina con el fundido de salida.
        public bool IsFinite => false;

        // La escena quedó lista (OnSceneReadiness).
        public void SceneReady(double now)
        {
            if (double.IsNaN(_readyAt))
            {
                _readyAt = now;
            }
        }

        public void Stop()
        {
            _done = true;
            _indicator.SetOpacity(0);
            _motion.Remove(this);
        }

        public bool Tick(double nowSeconds)
        {
            if (_done)
            {
                return false;
            }

            var sinceEnter = (nowSeconds - _enteredAt) * 1000.0;
            var sinceReady = double.IsNaN(_readyAt) ? double.NaN : (nowSeconds - _readyAt) * 1000.0;
            var opacity = SceneLoadingSpecs.Opacity(_readyAtEnter, sinceEnter, sinceReady);
            _indicator.SetOpacity(opacity);
            if (!double.IsNaN(sinceReady) && opacity <= 0)
            {
                _done = true;
                return false;
            }

            return true;
        }
    }
}
