using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using UnityEditor;
using UnityEngine;

namespace RowMate.EditorTools
{
    // QA-V2-07 (It. 2): los builds, los tests y el arnés de capturas volvían a escribir los 9 FontAsset generados (atlas
    // dinámicos) y los assets de URP en cada corrida, con contenido que podía depender de lo que la corrida dibujó.
    // Este AssetModificationProcessor saca de cada guardado esos FontAsset (siempre) y los assets de URP de
    // Assets/RowMate/Settings/URP (solo en batchmode: builds, tests y capturas; en la ventana del Editor se pueden ajustar).
    // Solo los escribe quien abre AllowWrites(): el bootstrapper (ProjectBootstrapper.Apply, que crea los que faltan). Cada
    // corrida arranca del mismo estado versionado, así que lo que dibuja es determinista.
    // Estado versionado de un FontAsset: sin glifos ni caracteres y con el atlas de 1 × 1 (lo dinámico se agrega en memoria
    // al dibujar), CON su tabla de funciones tipográficas (kerning GPOS). Esa tabla es parte del aspecto: vaciada (por
    // ejemplo con FontAsset.ClearFontAssetData), TextCore agrega los pares solo para los glifos nuevos y el kerning de un
    // mismo texto cambia según el orden en que aparecieron sus letras (medido al cerrar QA-V2-07: CAP-06, CAP-07, CAP-25 y
    // AUX-02 cambiaban, y AUX-02 dejaba de ser igual a CAP-01). Completarla para los textos nuevos es una decisión de arte
    // (docs/migration/iterations/03-rutinas/00-deuda-it2.md). Lo controla ItTwoDebtTests (dotnet) sobre los .asset versionados.
    public sealed class GeneratedAssetGuard : AssetModificationProcessor
    {
        public const string LogPrefix = "[ASSETS] ";
        public const string UrpFolder = "Assets/RowMate/Settings/URP/";

        private static readonly HashSet<string> Reported = new HashSet<string>(StringComparer.Ordinal);
        private static int _writers;

        // Mientras dure el scope, los assets protegidos se guardan normalmente.
        public static IDisposable AllowWrites()
        {
            _writers++;
            return new WriteScope();
        }

        public static bool IsGeneratedFont(string path)
        {
            var all = FontAssetPaths.All;
            for (var i = 0; i < all.Count; i++)
            {
                if (string.Equals(all[i], path, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // ¿Se saca este path del guardado? Fuentes generadas siempre; URP solo en batchmode.
        public static bool IsProtected(string path, bool batchMode)
        {
            if (IsGeneratedFont(path))
            {
                return true;
            }

            return batchMode && path.StartsWith(UrpFolder, StringComparison.Ordinal) && path.EndsWith(".asset", StringComparison.Ordinal);
        }

        // Unity llama a este método antes de escribir assets (SaveAssets, guardado al salir, Ctrl+S); devuelve los que se guardan.
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (_writers > 0)
            {
                return paths;
            }

            var kept = new List<string>(paths.Length);
            var batchMode = Application.isBatchMode;
            foreach (var path in paths)
            {
                if (!IsProtected(path, batchMode))
                {
                    kept.Add(path);
                    continue;
                }

                if (Reported.Add(path))
                {
                    Debug.Log(LogPrefix + "no se guarda " + path + " (generado o de URP; QA-V2-07): queda como está versionado.");
                }
            }

            return kept.Count == paths.Length ? paths : kept.ToArray();
        }

        private sealed class WriteScope : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _writers--;
            }
        }
    }
}
