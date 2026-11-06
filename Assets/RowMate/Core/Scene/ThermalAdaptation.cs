using System;

namespace RowMate.Core.Scene
{
    public enum QualityActionKind
    {
        None,
        LowerResolution,
        RaiseResolution,
        LowerShadowDistance,
        LowerVegetationDensity,
        LowerTier,
    }

    // Lo que hay que aplicar después de un cuadro: la acción y la resolución nueva de la RT (si es de resolución).
    public readonly struct QualityAction
    {
        public QualityAction(QualityActionKind kind, double resolution)
        {
            Kind = kind;
            Resolution = resolution;
        }

        public static QualityAction None => new QualityAction(QualityActionKind.None, double.NaN);

        public QualityActionKind Kind { get; }

        public double Resolution { get; }
    }

    // Las constantes de la adaptación (ARCHITECTURE §20.3; §9.3): disparo a 13 ms de GPU (15 de CPU) sostenidos 3 s; resolución
    // de a 0,05 entre 0,75 y 1, como mucho un cambio cada 10 s; subir con 30 s holgados bajo 10 ms.
    public readonly struct ThermalSpec
    {
        public ThermalSpec(double gpuThresholdMs, double cpuThresholdMs, double sustainSeconds, double resolutionStep, double minResolution,
                           double maxResolution, double resolutionCooldownSeconds, double raiseBelowMs, double raiseAfterSeconds)
        {
            GpuThresholdMs = gpuThresholdMs;
            CpuThresholdMs = cpuThresholdMs;
            SustainSeconds = sustainSeconds;
            ResolutionStep = resolutionStep;
            MinResolution = minResolution;
            MaxResolution = maxResolution;
            ResolutionCooldownSeconds = resolutionCooldownSeconds;
            RaiseBelowMs = raiseBelowMs;
            RaiseAfterSeconds = raiseAfterSeconds;
        }

        public static ThermalSpec Default => new ThermalSpec(13, 15, 3, 0.05, 0.75, 1.0, 10, 10, 30);

        public double GpuThresholdMs { get; }
        public double CpuThresholdMs { get; }
        public double SustainSeconds { get; }
        public double ResolutionStep { get; }
        public double MinResolution { get; }
        public double MaxResolution { get; }
        public double ResolutionCooldownSeconds { get; }
        public double RaiseBelowMs { get; }
        public double RaiseAfterSeconds { get; }
    }

    // La adaptación del detalle (06a-escena-lago/02-tech-design §2.16; AC-SCN-01-13): con los TIEMPOS DE TRABAJO de cada cuadro
    // (GPU de FrameTimingManager; de respaldo, CPU del hilo principal más el render thread, sin la espera de presentación), nunca
    // con el intervalo entre cuadros (un cuadro de 33 ms con 8 ms de trabajo es holgado). Orden: resolución → (reflejo: no hay en
    // la 6a) → distancia de sombras → densidad de vegetación → nivel. Nunca baja la tasa de cuadros. Solo mide los cuadros en que
    // la escena se dibujó: suspendida (renderFrameInterval 2, fuera del inmersivo, la escena oculta) no suma muestras y al volver
    // la ventana arranca de cero (QA-G6aT-10).
    public sealed class ThermalAdaptation
    {
        private readonly ThermalSpec _spec;
        private double _overSince = double.NaN;
        private double _underSince = double.NaN;
        private double _lastResolutionChange = double.NegativeInfinity;
        private bool _shadowsLowered;
        private bool _vegetationLowered;
        private bool _tierLowered;

        public ThermalAdaptation(ThermalSpec spec, double initialResolution)
        {
            _spec = spec;
            Resolution = initialResolution;
        }

        public ThermalAdaptation()
            : this(ThermalSpec.Default, 1.0)
        {
        }

        public double Resolution { get; private set; }

        public bool ShadowsLowered => _shadowsLowered;

        public bool VegetationLowered => _vegetationLowered;

        public bool TierLowered => _tierLowered;

        // Un cuadro: now (s, monótono), su tiempo de trabajo (ms), si es de GPU, si la escena se dibujó y si la medida está
        // suspendida. Devuelve la acción a aplicar (None casi siempre).
        public QualityAction Sample(double now, double workMs, bool isGpu, bool sceneDrawn, bool suspended)
        {
            if (suspended || !sceneDrawn || double.IsNaN(workMs))
            {
                _overSince = double.NaN;
                _underSince = double.NaN;
                return QualityAction.None;
            }

            var threshold = isGpu ? _spec.GpuThresholdMs : _spec.CpuThresholdMs;
            if (workMs > threshold)
            {
                _underSince = double.NaN;
                if (double.IsNaN(_overSince))
                {
                    _overSince = now;
                }

                if (now - _overSince >= _spec.SustainSeconds)
                {
                    var action = Lower(now);
                    if (action.Kind != QualityActionKind.None)
                    {
                        _overSince = now;
                    }

                    return action;
                }

                return QualityAction.None;
            }

            _overSince = double.NaN;
            if (workMs < _spec.RaiseBelowMs)
            {
                if (double.IsNaN(_underSince))
                {
                    _underSince = now;
                }

                if (now - _underSince >= _spec.RaiseAfterSeconds && Resolution < _spec.MaxResolution - 1e-9
                    && now - _lastResolutionChange >= _spec.ResolutionCooldownSeconds)
                {
                    Resolution = Math.Min(_spec.MaxResolution, Round2(Resolution + _spec.ResolutionStep));
                    _lastResolutionChange = now;
                    _underSince = now;
                    return new QualityAction(QualityActionKind.RaiseResolution, Resolution);
                }
            }
            else
            {
                _underSince = double.NaN;
            }

            return QualityAction.None;
        }

        private QualityAction Lower(double now)
        {
            if (Resolution > _spec.MinResolution + 1e-9)
            {
                if (now - _lastResolutionChange < _spec.ResolutionCooldownSeconds)
                {
                    return QualityAction.None;
                }

                Resolution = Math.Max(_spec.MinResolution, Round2(Resolution - _spec.ResolutionStep));
                _lastResolutionChange = now;
                return new QualityAction(QualityActionKind.LowerResolution, Resolution);
            }

            if (!_shadowsLowered)
            {
                _shadowsLowered = true;
                return new QualityAction(QualityActionKind.LowerShadowDistance, Resolution);
            }

            if (!_vegetationLowered)
            {
                _vegetationLowered = true;
                return new QualityAction(QualityActionKind.LowerVegetationDensity, Resolution);
            }

            if (!_tierLowered)
            {
                _tierLowered = true;
                return new QualityAction(QualityActionKind.LowerTier, Resolution);
            }

            return QualityAction.None;
        }

        // Pasos de 0,05 sin arrastrar error (1 − 0,05·5 = 0,75 exacto).
        private static double Round2(double x)
        {
            return Math.Floor(x * 100 + 0.5) / 100;
        }
    }
}
