import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';
import '../../core/bluetooth/ble_service.dart';
import '../../core/models/rowing_data.dart';

// Segundos sin ningún paquete BLE para considerar el monitor apagado
const _watchdogThresholdSeconds = 5;

class DeviceProvider extends ChangeNotifier {
  final BleService _ble;

  BleStatus _status = BleStatus.disconnected;
  List<ScanResult> _scanResults = [];
  RowingData _data = const RowingData();
  String? _error;

  // Debug info
  List<int> _lastRawBytes = [];
  int _notificationCount = 0;
  DateTime? _lastNotificationTime;

  // Watchdog: detecta cuando el monitor se apaga (deja de mandar datos)
  Timer? _watchdogTimer;

  StreamSubscription<BleStatus>? _statusSub;
  StreamSubscription<List<ScanResult>>? _devicesSub;
  StreamSubscription<RowingData>? _dataSub;
  StreamSubscription<List<int>>? _rawBytesSub;

  DeviceProvider(this._ble) {
    _statusSub = _ble.statusStream.listen((s) {
      _status = s;
      if (s == BleStatus.connected) {
        _resetSession();
        _startWatchdog();
      } else {
        _stopWatchdog();
      }
      notifyListeners();
    });
    _devicesSub = _ble.devicesStream.listen((results) {
      _scanResults = results;
      notifyListeners();
    });
    _dataSub = _ble.dataStream.listen((d) {
      _data = d;
      notifyListeners();
    });
    _rawBytesSub = _ble.rawBytesStream.listen((bytes) {
      _lastRawBytes = bytes;
      _notificationCount++;
      _lastNotificationTime = DateTime.now();
      notifyListeners();
    });
  }

  void _resetSession() {
    _lastNotificationTime = null;
    _data = const RowingData();
  }

  void _startWatchdog() {
    _watchdogTimer?.cancel();
    _watchdogTimer = Timer.periodic(const Duration(seconds: 1), (_) async {
      final lastData = _lastNotificationTime;
      if (lastData == null) return;
      final secondsSinceData = DateTime.now().difference(lastData).inSeconds;
      if (secondsSinceData >= _watchdogThresholdSeconds) {
        debugPrint('[Provider] Watchdog: sin datos por ${secondsSinceData}s → desconectando');
        _stopWatchdog();
        await _ble.disconnect();
      }
    });
  }

  void _stopWatchdog() {
    _watchdogTimer?.cancel();
    _watchdogTimer = null;
  }

  BleStatus get status => _status;
  List<ScanResult> get scanResults => _scanResults;
  RowingData get data => _data;
  String? get error => _error;
  bool get isConnected => _status == BleStatus.connected;
  String? get connectedDeviceName => _ble.connectedDeviceName;

  // Debug getters
  String get lastRawHex => _lastRawBytes.isEmpty
      ? '(sin datos)'
      : _lastRawBytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join(' ');
  int get notificationCount => _notificationCount;
  DateTime? get lastNotificationTime => _lastNotificationTime;

  Future<void> startScan() async {
    _error = null;
    _scanResults = [];
    notifyListeners();
    try {
      await _ble.startScan();
    } catch (e) {
      _error = 'Error al escanear: $e';
      notifyListeners();
    }
  }

  Future<void> stopScan() async => _ble.stopScan();

  Future<void> connect(BluetoothDevice device) async {
    _error = null;
    notifyListeners();
    try {
      await _ble.connect(device);
    } catch (e) {
      _error = 'Error al conectar: $e';
      notifyListeners();
    }
  }

  Future<void> disconnect() async => _ble.disconnect();

  @override
  void dispose() {
    _stopWatchdog();
    _statusSub?.cancel();
    _devicesSub?.cancel();
    _dataSub?.cancel();
    _rawBytesSub?.cancel();
    super.dispose();
  }
}
