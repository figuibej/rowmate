using System;

namespace RowMate.Core.Scene.Stroke
{
    // El IK de dos huesos (rodilla, codo) en Core (DT7-01): con la raíz, el objetivo, los dos largos y un punto guía (hint) del lado
    // al que se dobla la articulación. Si el objetivo no se alcanza, estira a lo largo de la recta (sin NaN); si queda demasiado
    // cerca, lo trata como el mínimo alcanzable.
    public static class TwoBoneIk
    {
        public static SceneVec3 Solve(in SceneVec3 root, in SceneVec3 target, double upper, double lower, in SceneVec3 hint)
        {
            var d = target - root;
            var dist = d.Length;
            var dir = d.Normalized(SceneVec3.UnitY);
            var min = Math.Abs(upper - lower) + 1e-9;
            var max = upper + lower - 1e-9;
            var reach = dist < min ? min : (dist > max ? max : dist);
            var a = (upper * upper - lower * lower + reach * reach) / (2 * reach);
            var h2 = upper * upper - a * a;
            var h = h2 > 0 ? Math.Sqrt(h2) : 0;

            // La dirección del doblez: la parte del hint perpendicular a la recta raíz → objetivo.
            var toHint = hint - root;
            var perp = toHint - dir * SceneVec3.Dot(toHint, dir);
            var bend = perp.Normalized(AnyPerpendicular(dir));
            return root + dir * a + bend * h;
        }

        private static SceneVec3 AnyPerpendicular(in SceneVec3 v)
        {
            var axis = Math.Abs(v.X) < 0.9 ? SceneVec3.UnitX : SceneVec3.UnitY;
            return SceneVec3.Cross(v, axis).Normalized(SceneVec3.UnitZ);
        }
    }
}
