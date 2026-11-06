using System;
using RowMate.Core.Scene;
using UnityEngine;

namespace RowMate.Tests.UiSupport
{
    // Fuerza un nivel de calidad para una prueba y lo restaura SIEMPRE (06a-escena-lago/02-tech-design §4.2; QA-G6aT-05): guarda el
    // nivel del Editor, llama QualitySettings.SetQualityLevel(nivel, false) (lo que vive en el asset de URP: sombras, MSAA) y en Dispose
    // vuelve al de antes. No asigna ni cambia ningún asset de URP. El fixture que la usa afirma al terminar que el nivel es el del
    // principio (y la guardia de ProjectSettings de los scripts, que nada quedó guardado).
    public sealed class TierScope : IDisposable
    {
        private readonly int _before;
        private bool _disposed;

        public TierScope(QualityTier tier)
        {
            _before = QualitySettings.GetQualityLevel();
            Tier = tier;
            var index = tier == QualityTier.Low ? 0 : tier == QualityTier.Medium ? 1 : 2;
            if (index < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(index, false);
            }
        }

        public QualityTier Tier { get; }

        public int LevelBefore => _before;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            QualitySettings.SetQualityLevel(_before, false);
        }
    }
}
