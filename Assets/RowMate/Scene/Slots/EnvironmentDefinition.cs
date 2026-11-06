using System;
using System.Collections.Generic;
using RowMate.Core.Scene;
using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Los slots de un escenario (06a-escena-lago/02-tech-design §3.4, §3.7; ADR 015): en la 6a, el Lago alpino (prop.*,
    // terrain.layer.*, vegetation.*, horizon.lake, water.profile.lake). Lo crea SceneAssetsBuilder en Scenes/Environments/Lake.asset.
    // Las pruebas usan una copia en memoria (Instantiate) para asignar el slot de prueba sin tocar el asset.
    [CreateAssetMenu(menuName = "RowMate/Scene/Environment", fileName = "Environment")]
    public sealed class EnvironmentDefinition : ScriptableObject
    {
        [SerializeField] private EnvironmentId _environment = EnvironmentId.Lake;
        [SerializeField] private List<AssetSlotDefinition> _slots = new List<AssetSlotDefinition>();

        public EnvironmentId Environment => _environment;

        public IReadOnlyList<AssetSlotDefinition> Slots => _slots;

        public AssetSlotDefinition? Find(string slotId)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot != null && string.Equals(slot.SlotId, slotId, StringComparison.Ordinal))
                {
                    return slot;
                }
            }

            return null;
        }

        // Para SceneAssetsBuilder (Editor) y las pruebas (una copia en memoria).
        public void Set(EnvironmentId environment, IEnumerable<AssetSlotDefinition> slots)
        {
            _environment = environment;
            _slots = new List<AssetSlotDefinition>(slots);
        }

        // Reemplaza un slot (las pruebas: el slot de prueba en una copia en memoria).
        public void Replace(AssetSlotDefinition slot)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null && string.Equals(_slots[i].SlotId, slot.SlotId, StringComparison.Ordinal))
                {
                    _slots[i] = slot;
                    return;
                }
            }

            _slots.Add(slot);
        }
    }
}
