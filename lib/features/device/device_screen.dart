import 'package:flutter/material.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';
import 'package:provider/provider.dart';
import '../../core/bluetooth/ble_service.dart';
import 'device_provider.dart';

class DeviceScreen extends StatelessWidget {
  const DeviceScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Dispositivo'),
        actions: [
          Consumer<DeviceProvider>(
            builder: (_, p, __) => p.isConnected
                ? TextButton.icon(
                    onPressed: p.disconnect,
                    icon: const Icon(Icons.bluetooth_disabled, size: 18),
                    label: const Text('Desconectar'),
                    style: TextButton.styleFrom(foregroundColor: Colors.red.shade300),
                  )
                : const SizedBox.shrink(),
          ),
        ],
      ),
      body: Consumer<DeviceProvider>(
        builder: (context, p, _) {
          if (p.isConnected) return _ConnectedView(p: p);
          return _ScanView(p: p);
        },
      ),
    );
  }
}

class _ConnectedView extends StatefulWidget {
  final DeviceProvider p;
  const _ConnectedView({required this.p});

  @override
  State<_ConnectedView> createState() => _ConnectedViewState();
}

class _ConnectedViewState extends State<_ConnectedView> {
  final _scrollController = ScrollController();
  int _scrollDownCount = 0;
  bool _showDebug = false;
  double _lastScrollPos = 0;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  void _onScroll() {
    final pos = _scrollController.position.pixels;
    if (pos > _lastScrollPos + 20) {
      _scrollDownCount++;
      _lastScrollPos = pos;
      if (_scrollDownCount >= 3 && !_showDebug) {
        setState(() {}); // muestra el botón
      }
    } else if (pos < _lastScrollPos - 20) {
      _lastScrollPos = pos;
    }
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final p = widget.p;
    final cs = Theme.of(context).colorScheme;
    return SingleChildScrollView(
      controller: _scrollController,
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          const SizedBox(height: 16),
          Icon(Icons.rowing, size: 72, color: cs.primary),
          const SizedBox(height: 16),
          Text(p.connectedDeviceName ?? 'Rower',
              style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 8),
          Chip(
            avatar: const Icon(Icons.bluetooth_connected, size: 14),
            label: const Text('Conectado'),
            backgroundColor: Colors.green.withOpacity(0.2),
            labelStyle: const TextStyle(color: Colors.greenAccent),
          ),
          const SizedBox(height: 24),
          const Text('Métricas en tiempo real:',
              style: TextStyle(color: Colors.white54)),
          const SizedBox(height: 16),
          _LiveMetricsRow(p: p),
          const SizedBox(height: 32),
          // Botón debug: aparece tras 3 scrolls hacia abajo
          if (_scrollDownCount >= 3 && !_showDebug)
            TextButton.icon(
              onPressed: () => setState(() => _showDebug = true),
              icon: const Icon(Icons.bug_report, size: 14, color: Colors.white24),
              label: const Text('Mostrar debug BLE',
                  style: TextStyle(color: Colors.white24, fontSize: 12)),
            ),
          if (_showDebug) ...[
            const SizedBox(height: 8),
            _DebugPanel(p: p),
          ],
          const SizedBox(height: 16),
        ],
      ),
    );
  }
}

class _DebugPanel extends StatelessWidget {
  final DeviceProvider p;
  const _DebugPanel({required this.p});

  @override
  Widget build(BuildContext context) {
    final t = p.lastNotificationTime;
    final timeStr = t != null
        ? '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}:${t.second.toString().padLeft(2, '0')}'
        : '--:--:--';

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.black38,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.white12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.bug_report, size: 14, color: Colors.white38),
              const SizedBox(width: 6),
              const Text('DEBUG BLE',
                  style: TextStyle(
                      fontSize: 11,
                      color: Colors.white38,
                      letterSpacing: 1.2,
                      fontWeight: FontWeight.bold)),
              const Spacer(),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: p.notificationCount > 0
                      ? Colors.green.withOpacity(0.2)
                      : Colors.red.withOpacity(0.2),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  p.notificationCount > 0 ? 'RECIBIENDO' : 'SIN DATOS',
                  style: TextStyle(
                      fontSize: 10,
                      color: p.notificationCount > 0
                          ? Colors.greenAccent
                          : Colors.redAccent,
                      fontWeight: FontWeight.bold),
                ),
              ),
            ],
          ),
          const Divider(color: Colors.white12, height: 16),
          _DebugRow('Notificaciones', '${p.notificationCount}'),
          _DebugRow('Última vez', timeStr),
          const SizedBox(height: 8),
          const Text('Bytes crudos (hex):',
              style: TextStyle(fontSize: 11, color: Colors.white38)),
          const SizedBox(height: 4),
          Text(
            p.lastRawHex,
            style: const TextStyle(
                fontSize: 11,
                color: Colors.cyanAccent,
                fontFamily: 'monospace'),
          ),
          const Divider(color: Colors.white12, height: 16),
          _DebugRow('SPM parseado', p.data.strokeRate.toStringAsFixed(1)),
          _DebugRow('Vatios parseados', '${p.data.powerWatts}'),
          _DebugRow('Distancia parseada', '${p.data.distanceMeters}m'),
          _DebugRow('Split parseado', p.data.pace500mFormatted),
        ],
      ),
    );
  }
}

class _DebugRow extends StatelessWidget {
  final String label;
  final String value;
  const _DebugRow(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(fontSize: 12, color: Colors.white54)),
          Text(value,
              style: const TextStyle(
                  fontSize: 12,
                  color: Colors.white,
                  fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}

class _LiveMetricsRow extends StatelessWidget {
  final DeviceProvider p;
  const _LiveMetricsRow({required this.p});

  @override
  Widget build(BuildContext context) {
    final d = p.data;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      child: Wrap(
        spacing: 12,
        runSpacing: 12,
        alignment: WrapAlignment.center,
        children: [
          _Pill(label: 'Spl', value: d.pace500mFormatted),
          _Pill(label: 'SPM', value: d.strokeRate.toStringAsFixed(1)),
          _Pill(label: 'W', value: d.powerWatts.toString()),
          _Pill(label: 'Dist', value: '${d.distanceMeters}m'),
          if (d.heartRate > 0) _Pill(label: 'BPM', value: d.heartRate.toString()),
        ],
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  final String label;
  final String value;
  const _Pill({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xFF1A2E45),
        borderRadius: BorderRadius.circular(24),
      ),
      child: Column(
        children: [
          Text(value,
              style: const TextStyle(
                  fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white)),
          Text(label,
              style: const TextStyle(fontSize: 11, color: Colors.white54)),
        ],
      ),
    );
  }
}

class _ScanView extends StatelessWidget {
  final DeviceProvider p;
  const _ScanView({required this.p});

  @override
  Widget build(BuildContext context) {
    final isScanning = p.status == BleStatus.scanning;
    final isConnecting = p.status == BleStatus.connecting;

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (p.error != null)
            Container(
              margin: const EdgeInsets.only(bottom: 12),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.red.withOpacity(0.15),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(p.error!,
                  style: const TextStyle(color: Colors.redAccent, fontSize: 13)),
            ),

          FilledButton.icon(
            onPressed: isScanning || isConnecting ? null : p.startScan,
            icon: isScanning
                ? const SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.bluetooth_searching),
            label: Text(isScanning ? 'Buscando...' : 'Buscar dispositivos'),
          ),

          const SizedBox(height: 12),

          if (isScanning)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 8),
              child: Text(
                'Buscando dispositivos FTMS con servicio 0x1826...',
                style: TextStyle(color: Colors.white54, fontSize: 12),
                textAlign: TextAlign.center,
              ),
            ),

          if (p.scanResults.isEmpty && !isScanning)
            const Expanded(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.bluetooth_disabled, size: 48, color: Colors.white24),
                    SizedBox(height: 12),
                    Text('No se encontraron dispositivos',
                        style: TextStyle(color: Colors.white54)),
                    SizedBox(height: 8),
                    Text(
                      'Asegurate que el rower esté encendido\ny el Bluetooth activado',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: Colors.white38, fontSize: 12),
                    ),
                  ],
                ),
              ),
            ),

          if (p.scanResults.isNotEmpty)
            Expanded(
              child: ListView.builder(
                itemCount: p.scanResults.length,
                itemBuilder: (context, i) {
                  final r = p.scanResults[i];
                  final name = r.device.platformName.isNotEmpty
                      ? r.device.platformName
                      : 'Dispositivo desconocido';
                  return Card(
                    margin: const EdgeInsets.symmetric(vertical: 4),
                    child: ListTile(
                      leading: const Icon(Icons.rowing, color: Color(0xFF00B4D8)),
                      title: Text(name),
                      subtitle: Text(r.device.remoteId.str,
                          style: const TextStyle(fontSize: 11, color: Colors.white38)),
                      trailing: isConnecting
                          ? const SizedBox(
                              width: 20,
                              height: 20,
                              child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.chevron_right),
                      onTap: isConnecting
                          ? null
                          : () => p.connect(r.device),
                    ),
                  );
                },
              ),
            ),
        ],
      ),
    );
  }
}
