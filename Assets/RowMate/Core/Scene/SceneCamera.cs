using System;

namespace RowMate.Core.Scene
{
    // La cámara de persecución del legacy (scene/scene_camera.dart:6-46): proyecta coordenadas de mundo (metros: x lateral,
    // y altura sobre el agua, z hacia adelante desde la cámara) a la pantalla en dp con el origen ARRIBA a la izquierda. El
    // horizonte va al 42 % del alto si el ancho es mayor que el alto y al 40 % si no (un cuadrado cuenta como vertical); la
    // focal es el lado corto (AC-SCN-02-01).
    public readonly struct SceneCamera : IEquatable<SceneCamera>
    {
        public const double CamHeight = 2.2;   // :12
        public const double BoatZ = 9.5;       // :16
        public const double MinZ = 0.5;        // :19

        public SceneCamera(double width, double height)
        {
            Width = width;
            Height = height;
            HorizonY = height * (width > height ? 0.42 : 0.40);
            Focal = Math.Min(Math.Abs(width), Math.Abs(height));
        }

        public double Width { get; }

        public double Height { get; }

        public double HorizonY { get; }

        public double Focal { get; }

        public double CenterX => Width / 2;

        // El centro de la zona del bote: donde se proyecta el remero (0, 0, 9,5) con la cámara del contrato.
        public double RowerY => HorizonY + Focal * CamHeight / BoatZ;

        // :28-34. false = el null del legacy (detrás de la cámara o a menos de MinZ).
        public bool TryProject(double x, double y, double z, out double sx, out double sy)
        {
            if (z <= MinZ)
            {
                sx = 0;
                sy = 0;
                return false;
            }

            sx = CenterX + Focal * x / z;
            sy = HorizonY + Focal * (CamHeight - y) / z;
            return true;
        }

        // :37: dp por metro a la distancia z.
        public double ScaleAt(double z)
        {
            return Focal / z;
        }

        // :41-45: la inversa para puntos sobre el agua (y = 0); sy tiene que estar debajo del horizonte.
        public void UnprojectWater(double sx, double sy, out double x, out double z)
        {
            z = Focal * CamHeight / (sy - HorizonY);
            x = (sx - CenterX) * z / Focal;
        }

        public bool Equals(SceneCamera other)
        {
            return Width.Equals(other.Width) && Height.Equals(other.Height);
        }

        public override bool Equals(object? obj)
        {
            return obj is SceneCamera other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Width.GetHashCode() * 397 ^ Height.GetHashCode();
            }
        }
    }
}
