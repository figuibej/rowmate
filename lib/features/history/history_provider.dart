import 'package:flutter/foundation.dart';
import '../../core/database/database_service.dart';
import '../../core/models/workout_session.dart';

class HistoryProvider extends ChangeNotifier {
  final DatabaseService _db;

  List<WorkoutSession> _sessions = [];
  Map<int, SessionStats> _stats = {};
  bool _loading = false;

  HistoryProvider(this._db) {
    load();
  }

  List<WorkoutSession> get sessions => _sessions;
  Map<int, SessionStats> get stats => _stats;
  bool get loading => _loading;

  SessionStats statsFor(int? sessionId) =>
      _stats[sessionId] ?? const SessionStats();

  Future<void> load() async {
    _loading = true;
    notifyListeners();
    _sessions = await _db.getSessions();
    final ids = _sessions
        .where((s) => s.id != null)
        .map((s) => s.id!)
        .toList();
    _stats = await _db.getStatsForSessions(ids);
    _loading = false;
    notifyListeners();
  }
}
