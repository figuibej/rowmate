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
    // - It. 5 (O-4 de la lente de motion de la 4b; 05-series-parciales/00-deuda-4b §4): cada animación se avanza aislada. Una que
    //   lanza sale del driver y queda en el registro como error, y las demás del cuadro siguen (la transición de la página
    //   incluida: la salida del inmersivo nunca queda trabada en negro por otra animación).
    public sealed class MotionDriver
    {
        public const string Tag = "UI";

        private readonly IClock _clock;
        private readonly FramePacingPolicy _pacing;
        private readonly IAppLogger _logger;
        private IMotion?[] _items = new IMotion?[32];
        private int _count;
        private bool _ticking;

        public MotionDriver(IClock clock, FramePacingPolicy pacing, MotionSettings settings, IAppLogger? logger = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _pacing = pacing ?? throw new ArgumentNullException(nameof(pacing));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? NullAppLogger.Instance;
        }

        // Animaciones que lanzaron y se sacaron (O-4).
        public int Faulted { get; private set; }

        public MotionSettings Settings { get; }

        public FramePacingPolicy Pacing => _pacing;

        public double Now => double.IsNaN(_startingAt) ? _clock.MonotonicSeconds : _startingAt;

        private double _startingAt = double.NaN;

        // It. 5 (QA-V4b-06; 05-series-parciales/00-deuda-4b §2): lo que arranca dentro de este alcance toma esa hora como su
        // comienzo (Now la devuelve). El HUD que se arma en el cuadro negro nace con la hora del toque: las fases de lo continuo (el
        // punto en vivo, el pulso de Próximo, el corazón) son las de la 4b, que lo armaba en el toque. Tick usa siempre el reloj.
        public StartTime StartingAt(double seconds)
        {
            var scope = new StartTime(this, _startingAt);
            _startingAt = seconds;
            return scope;
        }

        public readonly struct StartTime : IDisposable
        {
            private readonly MotionDriver _driver;
            private readonly double _previous;

            internal StartTime(MotionDriver driver, double previous)
            {
                _driver = driver;
                _previous = previous;
            }

            public void Dispose()
            {
                if (_driver != null)
                {
                    _driver._startingAt = _previous;
                }
            }
        }

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

        // It. 4b (la regla de toma del arnés, 04b-hud-exr/02-tech-design §5.2): copia las animaciones en curso (finitas y continuas).
        public void CopyMotions(System.Collections.Generic.List<IMotion> into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            into.Clear();
            for (var i = 0; i < _count; i++)
            {
                var item = _items[i];
                if (item != null)
                {
                    into.Add(item);
                }
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

                    bool alive;
                    try
                    {
                        alive = item.Tick(now);
                    }
                    catch (Exception error)
                    {
                        // O-4: sale y queda en el registro; las demás siguen en este mismo cuadro.
                        alive = false;
                        Faulted++;
                        _logger.Log(LogLevel.Error, Tag, "una animación lanzó " + error.GetType().Name + " y sale del cuadro (" + item.GetType().Name + ")", error);
                    }

                    if (!alive)
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
