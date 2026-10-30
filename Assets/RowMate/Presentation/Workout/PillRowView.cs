using System;
using System.Diagnostics;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La fila de píldoras de la barra de pasos (F-WKT-15; AC-WKT-14-07 a -13, -16, -17; AC-WKT-15-01 a -05; legacy
    // immersive_workout_screen.dart:358-377 y step_pill.dart; 04b-hud-exr/02-tech-design §2.1 a §2.3, §3.4; 04-design-notes §3.2,
    // §4.3 a §4.5; DN4b-17 a -19; DT4b-01):
    //   #hud-pill-row .rm-hud-pill-row     UN elemento, alto 20 (las píldoras en 10 / 8 / 10 / 4), sin eventos ni foco
    // Todo lo dibuja su generateVisualContent, en este orden: (1) el halo de la en curso (y el de la que termina, que se apaga)
    // con PillGlowTexture en 9 partes, solo por fuera; (2) cada píldora con su pintura (StepPills.Paint) en cápsulas de
    // Painter2D (radio 4); (3) el relleno de la en curso al 100 %, recortado por la cápsula, con su brillo de 1 @25 % arriba;
    // (4) el borde de la hueca (1,5 @0,85, por dentro), entero o como trazo parcial en el cambio de paso, y en las angostas los
    // dos rieles; (5) el destello blanco de la que se solidifica; (6) en las angostas, el pulgar de la en curso por encima.
    // Bordes en píxeles enteros (PillRowGeometry.PixelEdges): todos los lugares del mismo ancho ± 1 px. Con tramos de menos de
    // un píxel (la tira), por columnas de píxel (PillShapes.StripRuns): el costo no crece con n (DT4b-21).
    // El movimiento es PillRowMotion (WP-D1): el relleno continuo (PillProgress), el halo de los últimos 5 s, el cambio de paso y
    // el pulgar que pasa; la fila solo dibuja lo que dice su Frame. Sin panel no dibuja ni mide: con DetachFromPanelEvent se
    // detiene y, al volver, aplica el último estado en el acto (QA-G4bT-08 (b)). Sin asignaciones por cuadro.
    public sealed class PillRowView : VisualElement, IDisposable
    {
        public const string UssClass = "rm-hud-pill-row";

        private readonly UiContext _ui;
        private readonly PillRowMotion _motion;
        private readonly PolygonBuffer _outline = new PolygonBuffer(64);
        private readonly PolygonBuffer _scratch = new PolygonBuffer(64);
        private readonly Vertex[] _glowVertices = new Vertex[36];
        private readonly Vertex[] _quad = new Vertex[4];
        private MeshGenerationContext? _context;
        private readonly Stopwatch _work = new Stopwatch();
        private Texture2D? _glow;
        private int[] _left = new int[0];
        private int[] _right = new int[0];
        private double[] _stripLeft = new double[0];
        private double[] _stripRight = new double[0];
        private double[] _stripR = new double[0];
        private double[] _stripG = new double[0];
        private double[] _stripB = new double[0];
        private double[] _stripA = new double[0];
        private StripRun[] _runs = new StripRun[0];
        private HudPillRow? _pending;
        private bool _pendingInstant;
        private bool _disposed;
        private PillRowLayout _layout;

        private static readonly ushort[] GlowIndices = BuildGlowIndices();

        // Arriba a la izquierda, arriba a la derecha, abajo a la izquierda, abajo a la derecha (como el 9-slice del halo).
        private static readonly ushort[] QuadIndices = { 0, 1, 3, 0, 3, 2 };

        public PillRowView(UiContext ui, StepPulse pulse)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "hud-pill-row";
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            focusable = false;
            style.height = (float)PillRowGeometry.RowHeight;
            style.flexShrink = 0;
            _motion = new PillRowMotion(ui.Motion, pulse, MarkDirtyRepaint);
            generateVisualContent += Draw;
            RegisterCallback<DetachFromPanelEvent>(_ => _motion.Stop());
            RegisterCallback<AttachToPanelEvent>(_ => ApplyPending());
            RegisterCallback<GeometryChangedEvent>(_ => UpdateFillWidth());
        }

        public PillRowMotion Motion => _motion;

        // El relleno continuo (lo comparte la cuenta grande: el aro).
        public PillProgress Progress => _motion.Progress;

        public HudPillRow? Row => _motion.Row ?? _pending;

        public int Count => Row?.Types.Count ?? 0;

        // La geometría del último dibujo (para las pruebas: el régimen, el pulgar, los anchos).
        public PillRowLayout CurrentLayout => ComputeLayout();

        // El aspecto de la píldora n (DT4b-01: las pruebas leen cada píldora por índice).
        public StepPillLook LookAt(int index)
        {
            var row = Row;
            if (row == null || index < 0 || index >= row.Looks.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return row.Looks[index];
        }

        // El rectángulo local (dp base) de la píldora n tal como se dibuja (con los bordes en píxeles); el pulgar en las angostas.
        public Rect RectOf(int index)
        {
            var layout = ComputeLayout();
            if (index < 0 || index >= layout.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            EnsureEdges(layout);
            var rect = SnappedPill(layout, index);
            return new Rect((float)rect.Left, (float)rect.Top, (float)rect.Width, (float)rect.Height);
        }

        // El color (con su alfa) del fondo de la píldora n en reposo, como lo pinta StepPills.Paint sobre su color base.
        public Color TrackColorOf(int index)
        {
            var row = Row ?? throw new InvalidOperationException("PillRowView sin estado.");
            var paint = StepPills.Paint(row.Looks[index], 0);
            return WithAlpha(BaseColor(row.Types[index]), paint.TrackAlpha);
        }

        // El relleno de la en curso (0 a 1) del último cuadro.
        public double Fill => _motion.Frame.Fill;

        public double HaloAlpha => _motion.Frame.HaloAlpha;

        // El trabajo del último generateVisualContent (HudBudgetTests.LongSeriesStaysSmooth), en ms.
        public double LastDrawMs { get; private set; }

        public int DrawCount { get; private set; }

        // Un estado del HUD (null sin rutina: no pasa). Sin panel se guarda y se aplica en el acto al volver.
        public void Apply(HudPillRow? row, bool instant)
        {
            if (_disposed || row == null)
            {
                return;
            }

            if (panel == null)
            {
                _pending = row;
                _pendingInstant = true;
                return;
            }

            _pending = null;
            _motion.OnState(row, instant);
            UpdateFillWidth();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _motion.Stop();
            PillGlowTexture.Destroy(_glow);
            _glow = null;
        }

        private void ApplyPending()
        {
            if (_pending == null || _disposed)
            {
                return;
            }

            var row = _pending;
            _pending = null;
            _motion.OnState(row, _pendingInstant);
            UpdateFillWidth();
        }

        // El repintado por ¼ px (§3.4): el ancho de la en curso en píxeles.
        private void UpdateFillWidth()
        {
            if (panel == null)
            {
                return;
            }

            var layout = ComputeLayout();
            var px = layout.PillWidth * worldTransform.lossyScale.x * scaledPixelsPerPoint;
            _motion.FillWidthPx = px > 0 ? px : 0;
        }

        private PillRowLayout ComputeLayout()
        {
            var row = Row;
            var count = row?.Types.Count ?? 0;
            var useful = contentRect.width - PillRowGeometry.PadLeft - PillRowGeometry.PadRight;
            return PillRowGeometry.Layout(useful > 0 ? useful : 0, count);
        }

        private void Draw(MeshGenerationContext context)
        {
            var row = _motion.Row;
            if (row == null || panel == null)
            {
                return;
            }

            _work.Restart();
            var layout = ComputeLayout();
            _layout = layout;
            if (layout.Count == 0)
            {
                return;
            }

            EnsureEdges(layout);
            var frame = _motion.Frame;
            var painter = context.painter2D;
            _context = context;
            DrawHalos(context, row, layout, frame);
            if (layout.Strip)
            {
                DrawStrip(painter, row, layout, frame);
            }
            else
            {
                for (var i = 0; i < layout.Count; i++)
                {
                    DrawPill(painter, row, layout, frame, i);
                }
            }

            if (layout.Narrow)
            {
                DrawThumbs(painter, row, layout, frame);
            }

            _context = null;
            _work.Stop();
            LastDrawMs = _work.Elapsed.TotalMilliseconds;
            DrawCount++;
        }

        // ── Las partes ──

        private void DrawHalos(MeshGenerationContext context, HudPillRow row, in PillRowLayout layout, in PillRowFrame frame)
        {
            if (frame.Current >= 0 && frame.Current < layout.Count && frame.HaloAlpha > 0.0005)
            {
                DrawGlow(context, ShapeOf(layout, frame.Current), Radius(layout, frame.Current), BaseColor(row.Types[frame.Current]),
                    StepBarTimeline.HaloAtEdge(frame.HaloAlpha));
            }

            if (frame.Changing && frame.Ending >= 0 && frame.Ending < layout.Count && frame.EndingHaloAlpha > 0.0005)
            {
                DrawGlow(context, ShapeOf(layout, frame.Ending), Radius(layout, frame.Ending), BaseColor(row.Types[frame.Ending]),
                    StepBarTimeline.HaloAtEdge(frame.EndingHaloAlpha));
            }
        }

        private void DrawPill(Painter2D painter, HudPillRow row, in PillRowLayout layout, in PillRowFrame frame, int index)
        {
            var rect = SnappedPill(layout, index);
            var radius = Math.Min(PillRowGeometry.Radius, Math.Min(rect.Width, rect.Height) / 2);
            var baseColor = BaseColor(row.Types[index]);
            var narrow = layout.Narrow;
            if (frame.Changing && index == frame.Ending)
            {
                DrawEnding(painter, rect, radius, baseColor, frame, narrow);
                return;
            }

            if (index == frame.Current)
            {
                if (narrow)
                {
                    // La en curso angosta es el pulgar (DrawThumbs), que tapa su lugar: la pista del pulgar va sobre el vidrio
                    // (QA-V4b-03). Su lugar con la pista se ve solo mientras el pulgar llega (y se apaga con él).
                    FillCapsule(painter, rect, radius, WithAlpha(baseColor, TrackAlphaOfCurrent(frame) * UnderThumbAlpha(frame)));
                    return;
                }

                DrawCurrent(painter, rect, radius, baseColor, frame);
                return;
            }

            var paint = StepPills.Paint(row.Looks[index], 0);
            FillCapsule(painter, rect, radius, WithAlpha(baseColor, paint.TrackAlpha));
            if (paint.BorderWidthBase > 0)
            {
                DrawBorder(painter, rect, radius, WithAlpha(baseColor, paint.BorderAlpha), 1, narrow);
            }
        }

        // La en curso: la pista (0,30, o la que sube de 0,18 en el cambio de paso), el relleno al 100 % y su brillo.
        private void DrawCurrent(Painter2D painter, in DpRect rect, double radius, Color baseColor, in PillRowFrame frame)
        {
            FillCapsule(painter, rect, radius, WithAlpha(baseColor, TrackAlphaOfCurrent(frame)));
            var fill = frame.Fill;
            if (fill <= 0)
            {
                return;
            }

            DrawFill(painter, rect, radius, fill * rect.Width, WithAlpha(baseColor, 1), MotionSpecs.PillHighlightAlpha);
        }

        // La que termina, en su cambio de paso (04-design-notes §4.5): completa el relleno que le faltaba (0-120); la cumplida
        // "se solidifica" (1 → 0,85 con el destello) y la hueca baja su interior mientras el borde se dibuja.
        private void DrawEnding(Painter2D painter, in DpRect rect, double radius, Color baseColor, in PillRowFrame frame, bool narrow)
        {
            var change = frame.Change;
            var fill = frame.EndingFillFrom + (1 - frame.EndingFillFrom) * change.CompletingFill;
            var highlight = MotionSpecs.PillHighlightAlpha * change.Highlight;
            if (frame.EndingMet)
            {
                if (fill < 1)
                {
                    FillCapsule(painter, rect, radius, WithAlpha(baseColor, StepPills.CurrentTrackAlpha));
                    DrawFill(painter, rect, radius, fill * rect.Width, WithAlpha(baseColor, change.SolidOpacity), highlight);
                }
                else
                {
                    DrawFill(painter, rect, radius, rect.Width, WithAlpha(baseColor, change.SolidOpacity), highlight);
                }

                if (change.Flash > 0.0005)
                {
                    FillCapsule(painter, rect, radius, WithAlpha(Color.white, change.Flash));
                }

                return;
            }

            if (fill < 1)
            {
                FillCapsule(painter, rect, radius, WithAlpha(baseColor, StepPills.CurrentTrackAlpha));
                DrawFill(painter, rect, radius, fill * rect.Width, WithAlpha(baseColor, change.HollowInterior), highlight);
            }
            else
            {
                DrawFill(painter, rect, radius, rect.Width, WithAlpha(baseColor, change.HollowInterior), highlight);
            }

            if (change.HollowBorderOpacity > 0.0005 && change.HollowStroke > 0)
            {
                DrawBorder(painter, rect, radius, WithAlpha(baseColor, StepPills.DoneAlpha * change.HollowBorderOpacity), change.HollowStroke, narrow);
            }
        }

        // El relleno recortado por la cápsula (el borde derecho recto) y su brillo de 1·s blanco arriba.
        private void DrawFill(Painter2D painter, in DpRect rect, double radius, double fillWidth, Color color, double highlightAlpha)
        {
            if (fillWidth <= 0)
            {
                return;
            }

            if (color.a >= 0.999f)
            {
                FillUnderlay(rect, radius, Math.Min(fillWidth, rect.Width), color);
            }

            if (fillWidth >= rect.Width - 1e-6)
            {
                FillCapsule(painter, rect, radius, color);
            }
            else
            {
                PillShapes.FillPolygon(rect, radius, fillWidth, _outline, _scratch);
                FillPolygon(painter, _outline, color);
            }

            if (highlightAlpha > 0.0005 && PillShapes.HighlightSpan(rect, radius, fillWidth, MotionSpecs.PillHighlightDp, out var from, out var to))
            {
                FillRect(painter, from, rect.Top, to - from, MotionSpecs.PillHighlightDp, WithAlpha(Color.white, highlightAlpha));
            }
        }

        // QA-V4b-04 (H-D2-03): debajo del relleno OPACO, un rectángulo sin suavizado (Allocate, dos triángulos) que tapa la parte
        // recta del relleno sin tocar sus bordes (un píxel adentro arriba, abajo y en el corte; los arcos quedan afuera): la
        // teselación de Painter2D del polígono recortado deja a veces un píxel sin cubrir (un píxel de la pista dentro del relleno,
        // CAP-27). Opaco sobre opaco del mismo color: no cambia nada donde no hay grieta. Con alfa < 1 (las transiciones del cambio
        // de paso) no se pone: se vería el doble.
        private void FillUnderlay(in DpRect rect, double radius, double width, Color color)
        {
            var context = _context;
            if (context == null)
            {
                return;
            }

            var s = worldTransform.lossyScale.x;
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            if (!(s > 0))
            {
                return;
            }

            var pixel = 1.0 / (s * ppp);
            var full = width >= rect.Width - 1e-6;
            var x0 = rect.Left + radius;
            var x1 = full ? rect.Right - radius : rect.Left + width - pixel;
            var y0 = rect.Top + pixel;
            var y1 = rect.Bottom - pixel;
            if (!(x1 > x0) || !(y1 > y0))
            {
                return;
            }

            Color32 tint = color;
            _quad[0] = new Vertex { position = new Vector3((float)x0, (float)y0, Vertex.nearZ), tint = tint };
            _quad[1] = new Vertex { position = new Vector3((float)x1, (float)y0, Vertex.nearZ), tint = tint };
            _quad[2] = new Vertex { position = new Vector3((float)x0, (float)y1, Vertex.nearZ), tint = tint };
            _quad[3] = new Vertex { position = new Vector3((float)x1, (float)y1, Vertex.nearZ), tint = tint };
            var mesh = context.Allocate(4, QuadIndices.Length);
            mesh.SetAllVertices(_quad);
            mesh.SetAllIndices(QuadIndices);
        }

        // El borde de la hueca: 1,5 por dentro, entero o hasta la fracción del contorno (en el sentido del reloj desde el centro
        // de su extremo izquierdo); en las angostas, dos rieles de izquierda a derecha (DN4b-18).
        private void DrawBorder(Painter2D painter, in DpRect rect, double radius, Color color, double fraction, bool narrow)
        {
            var stroke = StepPills.MissedBorderWidth;
            if (narrow)
            {
                PillShapes.Rails(rect, stroke, fraction, out var top, out var bottom);
                FillRect(painter, top.Left, top.Top, top.Width, top.Height, color);
                FillRect(painter, bottom.Left, bottom.Top, bottom.Width, bottom.Height, color);
                return;
            }

            var half = stroke / 2;
            var inner = new DpRect(rect.Left + half, rect.Top + half, Math.Max(0, rect.Width - stroke), Math.Max(0, rect.Height - stroke));
            var innerRadius = Math.Max(0, radius - half);
            painter.strokeColor = color;
            painter.lineWidth = (float)stroke;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Butt;
            if (fraction >= 1)
            {
                PillShapes.CapsuleOutline(inner, innerRadius, _outline);
                painter.BeginPath();
                MoveThrough(painter, _outline);
                painter.ClosePath();
                painter.Stroke();
                return;
            }

            PillShapes.PerimeterPolyline(inner, innerRadius, fraction, _outline, _scratch);
            if (_outline.Count < 2)
            {
                return;
            }

            painter.BeginPath();
            MoveThrough(painter, _outline);
            painter.Stroke();
        }

        // La tira (DV-40): cada paso en su color y con la opacidad de su estado, por columnas de píxel.
        private void DrawStrip(Painter2D painter, HudPillRow row, in PillRowLayout layout, in PillRowFrame frame)
        {
            var n = layout.Count;
            EnsureStrip(n);
            var s = worldTransform.lossyScale.x;
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            var pxPerBase = s * ppp;
            var originPx = worldBound.x * ppp;
            for (var i = 0; i < n; i++)
            {
                var x0 = layout.LeftOf(i);
                _stripLeft[i] = originPx + x0 * pxPerBase;
                _stripRight[i] = originPx + (x0 + layout.PillWidth) * pxPerBase;
                var color = BaseColor(row.Types[i]);
                _stripR[i] = color.r;
                _stripG[i] = color.g;
                _stripB[i] = color.b;
                _stripA[i] = AlphaInStrip(row, frame, i);
            }

            var runs = PillShapes.StripRuns(n, _stripLeft, _stripRight, _stripR, _stripG, _stripB, _stripA, _runs);
            // QA-V4b-03 (H-D2-01; 04-design-notes §3.2): debajo del pulgar de la en curso la tira no se pinta (en columnas de píxel
            // enteras, hacia adentro del pulgar), así su pista al 30 % queda sobre el vidrio, como en las píldoras anchas. Mientras
            // el pulgar llega, lo de abajo se apaga con él (1 − su opacidad), sin saltos.
            var clipFrom = double.PositiveInfinity;
            var clipTo = double.PositiveInfinity;
            var under = 1.0;
            if (layout.Narrow && frame.Current >= 0 && frame.Current < n)
            {
                var thumb = layout.ThumbOf(frame.Current);
                clipFrom = Math.Ceiling(originPx + thumb.Left * pxPerBase);
                clipTo = Math.Floor(originPx + thumb.Right * pxPerBase);
                under = UnderThumbAlpha(frame);
            }

            for (var k = 0; k < runs; k++)
            {
                var run = _runs[k];
                var color = new Color((float)run.R, (float)run.G, (float)run.B, (float)Math.Min(1, run.A));
                if (!(clipTo > clipFrom) || run.Right <= clipFrom || run.Left >= clipTo)
                {
                    StripRect(painter, run.Left, run.Right, color);
                    continue;
                }

                StripRect(painter, run.Left, clipFrom, color);
                if (under > 0.0005)
                {
                    StripRect(painter, Math.Max(run.Left, clipFrom), Math.Min(run.Right, clipTo), WithAlpha(color, under));
                }

                StripRect(painter, clipTo, run.Right, color);
            }
        }

        // Un tramo de la tira entre dos bordes en píxeles del panel (vacío si no tiene ancho).
        private void StripRect(Painter2D painter, double leftPx, double rightPx, Color color)
        {
            if (!(rightPx > leftPx))
            {
                return;
            }

            var s = worldTransform.lossyScale.x;
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            var left = (leftPx / ppp - worldBound.x) / s;
            var right = (rightPx / ppp - worldBound.x) / s;
            FillRect(painter, left, PillRowGeometry.PadTop, right - left, PillRowGeometry.Height, color);
        }

        // Lo que se ve de la franja debajo del pulgar de la en curso: nada con el pulgar entero; mientras llega (ThumbSwapping),
        // 1 − su opacidad (QA-V4b-03).
        private static double UnderThumbAlpha(in PillRowFrame frame)
        {
            if (!frame.ThumbSwapping)
            {
                return 0;
            }

            var incoming = frame.Thumb.IncomingOpacity;
            return incoming <= 0 ? 1 : incoming >= 1 ? 0 : 1 - incoming;
        }

        // El alfa de un tramo de la tira: el de su aspecto; la en curso, el de su pista (el pulgar va encima).
        private static double AlphaInStrip(HudPillRow row, in PillRowFrame frame, int index)
        {
            if (index == frame.Current)
            {
                return StepPills.CurrentTrackAlpha;
            }

            var look = row.Looks[index];
            if (frame.Changing && index == frame.Ending)
            {
                return look == StepPillLook.Hollow ? StepPills.PendingAlpha : StepPills.DoneAlpha * frame.Change.SolidOpacity;
            }

            return StepPills.Paint(look, 0).TrackAlpha;
        }

        // El pulgar de la en curso (y el de la que termina mientras pasa, DN4b-18): ≥ 4 × 10 base, centrado en su lugar, con su
        // pista, su relleno y su brillo adentro. Nunca hay dos pulgares enteros.
        private void DrawThumbs(Painter2D painter, HudPillRow row, in PillRowLayout layout, in PillRowFrame frame)
        {
            var thumb = frame.Thumb;
            if (frame.ThumbSwapping && frame.Ending >= 0 && frame.Ending < layout.Count && thumb.OutgoingOpacity > 0.001)
            {
                DrawThumb(painter, row, layout, frame.Ending, thumb.OutgoingHeight, thumb.OutgoingOpacity, frame.EndingMet ? 1 : 0, false);
            }

            if (frame.Current >= 0 && frame.Current < layout.Count)
            {
                var height = frame.ThumbSwapping ? thumb.IncomingHeight : PillRowGeometry.ThumbHeight;
                var opacity = frame.ThumbSwapping ? thumb.IncomingOpacity : 1;
                DrawThumb(painter, row, layout, frame.Current, height, opacity, frame.Fill, true);
            }
        }

        private void DrawThumb(Painter2D painter, HudPillRow row, in PillRowLayout layout, int index, double height, double opacity, double fill, bool current)
        {
            var full = layout.ThumbOf(index);
            var center = full.Top + full.Height / 2;
            var rect = new DpRect(full.Left, center - height / 2, full.Width, height);
            var radius = Math.Min(PillRowGeometry.Radius, Math.Min(rect.Width, rect.Height) / 2);
            var baseColor = BaseColor(row.Types[index]);
            FillCapsule(painter, rect, radius, WithAlpha(baseColor, StepPills.CurrentTrackAlpha * opacity));
            if (fill > 0)
            {
                DrawFill(painter, rect, radius, fill * rect.Width, WithAlpha(baseColor, opacity), current ? MotionSpecs.PillHighlightAlpha * opacity : 0);
            }
        }

        // El halo en 9 partes estiradas por lado (DN4b-17): las esquinas se parten en el borde de la píldora (el perfil empieza
        // justo ahí) y en su radio; el centro (la píldora) es transparente. 6 × 6 vértices.
        private void DrawGlow(MeshGenerationContext context, in DpRect shape, double radius, Color baseColor, double edgeAlpha)
        {
            if (_glow == null)
            {
                _glow = PillGlowTexture.Create();
            }

            var e = PillGlowEdges(shape, radius);
            // Las UV van en [0; 1] de la textura: en Unity 6.3 el renderer las lleva al atlas solo (uvRegion quedó obsoleto).
            var mesh = context.Allocate(36, GlowIndices.Length, _glow);
            var tint = baseColor;
            tint.a = (float)Math.Max(0, Math.Min(1, edgeAlpha));
            Color32 tint32 = tint;
            var edge = PillGlowTexture.EdgeU;
            var slice = PillGlowTexture.SliceU;
            for (var row = 0; row < 6; row++)
            {
                for (var column = 0; column < 6; column++)
                {
                    var u = GlowU(column, edge, slice);
                    var v = GlowU(row, edge, slice);
                    _glowVertices[row * 6 + column] = new Vertex
                    {
                        position = new Vector3((float)e[column], (float)e[6 + row], Vertex.nearZ),
                        tint = tint32,
                        uv = new Vector2((float)u, (float)(1 - v)),
                    };
                }
            }

            mesh.SetAllVertices(_glowVertices);
            mesh.SetAllIndices(GlowIndices);
        }

        private readonly double[] _glowEdges = new double[12];

        private double[] PillGlowEdges(in DpRect shape, double radius)
        {
            var r = Math.Min(radius, Math.Min(shape.Width, shape.Height) / 2);
            var side = MotionSpecs.PillGlowExtentSideDp;
            var top = MotionSpecs.PillGlowExtentTopDp;
            var bottom = MotionSpecs.PillGlowExtentBottomDp;
            _glowEdges[0] = shape.Left - side;
            _glowEdges[1] = shape.Left;
            _glowEdges[2] = shape.Left + r;
            _glowEdges[3] = shape.Right - r;
            _glowEdges[4] = shape.Right;
            _glowEdges[5] = shape.Right + side;
            _glowEdges[6] = shape.Top - top;
            _glowEdges[7] = shape.Top;
            _glowEdges[8] = shape.Top + r;
            _glowEdges[9] = shape.Bottom - r;
            _glowEdges[10] = shape.Bottom;
            _glowEdges[11] = shape.Bottom + bottom;
            return _glowEdges;
        }

        // La coordenada de la textura de cada línea del 9-slice: 0 afuera, el borde de la píldora, el radio y el centro.
        private static float GlowU(int index, float edge, float slice)
        {
            switch (index)
            {
                case 0:
                    return 0;
                case 1:
                    return edge;
                case 2:
                    return slice;
                case 3:
                    return 1 - slice;
                case 4:
                    return 1 - edge;
                default:
                    return 1;
            }
        }

        private static ushort[] BuildGlowIndices()
        {
            var indices = new ushort[25 * 6];
            var k = 0;
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column < 5; column++)
                {
                    var a = (ushort)(row * 6 + column);
                    var b = (ushort)(a + 1);
                    var c = (ushort)(a + 6);
                    var d = (ushort)(c + 1);
                    indices[k++] = a;
                    indices[k++] = b;
                    indices[k++] = d;
                    indices[k++] = a;
                    indices[k++] = d;
                    indices[k++] = c;
                }
            }

            return indices;
        }

        // ── Geometría y pintura ──

        private double TrackAlphaOfCurrent(in PillRowFrame frame)
        {
            return frame.Changing ? frame.Change.NewTrack : StepPills.CurrentTrackAlpha;
        }

        // La forma de la píldora (o del pulgar en las angostas) para el halo.
        private DpRect ShapeOf(in PillRowLayout layout, int index)
        {
            return layout.Narrow ? layout.ThumbOf(index) : SnappedPill(layout, index);
        }

        private double Radius(in PillRowLayout layout, int index)
        {
            return layout.Narrow ? layout.ThumbRadiusOf(index) : PillRowGeometry.Radius;
        }

        // La píldora n con sus bordes en píxeles enteros, en coordenadas locales (dp base).
        private DpRect SnappedPill(in PillRowLayout layout, int index)
        {
            var s = worldTransform.lossyScale.x;
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            if (!(s > 0) || index >= _left.Length)
            {
                return layout.PillOf(index);
            }

            var left = (_left[index] / ppp - worldBound.x) / s;
            var right = (_right[index] / ppp - worldBound.x) / s;
            return new DpRect(left, PillRowGeometry.PadTop, Math.Max(0, right - left), PillRowGeometry.Height);
        }

        private void EnsureEdges(in PillRowLayout layout)
        {
            if (_left.Length < layout.Count)
            {
                _left = new int[layout.Count];
                _right = new int[layout.Count];
            }

            var s = worldTransform.lossyScale.x;
            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            PillRowGeometry.PixelEdges(layout, worldBound.x * ppp, s * ppp, _left, _right);
        }

        private void EnsureStrip(int count)
        {
            if (_stripLeft.Length < count)
            {
                _stripLeft = new double[count];
                _stripRight = new double[count];
                _stripR = new double[count];
                _stripG = new double[count];
                _stripB = new double[count];
                _stripA = new double[count];
            }

            var columns = (int)Math.Ceiling(contentRect.width * worldTransform.lossyScale.x * (scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f)) + 4;
            if (_runs.Length < columns)
            {
                _runs = new StripRun[columns];
            }
        }

        private static Color BaseColor(StepType type)
        {
            return TokenColors.ToColor(StepTypeInfo.Color(type));
        }

        private static Color WithAlpha(Color color, double alpha)
        {
            color.a *= (float)(alpha < 0 ? 0 : alpha > 1 ? 1 : alpha);
            return color;
        }

        private void FillCapsule(Painter2D painter, in DpRect rect, double radius, Color color)
        {
            if (color.a <= 0.0005f || rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            PillShapes.CapsuleOutline(rect, radius, _outline);
            FillPolygon(painter, _outline, color);
        }

        private static void FillPolygon(Painter2D painter, PolygonBuffer polygon, Color color)
        {
            if (polygon.Count < 3 || color.a <= 0.0005f)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            MoveThrough(painter, polygon);
            painter.ClosePath();
            painter.Fill();
        }

        private static void FillRect(Painter2D painter, double x, double y, double width, double height, Color color)
        {
            if (width <= 0 || height <= 0 || color.a <= 0.0005f)
            {
                return;
            }

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2((float)x, (float)y));
            painter.LineTo(new Vector2((float)(x + width), (float)y));
            painter.LineTo(new Vector2((float)(x + width), (float)(y + height)));
            painter.LineTo(new Vector2((float)x, (float)(y + height)));
            painter.ClosePath();
            painter.Fill();
        }

        private static void MoveThrough(Painter2D painter, PolygonBuffer polygon)
        {
            painter.MoveTo(new Vector2((float)polygon.X[0], (float)polygon.Y[0]));
            for (var i = 1; i < polygon.Count; i++)
            {
                painter.LineTo(new Vector2((float)polygon.X[i], (float)polygon.Y[i]));
            }
        }
    }
}
