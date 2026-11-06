using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El elemento scene-surface (06a-escena-lago/02-tech-design §3.2, §3.3; DT6a-05; ADR 022): el hijo del HUD que sigue al fondo
    // provisorio (encima de él: el fundido de la escena demorada se ve como una mezcla, QA-V6a-01), a pantalla completa, sin eventos, con la RT de la escena de fondo estirada a toda la ventana (el filtro lo pone la RT: de punto a tamaño
    // completo, bilineal escalada). El panel gamma la muestrea y encima se compone el HUD en sRGB, como sobre el fondo provisorio.
    // Solo MUESTRA Output: nunca llama Preload, Open ni ningún otro paso del ciclo de vida (los llaman WorkoutScreenView y
    // ImmersiveWorkoutPage). Se suscribe a OutputChanged al adjuntarse al panel y se desuscribe al soltarse y en Dispose: una superficie
    // que se desarma (la de HudWarmup) no deja suscripciones colgadas (QA-G6aT-16). Repinta en cada cuadro en que la escena se dibujó
    // (S6a-7: el panel vuelve a muestrear la RT aunque el HUD no cambie).
    public sealed class SceneSurface : VisualElement, IDisposable
    {
        private readonly IImmersiveScene _scene;
        private bool _subscribed;
        private Texture? _shown;
        private bool _disposed;

        public SceneSurface(IImmersiveScene scene)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            name = "scene-surface";
            AddToClassList("rm-scene-surface");
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            style.backgroundSize = new BackgroundSize(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
            RegisterCallback<AttachToPanelEvent>(_ => Subscribe());
            RegisterCallback<DetachFromPanelEvent>(_ => Unsubscribe());
        }

        public Texture? Shown => _shown;

        public bool IsSubscribed => _subscribed;

        // Vuelve a tomar la RT (otra ventana o resolución) y pide el repintado (en cada cuadro dibujado).
        public void Refresh()
        {
            if (_disposed)
            {
                return;
            }

            var output = _scene.Output;
            if (!ReferenceEquals(output, _shown))
            {
                _shown = output;
                style.backgroundImage = output is RenderTexture rt ? new StyleBackground(Background.FromRenderTexture(rt)) : (StyleBackground)StyleKeyword.Null;
            }

            MarkDirtyRepaint();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Unsubscribe();
            _disposed = true;
            style.backgroundImage = StyleKeyword.Null;
            _shown = null;
        }

        private void Subscribe()
        {
            if (_subscribed || _disposed)
            {
                return;
            }

            _subscribed = true;
            _scene.OutputChanged += OnOutputChanged;
            _scene.Drawn += OnDrawn;
            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _subscribed = false;
            _scene.OutputChanged -= OnOutputChanged;
            _scene.Drawn -= OnDrawn;
        }

        private void OnDrawn()
        {
            if (!_disposed && resolvedStyle.display != DisplayStyle.None)
            {
                Refresh();
            }
        }

        private void OnOutputChanged()
        {
            Refresh();
        }
    }
}
