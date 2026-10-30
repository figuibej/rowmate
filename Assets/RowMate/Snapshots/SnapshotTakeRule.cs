using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using RowMate.Tests.UiSupport;

namespace RowMate.Snapshots
{
    // La regla de toma de la It. 4b (04b-hud-exr/02-tech-design §5.2; DT4b-19; QA-G4bT-07): la toma es en el instante de la spec y
    // NO ESPERA: falla si algo de esta lista corre, en este orden:
    //   - un IMotion de la 4b (HudMotion) en una fase finita (cada uno dice IsFinite por fase, §7.4);
    //   - una animación de duración fija de la 4a (TimedMotion: fundidos, cruces de color, la entrada de Próximo) o cualquier otra
    //     finita (transiciones de página o de diálogo, la llave que cambia de carril) que no llegó a su final;
    //   - un rodillo de TabularNumber antes de su mitad (RollProgress < 0,5);
    //   - un resorte de la 4a a más de 0,15 dp de su destino (ISettlingMotion.RemainingDp).
    // Lo continuo (el halo, el pulso de la fila, el punto en vivo, el corazón) no bloquea y va en la fase fija de su instante.
    // InFlight deja en el manifest lo que corría, con su progreso.
    public static class SnapshotTakeRule
    {
        public const double RollHalf = 0.5;
        public const double SpringRemainingDp = 0.15;

        // Lo que bloquea la toma (vacío = se toma) y lo que corría (para el manifest).
        public static List<string> Blockers(UiTestHost host, List<string> inFlight)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var blockers = new List<string>();
            inFlight.Clear();
            var motions = new List<IMotion>();
            host.Handle.Motion.CopyMotions(motions);
            foreach (var motion in motions)
            {
                var name = Describe(motion);
                if (!motion.IsFinite)
                {
                    inFlight.Add(name + " (continua)");
                    continue;
                }

                if (motion is TabularNumber number)
                {
                    var progress = number.RollProgress;
                    inFlight.Add(name + " (rodillo " + progress.ToString("0.000", CultureInfo.InvariantCulture) + ")");
                    if (progress < RollHalf)
                    {
                        blockers.Add(name + ": rodillo antes de su mitad (" + progress.ToString("0.000", CultureInfo.InvariantCulture) + ")");
                    }

                    continue;
                }

                if (motion is ISettlingMotion spring)
                {
                    var remaining = spring.RemainingDp;
                    inFlight.Add(name + " (resorte, faltan " + remaining.ToString("0.000", CultureInfo.InvariantCulture) + " dp)");
                    if (remaining > SpringRemainingDp)
                    {
                        blockers.Add(name + ": resorte a " + remaining.ToString("0.000", CultureInfo.InvariantCulture) + " dp de su destino");
                    }

                    continue;
                }

                if (motion is AccentFlash flash && !UiQuery.IsShown(flash.Card))
                {
                    // Lo que no se ve no bloquea (el destello del tablero de Dispositivo, tapado por el inmersivo).
                    inFlight.Add(name + " (oculta)");
                    continue;
                }

                inFlight.Add(name + " (finita)");
                blockers.Add(name + (motion is HudMotion ? ": fase finita de la 4b" : ": animación finita sin terminar"));
            }

            return blockers;
        }

        private static string Describe(IMotion motion)
        {
            var name = motion.GetType().Name;
            for (var e = motion as UnityEngine.UIElements.VisualElement; e != null; e = e.hierarchy.parent)
            {
                if (!string.IsNullOrEmpty(e.name))
                {
                    name += "@" + e.name;
                    break;
                }
            }

            return name;
        }
    }
}
