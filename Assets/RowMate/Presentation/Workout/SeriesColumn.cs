using System;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Workout;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La columna de los paneles de series, debajo de SPM (05-series-parciales/02-tech-design §3.2, §3.3; 04-design-notes §5, §6.4 a
    // §6.7; DD5-03, DD5-26, DD5-27; DT5-14). No es un elemento: coloca los dos paneles (SeriesPanelView) con SeriesPanelGeometry
    // (Column por ventana y Place por estado), decide la serie que empieza o termina (el panel de repeticiones aparece en su estado
    // guardado o se va), la columna que sigue a SPM, el desplazamiento (RowsScroll: las filas de cada panel; ColumnScroll: la
    // columna entera, con la máscara de la regla 3; siempre dentro de su tope, que sigue sin saltar cuando el contenido se achica,
    // LM5-01; el foco del teclado trae el panel a la vista, LM5-02) y abrir o cerrar por la preferencia (ISeriesPanelsExpansion:
    // un toque guarda en el acto y la vista cambia con su State).
    // Es el ÚNICO que escribe style.translate y style.opacity de los paneles (DT5-14; QA-G5T-04): compone la cascada y la salida
    // (que le pasa la página), el SeriesColumnVisual (la serie que aparece o se va, el corrimiento que sigue a SPM, el
    // desplazamiento de ColumnScroll) y el FLIP del de abajo (el G de parciales menos su alto final, × sp). Así la caja del panel
    // va donde se ve y la selección de UI Toolkit lo encuentra ahí (S-B5). Nada se anima por layout: el layout va al estado final
    // en el cuadro del cambio.
    public sealed class SeriesColumn : IDisposable
    {
        private static readonly RepSummary[][] RepsOfCount = { new RepSummary[0], new RepSummary[1], new RepSummary[2], new RepSummary[3] };

        private readonly UiContext _ui;
        private readonly ISeriesPanelsExpansion _expansion;
        private readonly Action<bool, bool> _calloutsBlocked;
        private HudLayout? _layout;
        private SeriesColumnLayout _column;
        private SeriesPanelPlacement _lapsPlace;
        private SeriesPanelPlacement _repsPlace;
        private double _columnContent;
        private HudSeries? _series;
        private bool _spmHasTarget;
        private bool? _lastSpm;
        private SeriesPanelsExpanded _expanded;
        private bool _hasExpansion;
        private bool _repsShown;
        private bool _placed;
        private double _windowWidth = -1;
        private double _windowHeight = -1;
        private readonly double[] _cascadeOpacity = { 1, 1 };
        private readonly double[] _cascadeShift = { 0, 0 };
        private double _exit = 1;
        private bool _interactive;
        private bool _disposed;
        private readonly float[] _lastTranslate = { float.NaN, float.NaN };
        private readonly float[] _lastOpacity = { float.NaN, float.NaN };
        private float _repsLeaveY;

        public SeriesColumn(UiContext ui, ISeriesPanelsExpansion expansion, Action<bool, bool> calloutsBlocked)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _expansion = expansion ?? throw new ArgumentNullException(nameof(expansion));
            _calloutsBlocked = calloutsBlocked ?? throw new ArgumentNullException(nameof(calloutsBlocked));
            Laps = new SeriesPanelView(ui, SeriesPanelKind.Laps, OnActivate, OnPanelVisual);
            Reps = new SeriesPanelView(ui, SeriesPanelKind.Reps, OnActivate, OnPanelVisual);
            Reps.style.display = DisplayStyle.None;
            foreach (var panel in new[] { Laps, Reps })
            {
                panel.Dragged += OnDragged;
                panel.Wheeled += OnWheeled;
                panel.KeyboardFocused += OnKeyboardFocus;
            }

            Motion = new SeriesColumnMotion(ui.Motion, OnColumnVisual);
            // La preferencia: los dos inmersivos que conviven un instante (QA-V4a-01) ven lo mismo; el primer valor, en el acto.
            Laps.Bind(expansion.State, OnExpansion);
        }

        public SeriesPanelView Laps { get; }

        public SeriesPanelView Reps { get; }

        public SeriesColumnMotion Motion { get; }

        public ISeriesPanelsExpansion Expansion => _expansion;

        public SeriesPanelsExpanded Expanded => _expanded;

        // La columna de la ventana y el lugar de cada panel (la página, las pruebas y el arnés).
        public bool HasLayout => _placed;

        public SeriesColumnLayout Column => _column;

        public SeriesPanelPlacement LapsPlacement => _lapsPlace;

        public SeriesPanelPlacement RepsPlacement => _repsPlace;

        public double ColumnContentBase => _columnContent;

        public bool RepsShown => _repsShown;

        public bool SpmHasTarget => _spmHasTarget;

        // El desplazamiento de la columna (ColumnScroll, base del panel) y su tope.
        public double ScrollBase => Motion.Visual.ScrollBase;

        public double ScrollMaxBase => _column.Rule == SeriesFitRule.ColumnScroll ? Math.Max(0, _columnContent - _column.RoomBase(_spmHasTarget)) : 0;

        // Algo de los paneles en movimiento (la regla de toma la mira por fase).
        public bool IsAnimating => Laps.Motion.IsRunning || Reps.Motion.IsRunning || Motion.IsRunning || Laps.RowsWheel.IsRunning || Reps.RowsWheel.IsRunning;

        public void RenderTexts(Localizer texts)
        {
            Laps.RenderTexts(texts);
            Reps.RenderTexts(texts);
            UpdateTitleScale();
        }

        // La escala de título del idioma (DD5-20): la menor de los dos títulos, aunque repeticiones no se vea.
        private void UpdateTitleScale()
        {
            var laps = Laps.TitleWidth;
            var reps = Reps.TitleWidth;
            if (float.IsNaN(laps) || float.IsNaN(reps))
            {
                return;
            }

            var scale = SeriesPanelGeometry.TitleScale(laps, reps);
            Laps.SetTitleScale(scale);
            Reps.SetTitleScale(scale);
        }

        // ── El layout (HudZones, con cada disposición) ──

        public void SetLayout(HudLayout layout)
        {
            if (_disposed || layout == null)
            {
                return;
            }

            // Otra ventana (la rotación, AC-WKT-18-20): en el acto, con la geometría nueva, sp nueva y el desplazamiento en 0.
            var rotated = _placed && (Math.Abs(layout.WindowWidth - _windowWidth) > 0.01 || Math.Abs(layout.WindowHeight - _windowHeight) > 0.01);
            var first = !_placed;
            _layout = layout;
            _windowWidth = layout.WindowWidth;
            _windowHeight = layout.WindowHeight;
            _column = SeriesPanelGeometry.Column(layout);
            Motion.Scale = _column.Scale;
            if (rotated || first)
            {
                Motion.WheelTo(0, true);
                Laps.RowsWheel.WheelTo(0, true);
                Reps.RowsWheel.WheelTo(0, true);
            }

            _placed = true;
            UpdateTitleScale();
            Replace(rotated || first, true);
        }

        // ── El contenido (HudView.Apply) ──

        public void Apply(HudSeries? series, bool spmHasTarget, bool instant)
        {
            if (_override != null)
            {
                series = _override;
            }

            if (_disposed || series == null)
            {
                return;
            }

            var spmChanged = _lastSpm.HasValue && _lastSpm.Value != spmHasTarget;
            if (ReferenceEquals(series, _series) && !spmChanged)
            {
                return;
            }

            var first = _series == null;
            var change = HudSeriesChange.Between(_series, series);
            _series = series;
            _spmHasTarget = spmHasTarget;
            if (spmChanged)
            {
                Motion.FollowSpm(spmHasTarget, instant);
            }

            _lastSpm = spmHasTarget;
            var starts = series.InSeries && !_repsShown;
            var ends = !series.InSeries && _repsShown;
            if (starts)
            {
                _repsShown = true;
                Reps.style.display = DisplayStyle.Flex;
            }

            if (ends)
            {
                // La serie que termina: repeticiones se va (0-120) sin moverse, desde donde se ve.
                _repsShown = false;
                _repsLeaveY = float.IsNaN(_lastTranslate[1]) ? 0 : _lastTranslate[1];
                if (Reps.focusController?.focusedElement == Reps)
                {
                    // El panel enfocado que desaparece (§3.5): el siguiente Tab llega a la raíz y va a Terminar.
                    Reps.Blur();
                }

                Reps.SetInteractive(false);
            }

            // El layout va a su estado final en el cuadro del cambio (antes del contenido: los topes del desplazamiento de las filas
            // ya son los nuevos), y después el contenido con su motion.
            Compute(out var lapsFinal, out var repsFinal);
            PlaceBoth();
            Laps.Apply(series, in change, lapsFinal, instant || first);
            if (series.InSeries || starts)
            {
                // La serie que empieza: aparece en su estado guardado, en el acto (la entrada es de la columna: 120-400).
                Reps.Apply(series, in change, repsFinal, instant || first || starts);
            }

            if (starts)
            {
                Motion.RepsAppear(instant || first);
            }
            else if (ends)
            {
                Motion.RepsLeave(instant, OnRepsGone);
            }

            // LM5-01: el contenido que se achica por un dato (la serie que termina, un trabajo que pasa al vacío, SPM que gana obj.)
            // baja el tope; el desplazamiento lo sigue sin saltar (spring.snappy, como la rueda; en el acto con Instant). Antes
            // quedaba donde estaba: con la columna al tope, parciales podía quedar entero arriba del techo, sin toque ni arrastre.
            ClampScroll(instant || first);
            Compose();
            if (ends || starts)
            {
                Reps.SetInteractive(_interactive && _repsShown);
            }
        }

        // Para las pruebas y el arnés (S-LARGO, LT-59 a LT-61; 05 §5.1 punto 9, §6.4): un contenido armado (con ComposeSeries o a
        // mano) que reemplaza al del motor hasta que se saque (null), como la fila de 1998 píldoras de la 4b. Se aplica en el acto;
        // instant = false lo aplica como un dato del motor, con su animación (las pruebas de LM5-01).
        internal void OverrideSeries(HudSeries? series, bool instant = true)
        {
            _override = series;
            if (series != null)
            {
                Apply(series, _spmHasTarget, instant);
            }
        }

        private HudSeries? _override;

        private void OnRepsGone()
        {
            if (!_repsShown)
            {
                Reps.style.display = DisplayStyle.None;
            }
        }

        // ── Abrir y cerrar (la preferencia) ──

        private void OnActivate(SeriesPanelKind kind)
        {
            if (_disposed || !_interactive)
            {
                return;
            }

            // Abrir o cerrar no toca el motor, la sesión ni la telemetría (AC-WKT-13-14): solo la preferencia, que se guarda en el
            // acto; la vista cambia con su State.
            _expansion.Toggle(kind);
        }

        private void OnExpansion(SeriesPanelsExpanded state)
        {
            if (_disposed)
            {
                return;
            }

            var first = !_hasExpansion;
            var before = _expanded;
            _expanded = state;
            _hasExpansion = true;
            if (_series == null || !_placed)
            {
                // Todavía sin contenido o sin lugar: el estado entra con el primer Replace, sin animar.
                Laps.SetExpanded(state.Laps, Natural(SeriesPanelKind.Laps), true);
                Reps.SetExpanded(state.Reps, Natural(SeriesPanelKind.Reps), true);
                return;
            }

            Compute(out var lapsFinal, out var repsFinal);
            var instant = first;
            if (state.Laps != before.Laps || first)
            {
                Laps.SetExpanded(state.Laps, lapsFinal, instant);
            }
            else if (Math.Abs(lapsFinal - Laps.FinalHeightBase) > 1e-6)
            {
                Laps.Retarget(lapsFinal, instant);
            }

            if (state.Reps != before.Reps || first)
            {
                Reps.SetExpanded(state.Reps, repsFinal, instant);
            }
            else if (Math.Abs(repsFinal - Reps.FinalHeightBase) > 1e-6)
            {
                Reps.Retarget(repsFinal, instant);
            }

            PlaceBoth();
            // LM5-01 (b): cerrar con la columna al tope baja el tope; el desplazamiento lo sigue con spring.snappy mientras el
            // vidrio se cierra (antes saltaba 48 base en el cuadro del toque).
            ClampScroll(instant);
            Compose();
        }

        // ── El reparto y el lugar ──

        private double Natural(SeriesPanelKind kind)
        {
            var series = _series;
            var expanded = kind == SeriesPanelKind.Laps ? _expanded.Laps : _expanded.Reps;
            if (series == null || !expanded)
            {
                return SeriesPanelGeometry.ChipHeight;
            }

            return kind == SeriesPanelKind.Laps
                ? SeriesPanelGeometry.LapsOpenHeight(series.IsWork, Math.Min(series.Laps.Count, SeriesPanels.VisibleLaps), series.InSeries)
                : SeriesPanelGeometry.RepsOpenHeight(Math.Min(series.Reps.Count, SeriesTracker.MaxPreviousReps));
        }

        private SeriesPanelsContent ContentOf(HudSeries series)
        {
            var reps = Math.Min(series.Reps.Count, SeriesTracker.MaxPreviousReps);
            // Las vueltas a la vista son las de la lista (el panel dibuja esas: hasta 4).
            return new SeriesPanelsContent(true, series.StepIndex, series.IsWork, series.CurrentMetersValue, series.CurrentTimeValue,
                Math.Min(series.Laps.Count, SeriesPanels.VisibleLaps),
                default, default, default, default, series.InSeries, series.Rep, 0, series.RepMeters, RepsOfCount[reps]);
        }

        // Los altos finales del reparto (sin tocar el layout).
        private void Compute(out double lapsFinal, out double repsFinal)
        {
            var series = _series;
            if (series == null || !_placed)
            {
                lapsFinal = Natural(SeriesPanelKind.Laps);
                repsFinal = Natural(SeriesPanelKind.Reps);
                return;
            }

            var content = ContentOf(series);
            SeriesPanelGeometry.Place(in _column, _spmHasTarget, Natural(SeriesPanelKind.Laps), Natural(SeriesPanelKind.Reps), _repsShown, _expanded.Laps,
                _expanded.Reps, in content, out _lapsPlace, out _repsPlace, out _columnContent);
            lapsFinal = _lapsPlace.HeightBase;
            repsFinal = _repsPlace.HeightBase;
        }

        private void PlaceBoth()
        {
            if (!_placed || _series == null)
            {
                return;
            }

            Laps.Place(in _column, in _lapsPlace);
            if (_repsShown)
            {
                // El que se va (la serie terminó) queda donde estaba hasta salir del árbol.
                Reps.Place(in _column, in _repsPlace);
            }
            // La cuenta grande y PAUSA (AC-WKT-18-15): con los rectángulos FINALES de los paneles abiertos.
            var lapsRect = new DpRect(_column.Left, _lapsPlace.TopDp, _column.Width, _lapsPlace.HeightBase * _column.Scale);
            DpRect? repsRect = _repsShown ? new DpRect(_column.Left, _repsPlace.TopDp, _column.Width, _repsPlace.HeightBase * _column.Scale) : (DpRect?)null;
            _calloutsBlocked(_layout != null && SeriesPanelGeometry.BlocksCallout(lapsRect, repsRect, _layout), false);
        }

        private void Replace(bool instant, bool layoutChanged)
        {
            if (_series == null || !_placed)
            {
                return;
            }

            Compute(out var lapsFinal, out var repsFinal);
            if (layoutChanged)
            {
                Laps.Retarget(lapsFinal, instant);
                Reps.Retarget(repsFinal, instant);
            }

            PlaceBoth();
            ClampScroll(instant);
            Compose();
        }

        // ── El desplazamiento (AC-WKT-18-13; 04-design-notes §5.2) ──

        private bool OnDragged(SeriesPanelView panel, float deltaDp)
        {
            var sp = _column.Scale > 0 ? _column.Scale : 1;
            if (_column.Rule == SeriesFitRule.ColumnScroll)
            {
                var max = ScrollMaxBase;
                if (max <= 0.001)
                {
                    return false;
                }

                // El arrastre sigue al dedo 1:1, sin inercia, dentro de sus topes.
                var to = Math.Max(0, Math.Min(max, Motion.ScrollTargetBase - deltaDp / sp));
                Motion.WheelTo(to, true);
                return true;
            }

            if (_column.Rule == SeriesFitRule.RowsScroll)
            {
                return panel.ScrollRowsBy(-deltaDp / sp, false);
            }

            return false;
        }

        private bool OnWheeled(SeriesPanelView panel, float deltaY)
        {
            if (Math.Abs(deltaY) < 1e-4)
            {
                return false;
            }

            var step = SeriesTimelines.WheelStepBase * Math.Sign(deltaY);
            if (_column.Rule == SeriesFitRule.ColumnScroll)
            {
                var max = ScrollMaxBase;
                if (max <= 0.001)
                {
                    return false;
                }

                Motion.WheelTo(Math.Max(0, Math.Min(max, Motion.ScrollTargetBase + step)), false);
                return true;
            }

            return _column.Rule == SeriesFitRule.RowsScroll && panel.ScrollRowsBy(step, true);
        }

        // El desplazamiento dentro de su tope (LM5-01): cuando el contenido se achica (un dato, cerrar, otra disposición), el
        // destino baja al tope nuevo con spring.snappy desde donde está, como la rueda; en el acto solo con Instant (la rotación, el
        // primer estado, la vuelta del segundo plano) o con reducir movimiento (WheelTo). Mientras el resorte corre el tope ya es el
        // nuevo: el arrastre y la rueda parten de su destino.
        private void ClampScroll(bool instant)
        {
            var max = ScrollMaxBase;
            var target = Motion.ScrollTargetBase;
            if (target > max + 1e-6)
            {
                Motion.WheelTo(max, instant);
            }
        }

        // LM5-02: el panel que recibe el foco con el teclado en ColumnScroll se trae a la vista: la columna se desplaza (con
        // spring.snappy) lo justo para que entre entero o, si no entra, hasta que su cabecera quede en el techo. Así el Tab nunca
        // deja el foco en un panel escondido bajo la máscara y Enter abre o cierra lo que se ve. Un toque ya está a la vista: solo
        // el foco del teclado (como FocusScroller).
        private void OnKeyboardFocus(SeriesPanelView panel)
        {
            if (_disposed || !_placed || _series == null || _column.Rule != SeriesFitRule.ColumnScroll)
            {
                return;
            }

            if (ReferenceEquals(panel, Reps) && !_repsShown)
            {
                return;
            }

            var max = ScrollMaxBase;
            var sp = _column.Scale > 0 ? _column.Scale : 1;
            var place = ReferenceEquals(panel, Laps) ? _lapsPlace : _repsPlace;
            // Con el lugar del halo del anillo (8 dp) adentro de la ventana cuando entra; si no, manda la cabecera.
            var margin = SeriesPanelGeometry.FocusHaloOuterDp;
            var ceiling = _column.Top(_spmHasTarget) + margin;
            var floor = _column.Floor - margin;
            var target = Motion.ScrollTargetBase;
            var bottom = place.TopDp + place.HeightBase * sp - target * sp;
            if (bottom > floor)
            {
                target += (bottom - floor) / sp;
            }

            var top = place.TopDp - target * sp;
            if (top < ceiling)
            {
                target -= (ceiling - top) / sp;
            }

            target = Math.Max(0, Math.Min(max, target));
            if (Math.Abs(target - Motion.ScrollTargetBase) > 1e-6)
            {
                Motion.WheelTo(target, false);
            }
        }

        // ── La composición (DT5-14) ──

        // La cascada de la entrada (HudPiece.SeriesLaps / SeriesReps, con el tramo de SPM: opacidad y subida en base) y la salida.
        public void SetPiece(HudPiece piece, double opacity, double shiftBase)
        {
            var i = piece == HudPiece.SeriesLaps ? 0 : piece == HudPiece.SeriesReps ? 1 : -1;
            if (i < 0)
            {
                return;
            }

            _cascadeOpacity[i] = opacity;
            _cascadeShift[i] = shiftBase;
            Compose();
        }

        public void SetExit(double opacity)
        {
            _exit = opacity;
            Compose();
        }

        // Responden (desde su tramo de la cascada; no durante la salida, con un diálogo ni con la rutina terminada).
        public void SetInteractive(bool interactive)
        {
            _interactive = interactive;
            Laps.SetInteractive(interactive);
            Reps.SetInteractive(interactive && _repsShown);
        }

        public bool IsInteractive => _interactive;

        // It. 5 (AC-BLE-16-02; R10 con los paneles): lo que se ve de la columna en reposo, en dp de ventana con y desde arriba: del
        // borde de arriba de parciales al de abajo del último panel a la vista, con los altos finales, el desplazamiento de la
        // columna y, en ColumnScroll, recortado a su techo y su piso. null sin disposición. La página lo pasa a la llave con el
        // diálogo de completado abierto (los paneles ya no cambian: la rutina terminó).
        public DpRect? VisibleBounds()
        {
            if (!_placed || _layout == null)
            {
                return null;
            }

            var sp = _column.Scale > 0 ? _column.Scale : 1;
            // En reposo: el destino del desplazamiento (un recorte de LM5-01 puede estar terminando su resorte).
            var scroll = _column.Rule == SeriesFitRule.ColumnScroll ? Motion.ScrollTargetBase * sp : 0;
            var top = _lapsPlace.TopDp - scroll;
            var bottom = _repsShown ? _repsPlace.TopDp - scroll + Reps.FinalHeightBase * sp : top + Laps.FinalHeightBase * sp;
            if (_column.Rule == SeriesFitRule.ColumnScroll)
            {
                top = Math.Max(top, _column.Top(_spmHasTarget));
                bottom = Math.Min(bottom, _column.Floor);
            }

            return bottom > top ? DpRect.FromEdges(_column.Left, top, _column.Right, bottom) : (DpRect?)null;
        }

        private void OnColumnVisual(SeriesColumnVisual visual)
        {
            Compose();
        }

        private void OnPanelVisual(SeriesPanelView panel)
        {
            if (ReferenceEquals(panel, Laps))
            {
                // El FLIP del de abajo y la máscara siguen al G de parciales.
                Compose();
            }
            else if (_column.Rule == SeriesFitRule.ColumnScroll)
            {
                Compose();
            }
        }

        private void Compose()
        {
            if (_disposed)
            {
                return;
            }

            var visual = Motion.Visual;
            var sp = _column.Scale > 0 ? _column.Scale : 1;
            var s = _column.HudScale > 0 ? _column.HudScale : 1;
            var scroll = _column.Rule == SeriesFitRule.ColumnScroll ? visual.ScrollBase * sp : 0;
            var follow = visual.FollowBase * s;
            var lapsY = _cascadeShift[0] * sp + follow - scroll;
            var flip = (Laps.VisualHeightBase - Laps.FinalHeightBase) * sp;
            var repsY = _repsShown ? _cascadeShift[1] * sp + follow - scroll + visual.RepsRiseBase * s + flip : _repsLeaveY;
            Write(Laps, 0, lapsY, _cascadeOpacity[0] * _exit);
            Write(Reps, 1, repsY, _cascadeOpacity[1] * _exit * visual.RepsAlpha);
            if (_placed)
            {
                UpdateMask(lapsY, repsY);
            }
        }

        private void Write(SeriesPanelView panel, int i, double translateY, double opacity)
        {
            var y = (float)translateY;
            if (Math.Abs(y - _lastTranslate[i]) > 1e-4f || float.IsNaN(_lastTranslate[i]))
            {
                _lastTranslate[i] = y;
                panel.style.translate = Math.Abs(y) < 1e-4f ? (StyleTranslate)StyleKeyword.Null : new Translate(0, y);
            }

            var o = (float)Math.Max(0, Math.Min(1, opacity));
            if (Math.Abs(o - _lastOpacity[i]) > 1e-5f || float.IsNaN(_lastOpacity[i]))
            {
                _lastOpacity[i] = o;
                panel.style.opacity = o >= 0.9995f ? (StyleFloat)StyleKeyword.Null : o;
            }
        }

        // La máscara de la columna (regla 3; solo ColumnScroll) y la ventana del toque, en base de cada panel.
        private void UpdateMask(double lapsY, double repsY)
        {
            if (_column.Rule != SeriesFitRule.ColumnScroll)
            {
                Laps.SetColumnMask(false, 0, 0, 0, 0, null);
                Reps.SetColumnMask(false, 0, 0, 0, 0, null);
                return;
            }

            var sp = _column.Scale;
            var top = _column.Top(_spmHasTarget);
            var floor = _column.Floor;
            var lapsTop = _lapsPlace.TopDp + lapsY;
            var repsTop = _repsPlace.TopDp + repsY;
            var columnTop = lapsTop;
            var columnBottom = _repsShown ? repsTop + Reps.VisualHeightBase * sp : lapsTop + Laps.VisualHeightBase * sp;
            var hiddenAbove = Math.Max(0, top - columnTop) / sp;
            var hiddenBelow = Math.Max(0, columnBottom - floor) / sp;
            var hud = Laps.hierarchy.parent;
            var origin = hud != null ? hud.worldBound.position : Vector2.zero;
            var clip = new Rect(origin.x, origin.y + (float)top, (float)_layout!.WindowWidth, (float)(floor - top));
            Laps.SetColumnMask(true, (top - lapsTop) / sp, (floor - lapsTop) / sp, hiddenAbove, hiddenBelow, clip);
            Reps.SetColumnMask(true, (top - repsTop) / sp, (floor - repsTop) / sp, hiddenAbove, hiddenBelow, clip);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Motion.Stop();
            Laps.Dispose();
            Reps.Dispose();
        }
    }
}
