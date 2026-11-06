using UnityEngine;

namespace RowMate.Scene.Slots
{
    // De dónde sale el contenido de un slot (06a-escena-lago/02-tech-design §3.7; QA-G6aT-04): Procedural (el valor por defecto que
    // deja SceneAssetsBuilder: válido siempre y SIN aviso) o Asset (lo que asigna el usuario, del Terrain Sample Asset Pack o de otro
    // lado, bajo Assets/ThirdParty/).
    public enum SlotSource
    {
        Procedural,
        Asset,
    }

    // La base de todos los slots (ARCHITECTURE §14.4; ADR 015; ASSET_SLOTS §0): el id de SlotIds (Core), la ruta esperada del asset
    // de terceros (para re-enlazarlo si el .meta se perdió, RA-17), un presupuesto (triángulos para mallas y prefabs, lado máximo en
    // px para texturas; 0 = sin tope) y la fuente. La lógica no sabe qué hay detrás: pide la resolución (SlotResolver) y recibe el
    // asset o "usá el procedural". Las instancias viven en Assets/RowMate/Slots/ (las únicas que pueden referenciar Assets/ThirdParty/).
    public abstract class AssetSlotDefinition : ScriptableObject
    {
        [SerializeField] private string _slotId = "";
        [SerializeField] private SlotSource _source = SlotSource.Procedural;
        [SerializeField] private string _expectedPath = "";
        [SerializeField] private int _budget;

        public string SlotId => _slotId;

        public SlotSource Source => _source;

        public string ExpectedPath => _expectedPath;

        public int Budget => _budget;

        // El objeto asignado (null con la fuente Procedural, o si falta: una referencia vacía o a un archivo que no está).
        public abstract Object? AssignedObject { get; }

        // El tipo que acepta (para el validador del Editor).
        public abstract System.Type AssetType { get; }

        // Lo arma SceneAssetsBuilder (idempotente: no pisa una fuente Asset que puso el usuario).
        public void Configure(string slotId, string expectedPath, int budget)
        {
            _slotId = slotId ?? "";
            _expectedPath = expectedPath ?? "";
            _budget = budget;
        }

        // La fuente: Procedural limpia la referencia; Asset la deja (el usuario la asigna con AssignObject o en el Inspector).
        public void SetSource(SlotSource source)
        {
            _source = source;
            if (source == SlotSource.Procedural)
            {
                ClearAssigned();
            }
        }

        // Asigna un asset de terceros (fuente Asset). false si no es del tipo del slot.
        public bool AssignObject(Object? asset)
        {
            if (asset != null && !AssetType.IsInstanceOfType(asset))
            {
                return false;
            }

            _source = SlotSource.Asset;
            SetAssigned(asset);
            return true;
        }

        // ¿El asset asignado es válido? (formato, tamaño, mips, presupuesto). Solo se pregunta con la fuente Asset y el asset presente.
        public abstract bool ValidateAssigned(out string reason);

        protected abstract void SetAssigned(Object? asset);

        protected abstract void ClearAssigned();
    }
}
