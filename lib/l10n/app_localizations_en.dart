// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get navDevice => 'Device';

  @override
  String get navWorkout => 'Workout';

  @override
  String get navRoutines => 'Routines';

  @override
  String get navHistory => 'History';

  @override
  String get deviceTitle => 'Device';

  @override
  String get deviceDisconnect => 'Disconnect';

  @override
  String get deviceConnected => 'Connected';

  @override
  String get deviceSearching => 'Searching...';

  @override
  String get deviceSearch => 'Search devices';

  @override
  String get deviceSearchHint =>
      'Searching for FTMS devices with service 0x1826...';

  @override
  String get deviceNoResults => 'No devices found';

  @override
  String get deviceNoResultsHint =>
      'Make sure the rower is on\nand Bluetooth is enabled';

  @override
  String get deviceBtOff => 'Bluetooth is off';

  @override
  String get deviceBtOffHint =>
      'Turn on Bluetooth from Control Center\nor Settings to search for the rower';

  @override
  String get deviceBtDenied => 'Bluetooth permission denied';

  @override
  String get deviceBtDeniedHint =>
      'Enable Bluetooth permission in\nSettings > Privacy > Bluetooth';

  @override
  String get deviceBtEnable => 'Enable Bluetooth';

  @override
  String get workoutTitle => 'Workout';

  @override
  String get workoutFree => 'Free workout';

  @override
  String get workoutRowerNotConnected =>
      'Rower not connected — data will not be recorded';

  @override
  String get workoutOrPickRoutine => '  Or pick a routine:';

  @override
  String get workoutNoRoutines => 'Create a routine in the \"Routines\" tab';

  @override
  String get workoutInProgress => 'Workout in progress...';

  @override
  String get workoutFinish => 'Finish';

  @override
  String get workoutFinishTitle => 'Finish workout';

  @override
  String get workoutFinishContent => 'The session will be saved. Continue?';

  @override
  String get workoutPause => 'Pause';

  @override
  String get workoutResume => 'Resume';

  @override
  String get workoutStartTitle => 'Start workout?';

  @override
  String workoutStartContent(String summary) {
    return '$summary\n\nStart the workout?';
  }

  @override
  String get workoutStart => 'Start';

  @override
  String get metricTime => 'TIME';

  @override
  String get metricSplit => 'SPLIT 500m';

  @override
  String get metricDistance => 'DISTANCE';

  @override
  String get metricWatts => 'WATTS';

  @override
  String get metricSpm => 'SPM';

  @override
  String get metricCalories => 'CALORIES';

  @override
  String get metricHeartRate => 'HR';

  @override
  String get metricFreeLabel => 'Free workout';

  @override
  String get routinesTitle => 'Routines';

  @override
  String get routinesEmpty => 'No routines yet';

  @override
  String get routinesEmptyHint => 'Press + to create one';

  @override
  String get routineEdit => 'Edit';

  @override
  String get routineDelete => 'Delete';

  @override
  String get routineDeleteTitle => 'Delete routine';

  @override
  String routineDeleteContent(String name) {
    return 'Delete \"$name\"?';
  }

  @override
  String get historyTitle => 'History';

  @override
  String get historyEmpty => 'No saved sessions';

  @override
  String get historyFree => 'Free';

  @override
  String get historyStatTime => 'Time';

  @override
  String get historyStatDistance => 'Distance';

  @override
  String get historyStatWatts => 'Watts';

  @override
  String get historyStatCalories => 'kcal';

  @override
  String get sessionNoTelemetry => 'No telemetry data';

  @override
  String get sessionSummary => 'Summary';

  @override
  String get sessionStepBreakdown => 'Step Performance';

  @override
  String get sessionChartWatts => 'Watts';

  @override
  String get sessionChartSpm => 'SPM';

  @override
  String get sessionChartSplit => 'Split 500m';

  @override
  String get sessionChartDistance => 'Distance';

  @override
  String get sessionChartHr => 'HR';

  @override
  String get stepTypeWarmup => 'Warmup';

  @override
  String get stepTypeWork => 'Work';

  @override
  String get stepTypeRest => 'Rest';

  @override
  String get stepTypeCooldown => 'Cooldown';

  @override
  String get cancel => 'Cancel';

  @override
  String get delete => 'Delete';

  @override
  String get hrmTitle => 'Heart Rate Monitor';

  @override
  String get hrmAdd => 'Add HR Monitor';

  @override
  String get hrmSearching => 'Searching for HR monitors...';

  @override
  String get hrmConnected => 'HR Monitor connected';

  @override
  String get hrmDisconnect => 'Disconnect HR';

  @override
  String get hrmNoDevices => 'No HR monitors found';

  @override
  String get hrmNoDevicesHint => 'Enable HR broadcast on your device';
}
