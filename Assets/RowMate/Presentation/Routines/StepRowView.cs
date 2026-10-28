using System;
using RowMate.App.Routines;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Fila de un paso en el editor y vista previa del diálogo (AC-RUT-12-02, AC-RUT-13-16; legacy _buildStepTile,
    // routine_editor_screen.dart:316-369; 04-design-notes §4.6):
    //   #step-row-<i> .rm-step-row .rm-step-row--<tipo> [--in-series] [--preview]
    //   ├── .rm-highlight                          capa absoluta sin eventos (resaltado al editar)
    //   ├── .rm-step-row__badge > .rm-step-row__badge-bg (tipo @20 %, por código) + .rm-icon 16 (step.<tipo>.hud)
    //   ├── .rm-step-row__text > .rm-step-row__title (.rm-type-title-xs, step.<tipo>.hud) + StepSubtitleView
    //   ├── #step-edit-<i>   IconButton edit 20 (text.secondary)
    //   └── #step-delete-<i> IconButton close 20 --danger (state.dangerText)
    // Borde izquierdo de 3 dp en el color BASE del tipo y título e ícono en su variante legible (DV-12 ampliada, DN3-03): las
    // clases --<tipo> los ponen por USS; el fondo de la insignia @20 % va por código (USS no aplica transparencia a un var()).
    // La vista previa es la misma fila sin los dos botones (AC-RUT-13-16: al confirmar, la fila del editor es exactamente esa).
    public sealed class StepRowView : VisualElement
    {
        public const string UssClass = "rm-step-row";

        private readonly VisualElement _badgeBg;
        private readonly IconView _icon;
        private readonly Label _title;
        private StepType? _type;

        public StepRowView(UiContext ui, bool preview, Action? onEdit, Action? onDelete)
        {
            AddToClassList(UssClass);
            EnableInClassList(UssClass + "--preview", preview);
            pickingMode = PickingMode.Ignore;
            Highlight = new VisualElement();
            Highlight.AddToClassList("rm-highlight");
            Highlight.pickingMode = PickingMode.Ignore;
            Highlight.style.backgroundColor = TokenColors.ToColor(Core.Design.Tokens.TextPrimary);
            Highlight.style.opacity = 0;
            Add(Highlight);
            HighlightMotion = new HighlightMotion(ui.Motion, Highlight);

            var badge = new VisualElement();
            badge.AddToClassList(UssClass + "__badge");
            badge.pickingMode = PickingMode.Ignore;
            _badgeBg = new VisualElement();
            _badgeBg.AddToClassList(UssClass + "__badge-bg");
            _badgeBg.pickingMode = PickingMode.Ignore;
            badge.Add(_badgeBg);
            _icon = new IconView(Core.Design.Icons.FitnessCenter, false, 16);
            _icon.AddToClassList(UssClass + "__icon");
            badge.Add(_icon);
            Add(badge);

            var text = new VisualElement();
            text.AddToClassList(UssClass + "__text");
            text.pickingMode = PickingMode.Ignore;
            _title = new Label();
            _title.AddToClassList(UssClass + "__title");
            _title.AddToClassList("rm-type-title-xs");
            _title.pickingMode = PickingMode.Ignore;
            text.Add(_title);
            Subtitle = new StepSubtitleView();
            text.Add(Subtitle);
            Add(text);

            if (!preview)
            {
                Edit = new IconButton("", Core.Design.Icons.Edit, onEdit, 20);
                Edit.AddToClassList(UssClass + "__edit");
                Add(Edit);
                Delete = new IconButton("", Core.Design.Icons.Close, onDelete, 20, true);
                Delete.AddToClassList(UssClass + "__delete");
                Add(Delete);
            }
        }

        public VisualElement Highlight { get; }

        public HighlightMotion HighlightMotion { get; }

        public StepSubtitleView Subtitle { get; }

        public Label Title => _title;

        public IconView Icon => _icon;

        public VisualElement BadgeBackground => _badgeBg;

        public IconButton? Edit { get; }

        public IconButton? Delete { get; }

        public StepType? Type => _type;

        public IntervalStep? Step { get; private set; }

        // i = índice en la plantilla (nombres de contrato: step-row-<i>, step-edit-<i>, step-delete-<i>); -1 = sin nombres.
        public void SetIndex(int index)
        {
            name = index >= 0 ? "step-row-" + index : "";
            if (Edit != null)
            {
                Edit.name = index >= 0 ? "step-edit-" + index : "";
            }

            if (Delete != null)
            {
                Delete.name = index >= 0 ? "step-delete-" + index : "";
            }
        }

        public void SetInSeries(bool inSeries)
        {
            EnableInClassList(UssClass + "--in-series", inSeries);
        }

        public void Render(IntervalStep step, Localizer texts)
        {
            Step = step;
            var model = StepRowModel.For(step, texts);
            if (_type != model.Type)
            {
                if (_type.HasValue)
                {
                    RemoveFromClassList(TypeClass(_type.Value));
                }

                _type = model.Type;
                AddToClassList(TypeClass(model.Type));
                _icon.SetGlyph(model.Icon, false);
                _badgeBg.style.backgroundColor = TokenColors.WithAlpha(model.BadgeColor, 0.2);
            }

            if (_title.text != model.Title)
            {
                _title.text = model.Title;
            }

            Subtitle.SetSubtitle(model.Subtitle);
        }

        public static string TypeClass(StepType type)
        {
            return UssClass + "--" + StepTypeNames.ToStorage(type);
        }
    }
}
