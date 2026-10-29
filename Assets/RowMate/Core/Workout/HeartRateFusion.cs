using System;
using RowMate.Core.Ble;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Fusión del pulso (F-WKT-05 + F-HRM-14; LT-35 a LT-38): el pulsómetro manda sobre el pulso que (rara vez) manda el
    // monitor por FTMS. Anda en todas las fases. Todo lo que sigue usa Data: la tarjeta BPM y la columna heart_rate de los
    // puntos (AC-WKT-05-05). legacy: lib/features/workout/workout_provider.dart:68-70, :220-237.
    public sealed class HeartRateFusion
    {
        // Último bpm del pulsómetro. null = sensor no conectado (manda el FTMS); 0 = conectado sin lectura válida.
        public int? SensorBpm { get; private set; }

        // La lectura fusionada (arranca vacía, RowingData() del legacy).
        public RowingData Data { get; private set; } = RowingData.Empty;

        // :220-224: d con el pulso del sensor si hay, si no el suyo.
        public void OnRowerReading(RowingData reading)
        {
            if (reading == null)
            {
                throw new ArgumentNullException(nameof(reading));
            }

            Data = SensorBpm.HasValue ? reading.WithHeartRate(SensorBpm.Value) : reading;
        }

        // :226-230: el bpm llega aunque el remo no mande nada.
        public void OnSensorBpm(int bpm)
        {
            SensorBpm = bpm;
            Data = Data.WithHeartRate(bpm);
        }

        // :232-237: conectado, o sin bpm previo → false (nada cambia y no se avisa); si no, el sensor se va (pulso 0) → true.
        public bool OnSensorStatus(HrmStatus status)
        {
            if (status == HrmStatus.Connected || !SensorBpm.HasValue)
            {
                return false;
            }

            SensorBpm = null;
            Data = Data.WithHeartRate(0);
            return true;
        }
    }
}
