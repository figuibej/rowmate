import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';

/// BLE Heart Rate Service (0x180D) / Characteristic 0x2A37
const _hrmServiceUuid = '0000180d-0000-1000-8000-00805f9b34fb';
const _hrmCharUuid    = '00002a37-0000-1000-8000-00805f9b34fb';

enum HrmStatus { disconnected, scanning, connecting, connected }

/// Manages a secondary BLE Heart Rate Monitor (chest strap, watch, etc.)
/// Fully independent from BleService — losing the rower doesn't affect HRM.
class HrmService {
  HrmStatus _status = HrmStatus.disconnected;
  BluetoothDevice? _device;
  String? _connectedDeviceName;

  StreamSubscription<List<ScanResult>>? _scanSub;
  StreamSubscription<BluetoothConnectionState>? _connSub;
  StreamSubscription<List<int>>? _notifySub;

  final _statusController    = StreamController<HrmStatus>.broadcast();
  final _hrController        = StreamController<int>.broadcast();
  final _devicesController   = StreamController<List<ScanResult>>.broadcast();

  Stream<HrmStatus>       get statusStream  => _statusController.stream;
  Stream<int>             get hrStream      => _hrController.stream;
  Stream<List<ScanResult>> get devicesStream => _devicesController.stream;

  HrmStatus get status => _status;
  String? get connectedDeviceName => _connectedDeviceName;

  void _setStatus(HrmStatus s) {
    _status = s;
    _statusController.add(s);
  }

  // ── Scan ──────────────────────────────────────────────────────────────

  Future<void> startScan({Duration timeout = const Duration(seconds: 10)}) async {
    if (_status == HrmStatus.scanning || _status == HrmStatus.connecting) return;
    _setStatus(HrmStatus.scanning);

    await _scanSub?.cancel();

    try {
      await FlutterBluePlus.startScan(
        withServices: [Guid(_hrmServiceUuid)],
        timeout: timeout,
      );
    } catch (e) {
      debugPrint('[HRM] startScan error: $e');
    }

    _scanSub = FlutterBluePlus.scanResults.listen((results) {
      _devicesController.add(results);
    });

    await Future.delayed(timeout);
    if (_status == HrmStatus.scanning) {
      await FlutterBluePlus.stopScan();
      _setStatus(HrmStatus.disconnected);
    }
  }

  Future<void> stopScan() async {
    await FlutterBluePlus.stopScan();
    await _scanSub?.cancel();
    _scanSub = null;
    if (_status == HrmStatus.scanning) _setStatus(HrmStatus.disconnected);
  }

  // ── Connect ───────────────────────────────────────────────────────────

  Future<void> connect(BluetoothDevice device) async {
    if (_status == HrmStatus.connecting || _status == HrmStatus.connected) return;
    _setStatus(HrmStatus.connecting);
    await stopScan();

    try {
      await device.connect(autoConnect: false, timeout: const Duration(seconds: 15));
      _device = device;
      _connectedDeviceName = device.platformName.isNotEmpty
          ? device.platformName
          : device.remoteId.str;

      // Discover HR service and subscribe
      final services = await device.discoverServices();
      for (final svc in services) {
        if (svc.uuid.toString().startsWith('0000180d')) {
          for (final chr in svc.characteristics) {
            if (chr.uuid.toString().startsWith('00002a37')) {
              await chr.setNotifyValue(true);
              await _notifySub?.cancel();
              _notifySub = chr.onValueReceived.listen(_onHrData);
              break;
            }
          }
        }
      }

      // Watch for disconnection
      await _connSub?.cancel();
      _connSub = device.connectionState.listen((state) {
        if (state == BluetoothConnectionState.disconnected) {
          debugPrint('[HRM] Device disconnected');
          _cleanup();
          _setStatus(HrmStatus.disconnected);
        }
      });

      _setStatus(HrmStatus.connected);
      debugPrint('[HRM] Connected to $_connectedDeviceName');
    } catch (e) {
      debugPrint('[HRM] connect error: $e');
      await _cleanup();
      _setStatus(HrmStatus.disconnected);
    }
  }

  // ── HR Data Parser ────────────────────────────────────────────────────

  void _onHrData(List<int> bytes) {
    if (bytes.isEmpty) return;
    // BT spec: flags byte bit0 = 0 means 8-bit HR, bit0 = 1 means 16-bit HR
    final flags = bytes[0];
    final is16bit = (flags & 0x01) != 0;
    int bpm;
    if (is16bit && bytes.length >= 3) {
      bpm = bytes[1] | (bytes[2] << 8);
    } else if (bytes.length >= 2) {
      bpm = bytes[1];
    } else {
      return;
    }
    if (bpm > 0 && bpm < 250) {
      _hrController.add(bpm);
    }
  }

  // ── Disconnect ────────────────────────────────────────────────────────

  Future<void> disconnect() async {
    await _device?.disconnect();
    await _cleanup();
    _setStatus(HrmStatus.disconnected);
  }

  Future<void> _cleanup() async {
    await _notifySub?.cancel();
    _notifySub = null;
    await _connSub?.cancel();
    _connSub = null;
    _device = null;
    _connectedDeviceName = null;
  }

  void dispose() {
    _cleanup();
    _scanSub?.cancel();
    _statusController.close();
    _hrController.close();
    _devicesController.close();
  }
}
