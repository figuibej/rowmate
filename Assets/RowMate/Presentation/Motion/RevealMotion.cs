using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Algo que se abre o se cierra en su lugar empujando lo de abajo (03-rutinas/04-design-notes §5.2: la línea de error "abre
    // su alto de 0 al de su texto y funde 0 → 1 en 160 ease.move; al corregir el campo, al revés"; QA-V3-02 y 04-design-notes
    // §16.6, punto 4). Como GrowMotion y CollapseMotion: el contenido va en un slot sin estilo; mientras se mueve, el slot
    // recorta (overflow hidden) y su alto va de 0 al del contenido MÁS sus márgenes (así nada salta al final), y el contenido va
    // en position absolute arriba (left/right 0), con su alto natural. Para abrir, hasta tener el alto (el layout del cuadro
    // siguiente) la línea de tiempo no avanza (ILayoutBoundMotion: UiTestHost.Settle y el arnés dejan pasar un cuadro real).
    // Al cerrar, el contenido queda en display none y corre onClosed. Reducir movimiento, sin armar o fuera del panel: en el
    // acto (§5.0). Una orden nueva en medio de otra sigue desde donde está. En reposo no deja nada en línea salvo display.
    public sealed class RevealMotion : IMotion, ILayoutBoundMotion
    {
        private readonly MotionDriver _driver;
        private readonly VisualElement _slot;
        private readonly VisualElement _content;
        private readonly double _durationMs;
        private Action? _onClosed;
        private double _from;
        private double _to;
        private double _current;
        private double _start;
        private double _height;
        private bool _measured;
        private bool _running;

        public RevealMotion(MotionDriver driver, VisualElement slot, VisualElement content, double durationMs)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _durationMs = durationMs;
            _content.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (_running && !_measured)
                {
                    Measure();
                }
            });
        }

        public bool IsFinite => true;

        public bool IsRunning => _running;

        // Hacia dónde va (o dónde quedó): abierto o cerrado.
        public bool IsOpen => _to > 0.5;

        public bool IsWaitingForLayout => _running && !_measured && _content.panel != null;

        // Abre (true) o cierra (false). onClosed corre cuando terminó de cerrarse (también si fue en el acto); una apertura en
        // medio lo descarta.
        public void Set(bool open, Action? onClosed = null)
        {
            var target = open ? 1.0 : 0.0;
            _onClosed = open ? null : onClosed;
            if (Math.Abs(target - _to) < 1e-9 && (_running || Math.Abs(_current - target) < 1e-9))
            {
                if (!open && !_running)
                {
                    Closed();
                }

                return;
            }

            _to = target;
            if (open)
            {
                _content.style.display = DisplayStyle.Flex;
            }

            if (!_driver.Armed || _driver.Settings.ReduceMotion || _content.panel == null)
            {
                Snap();
                return;
            }

            _from = _current;
            if (!_running)
            {
                _measured = false;
                if (!open)
                {
                    // Abierto y en el flujo: ya tiene su alto.
                    Measure();
                }

                _slot.style.overflow = Overflow.Hidden;
                _slot.style.height = (float)(_measured ? _height * _from : 0);
                _content.style.position = Position.Absolute;
                _content.style.left = 0;
                _content.style.right = 0;
                _content.style.top = 0;
                _content.style.opacity = (float)_from;
                _running = true;
                _driver.Add(this);
            }

            _start = _driver.Now;
        }

        // Termina en el acto donde iba (un campo que se oculta con su fila, por ejemplo).
        public void Snap()
        {
            if (_running)
            {
                _running = false;
                _driver.Remove(this);
            }

            _current = _to;
            Rest();
        }

        public bool Tick(double nowSeconds)
        {
            if (!_running)
            {
                return false;
            }

            if (_content.panel == null)
            {
                // Se fue del árbol (el diálogo se cerró): queda en su estado final, sin esperar.
                _running = false;
                _current = _to;
                Rest();
                return false;
            }

            if (!_measured)
            {
                Measure();
                if (!_measured)
                {
                    _start = nowSeconds;
                    return true;
                }
            }

            var elapsed = (nowSeconds - _start) * 1000.0;
            var u = _durationMs <= 0 ? 1 : elapsed / _durationMs;
            u = u <= 0 ? 0 : u >= 1 ? 1 : u;
            _current = _from + (_to - _from) * Easing.Evaluate(Tokens.EaseMove, u);
            _slot.style.height = (float)(_height * _current);
            _content.style.opacity = (float)_current;
            if (u < 1)
            {
                return true;
            }

            _running = false;
            _current = _to;
            Rest();
            return false;
        }

        private void Measure()
        {
            var height = _content.layout.height;
            if (!(height > 0))
            {
                return;
            }

            var style = _content.resolvedStyle;
            _height = height + style.marginTop + style.marginBottom;
            _measured = true;
        }

        private void Rest()
        {
            _slot.style.height = StyleKeyword.Null;
            _slot.style.overflow = StyleKeyword.Null;
            _content.style.position = StyleKeyword.Null;
            _content.style.left = StyleKeyword.Null;
            _content.style.right = StyleKeyword.Null;
            _content.style.top = StyleKeyword.Null;
            _content.style.opacity = StyleKeyword.Null;
            if (_to <= 0)
            {
                _content.style.display = DisplayStyle.None;
                Closed();
            }
        }

        private void Closed()
        {
            var done = _onClosed;
            _onClosed = null;
            done?.Invoke();
        }
    }
}
