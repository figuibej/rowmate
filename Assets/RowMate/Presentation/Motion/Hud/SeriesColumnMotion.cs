using System;
using RowMate.Core.Design;

namespace RowMate.Presentation.Motion.Hud
{
    // Lo que mueve la columna de los paneles entera (05-series-parciales/02-tech-design §4.2, §7.4; 04-design-notes §6.4 a §6.6 y
    // §6.8; AC-WKT-18-01, -13; AC-APP-05-08). Con el contrato de la 4b (HudMotion): no conoce las vistas; le da a SeriesColumn (el
    // único que escribe style.translate y style.opacity de los paneles, DT5-14) un SeriesColumnVisual (SeriesTimelines, WP-A):
    // - RepsAppear: la serie que empieza; el panel de repeticiones aparece en su estado guardado, sin abrirse: opacidad 0 → 1 y
    //   +8·s → 0 en 120-400 (ease.enter). RepsLeave: la serie que termina; se va 1 → 0 en 0-120 (ease.exit), sin moverse, y
    //   después avisa gone (la vista lo saca del árbol). Una entrada o una salida a medio camino sigue desde la opacidad que tiene.
    // - FollowSpm: la columna sigue a SPM cuando gana o pierde obj. (16,9·s; gana en 0-240, pierde en 80-280, ease.move), sobre su
    //   lugar final, como Watts debajo de Split en la 4a. Un corrimiento que no terminó sigue con el nuevo, sin saltos.
    // - WheelTo: el desplazamiento de la columna (ColumnScroll; en base del panel) con spring.snappy desde su posición y su
    //   velocidad; con Instant, en el acto (el arrastre 1:1, sin inercia; la rotación, a 0). ShiftScroll: una fila que entra con la
    //   vista desplazada suma 22·sp·k en el mismo cuadro, así lo que se ve no se mueve (DD5-27); el resorte sigue igual, corrido.
    //   Como las fases son independientes, en RowsScroll (W1280) la vista puede usar una instancia por panel solo para la rueda.
    // Fases (F): la entrada (0-400) y la salida (0-120) de repeticiones, el corrimiento (0-240 / 0-280) y el resorte de la rueda;
    // sin fases, sale del MotionDriver. Reducir movimiento (§6.8): solo opacidad (la subida en 0), el corrimiento y la rueda en el
    // acto; se lee en cada llamada y en cada cuadro (QA-V4b-MOT-03). Sin armar o con Instant: el estado final en el acto (la salida
    // avisa gone en el acto). Stop(): ROB-02, sin avisar nada (tampoco gone); la llamada siguiente parte del estado final.
    // Nada de esto toca el dato ni cuándo cambia (AC-WKT-13-14). Sin asignaciones por cuadro.
    public sealed class SeriesColumnMotion : HudMotion
    {
        // Como SpringMotion: el resorte llega cuando le faltan ≤ 0,05 dp y va a ≤ 0,05 dp/s.
        private const double SettleDp = 0.05;
        private const double Epsilon = 1e-9;

        private readonly Action<SeriesColumnVisual> _apply;

        // El panel de repeticiones.
        private bool _repsOn;
        private bool _repsAppearing = true;
        private bool _repsReduced;
        private double _repsAt;
        private double _repsFromAlpha;
        private double _repsFromRise;
        private double _repsAlpha = 1;
        private double _repsRise;
        private Action? _gone;
        private bool _goneDue;

        // La columna que sigue a SPM.
        private bool _followOn;
        private bool _followGaining;
        private double _followAt;
        private double _followCarry;
        private double _follow;

        // La rueda.
        private bool _wheelOn;
        private double _wheelAt;
        private double _wheelFrom;
        private double _wheelV0;
        private double _wheelTo;
        private double _scroll;
        private double _wheelVelocity;

        private bool _stopped;
        private SeriesColumnVisual _visual = new SeriesColumnVisual(1, 0, 0, 0);
        private bool _hasVisual;
        private double _scale = 1;

        public SeriesColumnMotion(MotionDriver driver, Action<SeriesColumnVisual> apply)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        // sp: dp de ventana por base del panel (lo pone la vista; 1 por defecto). Solo para medir el resorte de la rueda en dp.
        public double Scale
        {
            get => _scale;
            set => _scale = value > 0 ? value : 1;
        }

        public SeriesColumnVisual Visual => _visual;

        // Adónde va el desplazamiento (base del panel): el destino de la rueda o donde está.
        public double ScrollTargetBase => _wheelOn ? _wheelTo : _scroll;

        // El panel de repeticiones se está yendo (todavía no avisó gone).
        public bool RepsLeaving => _repsOn && !_repsAppearing;

        public override bool IsFinite => IsRunning && AnyPhase;

        private bool AnyPhase => _repsOn || _followOn || _wheelOn;

        // ── La API (02-tech-design §4.2) ──

        public void RepsAppear(bool instant)
        {
            var now = Now;
            Prepare(now);
            // Una salida en curso se cancela sin avisar: el panel se queda.
            _gone = null;
            _goneDue = false;
            if (!CanAnimate(instant))
            {
                _repsOn = false;
                _repsAppearing = true;
                _repsAlpha = 1;
                _repsRise = 0;
                Commit(now);
                return;
            }

            if (_repsOn && _repsAppearing)
            {
                return;
            }

            if (!_repsOn && _repsAlpha >= 1 && _repsRise == 0)
            {
                // Ya está a la vista.
                return;
            }

            _repsOn = true;
            _repsAppearing = true;
            _repsAt = now;
            _repsReduced = ReduceMotion;
            _repsFromAlpha = _repsAlpha;
            _repsFromRise = _repsRise;
            Commit(now);
        }

        public void RepsLeave(bool instant, Action gone)
        {
            if (gone == null)
            {
                throw new ArgumentNullException(nameof(gone));
            }

            var now = Now;
            Prepare(now);
            if (!CanAnimate(instant) || (!_repsOn && _repsAlpha <= 0))
            {
                _repsOn = false;
                _repsAppearing = false;
                _repsAlpha = 0;
                _repsRise = 0;
                _gone = null;
                _goneDue = false;
                Commit(now);
                gone();
                return;
            }

            _gone = gone;
            if (_repsOn && !_repsAppearing)
            {
                return;
            }

            _repsOn = true;
            _repsAppearing = false;
            _repsAt = now;
            _repsReduced = ReduceMotion;
            _repsFromAlpha = _repsAlpha;
            _repsFromRise = _repsRise;
            Commit(now);
        }

        // gaining: SPM gana obj. (crece: la columna baja); si no, lo pierde (la columna sube).
        public void FollowSpm(bool gaining, bool instant)
        {
            var now = Now;
            Prepare(now);
            if (!CanAnimate(instant) || ReduceMotion)
            {
                _followOn = false;
                _follow = 0;
                Commit(now);
                return;
            }

            _followCarry = _follow;
            _followOn = true;
            _followGaining = gaining;
            _followAt = now;
            Commit(now);
        }

        public void WheelTo(double offsetBase, bool instant)
        {
            var now = Now;
            Prepare(now);
            if (!CanAnimate(instant) || ReduceMotion)
            {
                _wheelOn = false;
                _scroll = offsetBase;
                _wheelTo = offsetBase;
                _wheelVelocity = 0;
                Commit(now);
                return;
            }

            if (_wheelOn ? Math.Abs(offsetBase - _wheelTo) <= Epsilon : Math.Abs(offsetBase - _scroll) <= Epsilon)
            {
                NotifyGone();
                return;
            }

            _wheelFrom = _scroll;
            _wheelV0 = _wheelOn ? _wheelVelocity : 0;
            _wheelTo = offsetBase;
            _wheelAt = now;
            _wheelOn = true;
            Commit(now);
        }

        // En el acto (DD5-27): el desplazamiento y, si la rueda corre, su origen y su destino se corren lo mismo.
        public void ShiftScroll(double deltaBase)
        {
            var now = Now;
            Prepare(now);
            _scroll += deltaBase;
            if (_wheelOn)
            {
                _wheelFrom += deltaBase;
                _wheelTo += deltaBase;
            }
            else
            {
                _wheelTo = _scroll;
            }

            Commit(now);
        }

        // ── El cuadro ──

        protected override bool Advance(double nowSeconds)
        {
            if (ReduceMotion)
            {
                FinishForReducedMotion();
            }

            Emit(nowSeconds);
            var keep = AnyPhase;
            NotifyGone();
            return keep;
        }

        protected override void OnStopped()
        {
            // ROB-02: nada sigue y no avisa nada (tampoco gone); lo que se ve queda donde estaba.
            _repsOn = false;
            _followOn = false;
            _wheelOn = false;
            _gone = null;
            _goneDue = false;
            _stopped = true;
        }

        // ── Lo de cada llamada ──

        private void Prepare(double now)
        {
            if (_stopped)
            {
                // Después de Stop, la llamada siguiente parte del estado final de lo que corría.
                _stopped = false;
                _repsAlpha = _repsAppearing ? 1 : 0;
                _repsRise = 0;
                _follow = 0;
                _scroll = _wheelTo;
                _wheelVelocity = 0;
            }

            Sync(now);
        }

        private void FinishForReducedMotion()
        {
            if (_followOn)
            {
                _followOn = false;
                _follow = 0;
            }

            if (_wheelOn)
            {
                _wheelOn = false;
                _scroll = _wheelTo;
                _wheelVelocity = 0;
            }

            _repsReduced |= _repsOn;
        }

        // El final de cada llamada: el cuadro aplicado, dentro o fuera del MotionDriver, y el aviso de la salida si terminó.
        private void Commit(double now)
        {
            Emit(now);
            RunOrLeave();
            NotifyGone();
        }

        // El aviso de la salida, después de dejar el cuadro aplicado (puede volver a arrancarlo: HudMotion lo tiene en cuenta).
        private void NotifyGone()
        {
            if (!_goneDue)
            {
                return;
            }

            _goneDue = false;
            var gone = _gone;
            _gone = null;
            gone?.Invoke();
        }

        private void RunOrLeave()
        {
            if (AnyPhase)
            {
                Run();
            }
            else
            {
                Leave();
            }
        }

        private void Emit(double now)
        {
            Sync(now);
            var visual = new SeriesColumnVisual(_repsAlpha, _repsRise, _follow, _scroll);
            if (_hasVisual && visual.RepsAlpha == _visual.RepsAlpha && visual.RepsRiseBase == _visual.RepsRiseBase
                && visual.FollowBase == _visual.FollowBase && visual.ScrollBase == _visual.ScrollBase)
            {
                return;
            }

            _hasVisual = true;
            _visual = visual;
            _apply(visual);
        }

        private void Sync(double now)
        {
            SyncReps(now);
            SyncFollow(now);
            SyncWheel(now);
        }

        private void SyncReps(double now)
        {
            if (!_repsOn)
            {
                return;
            }

            var t = HudTimelines.Ms(now - _repsAt);
            if (HudTimelines.Reached(t, SeriesTimelines.RepsPanelEndMs(_repsAppearing)))
            {
                _repsOn = false;
                _repsAlpha = _repsAppearing ? 1 : 0;
                _repsRise = 0;
                _goneDue = !_repsAppearing && _gone != null;
                return;
            }

            var fade = SeriesTimelines.RepsPanel(t, _repsAppearing, _repsReduced);
            if (!_repsAppearing)
            {
                // Se va sin moverse, desde la opacidad que tiene.
                _repsAlpha = _repsFromAlpha * fade.Opacity;
                _repsRise = _repsFromRise;
                return;
            }

            if (_repsFromAlpha <= Epsilon)
            {
                _repsAlpha = fade.Opacity;
                _repsRise = fade.RiseBase;
                return;
            }

            // Una salida que se cortó: sigue desde su opacidad y su subida (la de la entrada es su propia curva, 0 → 1).
            _repsAlpha = _repsFromAlpha + (1 - _repsFromAlpha) * fade.Opacity;
            _repsRise = _repsReduced ? 0 : _repsFromRise * (1 - fade.Opacity);
        }

        private void SyncFollow(double now)
        {
            if (!_followOn)
            {
                return;
            }

            var t = HudTimelines.Ms(now - _followAt);
            if (HudTimelines.Reached(t, SeriesTimelines.ColumnFollowEndMs(_followGaining, false)))
            {
                _followOn = false;
                _follow = 0;
                return;
            }

            // Lo que le faltaba al anterior se apaga con la misma curva.
            var value = SeriesTimelines.ColumnFollow(t, _followGaining, false);
            var start = SeriesTimelines.ColumnFollow(0, _followGaining, false);
            _follow = value + (Math.Abs(start) > Epsilon ? _followCarry * value / start : 0);
        }

        private void SyncWheel(double now)
        {
            if (!_wheelOn)
            {
                return;
            }

            var t = HudTimelines.Ms(now - _wheelAt);
            var value = SeriesTimelines.Wheel(t, _wheelFrom, _wheelTo, _wheelV0, false);
            if (Math.Abs(value.Value - _wheelTo) * Scale <= SettleDp && Math.Abs(value.Velocity) * Scale <= SettleDp)
            {
                _wheelOn = false;
                _scroll = _wheelTo;
                _wheelVelocity = 0;
                return;
            }

            _scroll = value.Value;
            _wheelVelocity = value.Velocity;
        }
    }
}
