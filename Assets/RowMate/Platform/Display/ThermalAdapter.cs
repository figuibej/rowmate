using RowMate.Core.Scene;
using UnityEngine;

namespace RowMate.Platform.Display
{
    // La medida de la adaptación térmica (06a-escena-lago/02-tech-design §2.16, §4.2; ARCHITECTURE §20.3; AC-SCN-01-13; QA-G6aT-10):
    // los TIEMPOS DE TRABAJO de cada cuadro de FrameTimingManager (Frame Timing Stats lo prende el bootstrapper): el de GPU si está; si
    // no, el del hilo principal sin la espera de presentación más el render thread. Nunca el intervalo entre cuadros. Solo los cuadros
    // en que la escena se dibujó; con renderFrameInterval > 1 (el diálogo quieto a 30 fps, P-07) o fuera del inmersivo, suspendida.
    // La decisión (umbrales, histéresis, orden) es de Core (ThermalAdaptation); Bootstrap aplica la acción. Sin asignaciones.
    public sealed class ThermalAdapter
    {
        private readonly ThermalAdaptation _adaptation;
        private readonly FrameTiming[] _timings = new FrameTiming[1];

        public ThermalAdapter(double initialResolution)
        {
            _adaptation = new ThermalAdaptation(ThermalSpec.Default, initialResolution);
        }

        public ThermalAdaptation Adaptation => _adaptation;

        public double LastWorkMs { get; private set; }

        public bool LastWasGpu { get; private set; }

        // Un cuadro. Las pruebas pasan sus tiempos con SampleWith.
        public QualityAction Sample(double now, bool sceneDrawn)
        {
            var suspended = !sceneDrawn || UnityEngine.Rendering.OnDemandRendering.renderFrameInterval > 1;
            FrameTimingManager.CaptureFrameTimings();
            var count = FrameTimingManager.GetLatestTimings(1, _timings);
            if (count == 0)
            {
                return _adaptation.Sample(now, 0, false, sceneDrawn, true);
            }

            var timing = _timings[0];
            var gpu = timing.gpuFrameTime > 0;
            var work = gpu
                ? timing.gpuFrameTime
                : System.Math.Max(0, timing.cpuMainThreadFrameTime - timing.cpuMainThreadPresentWaitTime) + timing.cpuRenderThreadFrameTime;
            return SampleWith(now, work, gpu, sceneDrawn, suspended);
        }

        public QualityAction SampleWith(double now, double workMs, bool isGpu, bool sceneDrawn, bool suspended)
        {
            LastWorkMs = workMs;
            LastWasGpu = isGpu;
            return _adaptation.Sample(now, workMs, isGpu, sceneDrawn, suspended);
        }
    }
}
