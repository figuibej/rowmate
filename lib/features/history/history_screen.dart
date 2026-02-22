import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../core/database/database_service.dart';
import '../../core/models/workout_session.dart';
import 'history_provider.dart';
import 'session_detail_screen.dart';

class HistoryScreen extends StatelessWidget {
  const HistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final p = context.watch<HistoryProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Historial')),
      body: p.loading
          ? const Center(child: CircularProgressIndicator())
          : p.sessions.isEmpty
              ? const Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.history, size: 56, color: Colors.white12),
                      SizedBox(height: 12),
                      Text('No hay sesiones guardadas',
                          style: TextStyle(color: Colors.white38)),
                    ],
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(12),
                  itemCount: p.sessions.length,
                  itemBuilder: (context, i) {
                    final session = p.sessions[i];
                    return _SessionCard(
                      session,
                      onTap: () {
                        final db = context.read<DatabaseService>();
                        Navigator.of(context).push(MaterialPageRoute(
                          builder: (_) => SessionDetailScreen(
                            session: session,
                            db: db,
                          ),
                        ));
                      },
                    );
                  },
                ),
    );
  }
}

class _SessionCard extends StatelessWidget {
  final WorkoutSession s;
  final VoidCallback? onTap;
  const _SessionCard(this.s, {this.onTap});

  @override
  Widget build(BuildContext context) {
    final dateStr = DateFormat('dd/MM/yyyy  HH:mm').format(s.startedAt);
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 5),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.rowing, size: 18, color: Color(0xFF00B4D8)),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      s.routineName ?? 'Libre',
                      style: const TextStyle(
                          fontWeight: FontWeight.w600, fontSize: 15),
                    ),
                  ),
                  Text(dateStr,
                      style: const TextStyle(
                          fontSize: 11, color: Colors.white38)),
                  const SizedBox(width: 4),
                  const Icon(Icons.chevron_right, size: 16, color: Colors.white24),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  _Stat(label: 'Tiempo', value: s.durationFormatted),
                  _Stat(label: 'Distancia', value: '${s.totalDistanceMeters}m'),
                  _Stat(label: 'Vatios', value: '${s.avgPowerWatts}W'),
                  _Stat(label: 'SPM', value: s.avgStrokeRate.toStringAsFixed(1)),
                  _Stat(label: 'kcal', value: '${s.totalCalories}'),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Stat extends StatelessWidget {
  final String label;
  final String value;
  const _Stat({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        children: [
          Text(value,
              style: const TextStyle(
                  fontWeight: FontWeight.bold, fontSize: 14, color: Colors.white)),
          Text(label,
              style: const TextStyle(fontSize: 10, color: Colors.white38)),
        ],
      ),
    );
  }
}

