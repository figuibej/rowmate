using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RowMate.EditorTools
{
    // Estado del bootstrapper en ProjectSettings/RowMateBootstrap.json (ADR 014, 02-tech-design §1.5).
    // Se serializa con JsonUtility (campos públicos):
    // { "version": 1, "editorVersion": "6000.3.27f1", "appliedAtUtc": "…", "steps": [ { "id", "status", "detail" } ] }
    [Serializable]
    public sealed class BootstrapState
    {
        public const string StatusOk = "ok";
        public const string StatusFailed = "failed";
        public const string StatusSkipped = "skipped";

        public int version;
        public string editorVersion = "";
        public string appliedAtUtc = "";
        public List<Step> steps = new List<Step>();

        // null si el archivo no existe o no se puede leer (se reaplica todo).
        public static BootstrapState? Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<BootstrapState>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning(ProjectBootstrapper.LogPrefix + "no se pudo leer " + path + ": " + e.Message + ". Se reaplica.");
                return null;
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(this, true) + "\n");
        }

        // Resultado de un paso.
        [Serializable]
        public sealed class Step
        {
            public string id = "";
            public string status = "";
            public string detail = "";

            public static Step Ok(string id, string detail)
            {
                return new Step { id = id, status = StatusOk, detail = detail };
            }

            public static Step Skipped(string id, string detail)
            {
                return new Step { id = id, status = StatusSkipped, detail = detail };
            }

            public static Step Failed(string id, string detail)
            {
                return new Step { id = id, status = StatusFailed, detail = detail };
            }
        }
    }
}
