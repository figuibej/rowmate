using RowMate.Core.Scene;
using RowMate.Core.Scene.Water;
using UnityEngine;

namespace RowMate.Scene
{
    // Las dos aguas de la escena (06b-agua-avanzada/02-tech-design §4.1, §5; DT6b-12, DT6b-22; G6b-01, G6b-14): la degradada de la 6a
    // (WaterSurface, la grilla: el respaldo obligatorio, R-04) y la avanzada (AdvancedWaterSurface, los anillos). Nunca se dibujan las dos:
    // con el fundido hacia la avanzada, el renderer avanzado se prende con _RmDegradedBlend < 1 (con 1 dibuja lo mismo que la degradada) y
    // la grilla se apaga en el MISMO cuadro; hacia la degradada, la avanzada sigue con el blend 0 → 1 y al llegar a 1 se apaga y vuelve la
    // grilla. Pone por cuadro los globales del agua (las fases y corrimientos de Core, cuánto sube lo que flota en la costa, el blend y el
    // apagado de la estela), que también usan las marcas y la estela con la degradada. Apply no asigna.
    public sealed class WaterSurfaceController
    {
        public WaterSurfaceController(WaterSurface degraded, AdvancedWaterSurface? advanced)
        {
            Degraded = degraded;
            Advanced = advanced;
        }

        public WaterSurface Degraded { get; }

        public AdvancedWaterSurface? Advanced { get; }

        // El soporte del agua avanzada ya se comprobó y la variante se preparó (SceneHost, fase 13): hasta entonces nunca se prende.
        public bool AdvancedAvailable { get; set; }

        // Lo que se dibujó en el último Apply (las pruebas y las sondas del arnés).
        public bool ShowingAdvanced { get; private set; }

        public double LastBlend { get; private set; } = 1;

        // El agua avanzada no pudo aplicarse (una excepción): SceneHost lo informa una vez y corta a la degradada en el acto (DT6b-12).
        public bool Failed { get; set; }

        public string? FailureReason { get; private set; }

        // Para las pruebas: la próxima Apply de la avanzada falla como una excepción del dibujo.
        public bool FailNextApplyForTest { get; set; }

        public bool Owns(Renderer renderer)
        {
            return ReferenceEquals(renderer, Degraded.Renderer) || Advanced != null && ReferenceEquals(renderer, Advanced.Renderer);
        }

        public void Apply(in SceneFrame frame)
        {
            var water = frame.Water;
            var ph = water.Phases;
            var p = water.Profile;
            Shader.SetGlobalVector(SceneShaderIds.WaterPhases, new Vector4((float)ph.Phase1, (float)ph.Phase2, (float)p.Amplitude, (float)p.Scale));
            Shader.SetGlobalVector(SceneShaderIds.WaterOffsets, new Vector4((float)ph.B2U, (float)ph.B2V, (float)ph.B3U, (float)ph.B3V));
            Shader.SetGlobalVector(SceneShaderIds.WaterOffsets2, new Vector4((float)ph.B4U, (float)ph.B4V, (float)ph.NoiseV, 0f));
            Shader.SetGlobalFloat(SceneShaderIds.WaterLift, p.Swell ? (float)CoastSwell.HullScale : 0f);
            var blend = water.DegradedBlend;
            LastBlend = blend;
            Shader.SetGlobalFloat(SceneShaderIds.DegradedBlend, (float)blend);
            Shader.SetGlobalFloat(SceneShaderIds.WakeFade, (float)(1 - blend));

            var advanced = Advanced != null && AdvancedAvailable && blend < 0.9999;
            ShowingAdvanced = advanced;
            if (Degraded.Renderer.enabled == advanced)
            {
                Degraded.Renderer.enabled = !advanced;
            }

            if (Advanced != null)
            {
                if (Advanced.Renderer.enabled != advanced)
                {
                    Advanced.Renderer.enabled = advanced;
                }

                if (advanced)
                {
                    try
                    {
                        if (FailNextApplyForTest)
                        {
                            FailNextApplyForTest = false;
                            throw new System.InvalidOperationException("falla de prueba del agua avanzada");
                        }

                        Advanced.Apply(frame);
                    }
                    catch (System.Exception error)
                    {
                        // No puede dibujar: la grilla vuelve en este mismo cuadro.
                        Failed = true;
                        FailureReason = "falla de dibujo: " + error.Message;
                        AdvancedAvailable = false;
                        ShowingAdvanced = false;
                        Advanced.Renderer.enabled = false;
                        Degraded.Renderer.enabled = true;
                    }
                }
            }
        }
    }
}
