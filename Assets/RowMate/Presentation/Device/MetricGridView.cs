using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Grilla de métricas de la vista conectada (AC-BLE-11-03 a -05; legacy GridView.count, device_screen.dart:252-276).
    // No usa el wrap de flexbox (no puede expresar la proporción): en GeometryChangedEvent, si cambió su ancho, pide
    // DeviceScreenRules.Grid(ancho de la grilla) y fija tamaño y posición absoluta de cada tarjeta (fila y columna) y su propio
    // alto (2 columnas 1,3 o 3 columnas 1,6; separación 12). Un cambio de alto solo no recalcula (no hay bucle).
    // PULSO (solo con pulso FTMS > 0, AC-BLE-11-04) aparece y se va con la coreografía de 04-design-notes §5.9 (DN-16): al
    // aparecer, el alto de la grilla anima en dur.slow ease.move y la tarjeta entra como al "encender"; al irse, opacidad a 0
    // en 120 y el alto se recoge en 200. Un cambio de columnas (rotar, redimensionar) no se anima (paridad).
    public sealed class MetricGridView : VisualElement
    {
        private readonly UiContext _ui;
        private readonly List<MetricTileView> _tiles = new List<MetricTileView>(5);
        private readonly HeightAnimation _height;
        private MetricTileView? _leaving;
        private float _width = -1;

        public MetricGridView(UiContext ui)
        {
            _ui = ui;
            name = "metric-grid";
            AddToClassList("rm-metric-grid");
            pickingMode = PickingMode.Ignore;
            _height = new HeightAnimation(ui.Motion, this);
            RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        public IReadOnlyList<MetricTileView> Tiles => _tiles;

        public DeviceGridSpec? Spec { get; private set; }

        public MetricTileView? Find(MetricKind kind)
        {
            for (var i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i].Kind == kind)
                {
                    return _tiles[i];
                }
            }

            return null;
        }

        public void Render(IReadOnlyList<MetricTileSpec> specs)
        {
            var countBefore = _tiles.Count;
            // Saca las que ya no están (PULSO).
            for (var i = _tiles.Count - 1; i >= 0; i--)
            {
                if (!Contains(specs, _tiles[i].Kind))
                {
                    RemoveTile(_tiles[i]);
                }
            }

            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                var tile = Find(spec.Kind);
                if (tile == null)
                {
                    tile = new MetricTileView(_ui, spec);
                    tile.style.position = Position.Absolute;
                    _tiles.Insert(Mathf.Min(i, _tiles.Count), tile);
                    Add(tile);
                    if (countBefore > 0)
                    {
                        tile.PlayEnter(0);
                    }
                }
                else
                {
                    tile.Update(spec);
                }
            }

            if (_tiles.Count != countBefore)
            {
                Relayout(countBefore > 0);
            }
        }

        // "Encender" la grilla (04-design-notes §5.5): tarjeta i desde 35·i ms.
        public void PlayEnter(double startMs)
        {
            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].PlayEnter(startMs + MotionSpecs.TileStaggerMs * i);
            }
        }

        public void FinishAnimations()
        {
            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].FinishEnter();
                _tiles[i].Flash.Rest();
            }
        }

        private static bool Contains(IReadOnlyList<MetricTileSpec> specs, MetricKind kind)
        {
            for (var i = 0; i < specs.Count; i++)
            {
                if (specs[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveTile(MetricTileView tile)
        {
            _tiles.Remove(tile);
            if (!_ui.Motion.Armed || _ui.Motion.Settings.ReduceMotion)
            {
                tile.RemoveFromHierarchy();
                return;
            }

            // Sale con opacidad en 120 ms; el alto se recoge después (HeightAnimation).
            _leaving?.RemoveFromHierarchy();
            _leaving = tile;
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            if (Mathf.Abs(evt.newRect.width - _width) < 0.01f)
            {
                return;
            }

            _width = evt.newRect.width;
            Relayout(false);
        }

        private void Relayout(bool animateHeight)
        {
            var width = layout.width;
            if (!(width > 0))
            {
                return;
            }

            var spec = DeviceScreenRules.Grid(width);
            Spec = spec;
            var cellWidth = (float)spec.CellWidth(width);
            var cellHeight = (float)spec.CellHeight(width);
            for (var i = 0; i < _tiles.Count; i++)
            {
                var row = i / spec.Columns;
                var column = i % spec.Columns;
                var tile = _tiles[i];
                tile.style.left = column * (cellWidth + (float)spec.Spacing);
                tile.style.top = row * (cellHeight + (float)spec.Spacing);
                tile.style.width = cellWidth;
                tile.style.height = cellHeight;
            }

            var height = (float)spec.Height(width, _tiles.Count);
            if (animateHeight && _ui.Motion.Armed && !_ui.Motion.Settings.ReduceMotion)
            {
                _height.Animate(resolvedStyle.height, height, _leaving);
                _leaving = null;
            }
            else
            {
                _height.Finish();
                _leaving?.RemoveFromHierarchy();
                _leaving = null;
                style.height = height;
            }
        }

        // Alto de la grilla al agregar o sacar PULSO (DN-16).
        private sealed class HeightAnimation : TimedMotion
        {
            private readonly MetricGridView _grid;
            private float _from;
            private float _to;
            private VisualElement? _leaving;

            public HeightAnimation(MotionDriver driver, MetricGridView grid)
                : base(driver)
            {
                _grid = grid;
            }

            protected override double TotalMs => _leaving != null ? MotionSpecs.PulseTileOutMs + MotionSpecs.PulseTileCollapseMs : Tokens.DurSlow.Milliseconds;

            public void Animate(float from, float to, VisualElement? leaving)
            {
                Finish();
                _from = from;
                _to = to;
                _leaving = leaving;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_leaving != null)
                {
                    var fade = Progress(elapsedMs, 0, MotionSpecs.PulseTileOutMs);
                    _leaving.style.opacity = (float)(1 - Easing.Evaluate(Tokens.EaseExit, fade));
                    var collapse = Progress(elapsedMs, MotionSpecs.PulseTileOutMs, MotionSpecs.PulseTileOutMs + MotionSpecs.PulseTileCollapseMs);
                    _grid.style.height = _from + (_to - _from) * (float)Easing.Evaluate(Tokens.EaseMove, collapse);
                    return;
                }

                var u = Progress(elapsedMs, 0, Tokens.DurSlow.Milliseconds);
                _grid.style.height = _from + (_to - _from) * (float)Easing.Evaluate(Tokens.EaseMove, u);
            }

            protected override void OnFinished()
            {
                _grid.style.height = _to;
                _leaving?.RemoveFromHierarchy();
                _leaving = null;
            }
        }
    }
}
