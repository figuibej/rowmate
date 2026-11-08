using System;
using System.Collections.Generic;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // La lista de remos encontrados (AC-BLE-05-05, -07; device_screen.dart:419-471): una ScanResultCard por dirección, en el orden del
    // servicio (primer descubrimiento, reemplazo en el lugar) y con clave estable (A10-04): una tarjeta que ya estaba no se rearma al
    // llegar un resultado nuevo. Sin asignar en el camino quieto (mismo largo y mismas claves).
    //   #device-results .rm-scan-results
    public sealed class ScanResultList : VisualElement
    {
        private readonly UiContext _ui;
        private readonly Action<string> _onTap;
        private readonly Dictionary<string, ScanResultCard> _cards = new Dictionary<string, ScanResultCard>(StringComparer.Ordinal);
        private readonly List<string> _stale = new List<string>();

        public ScanResultList(UiContext ui, Action<string> onTap)
        {
            _ui = ui;
            _onTap = onTap;
            name = "device-results";
            AddToClassList("rm-scan-results");
            pickingMode = PickingMode.Ignore;
        }

        public int Count => childCount;

        public ScanResultCard? CardAt(int index)
        {
            return index >= 0 && index < childCount ? ElementAt(index) as ScanResultCard : null;
        }

        public ScanResultCard? CardFor(string key)
        {
            return _cards.TryGetValue(key, out var card) ? card : null;
        }

        public void Render(IReadOnlyList<ScanItemSpec> items)
        {
            var fresh = 0;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!_cards.TryGetValue(item.Key, out var card))
                {
                    card = new ScanResultCard(_ui, item.Key, _onTap);
                    _cards[item.Key] = card;
                    // WP-D (00-arte §1.3, A10-04): un remo nuevo crece con la regla del empuje (alto 0-160, opacidad e y −8 → 0 en 60-260),
                    // escalonado cada 35 ms con los que llegaron en el mismo cuadro (8 como máximo). Al reaparecer, se actualiza en su lugar.
                    if (_ui.Motion.Armed)
                    {
                        new BleMotion.Grow(_ui.Motion, card).Play(BleMotionSpecs.CardMinHeightDp, BleMotionSpecs.CardGapDp,
                            BleMotionSpecs.CardDropDp, BleMotionSpecs.CardDelayMs(fresh++));
                    }
                }

                if (card.parent != this)
                {
                    Insert(Math.Min(i, childCount), card);
                }
                else if (IndexOf(card) != i)
                {
                    card.RemoveFromHierarchy();
                    Insert(Math.Min(i, childCount), card);
                }

                card.Render(item);
            }

            if (childCount == items.Count && _cards.Count == items.Count)
            {
                return;
            }

            _stale.Clear();
            foreach (var entry in _cards)
            {
                var keep = false;
                for (var i = 0; i < items.Count; i++)
                {
                    if (string.Equals(items[i].Key, entry.Key, StringComparison.Ordinal))
                    {
                        keep = true;
                        break;
                    }
                }

                if (!keep)
                {
                    _stale.Add(entry.Key);
                }
            }

            for (var i = 0; i < _stale.Count; i++)
            {
                _cards[_stale[i]].RemoveFromHierarchy();
                _cards.Remove(_stale[i]);
            }
        }
    }
}
