using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RowMate.Scene.Slots
{
    // Un slot tipado (ARCHITECTURE §14.4): guarda la referencia al asset de terceros y su validación. El procedural (el placeholder
    // de ASSET_SLOTS §8) no vive acá: lo arma la escena (SceneWorldBuilder) cuando la resolución dice "procedural". Unity no crea
    // assets de una clase genérica: cada slot concreto es una subclase (PropSlot, TerrainLayerSlot…).
    public abstract class AssetSlot<T> : AssetSlotDefinition where T : Object
    {
        [SerializeField] private T? _asset;

        public T? Asset => _asset;

        public override Object? AssignedObject => _asset;

        public override Type AssetType => typeof(T);

        public override bool ValidateAssigned(out string reason)
        {
            if (_asset == null)
            {
                reason = "falta el asset asignado";
                return false;
            }

            return ValidateAsset(_asset, out reason);
        }

        protected virtual bool ValidateAsset(T asset, out string reason)
        {
            reason = "";
            return true;
        }

        protected override void SetAssigned(Object? asset)
        {
            _asset = asset as T;
        }

        protected override void ClearAssigned()
        {
            _asset = null;
        }

        // Los triángulos de una malla (para el presupuesto).
        protected static int Triangles(Mesh? mesh)
        {
            if (mesh == null)
            {
                return 0;
            }

            var total = 0L;
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                total += (long)mesh.GetIndexCount(i) / 3;
            }

            return (int)Math.Min(int.MaxValue, total);
        }

        // Una textura de un pack: cuadrada, potencia de 2 y dentro del tope (el Texture2DArray del terreno las exige iguales).
        protected bool ValidateTexture(Texture? texture, string what, out string reason)
        {
            if (texture == null)
            {
                reason = "falta " + what;
                return false;
            }

            if (Budget > 0 && (texture.width > Budget || texture.height > Budget))
            {
                reason = what + " de " + texture.width + " × " + texture.height + " px (tope " + Budget + ")";
                return false;
            }

            if (!Mathf.IsPowerOfTwo(texture.width) || texture.width != texture.height)
            {
                reason = what + " no es cuadrada y potencia de 2 (" + texture.width + " × " + texture.height + ")";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
