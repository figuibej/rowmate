// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Spanish Castilian (`es`).
class AppLocalizationsEs extends AppLocalizations {
  AppLocalizationsEs([String locale = 'es']) : super(locale);

  @override
  String get navDevice => 'Dispositivo';

  @override
  String get navWorkout => 'Workout';

  @override
  String get navRoutines => 'Rutinas';

  @override
  String get navHistory => 'Historial';

  @override
  String get deviceTitle => 'Dispositivo';

  @override
  String get deviceDisconnect => 'Desconectar';

  @override
  String get deviceConnected => 'Conectado';

  @override
  String get deviceSearching => 'Buscando...';

  @override
  String get deviceSearch => 'Buscar dispositivos';

  @override
  String get deviceSearchHint =>
      'Buscando dispositivos FTMS con servicio 0x1826...';

  @override
  String get deviceNoResults => 'No se encontraron dispositivos';

  @override
  String get deviceNoResultsHint =>
      'Asegurate que el rower esté encendido\ny el Bluetooth activado';

  @override
  String get deviceBtOff => 'Bluetooth apagado';

  @override
  String get deviceBtOffHint =>
      'Activá el Bluetooth desde el Centro de Control\no Ajustes para buscar el rower';

  @override
  String get deviceBtDenied => 'Permiso de Bluetooth denegado';

  @override
  String get deviceBtDeniedHint =>
      'Habilitá el permiso de Bluetooth en\nAjustes > Privacidad > Bluetooth';

  @override
  String get deviceBtEnable => 'Activar Bluetooth';

  @override
  String get workoutTitle => 'Entrenamiento';

  @override
  String get workoutFree => 'Entrenamiento libre';

  @override
  String get workoutRowerNotConnected =>
      'Rower no conectado — los datos no se registrarán';

  @override
  String get workoutOrPickRoutine => '  O elegí una rutina:';

  @override
  String get workoutNoRoutines => 'Creá una rutina en la pestaña \"Rutinas\"';

  @override
  String get workoutInProgress => 'Entrenamiento en curso...';

  @override
  String get workoutFinish => 'Terminar';

  @override
  String get workoutFinishTitle => 'Terminar entrenamiento';

  @override
  String get workoutFinishContent => 'Se guardará la sesión. ¿Continuar?';

  @override
  String get workoutPause => 'Pausa';

  @override
  String get workoutResume => 'Reanudar';

  @override
  String get workoutStartTitle => 'Iniciar entrenamiento?';

  @override
  String workoutStartContent(String summary) {
    return '$summary\n\nIniciar el entrenamiento?';
  }

  @override
  String get workoutStart => 'Iniciar';

  @override
  String get metricTime => 'TIEMPO';

  @override
  String get metricSplit => 'SPLIT 500m';

  @override
  String get metricDistance => 'DISTANCIA';

  @override
  String get metricWatts => 'VATIOS';

  @override
  String get metricSpm => 'SPM';

  @override
  String get metricCalories => 'CALORÍAS';

  @override
  String get metricHeartRate => 'PULSO';

  @override
  String get metricFreeLabel => 'Entrenamiento libre';

  @override
  String get routinesTitle => 'Rutinas';

  @override
  String get routinesEmpty => 'No hay rutinas todavía';

  @override
  String get routinesEmptyHint => 'Presioná + para crear una';

  @override
  String get routineEdit => 'Editar';

  @override
  String get routineDelete => 'Eliminar';

  @override
  String get routineDeleteTitle => 'Eliminar rutina';

  @override
  String routineDeleteContent(String name) {
    return '¿Eliminar \"$name\"?';
  }

  @override
  String get historyTitle => 'Historial';

  @override
  String get historyEmpty => 'No hay sesiones guardadas';

  @override
  String get historyFree => 'Libre';

  @override
  String get historyStatTime => 'Tiempo';

  @override
  String get historyStatDistance => 'Distancia';

  @override
  String get historyStatWatts => 'Vatios';

  @override
  String get historyStatCalories => 'kcal';

  @override
  String get sessionNoTelemetry => 'No hay datos de telemetría';

  @override
  String get sessionSummary => 'Resumen';

  @override
  String get sessionStepBreakdown => 'Rendimiento por paso';

  @override
  String get sessionChartWatts => 'Vatios';

  @override
  String get sessionChartSpm => 'SPM';

  @override
  String get sessionChartSplit => 'Split 500m';

  @override
  String get sessionChartDistance => 'Distancia';

  @override
  String get sessionChartHr => 'Pulso';

  @override
  String get stepTypeWarmup => 'Calentamiento';

  @override
  String get stepTypeWork => 'Trabajo';

  @override
  String get stepTypeRest => 'Descanso';

  @override
  String get stepTypeCooldown => 'Enfriamiento';

  @override
  String get cancel => 'Cancelar';

  @override
  String get delete => 'Eliminar';

  @override
  String get hrmTitle => 'Monitor de frecuencia cardíaca';

  @override
  String get hrmAdd => 'Agregar HR Monitor';

  @override
  String get hrmSearching => 'Buscando monitores de pulso...';

  @override
  String get hrmConnected => 'HR Monitor conectado';

  @override
  String get hrmDisconnect => 'Desconectar HR';

  @override
  String get hrmNoDevices => 'No se encontraron monitores';

  @override
  String get hrmNoDevicesHint =>
      'Activá el broadcast de pulso en tu dispositivo';
}
