using System;

namespace RowMate.Core.Scene
{
    // Un objeto de la orilla en coordenadas de mundo, metros absolutos (scene/environment.dart:18-37): X lateral (negativo =
    // orilla izquierda), Z = distancia desde el inicio del recorrido, Scale (1 por defecto) y Seed (variación de forma y color;
    // en distanceMarker, los metros a mostrar).
    public readonly struct ShoreProp : IEquatable<ShoreProp>
    {
        public ShoreProp(double x, double z, PropKind kind, double scale = 1.0, int seed = 0)
        {
            X = x;
            Z = z;
            Kind = kind;
            Scale = scale;
            Seed = seed;
        }

        public double X { get; }

        public double Z { get; }

        public PropKind Kind { get; }

        public double Scale { get; }

        public int Seed { get; }

        // Igualdad bit a bit (la de los segmentos contra el oráculo).
        public bool Equals(ShoreProp other)
        {
            return Kind == other.Kind && Seed == other.Seed
                && BitConverter.DoubleToInt64Bits(X) == BitConverter.DoubleToInt64Bits(other.X)
                && BitConverter.DoubleToInt64Bits(Z) == BitConverter.DoubleToInt64Bits(other.Z)
                && BitConverter.DoubleToInt64Bits(Scale) == BitConverter.DoubleToInt64Bits(other.Scale);
        }

        public override bool Equals(object? obj)
        {
            return obj is ShoreProp other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var h = (int)Kind;
                h = h * 31 + Seed;
                h = h * 31 + BitConverter.DoubleToInt64Bits(X).GetHashCode();
                h = h * 31 + BitConverter.DoubleToInt64Bits(Z).GetHashCode();
                return h * 31 + BitConverter.DoubleToInt64Bits(Scale).GetHashCode();
            }
        }

        public override string ToString()
        {
            return Kind + "(" + X.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "; "
                + Z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "; "
                + Scale.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "; " + Seed + ")";
        }
    }
}
