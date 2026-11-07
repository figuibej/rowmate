using System;
using System.Collections.Generic;
using RowMate.Core.Scene;
using RowMate.Scene.Quality;
using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Los materiales de la escena (Art/Materials/Scene/*.mat, los crea SceneMaterialsBuilder con los shaders de WP-D1). La escena
    // trabaja con COPIAS en memoria (los .mat del proyecto no cambian en Play: SceneQualityApplier les pone el nivel a las copias).
    [Serializable]
    public sealed class SceneMaterialSet
    {
        public Material? Sky;
        public Material? Stars;
        public Material? Clouds;
        public Material? HorizonFar;
        public Material? HorizonNear;
        public Material? Water;
        public Material? Terrain;
        public Material? Prop;
        public Material? PropWindow;
        public Material? Foliage;
        public Material? Grass;
        public Material? Reeds;
        public Material? Hull;
        public Material? BoatShadow;
        public Material? ScreenOverlay;

        // It. 7 (07-remero-palada/02-tech-design §1.3; WP-B1): el remero, los remos, la salpicadura, el agua que responde y la boya. Sin
        // ellos (un SceneWorld.asset de antes de la 7) el mundo copia el shader por nombre.
        public Material? Rower;
        public Material? Oar;
        public Material? Spray;
        public Material? WaterMarks;
        public Material? Buoy;

        public Material?[] All()
        {
            return new[]
            {
                Sky, Stars, Clouds, HorizonFar, HorizonNear, Water, Terrain, Prop, PropWindow, Foliage, Grass, Reeds, Hull, BoatShadow, ScreenOverlay, Rower, Oar,
                Spray, WaterMarks, Buoy,
            };
        }

        public bool IsComplete
        {
            get
            {
                foreach (var material in All())
                {
                    if (material == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    // Todo lo que arma el mundo de la escena (06a-escena-lago/02-tech-design §3.4; DT6a-04: sin escenas aditivas en la 6a): el
    // escenario (Lake.asset), los slots del cielo y del bote, los tres niveles de calidad, los materiales y la colección de variantes.
    // Lo crea SceneAssetsBuilder en Scenes/SceneWorld.asset (B0, sin materiales) y SceneMaterialsBuilder le suma los materiales y la
    // colección (B1). AppRoot lo recibe por UiAssets; UiTestHost y el arnés leen el mismo asset.
    [CreateAssetMenu(menuName = "RowMate/Scene/World", fileName = "SceneWorld")]
    public sealed class SceneWorldDefinition : ScriptableObject
    {
        public const string AssetPath = "Assets/RowMate/Scenes/SceneWorld.asset";

        [SerializeField] private EnvironmentDefinition? _lake;
        [SerializeField] private List<AssetSlotDefinition> _slots = new List<AssetSlotDefinition>();
        [SerializeField] private QualityTierProfile? _high;
        [SerializeField] private QualityTierProfile? _medium;
        [SerializeField] private QualityTierProfile? _low;
        [SerializeField] private SceneMaterialSet _materials = new SceneMaterialSet();
        [SerializeField] private ShaderVariantCollection? _variants;

        public EnvironmentDefinition? Lake => _lake;

        public IReadOnlyList<AssetSlotDefinition> Slots => _slots;

        public SceneMaterialSet Materials => _materials;

        public ShaderVariantCollection? Variants => _variants;

        public QualityTierProfile? Profile(QualityTier tier)
        {
            return tier == QualityTier.High ? _high : tier == QualityTier.Medium ? _medium : _low;
        }

        // El slot del mundo o del escenario con ese id.
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

            return _lake != null ? _lake.Find(slotId) : null;
        }

        public bool HasSlotsAndTiers => _lake != null && _high != null && _medium != null && _low != null;

        public bool IsComplete => HasSlotsAndTiers && _materials.IsComplete;

        // Para SceneAssetsBuilder (B0).
        public void SetSlots(EnvironmentDefinition lake, IEnumerable<AssetSlotDefinition> slots, QualityTierProfile high, QualityTierProfile medium,
                             QualityTierProfile low)
        {
            _lake = lake;
            _slots = new List<AssetSlotDefinition>(slots);
            _high = high;
            _medium = medium;
            _low = low;
        }

        // Para SceneMaterialsBuilder (B1).
        public void SetMaterials(SceneMaterialSet materials, ShaderVariantCollection? variants)
        {
            _materials = materials ?? new SceneMaterialSet();
            _variants = variants;
        }

        // Para las pruebas: una copia en memoria con otro escenario (el slot de prueba).
        public SceneWorldDefinition WithLake(EnvironmentDefinition lake)
        {
            var copy = Instantiate(this);
            copy._lake = lake;
            copy.hideFlags = HideFlags.HideAndDontSave;
            return copy;
        }
    }
}
