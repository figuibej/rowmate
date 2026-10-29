using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Presentation.Workout
{
    // El fondo provisorio del inmersivo (AC-SCN-01-01; DN4a-03, DN4a-18 con la variante de presupuesto de 04-design-notes §7;
    // 02-tech-design §6.3, §9.5): UN solo elemento con la textura de BackdropTexture, el cielo backdrop.sky del borde de arriba
    // hasta el horizonte de la cámara (0,40 del alto en vertical, 0,42 en horizontal) y el agua backdrop.water debajo, con el
    // borde recto. Quieto, sin eventos ni animación, sin dibujar agua, bote ni orilla; la textura se elige con la orientación,
    // en el acto. Hasta la escena de la It. 6a.
    public sealed class HudBackdrop : VisualElement, IDisposable
    {
        private Texture2D? _texture;
        private double _horizon = double.NaN;

        public HudBackdrop()
        {
            name = "hud-backdrop";
            AddToClassList("rm-hud-backdrop");
            pickingMode = PickingMode.Ignore;
        }

        public double Horizon => _horizon;

        public Texture2D? Texture => _texture;

        // El horizonte de la ventana (HudLayout.Horizon): rehace la textura solo si cambió (rotar).
        public void SetHorizon(double horizon)
        {
            if (Math.Abs(horizon - _horizon) < 1e-9 && _texture != null)
            {
                return;
            }

            _horizon = horizon;
            var sky = TokenColors.ToColor(Tokens.BackdropSky);
            var water = TokenColors.ToColor(Tokens.BackdropWater);
            if (_texture == null)
            {
                _texture = BackdropTexture.Create(sky, water, horizon);
                style.backgroundImage = new StyleBackground(_texture);
            }
            else
            {
                BackdropTexture.Fill(_texture, sky, water, horizon);
                MarkDirtyRepaint();
            }
        }

        public void Dispose()
        {
            if (_texture == null)
            {
                return;
            }

            style.backgroundImage = StyleKeyword.Null;
            if (Application.isPlaying)
            {
                Object.Destroy(_texture);
            }
            else
            {
                Object.DestroyImmediate(_texture);
            }

            _texture = null;
        }
    }
}
