using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // El ancla de desplazamiento al cambiar el ancho de la ventana (It. 14, T-07, AC-APP-15-10; 00-arte-pulido §3.3 punto 5; 02-tech-design
    // §3): fuera del HUD, una rotación, un redimensionado o la pantalla completa reacomodan la lista en el acto y el primer elemento que se
    // veía queda arriba, a la misma distancia del borde (no se salta a otra tarjeta porque el texto se volvió a partir).
    // - Captura (índice del primer hijo visible del contenido + su distancia al borde de arriba) con cada desplazamiento y con cada cambio
    //   de alto del contenido sin cambio de ancho (una tarjeta que entra o sale).
    // - Con un cambio de ANCHO del contenido (la ventana cambió) repone el desplazamiento: hijo.y − distancia, recortado al rango. Lo
    //   vuelve a aplicar en los GeometryChangedEvent siguientes (el texto que se vuelve a partir cambia los altos en más de un paso) hasta
    //   que el usuario toque la lista (rueda, puntero o tecla).
    // - Sin elementos nuevos ni estilos; sin asignar por cuadro (solo corre con eventos de layout o de desplazamiento).
    public sealed class ScrollAnchor
    {
        private readonly ScrollView _scroll;
        private float _contentWidth = -1;
        private bool _pending;
        private bool _restoring;

        public ScrollAnchor(ScrollView scroll)
        {
            _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            _scroll.verticalScroller.valueChanged += _ => Capture();
            _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(OnContentGeometry);
            _scroll.RegisterCallback<WheelEvent>(_ => _pending = false, TrickleDown.TrickleDown);
            _scroll.RegisterCallback<PointerDownEvent>(_ => _pending = false, TrickleDown.TrickleDown);
            _scroll.RegisterCallback<KeyDownEvent>(_ => _pending = false, TrickleDown.TrickleDown);
        }

        // El hijo del contenido que manda (-1 sin capturar) y su distancia al borde de arriba de la vista, en dp.
        public int AnchorIndex { get; private set; } = -1;

        public float AnchorDelta { get; private set; }

        public static ScrollAnchor Attach(ScrollView scroll)
        {
            return new ScrollAnchor(scroll);
        }

        private void OnContentGeometry(GeometryChangedEvent evt)
        {
            var width = evt.newRect.width;
            if (_contentWidth >= 0 && Mathf.Abs(width - _contentWidth) > 0.5f && AnchorIndex >= 0)
            {
                _pending = true;
            }

            _contentWidth = width;
            if (_pending)
            {
                Restore();
            }
            else
            {
                Capture();
            }
        }

        private void Capture()
        {
            if (_restoring || _pending)
            {
                return;
            }

            var content = _scroll.contentContainer;
            var top = _scroll.scrollOffset.y;
            AnchorIndex = -1;
            for (var i = 0; i < content.childCount; i++)
            {
                var child = content[i];
                if (child.resolvedStyle.display == DisplayStyle.None)
                {
                    continue;
                }

                var box = child.layout;
                if (float.IsNaN(box.height) || box.yMax <= top + 0.5f)
                {
                    continue;
                }

                AnchorIndex = i;
                AnchorDelta = box.y - top;
                return;
            }
        }

        private void Restore()
        {
            var content = _scroll.contentContainer;
            if (AnchorIndex < 0 || AnchorIndex >= content.childCount)
            {
                _pending = false;
                return;
            }

            var box = content[AnchorIndex].layout;
            if (float.IsNaN(box.y))
            {
                return;
            }

            var max = Mathf.Max(0, content.layout.height - _scroll.contentViewport.layout.height);
            var desired = Mathf.Clamp(box.y - AnchorDelta, 0, float.IsNaN(max) ? 0 : max);
            var offset = _scroll.scrollOffset;
            if (Mathf.Abs(offset.y - desired) <= 0.01f)
            {
                return;
            }

            _restoring = true;
            try
            {
                _scroll.scrollOffset = new Vector2(offset.x, desired);
            }
            finally
            {
                _restoring = false;
            }
        }
    }
}
