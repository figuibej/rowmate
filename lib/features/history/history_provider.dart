import 'package:flutter/foundation.dart';
import '../../core/database/database_service.dart';
import '../../core/models/workout_session.dart';

class HistoryProvider extends ChangeNotifier {
  final DatabaseService _db;

  List<WorkoutSession> _sessions = [];
  bool _loading = false;

  HistoryProvider(this._db) {
    load();
  }

  List<WorkoutSession> get sessions => _sessions;
  bool get loading => _loading;

  Future<void> load() async {
    _loading = true;
    notifyListeners();
    _sessions = await _db.getSessions();
    _loading = false;
    notifyListeners();
  }
}
