import 'package:flutter/material.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';
import 'package:provider/provider.dart';
import '../../core/bluetooth/ble_service.dart';
import '../../shared/theme.dart';
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
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
      child: Column(
        children: [
          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.rowing, size: 32, color: cs.primary),
              const SizedBox(width: 12),
              Text(p.connectedDeviceName ?? 'Rower',
                  style: Theme.of(context).textTheme.titleLarge),
              const SizedBox(width: 12),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: Colors.green.withOpacity(0.2),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.bluetooth_connected, size: 12, color: Colors.greenAccent),
                    SizedBox(width: 4),
                    Text('Conectado',
                        style: TextStyle(color: Colors.greenAccent, fontSize: 12)),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 24),
          _LiveMetricsGrid(p: p),
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

class _LiveMetricsGrid extends StatelessWidget {
  final DeviceProvider p;
  const _LiveMetricsGrid({required this.p});

  @override
  Widget build(BuildContext context) {
    final d = p.data;
    final metrics = <_MetricData>[
      _MetricData('SPLIT /500m', d.pace500mFormatted, Icons.timer_outlined, MetricColors.split),
      _MetricData('SPM', d.strokeRate.toStringAsFixed(1), Icons.speed, MetricColors.spm),
      _MetricData('WATTS', d.powerWatts.toString(), Icons.bolt, MetricColors.watts),
      _MetricData('DISTANCIA', '${d.distanceMeters}m', Icons.straighten, MetricColors.distance),
      if (d.heartRate > 0)
        _MetricData('BPM', d.heartRate.toString(), Icons.favorite, MetricColors.heartRate),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final crossCount = width > 600 ? 3 : 2;
        // Aspect ratio más bajo = tarjetas más altas
        final aspectRatio = width > 600 ? 1.6 : 1.3;
        return GridView.count(
          crossAxisCount: crossCount,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: aspectRatio,
          children: metrics.map((m) => _MetricCard(data: m)).toList(),
        );
      },
    );
  }
}

class _MetricData {
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  const _MetricData(this.label, this.value, this.icon, this.color);
}

class _MetricCard extends StatelessWidget {
  final _MetricData data;
  const _MetricCard({required this.data});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFF1A2E45),
        borderRadius: BorderRadius.circular(16),
        border: Border(
          top: BorderSide(color: data.color.withOpacity(0.6), width: 3),
        ),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(data.icon, size: 14, color: data.color.withOpacity(0.7)),
              const SizedBox(width: 6),
              Text(data.label,
                  style: TextStyle(
                      fontSize: 12,
                      color: data.color.withOpacity(0.7),
                      fontWeight: FontWeight.w600,
                      letterSpacing: 0.8)),
            ],
          ),
          const Spacer(),
          FittedBox(
            fit: BoxFit.scaleDown,
            child: Text(data.value,
                style: TextStyle(
                    fontSize: 52,
                    fontWeight: FontWeight.w800,
                    color: data.color,
                    height: 1)),
          ),
          const Spacer(),
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
    final btOff = p.adapterState == BluetoothAdapterState.off;
    final btUnauthorized = p.adapterState == BluetoothAdapterState.unauthorized;

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

          if (btOff || btUnauthorized)
            Expanded(
              child: Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(Icons.bluetooth_disabled, size: 64, color: Colors.redAccent),
                    const SizedBox(height: 16),
                    Text(
                      btUnauthorized
                          ? 'Permiso de Bluetooth denegado'
                          : 'Bluetooth apagado',
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      btUnauthorized
                          ? 'Habilitá el permiso de Bluetooth en\nAjustes > Privacidad > Bluetooth'
                          : 'Activá el Bluetooth desde el Centro de Control\no Ajustes para buscar el rower',
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: Colors.white54, fontSize: 14),
                    ),
                    const SizedBox(height: 24),
                    FilledButton.icon(
                      onPressed: () async {
                        await FlutterBluePlus.turnOn();
                      },
                      icon: const Icon(Icons.bluetooth),
                      label: const Text('Activar Bluetooth'),
                    ),
                  ],
                ),
              ),
            )
          else ...[
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
        ],
      ),
    );
  }
}
