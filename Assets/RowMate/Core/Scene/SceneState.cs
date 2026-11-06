using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // Velocidad, distancia y tiempo de la escena (scene/scene_state.dart:13-57, la parte de la 6a; F-SCN-03). Puerto parcial a
    // propósito: la fase de palada, el cabeceo y los charcos (scene_state.dart:59-85) son de la It. 7. La distancia de escena es
    // propia (no la del monitor), en double (Q-68), y no toca nada fuera de la escena (AC-SCN-03-05).
    public sealed class SceneState
    {
        public const double SpeedTau = 1.5;       // scene_state.dart:16
        public const double MaxStep = 0.1;        // :52 (scene_view.dart:63)

        public double Speed { get; private set; }

        public double Distance { get; private set; }

        public double Time { get; private set; }

        // :33-34: la distancia acotada para el shader del legacy. Contrato (LT-68); el agua de la app no la usa (QA-P-28).
        public double ShaderDistance => DartMath.Mod(Distance, 4096.0);

        // :38-43: el parser repone el ritmo medio cuando el instantáneo es 0, así que la señal de remar es el SPM.
        public static double TargetSpeedFor(double pace500m, double spm, bool isActive)
        {
            return isActive && spm > 0 && pace500m > 0 ? 500 / pace500m : 0;
        }

        // :45-57: paso acotado a [0; 0,1], inercia con τ = 1,5 s, corte a 0 bajo 0,01 m/s y la distancia con la velocidad nueva.
        public void Update(double dt, double pace500m, double spm, bool isActive)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > MaxStep ? MaxStep : dt);
            Time += step;
            var target = TargetSpeedFor(pace500m, spm, isActive);
            Speed += (target - Speed) * Math.Min(1.0, step / SpeedTau);
            if (target == 0 && Speed < 0.01)
            {
                Speed = 0;
            }

            Distance += Speed * step;
        }

        // Vuelve a cero (al abrir una sesión de escena, AC-SCN-01-03).
        public void Reset()
        {
            Speed = 0;
            Distance = 0;
            Time = 0;
        }

        // E-0, E-920, E-1234, E-42K (§0.4 de la spec): solo las pruebas y el arnés (InternalsVisibleTo).
        internal void ForceForTest(double distance, double speed, double time)
        {
            Distance = distance;
            Speed = speed;
            Time = time;
        }
    }
}
