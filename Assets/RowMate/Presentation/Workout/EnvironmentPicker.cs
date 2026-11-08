using System;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Scene;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Una tarjeta del selector (08-entornos-selector/02-tech-design §4; AC-SCN-22-02, -03; environment_picker.dart:23-58): la
    // miniatura de 152 × 96 con radio 12, el velo de la no elegida, el anillo absoluto (2,5 #00B4D8 elegida; 1 blanco al 12 % la
    // otra: no se anima el ancho del borde de un elemento con flujo, D8-11), el check y el nombre en 13 (700 blanco elegida, 400 al
    // 70 % la otra: dos etiquetas superpuestas). Enfocable (Tab y flechas, con el anillo de foco del sistema).
    public sealed class EnvironmentCard : VisualElement
    {
        public const string UssClass = "rm-env-card";
        public const string SelectedClass = "rm-env-card--selected";

        private readonly Image _thumb;
        private readonly Label _name;
        private readonly Label _nameBold;
        private readonly RowMate.Presentation.Motion.EnvironmentCardMotion? _motion;

        public EnvironmentCard(EnvironmentId environment, Texture? thumbnail, RowMate.Presentation.Motion.MotionDriver? motion = null)
        {
            _motion = motion != null ? new RowMate.Presentation.Motion.EnvironmentCardMotion(motion) : null;
            Environment = environment;
            name = "env-card-" + EnvironmentKeys.ToKey(environment);
            AddToClassList(UssClass);
            focusable = true;
            tabIndex = 0;

            var frame = new VisualElement { name = name + "-frame" };
            frame.AddToClassList("rm-env-card__frame");
            frame.pickingMode = PickingMode.Ignore;
            _thumb = new Image { name = name + "-thumb", scaleMode = ScaleMode.ScaleAndCrop, image = thumbnail };
            _thumb.AddToClassList("rm-env-card__thumb");
            _thumb.pickingMode = PickingMode.Ignore;
            if (thumbnail == null)
            {
                // Sin la miniatura horneada: el degradé de las 12 h (cielo arriba del 42 %, agua abajo; §3.4).
                _thumb.AddToClassList("rm-env-card__thumb--fallback");
            }

            frame.Add(_thumb);
            var veil = new VisualElement();
            veil.AddToClassList("rm-env-card__veil");
            veil.pickingMode = PickingMode.Ignore;
            frame.Add(veil);
            var ring = new VisualElement();
            ring.AddToClassList("rm-env-card__ring");
            ring.pickingMode = PickingMode.Ignore;
            frame.Add(ring);
            // WP-D1: el anillo de color va aparte (siempre 2,5 #00B4D8, con opacidad y escala): se anima sin tocar el ancho del borde (D8-11).
            var accent = new VisualElement();
            accent.AddToClassList("rm-env-card__ring-accent");
            accent.pickingMode = PickingMode.Ignore;
            frame.Add(accent);
            var check = new VisualElement();
            check.AddToClassList("rm-env-card__check");
            check.pickingMode = PickingMode.Ignore;
            var glyph = new IconView(Icons.CheckCircle, true, 22);
            glyph.AddToClassList("rm-env-card__check-glyph");
            glyph.pickingMode = PickingMode.Ignore;
            check.Add(glyph);
            frame.Add(check);
            Add(frame);

            var names = new VisualElement();
            names.AddToClassList("rm-env-card__names");
            names.pickingMode = PickingMode.Ignore;
            _name = new Label { name = name + "-name" };
            _name.AddToClassList("rm-env-card__name");
            _name.AddToClassList("rm-type-body-s");
            _name.pickingMode = PickingMode.Ignore;
            _nameBold = new Label { name = name + "-name-bold" };
            _nameBold.AddToClassList("rm-env-card__name");
            _nameBold.AddToClassList("rm-env-card__name--bold");
            _nameBold.AddToClassList("rm-type-body-s");
            _nameBold.pickingMode = PickingMode.Ignore;
            names.Add(_name);
            names.Add(_nameBold);
            Add(names);
            Add(RmButton.FocusRing());
        }

        public EnvironmentId Environment { get; }

        public bool IsSelected { get; private set; }

        public Image Thumbnail => _thumb;

        public string Text => _name.text;

        public void SetText(string text)
        {
            _name.text = text;
            _nameBold.text = text;
        }

        // El estado de la elegida: EnvironmentCardMotion (WP-D1) anima el cambio desde lo que se ve; sin MotionDriver, en el acto.
        public void SetSelected(bool selected)
        {
            var changed = selected != IsSelected;
            IsSelected = selected;
            if (_motion != null)
            {
                _motion.Apply(this, selected, !changed);
            }
            else
            {
                EnableInClassList(SelectedClass, selected);
            }
        }
    }

    // El selector de escenario de Workout en reposo (02-tech-design §4; AC-SCN-22-01 a -03, -05): una fila horizontal de 4 tarjetas en
    // el orden del legacy (EnvironmentCatalog.All), separadas 10, con su ScrollView de costado (sin barras). El toque (miniatura o
    // nombre) y Enter / Espacio eligen (SceneEnvironmentChoice.Select); ← → mueven el foco y corren la fila hasta ver la tarjeta + 16;
    // la rueda sobre la fila la corre de costado sin mover la página (si la fila no entra). Habilitado sin remo. Las tarjetas se arman
    // una vez: 0 B por cuadro.
    public sealed class EnvironmentPicker : VisualElement
    {
        public const string UssClass = "rm-env-picker";
        public const float WheelStepDp = 60;
        public const float RevealMarginDp = 16;

        private readonly RowMate.App.Scene.SceneEnvironmentChoice _choice;
        private readonly EnvironmentCard[] _cards;

        public EnvironmentPicker(UiContext ui)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            _choice = ui.App.SceneEnvironment;
            name = "env-picker";
            AddToClassList(UssClass);
            Scroll = new ScrollView(ScrollViewMode.Horizontal) { name = "env-picker-scroll" };
            Scroll.AddToClassList("rm-env-picker__scroll");
            Scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            Scroll.contentContainer.AddToClassList("rm-env-picker__row");
            Add(Scroll);

            var all = EnvironmentCatalog.All;
            _cards = new EnvironmentCard[all.Length];
            for (var i = 0; i < all.Length; i++)
            {
                var card = new EnvironmentCard(all[i], ui.Thumbnail(all[i]), ui.Motion);
                var index = i;
                card.RegisterCallback<ClickEvent>(_ => Choose(index));
                card.RegisterCallback<NavigationSubmitEvent>(evt =>
                {
                    Choose(index);
                    evt.StopPropagation();
                });
                card.RegisterCallback<NavigationMoveEvent>(evt => OnMove(index, evt));
                card.RegisterCallback<FocusInEvent>(_ => Reveal(index));
                if (i == all.Length - 1)
                {
                    card.AddToClassList("rm-env-card--last");
                }

                _cards[i] = card;
                Scroll.Add(card);
            }

            Scroll.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            this.Bind(_choice.Selected, Render);
            this.Bind(ui.App.Texts, OnTexts);
        }

        public ScrollView Scroll { get; }

        public EnvironmentCard Card(int index) => _cards[index];

        public int Count => _cards.Length;

        private void Choose(int index)
        {
            _choice.Select(_cards[index].Environment);
        }

        private void Render(EnvironmentId selected)
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetSelected(_cards[i].Environment == selected);
            }
        }

        private void OnTexts(Localizer texts)
        {
            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetText(texts.Get(KeyFor(_cards[i].Environment)));
            }
        }

        public static string KeyFor(EnvironmentId id)
        {
            switch (id)
            {
                case EnvironmentId.River:
                    return L10nKeys.SceneRiver;
                case EnvironmentId.Coast:
                    return L10nKeys.SceneCoast;
                case EnvironmentId.Regatta:
                    return L10nKeys.SceneRegatta;
                default:
                    return L10nKeys.SceneLake;
            }
        }

        private void OnMove(int index, NavigationMoveEvent evt)
        {
            var next = evt.direction == NavigationMoveEvent.Direction.Left ? index - 1
                : evt.direction == NavigationMoveEvent.Direction.Right ? index + 1 : -1;
            if (next < 0 || next >= _cards.Length || (evt.direction != NavigationMoveEvent.Direction.Left && evt.direction != NavigationMoveEvent.Direction.Right))
            {
                return;
            }

            _cards[next].Focus();
            evt.StopPropagation();
        }

        // Corre la fila hasta ver la tarjeta entera + 16 (sin animar: el foco ya se ve).
        private void Reveal(int index)
        {
            var card = _cards[index];
            var viewport = Scroll.contentViewport.layout.width;
            if (!(viewport > 0))
            {
                return;
            }

            var left = card.layout.x;
            var right = left + card.layout.width;
            var offset = Scroll.scrollOffset.x;
            if (left - RevealMarginDp < offset)
            {
                offset = Math.Max(0, left - RevealMarginDp);
            }
            else if (right + RevealMarginDp > offset + viewport)
            {
                offset = right + RevealMarginDp - viewport;
            }

            Scroll.scrollOffset = new Vector2(offset, 0);
        }

        // La rueda (vertical u horizontal) corre la fila de costado y no llega a la página, si la fila no entra.
        private void OnWheel(WheelEvent evt)
        {
            var content = Scroll.contentContainer.layout.width;
            var viewport = Scroll.contentViewport.layout.width;
            if (!(content > viewport + 0.5f))
            {
                return;
            }

            var delta = Math.Abs(evt.delta.x) > Math.Abs(evt.delta.y) ? evt.delta.x : evt.delta.y;
            var offset = Mathf.Clamp(Scroll.scrollOffset.x + delta * WheelStepDp / 3f, 0, content - viewport);
            Scroll.scrollOffset = new Vector2(offset, 0);
            evt.StopPropagation();
        }
    }
}
