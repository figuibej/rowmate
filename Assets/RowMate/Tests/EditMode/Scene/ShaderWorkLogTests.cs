using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // SWL · El detector de "shaders de la escena después de Ready" de [PERF] (06a-escena-lago/02-tech-design §4.5, "Cambios por
    // verificación (ronda 1)"; QA-V6a-REN-01): cuenta las líneas que el player escribe de verdad ("Uploaded shader variant to the GPU
    // driver: …"), de los shaders de la escena y de los de URP, solo después de la marca de Ready; con un control positivo (una subida de
    // UberPost después de la marca: 1) y uno negativo (lo de la UI y lo de antes de la marca: 0). Las líneas se arman acá, con nombres
    // de prueba (sin nada del equipo).
    public sealed class ShaderWorkLogTests
    {
        private static string Upload(string shader, string stage, string keywords)
        {
            return "Uploaded shader variant to the GPU driver: " + shader + " (instance 0x10), pass: <Unnamed Pass 0>, stage: " + stage
                   + ", keywords " + keywords + ", time: 0.9 ms";
        }

        private static readonly string Mark = ShaderWorkLog.ReadyMark;

        [Test, Category("AC-WKT-13-15")]
        public void CountsTheRealPlayerLinesAfterTheMark()
        {
            var lines = new List<string>
            {
                "[SCENE] escena: nivel High",
                Upload("RowMate/Scene/Sky", "pixel", "<no keywords>"),
                Upload("Hidden/Universal Render Pipeline/UberPost", "pixel", "_BLOOM_LQ"),
                "[UI] HUD precalentado",
                Mark,
                Upload("Hidden/Internal-UIRDefault", "vertex", "_UIE_FORCE_GAMMA"),
                "[PERF] entrada 1: libre",
                Upload("Hidden/Universal Render Pipeline/UberPost", "pixel", "<no keywords>"),
            };
            var found = new List<string>();
            Assert.That(ShaderWorkLog.CountAfterReady(lines, found), Is.EqualTo(1), "el control positivo: la subida de UberPost después de Ready");
            Assert.That(found[0], Does.Contain("UberPost"));
            lines.Add(Upload("RowMate/Scene/WaterDegraded", "pixel", "RM_WATER_EXTRAS"));
            lines.Add("Compiled shader: RowMate/Scene/Terrain, pass: ShadowCaster");
            Assert.That(ShaderWorkLog.CountAfterReady(lines), Is.EqualTo(3), "la de la escena y la compilación del Editor también");
        }

        [Test, Category("AC-WKT-13-15")]
        public void TheUiAndWhatCameBeforeTheMarkDoNotCount()
        {
            var lines = new List<string>
            {
                Upload("RowMate/Scene/Sky", "pixel", "<no keywords>"),
                Upload("Hidden/Universal Render Pipeline/Bloom", "pixel", "<no keywords>"),
                Mark,
                Upload("Hidden/Internal-UIRDefault", "pixel", "<no keywords>"),
                Upload("Hidden/Internal-GUITexture", "vertex", "<no keywords>"),
                "un texto con RowMate/Scene/ pero sin subida",
            };
            Assert.That(ShaderWorkLog.CountAfterReady(lines), Is.EqualTo(0));
            Assert.That(ShaderWorkLog.IsSceneShaderWork(null), Is.False);
            Assert.That(ShaderWorkLog.IsSceneShaderWork(""), Is.False);
        }

        [Test, Category("AC-WKT-13-15")]
        public void WithoutTheMarkItIsNotVerifiable()
        {
            var lines = new List<string> { Upload("Hidden/Universal Render Pipeline/UberPost", "pixel", "<no keywords>") };
            Assert.That(ShaderWorkLog.CountAfterReady(lines), Is.EqualTo(-1), "sin la marca de Ready: NO VERIFICABLE");
        }
    }
}
