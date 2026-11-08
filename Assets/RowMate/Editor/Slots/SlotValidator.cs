using System.Collections.Generic;
using System.Text;
using RowMate.Scene.Slots;
using UnityEditor;
using UnityEngine;

namespace RowMate.EditorTools
{
    // El validador de slots del Editor (06a-escena-lago/02-tech-design §3.7; ARCHITECTURE §14.4; ASSET_SLOTS §1; RA-17): RowMate/Slots/
    // Validar recorre los slots del mundo (SceneWorld.asset y el entorno del lago) y, por cada uno con la fuente Asset:
    //   - si la referencia se perdió (el .meta del asset de terceros cambió al reimportar el pack) y hay un archivo del tipo del slot en su
    //     ruta esperada (Assets/ThirdParty/...), lo vuelve a enlazar;
    //   - si el asset está, lo valida (tipo, tamaño, presupuesto: ValidateAssigned).
    // Los slots con la fuente Procedural (el valor por defecto) son válidos siempre y no se tocan. Escribe un resumen en la consola, sin
    // rutas de la PC (solo las del proyecto). Nunca cambia la fuente de un slot: eso lo decide el usuario en el Inspector.
    public static class SlotValidator
    {
        [MenuItem("RowMate/Slots/Validar")]
        public static void ValidateFromMenu()
        {
            var report = Run(out var problems);
            if (problems > 0)
            {
                Debug.LogWarning(report);
            }
            else
            {
                Debug.Log(report);
            }
        }

        // El resumen y la cantidad de slots con problemas (los que se pudieron re-enlazar no cuentan).
        public static string Run(out int problems)
        {
            problems = 0;
            var sb = new StringBuilder("[SLOT] validación de slots\n");
            var world = AssetDatabase.LoadAssetAtPath<SceneWorldDefinition>(SceneWorldDefinition.AssetPath);
            if (world == null)
            {
                problems++;
                sb.Append("  falta ").Append(SceneWorldDefinition.AssetPath).Append(" (correr RowMate/Bootstrap/Reaplicar)\n");
                return sb.ToString();
            }

            var seen = new HashSet<AssetSlotDefinition>();
            var slots = new List<AssetSlotDefinition>();
            foreach (var slot in world.Slots)
            {
                if (slot != null && seen.Add(slot))
                {
                    slots.Add(slot);
                }
            }

            if (world.Lake != null)
            {
                foreach (var slot in world.Lake.Slots)
                {
                    if (slot != null && seen.Add(slot))
                    {
                        slots.Add(slot);
                    }
                }
            }

            // It. 8: los slots de río, costa y regata.
            foreach (var environment in world.OtherEnvironments)
            {
                if (environment == null)
                {
                    continue;
                }

                foreach (var slot in environment.Slots)
                {
                    if (slot != null && seen.Add(slot))
                    {
                        slots.Add(slot);
                    }
                }
            }

            var procedural = 0;
            foreach (var slot in slots)
            {
                if (slot.Source == SlotSource.Procedural)
                {
                    procedural++;
                    continue;
                }

                if (slot.AssignedObject == null)
                {
                    var relinked = string.IsNullOrEmpty(slot.ExpectedPath) ? null : AssetDatabase.LoadAssetAtPath(slot.ExpectedPath, slot.AssetType);
                    if (relinked != null && slot.AssignObject(relinked))
                    {
                        EditorUtility.SetDirty(slot);
                        sb.Append("  ").Append(slot.SlotId).Append(": re-enlazado a ").Append(slot.ExpectedPath).Append('\n');
                    }
                    else
                    {
                        problems++;
                        sb.Append("  ").Append(slot.SlotId).Append(": fuente Asset sin asset (ruta esperada ").Append(slot.ExpectedPath)
                            .Append("); la escena usa el procedural con un aviso [SLOT]\n");
                        continue;
                    }
                }

                if (!slot.ValidateAssigned(out var reason))
                {
                    problems++;
                    sb.Append("  ").Append(slot.SlotId).Append(": ").Append(reason).Append('\n');
                }
                else
                {
                    sb.Append("  ").Append(slot.SlotId).Append(": asset válido\n");
                }
            }

            // It. 7 (DT7-13): el remero no se asigna en el slot (queda en Procedural): se informa el binding y su validación.
            sb.Append("  ").Append(RowerReport()).Append('\n');
            AssetDatabase.SaveAssets();
            sb.Append("  ").Append(slots.Count).Append(" slots: ").Append(procedural).Append(" procedurales, ").Append(problems).Append(" con problemas\n");
            return sb.ToString();
        }

        // rower.character: el binding (Bootstrap/Generated, ignorado) y lo que mide su modelo, o el maniquí.
        public static string RowerReport()
        {
            var binding = AssetDatabase.LoadAssetAtPath<RowerCharacterBinding>(RowerCharacterBinding.AssetPath);
            if (binding == null)
            {
                return "rower.character: sin binding (ningún FBX de Mixamo en las rutas esperadas): el maniquí";
            }

            if (binding.Model == null)
            {
                return "rower.character: el binding apunta a " + binding.SourcePath + " y el FBX no está: el maniquí con un aviso [SLOT]";
            }

            var ok = RowerCharacterValidation.Validate(binding.Model, out var reason, out var stats);
            var measures = "Humanoid " + stats.Humanoid + ", dedos " + stats.FingerBones + ", alto " + stats.HeightM.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
                           + " m, " + stats.Triangles + " triángulos, " + stats.Bones + " huesos, legible " + stats.Readable;
            return "rower.character: " + binding.SourcePath + (ok ? " válido (" : " NO valida: " + reason + " (") + measures + ")";
        }
    }
}
