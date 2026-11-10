using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RowMate.Core.Accessibility
{
    public enum A11yEditKind
    {
        Keep,
        Insert,
        Move,
        Remove,
    }

    // Una operación sobre la lista de nodos de Unity (InsertNode / MoveNode / RemoveNode): Index es la posición en la lista ya
    // editada hasta ese paso (Remove: la posición desde donde se saca).
    public readonly struct A11yEdit
    {
        public A11yEdit(A11yEditKind kind, object key, int index)
        {
            Kind = kind;
            Key = key;
            Index = index;
        }

        public A11yEditKind Kind { get; }

        public object Key { get; }

        public int Index { get; }

        public override string ToString()
        {
            return Kind + "(" + Key + ", " + Index + ")";
        }
    }

    // La reconciliación del árbol del lector (It. 14, DT14-19, QA-G14-06): compara la lista anterior con la nueva por clave estable
    // (identidad del objeto, no Equals) y produce las operaciones en orden: primero los Remove (de atrás para adelante) y después,
    // posición por posición, Keep, Move o Insert. Aplicadas en orden sobre la lista anterior dan la nueva. Los que siguen
    // conservan su nodo (y el foco del lector). Sin cambios → solo Keep. Asigna solo ante un cambio estructural (lo llama el
    // puente cuando algo entró, salió o cambió de orden).
    public static class A11yReconciler
    {
        public static void Diff(IReadOnlyList<object> previous, IReadOnlyList<object> next, List<A11yEdit> edits)
        {
            if (previous == null || next == null || edits == null)
            {
                throw new ArgumentNullException(previous == null ? nameof(previous) : next == null ? nameof(next) : nameof(edits));
            }

            edits.Clear();
            var wanted = new HashSet<object>(next, ReferenceComparer.Instance);
            for (var i = previous.Count - 1; i >= 0; i--)
            {
                if (!wanted.Contains(previous[i]))
                {
                    edits.Add(new A11yEdit(A11yEditKind.Remove, previous[i], i));
                }
            }

            var working = new List<object>(previous.Count);
            for (var i = 0; i < previous.Count; i++)
            {
                if (wanted.Contains(previous[i]))
                {
                    working.Add(previous[i]);
                }
            }

            var present = new HashSet<object>(working, ReferenceComparer.Instance);
            for (var i = 0; i < next.Count; i++)
            {
                var key = next[i];
                if (!present.Contains(key))
                {
                    working.Insert(i, key);
                    present.Add(key);
                    edits.Add(new A11yEdit(A11yEditKind.Insert, key, i));
                    continue;
                }

                if (ReferenceEquals(working[i], key))
                {
                    edits.Add(new A11yEdit(A11yEditKind.Keep, key, i));
                    continue;
                }

                var from = IndexOf(working, key, i);
                working.RemoveAt(from);
                working.Insert(i, key);
                edits.Add(new A11yEdit(A11yEditKind.Move, key, i));
            }
        }

        // ¿Hay algo más que Keep?
        public static bool HasChanges(IReadOnlyList<A11yEdit> edits)
        {
            if (edits == null)
            {
                throw new ArgumentNullException(nameof(edits));
            }

            for (var i = 0; i < edits.Count; i++)
            {
                if (edits[i].Kind != A11yEditKind.Keep)
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexOf(List<object> list, object key, int start)
        {
            for (var i = start; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], key))
                {
                    return i;
                }
            }

            throw new InvalidOperationException("A11yReconciler: clave perdida.");
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object? x, object? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
