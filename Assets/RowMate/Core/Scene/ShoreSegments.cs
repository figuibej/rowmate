using System;

namespace RowMate.Core.Scene
{
    // La orilla determinista de una sesión de escena (scene/environment.dart:72-120; F-SCN-14; LT-75 a LT-77): la caché por
    // segmento, PropsForSegment, VisibleProps con el tope y la prioridad de los hitos, y el olvido de lo de atrás.
    // DT6a-15: la caché del legacy es estática y global; acá es de cada instancia (una por sesión): el resultado no cambia y
    // una sesión nueva no hereda nada. Sin asignaciones después de armarse: los segmentos viven en búferes de un pool (un
    // segmento olvidado devuelve el suyo) y el ordenamiento es por inserción sobre un arreglo propio.
    public sealed class ShoreSegments
    {
        private readonly ShoreEnvironment _environment;
        private readonly DartRandom _random = new DartRandom(0);
        private readonly long[] _keys;
        private readonly long[] _stamps;
        private readonly ShorePropBuffer[] _buffers;
        private readonly ShorePropBuffer _gather;
        private readonly ShorePropBuffer _landmarks;
        private readonly ShorePropBuffer _buoys;
        private long _clock;

        public ShoreSegments(ShoreEnvironment environment, int poolSize = 32)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            if (poolSize < 12)
            {
                throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "ShoreSegments: hacen falta al menos 12 segmentos en el pool.");
            }

            var perSegment = environment.MaxPropsPerSegment;
            _keys = new long[poolSize];
            _stamps = new long[poolSize];
            _buffers = new ShorePropBuffer[poolSize];
            for (var i = 0; i < poolSize; i++)
            {
                _keys[i] = -1;
                _buffers[i] = new ShorePropBuffer(perSegment);
            }

            // Lo visible abarca a lo sumo 400/50 + 2 segmentos.
            var gather = ((int)(ShoreEnvironment.VisibleRange / ShoreEnvironment.SegmentLength) + 2) * perSegment;
            _gather = new ShorePropBuffer(gather);
            _landmarks = new ShorePropBuffer(gather);
            _buoys = new ShorePropBuffer(gather);
        }

        public ShoreEnvironment Environment => _environment;

        // AC-SCN-14-06: cuántos segmentos hay en memoria (no crece con la distancia).
        public int CachedSegments
        {
            get
            {
                var n = 0;
                for (var i = 0; i < _keys.Length; i++)
                {
                    n += _keys[i] >= 0 ? 1 : 0;
                }

                return n;
            }
        }

        // :74-80: los objetos del segmento k (generados una vez y guardados). No hay segmentos negativos: vacío.
        public ReadOnlySpan<ShoreProp> PropsForSegment(long k)
        {
            if (k < 0)
            {
                return ReadOnlySpan<ShoreProp>.Empty;
            }

            _clock++;
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] == k)
                {
                    _stamps[i] = _clock;
                    return _buffers[i].Items;
                }
            }

            var slot = FreeSlot();
            var buffer = _buffers[slot];
            buffer.Clear();
            _random.Reseed(ShoreEnvironment.SeedFor(_environment.Id, k));
            _environment.Generate(k, _random, buffer);
            _keys[slot] = k;
            _stamps[slot] = _clock;
            return buffer.Items;
        }

        // :89-114: los objetos con z en [d + 1; d + 400], de lejos a cerca. Si superan el tope: se conservan los hitos (todo lo que
        // no es boya), se completa con las boyas más cercanas, si aún sobra se descartan los hitos más lejanos y se reordena.
        // Olvida los segmentos de atrás (:94, :116-120). Vacía `into` y devuelve cuántos dejó.
        public int VisibleProps(double distance, ShorePropBuffer into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            var zMin = distance + 1;
            var zMax = distance + ShoreEnvironment.VisibleRange;
            var first = Math.Max(0L, (long)Math.Floor(zMin / ShoreEnvironment.SegmentLength));
            var last = (long)Math.Floor(zMax / ShoreEnvironment.SegmentLength);
            EvictBehind(first - 1);
            _gather.Clear();
            for (var k = first; k <= last; k++)
            {
                var props = PropsForSegment(k);
                for (var i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (p.Z >= zMin && p.Z <= zMax)
                    {
                        _gather.Add(p);
                    }
                }
            }

            SortFarToNear(_gather.Writable);
            into.Clear();
            var max = _environment.MaxVisibleProps;
            if (_gather.Count <= max)
            {
                CopyTo(_gather, into);
                return into.Count;
            }

            _landmarks.Clear();
            _buoys.Clear();
            var all = _gather.Items;
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].Kind == PropKind.LaneBuoy)
                {
                    _buoys.Add(all[i]);
                }
                else
                {
                    _landmarks.Add(all[i]);
                }
            }

            var room = max - _landmarks.Count;
            if (room > 0)
            {
                // kept.addAll(buoys.sublist(buoys.length − room)): las boyas más cercanas (la lista va de lejos a cerca).
                var buoys = _buoys.Items;
                for (var i = buoys.Length - room; i < buoys.Length; i++)
                {
                    _landmarks.Add(buoys[i]);
                }

                CopyTo(_landmarks, into);
            }
            else
            {
                // kept.removeRange(0, −room): los hitos más lejanos.
                var landmarks = _landmarks.Items;
                for (var i = -room; i < landmarks.Length; i++)
                {
                    into.Add(landmarks[i]);
                }
            }

            SortFarToNear(into.Writable);
            return into.Count;
        }

        // It. 8 (DT8-08; AC-SCN-16-03): los puentes con z ∈ [d − behind; d + 1), los que ya salieron de la lista del legacy
        // (relZ < 1) pero siguen a la vista hasta pasar detrás de la cámara. No entran en la lista ni en el tope (LT-101 no cambia);
        // los dibuja ShoreStreamer sin tramado. Vacía `into` y devuelve cuántos dejó, de lejos a cerca. Sin asignar.
        public int TrailingLandmarks(double distance, double behind, ShorePropBuffer into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            into.Clear();
            var zMin = distance - behind;
            var zMax = distance + 1;
            var first = Math.Max(0L, (long)Math.Floor(zMin / ShoreEnvironment.SegmentLength));
            var last = (long)Math.Floor(zMax / ShoreEnvironment.SegmentLength);
            for (var k = first; k <= last; k++)
            {
                var props = PropsForSegment(k);
                for (var i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (p.Kind == PropKind.Bridge && p.Z >= zMin && p.Z < zMax && into.Count < into.Capacity)
                    {
                        into.Add(p);
                    }
                }
            }

            SortFarToNear(into.Writable);
            return into.Count;
        }

        // :116-120: olvida los segmentos anteriores a k.
        private void EvictBehind(long k)
        {
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] >= 0 && _keys[i] < k)
                {
                    _keys[i] = -1;
                    _buffers[i].Clear();
                }
            }
        }

        // Un lugar libre o, si no hay, el usado hace más tiempo (se regenera idéntico si vuelve a hacer falta).
        private int FreeSlot()
        {
            var oldest = 0;
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_keys[i] < 0)
                {
                    return i;
                }

                if (_stamps[i] < _stamps[oldest])
                {
                    oldest = i;
                }
            }

            return oldest;
        }

        private static void CopyTo(ShorePropBuffer from, ShorePropBuffer into)
        {
            var items = from.Items;
            for (var i = 0; i < items.Length; i++)
            {
                into.Add(items[i]);
            }
        }

        // Por inserción, de z decreciente, estable (casi ordenados: ≈ 100 objetos). Dart no garantiza estabilidad con más de 32
        // elementos (QA-G6aT-15 a): en el lago no hay empates de z; las comparaciones contra el oráculo ordenan cada tramo de z
        // iguales por (tipo, x, semilla) en los dos lados.
        private static void SortFarToNear(Span<ShoreProp> items)
        {
            for (var i = 1; i < items.Length; i++)
            {
                var current = items[i];
                var j = i - 1;
                while (j >= 0 && items[j].Z < current.Z)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = current;
            }
        }
    }
}
