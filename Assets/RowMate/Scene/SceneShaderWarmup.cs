using UnityEngine;

namespace RowMate.Scene
{
    // El precalentado de los shaders por tramos (06a-escena-lago/02-tech-design §3.4 paso 6; QA-G6aT-03, -07): en el player,
    // ShaderVariantCollection.WarmUpProgressively(1) por unidad sobre la colección de la escena (SceneShaderVariants), sin pasos
    // indivisibles; en el Editor la compilación es asíncrona (el dibujo nunca espera) y la escena sigue en Loading mientras el Editor
    // diga que hay algo compilando (ShaderUtil.anythingCompiling), así el dibujo de calentamiento sale con los shaders de verdad. Las
    // pruebas y el arnés que leen píxeles apagan la compilación asíncrona (ShaderUtil.allowAsyncCompilation, que no se guarda).
    public sealed class SceneShaderWarmup
    {
        private readonly ShaderVariantCollection? _variants;

        public SceneShaderWarmup(ShaderVariantCollection? variants)
        {
            _variants = variants;
        }

        public int Units { get; private set; }

        // Una unidad: una variante (player) o una espera (Editor). true cuando ya no queda nada.
        public bool Step()
        {
            Units++;
#if UNITY_EDITOR
            return !UnityEditor.ShaderUtil.anythingCompiling;
#else
            if (_variants == null || _variants.isWarmedUp)
            {
                return true;
            }

            return _variants.WarmUpProgressively(1);
#endif
        }

        // En el Editor: ¿hay algo compilando? (la sonda de la escena demorada y el arnés esperan a que no).
        public static bool AnythingCompiling
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.ShaderUtil.anythingCompiling;
#else
                return false;
#endif
            }
        }
    }
}
