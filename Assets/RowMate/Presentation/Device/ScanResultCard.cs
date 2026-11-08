using System;
using RowMate.App.Device;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Device
{
    // Una tarjeta de remo encontrado (AC-BLE-05-07, AC-BLE-06-01, -07; device_screen.dart:444-471; 00-arte §1.3, §2.1):
    //   #device-result-<dirección> .rm-scan-card (Button, foco y Enter; surface.card, r.card 16, relleno 12 / 16, 64 de alto mínimo)
    //   ├── .rm-button__state
    //   ├── .rm-scan-card__disc     disco de 40 en accent.tonalBg con rowing 24 en accent.primary (WP-D)
    //   ├── .rm-scan-card__texts    nombre (o deviceUnknown) titleS 16/600 y renglón 2 caption 11/500: la dirección en text.tertiary o, en
    //   │                           la tocada, el paso (deviceStepConnecting / Control / Ready) en text.secondary ("Listo" en state.connected)
    //   │                           con .rm-scan-card__ghost: el texto anterior mientras cruza (sale 0-80, el nuevo entra 80-200; WP-D)
    //   ├── .rm-scan-card__chevron  chevron_right; conectando, el spinner de 20 en su lugar (todas, paridad)
    //   ├── .rm-scan-card__check    "Listo": el check dibujado (LinkIndicator; círculo 0-160, tilde 80-240; WP-D)
    //   └── .rm-focus-ring
    // Conectando, ninguna responde (SetEnabled false: la opacidad 0,38 la pone :disabled y WP-D la funde en 160; la tocada queda en 1,
    // .rm-scan-card--active). Lo dibuja ScanItemSpec (App).
    public sealed class ScanResultCard : Button
    {
        public const string NamePrefix = "device-result-";

        private readonly UiContext _ui;
        private readonly Spinner _spinner;
        private readonly IconView _chevron;
        private readonly Label _ghost;
        private readonly BleMotion.Fade _dim;
        private readonly BleMotion.Fade _ghostFade;
        private readonly BleMotion.Fade _lineFade;
        private LinkIndicator? _check;
        private bool _rendered;
        private bool _enabled = true;

        public ScanResultCard(UiContext ui, string key, Action<string> onTap)
        {
            _ui = ui;
            Key = key;
            name = NamePrefix + key;
            text = "";
            RmButton.StripUnityChrome(this);
            AddToClassList("rm-scan-card");
            focusable = true;
            style.flexShrink = 0;
            Add(RmButton.StateLayer());
            var disc = new VisualElement();
            disc.AddToClassList("rm-scan-card__disc");
            disc.pickingMode = PickingMode.Ignore;
            var icon = new IconView(Icons.Rowing, false, (float)BleMotionSpecs.CardIconDp);
            icon.AddToClassList("rm-scan-card__icon");
            disc.Add(icon);
            Add(disc);
            var texts = new VisualElement();
            texts.AddToClassList("rm-scan-card__texts");
            texts.pickingMode = PickingMode.Ignore;
            NameLabel = new Label();
            NameLabel.AddToClassList("rm-type-title-s");
            NameLabel.AddToClassList("rm-scan-card__name");
            NameLabel.pickingMode = PickingMode.Ignore;
            texts.Add(NameLabel);
            var line = new VisualElement();
            line.AddToClassList("rm-scan-card__line");
            line.pickingMode = PickingMode.Ignore;
            Line2 = new Label();
            Line2.AddToClassList("rm-type-caption");
            Line2.AddToClassList("rm-scan-card__line2");
            Line2.pickingMode = PickingMode.Ignore;
            line.Add(Line2);
            _ghost = new Label();
            _ghost.AddToClassList("rm-type-caption");
            _ghost.AddToClassList("rm-scan-card__line2");
            _ghost.AddToClassList("rm-scan-card__ghost");
            _ghost.pickingMode = PickingMode.Ignore;
            _ghost.style.opacity = 0;
            line.Add(_ghost);
            texts.Add(line);
            Add(texts);
            _chevron = new IconView(Icons.ChevronRight, false, 20);
            _chevron.AddToClassList("rm-scan-card__chevron");
            Add(_chevron);
            _spinner = new Spinner(ui.Motion, 20);
            _spinner.AddToClassList("rm-scan-card__spinner");
            _spinner.SetShown(false);
            Add(_spinner);
            Add(RmButton.FocusRing());
            _dim = new BleMotion.Fade(ui.Motion);
            _ghostFade = new BleMotion.Fade(ui.Motion);
            _lineFade = new BleMotion.Fade(ui.Motion);
            clicked += () => onTap(Key);
        }

        public string Key { get; }

        public Label NameLabel { get; }

        public Label Line2 { get; }

        public bool ShowsSpinner => _spinner.IsShown;

        // "Listo": el check dibujado a la vista (A10-05).
        public bool ShowsCheck => _check != null && _check.Current == LinkIndicator.Mode.Check;

        public void Render(ScanItemSpec spec)
        {
            if (NameLabel.text != spec.Name)
            {
                NameLabel.text = spec.Name;
            }

            // "Listo": la tocada, ya conectada (sin spinner). El check reemplaza al spinner en t₀ (§2.1).
            var ready = spec.IsActive && !spec.ShowSpinner;
            if (Line2.text != spec.Line2)
            {
                CrossLine(Line2.text);
                Line2.text = spec.Line2;
            }

            EnableInClassList("rm-scan-card--active", spec.IsActive);
            Line2.EnableInClassList("rm-scan-card__line2--step", spec.IsActive);
            Line2.EnableInClassList("rm-scan-card__line2--ready", ready);
            _spinner.SetShown(spec.ShowSpinner);
            if (ready)
            {
                _check ??= NewCheck();
                _check.Show(LinkIndicator.Mode.Check);
            }
            else
            {
                _check?.Show(LinkIndicator.Mode.None);
            }

            _chevron.style.display = spec.ShowSpinner || ready ? DisplayStyle.None : DisplayStyle.Flex;
            Dim(spec.Enabled, spec.IsActive);
            SetEnabled(spec.Enabled);
            _rendered = true;
        }

        private LinkIndicator NewCheck()
        {
            var check = new LinkIndicator(_ui.Motion, 20) { name = "device-result-check" };
            check.AddToClassList("rm-scan-card__check");
            Insert(IndexOf(_spinner) + 1, check);
            return check;
        }

        // §2.1: las otras tarjetas bajan a 0,38 en 160 (ease.move) al conectar y vuelven a 1 en 160; la tocada queda en 1. Reducir
        // movimiento: en el acto (lo pone :disabled).
        private void Dim(bool enabled, bool active)
        {
            if (enabled == _enabled)
            {
                return;
            }

            _enabled = enabled;
            if (!_rendered || active || _ui.Motion.Settings.ReduceMotion)
            {
                _dim.Finish();
                style.opacity = StyleKeyword.Null;
                return;
            }

            var from = enabled ? (float)BleMotionSpecs.CardDimOpacity : 1f;
            var to = enabled ? 1f : (float)BleMotionSpecs.CardDimOpacity;
            _dim.Play(this, from, to, 0, BleMotionSpecs.CardDimMs, Tokens.EaseMove, true);
        }

        // §2.1: el renglón 2 cruza (el viejo funde → 0 en 0-80 y el nuevo entra en 80-200). Reducir movimiento: fundido de 80. Si un paso
        // dura menos de 200, el siguiente arranca desde la opacidad que tenga el texto (no se encolan).
        private void CrossLine(string previous)
        {
            if (!_rendered || !_ui.Motion.Armed || string.IsNullOrEmpty(previous))
            {
                return;
            }

            var reduced = _ui.Motion.Settings.ReduceMotion;
            var current = Line2.resolvedStyle.opacity;
            _ghost.text = previous;
            _ghost.EnableInClassList("rm-scan-card__line2--step", Line2.ClassListContains("rm-scan-card__line2--step"));
            _ghost.EnableInClassList("rm-scan-card__line2--ready", Line2.ClassListContains("rm-scan-card__line2--ready"));
            var outEnd = reduced ? BleMotionSpecs.ReducedStepFadeMs : BleMotionSpecs.StepOutMs;
            var inStart = reduced ? 0 : BleMotionSpecs.StepOutMs;
            var inEnd = reduced ? BleMotionSpecs.ReducedStepFadeMs : BleMotionSpecs.StepInEndMs;
            _ghostFade.Play(_ghost, current, 0, 0, outEnd, Tokens.EaseExit, false);
            _lineFade.Play(Line2, 0, 1, inStart, inEnd, Tokens.EaseEnter, true);
        }
    }
}
