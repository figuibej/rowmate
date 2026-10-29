using System.Collections.Generic;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Cruce de colores por código (03-rutinas/04-design-notes §5.6, §5.7; QA-V3-02 y 04-design-notes §16.6, punto 3): el mosaico
    // elegido (fondo, borde y etiqueta, y el anterior al revés), el segmento del modo, la vista previa y la fila del editor al
    // cambiar de tipo (borde, insignia, ícono y título) van del color de antes al nuevo en dur.fast 160 con ease.move. Igual con
    // reducir movimiento: es un fundido (§5.10). Sin armar (el primer cuadro), en el acto.
    // - Lo que pone el USS por clase (el borde y los textos de la fila, el fondo del segmento) se anima en línea y al terminar
    //   se suelta (StyleKeyword.Null): la clase nueva ya da el color final, así que no salta (releaseAtEnd).
    // - Lo que pinta el código (fondo y borde del mosaico, la insignia) queda en línea con el color final.
    // Mezcla en sRGB, como TokenColors.Lerp; si un extremo es transparente toma el tono del otro (no pasa por el negro).
    // Un cruce nuevo en medio de otro arranca desde el color que se ve (CurrentOr). Sin asignaciones por cuadro.
    public sealed class ColorFade : TimedMotion
    {
        public enum Channel
        {
            Background,
            Border,
            BorderLeft,
            Text
        }

        private readonly List<Item> _items = new List<Item>(4);
        private double _t = 1;

        public ColorFade(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => Tokens.DurFast.Milliseconds;

        // El color que se ve ahora en ese canal si está cruzando; si no, fallback (el color en reposo que conoce quien llama).
        public Color CurrentOr(VisualElement element, Channel channel, Color fallback)
        {
            if (!IsRunning)
            {
                return fallback;
            }

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (ReferenceEquals(item.Element, element) && item.Channel == channel)
                {
                    return Mix(item.From, item.To, _t);
                }
            }

            return fallback;
        }

        // Empieza a armar un cruce: los canales que se agreguen reemplazan a los del cruce anterior. Si había uno en curso, sus
        // canales quedan en su color final (o sueltos) antes de borrarlos: ninguno queda a medias en línea. En el mismo cuadro,
        // Play pone los colores de partida nuevos, así que no se ve.
        public void Reset()
        {
            if (IsRunning)
            {
                ApplyEnd();
            }

            _items.Clear();
        }

        public void Add(VisualElement element, Channel channel, Color from, Color to, bool releaseAtEnd)
        {
            _items.Add(new Item(element, channel, from, to, releaseAtEnd));
        }

        // Arranca (o reinicia desde 0 con los colores nuevos). Sin armar, aplica el final en el acto.
        public void Play()
        {
            if (!Driver.Armed)
            {
                _t = 1;
                ApplyEnd();
                return;
            }

            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            _t = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, TotalMs));
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                Apply(item.Element, item.Channel, Mix(item.From, item.To, _t));
            }
        }

        protected override void OnFinished()
        {
            _t = 1;
            ApplyEnd();
        }

        private void ApplyEnd()
        {
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.ReleaseAtEnd)
                {
                    Release(item.Element, item.Channel);
                }
                else
                {
                    Apply(item.Element, item.Channel, item.To);
                }
            }
        }

        public static Color Mix(Color from, Color to, double t)
        {
            if (from.a <= 0)
            {
                from = new Color(to.r, to.g, to.b, 0);
            }

            if (to.a <= 0)
            {
                to = new Color(from.r, from.g, from.b, 0);
            }

            return Color.LerpUnclamped(from, to, (float)(t < 0 ? 0 : t > 1 ? 1 : t));
        }

        private static void Apply(VisualElement element, Channel channel, Color color)
        {
            var style = element.style;
            switch (channel)
            {
                case Channel.Background:
                    style.backgroundColor = color;
                    break;
                case Channel.Border:
                    style.borderLeftColor = color;
                    style.borderRightColor = color;
                    style.borderTopColor = color;
                    style.borderBottomColor = color;
                    break;
                case Channel.BorderLeft:
                    style.borderLeftColor = color;
                    break;
                default:
                    style.color = color;
                    break;
            }
        }

        private static void Release(VisualElement element, Channel channel)
        {
            var style = element.style;
            switch (channel)
            {
                case Channel.Background:
                    style.backgroundColor = StyleKeyword.Null;
                    break;
                case Channel.Border:
                    style.borderLeftColor = StyleKeyword.Null;
                    style.borderRightColor = StyleKeyword.Null;
                    style.borderTopColor = StyleKeyword.Null;
                    style.borderBottomColor = StyleKeyword.Null;
                    break;
                case Channel.BorderLeft:
                    style.borderLeftColor = StyleKeyword.Null;
                    break;
                default:
                    style.color = StyleKeyword.Null;
                    break;
            }
        }

        private readonly struct Item
        {
            public Item(VisualElement element, Channel channel, Color from, Color to, bool releaseAtEnd)
            {
                Element = element;
                Channel = channel;
                From = from;
                To = to;
                ReleaseAtEnd = releaseAtEnd;
            }

            public VisualElement Element { get; }

            public Channel Channel { get; }

            public Color From { get; }

            public Color To { get; }

            public bool ReleaseAtEnd { get; }
        }
    }
}
