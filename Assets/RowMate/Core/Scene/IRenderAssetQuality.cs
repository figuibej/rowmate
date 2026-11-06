namespace RowMate.Core.Scene
{
    public enum QualityTier
    {
        High,
        Medium,
        Low,
    }

    // El puerto puro para lo que solo vive en el asset de URP: el nivel de QualitySettings y la distancia de sombras de la
    // adaptación (06a-escena-lago/02-tech-design §4.2; QA-G6aT-05). En el player lo implementa Platform/Display/
    // PlayerUrpAssetQuality (una copia en ejecución del asset activo); en el Editor, uno que solo anota la acción: el Editor nunca
    // asigna una copia a la configuración ni cambia un asset de URP.
    public interface IRenderAssetQuality
    {
        QualityTier Tier { get; }

        void ApplyTier(QualityTier tier);

        // La distancia de sombras en metros (la adaptación la baja un escalón).
        void SetShadowDistance(double meters);
    }
}
