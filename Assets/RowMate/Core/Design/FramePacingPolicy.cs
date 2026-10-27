using System;

namespace RowMate.Core.Design
{
    // Frecuencia de cuadros por demandas de animación (ARCHITECTURE §13.1). Application.targetFrameRate queda en 60 y el
    // ahorro se hace con OnDemandRendering.renderFrameInterval: 1 (60 fps) mientras alguna animación lo pide y hasta
    // idleSecondsBeforeThirty después de la última; si no, 2 (30 fps). Cada animación pide al empezar (Acquire) y libera al
    // terminar (Release); FramePacer (Presentation) consulta RenderFrameInterval en cada cuadro. Sin asignaciones.
    public sealed class FramePacingPolicy
    {
        public const int SixtyFps = 1;
        public const int ThirtyFps = 2;

        private readonly double _idleSeconds;
        private int _demands;
        private bool _demandedSinceLastQuery;
        private double _lastBusySeconds = double.NegativeInfinity;

        public FramePacingPolicy(double idleSecondsBeforeThirty = 1.0)
        {
            if (!(idleSecondsBeforeThirty >= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(idleSecondsBeforeThirty), idleSecondsBeforeThirty,
                    "FramePacingPolicy: la espera no puede ser negativa.");
            }

            _idleSeconds = idleSecondsBeforeThirty;
        }

        public int Demands => _demands;

        public void Acquire()
        {
            _demands++;
            _demandedSinceLastQuery = true;
        }

        // Una liberación sin demanda es un error de programación (una animación que libera dos veces).
        public void Release()
        {
            if (_demands == 0)
            {
                throw new InvalidOperationException("FramePacingPolicy: Release sin Acquire.");
            }

            _demands--;
        }

        // 1 (60 fps) con demandas o hasta idleSecondsBeforeThirty después de la última vez que hubo; si no, 2 (30 fps).
        // Una demanda que empezó y terminó entre dos consultas cuenta como si hubiera estado en esta (precisión de un cuadro).
        // Al arrancar, sin demandas: 30 fps.
        public int RenderFrameInterval(double nowSeconds)
        {
            if (_demands > 0 || _demandedSinceLastQuery)
            {
                _demandedSinceLastQuery = false;
                _lastBusySeconds = nowSeconds;
                return SixtyFps;
            }

            return nowSeconds - _lastBusySeconds < _idleSeconds ? SixtyFps : ThirtyFps;
        }
    }
}
