using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // "Lo borrado se desvanece y colapsa" (03-rutinas/04-design-notes §5.5, §5.6; MotionSpecs.Collapse*): el contenido funde
    // 1 → 0 en 0-120 (ease.exit) y el slot (con los márgenes adentro) baja su alto al 0 en 80-280 (ease.move); lo de abajo
    // sube sin salto. Al terminar llama a onDone (quien lo usa saca el slot del árbol). Desde su alto actual (borrar algo que
    // está creciendo). Reducir movimiento: fundido de 120 y el reacomodo en el acto. Mientras sale, el contenido no recibe
    // toques (pickingMode Ignore en todo su árbol, como lo que sale en una página o un diálogo).
    public sealed class CollapseMotion : TimedMotion
    {
        private VisualElement? _slot;
        private VisualElement? _content;
        private Action? _onDone;
        private double _from;
        private bool _reduced;

        public CollapseMotion(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => _reduced ? MotionSpecs.CollapseFadeMs : MotionSpecs.CollapseEndMs;

        public void Play(VisualElement slot, VisualElement content, Action onDone)
        {
            Finish();
            _slot = slot;
            _content = content;
            _onDone = onDone;
            _reduced = Driver.Settings.ReduceMotion;
            Shell.ExitLock.Apply(slot, null);
            var height = slot.layout.height;
            _from = height > 0 ? height : 0;
            if (!Driver.Armed || !(_from > 0))
            {
                OnFinished();
                return;
            }

            slot.style.overflow = Overflow.Hidden;
            slot.style.height = (float)_from;
            content.style.flexShrink = 0;
            // Fuera del flujo, con su alto natural: el slot lo recorta mientras se achica (no se aplasta; GrowMotion).
            var top = content.layout.y - content.resolvedStyle.marginTop;
            content.style.position = Position.Absolute;
            content.style.left = 0;
            content.style.right = 0;
            content.style.top = top;
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            if (_slot == null || _content == null)
            {
                return;
            }

            var fade = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.CollapseFadeMs));
            _content.style.opacity = (float)(1 - fade);
            if (_reduced)
            {
                return;
            }

            var shrink = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, MotionSpecs.CollapseStartMs, MotionSpecs.CollapseEndMs));
            _slot.style.height = (float)(_from * (1 - shrink));
        }

        protected override void OnFinished()
        {
            var done = _onDone;
            _onDone = null;
            _slot = null;
            _content = null;
            done?.Invoke();
        }
    }
}
