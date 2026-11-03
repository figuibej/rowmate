using System;
using System.Collections.Generic;
using System.Diagnostics;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Un panel de series del inmersivo: "Parciales 500 m" (las vueltas) o "Series anteriores" (las repeticiones) (F-WKT-18;
    // legacy series_panels.dart; 05-series-parciales/02-tech-design §3.1 a §3.5; 04-design-notes §3 a §6; DD5-04, DD5-13 a DD5-24).
    //   #hud-laps-panel / #hud-reps-panel .rm-hud-series-panel .rm-hud-series-panel--laps|--reps
    //   │      UN elemento que dibuja TODO en su generateVisualContent: el vidrio sin reflejo, el borde, la capa de hover, la
    //   │      cabecera (ícono, rótulo con tracking, título, flecha), la vuelta en curso con sus celdas tabulares y su rodillo, las
    //   │      filas, la línea Rep, los vacíos en dos renglones parejos, la marca de la fila nueva y el anillo de foco (en dp de
    //   │      ventana). Absoluto, escala sp desde arriba a la izquierda; su layout es su rectángulo FINAL (137 × alto final, base);
    //   │      pickingMode Position con ContainsPoint = el área de toque de este momento; focusable solo cuando responde.
    //   ├── 5 holders ocultos (.rm-hud-series-panel__font): las fuentes de cada clase de tipo y su medida (DrawnFont)
    //   └── .rm-hud-series-panel__hit: oculto, sin eventos: agranda la caja del panel hasta el área de toque y lo que dibuja en la
    //       animación en curso (Panel.PerformPick descarta un elemento fuera de su caja antes de llamar a ContainsPoint: DT5-15,
    //       QA-G5T-04; lo confirmó S-B5). Se ajusta al empezar y al terminar cada animación del alto, nunca por cuadro.
    // Sin recortes: cada renglón se dibuja entero con un alfa = el de su motion × las reglas de la tinta (DT5-02; 04-design-notes
    // §5.3): revelar contra el vidrio (regla 1), la ventana de filas (regla 2) y la máscara de la columna (regla 3, solo
    // ColumnScroll). Nada se anima por layout: style.translate y style.opacity del panel los escribe solo SeriesColumn (DT5-14).
    // Los textos se miden cuando cambian (a lo sumo una vez por segundo, o con el idioma y la ventana), nunca por cuadro; por cuadro
    // solo se multiplican alfas. 0 B por cuadro (FrameAllocations, HBT).
    // Toque, Enter y Espacio (una sola vez por tecla, QA-G5T-09) piden abrir o cerrar (Activated); la capa de hover y presionado
    // con un puntero de mouse (§6.10). Las pruebas leen las ranuras (IDrawnTextSource): hud-laps-chip, -title, -current,
    // -current-meters, -row-0..3, -empty, hud-rep-progress; hud-reps-chip, -title, -row-0..2, -empty.
    public sealed class SeriesPanelView : VisualElement, IDrawnTextSource, IDisposable
    {
        public const string UssClass = "rm-hud-series-panel";
        public const string HitClass = "rm-hud-series-panel__hit";

        // Con la máscara de la columna (ColumnScroll): la parte de la caja que queda fuera de la ventana de la columna no se ve
        // ni responde; LayoutAudit no la cuenta y HudOverlap mide el rectángulo recortado (02-tech-design §5.3).
        public const string MaskedClass = "rm-hud-series-panel--masked";
        public const float ChipLabelSizeDp = 13;
        public const float TitleSizeDp = 11;
        public const float RowLabelSizeDp = 12;
        public const float EmptySizeDp = 15;
        public const float IconSizeDp = 14;

        private const float HeroSize = 28;
        private const float RowValueSize = 20;
        private const float MetersSize = 13;
        private const float RepLineSize = 16;
        private const int MaxRows = 4;
        private const int MaskSegments = 48;
        private const int Slots = 16;

        private static readonly ushort[] MaskIndices = BuildMaskIndices();

        private readonly UiContext _ui;
        private readonly Action<SeriesPanelKind> _activate;
        private readonly Action<SeriesPanelView> _visualChanged;
        private readonly DrawnFont _chipFont;
        private readonly DrawnFont _labelFont;
        private readonly DrawnFont _titleFont;
        private readonly DrawnFont _bodyFont;
        private readonly DrawnFont _numFont;
        private readonly DrawnFont _iconFont;
        private readonly VisualElement _hit;
        private readonly TabularCells _heroTime;
        private readonly TabularCells _heroMeters;
        private readonly TabularCells _repLine;
        private readonly Stopwatch _work = new Stopwatch();
        private readonly Vertex[] _maskVertices = new Vertex[(MaskSegments + 1) * 2];
        private readonly float[] _rowLabelWidth = new float[MaxRows];
        private readonly float[] _rowValueWidth = new float[MaxRows];
        private readonly float[] _rowMetersWidth = new float[MaxRows];
        private readonly string[] _rowSplit = new string[MaxRows];
        private List<DrawnSlot>? _slots;

        // El contenido.
        private HudSeries? _series;
        private HudSeries? _before;
        private HudSeriesChange _change;
        private string _chipText = "";
        private string _titleText = "";
        private string _titleDrawn = "";
        private string _emptyText = "";
        private string _emptyLine0 = "";
        private string _emptyLine1 = "";
        private string _beforeRepLine = "";
        private bool _chipDescender;
        private bool _titleDescender;
        private double _titleScale = 1;
        private bool _measureDirty = true;

        // Lo medido (base, a escala 1).
        private float _heroTimeWidth;
        private float _heroMetersWidth;
        private float _kHero = 1;
        private float _rowsColumn;
        private float _kRowsFrom = 1;
        private float _kRowsTo = 1;
        private float _kLine = 1;

        // El lugar y el estado.
        private double _scale = 1;
        private double _widthBase = SeriesPanelGeometry.PanelWidth;
        private double _finalHeight = SeriesPanelGeometry.ChipHeight;
        private double _viewport;
        private double _rowsContent;
        private double _hitUp;
        private double _hitDown;
        private double _hitTop = double.NaN;
        private double _hitBottom = double.NaN;
        private bool _rowsScrollRule;
        private SeriesPanelVisual _v;
        private double _rowsScroll;

        // La máscara de la columna (ColumnScroll), en base del panel.
        private bool _maskOn;
        private double _maskTop;
        private double _maskBottom;
        private double _maskHiddenAbove;
        private double _maskHiddenBelow;
        private Rect? _clipWorld;

        // La interacción.
        private bool _interactive;
        private bool _hovered;
        private bool _pressed;
        private int _pointerId = -1;
        private Vector2 _pointerStart;
        private bool _dragging;
        private double _spaceAt = double.NegativeInfinity;
        private bool _disposed;
        private bool _detached;

        public SeriesPanelView(UiContext ui, SeriesPanelKind kind, Action<SeriesPanelKind> activate, Action<SeriesPanelView> visualChanged)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _activate = activate ?? throw new ArgumentNullException(nameof(activate));
            _visualChanged = visualChanged ?? throw new ArgumentNullException(nameof(visualChanged));
            Kind = kind;
            name = kind == SeriesPanelKind.Laps ? "hud-laps-panel" : "hud-reps-panel";
            AddToClassList(UssClass);
            AddToClassList(UssClass + (kind == SeriesPanelKind.Laps ? "--laps" : "--reps"));
            pickingMode = PickingMode.Position;
            focusable = false;
            tabIndex = 0;
            style.position = Position.Absolute;
            style.width = (float)SeriesPanelGeometry.PanelWidth;
            style.height = (float)SeriesPanelGeometry.ChipHeight;
            style.transformOrigin = new TransformOrigin(0, 0);

            _chipFont = new DrawnFont(this, "rm-type-label", ChipLabelSizeDp, null);
            _labelFont = new DrawnFont(this, "rm-type-label", RowLabelSizeDp, 0);
            _titleFont = new DrawnFont(this, "rm-type-label", TitleSizeDp, 0);
            _bodyFont = new DrawnFont(this, "rm-type-body", EmptySizeDp, null);
            _numFont = new DrawnFont(this, "rm-type-num-row", RowValueSize, null);
            _iconFont = new DrawnFont(this, "rm-icon", IconSizeDp, null);

            _hit = new VisualElement();
            _hit.AddToClassList(HitClass);
            _hit.pickingMode = PickingMode.Ignore;
            _hit.focusable = false;
            _hit.style.position = Position.Absolute;
            _hit.style.visibility = Visibility.Hidden;
            _hit.style.left = 0;
            Add(_hit);

            _heroTime = new TabularCells(ui.Motion, MarkDirtyRepaint);
            _heroMeters = new TabularCells(ui.Motion, MarkDirtyRepaint);
            _repLine = new TabularCells(ui.Motion, MarkDirtyRepaint);
            Motion = new SeriesPanelMotion(ui.Motion, kind, OnVisual);
            RowsWheel = new SeriesColumnMotion(ui.Motion, OnRowsWheel);
            _v = SeriesPanelVisual.Rest(SeriesPanelGeometry.ChipHeight, false);
            UpdateHit();

            generateVisualContent += Draw;
            RegisterCallback<GeometryChangedEvent>(_ => OnGeometry());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());
            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<FocusInEvent>(_ => OnFocusIn());
            RegisterCallback<FocusOutEvent>(_ => MarkDirtyRepaint());
        }

        // Arrastre y rueda (SeriesColumn decide qué se desplaza: las filas de este panel o la columna). delta en dp de ventana.
        public event Func<SeriesPanelView, float, bool>? Dragged;

        public event Func<SeriesPanelView, float, bool>? Wheeled;

        // El foco llegó con el teclado (Tab, Mayús+Tab): SeriesColumn trae el panel a la vista en ColumnScroll (LM5-02).
        public event Action<SeriesPanelView>? KeyboardFocused;

        public SeriesPanelKind Kind { get; }

        public SeriesPanelMotion Motion { get; }

        // El desplazamiento de las filas de este panel (RowsScroll, W1280): la rueda con spring.snappy (D1-11).
        public SeriesColumnMotion RowsWheel { get; }

        public HudSeries? Series => _series;

        // El alto visual G (base) y el final.
        public double VisualHeightBase => _v.HeightBase;

        public double FinalHeightBase => _finalHeight;

        public SeriesPanelVisual Visual => _v;

        public double Scale => _scale;

        public double TitleScale => _titleScale;

        public bool IsInteractive => _interactive;

        // El desplazamiento de las filas (base; RowsScroll) y su tope.
        public double RowsScrollBase => _rowsScroll;

        public double RowsScrollMaxBase => Math.Max(0, _rowsContent - _viewport);

        public double RowsViewportBase => _viewport;

        // Las escalas de ajuste vigentes (05 §3.1; DV-49): la cifra de la vuelta en curso, las filas (común) y la línea Rep.
        public float HeroScale => _kHero;

        public float RowsScaleNow => RowsScale();

        public float RepLineScale => _kLine;

        // El área de toque de este momento, en base del panel (de arriba a abajo).
        public double HitTopBase => _hitTop;

        public double HitBottomBase => _hitBottom;

        public bool HasFocusRing => _interactive && focusController?.focusedElement == this && InKeyboardMode();

        // Las cifras de la línea Rep (las pruebas: qué rueda; QA-V5-02).
        internal TabularCells RepLineCells => _repLine;

        // Los rodillos en curso (la regla de toma: uno antes de su mitad bloquea).
        public double RollProgress => Math.Min(_heroTime.RollProgress, Math.Min(_heroMeters.RollProgress, _repLine.RollProgress));

        // El trabajo del último generateVisualContent, en ms (HBT.SeriesWorstCaseWork) y cuántas veces dibujó.
        public double LastDrawMs { get; private set; }

        public int DrawCount { get; private set; }

        // ── Textos (el idioma) ──

        public void RenderTexts(Localizer texts)
        {
            var laps = Kind == SeriesPanelKind.Laps;
            _chipText = texts.Get(laps ? L10nKeys.HudLapsChip : L10nKeys.HudRepsChip);
            _titleText = texts.Get(laps ? L10nKeys.HudLapsTitle : L10nKeys.HudRepsTitle);
            _emptyText = texts.Get(laps ? L10nKeys.HudLapsEmpty : L10nKeys.HudRepsEmpty);
            _chipDescender = DrawnText.HasDescender(_chipText);
            _titleDescender = DrawnText.HasDescender(_titleText);
            _measureDirty = true;
            Remeasure();
        }

        // El ancho del título a escala 1 (base; NaN sin medir): SeriesColumn arma la escala de título del idioma con los dos.
        public float TitleWidth => _titleText.Length == 0 ? 0 : _titleFont.Measure(_titleText, TitleSizeDp);

        public void SetTitleScale(double scale)
        {
            if (Math.Abs(scale - _titleScale) < 1e-6 && _titleDrawn.Length > 0)
            {
                return;
            }

            _titleScale = scale;
            _measureDirty = true;
            Remeasure();
        }

        // ── El contenido (una vez por segundo como mucho; P-01) ──

        // Un HudSeries nuevo con lo que cambió (por pieza) y el alto final que le da la columna.
        public void Apply(HudSeries series, in HudSeriesChange change, double finalHeightBase, bool instant)
        {
            if (_disposed || series == null)
            {
                return;
            }

            var first = _series == null;
            _before = _series;
            _change = change;
            _series = series;
            if (_before != null && _before.RepProgress != null)
            {
                _beforeRepLine = _before.RepProgress;
            }

            var animate = !instant && !first && Motion.Expanded && panel != null;
            if (Kind == SeriesPanelKind.Laps)
            {
                // Con un cambio de clase la vuelta en curso no rueda: sale o entra entera (DD5-18).
                var roll = animate && change.LapsBody != SeriesBodyChange.ToEmpty && change.LapsBody != SeriesBodyChange.FromEmpty;
                _heroTime.Set(series.CurrentTime, series.CurrentTimeValue, roll && series.IsWork);
                _heroMeters.Set(series.CurrentMeters, series.CurrentMetersValue, roll && series.IsWork);
                _repLine.Set(series.RepProgress ?? "", series.RepMeters, animate && !change.SeriesStarted && series.RepProgress != null);
            }

            Array.Clear(_rowSplit, 0, MaxRows);
            var dropRowScale = _kRowsTo;
            _measureDirty = true;
            Remeasure();
            _kRowsFrom = animate && (change.NewLaps > 0 || change.NewRep || change.StepChanged) ? dropRowScale : _kRowsTo;
            if (first)
            {
                _kRowsFrom = _kRowsTo;
            }

            // DD5-27: una fila que entra con las filas desplazadas suma su alto al desplazamiento en el mismo cuadro, y las que estaban
            // no bajan con el empuje: lo que se ve no se mueve (la nueva entra arriba, fuera de la vista). LM5-01 (b): solo lo que
            // cabe debajo del tope (Place ya dejó el tope nuevo); lo que no cabe (al tope, con la más vieja que sale por abajo) lo
            // hace el empuje, en su proporción. Antes sumaba todo y lo recortaba en el acto: la fila a la vista saltaba 22·sp.
            _pushScale = 1;
            if (_rowsScrollRule && _rowsScroll > 0.001)
            {
                var newRows = Kind == SeriesPanelKind.Laps ? (change.StepChanged ? 0 : Math.Min(change.NewLaps, MaxRows)) : (change.NewRep && change.RepsBody == SeriesBodyChange.None ? 1 : 0);
                if (newRows > 0)
                {
                    var wanted = SeriesTimelines.WheelStepBase * newRows;
                    var shift = Math.Min(wanted, Math.Max(0, RowsScrollMaxBase - RowsWheel.ScrollTargetBase));
                    if (shift > 1e-6)
                    {
                        RowsWheel.ShiftScroll(shift);
                    }

                    _pushScale = 1 - shift / wanted;
                }
            }

            if (first)
            {
                Motion.Retarget(finalHeightBase, true);
            }
            else
            {
                Motion.ContentChanged(in change, finalHeightBase, instant);
            }

            _finalHeight = finalHeightBase;
            UpdateHit();
            ClampRowsScroll(instant);
            MarkDirtyRepaint();
        }

        // Abrir o cerrar (el estado de la preferencia), con el alto final del reparto.
        public void SetExpanded(bool expanded, double finalHeightBase, bool instant)
        {
            if (_disposed)
            {
                return;
            }

            var from = _v.HeightBase;
            _finalHeight = finalHeightBase;
            if (!expanded && RowsWheel.ScrollTargetBase > 0)
            {
                // LM5-01 (b): las filas vuelven a 0 con spring.snappy mientras el contenido se apaga (0-120), no en el cuadro del
                // toque (en el acto solo con Instant o reducir movimiento).
                RowsWheel.WheelTo(0, instant);
            }

            Motion.SetExpanded(expanded, finalHeightBase, instant);
            UpdateHit(Math.Max(from, finalHeightBase));
            MarkDirtyRepaint();
        }

        // Otro alto final sin abrir ni cerrar (el reparto de RowsScroll cuando el otro panel se abre o se cierra; la rotación).
        public void Retarget(double finalHeightBase, bool instant)
        {
            var from = _v.HeightBase;
            _finalHeight = finalHeightBase;
            Motion.Retarget(finalHeightBase, instant);
            UpdateHit(Math.Max(from, finalHeightBase));
            ClampRowsScroll(instant);
            MarkDirtyRepaint();
        }

        // El lugar final (de SeriesPanelGeometry.Place): layout en el cuadro del cambio, nunca por cuadro.
        public void Place(in SeriesColumnLayout column, in SeriesPanelPlacement placement)
        {
            var leftDp = (float)column.Left;
            var topDp = (float)placement.TopDp;
            var width = (float)column.WidthBase;
            var height = (float)placement.HeightBase;
            var scale = (float)column.Scale;
            if (float.IsNaN(_layoutLeft) || Math.Abs(_layoutLeft - leftDp) > 0.0001f || Math.Abs(_layoutTop - topDp) > 0.0001f)
            {
                style.left = leftDp;
                style.top = topDp;
                _layoutLeft = leftDp;
                _layoutTop = topDp;
            }

            if (float.IsNaN(_layoutWidth) || Math.Abs(_layoutWidth - width) > 0.0001f || Math.Abs(_layoutHeight - height) > 0.0001f)
            {
                style.width = width;
                style.height = height;
                _layoutWidth = width;
                _layoutHeight = height;
            }

            if (float.IsNaN(_layoutScale) || Math.Abs(_layoutScale - scale) > 0.00001f)
            {
                style.scale = new Scale(new Vector3(scale, scale, 1));
                _layoutScale = scale;
                _measureDirty = true;
            }

            _scale = column.Scale;
            _widthBase = column.WidthBase;
            Motion.Scale = column.Scale;
            RowsWheel.Scale = column.Scale;
            _rowsScrollRule = column.Rule == SeriesFitRule.RowsScroll;
            _viewport = placement.RowsViewportBase;
            _rowsContent = placement.RowsContentBase;
            var hit = placement.HitRect;
            var heightDp = placement.HeightBase * column.Scale;
            _hitUp = Math.Max(0, (placement.TopDp - hit.Top) / column.Scale);
            _hitDown = Math.Max(0, (hit.Bottom - (placement.TopDp + heightDp)) / column.Scale);
            UpdateHit();
            MarkDirtyRepaint();
        }

        private float _layoutLeft = float.NaN;
        private float _layoutTop = float.NaN;
        private float _layoutWidth = float.NaN;
        private float _layoutHeight = float.NaN;
        private float _layoutScale = float.NaN;

        // La máscara de la columna (regla 3; solo ColumnScroll) en base de este panel, y la ventana de la columna para el toque
        // (en coordenadas del panel de UI Toolkit). active = false: sin máscara (nada escondido).
        public void SetColumnMask(bool active, double topBase, double bottomBase, double hiddenAboveBase, double hiddenBelowBase, Rect? clipWorld)
        {
            var changed = active != _maskOn || (active && (Math.Abs(topBase - _maskTop) > 1e-4 || Math.Abs(bottomBase - _maskBottom) > 1e-4
                                                            || Math.Abs(hiddenAboveBase - _maskHiddenAbove) > 1e-4 || Math.Abs(hiddenBelowBase - _maskHiddenBelow) > 1e-4));
            if (active != _maskOn)
            {
                EnableInClassList(MaskedClass, active);
            }

            _maskOn = active;
            _maskTop = topBase;
            _maskBottom = bottomBase;
            _maskHiddenAbove = hiddenAboveBase;
            _maskHiddenBelow = hiddenBelowBase;
            _clipWorld = clipWorld;
            if (changed)
            {
                MarkDirtyRepaint();
            }
        }

        // Responde (desde su tramo de la cascada; no durante la salida, con un diálogo ni con la rutina terminada): inerte, sin
        // hover, sin presionado y sin anillo (§3.3; 04-design-notes §4.9).
        public void SetInteractive(bool interactive)
        {
            if (_interactive == interactive)
            {
                return;
            }

            _interactive = interactive;
            focusable = interactive;
            if (!interactive)
            {
                ReleaseCapture();
                _hovered = false;
                _pressed = false;
                Motion.Hover(false, false, false);
                if (focusController?.focusedElement == this)
                {
                    Blur();
                }
            }

            MarkDirtyRepaint();
        }

        // Nada animando al salir (AC-WKT-13-09; ROB-02).
        public void Stop()
        {
            Motion.Stop();
            RowsWheel.Stop();
            _heroTime.Finish();
            _heroMeters.Finish();
            _repLine.Finish();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            ReleaseCapture();
            Stop();
        }

        // ── La motion y el layout ──

        private void OnVisual(SeriesPanelVisual visual)
        {
            _v = visual;
            if (Math.Abs(visual.HeightBase - _finalHeight) < 1e-3)
            {
                // Terminó el alto: la caja de toque vuelve al área final.
                UpdateHit();
            }

            MarkDirtyRepaint();
            _visualChanged(this);
        }

        private void OnRowsWheel(SeriesColumnVisual visual)
        {
            _rowsScroll = visual.ScrollBase;
            MarkDirtyRepaint();
        }

        private void OnFocusIn()
        {
            MarkDirtyRepaint();
            if (_interactive && !_disposed && InKeyboardMode())
            {
                KeyboardFocused?.Invoke(this);
            }
        }

        private void OnGeometry()
        {
            if (_measureDirty)
            {
                Remeasure();
            }
        }

        private void OnAttach()
        {
            if (!_detached)
            {
                return;
            }

            // QA-G5T-12: de vuelta en un panel, todo en su estado final; las medidas de antes pueden ser de otra ventana o letra.
            _detached = false;
            Motion.Retarget(_finalHeight, true);
            Motion.Hover(false, false, true);
            RowsWheel.WheelTo(_rowsScroll, true);
            _measureDirty = true;
            Remeasure();
            _kRowsFrom = _kRowsTo;
            UpdateHit();
            MarkDirtyRepaint();
        }

        private void OnDetach()
        {
            _detached = true;
            ReleaseCapture();
            Stop();
        }

        private void ClampRowsScroll(bool instant)
        {
            var max = RowsScrollMaxBase;
            var target = RowsWheel.ScrollTargetBase;
            if (target > max + 1e-6 || target < 0)
            {
                // LM5-01 (b): con spring.snappy desde donde está (en el acto con Instant o reducir movimiento), como la rueda.
                RowsWheel.WheelTo(Math.Max(0, Math.Min(max, target)), instant);
            }
        }

        // La caja del hijo de toque: el área (cerrado, el chip agrandado) y lo que el vidrio dibuja hasta reachBase.
        private void UpdateHit(double reachBase = 0)
        {
            double top;
            double bottom;
            if (Motion.Expanded)
            {
                top = 0;
                bottom = Math.Max(Math.Max(_finalHeight, _v.HeightBase), reachBase);
            }
            else
            {
                top = -_hitUp;
                bottom = Math.Max(SeriesPanelGeometry.ChipHeight + _hitDown, Math.Max(_v.HeightBase, reachBase));
            }

            if (Math.Abs(top - _hitTop) < 1e-4 && Math.Abs(bottom - _hitBottom) < 1e-4)
            {
                return;
            }

            _hitTop = top;
            _hitBottom = bottom;
            _hit.style.top = (float)top;
            _hit.style.height = (float)(bottom - top);
            _hit.style.width = (float)_widthBase;
        }

        // El área de toque de este momento (DT5-15): cerrado, el chip agrandado hasta 56 dp; abierto o animando, de 0 a G; en
        // ColumnScroll, recortada a la ventana de la columna.
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (localPoint.x < 0 || localPoint.x > _widthBase)
            {
                return false;
            }

            double top;
            double bottom;
            if (Motion.Expanded || _v.HeightBase > SeriesPanelGeometry.ChipHeight + 1e-3)
            {
                top = 0;
                bottom = _v.HeightBase;
                if (!Motion.Expanded)
                {
                    top = -_hitUp;
                    bottom = Math.Max(bottom, SeriesPanelGeometry.ChipHeight + _hitDown);
                }
            }
            else
            {
                top = -_hitUp;
                bottom = SeriesPanelGeometry.ChipHeight + _hitDown;
            }

            if (localPoint.y < top || localPoint.y > bottom)
            {
                return false;
            }

            if (_clipWorld.HasValue)
            {
                var world = this.LocalToWorld(localPoint);
                return _clipWorld.Value.Contains(world);
            }

            return true;
        }

        // ── Las medidas (cuando cambia el texto, el idioma o la ventana; nunca por cuadro) ──

        private void Remeasure()
        {
            if (panel == null || _chipFont.Font == null || _labelFont.Font == null || _bodyFont.Font == null || _numFont.Font == null)
            {
                return;
            }

            _measureDirty = false;
            // El título: la escala de título del idioma (DD5-20); debajo de 0,8, "…" al final (AC-WKT-18-03).
            _titleDrawn = _titleText;
            var titleWidth = _titleFont.Measure(_titleText, TitleSizeDp);
            if (titleWidth * _titleScale > SeriesPanelGeometry.TitleRoom + 0.01)
            {
                _titleDrawn = DrawnText.Ellipsize(_titleText, (float)SeriesPanelGeometry.TitleRoom, (float)_titleScale, t => _titleFont.Measure(t, TitleSizeDp));
            }

            // Los vacíos en dos renglones parejos (DD5-19).
            SplitEmpty();
            var series = _series;
            if (series == null)
            {
                return;
            }

            var available = (float)(SeriesPanelGeometry.ContentRight - SeriesPanelGeometry.ContentLeft);
            if (Kind == SeriesPanelKind.Laps)
            {
                // La vuelta en curso: solo la cifra se achica, con los metros a 13 pegados a x 127 y al menos 6 de aire (DD5-24).
                _heroTimeWidth = _heroTime.Width(_numFont, HeroSize);
                _heroMetersWidth = _heroMeters.Width(_numFont, MetersSize);
                _kHero = (float)SeriesPanelGeometry.FitScale(available - _heroMetersWidth - SeriesPanelGeometry.HeroMetersGap, _heroTimeWidth,
                    SeriesPanelGeometry.SeriesNumberMinScale);
                // Las filas: la columna del rótulo del ancho de #99 (o del más ancho a la vista) y una escala común.
                _rowsColumn = _labelFont.Measure("#99", RowLabelSizeDp);
                var widest = 0f;
                var laps = series.Laps;
                for (var i = 0; i < laps.Count && i < MaxRows; i++)
                {
                    _rowLabelWidth[i] = _labelFont.Measure(laps[i].Label, RowLabelSizeDp);
                    _rowValueWidth[i] = TabularCells.Measure(_numFont, laps[i].Time, RowValueSize);
                    _rowsColumn = Math.Max(_rowsColumn, _rowLabelWidth[i]);
                }

                for (var i = 0; i < laps.Count && i < MaxRows; i++)
                {
                    widest = Math.Max(widest, _rowsColumn + (float)SeriesPanelGeometry.LabelGap + _rowValueWidth[i]);
                }

                _kRowsTo = (float)SeriesPanelGeometry.FitScale(available, widest, SeriesPanelGeometry.SeriesNumberMinScale);
                _kLine = series.RepProgress == null
                    ? 1
                    : (float)SeriesPanelGeometry.FitScale(available, TabularCells.Measure(_numFont, series.RepProgress, RepLineSize),
                        SeriesPanelGeometry.SeriesNumberMinScale);
            }
            else
            {
                // Las filas de repeticiones: una escala común para las tres piezas, con los metros pegados a x 127 (DD5-24); lo que
                // en el piso de 0,6 no entra se recorta con "…" al final del split (DT5-16).
                _rowsColumn = 0;
                var reps = series.Reps;
                var widestSplit = 0f;
                var widestMeters = 0f;
                for (var i = 0; i < reps.Count && i < MaxRows; i++)
                {
                    _rowLabelWidth[i] = _labelFont.Measure(reps[i].Label, RowLabelSizeDp);
                    _rowValueWidth[i] = TabularCells.Measure(_numFont, reps[i].Split, RowValueSize);
                    _rowMetersWidth[i] = TabularCells.Measure(_numFont, reps[i].Meters, MetersSize);
                    _rowsColumn = Math.Max(_rowsColumn, _rowLabelWidth[i]);
                    widestSplit = Math.Max(widestSplit, _rowValueWidth[i]);
                    widestMeters = Math.Max(widestMeters, _rowMetersWidth[i]);
                    _rowSplit[i] = reps[i].Split;
                }

                var fixedPart = _rowsColumn + (float)SeriesPanelGeometry.LabelGap + (float)SeriesPanelGeometry.MetersMinGap + widestMeters;
                _kRowsTo = (float)SeriesPanelGeometry.FitScale(available, fixedPart + widestSplit, SeriesPanelGeometry.SeriesNumberMinScale);
                var roomForSplit = available / _kRowsTo - fixedPart;
                for (var i = 0; i < reps.Count && i < MaxRows; i++)
                {
                    if (_rowValueWidth[i] > roomForSplit + 0.01f)
                    {
                        _rowSplit[i] = EllipsizeCells(reps[i].Split, roomForSplit);
                        _rowValueWidth[i] = TabularCells.Measure(_numFont, _rowSplit[i], RowValueSize);
                    }
                }
            }

            MarkDirtyRepaint();
        }

        // Las cifras que entran con "…" al final (el "…" en las celdas de la fuente numérica, o su avance).
        private string EllipsizeCells(string text, float room)
        {
            for (var length = text.Length - 1; length > 0; length--)
            {
                var candidate = text.Substring(0, length) + DrawnText.Ellipsis;
                if (TabularCells.Measure(_numFont, candidate, RowValueSize) <= room + 0.01f)
                {
                    return candidate;
                }
            }

            return DrawnText.Ellipsis;
        }

        private void SplitEmpty()
        {
            _emptyLine0 = _emptyText;
            _emptyLine1 = "";
            var words = _emptyText.Split(' ');
            if (words.Length < 2)
            {
                return;
            }

            var widths = new double[words.Length];
            for (var i = 0; i < words.Length; i++)
            {
                widths[i] = _bodyFont.Measure(words[i], EmptySizeDp);
            }

            var space = _bodyFont.Measure("a a", EmptySizeDp) - _bodyFont.Measure("aa", EmptySizeDp);
            var cut = SeriesPanelGeometry.BalancedBreak(widths, space);
            if (cut < 0)
            {
                // Ninguno entra en 117: el primer espacio (la regla del título queda para un idioma que no entre).
                cut = 0;
            }

            _emptyLine0 = string.Join(" ", words, 0, cut + 1);
            _emptyLine1 = string.Join(" ", words, cut + 1, words.Length - cut - 1);
        }

        // 1 = el empuje entero; 0 = el desplazamiento ya lo compensó entero (DD5-27); en el medio, lo que no cupo debajo del tope.
        private double _pushScale = 1;

        // El empuje de las filas que estaban: el de la motion, menos lo que el desplazamiento ya compensó (DD5-27).
        private double Push(in SeriesPanelVisual v)
        {
            return v.RowsPushBase * _pushScale;
        }

        // Para las pruebas (FrameAllocations, HBT): avisa al empezar (true) y al terminar (false) cada generateVisualContent.
        internal Action<bool>? DrawObserver { get; set; }

        // La escala común de las filas en este cuadro: llega a la nueva con el empuje (DD5-24).
        private float RowsScale()
        {
            var p = (float)Math.Max(0, Math.Min(1, _v.PushProgress));
            return _kRowsFrom + (_kRowsTo - _kRowsFrom) * p;
        }

        // ── El dibujo ──

        private void Draw(MeshGenerationContext context)
        {
            if (panel == null)
            {
                return;
            }

            var observer = DrawObserver;
            observer?.Invoke(true);
            _work.Restart();
            Walk(context, null);
            _work.Stop();
            observer?.Invoke(false);
            LastDrawMs = _work.Elapsed.TotalMilliseconds;
            DrawCount++;
        }

        // Las ranuras (las pruebas y el arnés; asigna).
        public int SlotCount
        {
            get
            {
                BuildSlots();
                return _slots!.Count;
            }
        }

        public DrawnSlot SlotAt(int index)
        {
            BuildSlots();
            return _slots![index];
        }

        // La ranura de ese nombre (o null).
        public DrawnSlot? Slot(string slotName)
        {
            BuildSlots();
            foreach (var slot in _slots!)
            {
                if (slot.Name == slotName)
                {
                    return slot;
                }
            }

            return null;
        }

        private void BuildSlots()
        {
            _slots ??= new List<DrawnSlot>(Slots);
            _slots.Clear();
            if (panel != null)
            {
                Walk(null, _slots);
            }
        }

        // Una pasada por todo lo que se ve: con context dibuja; con slots arma las ranuras. Lo mismo para los dos, así las pruebas
        // leen exactamente lo que se dibuja.
        private void Walk(MeshGenerationContext? context, List<DrawnSlot>? slots)
        {
            var ppp = scaledPixelsPerPoint;
            var v = _v;
            var g = v.HeightBase;
            var laps = Kind == SeriesPanelKind.Laps;
            var accent = laps ? Tokens.MetricSplit : Tokens.MetricDistance;

            if (context != null)
            {
                DrawGlass(context, g, v.HoverAlpha);
            }

            // La cabecera (y 8-26): el ícono, el rótulo corto o el título y la flecha.
            var headerTop = SeriesPanelGeometry.HeaderTop;
            var labelBaseline = headerTop + SeriesPanelGeometry.Baseline(SeriesRowKind.HeaderLabel);
            var chipAlpha = (float)(v.ChipLabelAlpha * RowAlpha(SeriesRowKind.HeaderLabel, headerTop, 1, _chipDescender, false));
            var iconAlpha = (float)RowAlpha(SeriesRowKind.HeaderLabel, headerTop, 1, false, false);
            if (context != null)
            {
                _iconFont.DrawGlyphCentered(context, laps ? Icons.Timer : Icons.History,
                    (float)(SeriesPanelGeometry.IconLeft + SeriesPanelGeometry.Icon / 2), (float)SeriesPanelGeometry.ChevronCenterY, IconSizeDp,
                    TokenColors.ToColor(accent), (float)SeriesPanelGeometry.IconAlpha * iconAlpha, ppp);
                _chipFont.DrawTracked(context, _chipText, (float)SeriesPanelGeometry.LabelLeft, (float)labelBaseline, ChipLabelSizeDp,
                    (float)SeriesPanelGeometry.LabelTrackingEm, TokenColors.ToColor(accent), chipAlpha, ppp);
            }
            else
            {
                AddSlot(slots!, laps ? "hud-laps-chip" : "hud-reps-chip", _chipText, SeriesPanelGeometry.LabelLeft, headerTop + 4.27,
                    _chipFont.MeasureTracked(_chipText, ChipLabelSizeDp, (float)SeriesPanelGeometry.LabelTrackingEm), 9.46, chipAlpha, 1, accent);
            }

            var k = (float)_titleScale;
            var titleSize = TitleSizeDp * k;
            var titleBaseline = headerTop + SeriesPanelGeometry.BandCenter(SeriesRowKind.HeaderTitle)
                                + (SeriesPanelGeometry.Baseline(SeriesRowKind.HeaderTitle) - SeriesPanelGeometry.BandCenter(SeriesRowKind.HeaderTitle)) * k;
            var titleAlpha = (float)(v.TitleAlpha * RowAlpha(SeriesRowKind.HeaderTitle, headerTop, k, _titleDescender, false));
            if (context != null)
            {
                _titleFont.Draw(context, _titleDrawn, (float)SeriesPanelGeometry.LabelLeft, (float)titleBaseline, titleSize, TokenColors.ToColor(accent),
                    titleAlpha, ppp);
                DrawChevron(context, v, (float)RowAlpha(SeriesRowKind.HeaderLabel, headerTop, 1, false, false));
            }
            else
            {
                AddSlot(slots!, laps ? "hud-laps-title" : "hud-reps-title", _titleDrawn, SeriesPanelGeometry.LabelLeft,
                    titleBaseline - 0.7275 * titleSize, _titleFont.Measure(_titleDrawn, titleSize), 0.7275 * titleSize, titleAlpha, k, accent);
            }

            var series = _series;
            if (series != null && v.ContentAlpha > 0.0005)
            {
                if (laps)
                {
                    WalkLaps(context, slots, series, v, ppp);
                }
                else
                {
                    WalkReps(context, slots, series, v, ppp);
                }
            }
            else if (slots != null)
            {
                // Cerrado: el contenido existe con alfa 0 (no se ve; la prueba lo puede leer).
                if (series != null)
                {
                    if (laps)
                    {
                        WalkLaps(null, slots, series, v, ppp);
                    }
                    else
                    {
                        WalkReps(null, slots, series, v, ppp);
                    }
                }
            }

            if (context != null && HasFocusRing)
            {
                DrawFocusRing(context.painter2D);
            }
        }

        private void WalkLaps(MeshGenerationContext? context, List<DrawnSlot>? slots, HudSeries series, in SeriesPanelVisual v, float ppp)
        {
            var content = v.ContentAlpha;
            var change = _change;
            var before = _before;
            var classChange = change.LapsBody == SeriesBodyChange.ToEmpty || change.LapsBody == SeriesBodyChange.FromEmpty;
            var newAlpha = classChange ? v.BodyNewAlpha : 1;
            var newRise = classChange ? v.BodyNewRiseBase : 0;
            var top = SeriesPanelGeometry.ContentTop;

            // Lo viejo que sale (un cambio de clase): la vuelta en curso o el vacío anterior, en su lugar.
            if (before != null && classChange && v.BodyOldAlpha > 0.0005)
            {
                if (before.IsWork)
                {
                    DrawHero(context, null, before.CurrentTime, before.CurrentMeters, null, null, top, v.BodyOldAlpha * content, 1, ppp, false);
                }
                else
                {
                    DrawEmpty(context, null, top, v.BodyOldAlpha * content, ppp, false);
                }
            }

            if (series.IsWork)
            {
                DrawHero(context, slots, series.CurrentTime, series.CurrentMeters, _heroTime, _heroMeters, top + newRise, newAlpha * content, _kHero, ppp, true);
            }
            else
            {
                DrawEmpty(context, slots, top + newRise, newAlpha * content, ppp, true);
            }

            // Las filas que se vacían (el paso nuevo empieza sin vueltas): salen en su lugar (0-120).
            if (before != null && change.StepChanged && v.ClearedRowsAlpha > 0.0005)
            {
                var oldRows = before.Laps;
                for (var i = 0; i < oldRows.Count && i < MaxRows; i++)
                {
                    DrawLapRow(context, null, -1, oldRows[i].Label, oldRows[i].Time, SeriesPanelGeometry.LapRowTop(i), v.ClearedRowsAlpha * content,
                        _kRowsFrom, ppp, true, before.LapCount, i);
                }
            }

            // Las filas (la más reciente arriba): las que estaban bajan con el empuje; las nuevas entran desde −8·sp (80-240).
            var rows = series.Laps;
            var kRows = RowsScale();
            var visible = Math.Min(rows.Count, MaxRows);
            var newRows = change.StepChanged ? Math.Min(v.NewRows, visible) : v.NewRows;
            for (var i = 0; i < visible; i++)
            {
                var isNew = i < newRows;
                var y = SeriesPanelGeometry.LapRowTop(i) + (isNew ? v.NewRowsShiftBase : Push(v)) - _rowsScroll;
                var alpha = (isNew ? v.NewRowsAlpha : 1) * content;
                DrawLapRow(context, slots, i, rows[i].Label, rows[i].Time, y, alpha, kRows, ppp, false, series.LapCount, i);
                DrawMarker(context, i, y, v, content, true);
            }

            // La que se va (había 4 a la vista): baja con las demás y se apaga (0-120) y por la ventana de filas.
            if (before != null && change.LapDropped && !change.StepChanged && v.DroppedAlpha > 0.0005 && before.Laps.Count >= MaxRows)
            {
                var gone = before.Laps[MaxRows - 1];
                DrawLapRow(context, null, -1, gone.Label, gone.Time, SeriesPanelGeometry.LapRowTop(MaxRows) + Push(v) - _rowsScroll,
                    v.DroppedAlpha * content, _kRowsFrom, ppp, false, before.LapCount, MaxRows);
            }

            // La línea Rep (con la serie): baja o sube con la cola; entra o sale con la serie.
            var line = series.RepProgress ?? (v.RepLineAlpha > 0.0005 && _beforeRepLine.Length > 0 ? _beforeRepLine : null);
            if (line != null)
            {
                var lineTop = SeriesPanelGeometry.RepLineTop(series.IsWork, visible) + v.TailShiftBase + v.RepLineRiseBase;
                var lineAlpha = v.RepLineAlpha * content * RowAlpha(SeriesRowKind.RepLine, lineTop, _kLine, true, false);
                var baseline = lineTop + SeriesPanelGeometry.BandCenter(SeriesRowKind.RepLine)
                               + (SeriesPanelGeometry.Baseline(SeriesRowKind.RepLine) - SeriesPanelGeometry.BandCenter(SeriesRowKind.RepLine)) * _kLine;
                var size = RepLineSize * _kLine;
                if (context != null)
                {
                    if (series.RepProgress != null)
                    {
                        // Anclada a la izquierda con su rótulo: rueda por tramos, solo lo que cambia (QA-V5-02).
                        _repLine.DrawFromLeft(context, _numFont, size, (float)SeriesPanelGeometry.ContentLeft, (float)baseline,
                            TokenColors.ToColor(Tokens.MetricDistanceHud), (float)lineAlpha, ppp);
                    }
                    else
                    {
                        TabularCells.DrawPlain(context, _numFont, line, size, (float)SeriesPanelGeometry.ContentLeft + TabularCells.Measure(_numFont, line, size),
                            (float)baseline, TokenColors.ToColor(Tokens.MetricDistanceHud), (float)lineAlpha, ppp);
                    }
                }
                else if (series.RepProgress != null)
                {
                    AddSlot(slots!, "hud-rep-progress", series.RepProgress, SeriesPanelGeometry.ContentLeft, baseline - 0.7 * size,
                        TabularCells.Measure(_numFont, series.RepProgress, size), 0.7 * size, lineAlpha, _kLine, Tokens.MetricDistanceHud);
                }
            }
        }

        private void WalkReps(MeshGenerationContext? context, List<DrawnSlot>? slots, HudSeries series, in SeriesPanelVisual v, float ppp)
        {
            var content = v.ContentAlpha;
            var change = _change;
            var before = _before;
            var classChange = change.RepsBody == SeriesBodyChange.ToEmpty || change.RepsBody == SeriesBodyChange.FromEmpty;
            var top = SeriesPanelGeometry.ContentTop;

            if (before != null && classChange && v.BodyOldAlpha > 0.0005)
            {
                if (before.Reps.Count > 0)
                {
                    for (var i = 0; i < before.Reps.Count && i < SeriesTracker.MaxPreviousReps; i++)
                    {
                        var row = before.Reps[i];
                        DrawRepRow(context, null, -1, row.Label, row.Split, row.Meters, SeriesPanelGeometry.RepRowTop(i) - _rowsScroll,
                            v.BodyOldAlpha * content, _kRowsFrom, ppp, i);
                    }
                }
                else
                {
                    DrawEmpty(context, null, top, v.BodyOldAlpha * content, ppp, false);
                }
            }

            var rows = series.Reps;
            var newAlpha = classChange ? v.BodyNewAlpha : 1;
            var newRise = classChange ? v.BodyNewRiseBase : 0;
            if (rows.Count == 0)
            {
                DrawEmpty(context, slots, top + newRise, newAlpha * content, ppp, true);
                return;
            }

            var kRows = RowsScale();
            var visible = Math.Min(rows.Count, SeriesTracker.MaxPreviousReps);
            for (var i = 0; i < visible; i++)
            {
                var isNew = !classChange && i < v.NewRows;
                double y;
                double alpha;
                if (classChange)
                {
                    y = SeriesPanelGeometry.RepRowTop(i) + newRise - _rowsScroll;
                    alpha = newAlpha * content;
                }
                else
                {
                    y = SeriesPanelGeometry.RepRowTop(i) + (isNew ? v.NewRowsShiftBase : Push(v)) - _rowsScroll;
                    alpha = (isNew ? v.NewRowsAlpha : 1) * content;
                }

                DrawRepRow(context, slots, i, rows[i].Label, i < MaxRows ? _rowSplit[i] ?? rows[i].Split : rows[i].Split, rows[i].Meters, y, alpha,
                    kRows, ppp, i);
                DrawMarker(context, i, y, v, content, false);
            }

            if (before != null && change.RepDropped && !classChange && v.DroppedAlpha > 0.0005 && before.Reps.Count >= SeriesTracker.MaxPreviousReps)
            {
                var gone = before.Reps[SeriesTracker.MaxPreviousReps - 1];
                DrawRepRow(context, null, -1, gone.Label, gone.Split, gone.Meters,
                    SeriesPanelGeometry.RepRowTop(SeriesTracker.MaxPreviousReps) + Push(v) - _rowsScroll, v.DroppedAlpha * content, _kRowsFrom, ppp,
                    SeriesTracker.MaxPreviousReps);
            }
        }

        // La vuelta en curso: la cifra num.lap 28 desde x 10 (con su escala, anclada en la línea de base) y los metros num.xs 13
        // pegados a x 127, por la misma línea de base (SD-16).
        private void DrawHero(MeshGenerationContext? context, List<DrawnSlot>? slots, string time, string meters, TabularCells? timeCells,
                              TabularCells? metersCells, double rowTop, double motionAlpha, float k, float ppp, bool current)
        {
            var baseline = rowTop + SeriesPanelGeometry.Baseline(SeriesRowKind.Hero);
            var alpha = (float)(motionAlpha * RowAlpha(SeriesRowKind.Hero, rowTop, 1, false, false));
            var timeSize = HeroSize * k;
            var left = (float)SeriesPanelGeometry.ContentLeft;
            var right = (float)SeriesPanelGeometry.ContentRight;
            var timeColor = TokenColors.ToColor(Tokens.MetricSplitHud);
            var metersColor = DrawnText.WithAlpha(Tokens.MetricSplitHud, SeriesPanelGeometry.HeroMetersAlpha);
            if (context != null)
            {
                if (timeCells != null)
                {
                    timeCells.Draw(context, _numFont, timeSize, left + timeCells.WidthNow(_numFont, timeSize), (float)baseline, timeColor, alpha, ppp);
                    metersCells!.Draw(context, _numFont, MetersSize, right, (float)baseline, metersColor, alpha, ppp);
                }
                else
                {
                    TabularCells.DrawPlain(context, _numFont, time, timeSize, left + TabularCells.Measure(_numFont, time, timeSize), (float)baseline, timeColor,
                        alpha, ppp);
                    TabularCells.DrawPlain(context, _numFont, meters, MetersSize, right, (float)baseline, metersColor, alpha, ppp);
                }
            }
            else if (slots != null && current)
            {
                var timeWidth = TabularCells.Measure(_numFont, time, timeSize);
                AddSlot(slots, "hud-laps-current", time, left, baseline - 0.7 * timeSize, timeWidth, 0.7 * timeSize, alpha, k, Tokens.MetricSplitHud);
                var metersWidth = TabularCells.Measure(_numFont, meters, MetersSize);
                AddSlot(slots, "hud-laps-current-meters", meters, right - metersWidth, baseline - 0.7 * MetersSize, metersWidth, 0.7 * MetersSize, alpha, 1,
                    Tokens.MetricSplitHud);
            }
        }

        // Un vacío en dos renglones parejos (body 15/22, text.tertiary), y 32 y 54 (DD5-19).
        private void DrawEmpty(MeshGenerationContext? context, List<DrawnSlot>? slots, double top, double motionAlpha, float ppp, bool current)
        {
            var color = TokenColors.ToColor(Tokens.TextTertiary);
            var alpha0 = (float)(motionAlpha * RowAlpha(SeriesRowKind.EmptyLine, top, 1, true, false));
            var top1 = top + SeriesPanelGeometry.EmptyLineHeight;
            var alpha1 = (float)(motionAlpha * RowAlpha(SeriesRowKind.EmptyLine, top1, 1, true, false));
            var baseline0 = top + SeriesPanelGeometry.Baseline(SeriesRowKind.EmptyLine);
            var baseline1 = top1 + SeriesPanelGeometry.Baseline(SeriesRowKind.EmptyLine);
            var left = (float)SeriesPanelGeometry.ContentLeft;
            if (context != null)
            {
                _bodyFont.Draw(context, _emptyLine0, left, (float)baseline0, EmptySizeDp, color, alpha0, ppp);
                _bodyFont.Draw(context, _emptyLine1, left, (float)baseline1, EmptySizeDp, color, alpha1, ppp);
            }
            else if (slots != null && current)
            {
                var width = Math.Max(_bodyFont.Measure(_emptyLine0, EmptySizeDp), _bodyFont.Measure(_emptyLine1, EmptySizeDp));
                var rect = LocalRect(left, top + SeriesPanelGeometry.InkTop(SeriesRowKind.EmptyLine), width,
                    top1 + SeriesPanelGeometry.InkBottom(SeriesRowKind.EmptyLine) - (top + SeriesPanelGeometry.InkTop(SeriesRowKind.EmptyLine)));
                slots.Add(new DrawnSlot(Kind == SeriesPanelKind.Laps ? "hud-laps-empty" : "hud-reps-empty", _emptyText, rect, Math.Min(alpha0, alpha1), 1,
                    color, _emptyLine0, _emptyLine1));
            }
        }

        // Una fila de vuelta: `#n` 12/600 text.secondary en su columna y el tiempo num.row 20 a 4, por la misma línea de base; con
        // la escala común (anclada en x 10 y la banda centrada).
        private void DrawLapRow(MeshGenerationContext? context, List<DrawnSlot>? slots, int slot, string label, string time, double rowTop, double motionAlpha,
                                float k, float ppp, bool ignoreWindow, int lapCount, int index)
        {
            var alpha = (float)(motionAlpha * RowAlpha(SeriesRowKind.LapRow, rowTop, k, false, !ignoreWindow));
            if (alpha <= 0.0005f && slots == null)
            {
                return;
            }

            var center = rowTop + SeriesPanelGeometry.BandCenter(SeriesRowKind.LapRow);
            var baseline = center + (SeriesPanelGeometry.Baseline(SeriesRowKind.LapRow) - SeriesPanelGeometry.BandCenter(SeriesRowKind.LapRow)) * k;
            var left = (float)SeriesPanelGeometry.ContentLeft;
            var valueLeft = left + (_rowsColumn + (float)SeriesPanelGeometry.LabelGap) * k;
            var valueSize = RowValueSize * k;
            if (context != null)
            {
                _labelFont.Draw(context, label, left, (float)baseline, RowLabelSizeDp * k, TokenColors.ToColor(Tokens.TextSecondary), alpha, ppp);
                TabularCells.DrawPlain(context, _numFont, time, valueSize, valueLeft + TabularCells.Measure(_numFont, time, valueSize), (float)baseline,
                    TokenColors.ToColor(Tokens.MetricSplitHud), alpha, ppp);
            }
            else if (slots != null && slot >= 0)
            {
                var right = valueLeft + TabularCells.Measure(_numFont, time, valueSize);
                AddSlot(slots, "hud-laps-row-" + slot, label + " " + time, left, center - 7 * k, right - left, 14 * k, alpha, k, Tokens.MetricSplitHud);
            }
        }

        // Una fila de repetición: `Rep n` en su columna, el split a 4 y los metros pegados a x 127, con la escala común.
        private void DrawRepRow(MeshGenerationContext? context, List<DrawnSlot>? slots, int slot, string label, string split, string meters, double rowTop,
                                double motionAlpha, float k, float ppp, int index)
        {
            var alpha = (float)(motionAlpha * RowAlpha(SeriesRowKind.RepRow, rowTop, k, true, true));
            if (alpha <= 0.0005f && slots == null)
            {
                return;
            }

            var center = rowTop + SeriesPanelGeometry.BandCenter(SeriesRowKind.RepRow);
            var baseline = center + (SeriesPanelGeometry.Baseline(SeriesRowKind.RepRow) - SeriesPanelGeometry.BandCenter(SeriesRowKind.RepRow)) * k;
            var left = (float)SeriesPanelGeometry.ContentLeft;
            var right = (float)SeriesPanelGeometry.ContentRight;
            var valueLeft = left + (_rowsColumn + (float)SeriesPanelGeometry.LabelGap) * k;
            var valueSize = RowValueSize * k;
            var metersSize = MetersSize * k;
            if (context != null)
            {
                _labelFont.Draw(context, label, left, (float)baseline, RowLabelSizeDp * k, TokenColors.ToColor(Tokens.TextSecondary), alpha, ppp);
                TabularCells.DrawPlain(context, _numFont, split, valueSize, valueLeft + TabularCells.Measure(_numFont, split, valueSize), (float)baseline,
                    TokenColors.ToColor(Tokens.MetricSplitHud), alpha, ppp);
                TabularCells.DrawPlain(context, _numFont, meters, metersSize, right, (float)baseline, TokenColors.ToColor(Tokens.MetricDistanceHud), alpha, ppp);
            }
            else if (slots != null && slot >= 0)
            {
                AddSlot(slots, "hud-reps-row-" + slot, label + " " + split + " " + meters, left, center - 7 * k, right - left, 14 * k, alpha, k,
                    Tokens.MetricSplitHud);
            }
        }

        // La marca de la fila nueva (3 × 20 base, radio 1,5, x 4-7; DT5-03): por fila dibujada (D1-06).
        private void DrawMarker(MeshGenerationContext? context, int row, double rowTop, in SeriesPanelVisual v, double content, bool laps)
        {
            if (context == null)
            {
                return;
            }

            var marker = row == 0 ? v.MarkerAlpha0 : row == 1 ? v.MarkerAlpha1 : row == 2 ? v.MarkerAlpha2 : row == 3 ? v.MarkerAlpha3 : 0;
            if (marker <= 0.0005)
            {
                return;
            }

            var alpha = marker * content * RowAlpha(laps ? SeriesRowKind.LapRow : SeriesRowKind.RepRow, rowTop, 1, !laps, true);
            if (alpha <= 0.0005)
            {
                return;
            }

            var painter = context.painter2D;
            var color = DrawnText.WithAlpha(laps ? Tokens.MetricSplit : Tokens.MetricDistance, alpha);
            painter.fillColor = color;
            RoundedRect(painter, (float)SeriesPanelGeometry.MarkerLeft, (float)rowTop, (float)SeriesPanelGeometry.MarkerWidth,
                (float)SeriesPanelGeometry.MarkerHeight, (float)SeriesPanelGeometry.MarkerRadius);
            painter.Fill();
        }

        // ── Las reglas de alfa por la tinta (DT5-02; DD5-13 a DD5-15) ──

        private double RowAlpha(SeriesRowKind kind, double rowTop, double scale, bool descender, bool rowsWindow)
        {
            var inkTop = SeriesPanelGeometry.InkTopAt(kind, rowTop, scale);
            var inkBottom = SeriesPanelGeometry.InkBottomAt(kind, rowTop, scale, descender);
            // Regla 1: revelar contra el vidrio.
            var alpha = SeriesPanelGeometry.RevealAlpha(_v.HeightBase, inkBottom);
            if (rowsWindow && alpha > 0)
            {
                // Regla 2: la ventana de filas (abajo siempre, con la cola; arriba solo desplazada).
                var windowTop = Kind == SeriesPanelKind.Laps ? SeriesPanelGeometry.LapRowsTop : SeriesPanelGeometry.ContentTop;
                var rowsHeight = _viewport > 0 ? _viewport : _rowsContent;
                var windowBottom = windowTop + rowsHeight + _v.TailShiftBase;
                alpha *= SeriesPanelGeometry.RowsEdgeAlpha(inkTop, inkBottom, windowTop, windowBottom, _rowsScroll > 0.001);
            }

            if (_maskOn && alpha > 0)
            {
                // Regla 3: la máscara de la columna (solo ColumnScroll).
                alpha *= SeriesPanelGeometry.ColumnMaskAlpha(inkTop, inkBottom, _maskTop, _maskBottom, _maskHiddenAbove, _maskHiddenBelow);
            }

            return alpha;
        }

        // ── El vidrio, la flecha, la capa de estado y el anillo ──

        private void DrawGlass(MeshGenerationContext context, double g, double hover)
        {
            var painter = context.painter2D;
            var width = (float)_widthBase;
            var height = (float)g;
            var radius = (float)SeriesPanelGeometry.GlassRadius;
            var glass = TokenColors.ToColor(Tokens.GlassFallback);
            var border = DrawnText.WithAlpha(Tokens.TextPrimary, SeriesPanelGeometry.BorderAlpha);
            if (!MaskNeeded(g))
            {
                painter.fillColor = glass;
                RoundedRect(painter, 0, 0, width, height, radius);
                painter.Fill();
                painter.strokeColor = border;
                painter.lineWidth = 1;
                RoundedRect(painter, 0.5f, 0.5f, width - 1, height - 1, radius - 0.5f);
                painter.Stroke();
            }
            else
            {
                DrawMaskedGlass(context, g, glass, border);
            }

            if (hover > 0.0005)
            {
                // La capa de hover y presionado: la banda de la cabecera (y 0-34) con los radios de arriba (§4.9).
                painter.fillColor = DrawnText.WithAlpha(Tokens.TextPrimary, hover);
                var bottom = (float)Math.Min(g, SeriesPanelGeometry.StateLayerBottom);
                painter.BeginPath();
                painter.MoveTo(new Vector2(radius, 0));
                painter.LineTo(new Vector2(width - radius, 0));
                painter.ArcTo(new Vector2(width, 0), new Vector2(width, radius), radius);
                if (g <= SeriesPanelGeometry.StateLayerBottom + 1e-3)
                {
                    painter.LineTo(new Vector2(width, bottom - radius));
                    painter.ArcTo(new Vector2(width, bottom), new Vector2(width - radius, bottom), radius);
                    painter.LineTo(new Vector2(radius, bottom));
                    painter.ArcTo(new Vector2(0, bottom), new Vector2(0, bottom - radius), radius);
                }
                else
                {
                    painter.LineTo(new Vector2(width, bottom));
                    painter.LineTo(new Vector2(0, bottom));
                }

                painter.LineTo(new Vector2(0, radius));
                painter.ArcTo(new Vector2(0, 0), new Vector2(radius, 0), radius);
                painter.ClosePath();
                painter.Fill();
            }
        }

        private bool MaskNeeded(double g)
        {
            if (!_maskOn)
            {
                return false;
            }

            return SeriesPanelGeometry.ColumnMaskAlpha(0, g, _maskTop, _maskBottom, _maskHiddenAbove, _maskHiddenBelow) < 0.9995;
        }

        // El vidrio con la máscara de la columna (regla 3, DD5-15): el relleno y el borde en franjas horizontales con el alfa por
        // vértice (Allocate), con el contorno redondeado por fila.
        private void DrawMaskedGlass(MeshGenerationContext context, double g, Color glass, Color border)
        {
            var width = _widthBase;
            var n = MaskSegments;
            for (var i = 0; i <= n; i++)
            {
                var y = g * i / n;
                var inset = CornerInset(y, g);
                var mask = SeriesPanelGeometry.ColumnMaskAlpha(y, y, _maskTop, _maskBottom, _maskHiddenAbove, _maskHiddenBelow);
                var c = glass;
                c.a = (float)(glass.a * mask);
                Color32 tint = c;
                _maskVertices[i * 2] = new Vertex { position = new Vector3((float)inset, (float)y, Vertex.nearZ), tint = tint };
                _maskVertices[i * 2 + 1] = new Vertex { position = new Vector3((float)(width - inset), (float)y, Vertex.nearZ), tint = tint };
            }

            var mesh = context.Allocate(_maskVertices.Length, MaskIndices.Length);
            mesh.SetAllVertices(_maskVertices);
            mesh.SetAllIndices(MaskIndices);

            // El borde: los dos lados, cada uno con su alfa por vértice (1 base de ancho, por dentro).
            for (var side = 0; side < 2; side++)
            {
                for (var i = 0; i <= n; i++)
                {
                    var y = g * i / n;
                    var inset = CornerInset(y, g);
                    var mask = SeriesPanelGeometry.ColumnMaskAlpha(y, y, _maskTop, _maskBottom, _maskHiddenAbove, _maskHiddenBelow);
                    var c = border;
                    c.a = (float)(border.a * mask);
                    Color32 tint = c;
                    var x0 = side == 0 ? inset : width - inset - 1;
                    _maskVertices[i * 2] = new Vertex { position = new Vector3((float)x0, (float)y, Vertex.nearZ), tint = tint };
                    _maskVertices[i * 2 + 1] = new Vertex { position = new Vector3((float)(x0 + 1), (float)y, Vertex.nearZ), tint = tint };
                }

                var edge = context.Allocate(_maskVertices.Length, MaskIndices.Length);
                edge.SetAllVertices(_maskVertices);
                edge.SetAllIndices(MaskIndices);
            }
        }

        private static double CornerInset(double y, double g)
        {
            var r = SeriesPanelGeometry.GlassRadius;
            double d;
            if (y < r)
            {
                d = r - y;
            }
            else if (y > g - r)
            {
                d = y - (g - r);
            }
            else
            {
                return 0;
            }

            d = Math.Min(d, r);
            return r - Math.Sqrt(Math.Max(0, r * r - d * d));
        }

        // La flecha (§4.3): el trazo ∨ de 1,5 que gira alrededor de (122,5; 17); con reducir movimiento, ∨ y ∧ con su fundido.
        private void DrawChevron(MeshGenerationContext context, in SeriesPanelVisual v, float rowAlpha)
        {
            var painter = context.painter2D;
            painter.lineWidth = (float)SeriesPanelGeometry.SeriesChevronStroke;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            var down = v.ChevronDownAlpha * rowAlpha;
            if (down > 0.0005)
            {
                StrokeChevron(painter, v.ChevronDegrees, down);
            }

            var up = v.ChevronUpAlpha * rowAlpha;
            if (up > 0.0005)
            {
                StrokeChevron(painter, v.ChevronDegrees + 180, up);
            }
        }

        private static void StrokeChevron(Painter2D painter, double degrees, double alpha)
        {
            painter.strokeColor = DrawnText.WithAlpha(Tokens.TextPrimary, SeriesPanelGeometry.ChevronAlpha * alpha);
            var cx = SeriesPanelGeometry.ChevronCenterX;
            var cy = SeriesPanelGeometry.ChevronCenterY;
            var rad = degrees * Math.PI / 180.0;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);
            var hw = SeriesPanelGeometry.ChevronHalfWidth;
            var hh = SeriesPanelGeometry.ChevronHalfHeight;
            painter.BeginPath();
            painter.MoveTo(Rotate(-hw, -hh, cx, cy, cos, sin));
            painter.LineTo(Rotate(0, hh, cx, cy, cos, sin));
            painter.LineTo(Rotate(hw, -hh, cx, cy, cos, sin));
            painter.Stroke();
        }

        private static Vector2 Rotate(double x, double y, double cx, double cy, double cos, double sin)
        {
            return new Vector2((float)(cx + x * cos - y * sin), (float)(cy + x * sin + y * cos));
        }

        // Lo que rodea el anillo de foco, en base del panel (de arriba a abajo; el anillo y su halo van de 2 a 8 dp por fuera): el
        // vidrio entero, o en ColumnScroll la parte que queda en la ventana de la columna, con lugar para el halo adentro (LM5-02:
        // el anillo se recorta a la ventana como el resto del panel; su borde de afuera nunca pasa el techo ni el piso, así no cae
        // sobre la banda, que en L640 empieza 7,1 dp debajo del piso). Vacío (Bottom ≤ Top) si no queda lugar.
        internal (double Top, double Bottom) FocusRingSpanBase
        {
            get
            {
                var g = _v.HeightBase;
                if (!_maskOn)
                {
                    return (0, g);
                }

                var halo = SeriesPanelGeometry.FocusHaloOuterDp / (_scale > 0 ? _scale : 1);
                return (Math.Max(0, _maskTop + halo), Math.Min(g, _maskBottom - halo));
            }
        }

        // El anillo de foco en dp de ventana (DD5-22): blanco de −4 a −2 y halo overlay.scrim @50 % de −8 a −4 del borde visual,
        // radios 14·sp + 4 y 14·sp + 8 (el trazo se divide por sp: el elemento está escalado).
        private void DrawFocusRing(Painter2D painter)
        {
            var span = FocusRingSpanBase;
            var top = span.Top;
            var height = span.Bottom - span.Top;
            if (height <= 1e-3)
            {
                return;
            }

            var sp = _scale > 0 ? _scale : 1;
            var width = _widthBase;
            var r = Math.Min(SeriesPanelGeometry.GlassRadius, height / 2);
            // El halo: centro a −6 dp, 4 dp de ancho.
            var haloOffset = (SeriesPanelGeometry.FocusRingOuterDp + SeriesPanelGeometry.FocusHaloOuterDp) / 2 / sp;
            painter.strokeColor = DrawnText.WithAlpha(Tokens.OverlayScrim, SeriesPanelGeometry.FocusHaloAlpha);
            painter.lineWidth = (float)((SeriesPanelGeometry.FocusHaloOuterDp - SeriesPanelGeometry.FocusRingOuterDp) / sp);
            RoundedRect(painter, (float)-haloOffset, (float)(top - haloOffset), (float)(width + 2 * haloOffset), (float)(height + 2 * haloOffset),
                (float)(r + haloOffset));
            painter.Stroke();
            // El anillo: centro a −3 dp, 2 dp de ancho.
            var ringOffset = (SeriesPanelGeometry.FocusRingInnerDp + SeriesPanelGeometry.FocusRingOuterDp) / 2 / sp;
            painter.strokeColor = TokenColors.ToColor(Tokens.TextPrimary);
            painter.lineWidth = (float)((SeriesPanelGeometry.FocusRingOuterDp - SeriesPanelGeometry.FocusRingInnerDp) / sp);
            RoundedRect(painter, (float)-ringOffset, (float)(top - ringOffset), (float)(width + 2 * ringOffset), (float)(height + 2 * ringOffset),
                (float)(r + ringOffset));
            painter.Stroke();
        }

        private bool InKeyboardMode()
        {
            for (var e = hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e.ClassListContains(FocusHighlight.KeyboardNavClass))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RoundedRect(Painter2D painter, float x, float y, float width, float height, float radius)
        {
            radius = Math.Max(0, Math.Min(radius, Math.Min(width, height) / 2f));
            painter.BeginPath();
            painter.MoveTo(new Vector2(x + radius, y));
            painter.LineTo(new Vector2(x + width - radius, y));
            painter.ArcTo(new Vector2(x + width, y), new Vector2(x + width, y + radius), radius);
            painter.LineTo(new Vector2(x + width, y + height - radius));
            painter.ArcTo(new Vector2(x + width, y + height), new Vector2(x + width - radius, y + height), radius);
            painter.LineTo(new Vector2(x + radius, y + height));
            painter.ArcTo(new Vector2(x, y + height), new Vector2(x, y + height - radius), radius);
            painter.LineTo(new Vector2(x, y + radius));
            painter.ArcTo(new Vector2(x, y), new Vector2(x + radius, y), radius);
            painter.ClosePath();
        }

        // ── Las ranuras ──

        private void AddSlot(List<DrawnSlot> slots, string slotName, string text, double left, double top, double width, double height, double alpha,
                             double scale, ColorToken color)
        {
            slots.Add(new DrawnSlot(slotName, text, LocalRect(left, top, width, height), (float)alpha, (float)scale, TokenColors.ToColor(color)));
        }

        private Rect LocalRect(double left, double top, double width, double height)
        {
            return this.LocalToWorld(new Rect((float)left, (float)top, (float)Math.Max(0, width), (float)Math.Max(0, height)));
        }

        // ── El toque, el teclado y la rueda (§3.3, §3.5; 04-design-notes §6.10) ──

        private bool Responds => _interactive && !_disposed && !_ui.Dialogs.HasOpen;

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            evt.StopPropagation();
            if (!Responds)
            {
                return;
            }

            // Un mismo toque de Espacio cambia el panel una sola vez (QA-G5T-09).
            if (Time.realtimeSinceStartupAsDouble - _spaceAt < 0.05)
            {
                return;
            }

            _activate(Kind);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Space && evt.character != ' ')
            {
                return;
            }

            evt.StopPropagation();
            if (!Responds || evt.keyCode != KeyCode.Space)
            {
                return;
            }

            _spaceAt = Time.realtimeSinceStartupAsDouble;
            _activate(Kind);
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse || !Responds)
            {
                return;
            }

            _hovered = true;
            Motion.Hover(true, _pressed, false);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
            {
                return;
            }

            _hovered = false;
            if (_pointerId < 0)
            {
                Motion.Hover(false, false, false);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!Responds || evt.button != 0)
            {
                return;
            }

            _pointerId = evt.pointerId;
            _pointerStart = evt.position;
            _lastDrag = evt.position.y;
            _dragging = false;
            _pressed = true;
            this.CapturePointer(evt.pointerId);
            Motion.Hover(_hovered, true, false);
            evt.StopPropagation();
        }

        private float _lastDrag;

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            var delta = (Vector2)evt.position - _pointerStart;
            if (!_dragging && delta.magnitude >= SeriesPanelGeometry.DragSlopDp)
            {
                // Pasa a arrastre: ya no va a abrir ni cerrar (AC-WKT-18-13).
                _dragging = true;
                _pressed = false;
                Motion.Hover(_hovered && evt.pointerType == UnityEngine.UIElements.PointerType.mouse, false, false);
            }

            if (_dragging)
            {
                var step = evt.position.y - _lastDrag;
                _lastDrag = evt.position.y;
                Dragged?.Invoke(this, step);
            }

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            var tap = !_dragging && ((Vector2)evt.position - _pointerStart).magnitude < SeriesPanelGeometry.DragSlopDp;
            ReleaseCapture();
            _pressed = false;
            var stillOver = evt.pointerType == UnityEngine.UIElements.PointerType.mouse && _hovered;
            Motion.Hover(stillOver, false, false);
            evt.StopPropagation();
            if (tap && Responds)
            {
                _activate(Kind);
            }
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != _pointerId)
            {
                return;
            }

            ReleaseCapture();
            _pressed = false;
            Motion.Hover(false, false, false);
        }

        private void ReleaseCapture()
        {
            if (_pointerId >= 0)
            {
                if (this.HasPointerCapture(_pointerId))
                {
                    this.ReleasePointer(_pointerId);
                }

                _pointerId = -1;
            }

            _dragging = false;
        }

        private void OnWheel(WheelEvent evt)
        {
            if (!Responds)
            {
                return;
            }

            if (Wheeled != null && Wheeled(this, evt.delta.y))
            {
                evt.StopPropagation();
            }
        }

        // Desplaza las filas de este panel (RowsScroll): el arrastre 1:1 (en el acto, sin inercia) o la rueda de a una fila.
        public bool ScrollRowsBy(double deltaBase, bool wheel)
        {
            var max = RowsScrollMaxBase;
            if (max <= 0.001 || !Motion.Expanded)
            {
                return false;
            }

            var from = wheel ? RowsWheel.ScrollTargetBase : _rowsScroll;
            var to = Math.Max(0, Math.Min(max, from + deltaBase));
            RowsWheel.WheelTo(to, !wheel);
            return true;
        }

        private static ushort[] BuildMaskIndices()
        {
            var indices = new ushort[MaskSegments * 6];
            for (var i = 0; i < MaskSegments; i++)
            {
                var a = (ushort)(i * 2);
                indices[i * 6] = a;
                indices[i * 6 + 1] = (ushort)(a + 1);
                indices[i * 6 + 2] = (ushort)(a + 3);
                indices[i * 6 + 3] = a;
                indices[i * 6 + 4] = (ushort)(a + 3);
                indices[i * 6 + 5] = (ushort)(a + 2);
            }

            return indices;
        }
    }
}
