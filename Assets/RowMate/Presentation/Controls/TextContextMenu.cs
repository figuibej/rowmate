using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Cómo se abre el menú de texto en el teléfono (R3-13, QA-G3-16): SystemMenu = el del sistema (la app no abre nada);
    // AppMenu = el mismo menú de la PC al mantener presionado 0,5 s, con los textos del catálogo en el idioma de la app (la
    // variante de P-06 que el PO ya escribió). Se decide en [Device-And].
    public enum TextMenuTouchMode
    {
        SystemMenu,
        AppMenu
    }

    // Menú del clic derecho de los campos de texto (AC-RUT-11-09, AC-APP-09-07; 03-rutinas/02-tech-design §4.10). En runtime
    // UI Toolkit no tiene menú contextual (S3-b: contextualMenuManager = null): este es propio, en #popup-layer.
    // - PointerDown con el botón derecho, en TrickleDown y frenado (S3-b: si no, Unity borra la selección).
    // - Ítems según el estado, como Flutter en escritorio: textCut y textCopy si hay selección; textPaste si el portapapeles
    //   tiene texto; textSelectAll si hay texto y no está todo seleccionado. Sin ninguno, no se abre nada (P-06).
    // - Copiar = IClipboard.SetText(selección); cortar = copiar y borrar la selección; pegar = insertar en la selección (sin
    //   \n en una línea); seleccionar todo = SelectAll. El cursor queda donde corresponde (SelectRange).
    // - Menú denso (.rm-menu--dense) en el puntero + (2; 2).
    public sealed class TextContextMenu
    {
        public const string CutName = "text-menu-cut";
        public const string CopyName = "text-menu-copy";
        public const string PasteName = "text-menu-paste";
        public const string SelectAllName = "text-menu-select-all";

        private readonly UiContext _ui;
        private readonly RmTextField _field;
        private IDisposable? _longPress;
        private Vector2 _pressPoint;

        public TextContextMenu(UiContext ui, RmTextField field)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            field.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            field.RegisterCallback<PointerUpEvent>(_ => CancelLongPress(), TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCancelEvent>(_ => CancelLongPress(), TrickleDown.TrickleDown);
            field.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        }

        // Los ítems que tendría el menú ahora (nombres de contrato, en orden).
        public IReadOnlyList<string> ItemsNow()
        {
            var items = new List<string>(4);
            var value = _field.Value;
            var selection = _field.Selection;
            var hasSelection = selection.End > selection.Start;
            if (hasSelection)
            {
                items.Add(CutName);
                items.Add(CopyName);
            }

            if (_ui.Clipboard.HasText)
            {
                items.Add(PasteName);
            }

            if (value.Length > 0 && !(selection.Start == 0 && selection.End == value.Length))
            {
                items.Add(SelectAllName);
            }

            return items;
        }

        // Abre el menú en point (coordenadas del panel). false si no había ninguna acción (P-06).
        public bool OpenAt(Vector2 point)
        {
            var items = ItemsNow();
            if (items.Count == 0)
            {
                return false;
            }

            // La selección de ahora: un ítem del menú no le saca el foco al campo (no son enfocables).
            var selection = _field.Selection;
            var texts = _ui.Texts;
            var menu = new PopupMenu(true);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                menu.AddItem(item, Label(item, texts), false, () => Run(item, selection), false);
            }

            _ui.Popups.ShowMenuAt(menu, point, _field.Input);
            return true;
        }

        private void Run(string item, (int Start, int End) selection)
        {
            var value = _field.Value;
            var start = Mathf.Clamp(selection.Start, 0, value.Length);
            var end = Mathf.Clamp(selection.End, start, value.Length);
            switch (item)
            {
                case CopyName:
                    _ui.Clipboard.SetText(value.Substring(start, end - start));
                    Refocus(start, end);
                    break;
                case CutName:
                    _ui.Clipboard.SetText(value.Substring(start, end - start));
                    Refocus(start, end);
                    _field.ReplaceRange(start, end, "");
                    break;
                case PasteName:
                    Refocus(start, end);
                    _field.ReplaceRange(start, end, _ui.Clipboard.Text);
                    break;
                case SelectAllName:
                    Refocus(0, value.Length);
                    break;
            }
        }

        private void Refocus(int start, int end)
        {
            if (!_field.IsFocused)
            {
                _field.FocusInput(false);
            }

            _field.Select(start, end);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_ui.IsDesktop)
            {
                if (evt.button != 1)
                {
                    return;
                }

                evt.StopImmediatePropagation();
                _field.Input.focusController?.IgnoreEvent(evt);
                if (!_field.IsFocused)
                {
                    _field.FocusInput(false);
                }

                OpenAt(evt.position);
                return;
            }

            if (_ui.TextMenuTouchMode != TextMenuTouchMode.AppMenu || evt.button != 0)
            {
                return;
            }

            CancelLongPress();
            _pressPoint = evt.position;
            _longPress = _ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(UiTimings.LongPressMs), () =>
            {
                _longPress = null;
                OpenAt(_pressPoint);
            });
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_longPress != null && (new Vector2(evt.position.x, evt.position.y) - _pressPoint).sqrMagnitude > 100)
            {
                CancelLongPress();
            }
        }

        private void CancelLongPress()
        {
            _longPress?.Dispose();
            _longPress = null;
        }

        private static string Label(string item, Localizer texts)
        {
            switch (item)
            {
                case CutName:
                    return texts.Get(L10nKeys.TextCut);
                case CopyName:
                    return texts.Get(L10nKeys.TextCopy);
                case PasteName:
                    return texts.Get(L10nKeys.TextPaste);
                default:
                    return texts.Get(L10nKeys.TextSelectAll);
            }
        }
    }
}
