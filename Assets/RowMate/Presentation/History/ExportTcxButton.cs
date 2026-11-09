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
    // "Exportar TCX" en la AppBar del Detalle (It. 12; 12-exportar-tcx/02-tech-design §5.1, DT12-17; AC-STR-09-17, -18; legacy, el
    // botón de subida de session_detail_screen.dart:86-120):
    //   #session-export .rm-icon-button .rm-export-button[--busy]   (la caja de 48 de IconButton, foco con Tab, Enter o Espacio)
    //   ├── .rm-button__state
    //   ├── el glifo (descarga en Windows, compartir en Android)   ← oculto mientras exporta
    //   ├── Spinner 18/2 (ExportMotionSpecs)                       ← solo mientras exporta
    //   └── foco
    // - Visible solo con el Detalle listo y exportable (SessionDetailState.CanExport), una plataforma que exporta (Delivery != None)
    //   y la sesión con id (DV-126): en carga, error de carga y sin telemetría no está.
    // - Ocupado = el id está en TcxExportService.Busy (también al reabrir el Detalle durante una exportación): deshabilitado,
    //   indicador en lugar del glifo y la ayuda "Exportando…". Exportar va al servicio (uno por app): salir del Detalle no cancela.
    // - La ayuda (exportTcx / exportTcxExporting) es también el nombre accesible (Tooltip, DT-05), con la regla del toque largo.
    public sealed class ExportTcxButton
    {
        public const string ElementName = "session-export";
        public const string UssClass = "rm-export-button";
        public const string BusyClass = "rm-export-button--busy";

        private readonly UiContext _ui;
        private readonly TcxExportService _export;
        private WorkoutSession? _session;
        private bool _exportable;
        private bool _busy;

        public ExportTcxButton(UiContext ui, TcxExportService export)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _export = export ?? throw new ArgumentNullException(nameof(export));
            Delivery = export.Delivery;
            // La caja de IconButton sin su glifo de fuente (download y share no están en el recorte, D12-03): el glifo dibujado.
            Button = new IconButton(ElementName, "", OnClick);
            Button.AddToClassList(UssClass);
            Button.Icon.style.display = DisplayStyle.None;
            Button.RegisterCallback<KeyDownEvent>(OnKeyDown);
            GlyphView = new ExportGlyphView(GlyphFor(Delivery), IconButton.DefaultIconDp) { name = ElementName + "-glyph" };
            GlyphView.AddToClassList("rm-icon-button__icon");
            Spinner = new Spinner(ui.Motion, (float)ExportMotionSpecs.SpinnerDp, (float)ExportMotionSpecs.StrokeDp) { name = ElementName + "-spinner" };
            Spinner.SetShown(false);
            // El glifo y el indicador en el lugar del ícono (antes del anillo de foco).
            var at = Button.IndexOf(Button.Icon) + 1;
            Button.Insert(at, GlyphView);
            Button.Insert(at + 1, Spinner);
            Button.SetTooltip(ui, () => ui.Texts.Get(_busy ? L10nKeys.ExportTcxExporting : L10nKeys.ExportTcx));
            Button.style.display = DisplayStyle.None;
            Button.Bind(export.Busy, OnBusy);
        }

        public IconButton Button { get; }

        public Spinner Spinner { get; }

        // El glifo dibujado (download con SaveToFolder, share con Share).
        public ExportGlyphView GlyphView { get; }

        // Cómo entrega la plataforma (el glifo: descarga con SaveToFolder, compartir con Share).
        public ExportDelivery Delivery { get; }

        public bool IsShown => Button.style.display != DisplayStyle.None;

        public bool IsBusy => _busy;

        public string? AccessibleName => Button.AccessibleName;

        // El estado del Detalle: la sesión y si se puede exportar.
        public void SetSession(WorkoutSession session, bool exportable)
        {
            _session = session;
            _exportable = exportable;
            Refresh();
        }

        // El glifo de la entrega.
        public ExportGlyph.Kind Glyph => GlyphView.Kind;

        public static ExportGlyph.Kind GlyphFor(ExportDelivery delivery)
        {
            return delivery == ExportDelivery.Share ? ExportGlyph.Kind.Share : ExportGlyph.Kind.Download;
        }

        private void OnBusy(IReadOnlyCollection<long> busy)
        {
            Refresh();
        }

        private void Refresh()
        {
            var id = _session?.Id;
            var shown = _exportable && Delivery != ExportDelivery.None && id.HasValue;
            Button.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            var busy = id.HasValue && _export.IsBusy(id.Value);
            if (busy == _busy && Button.enabledSelf == !busy)
            {
                return;
            }

            _busy = busy;
            Button.SetEnabled(!busy);
            Button.EnableInClassList(BusyClass, busy);
            GlyphView.SetShown(!busy);
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
