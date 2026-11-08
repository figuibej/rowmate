using System;
using System.Text;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.Ble
{
    // La cadencia real del monitor (QA-V4a-L01, D-ANI-06; 10a 02-tech-design §7): con cada paquete de Rower Data guarda, sin
    // asignar, el intervalo entre paquetes y entre cambios de la distancia (en cubetas < 0,75 s, 0,75-1,5, 1,5-2,5 y > 2,5, y
    // los últimos Capacity para los percentiles), la velocidad máxima entre cambios (Δdist/Δt) y cada cambio del contador de
    // paladas con su hora y la potencia del paquete. Report() arma la línea del registro ([BLE] Cadencia: …), que RowerService
    // escribe cada 60 s y al desconectar. Va al Player.log del usuario (no se versiona). Hilo principal.
    public sealed class BleCadence
    {
        public const int Capacity = 256;
        public const int StrokeLog = 8;
        public static readonly double[] BucketEdges = { 0.75, 1.5, 2.5 };

        private readonly double[] _packetIntervals = new double[Capacity];
        private readonly double[] _distanceIntervals = new double[Capacity];
        private readonly double[] _scratch = new double[Capacity];
        private readonly int[] _packetBuckets = new int[4];
        private readonly int[] _distanceBuckets = new int[4];
        private readonly double[] _strokeTimes = new double[StrokeLog];
        private readonly int[] _strokePowers = new int[StrokeLog];
        private int _packetIntervalCount;
        private int _distanceIntervalCount;
        private double _lastPacketAt = double.NaN;
        private double _lastDistanceAt = double.NaN;
        private int _lastDistance;
        private int _lastStrokes = -1;
        private int _strokeChanges;

        public int Packets { get; private set; }

        public int StrokeChanges => _strokeChanges;

        // m/s entre dos cambios de la distancia.
        public double MaxSpeed { get; private set; }

        public int PacketBucket(int index)
        {
            return _packetBuckets[index];
        }

        public int DistanceBucket(int index)
        {
            return _distanceBuckets[index];
        }

        public void Reset()
        {
            Packets = 0;
            MaxSpeed = 0;
            _packetIntervalCount = 0;
            _distanceIntervalCount = 0;
            _lastPacketAt = double.NaN;
            _lastDistanceAt = double.NaN;
            _lastDistance = 0;
            _lastStrokes = -1;
            _strokeChanges = 0;
            Array.Clear(_packetBuckets, 0, _packetBuckets.Length);
            Array.Clear(_distanceBuckets, 0, _distanceBuckets.Length);
        }

        // Un paquete de 0x2AD1 ya decodificado, con su hora (BleNotification.MonotonicSeconds).
        public void Add(double t, RowingData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Packets++;
            if (!double.IsNaN(_lastPacketAt))
            {
                Push(_packetIntervals, ref _packetIntervalCount, _packetBuckets, t - _lastPacketAt);
            }

            _lastPacketAt = t;
            if (double.IsNaN(_lastDistanceAt))
            {
                _lastDistanceAt = t;
                _lastDistance = data.DistanceMeters;
            }
            else if (data.DistanceMeters != _lastDistance)
            {
                var dt = t - _lastDistanceAt;
                Push(_distanceIntervals, ref _distanceIntervalCount, _distanceBuckets, dt);
                if (dt > 0)
                {
                    var speed = (data.DistanceMeters - _lastDistance) / dt;
                    if (speed > MaxSpeed)
                    {
                        MaxSpeed = speed;
                    }
                }

                _lastDistanceAt = t;
                _lastDistance = data.DistanceMeters;
            }

            if (_lastStrokes >= 0 && data.StrokeCount != _lastStrokes)
            {
                var slot = _strokeChanges % StrokeLog;
                _strokeTimes[slot] = t;
                _strokePowers[slot] = data.PowerWatts;
                _strokeChanges++;
            }

            _lastStrokes = data.StrokeCount;
        }

        // El percentil p (0..1) de los últimos intervalos entre paquetes, o NaN sin datos.
        public double PacketPercentile(double p)
        {
            return Percentile(_packetIntervals, _packetIntervalCount, p);
        }

        public double DistancePercentile(double p)
        {
            return Percentile(_distanceIntervals, _distanceIntervalCount, p);
        }

        // "Cadencia: paquetes=N intervalo p50=… p95=… [a/b/c/d] distancia cada p50=… p95=… [a/b/c/d] maxVel=… paladas=N (t W, …)".
        public string Report()
        {
            var sb = new StringBuilder(160);
            sb.Append("Cadencia: paquetes=").Append(Fmt.Integer(Packets));
            sb.Append(" intervalo p50=").Append(Seconds(PacketPercentile(0.5))).Append(" p95=").Append(Seconds(PacketPercentile(0.95)));
            AppendBuckets(sb, _packetBuckets);
            sb.Append(" distancia cada p50=").Append(Seconds(DistancePercentile(0.5))).Append(" p95=").Append(Seconds(DistancePercentile(0.95)));
            AppendBuckets(sb, _distanceBuckets);
            sb.Append(" maxVel=").Append(Fmt.Fixed(MaxSpeed, 2)).Append("m/s");
            sb.Append(" paladas=").Append(Fmt.Integer(_strokeChanges));
            var shown = Math.Min(_strokeChanges, StrokeLog);
            if (shown > 0)
            {
                sb.Append(" (");
                for (var i = 0; i < shown; i++)
                {
                    var slot = (_strokeChanges - shown + i) % StrokeLog;
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(Fmt.Fixed(_strokeTimes[slot], 2)).Append("s ").Append(Fmt.Integer(_strokePowers[slot])).Append('W');
                }

                sb.Append(')');
            }

            return sb.ToString();
        }

        private static void Push(double[] ring, ref int count, int[] buckets, double interval)
        {
            ring[count % Capacity] = interval;
            count++;
            var bucket = 0;
            while (bucket < BucketEdges.Length && interval >= BucketEdges[bucket])
            {
                bucket++;
            }

            buckets[bucket]++;
        }

        private double Percentile(double[] ring, int count, double p)
        {
            var n = Math.Min(count, Capacity);
            if (n == 0)
            {
                return double.NaN;
            }

            Array.Copy(ring, _scratch, n);
            Array.Sort(_scratch, 0, n);
            var index = (int)Math.Ceiling(p * n) - 1;
            if (index < 0)
            {
                index = 0;
            }

            return _scratch[index < n ? index : n - 1];
        }

        private static string Seconds(double value)
        {
            return double.IsNaN(value) ? "-" : Fmt.Fixed(value, 2) + "s";
        }

        private static void AppendBuckets(StringBuilder sb, int[] buckets)
        {
            sb.Append(" [");
            for (var i = 0; i < buckets.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append('/');
                }

                sb.Append(Fmt.Integer(buckets[i]));
            }

            sb.Append(']');
        }
    }
}
