import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../core/database/database_service.dart';
import '../../core/models/workout_session.dart';
import '../../core/models/interval_step.dart';
import '../../core/strava/strava_api_service.dart';
import '../../core/strava/strava_auth_service.dart';

enum UploadPreference { auto, ask, manual }

class ProfileProvider extends ChangeNotifier {
  final StravaAuthService _auth;
  final StravaApiService _api;
  final DatabaseService _db;

  bool _connected = false;
  String? _athleteName;
  String? _athleteAvatar;
  UploadPreference _uploadPref = UploadPreference.auto;
  bool _isUploading = false;
  bool _isSyncing = false;
  String? _lastError;
  int _syncedCount = 0;

  ProfileProvider(this._auth, this._api, this._db) {
    _init();
  }

  bool get isConnected => _connected;
  String? get athleteName => _athleteName;
  String? get athleteAvatar => _athleteAvatar;
  UploadPreference get uploadPreference => _uploadPref;
  bool get isUploading => _isUploading;
  bool get isSyncing => _isSyncing;
  String? get lastError => _lastError;
  int get syncedCount => _syncedCount;

  Future<void> _init() async {
    _connected = await _auth.isAuthenticated;
    if (_connected) {
      _athleteName = await _auth.athleteName;
      _athleteAvatar = await _auth.athleteAvatar;
    }
    final prefs = await SharedPreferences.getInstance();
    final prefStr = prefs.getString('strava_upload_pref') ?? 'auto';
    _uploadPref = UploadPreference.values.firstWhere(
      (e) => e.name == prefStr,
      orElse: () => UploadPreference.auto,
    );
    notifyListeners();
  }

  Future<bool> connectStrava() async {
    _lastError = null;
    final success = await _auth.login();
    if (success) {
      _connected = true;
      _athleteName = await _auth.athleteName;
      _athleteAvatar = await _auth.athleteAvatar;
    } else {
      _lastError = 'Failed to connect to Strava';
    }
    notifyListeners();
    return success;
  }

  Future<void> disconnectStrava() async {
    await _auth.logout();
    _connected = false;
    _athleteName = null;
    _athleteAvatar = null;
    notifyListeners();
  }

  Future<void> setUploadPreference(UploadPreference pref) async {
    _uploadPref = pref;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('strava_upload_pref', pref.name);
    notifyListeners();
  }

  /// Sube una sesión a Strava
  Future<bool> uploadSession(int sessionId) async {
    if (!_connected) return false;

    _isUploading = true;
    _lastError = null;
    notifyListeners();

    try {
      final session = await _db.getSessionById(sessionId);
      if (session == null) {
        _lastError = 'Session not found';
        _isUploading = false;
        notifyListeners();
        return false;
      }

      final points = await _db.getDataPointsForSession(sessionId);

      // Obtener información de intervalos si la sesión tiene una rutina
      List<IntervalStep>? steps;
      if (session.routineId != null) {
        final routine = await _db.getRoutineById(session.routineId!);
        if (routine != null) {
          steps = routine.flattenedSteps;
        }
      }

      debugPrint('[Profile] Uploading session $sessionId with ${points.length} data points'
          '${steps != null ? ' and ${steps.length} intervals' : ''}');
      final stravaId = await _api.uploadActivity(session, points, steps: steps);

      if (stravaId != null) {
        await _db.updateSessionStravaId(sessionId, stravaId);
        _isUploading = false;
        notifyListeners();
        return true;
      } else {
        _lastError = 'Upload failed – check console for details';
        _isUploading = false;
        notifyListeners();
        return false;
      }
    } catch (e) {
      debugPrint('[Profile] Upload error: $e');
      _lastError = e.toString();
      _isUploading = false;
      notifyListeners();
      return false;
    }
  }

  /// Sube todas las sesiones locales que no están en Strava.
  /// Auto-repara sesiones que no guardaron stats/finishedAt correctamente.
  Future<int> syncToStrava() async {
    if (!_connected) return 0;

    _isUploading = true;
    _lastError = null;
    _syncedCount = 0;
    notifyListeners();

    try {
      final sessions = await _db.getSessions();
      final unsynced = sessions.where((s) => s.stravaActivityId == null).toList();

      debugPrint('[Profile] syncToStrava: ${sessions.length} total, ${unsynced.length} without stravaId');

      // Auto-repair: recompute stats from data_points for broken sessions
      final pending = <WorkoutSession>[];
      for (final session in unsynced) {
        final points = await _db.getDataPointsForSession(session.id!);
        if (points.isEmpty) {
          debugPrint('[Profile] Session ${session.id} has no data points, skipping');
          continue;
        }

        // Recompute stats from data_points
        final stats = SessionStats.compute(points);

        // Skip sessions with no meaningful data
        if (stats.totalTimeSeconds == 0 && stats.totalDistance == 0) {
          debugPrint('[Profile] Session ${session.id} has empty data points, skipping');
          continue;
        }

        // Repair session row if finish() never completed the DB update
        if (session.finishedAt == null || session.totalTimeSeconds == 0) {
          debugPrint('[Profile] Session ${session.id} needs repair: '
              'finishedAt=${session.finishedAt}, time=${session.totalTimeSeconds}s');
          final repaired = WorkoutSession(
            id: session.id,
            routineId: session.routineId,
            routineName: session.routineName,
            startedAt: session.startedAt,
            finishedAt: session.startedAt.add(Duration(seconds: stats.totalTimeSeconds)),
            totalDistanceMeters: stats.totalDistance,
            totalTimeSeconds: stats.totalTimeSeconds,
            avgPowerWatts: stats.p99Watts,
            avgStrokeRate: stats.p99Spm,
            totalCalories: stats.totalCalories,
          );
          await _db.updateSession(repaired);
          pending.add(repaired);
          debugPrint('[Profile] Session ${session.id} repaired: '
              '${stats.totalTimeSeconds}s, ${stats.totalDistance}m');
        } else {
          pending.add(session);
        }
      }

      debugPrint('[Profile] ${pending.length} sessions ready to upload');

      for (final session in pending) {
        try {
          final points = await _db.getDataPointsForSession(session.id!);

          // Obtener información de intervalos si la sesión tiene una rutina
          List<IntervalStep>? steps;
          if (session.routineId != null) {
            final routine = await _db.getRoutineById(session.routineId!);
            if (routine != null) {
              steps = routine.flattenedSteps;
            }
          }

          debugPrint('[Profile] Uploading session ${session.id} "${session.routineName}" (${points.length} pts)'
              '${steps != null ? ' with ${steps.length} intervals' : ''}');
          final stravaId = await _api.uploadActivity(session, points, steps: steps);
          if (stravaId != null) {
            await _db.updateSessionStravaId(session.id!, stravaId);
            _syncedCount++;
            notifyListeners();
          } else {
            debugPrint('[Profile] Session ${session.id} upload returned null');
          }
        } catch (e) {
          debugPrint('[Profile] Error uploading session ${session.id}: $e');
        }
      }

      _isUploading = false;
      notifyListeners();
      return _syncedCount;
    } catch (e) {
      debugPrint('[Profile] syncToStrava error: $e');
      _lastError = e.toString();
      _isUploading = false;
      notifyListeners();
      return _syncedCount;
    }
  }

  /// Descarga actividades de rowing desde Strava que no están en el historial local
  Future<int> syncFromStrava() async {
    if (!_connected) return 0;

    _isSyncing = true;
    _lastError = null;
    _syncedCount = 0;
    notifyListeners();

    try {
      // Obtener sesiones locales para saber cuáles ya están sincronizadas
      final localSessions = await _db.getSessions();
      final localStravaIds = localSessions
          .where((s) => s.stravaActivityId != null)
          .map((s) => s.stravaActivityId!)
          .toSet();

      final activities = await _api.getRowingActivities();

      for (final activity in activities) {
        final stravaIdStr = '${activity.stravaId}';
        if (localStravaIds.contains(stravaIdStr)) continue;

        // Descargar streams de telemetría y detalles (metadata + laps)
        final streams = await _api.getActivityStreams(activity.stravaId);
        final details = await _api.getActivityDetails(activity.stravaId);

        debugPrint('[Profile] Activity ${activity.stravaId}: '
            '${details?.laps.length ?? 0} laps, '
            'metadata: ${details?.metadata != null}');

        // Crear sesión local
        final session = WorkoutSession(
          routineName: activity.name,
          startedAt: activity.startDate,
          finishedAt: activity.startDate.add(Duration(seconds: activity.elapsedTime)),
          totalDistanceMeters: activity.distance.round(),
          totalTimeSeconds: activity.elapsedTime,
          totalCalories: activity.calories ?? 0,
          stravaActivityId: stravaIdStr,
        );
        final sessionId = await _db.insertSession(session);

        // Insertar data points si hay streams
        if (streams != null && streams.time.isNotEmpty) {
          // Primero calcular pace instantáneo para todos los puntos
          final rawPaces = <double>[];
          for (var i = 0; i < streams.time.length; i++) {
            final elapsedSec = streams.time[i];
            final distMeters = i < streams.distance.length ? streams.distance[i].round() : 0;

            double paceSeconds = 0.0;
            if (i > 0) {
              final prevTime = streams.time[i - 1];
              final prevDist = streams.distance.length > i - 1 ? streams.distance[i - 1].round() : 0;
              final deltaTime = elapsedSec - prevTime;
              final deltaDist = distMeters - prevDist;

              // Solo calcular si hay movimiento significativo (mínimo 2 metros)
              if (deltaDist >= 2 && deltaTime > 0) {
                paceSeconds = (deltaTime / deltaDist) * 500;
              }
            } else if (distMeters > 0 && elapsedSec > 0) {
              paceSeconds = (elapsedSec / distMeters) * 500;
            }
            rawPaces.add(paceSeconds);
          }

          // Filtrar outliers: remover valores irreales (pace < 60s o > 600s /500m)
          final filteredPaces = <double>[];
          for (var i = 0; i < rawPaces.length; i++) {
            final pace = rawPaces[i];
            if (pace >= 60 && pace <= 600) {
              filteredPaces.add(pace);
            } else {
              // Reemplazar outlier con valor anterior válido o promedio
              if (filteredPaces.isNotEmpty) {
                filteredPaces.add(filteredPaces.last);
              } else {
                // Buscar el próximo valor válido
                var nextValid = 120.0; // fallback a 2:00/500m
                for (var j = i + 1; j < rawPaces.length; j++) {
                  if (rawPaces[j] >= 60 && rawPaces[j] <= 600) {
                    nextValid = rawPaces[j];
                    break;
                  }
                }
                filteredPaces.add(nextValid);
              }
            }
          }

          // Aplicar moving average más agresivo para suavizar (ventana de 15 puntos)
          final smoothedPaces = _applyMovingAverage(filteredPaces, windowSize: 15);

          // Mapear índices de stream a laps para asignar stepIndex
          final lapMap = <int, int>{}; // streamIndex -> stepIndex
          if (details?.laps != null && details!.laps.isNotEmpty) {
            for (var stepIdx = 0; stepIdx < details.laps.length; stepIdx++) {
              final lap = details.laps[stepIdx];
              for (var i = lap.startIndex; i <= lap.endIndex && i < streams.time.length; i++) {
                lapMap[i] = stepIdx;
              }
            }
          }

          // Crear data points con pace suavizado e información de intervalos
          final points = <DataPoint>[];
          for (var i = 0; i < streams.time.length; i++) {
            final elapsedSec = streams.time[i];
            final distMeters = i < streams.distance.length ? streams.distance[i].round() : 0;
            final stepIdx = lapMap[i];

            // Obtener tipo de intervalo desde metadata si existe
            String? stepType;
            if (stepIdx != null && details?.metadata?.intervals != null) {
              if (stepIdx < details!.metadata!.intervals!.length) {
                stepType = details.metadata!.intervals![stepIdx].type;
              }
            }

            points.add(DataPoint(
              sessionId: sessionId,
              elapsedSeconds: elapsedSec,
              powerWatts: i < streams.watts.length ? streams.watts[i] : 0,
              strokeRate: i < streams.cadence.length ? streams.cadence[i] : 0,
              heartRate: i < streams.heartRate.length ? streams.heartRate[i] : 0,
              distanceMeters: distMeters,
              pace500mSeconds: smoothedPaces[i].round(),
              stepIndex: stepIdx,
              stepType: stepType,
            ));
          }
          await _db.insertDataPoints(points);

          // Actualizar sesión con stats computadas
          final stats = SessionStats.compute(points);
          final updatedSession = WorkoutSession(
            id: sessionId,
            routineName: activity.name,
            startedAt: activity.startDate,
            finishedAt: activity.startDate.add(Duration(seconds: activity.elapsedTime)),
            totalDistanceMeters: stats.totalDistance,
            totalTimeSeconds: stats.totalTimeSeconds,
            avgPowerWatts: stats.p99Watts,
            avgStrokeRate: stats.p99Spm,
            totalCalories: stats.totalCalories,
            stravaActivityId: stravaIdStr,
          );
          await _db.updateSession(updatedSession);
        }

        _syncedCount++;
      }

      _isSyncing = false;
      notifyListeners();
      return _syncedCount;
    } catch (e) {
      _lastError = e.toString();
      _isSyncing = false;
      notifyListeners();
      return _syncedCount;
    }
  }

  /// Elimina todas las sesiones descargadas de Strava (para re-sincronizar)
  Future<void> clearStravaSessions() async {
    await _db.deleteStravaSessions();
    notifyListeners();
  }

  /// Aplica un filtro de media móvil para suavizar una serie de datos
  /// Reduce el ruido manteniendo las tendencias generales
  List<double> _applyMovingAverage(List<double> values, {int windowSize = 7}) {
    if (values.isEmpty) return [];

    final result = <double>[];
    final halfWindow = windowSize ~/ 2;

    for (var i = 0; i < values.length; i++) {
      final start = (i - halfWindow).clamp(0, values.length - 1);
      final end = (i + halfWindow + 1).clamp(0, values.length);

      final window = values.sublist(start, end);
      final avg = window.reduce((a, b) => a + b) / window.length;
      result.add(avg);
    }

    return result;
  }
}
