namespace RowMate.Core.Design
{
    // Seno propio para el ícono (It. 14, DT14-01, QA-G14-05): el rasterizado no usa funciones trascendentes de la plataforma
    // (Math.Sin cambia entre libm de UCRT, glibc y Mono), así los PNG dan los mismos bytes en la PC, en el CI y en Unity.
    // Reducción a [−π; π] con Math.Floor, plegado a [−π/2; π/2] y la serie de Taylor impar hasta x^15 por Horner (solo + y *
    // en double: IEEE 754 fija el resultado bit a bit). Error < 1e-11 en todo el rango usado (|x| ≤ 100).
    // Desvío anotado: el diseño pedía un polinomio de grado 11; con Taylor, grado 11 deja ~6e-8 en π/2, así que llega a 15.
    public static class IconMath
    {
        public const double Pi = 3.141592653589793;
        public const double TwoPi = 6.283185307179586;
        public const double HalfPi = 1.5707963267948966;

        // Coeficientes 1/n! con el signo alternado (n = 3, 5, …, 15).
        private const double C3 = -1.0 / 6.0;
        private const double C5 = 1.0 / 120.0;
        private const double C7 = -1.0 / 5040.0;
        private const double C9 = 1.0 / 362880.0;
        private const double C11 = -1.0 / 39916800.0;
        private const double C13 = 1.0 / 6227020800.0;
        private const double C15 = -1.0 / 1307674368000.0;

        public static double Sin(double x)
        {
            // k = round(x / 2π) con Floor(+0,5): Math.Round está prohibido en Core (ADR 012).
            var k = System.Math.Floor(x / TwoPi + 0.5);
            var r = x - k * TwoPi;
            if (r > HalfPi)
            {
                r = Pi - r;
            }
            else if (r < -HalfPi)
            {
                r = -Pi - r;
            }

            var r2 = r * r;
            var p = C15;
            p = p * r2 + C13;
            p = p * r2 + C11;
            p = p * r2 + C9;
            p = p * r2 + C7;
            p = p * r2 + C5;
            p = p * r2 + C3;
            return r + r * r2 * p;
        }

        // smoothstep(0, 1, t) = t²(3 − 2t) con t recortado a [0; 1].
        public static double SmoothStep(double t)
        {
            if (t <= 0)
            {
                return 0;
            }

            if (t >= 1)
            {
                return 1;
            }

            return t * t * (3 - 2 * t);
        }

        public static double Clamp01(double t)
        {
            return t < 0 ? 0 : (t > 1 ? 1 : t);
        }
    }
}
