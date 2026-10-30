using System;

namespace RowMate.Core.Design
{
    // Los tres modos de la fila de información (04-design-notes §3.3; DN4b-16): normal 6 · 8 · 10 con Próximo 7 × 3; compacto 1
    // con el aire antes de la cuenta y el de la cuenta al reloj en 6; compacto 2, además, Próximo con 4 × 3.
    public enum InfoRowMode
    {
        Normal,
        Compact1,
        Compact2,
    }

    // El plan de la fila para un paso (02-tech-design §2.15 de la It. 4b): los aires, el relleno de Próximo y cuántos fragmentos
    // enteros de la pastilla se ven sin Próximo y con Próximo. Vale para todo el paso: cuando entra Próximo solo cambia la
    // pastilla (nada salta, DV-46).
    public readonly struct InfoRowPlan
    {
        public InfoRowPlan(InfoRowMode mode, int fragmentsAlone, int fragmentsWithUpNext)
        {
            Mode = mode;
            FragmentsAlone = fragmentsAlone;
            FragmentsWithUpNext = fragmentsWithUpNext;
        }

        public InfoRowMode Mode { get; }

        // El aire antes de la cuenta (a Próximo o a la pastilla): 8 → 6 (immersive_workout_screen.dart:452).
        public double CountdownGap => Mode == InfoRowMode.Normal ? InfoRowFit.CountdownGap : InfoRowFit.CompactGap;

        // De la cuenta al reloj: 10 → 6 (:473).
        public double ClockGap => Mode == InfoRowMode.Normal ? InfoRowFit.ClockGap : InfoRowFit.CompactGap;

        // El relleno de Próximo por lado: 7 → 4.
        public double UpNextPadH => Mode == InfoRowMode.Compact2 ? InfoRowFit.UpNextCompactPadH : InfoRowFit.UpNextPadH;

        // Los fragmentos de la pastilla mientras no hay Próximo (0 = sin pastilla).
        public int FragmentsAlone { get; }

        // Con Próximo; 0 con FragmentsAlone > 0: la pastilla funde y se va al entrar Próximo (DN4b-16).
        public int FragmentsWithUpNext { get; }

        public override string ToString()
        {
            return Mode + " " + FragmentsAlone + "·" + FragmentsWithUpNext;
        }
    }

    // Qué cede en la fila de información y en la barra libre (02-tech-design §2.15 de la It. 4b; 04-design-notes §3.3, §3.5;
    // DN4b-16, -27; DV-45, DV-47; AC-WKT-14-03, -04, -14, -15, AC-WKT-16-06). El legacy reparte de otra forma: la pastilla es un
    // Flexible que recibe a lo sumo la mitad del espacio libre, se corta por carácter y nunca se oculta
    // (immersive_workout_screen.dart:394-416; DV-45). Pura: la vista mide los anchos (WP-B1, MeasureTextSize, solo con panel) y la
    // regla decide; dotnet la prueba con los anchos de las notas. Todo en dp base.
    public static class InfoRowFit
    {
        // :395, :452, :473; DN4b-16.
        public const double TypeGap = 6, CountdownGap = 8, ClockGap = 10, CompactGap = 6;

        // 1 de margen por el redondeo a píxeles.
        public const double UpNextPadH = 7, UpNextCompactPadH = 4, FitMarginDp = 1;

        // DN4b-27 (spec AC-WKT-16-06): el título libre se achica hasta 0,8 antes de recortarse con "…".
        public const double FreeTitleMinScale = 0.8;

        // El aire mínimo entre el título libre y el reloj (el "espacio flexible" de AC-WKT-16-02; 0 en 04-design-notes §3.5). B-14
        // le había puesto 6 (CompactGap) con el chip angosto de QA-V4b-02; con el chip a su ancho (101,8) y 6, en TV en español el
        // título no entraba a 0,8 y se recortaba. Con 2 entra entero a ≈ 0,805; a la vista quedan ≈ 5 (el ícono del reloj y el lado
        // de la última letra suman ≈ 3).
        public const double FreeTitleClockGap = 2;

        // Se decide al empezar el paso y cuando cambian la ventana, el idioma o la letra (nunca dentro del paso).
        // available: el ancho útil de la fila (la barra − el borde − 12 de cada lado: TV 339,9). typeWidth: el tipo del paso.
        // upNextWidth: Próximo con su tipo y el relleno normal (0 si el paso no lo va a tener: sin paso siguiente o sin restante
        // por tiempo, AC-WKT-04-05). countdownWidth: la cuenta como se ve en los últimos 15 s (M:SS). clockWidth: el reloj de
        // pared con su ícono. targetPrefix[k]: la pastilla con sus k + 1 primeros fragmentos, con el "…" si queda alguno afuera,
        // y su relleno de 6 × 2 (los arma la vista una vez por paso; sin asignar). fragmentCount: los fragmentos (0 sin pastilla).
        //   1. el modo es el primero en el que entra todo con la pastilla en su mínimo (el primer fragmento, o entera si tiene
        //      uno), junto a Próximo si el paso lo va a tener; si en ninguno entra, el primero en el que entra el resto (y si ni
        //      así, compacto 2: lo que nunca se corta no se corta y la vista lo anota);
        //   2. con ese modo, cuántos fragmentos ENTEROS entran sin Próximo y con Próximo.
        public static InfoRowPlan Plan(double available, double typeWidth, double upNextWidth, double countdownWidth, double clockWidth,
                                       double[]? targetPrefix, int fragmentCount)
        {
            if (fragmentCount > 0 && (targetPrefix == null || targetPrefix.Length < fragmentCount))
            {
                throw new ArgumentException("InfoRowFit: faltan los anchos de la pastilla.", nameof(targetPrefix));
            }

            var hasUpNext = upNextWidth > 0;
            var mode = InfoRowMode.Compact2;
            var found = false;
            if (fragmentCount > 0)
            {
                for (var m = InfoRowMode.Normal; m <= InfoRowMode.Compact2; m++)
                {
                    if (Fits(available, Width(m, typeWidth, upNextWidth, hasUpNext, countdownWidth, clockWidth, targetPrefix![0])))
                    {
                        mode = m;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                for (var m = InfoRowMode.Normal; m <= InfoRowMode.Compact2; m++)
                {
                    if (Fits(available, Width(m, typeWidth, upNextWidth, hasUpNext, countdownWidth, clockWidth, 0)))
                    {
                        mode = m;
                        break;
                    }
                }
            }

            var alone = WholeFragments(mode, available, typeWidth, 0, false, countdownWidth, clockWidth, targetPrefix, fragmentCount);
            var withUpNext = hasUpNext
                ? WholeFragments(mode, available, typeWidth, upNextWidth, true, countdownWidth, clockWidth, targetPrefix, fragmentCount)
                : alone;
            return new InfoRowPlan(mode, alone, withUpNext);
        }

        // El ancho de la fila en un modo, con la pastilla de ese ancho (0 = sin pastilla, sin su aire de 6) y Próximo si lo tiene.
        public static double Width(InfoRowMode mode, double typeWidth, double upNextWidth, bool hasUpNext, double countdownWidth, double clockWidth,
                                   double targetWidth)
        {
            var plan = new InfoRowPlan(mode, 0, 0);
            var upNext = hasUpNext ? upNextWidth - 2 * (UpNextPadH - plan.UpNextPadH) : 0;
            return typeWidth + (targetWidth > 0 ? TypeGap + targetWidth : 0) + upNext + plan.CountdownGap + countdownWidth + plan.ClockGap
                + clockWidth;
        }

        // La barra libre (AC-WKT-16-06; DN4b-27): la escala pareja del título para que entre, en [0,8; 1]; con menos de 0,8 queda
        // en 0,8 y el título se recorta con "…". TV y 360 en español: 127,0 / 155,4 ≈ 0,818 (04-design-notes §3.5).
        // othersWidth: el ícono, su aire, el reloj, el aire y el chip.
        public static double FreeTitleScale(double available, double titleWidth, double othersWidth)
        {
            if (titleWidth <= 0)
            {
                return 1;
            }

            var scale = (available - othersWidth) / titleWidth;
            return scale >= 1 ? 1 : scale <= FreeTitleMinScale ? FreeTitleMinScale : scale;
        }

        // Con la escala mínima el título igual no entra: se recorta con "…" (por debajo de 0,8).
        public static bool FreeTitleTruncates(double available, double titleWidth, double othersWidth)
        {
            return titleWidth > 0 && (available - othersWidth) / titleWidth < FreeTitleMinScale;
        }

        private static bool Fits(double available, double width)
        {
            return width + FitMarginDp <= available + 1e-9;
        }

        private static int WholeFragments(InfoRowMode mode, double available, double typeWidth, double upNextWidth, bool hasUpNext,
                                          double countdownWidth, double clockWidth, double[]? targetPrefix, int fragmentCount)
        {
            for (var k = fragmentCount; k >= 1; k--)
            {
                if (Fits(available, Width(mode, typeWidth, upNextWidth, hasUpNext, countdownWidth, clockWidth, targetPrefix![k - 1])))
                {
                    return k;
                }
            }

            return 0;
        }
    }
}
