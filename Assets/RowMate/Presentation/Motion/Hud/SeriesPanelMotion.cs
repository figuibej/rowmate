using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Workout;

namespace RowMate.Presentation.Motion.Hud
{
    // Un panel de series (parciales o repeticiones) en movimiento (05-series-parciales/02-tech-design §4.2, §7.4; 04-design-notes
    // §6.1 a §6.3, §6.8 y §6.10; AC-WKT-18-18, -19; AC-APP-05-08). Con el contrato de la 4b (HudMotion): no conoce la vista; recibe
    // el MotionDriver y un callback con un SeriesPanelVisual (SeriesTimelines, WP-A), que la vista dibuja; nunca escribe estilos ni
    // layout; lee Armed, ReduceMotion e Instant en cada llamada; pasa el tiempo a ms enteros; no asigna por cuadro; Stop() lo saca
    // del MotionDriver en el acto. Nada de esto decide cuándo cambia un valor (AC-WKT-13-14, AC-WKT-18-08): el dato ya cambió
    // cuando la vista llama; esto es solo cómo se ve.
    //
    // Dos clases de movimiento (SD-12):
    // - lo que hace el usuario (SetExpanded, y Retarget sin Instant) va con spring.smooth desde la posición y la velocidad que
    //   tenga el alto: un toque en medio invierte el resorte sin saltos, y los fundidos y la flecha siguen desde su valor;
    // - lo que cambia el dato (ContentChanged) va con curvas de duración fija que terminan antes del segundo siguiente: el alto
    //   con el empuje (DD5-16), las filas que bajan y la que entra con su marca (DD5-17), lo que cambia de clase por pieza (DD5-18)
    //   y la línea Rep que entra o sale con la serie.
    //
    // La cola (TailShiftBase, la línea Rep y la ventana de filas): 0 al abrir o cerrar (la revela el vidrio); G − H_final por un
    // dato y con Retarget. Si llega un dato con el resorte corriendo, el resorte toma el alto nuevo desde donde está y las filas y
    // la cola nunca van más rápido que el vidrio: corrimiento = máx(mín(empuje; G − H_final); el corrimiento de partida) (§4.1). La
    // cota de abajo es de D1: sin ella, en una apertura el corrimiento saltaría |G − H_anterior| en el cuadro del dato (D1-03).
    //
    // Con el panel cerrado no se anima nada del contenido: un dato solo deja el alto final; al abrir, la lista ya está, sin marcas
    // ni empuje (04-design-notes §6.0). Repeticiones: con la serie que empieza aparece en su estado guardado (en el acto; la entrada
    // la hace SeriesColumnMotion) y con la que termina no cambia nada (se va con la columna, sin moverse).
    //
    // Fases, todas finitas (F): el resorte del alto, el alto por un dato, los fundidos y la flecha de abrir y cerrar, el empuje y
    // la fila que entra o sale, las piezas que cambian, la línea Rep, cada marca hasta sus 1400 y la capa de estado. Sin fases,
    // sale del MotionDriver. ISettlingMotion: RemainingDp es lo que le falta al resorte del alto (× Scale, en dp) si es lo único
    // que corre, e infinito si además corre una fase de duración fija (así la regla de toma de la 4b bloquea sin conocer esta
    // motion); HeightRemainingDp e IsTimedPhaseRunning, por separado, para la regla de toma de la 5 (WP-B3).
    //
    // Reducir movimiento se lee al empezar cada fase y en cada cuadro (QA-V4b-MOT-03): si se prende con algo en camino, lo que
    // se mueve (el resorte, el alto por un dato, el empuje, los fundidos de abrir y cerrar) va a su final en el acto, las subidas
    // de las piezas y de la línea Rep van a 0 y las marcas y la capa de estado siguen (son opacidad).
    // Sin armar o con Instant: el estado final en el acto, sin empuje, sin marcas y sin piezas (AC-WKT-02-08), con la capa de
    // estado en su objetivo. Stop(): ROB-02; lo que se ve queda donde estaba, salvo la capa de estado, que va a 0 (§6.10), sin
    // avisar nada; la llamada siguiente parte del estado final (QA-G5T-12: al volver a un panel, la vista llama con Instant).
    public sealed class SeriesPanelMotion : HudMotion, ISettlingMotion
    {
        // Como SpringMotion: el resorte llega cuando le faltan ≤ 0,05 dp y va a ≤ 0,05 dp/s.
        private const double SettleDp = 0.05;
        private const double Epsilon = 1e-9;
        private const int MarkerSlots = 4;

        private readonly Action<SeriesPanelVisual> _apply;
        private readonly bool[] _markerOn = new bool[MarkerSlots];
        private readonly bool[] _markerReduced = new bool[MarkerSlots];
        private readonly double[] _markerAt = new double[MarkerSlots];
        private readonly long[] _markerDelayMs = new long[MarkerSlots];
        private readonly double[] _marker = new double[MarkerSlots];

        private bool _expanded;
        private double _final = SeriesPanelGeometry.ChipHeight;
        private double _height = SeriesPanelGeometry.ChipHeight;
        private double _velocity;
        private bool _stopped;

        // El alto con el resorte (abrir, cerrar, su inversión y Retarget).
        private bool _springOn;
        private bool _springReduced;
        private double _springAt;
        private double _springFrom;
        private double _springV0;
        private long _springDelayMs;
        private long _springHoldMs;
        private bool _tailFollowsGlass;
        private bool _dataDuringSpring;
        private double _tailFloor;
        private double _rowsFloor;

        // El corrimiento por un dato (DD5-16): la cola y, sin resorte, el alto (G = H_final + corrimiento).
        private bool _shiftOn;
        private bool _shiftReduced;
        private bool _shiftDrivesHeight;
        private double _shiftAt;
        private double _shiftFrom;
        private double _shift;

        // Abrir y cerrar: los fundidos y la flecha.
        private bool _toggleOn;
        private bool _toggleReduced;
        private bool _opening;
        private double _toggleAt;
        private PanelToggleStart _toggleStart;
        private double _content;
        private double _label = 1;
        private double _title;
        private double _chevron;
        private double _chevronDown = 1;
        private double _chevronUp;

        // Las filas: el empuje, la que entra y la que sale (DD5-17, DD5-24).
        private bool _rowsOn;
        private bool _rowsReduced;
        private bool _rowsDropped;
        private int _rowsNew;
        private double _rowsAt;
        private double _rowsCarry;
        private double _push;
        private double _pushProgress = 1;
        private double _newAlpha = 1;
        private double _newShift;
        private double _dropped;

        // Lo que cambia de clase, por pieza (DD5-18), y las filas que se vacían.
        private bool _swapOn;
        private bool _swapReduced;
        private bool _swapClass;
        private double _swapAt;
        private double _bodyOld;
        private double _bodyNew = 1;
        private double _bodyRise;
        private double _cleared;

        // La línea Rep que entra o sale con la serie (parciales).
        private bool _lineOn;
        private bool _lineReduced;
        private bool _lineAppearing;
        private double _lineAt;
        private double _lineAlpha = 1;
        private double _lineRise;

        // La capa de estado: hover y presionado (DD5-28).
        private bool _hoverOn;
        private double _hoverAt;
        private double _hoverFrom;
        private double _hoverTo;
        private double _hover;

        private SeriesPanelVisual _visual;
        private bool _hasVisual;
        private double _scale = 1;

        public SeriesPanelMotion(MotionDriver driver, SeriesPanelKind kind, Action<SeriesPanelVisual> apply)
            : base(driver)
        {
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Kind = kind;
            _visual = Compose();
        }

        public SeriesPanelKind Kind { get; }

        // sp: dp de ventana por base del panel (lo pone la vista con la geometría; 1 por defecto). Solo para medir el resorte en dp.
        public double Scale
        {
            get => _scale;
            set => _scale = value > 0 ? value : 1;
        }

        // El estado lógico (el de la preferencia) y el alto final que recibió (base).
        public bool Expanded => _expanded;

        public double TargetHeightBase => _final;

        // Lo último que se le pasó a la vista.
        public SeriesPanelVisual Visual => _visual;

        public override bool IsFinite => IsRunning && AnyPhase;

        // Corre una fase de duración fija (todo salvo el resorte del alto).
        public bool IsTimedPhaseRunning => IsRunning && TimedPhase;

        // Lo que le falta al resorte del alto, en dp (0 sin resorte).
        public double HeightRemainingDp => IsRunning && _springOn && !_springReduced ? Math.Abs(_final - _height) * Scale : 0;

        // ISettlingMotion (la regla de toma de la 4b): el resorte solo, en dp; con una fase de duración fija en curso, infinito.
        public double RemainingDp
        {
            get
            {
                if (!IsRunning)
                {
                    return 0;
                }

                return TimedPhase ? double.PositiveInfinity : HeightRemainingDp;
            }
        }

        private bool AnyPhase => _springOn || TimedPhase;

        private bool TimedPhase => (_springOn && _springReduced) || _shiftOn || _toggleOn || _rowsOn || _swapOn || _lineOn || _hoverOn || AnyMarker;

        private bool AnyMarker
        {
            get
            {
                for (var i = 0; i < MarkerSlots; i++)
                {
                    if (_markerOn[i])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // ── La API (02-tech-design §4.2) ──

        // Abrir o cerrar (el toque, la tecla, la preferencia leída tarde): el alto con el resorte hacia targetHeightBase (el final
        // de Place; cerrado, el chip), los fundidos y la flecha. Desde una animación en curso, la invierte desde donde está.
        public void SetExpanded(bool expanded, double targetHeightBase, bool instant)
        {
            if (!CanAnimate(instant))
            {
                _expanded = expanded;
                GoToRest(targetHeightBase);
                return;
            }

            var now = Now;
            Prepare(now);
            if (expanded == _expanded)
            {
                RetargetAt(now, targetHeightBase);
                return;
            }

            var reduced = ReduceMotion;
            var velocity = CurrentVelocity(now);
            _toggleStart = new PanelToggleStart(_height, velocity, _content, _label, _title, _chevron, _chevronDown, _chevronUp);
            _expanded = expanded;
            if (expanded)
            {
                ClearContent();
            }

            _shiftDrivesHeight = false;
            _toggleOn = true;
            _toggleAt = now;
            _toggleReduced = reduced;
            _opening = expanded;
            StartSpring(now, _height, velocity, targetHeightBase, reduced, expanded);
            Emit(now);
            RunOrLeave();
        }

        // El contenido cambió (el segundo del motor; P-01): lo que cambió, pieza por pieza, y el alto final nuevo.
        public void ContentChanged(in HudSeriesChange change, double targetHeightBase, bool instant)
        {
            if (Kind == SeriesPanelKind.Reps && change.SeriesEnded)
            {
                // Se va con la columna (SeriesColumnMotion.RepsLeave), sin moverse.
                return;
            }

            if (!CanAnimate(instant) || (Kind == SeriesPanelKind.Reps && change.SeriesStarted))
            {
                GoToRest(targetHeightBase);
                return;
            }

            var now = Now;
            Prepare(now);
            if (!_expanded)
            {
                // Cerrado (o cerrándose): el chip no cambia y no empieza nada del contenido (lo que ya corría se apaga con él);
                // al abrir, la lista ya está.
                RetargetAt(now, targetHeightBase);
                return;
            }

            var reduced = ReduceMotion;
            var tailNow = CurrentTail();
            var rowsNow = CurrentRows();
            if (Kind == SeriesPanelKind.Laps)
            {
                LapsChanged(now, in change, reduced, rowsNow);
            }
            else
            {
                RepsChanged(now, in change, reduced, rowsNow);
            }

            if (Math.Abs(targetHeightBase - _final) > Epsilon)
            {
                // El empuje de partida de las filas: el de las que acaban de entrar (t = 0) o el que tenían.
                var rowsStart = _rowsOn && _rowsAt == now
                    ? SeriesTimelines.RowsChange(0, _rowsNew, _rowsDropped, _rowsReduced).RowsPushBase + _rowsCarry
                    : rowsNow;
                StartData(now, targetHeightBase, tailNow, rowsStart, reduced);
            }

            Emit(now);
            RunOrLeave();
        }

        // La capa de estado (04-design-notes §6.10): presionado 0,08, hover 0,04, si no 0; desde el alfa que tiene, 90 al subir
        // (ease.enter) y 160 al bajar (ease.exit), igual con reducir movimiento. Con Instant (o sin armar), su objetivo en el acto.
        public void Hover(bool hovered, bool pressed, bool instant)
        {
            var target = pressed ? MotionSpecs.SeriesPressedAlpha : hovered ? MotionSpecs.SeriesHoverAlpha : 0.0;
            var now = Now;
            Prepare(now);
            if (!CanAnimate(instant) || _hover == target)
            {
                _hoverOn = false;
                _hover = target;
                _hoverTo = target;
                Emit(now);
                RunOrLeave();
                return;
            }

            if (_hoverOn && _hoverTo == target)
            {
                return;
            }

            _hoverOn = true;
            _hoverAt = now;
            _hoverFrom = _hover;
            _hoverTo = target;
            Emit(now);
            RunOrLeave();
        }

        // Otro alto final sin abrir ni cerrar. Con Instant (la rotación, el segundo plano, el panel que vuelve a un panel de UI
        // Toolkit): todo a su estado final en el acto. Si no (el reparto de RowsScroll cuando el otro panel se abre o se cierra):
        // el resorte desde donde está, con la cola pegada al vidrio.
        public void Retarget(double heightBase, bool instant)
        {
            if (!CanAnimate(instant))
            {
                GoToRest(heightBase);
                return;
            }

            var now = Now;
            Prepare(now);
            RetargetAt(now, heightBase);
        }

        // ── El cuadro ──

        protected override bool Advance(double nowSeconds)
        {
            if (ReduceMotion)
            {
                FinishForReducedMotion();
            }

            Emit(nowSeconds);
            return AnyPhase;
        }

        protected override void OnStopped()
        {
            // ROB-02: nada sigue; lo que se ve queda donde estaba, salvo la capa de estado (0 en el acto). Sin avisos.
            _springOn = false;
            _shiftOn = false;
            _shiftDrivesHeight = false;
            _toggleOn = false;
            _rowsOn = false;
            _swapOn = false;
            _lineOn = false;
            _hoverOn = false;
            for (var i = 0; i < MarkerSlots; i++)
            {
                _markerOn[i] = false;
            }

            _tailFollowsGlass = false;
            _dataDuringSpring = false;
            _hover = 0;
            _hoverTo = 0;
            _stopped = true;
            _visual = Compose();
        }

        // ── Lo de cada llamada ──

        // El estado de este instante, antes de cambiar algo. Después de Stop, la llamada siguiente parte del estado final.
        private void Prepare(double now)
        {
            if (_stopped)
            {
                _stopped = false;
                ResetToRest(_final);
            }

            Sync(now);
        }

        private void RetargetAt(double now, double heightBase)
        {
            if (Math.Abs(heightBase - _final) > Epsilon)
            {
                if (ReduceMotion)
                {
                    EndSpring();
                    EndShift();
                    _final = heightBase;
                }
                else if (_springOn)
                {
                    RetargetSpring(now, heightBase);
                    if (!_toggleOn)
                    {
                        _tailFollowsGlass = true;
                        _dataDuringSpring = false;
                    }
                }
                else
                {
                    var velocity = CurrentVelocity(now);
                    EndShift();
                    StartSpring(now, _height, velocity, heightBase, false, true);
                    _tailFollowsGlass = true;
                }
            }

            Emit(now);
            RunOrLeave();
        }

        // Parciales: las vueltas nuevas (con su marca), las filas que se vacían y la pieza que cambia de clase con el paso, y la
        // línea Rep con la serie.
        private void LapsChanged(double now, in HudSeriesChange change, bool reduced, double rowsNow)
        {
            var k = Math.Min(change.NewLaps, SeriesPanels.VisibleLaps);
            if (k > 0)
            {
                if (change.StepChanged)
                {
                    // Las del paso anterior se vacían: sus marcas no pasan a las nuevas.
                    ClearMarkers();
                }

                AddMarkers(now, k, 0, reduced, SeriesPanels.VisibleLaps);
                StartRows(now, k, !change.StepChanged && change.LapDropped, reduced, rowsNow);
            }

            var changesClass = change.LapsBody == SeriesBodyChange.ToEmpty || change.LapsBody == SeriesBodyChange.FromEmpty;
            if (changesClass || change.ClearedLaps > 0)
            {
                StartSwap(now, changesClass, reduced);
            }

            if (change.SeriesStarted || change.SeriesEnded)
            {
                _lineOn = true;
                _lineAt = now;
                _lineAppearing = change.SeriesStarted;
                _lineReduced = reduced;
            }
        }

        // Repeticiones: la nueva al frente de la lista (con su marca), o la primera, que reemplaza al vacío (con su marca, que
        // entra con ella en 120-280), o la lista que se vacía.
        private void RepsChanged(double now, in HudSeriesChange change, bool reduced, double rowsNow)
        {
            var changesClass = change.RepsBody == SeriesBodyChange.ToEmpty || change.RepsBody == SeriesBodyChange.FromEmpty;
            if (change.NewRep)
            {
                if (change.RepsBody == SeriesBodyChange.FromEmpty)
                {
                    // La marca entra con su fila: la del cambio de pieza (120-280) y no la de la fila nueva (80-240).
                    var delay = reduced
                        ? (long)MotionSpecs.SeriesSwapInStartMs
                        : (long)(MotionSpecs.SeriesSwapInStartMs - MotionSpecs.SeriesRowInStartMs);
                    AddMarkers(now, 1, delay, reduced, SeriesTracker.MaxPreviousReps);
                }
                else
                {
                    AddMarkers(now, 1, 0, reduced, SeriesTracker.MaxPreviousReps);
                    StartRows(now, 1, change.RepDropped, reduced, rowsNow);
                }
            }

            if (changesClass)
            {
                StartSwap(now, true, reduced);
            }
        }

        // El alto que cambia por un dato. Sin resorte: con el empuje (crece en 0-160, se achica en 80-240), y la cola con él. Con
        // el resorte corriendo: el resorte toma el alto nuevo desde donde está y la cola y las filas siguen al vidrio sin pasarlo.
        private void StartData(double now, double target, double tailNow, double rowsStart, bool reduced)
        {
            if (_springOn && !_springReduced)
            {
                // La cola, relativa al final nuevo: donde está ahora (su cota de abajo: nunca sube de ahí).
                var from = tailNow + (_final - target);
                RetargetSpring(now, target);
                _tailFollowsGlass = false;
                _dataDuringSpring = true;
                _tailFloor = from;
                _rowsFloor = rowsStart;
                StartShift(now, from, reduced, false);
                return;
            }

            if (_springOn)
            {
                // El alto de reducir movimiento ya llegó (abrir) o salta a los 120 (cerrar, que no llega acá): al final nuevo.
                EndSpring();
            }

            var shift = _height - target;
            _final = target;
            StartShift(now, shift, reduced, true);
        }

        // ── Las fases ──

        private void StartSpring(double now, double from, double velocity, double to, bool reduced, bool opening)
        {
            _final = to;
            _springOn = true;
            _springAt = now;
            _springFrom = from;
            _springV0 = reduced ? 0 : velocity;
            _springReduced = reduced;
            _springDelayMs = !reduced && !opening && Math.Abs(velocity) <= Epsilon ? (long)MotionSpecs.SeriesShrinkDelayMs : 0;
            _springHoldMs = reduced && !opening ? (long)MotionSpecs.SeriesReducedOutMs : 0;
            _tailFollowsGlass = false;
            _dataDuringSpring = false;
        }

        // El resorte sigue hacia otro final desde su posición y su velocidad (en la demora del cierre, con lo que le falta).
        private void RetargetSpring(double now, double to)
        {
            if (_springReduced)
            {
                _final = to;
                return;
            }

            var t = HudTimelines.Ms(now - _springAt);
            var delay = Math.Max(0, _springDelayMs - t);
            _springFrom = _height;
            _springV0 = _velocity;
            _springAt = now;
            _springDelayMs = delay;
            _final = to;
        }

        private void EndSpring()
        {
            _springOn = false;
            _height = _final;
            _velocity = 0;
            _tailFollowsGlass = false;
            _dataDuringSpring = false;
        }

        private void StartShift(double now, double from, bool reduced, bool drivesHeight)
        {
            if (Math.Abs(from) <= Epsilon)
            {
                EndShift();
                return;
            }

            _shiftOn = true;
            _shiftAt = now;
            _shiftFrom = from;
            _shiftReduced = reduced;
            _shiftDrivesHeight = drivesHeight;
            _shift = from;
        }

        private void EndShift()
        {
            _shiftOn = false;
            _shiftDrivesHeight = false;
            _shift = 0;
        }

        private void StartRows(double now, int newRows, bool dropped, bool reduced, double rowsNow)
        {
            _rowsOn = true;
            _rowsAt = now;
            _rowsNew = newRows;
            _rowsDropped = dropped;
            _rowsReduced = reduced;
            // Lo que les faltaba bajar a las de antes sigue con el empuje nuevo (dos datos seguidos: sin saltos).
            _rowsCarry = reduced ? 0 : rowsNow;
        }

        private void StartSwap(double now, bool changesClass, bool reduced)
        {
            _swapOn = true;
            _swapAt = now;
            _swapClass = changesClass;
            _swapReduced = reduced;
        }

        // k filas nuevas al frente: las marcas que había bajan k lugares (las que salen de la vista se van) y las nuevas entran
        // con su fila.
        private void AddMarkers(double now, int k, long delayMs, bool reduced, int slots)
        {
            for (var i = MarkerSlots - 1; i >= 0; i--)
            {
                var from = i - k;
                if (i < slots && from >= 0)
                {
                    _markerOn[i] = _markerOn[from];
                    _markerAt[i] = _markerAt[from];
                    _markerDelayMs[i] = _markerDelayMs[from];
                    _markerReduced[i] = _markerReduced[from];
                    _marker[i] = _marker[from];
                }
                else
                {
                    _markerOn[i] = i < k && i < slots;
                    _markerAt[i] = now;
                    _markerDelayMs[i] = delayMs;
                    _markerReduced[i] = reduced;
                    _marker[i] = 0;
                }
            }
        }

        private void ClearMarkers()
        {
            for (var i = 0; i < MarkerSlots; i++)
            {
                _markerOn[i] = false;
                _marker[i] = 0;
            }
        }

        // Sin animaciones de contenido: el cerrado, la apertura (la lista ya está) y el estado final.
        private void ClearContent()
        {
            _rowsOn = false;
            _rowsCarry = 0;
            _rowsNew = 0;
            _push = 0;
            _pushProgress = 1;
            _newAlpha = 1;
            _newShift = 0;
            _dropped = 0;
            ClearMarkers();
            _swapOn = false;
            _bodyOld = 0;
            _bodyNew = 1;
            _bodyRise = 0;
            _cleared = 0;
            _lineOn = false;
            _lineAlpha = 1;
            _lineRise = 0;
            EndShift();
            _dataDuringSpring = false;
        }

        private void ResetToRest(double heightBase)
        {
            _final = heightBase;
            _height = heightBase;
            _velocity = 0;
            _springOn = false;
            _toggleOn = false;
            _tailFollowsGlass = false;
            RestFades();
            ClearContent();
            _hoverOn = false;
            _hover = _hoverTo;
        }

        // Todo en su estado final en el acto y fuera del MotionDriver (Instant, sin armar, la rotación).
        private void GoToRest(double heightBase)
        {
            _stopped = false;
            Leave();
            ResetToRest(heightBase);
            Emit(Now);
        }

        private void RestFades()
        {
            _content = _expanded ? 1 : 0;
            _label = _expanded ? 0 : 1;
            _title = _expanded ? 1 : 0;
            _chevron = _expanded ? 180 : 0;
            _chevronDown = 1;
            _chevronUp = 0;
        }

        // Reducir movimiento se prendió con algo en camino: lo que se mueve, a su final (QA-V4b-MOT-03).
        private void FinishForReducedMotion()
        {
            if (_springOn && !_springReduced)
            {
                EndSpring();
            }

            if (_toggleOn && !_toggleReduced)
            {
                _toggleOn = false;
                RestFades();
            }

            if (_shiftOn && !_shiftReduced)
            {
                EndShift();
            }

            if (_rowsOn && !_rowsReduced)
            {
                _rowsOn = false;
                _rowsCarry = 0;
            }

            _swapReduced |= _swapOn;
            _lineReduced |= _lineOn;
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

        // ── La evaluación ──

        private void Emit(double now)
        {
            Sync(now);
            var visual = Compose();
            if (_hasVisual && Same(in visual, in _visual))
            {
                return;
            }

            _hasVisual = true;
            _visual = visual;
            _apply(visual);
        }

        private void Sync(double now)
        {
            SyncShift(now);
            if (_springOn)
            {
                SyncSpring(now);
            }

            if (!_springOn)
            {
                _height = _final + (_shiftDrivesHeight ? _shift : 0);
                _velocity = 0;
            }

            SyncToggle(now);
            SyncRows(now);
            SyncMarkers(now);
            SyncSwap(now);
            SyncLine(now);
            SyncHover(now);
            if (!_expanded && _content <= 0 && (_rowsOn || _swapOn || _lineOn || _shiftOn || AnyMarker))
            {
                // Cerrándose: cuando el contenido ya se fue, nada de él sigue.
                ClearContent();
            }
        }

        private void SyncSpring(double now)
        {
            var t = HudTimelines.Ms(now - _springAt);
            if (_springReduced)
            {
                if (HudTimelines.Reached(t, _springHoldMs))
                {
                    EndSpring();
                }
                else
                {
                    _height = _springFrom;
                    _velocity = 0;
                }

                return;
            }

            if (t <= _springDelayMs)
            {
                _height = _springFrom;
                _velocity = _springV0;
                return;
            }

            var value = SeriesTimelines.Spring(_springFrom, _springV0, _final, MotionSpecs.SeriesHeightOmega, (t - _springDelayMs) / 1000.0);
            if (Math.Abs(value.Value - _final) * Scale <= SettleDp && Math.Abs(value.Velocity) * Scale <= SettleDp)
            {
                EndSpring();
                return;
            }

            _height = value.Value;
            _velocity = value.Velocity;
        }

        private void SyncShift(double now)
        {
            if (!_shiftOn)
            {
                _shift = 0;
                return;
            }

            var t = HudTimelines.Ms(now - _shiftAt);
            if (HudTimelines.Reached(t, SeriesTimelines.DataResizeEndMs(_shiftFrom < 0, _shiftReduced)))
            {
                EndShift();
                return;
            }

            _shift = SeriesTimelines.DataResize(t, _shiftFrom, 0, _shiftReduced);
        }

        private void SyncToggle(double now)
        {
            if (!_toggleOn)
            {
                return;
            }

            var t = HudTimelines.Ms(now - _toggleAt);
            if (HudTimelines.Reached(t, SeriesTimelines.ToggleFadesEndMs(_toggleReduced)))
            {
                _toggleOn = false;
                RestFades();
                return;
            }

            var visual = _opening
                ? SeriesTimelines.Open(t, in _toggleStart, _final, _toggleReduced)
                : SeriesTimelines.Close(t, in _toggleStart, _final, _toggleReduced);
            _content = visual.ContentAlpha;
            _label = visual.ChipLabelAlpha;
            _title = visual.TitleAlpha;
            _chevron = visual.ChevronDegrees;
            _chevronDown = visual.ChevronDownAlpha;
            _chevronUp = visual.ChevronUpAlpha;
        }

        private void SyncRows(double now)
        {
            if (_rowsOn)
            {
                var t = HudTimelines.Ms(now - _rowsAt);
                if (!HudTimelines.Reached(t, SeriesTimelines.RowsChangeEndMs(_rowsReduced)))
                {
                    var rows = SeriesTimelines.RowsChange(t, _rowsNew, _rowsDropped, _rowsReduced);
                    _push = rows.RowsPushBase + _rowsCarry * (1 - rows.PushProgress);
                    _pushProgress = rows.PushProgress;
                    _newAlpha = rows.NewRowsAlpha;
                    _newShift = rows.NewRowsShiftBase;
                    _dropped = _rowsDropped ? rows.DroppedAlpha : 0;
                    return;
                }

                _rowsOn = false;
                _rowsCarry = 0;
            }

            _push = 0;
            _pushProgress = 1;
            _newAlpha = 1;
            _newShift = 0;
            _dropped = 0;
        }

        private void SyncMarkers(double now)
        {
            for (var i = 0; i < MarkerSlots; i++)
            {
                if (!_markerOn[i])
                {
                    _marker[i] = 0;
                    continue;
                }

                var t = HudTimelines.Ms(now - _markerAt[i]) - _markerDelayMs[i];
                if (HudTimelines.Reached(t, SeriesTimelines.MarkerEndMs))
                {
                    _markerOn[i] = false;
                    _marker[i] = 0;
                    continue;
                }

                _marker[i] = SeriesTimelines.Marker(t, _markerReduced[i]);
            }
        }

        private void SyncSwap(double now)
        {
            if (_swapOn)
            {
                var t = HudTimelines.Ms(now - _swapAt);
                var end = _swapClass ? SeriesTimelines.SwapEndMs : (long)MotionSpecs.SeriesSwapOutMs;
                if (!HudTimelines.Reached(t, end))
                {
                    var swap = SeriesTimelines.Swap(t, _swapClass, _swapReduced);
                    _bodyOld = swap.OldAlpha;
                    _bodyNew = swap.NewAlpha;
                    _bodyRise = swap.NewRiseBase;
                    _cleared = swap.ClearedRowsAlpha;
                    return;
                }

                _swapOn = false;
            }

            _bodyOld = 0;
            _bodyNew = 1;
            _bodyRise = 0;
            _cleared = 0;
        }

        private void SyncLine(double now)
        {
            if (!_lineOn)
            {
                _lineRise = 0;
                return;
            }

            var t = HudTimelines.Ms(now - _lineAt);
            var end = _lineAppearing ? SeriesTimelines.SwapEndMs : (long)MotionSpecs.SeriesSwapOutMs;
            if (HudTimelines.Reached(t, end))
            {
                // Después de la salida queda en 0 hasta que la serie vuelva a empezar (o un estado final).
                _lineOn = false;
                _lineAlpha = _lineAppearing ? 1 : 0;
                _lineRise = 0;
                return;
            }

            var line = SeriesTimelines.RepLine(t, _lineAppearing, _lineReduced);
            _lineAlpha = line.Opacity;
            _lineRise = line.RiseBase;
        }

        private void SyncHover(double now)
        {
            if (!_hoverOn)
            {
                return;
            }

            var t = HudTimelines.Ms(now - _hoverAt);
            if (HudTimelines.Reached(t, SeriesTimelines.HoverEndMs(_hoverFrom, _hoverTo)))
            {
                _hoverOn = false;
                _hover = _hoverTo;
                return;
            }

            _hover = SeriesTimelines.Hover(t, _hoverFrom, _hoverTo);
        }

        // La velocidad del alto (base por segundo): la del resorte, o la del empuje por diferencia de un ms a cada lado.
        private double CurrentVelocity(double now)
        {
            if (_springOn)
            {
                return _velocity;
            }

            if (!_shiftOn || !_shiftDrivesHeight || _shiftReduced)
            {
                return 0;
            }

            var t = HudTimelines.Ms(now - _shiftAt);
            var before = SeriesTimelines.DataResize(t - 1, _shiftFrom, 0, false);
            var after = SeriesTimelines.DataResize(t + 1, _shiftFrom, 0, false);
            return (after - before) / 0.002;
        }

        // La cola de este instante (DD5-16; §4.1).
        private double CurrentTail()
        {
            if (_springOn && _expanded)
            {
                if (_tailFollowsGlass)
                {
                    return _height - _final;
                }

                if (_dataDuringSpring)
                {
                    return _tailFloor < 0 ? Math.Max(Math.Min(_shift, _height - _final), _tailFloor) : _shift;
                }
            }

            return _shift;
        }

        // El empuje de las filas de este instante (con el resorte y un dato, nunca más rápido que el vidrio).
        private double CurrentRows()
        {
            var push = _push;
            if (_springOn && _expanded && _dataDuringSpring)
            {
                push = Math.Max(Math.Min(push, _height - _final), _rowsFloor);
            }

            return push;
        }

        private SeriesPanelVisual Compose()
        {
            return new SeriesPanelVisual
            {
                HeightBase = _height,
                TailShiftBase = CurrentTail(),
                ContentAlpha = _content,
                ChipLabelAlpha = _label,
                TitleAlpha = _title,
                ChevronDegrees = _chevron,
                ChevronDownAlpha = _chevronDown,
                ChevronUpAlpha = _chevronUp,
                RowsPushBase = CurrentRows(),
                PushProgress = _pushProgress,
                NewRowsAlpha = _newAlpha,
                NewRowsShiftBase = _newShift,
                NewRows = _rowsOn ? _rowsNew : 0,
                DroppedAlpha = _dropped,
                ClearedRowsAlpha = _cleared,
                MarkerAlpha0 = _marker[0],
                MarkerAlpha1 = _marker[1],
                MarkerAlpha2 = _marker[2],
                MarkerAlpha3 = _marker[3],
                BodyOldAlpha = _bodyOld,
                BodyNewAlpha = _bodyNew,
                BodyNewRiseBase = _bodyRise,
                RepLineAlpha = _lineAlpha,
                RepLineRiseBase = _lineRise,
                HoverAlpha = _hover,
            };
        }

        // Sin Equals de ValueType (reflexión y boxing): campo por campo.
        private static bool Same(in SeriesPanelVisual a, in SeriesPanelVisual b)
        {
            return a.HeightBase == b.HeightBase && a.TailShiftBase == b.TailShiftBase && a.ContentAlpha == b.ContentAlpha
                   && a.ChipLabelAlpha == b.ChipLabelAlpha && a.TitleAlpha == b.TitleAlpha && a.ChevronDegrees == b.ChevronDegrees
                   && a.ChevronDownAlpha == b.ChevronDownAlpha && a.ChevronUpAlpha == b.ChevronUpAlpha && a.RowsPushBase == b.RowsPushBase
                   && a.PushProgress == b.PushProgress && a.NewRowsAlpha == b.NewRowsAlpha && a.NewRowsShiftBase == b.NewRowsShiftBase
                   && a.NewRows == b.NewRows && a.DroppedAlpha == b.DroppedAlpha && a.ClearedRowsAlpha == b.ClearedRowsAlpha
                   && a.MarkerAlpha0 == b.MarkerAlpha0 && a.MarkerAlpha1 == b.MarkerAlpha1 && a.MarkerAlpha2 == b.MarkerAlpha2
                   && a.MarkerAlpha3 == b.MarkerAlpha3 && a.BodyOldAlpha == b.BodyOldAlpha && a.BodyNewAlpha == b.BodyNewAlpha
                   && a.BodyNewRiseBase == b.BodyNewRiseBase && a.RepLineAlpha == b.RepLineAlpha && a.RepLineRiseBase == b.RepLineRiseBase
                   && a.HoverAlpha == b.HoverAlpha;
        }
    }
}
