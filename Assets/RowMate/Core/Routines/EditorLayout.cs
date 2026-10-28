using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Routines
{
    // Bloques de la lista del editor (legacy: _buildStepsList, lib/features/routines/routine_editor_screen.dart:275-295):
    // un paso suelto es un bloque; una serie (pasos CONSECUTIVOS con el mismo GroupId) es encabezado + sus pasos + pie.
    // - Una serie que se queda sin pasos desaparece entera: no hay pasos, no hay bloque (AC-RUT-14-10).
    // - Dos series seguidas son dos bloques, cada una con su encabezado y su pie (AC-RUT-14-11).
    // - Un paso suelto entre dos tramos con el mismo id parte la serie en dos bloques (como la expansión, AC-RUT-06-11);
    //   los dos muestran el ×N del primero con ese id, como el encabezado del legacy (DT-14).
    public static class EditorLayout
    {
        public static IReadOnlyList<EditorItem> Build(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var items = new List<EditorItem>(template.Count + 4);
            var ordinal = 0;
            var i = 0;
            while (i < template.Count)
            {
                var gid = template[i].GroupId;
                if (gid == null)
                {
                    items.Add(new EditorItem(EditorItemKind.LooseStep, i, null, -1, 1));
                    i++;
                    continue;
                }

                var start = i;
                var repeat = RoutineTemplate.RepeatCountOf(template, gid);
                items.Add(new EditorItem(EditorItemKind.SeriesHeader, start, gid, ordinal, repeat));
                while (i < template.Count && template[i].GroupId == gid)
                {
                    items.Add(new EditorItem(EditorItemKind.SeriesStep, i, gid, ordinal, repeat));
                    i++;
                }

                items.Add(new EditorItem(EditorItemKind.SeriesFooter, start, gid, ordinal, repeat));
                ordinal++;
            }

            return items;
        }
    }
}
