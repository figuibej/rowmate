using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Un perfil de datos (un ScriptableObject con números de arte: el cielo, una cordillera, el agua de un escenario). Unity no crea
    // assets de un ScriptableObject genérico: los concretos son SkyProfileSlot, HorizonProfileSlot y WaterProfileSlot. Sin asignar:
    // los números de Core/Scene/Art (SkyArt, LakeArt, WaterArt).
    public abstract class DataSlot : AssetSlot<ScriptableObject>
    {
    }
}
