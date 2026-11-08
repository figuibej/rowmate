using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;

namespace RowMate.Tests.Support
{
    // El permiso de Android falso (10a 02-tech-design §8.2): un estado, las respuestas guionadas del diálogo (sin guion, concede) y
    // lo que el usuario hizo en los ajustes mientras la app estaba atrás (lo ve el próximo Refresh). Cuenta pedidos, relecturas y
    // aperturas de los ajustes de la app.
    public sealed class FakeBlePermissions : IBlePermissions
    {
        private readonly Queue<BlePermissionState> _answers = new Queue<BlePermissionState>();
        private BlePermissionState? _external;

        public FakeBlePermissions(BlePermissionState initial = BlePermissionState.NotAsked)
        {
            State = initial;
        }

        public BlePermissionState State { get; set; }

        public int Requests { get; private set; }

        public int Refreshes { get; private set; }

        public int SettingsOpens { get; private set; }

        public void Answer(BlePermissionState answer)
        {
            _answers.Enqueue(answer);
        }

        // El usuario cambió el permiso en los ajustes: lo ve el próximo Refresh (al volver al frente).
        public void ChangeInSettings(BlePermissionState state)
        {
            _external = state;
        }

        public Task<BlePermissionState> RequestAsync()
        {
            Requests++;
            State = _answers.Count > 0 ? _answers.Dequeue() : BlePermissionState.Granted;
            return Task.FromResult(State);
        }

        public void Refresh()
        {
            Refreshes++;
            if (_external.HasValue)
            {
                State = _external.Value;
                _external = null;
            }
        }

        public void OpenAppSettings()
        {
            SettingsOpens++;
        }
    }
}
