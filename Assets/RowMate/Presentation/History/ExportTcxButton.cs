using System;
using System.Collections.Generic;
using RowMate.App.Export;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // "Exportar TCX" (It. 12; 12-exportar-tcx/02-tech-design §5.1, DT12-17; AC-STR-09-17, -18; legacy, el botón de subida de
    // session_detail_screen.dart:86-120). Dos lugares (It. 15, DT15-04): la AppBar del Detalle y la tarjeta del Historial.
    //   #<nombre> .rm-icon-button .rm-export-button[--busy]   (la caja de IconButton, foco con Tab, Enter o Espacio)
    //   ├── .rm-button__state
    //   ├── el glifo de la fuente de íconos (download en Windows, ios_share en Android; It. 15, #66)   ← oculto mientras exporta
    //   ├── Spinner 18/2 (ExportMotionSpecs)                                                         ← solo mientras exporta
    //   └── foco
    // - Detalle: "session-export", glifo de 24 (el de la flecha de volver) y la caja de 48. Tarjeta: "history-card-<n>-export",
    //   glifo de 18 y área de toque de 40 (AC-D15-09), con márgenes negativos para no cambiar el alto del renglón.
    // - Visible solo con la sesión exportable, una plataforma que exporta (Delivery != None) y la sesión con id (DV-126).
    // - Ocupado = el id está en TcxExportService.Busy (también al reabrir el Detalle o volver a la lista durante una exportación):
    //   deshabilitado, indicador en lugar del glifo y la ayuda "Exportando…". Exportar va al servicio (uno por app): salir no cancela.
    // - La ayuda (exportTcx / exportTcxExporting) es el nombre accesible, salvo que se pase otro (la tarjeta: exportTcxFor).
    // - El botón no deja pasar el toque ni el submit a su contenedor (la tarjeta no abre el Detalle, DT15-05).
    public sealed class ExportTcxButton
    {
        public const string ElementName = "session-export";
        public const string UssClass = "rm-export-button";
        public const string BusyClass = "rm-export-button--busy";
        public const string CardClass = "rm-export-button--card";
        public const float CardGlyphDp = 18;
        public const float CardTouchDp = 40;

        private readonly UiContext _ui;
        private readonly TcxExportService _export;
        private WorkoutSession? _session;
        private bool _exportable;
        private bool _busy;

        public ExportTcxButton(UiContext ui, TcxExportService export)
            : this(ui, export, ElementName, IconButton.DefaultIconDp, null, null)
        {
        }

        // elementName: el nombre de contrato; glyphDp: el tamaño del glifo; touchDp: el lado del área de toque (null = la caja de 48 de
        // IconButton); spokenName: el nombre accesible si no es la ayuda.
        public ExportTcxButton(UiContext ui, TcxExportService export, string elementName, float glyphDp, float? touchDp, Func<string>? spokenName)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _export = export ?? throw new ArgumentNullException(nameof(export));
            Delivery = export.Delivery;
            Button = new IconButton(elementName, GlyphFor(Delivery), OnClick, glyphDp);
            Button.AddToClassList(UssClass);
            Button.Icon.name = elementName + "-glyph";
            if (touchDp.HasValue)
            {
                // La tarjeta: el área de toque de 40 que sobresale del renglón sin cambiar su alto y el glifo en text.secondary los
                // pone la USS (.rm-export-button--card, Skin.uss; WP-D de la It. 15). Los 18/40 de acá son los de esa regla.
                Button.AddToClassList(CardClass);
            }

            Button.RegisterCallback<KeyDownEvent>(OnKeyDown);
            // DT15-05: el toque y el submit quedan en el botón (la tarjeta no los ve).
            Button.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            Button.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
            Button.RegisterCallback<NavigationSubmitEvent>(evt => evt.StopPropagation());
            Spinner = new Spinner(ui.Motion, (float)ExportMotionSpecs.SpinnerDp, (float)ExportMotionSpecs.StrokeDp) { name = elementName + "-spinner" };
            Spinner.SetShown(false);
            // El indicador en el lugar del ícono (antes del anillo de foco).
            Button.Insert(Button.IndexOf(Button.Icon) + 1, Spinner);
            Button.SetTooltip(ui, () => ui.Texts.Get(_busy ? L10nKeys.ExportTcxExporting : L10nKeys.ExportTcx));
            Button.SpokenName = spokenName;
            Button.style.display = DisplayStyle.None;
            Button.Bind(export.Busy, OnBusy);
        }

        public IconButton Button { get; }

        public Spinner Spinner { get; }

        // El glifo de la fuente (download con SaveToFolder, ios_share con Share).
        public IconView GlyphView => Button.Icon;

        // Cómo entrega la plataforma.
        public ExportDelivery Delivery { get; }

        public bool IsShown => Button.style.display != DisplayStyle.None;

        public bool IsBusy => _busy;

        public string? AccessibleName => Button.AccessibleName;

        // El código del glifo de la entrega.
        public string Glyph => GlyphFor(Delivery);

        public static string GlyphFor(ExportDelivery delivery)
        {
            return delivery == ExportDelivery.Share ? Icons.IosShare : Icons.Download;
        }

        // El estado del Detalle o de la tarjeta: la sesión y si se puede exportar.
        public void SetSession(WorkoutSession session, bool exportable)
        {
            _session = session;
            _exportable = exportable;
            Refresh();
        }

        private void OnBusy(IReadOnlyCollection<long> busy)
        {
            Refresh();
        }

        private void Refresh()
        {
            var id = _session?.Id;
            var shown = _exportable && Delivery != ExportDelivery.None && id.HasValue;
            var display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (Button.style.display != display)
            {
                Button.style.display = display;
            }

            var busy = id.HasValue && _export.IsBusy(id.Value);
            if (busy == _busy && Button.enabledSelf == !busy)
            {
                return;
            }

            _busy = busy;
            Button.SetEnabled(!busy);
            Button.EnableInClassList(BusyClass, busy);
            Button.Icon.style.display = busy ? DisplayStyle.None : DisplayStyle.Flex;
            Spinner.SetShown(busy);
        }

        private void OnClick()
        {
            var session = _session;
            if (session == null || _busy || !IsShown)
            {
                return;
            }

            _export.ExportAsync(session).Forget(_ui.Logger, TcxExportServiceTag);
        }

        // Espacio activa el botón como Enter (Enter ya llega como NavigationSubmitEvent).
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != UnityEngine.KeyCode.Space || !Button.enabledInHierarchy)
            {
                return;
            }

            evt.StopPropagation();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = Button;
                Button.SendEvent(submit);
            }
        }

        private const string TcxExportServiceTag = RowMate.Core.Export.ExportLog.Tag;
    }
}
