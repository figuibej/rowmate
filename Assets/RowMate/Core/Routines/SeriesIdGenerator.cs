using System;
using System.Collections.Generic;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.Routines
{
    // Identificador de una serie nueva (legacy: 'g${DateTime.now().millisecondsSinceEpoch}_${_idCounter++}',
    // lib/features/routines/routine_editor_screen.dart:111): "g" + milisegundos desde 1970 + "_" + contador del editor.
    // Dos series en el mismo milisegundo tienen ids distintos por el contador (AC-RUT-08-06). Si el id ya existe en la
    // plantilla (una rutina guardada en otra sesión), sigue contando. El id no se ve en ninguna pantalla.
    // Reloj: el Scheduler de la app, nunca el CreationClock de las fechas de creación (QA-G3-19).
    public sealed class SeriesIdGenerator
    {
        private readonly IClock _clock;
        private int _counter;

        public SeriesIdGenerator(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public string Next(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var milliseconds = Fmt.Integer(_clock.UtcNow.ToUnixTimeMilliseconds());
            while (true)
            {
                var id = "g" + milliseconds + "_" + Fmt.Integer(_counter++);
                if (RoutineTemplate.FirstIndexOf(template, id) < 0)
                {
                    return id;
                }
            }
        }
    }
}
