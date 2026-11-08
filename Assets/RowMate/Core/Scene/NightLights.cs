using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene
{
    // Las luces de la noche (06a-escena-lago/02-tech-design §2.7; AC-SCN-09-06, AC-SCN-20-02; DV-60). El legacy las enciende todas
    // juntas con ambiente < 0,6 (shore_painter.dart:26): de ≈ 19:26 a ≈ 4:43. Acá cada luz (su semilla: la k de la cabaña) cambia
    // en su propio minuto, fijo, dentro de ±10 min del umbral (19:16 a 19:35 al anochecer y 4:33 a 4:52 al amanecer); fuera de
    // esa ventana, el estado del legacy.
    public static class NightLights
    {
        public const double WindowMinutes = 10;

        // shore_painter.dart:26
        public static bool IsNightLegacy(double ambient)
        {
            return ambient < 0.6;
        }

        // Las horas en que el ambiente de la paleta cruza 0,6: entre el atardecer (18 h, 0,85) y el anochecer (20 h, 0,5), y entre
        // la noche cerrada (4 h, 0,35) y el amanecer (5 h, 0,7) (time_of_day.dart:94-98).
        public static readonly double DuskThresholdHour = Cross(SceneTimeOfDay.Sunset.Ambient, SceneTimeOfDay.Dusk.Ambient, 18, 20);
        public static readonly double DawnThresholdHour = Cross(SceneTimeOfDay.Night.Ambient, SceneTimeOfDay.Dawn.Ambient, 4, 5);

        // El minuto (desde las 0 h) en que se enciende la luz al anochecer y en que se apaga al amanecer: el minuto entero del
        // umbral más un desfase propio de −9 a +10 min.
        public static int DuskMinute(int lightSeed)
        {
            return (int)Math.Floor(DuskThresholdHour * 60) + Offset(lightSeed);
        }

        public static int DawnMinute(int lightSeed)
        {
            return (int)Math.Floor(DawnThresholdHour * 60) + Offset(lightSeed);
        }

        // ¿Encendida? hour es la hora visible de la escena y ambient el de su paleta.
        public static bool IsLit(int lightSeed, double hour, double ambient)
        {
            var h = hour % 24;
            if (h < 0)
            {
                h += 24;
            }

            var window = WindowMinutes / 60;
            if (Math.Abs(h - DuskThresholdHour) <= window)
            {
                return h * 60 >= DuskMinute(lightSeed);
            }

            if (Math.Abs(h - DawnThresholdHour) <= window)
            {
                return h * 60 < DawnMinute(lightSeed);
            }

            return IsNightLegacy(ambient);
        }

        // It. 8 (02-tech-design §2.7; AC-SCN-20-04): la misma regla por tipo de luz. La cabaña conserva su minuto de la 6a; los
        // demás tipos mezclan el tipo en la sal (dos luces de distinto tipo con la misma semilla no se encienden juntas).
        public static int DuskMinute(NightLightKind kind, int lightSeed)
        {
            return (int)Math.Floor(DuskThresholdHour * 60) + Offset(kind, lightSeed);
        }

        public static int DawnMinute(NightLightKind kind, int lightSeed)
        {
            return (int)Math.Floor(DawnThresholdHour * 60) + Offset(kind, lightSeed);
        }

        public static bool IsLit(NightLightKind kind, int lightSeed, double hour, double ambient)
        {
            if (kind == NightLightKind.Cabin)
            {
                return IsLit(lightSeed, hour, ambient);
            }

            var h = hour % 24;
            if (h < 0)
            {
                h += 24;
            }

            var window = WindowMinutes / 60;
            if (Math.Abs(h - DuskThresholdHour) <= window)
            {
                return h * 60 >= DuskMinute(kind, lightSeed);
            }

            if (Math.Abs(h - DawnThresholdHour) <= window)
            {
                return h * 60 < DawnMinute(kind, lightSeed);
            }

            return IsNightLegacy(ambient);
        }

        // El tipo y la semilla de la luz de un objeto de la orilla (false si no se enciende): la cabaña con su k, edificios y clubes
        // con su variante, faroles por su z y su lado (todos tienen variante 0 en el legacy), faro y torre por su z.
        public static bool TryKindOf(in ShoreProp prop, out NightLightKind kind, out int seed)
        {
            switch (prop.Kind)
            {
                case PropKind.Cabin:
                    kind = NightLightKind.Cabin;
                    seed = prop.Seed;
                    return true;
                case PropKind.Building:
                    kind = NightLightKind.BuildingWindows;
                    seed = prop.Seed;
                    return true;
                case PropKind.Boathouse:
                    kind = NightLightKind.BoathouseGate;
                    seed = prop.Seed;
                    return true;
                case PropKind.Lamp:
                    kind = NightLightKind.Lamp;
                    seed = unchecked((int)(prop.Z * 2) * 2 + (prop.X < 0 ? 0 : 1));
                    return true;
                case PropKind.Lighthouse:
                    kind = NightLightKind.Lighthouse;
                    seed = unchecked((int)prop.Z);
                    return true;
                case PropKind.FinishTower:
                    kind = NightLightKind.FinishTower;
                    seed = unchecked((int)prop.Z);
                    return true;
                default:
                    kind = NightLightKind.Cabin;
                    seed = 0;
                    return false;
            }
        }

        private static int Offset(int lightSeed)
        {
            return -9 + (int)Math.Floor(SceneHash.Unit(0x4E49474854L, lightSeed) * 20);
        }

        private static int Offset(NightLightKind kind, int lightSeed)
        {
            return kind == NightLightKind.Cabin ? Offset(lightSeed) : -9 + (int)Math.Floor(SceneHash.Unit(0x4E49474854L + 101L * (long)kind, lightSeed) * 20);
        }

        private static double Cross(double a0, double a1, double h0, double h1)
        {
            return h0 + (0.6 - a0) / (a1 - a0) * (h1 - h0);
        }
    }

    // Los tipos de luz de la noche (AC-SCN-20-04; D8-10): la semilla de cada una es la del objeto (la k de la cabaña, la variante
    // del edificio, el índice del bloque del skyline…).
    public enum NightLightKind
    {
        Cabin,
        BuildingWindows,
        Lamp,
        Lighthouse,
        SkylineWindows,
        FinishTower,
        BoathouseGate,
        CityBlock,
    }

    // El fundido de una luz hacia su estado: 0,4 s lineal en el tiempo real del cuadro (sigue con reducir movimiento: es un
    // fundido, AC-APP-05-08).
    public struct NightLightFader
    {
        public const double Seconds = MotionSpecs.NightLightFadeSeconds;

        public double Level { get; private set; }

        public void Step(bool lit, double dt)
        {
            var target = lit ? 1.0 : 0.0;
            var step = dt <= 0 ? 0 : dt / Seconds;
            Level = Level < target ? Math.Min(target, Level + step) : Math.Max(target, Level - step);
        }

        public void Snap(bool lit)
        {
            Level = lit ? 1 : 0;
        }
    }
}
