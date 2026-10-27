using System;
using UnityEngine;

namespace RowMate.Presentation.Surface
{
    // Lleva la RT gamma de la UI (GammaRt) a la pantalla sin volver a convertir los colores (02-tech-design §3.1, S-B1).
    public interface IUiPresenter : IDisposable
    {
        // La RT de la UI cambió (se creó o se recreó por un cambio de tamaño).
        void SetSource(RenderTexture gammaUi);

        // Costo de la última presentación en ms de CPU (criterio 3 de S-B1).
        double LastCostMs { get; }
    }
}
