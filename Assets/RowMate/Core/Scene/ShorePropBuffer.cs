using System;

namespace RowMate.Core.Scene
{
    // Un búfer de objetos de la orilla con capacidad fija, reservado una vez (06a-escena-lago/02-tech-design §2.11): los
    // segmentos de ShoreSegments, la lista visible y los rellenos lo reutilizan sin asignar. Agregar más allá de la capacidad
    // es un error de programación (InvalidOperationException), nunca un recorte silencioso.
    public sealed class ShorePropBuffer
    {
        private readonly ShoreProp[] _items;

        public ShorePropBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "ShorePropBuffer: capacidad positiva.");
            }

            _items = new ShoreProp[capacity];
        }

        public int Count { get; private set; }

        public int Capacity => _items.Length;

        public ShoreProp this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _items[index];
            }
        }

        public ReadOnlySpan<ShoreProp> Items => new ReadOnlySpan<ShoreProp>(_items, 0, Count);

        public void Clear()
        {
            Count = 0;
        }

        public void Add(in ShoreProp prop)
        {
            if (Count >= _items.Length)
            {
                throw new InvalidOperationException("ShorePropBuffer: se superó la capacidad de " + _items.Length + " objetos.");
            }

            _items[Count++] = prop;
        }

        // Para el ordenamiento en el lugar de ShoreSegments.
        internal Span<ShoreProp> Writable => new Span<ShoreProp>(_items, 0, Count);
    }
}
