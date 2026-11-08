using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion.History
{
    // El movimiento del Historial y del detalle (09-historial/02-tech-design §5.6; 00-arte-historial §1.4 y §2.1; ANIMATION_BIBLE
    // §2.3), sin tiempos nuevos (MotionSpecs y Tokens):
    // - Primera carga con la pestaña a la vista: tarjeta i en 35·i, hasta 8; cada una opacidad 0 → 1 e y +12 → 0 en 240
    //   ease.enter (EnterMotion).
    // - Sesión nueva arriba con la lista arriba de todo: crece su slot (GrowMotion: spring.smooth ω 12 y fundido 60-260 ease.enter).
    // - Detalle: si los datos llegan después del push, Resumen, Pasos, selector y gráfico entran escalonados cada 35 ms (lo mismo
    //   que una tarjeta); si llegan durante el push, entran con la página, sin animación propia (dos movimientos encimados se ven
    //   como un tirón). La línea de tiempo del gráfico NO está acá: es ChartTimeline (Core) y la corre SessionChartView; su t0 es
    //   el comienzo de la entrada de su sección.
    // Reducir movimiento: lo de EnterMotion y GrowMotion (fundido de 160 junto, sin desplazamiento, escalonado ni cambio de alto).
    // Sin MotionDriver armado (antes del primer cuadro, en las tomas) todo queda en su estado final.
    // Las vistas son dueñas de los movimientos (una EnterMotion o GrowMotion por pieza, reusadas): acá solo está la coreografía.
    // Contrato de §5.6 con un cambio (04-design-notes §3): reciben los movimientos de la vista en lugar del MotionDriver solo, y la
    // nueva arriba crece en su slot (el GrowMotion de Rutinas) en lugar de correr todo el contenido desde −k·alto.
    public static class HistoryMotion
    {
        // Cuántas tarjetas escalona la primera carga (las demás quedan fuera de la vista o entran sin animar).
        public static int FirstLoadCount(int cards)
        {
            return Math.Max(0, Math.Min(cards, MotionSpecs.ListStaggerMax));
        }

        // La tarjeta i de la primera carga.
        public static void PlayFirstLoadCard(EnterMotion motion, VisualElement card, int index)
        {
            if (motion == null)
            {
                throw new ArgumentNullException(nameof(motion));
            }

            motion.Play(card, MotionSpecs.ListStaggerMs * index, MotionSpecs.ListItemInMs, Tokens.ShiftM.Value);
        }

        // La primera carga entera: visibleCards[i] con motions[i] (los dos en el orden de la lista).
        public static void PlayFirstLoad(MotionDriver driver, IReadOnlyList<VisualElement> visibleCards, IReadOnlyList<EnterMotion> motions)
        {
            if (driver == null || !driver.Armed)
            {
                return;
            }

            var count = Math.Min(FirstLoadCount(visibleCards.Count), motions.Count);
            for (var i = 0; i < count; i++)
            {
                PlayFirstLoadCard(motions[i], visibleCards[i], i);
            }
        }

        // La sesión nueva arriba (la lista está arriba de todo, la pestaña a la vista y sin reducir movimiento: lo decide la lista).
        public static void PlayTopInsert(GrowMotion grow)
        {
            if (grow == null)
            {
                throw new ArgumentNullException(nameof(grow));
            }

            grow.Play();
        }

        // La cascada del detalle: sections en orden (Resumen, [Pasos], selector, gráfico). pool crece hasta tener una EnterMotion por
        // sección y se reusa en la próxima.
        public static void PlayDetailCascade(MotionDriver driver, IReadOnlyList<VisualElement> sections, List<EnterMotion> pool, bool duringPush)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            if (duringPush || !driver.Armed)
            {
                return;
            }

            while (pool.Count < sections.Count)
            {
                pool.Add(new EnterMotion(driver));
            }

            for (var i = 0; i < sections.Count; i++)
            {
                pool[i].Play(sections[i], MotionSpecs.ListStaggerMs * i, MotionSpecs.ListItemInMs, Tokens.ShiftM.Value);
            }
        }
    }
}
