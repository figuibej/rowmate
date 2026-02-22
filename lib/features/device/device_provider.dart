import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';
import '../../core/bluetooth/ble_service.dart';
import '../../core/bluetooth/hrm_service.dart';
import '../../core/models/rowing_data.dart';

const _watchdogThresholdSeconds = 5;

class DeviceProvider extends ChangeNotifier {
  final BleService _ble;
  final HrmService _hrm;

  // ── Rower state ────────────────────────────────────────────────────────
  BleStatus _status = BleStatus.disconnected;
  BluetoothAdapterState _adapterState = BluetoothAdapterState.unknown;
  List<ScanResult> _scanResults = [];
  RowingData _data = const RowingData();
  String? _error;

  // Debug info
  List<int> _lastRawBytes = [];
  int _notificationCount = 0;
  DateTime? _lastNotificationTime;
  Timer? _watchdogTimer;

  // ── HRM state ──────────────────────────────────────────────────────────
  HrmStatus _hrmStatus = HrmStatus.disconnected;
  List<ScanResult> _hrmScanResults = [];
  int _externalHr = 0;

  // ── Subscriptions ──────────────────────────────────────────────────────
  StreamSubscription<BleStatus>? _statusSub;
  StreamSubscription<List<ScanResult>>? _devicesSub;
  StreamSubscription<RowingData>? _dataSub;
  StreamSubscription<List<int>>? _rawBytesSub;
  StreamSubscription<BluetoothAdapterState>? _adapterSub;
  StreamSubscription<HrmStatus>? _hrmStatusSub;
  StreamSubscription<int>? _hrmHrSub;
  StreamSubscription<List<ScanResult>>? _hrmDevicesSub;

  DeviceProvider(this._ble, this._hrm) {
    _adapterSub = _ble.adapterStateStream.listen((state) {
      _adapterState = state;
      notifyListeners();
    });
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
      // Merge external HR if rower doesn't report one
      _data = (d.heartRate == 0 && _externalHr > 0)
          ? d.copyWith(heartRate: _externalHr)
          : d;
      notifyListeners();
    });
    _rawBytesSub = _ble.rawBytesStream.listen((bytes) {
      _lastRawBytes = bytes;
      _notificationCount++;
      _lastNotificationTime = DateTime.now();
      notifyListeners();
    });

    // HRM subscriptions
    _hrmStatusSub = _hrm.statusStream.listen((s) {
      _hrmStatus = s;
      if (s == HrmStatus.disconnected) _externalHr = 0;
      notifyListeners();
    });
    _hrmHrSub = _hrm.hrStream.listen((bpm) {
      _externalHr = bpm;
      // If rower data has no HR, inject the external one live
      if (_data.heartRate == 0) {
        _data = _data.copyWith(heartRate: bpm);
        notifyListeners();
      }
    });
    _hrmDevicesSub = _hrm.devicesStream.listen((results) {
      _hrmScanResults = results;
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

  // ── Rower getters ──────────────────────────────────────────────────────
  BleStatus get status => _status;
  BluetoothAdapterState get adapterState => _adapterState;
  bool get isBluetoothOn => _adapterState == BluetoothAdapterState.on;
  List<ScanResult> get scanResults => _scanResults;
  RowingData get data => _data;
  String? get error => _error;
  bool get isConnected => _status == BleStatus.connected;
  String? get connectedDeviceName => _ble.connectedDeviceName;

  String get lastRawHex => _lastRawBytes.isEmpty
      ? '(sin datos)'
      : _lastRawBytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join(' ');
  int get notificationCount => _notificationCount;
  DateTime? get lastNotificationTime => _lastNotificationTime;

  // ── HRM getters ────────────────────────────────────────────────────────
  HrmStatus get hrmStatus => _hrmStatus;
  bool get hrmIsConnected => _hrmStatus == HrmStatus.connected;
  bool get hrmIsScanning => _hrmStatus == HrmStatus.scanning;
  List<ScanResult> get hrmScanResults => _hrmScanResults;
  String? get hrmDeviceName => _hrm.connectedDeviceName;
  int get externalHr => _externalHr;

  // ── Rower actions ──────────────────────────────────────────────────────
  Future<void> startScan() async {
    _error = null;
    _scanResults = [];
    notifyListeners();
    try {
      await _ble.startScan();
    } catch (e) {
      _error = 'Error scanning: $e';
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
      _error = 'Error connecting: $e';
      notifyListeners();
    }
  }

  Future<void> disconnect() async => _ble.disconnect();

  // ── HRM actions ────────────────────────────────────────────────────────
  Future<void> startHrmScan() async {
    _hrmScanResults = [];
    notifyListeners();
    await _hrm.startScan();
  }

  Future<void> connectHrm(BluetoothDevice device) async => _hrm.connect(device);

  Future<void> disconnectHrm() async => _hrm.disconnect();

  @override
  void dispose() {
    _stopWatchdog();
    _adapterSub?.cancel();
    _statusSub?.cancel();
    _devicesSub?.cancel();
    _dataSub?.cancel();
    _rawBytesSub?.cancel();
    _hrmStatusSub?.cancel();
    _hrmHrSub?.cancel();
    _hrmDevicesSub?.cancel();
    super.dispose();
  }
}
