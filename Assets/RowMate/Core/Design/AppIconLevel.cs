using System;

namespace RowMate.Core.Design
{
    // Nivel de detalle del ícono por tamaño FINAL en px (It. 14, 00-arte §1.4; DT14-04): nunca se achica un PNG grande.
    //   M  ≥ 49 px: todo (resplandor, dos olas, empuñaduras); marca a 0,70 del lienzo.
    //   S  25 a 48: sin resplandor, ola 2 ni empuñaduras; trazos más gruesos; marca a 0,80.
    //   XS ≤ 24 px: SOLO los dos remos, sin sol, sin olas y sin recorte entre ellos (la cruz en una pieza), para que a 16 px
    //      nada mida menos de 2 px (AC-APP-16-08); marca a 0,88.
    public enum AppIconLevel
    {
        M,
        S,
        XS,
    }

    public static class AppIconLevels
    {
        public const int MinM = 49;
        public const int MinS = 25;

        public static AppIconLevel For(int px)
        {
            if (px <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(px), px, "AppIconLevels: el tamaño tiene que ser positivo.");
            }

            return px >= MinM ? AppIconLevel.M : (px >= MinS ? AppIconLevel.S : AppIconLevel.XS);
        }

        // Lado de la caja de 100 de la marca sobre el lado del lienzo (placa, a sangre o disco).
        public static double MarkScale(AppIconLevel level)
        {
            switch (level)
            {
                case AppIconLevel.M:
                    return 0.70;
                case AppIconLevel.S:
                    return 0.80;
                default:
                    return 0.88;
            }
        }
    }
}
