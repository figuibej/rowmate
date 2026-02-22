import 'package:flutter/foundation.dart';
import '../../core/database/database_service.dart';
import '../../core/models/routine.dart';
import '../../core/models/interval_step.dart';

class RoutinesProvider extends ChangeNotifier {
  final DatabaseService _db;

  List<Routine> _routines = [];
  bool _loading = false;

  RoutinesProvider(this._db) {
    load();
  }

  List<Routine> get routines => _routines;
  bool get loading => _loading;

  Future<void> load() async {
    _loading = true;
    notifyListeners();
    _routines = await _db.getRoutines();
    _loading = false;
    notifyListeners();
  }

  Future<Routine> create(String name, String description, List<IntervalStep> steps) async {
    final routine = Routine(
      name: name,
      description: description,
      createdAt: DateTime.now(),
      steps: steps,
    );
    final saved = await _db.insertRoutine(routine);
    _routines.insert(0, saved);
    notifyListeners();
    return saved;
  }

  Future<Routine> update(Routine routine) async {
    final saved = await _db.updateRoutine(routine);
    final idx = _routines.indexWhere((r) => r.id == routine.id);
    if (idx >= 0) _routines[idx] = saved;
    notifyListeners();
    return saved;
  }

  Future<void> delete(int id) async {
    await _db.deleteRoutine(id);
    _routines.removeWhere((r) => r.id == id);
    notifyListeners();
  }
}
