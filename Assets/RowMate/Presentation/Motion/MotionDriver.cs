using System;
using RowMate.Core.Common;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion
{
    // Un solo reloj para todas las animaciones de la UI (02-tech-design §3.9, D-14): IClock.MonotonicSeconds
    // (UnityScheduler en la app, FakeScheduler en tests y en el arnés). Lo avanza ShellHandle.Tick una vez por cuadro.
    // - Sin asignaciones por cuadro: arreglo propio, sin LINQ ni closures nuevas.
    // - Arranca "desarmado" (04-design-notes §5.0, AC-APP-01-07): hasta el primer Tick las vistas aplican el estado tal cual,
    //   sin animar; se arma al terminar el primer Tick.
    // - Mientras haya algo activo pide 60 fps a FramePacingPolicy.
    public sealed class MotionDriver
    {
        private readonly IClock _clock;
        private readonly FramePacingPolicy _pacing;
        private IMotion?[] _items = new IMotion?[32];
        private int _count;
        private bool _ticking;

        public MotionDriver(IClock clock, FramePacingPolicy pacing, MotionSettings settings)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _pacing = pacing ?? throw new ArgumentNullException(nameof(pacing));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public MotionSettings Settings { get; }

        public FramePacingPolicy Pacing => _pacing;

        public double Now => _clock.MonotonicSeconds;

        // Después del primer Tick (el primer cuadro presentado): antes, nada se anima.
        public bool Armed { get; private set; }

        public int ActiveCount => _count;

        // Alguna animación con fin en curso (todas salvo latido y spinner).
        public bool HasFiniteMotions
        {
            get
            {
                for (var i = 0; i < _count; i++)
                {
                    var item = _items[i];
                    if (item != null && item.IsFinite)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Alguna animación finita espera el layout del cuadro siguiente (ILayoutBoundMotion): avanzar el reloj no la mueve, hace
        // falta un cuadro real (UiTestHost.Settle corta y el que llama deja pasar uno).
        public bool WaitingForLayout
        {
            get
            {
                for (var i = 0; i < _count; i++)
                {
                    if (_items[i] is ILayoutBoundMotion bound && bound.IsWaitingForLayout)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // Diagnóstico (arnés): los tipos de las animaciones finitas en curso.
        public string DescribeFinite()
        {
            var text = "";
            for (var i = 0; i < _count; i++)
            {
                var item = _items[i];
                if (item != null && item.IsFinite)
                {
                    var name = item.GetType().Name;
                    for (var e = item as UnityEngine.UIElements.VisualElement; e != null; e = e.hierarchy.parent)
                    {
                        if (!string.IsNullOrEmpty(e.name))
                        {
                            name += "@" + e.name;
                            break;
                        }
                    }

                    if (item is RowMate.Presentation.Controls.FitBox fit)
                    {
                        name += "(" + fit.AppliedScale.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "→"
                                + fit.FitScale.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + ")";
                    }
                    text += (text.Length > 0 ? ", " : "") + name;
                }
            }

            return text;
        }

        public bool Contains(IMotion motion)
        {
            for (var i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_items[i], motion))
                {
                    return true;
                }
            }

            return false;
        }

        // Registra una animación (dos veces la misma, una sola). Corre desde el próximo Tick.
        public void Add(IMotion motion)
        {
            if (motion == null)
            {
                throw new ArgumentNullException(nameof(motion));
            }

            if (Contains(motion))
            {
                return;
            }

            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[_count++] = motion;
            _pacing.Acquire();
        }

        // La saca sin esperar a que termine (una vista que se va del panel).
        public void Remove(IMotion motion)
        {
            for (var i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_items[i], motion))
                {
                    _items[i] = null;
                    if (!_ticking)
                    {
                        Compact();
                    }

                    return;
                }
            }
        }

        public void Tick()
        {
            var now = _clock.MonotonicSeconds;
            _ticking = true;
            try
            {
                // Las que se agregan durante el Tick corren desde el siguiente (count fijo al empezar).
                var count = _count;
                for (var i = 0; i < count; i++)
                {
                    var item = _items[i];
                    if (item == null)
                    {
                        continue;
                    }

                    if (!item.Tick(now))
                    {
                        if (ReferenceEquals(_items[i], item))
                        {
                            _items[i] = null;
                        }
                    }
                }
            }
            finally
            {
                _ticking = false;
                Compact();
            }

            Armed = true;
        }

        private void Compact()
        {
            var write = 0;
            for (var read = 0; read < _count; read++)
            {
                var item = _items[read];
                if (item == null)
                {
                    _pacing.Release();
                    continue;
                }

                _items[write++] = item;
            }

            for (var i = write; i < _count; i++)
            {
                _items[i] = null;
            }

            _count = write;
        }
    }
}
