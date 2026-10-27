using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace RowMate.Presentation.Motion
{
    // Aplica FramePacingPolicy a OnDemandRendering (ARCHITECTURE §13.1, 02-tech-design §3.9): 60 fps (intervalo 1) con algo
    // animándose, un dedo o el botón del mouse apoyado (y 500 ms después), o hasta 1 s después de la última demanda; si no,
    // 30 fps (intervalo 2). Application.targetFrameRate = 60. Solo lo usa la app (AppRoot); los tests y el arnés no.
    public sealed class FramePacer
    {
        private const double PointerHoldSeconds = 0.5;

        private readonly FramePacingPolicy _policy;
        private double _lastPointerDown = double.NegativeInfinity;
        private bool _holding;

        public FramePacer(FramePacingPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            Application.targetFrameRate = 60;
        }

        public int Interval { get; private set; } = FramePacingPolicy.SixtyFps;

        public void Apply(double nowSeconds)
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed)
            {
                _lastPointerDown = nowSeconds;
            }

            var hold = nowSeconds - _lastPointerDown < PointerHoldSeconds;
            if (hold && !_holding)
            {
                _policy.Acquire();
                _holding = true;
            }
            else if (!hold && _holding)
            {
                _policy.Release();
                _holding = false;
            }

            Interval = _policy.RenderFrameInterval(nowSeconds);
            OnDemandRendering.renderFrameInterval = Interval;
        }
    }
}
