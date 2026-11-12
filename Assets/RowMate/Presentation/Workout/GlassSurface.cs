using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El vidrio desenfocado de las piezas del HUD y del diálogo de completado (F-WKT-17; 06b-agua-avanzada/02-tech-design §4.4; DT6b-09,
    // DT6b-10; AC-6B-23 a -27). Un ayudante SIN elementos (el HUD sigue en sus elementos de hoy, HudBudgetTests): a cada pieza de vidrio le
    // pone de background-image el compuesto _RmHudGlass de la escena (IImmersiveScene.GlassTexture), con background-size y
    // background-position = el rectángulo de scene-surface en coordenadas de la pieza (así el desenfoque queda alineado con la escena de
    // atrás). Se recalcula en GeometryChangedEvent (con las posiciones de layout, sin el translate de la cascada: durante la cascada el
    // desenfoque se corre con la pieza, imperceptible con un radio de ≈ 10 dp), nunca por cuadro: el panel muestrea la RT viva. El peso
    // (GlassWeight, con su fundido de 200 ms) va en el alfa del tinte de la imagen: con 0 se ve el respaldo plano de hoy (background-color
    // glass.fallback, que no cambia). Debajo de los hijos, el reflejo interior (#FFFFFF @6 % → 0 en los 8 dp de arriba), solo con vidrio.
    // Avisa a la escena si hay piezas de vidrio con tamaño (sin piezas a la vista, el desenfoque no corre).
    public sealed class GlassSurface : IDisposable
    {
        public const float ReflexHeightDp = 8f;
        public const float ReflexAlpha = 0.06f;

        private readonly IImmersiveScene _scene;
        private readonly VisualElement _surface;
        private readonly List<VisualElement> _pieces = new List<VisualElement>();
        private readonly Action<MeshGenerationContext> _reflex;
        private Texture? _texture;
        private double _weight;
        private bool _disposed;
        private bool _inUse;

        public GlassSurface(IImmersiveScene scene, VisualElement surface)
        {
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _reflex = DrawReflex;
            _scene.GlassChanged += OnGlassChanged;
            _surface.RegisterCallback<GeometryChangedEvent>(OnSurfaceGeometry);
        }

        public IReadOnlyList<VisualElement> Pieces => _pieces;

        public double Weight => _weight;

        public Texture? Texture => _texture;

        public void Attach(VisualElement piece)
        {
            if (piece == null || _pieces.Contains(piece))
            {
                return;
            }

            _pieces.Add(piece);
            piece.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            piece.RegisterCallback<GeometryChangedEvent>(OnPieceGeometry);
            piece.generateVisualContent += _reflex;
            Apply(piece);
            if (_texture is RenderTexture rt)
            {
                // Una pieza que llega con el vidrio ya corriendo (el diálogo de completado): la textura y el peso de ahora.
                piece.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
                piece.style.unityBackgroundImageTintColor = White((float)Math.Max(0, Math.Min(1, _weight)));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _scene.GlassChanged -= OnGlassChanged;
            _surface.UnregisterCallback<GeometryChangedEvent>(OnSurfaceGeometry);
            foreach (var piece in _pieces)
            {
                piece.UnregisterCallback<GeometryChangedEvent>(OnPieceGeometry);
                piece.generateVisualContent -= _reflex;
                piece.style.backgroundImage = StyleKeyword.Null;
                piece.style.unityBackgroundImageTintColor = StyleKeyword.Null;
            }

            _pieces.Clear();
            SetInUse(false);
        }

        private void OnGlassChanged()
        {
            if (_disposed)
            {
                return;
            }

            var texture = _scene.GlassTexture;
            var weight = _scene.GlassWeight;
            var textureChanged = !ReferenceEquals(texture, _texture);
            var weightChanged = Math.Abs(weight - _weight) > 1e-4;
            _texture = texture;
            _weight = weight;
            for (var i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (textureChanged)
                {
                    piece.style.backgroundImage = texture is RenderTexture rt ? new StyleBackground(Background.FromRenderTexture(rt)) : (StyleBackground)StyleKeyword.Null;
                }

                if (weightChanged || textureChanged)
                {
                    piece.style.unityBackgroundImageTintColor = White((float)Math.Max(0, Math.Min(1, _texture != null ? _weight : 0)));
                    piece.MarkDirtyRepaint();
                }
            }
        }

        private void OnSurfaceGeometry(GeometryChangedEvent evt)
        {
            for (var i = 0; i < _pieces.Count; i++)
            {
                Apply(_pieces[i]);
            }
        }

        private void OnPieceGeometry(GeometryChangedEvent evt)
        {
            if (evt.target is VisualElement piece)
            {
                Apply(piece);
            }
        }

        // DT6b-10: el rectángulo de scene-surface en coordenadas de la pieza (con las posiciones de layout, sin transformaciones).
        private void Apply(VisualElement piece)
        {
            var surface = LayoutOrigin(_surface);
            var origin = LayoutOrigin(piece);
            var size = _surface.layout.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x <= 0 || size.y <= 0)
            {
                UpdateInUse();
                return;
            }

            var border = new Vector2(piece.resolvedStyle.borderLeftWidth, piece.resolvedStyle.borderTopWidth);
            var offset = surface - origin - border;
            piece.style.backgroundSize = new BackgroundSize(new Length(size.x, LengthUnit.Pixel), new Length(size.y, LengthUnit.Pixel));
            piece.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(offset.x, LengthUnit.Pixel));
            piece.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(offset.y, LengthUnit.Pixel));
            UpdateInUse();
        }

        // La posición de layout en el panel (la suma de layout.position hasta la raíz): no incluye los translate de la cascada.
        public static Vector2 LayoutOrigin(VisualElement element)
        {
            var p = Vector2.zero;
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                var r = e.layout;
                if (!float.IsNaN(r.x) && !float.IsNaN(r.y))
                {
                    p += r.position;
                }
            }

            return p;
        }

        private void UpdateInUse()
        {
            var any = false;
            for (var i = 0; i < _pieces.Count && !any; i++)
            {
                var r = _pieces[i].layout;
                any = _pieces[i].panel != null && r.width > 0 && r.height > 0;
            }

            SetInUse(any);
        }

        private void SetInUse(bool inUse)
        {
            if (inUse == _inUse)
            {
                return;
            }

            _inUse = inUse;
            _scene.SetGlassInUse(inUse);
        }

        // Blanco con alfa (el tinte de la imagen y el reflejo: no es un color de la paleta).
        private static Color White(float alpha)
        {
            var c = Color.white;
            c.a = alpha;
            return c;
        }

        // El reflejo interior (P-19): una franja blanca @6 % que se apaga en los 8 dp de arriba, debajo de los hijos; solo con vidrio.
        private void DrawReflex(MeshGenerationContext context)
        {
            if (_texture == null || _weight <= 0)
            {
                return;
            }

            var element = context.visualElement;
            var w = element.layout.width;
            if (!(w > 0))
            {
                return;
            }

            var inset = Math.Min(element.resolvedStyle.borderTopLeftRadius, w / 2);
            var mesh = context.Allocate(4, 6);
            var top = White(ReflexAlpha * (float)_weight);
            var bottom = White(0f);
            var left = inset * 0.5f;
            var right = w - inset * 0.5f;
            mesh.SetNextVertex(new Vertex { position = new Vector3(left, 0, Vertex.nearZ), tint = top });
            mesh.SetNextVertex(new Vertex { position = new Vector3(right, 0, Vertex.nearZ), tint = top });
            mesh.SetNextVertex(new Vertex { position = new Vector3(right, ReflexHeightDp, Vertex.nearZ), tint = bottom });
            mesh.SetNextVertex(new Vertex { position = new Vector3(left, ReflexHeightDp, Vertex.nearZ), tint = bottom });
            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(0);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
        }
    }
}
