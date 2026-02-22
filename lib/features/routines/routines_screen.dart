import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'routine_editor_screen.dart';
import 'routines_provider.dart';

class RoutinesScreen extends StatelessWidget {
  const RoutinesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final p = context.watch<RoutinesProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Rutinas')),
      floatingActionButton: FloatingActionButton(
        onPressed: () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => const RoutineEditorScreen()),
        ),
        child: const Icon(Icons.add),
      ),
      body: p.loading
          ? const Center(child: CircularProgressIndicator())
          : p.routines.isEmpty
              ? const Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.fitness_center, size: 56, color: Colors.white12),
                      SizedBox(height: 12),
                      Text('No hay rutinas todavía',
                          style: TextStyle(color: Colors.white38)),
                      SizedBox(height: 8),
                      Text('Presioná + para crear una',
                          style: TextStyle(color: Colors.white24, fontSize: 13)),
                    ],
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(12),
                  itemCount: p.routines.length,
                  itemBuilder: (context, i) {
                    final r = p.routines[i];
                    return Card(
                      margin: const EdgeInsets.symmetric(vertical: 4),
                      child: ListTile(
                        leading: const Icon(Icons.fitness_center,
                            color: Color(0xFF00B4D8)),
                        title: Text(r.name,
                            style: const TextStyle(fontWeight: FontWeight.w600)),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            if (r.description.isNotEmpty)
                              Text(r.description,
                                  style: const TextStyle(fontSize: 12)),
                            Text(r.summary,
                                style: const TextStyle(
                                    fontSize: 11, color: Colors.white54)),
                          ],
                        ),
                        trailing: PopupMenuButton<_Action>(
                          onSelected: (action) =>
                              _onAction(context, action, r, p),
                          itemBuilder: (_) => const [
                            PopupMenuItem(
                                value: _Action.edit, child: Text('Editar')),
                            PopupMenuItem(
                                value: _Action.delete,
                                child: Text('Eliminar',
                                    style: TextStyle(color: Colors.redAccent))),
                          ],
                        ),
                        onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (_) =>
                                  RoutineEditorScreen(existing: r)),
                        ),
                      ),
                    );
                  },
                ),
    );
  }

  void _onAction(BuildContext context, _Action action, routine, p) async {
    switch (action) {
      case _Action.edit:
        Navigator.push(
          context,
          MaterialPageRoute(
              builder: (_) => RoutineEditorScreen(existing: routine)),
        );
      case _Action.delete:
        final ok = await showDialog<bool>(
          context: context,
          builder: (_) => AlertDialog(
            title: const Text('Eliminar rutina'),
            content: Text('¿Eliminar "${routine.name}"?'),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: const Text('Cancelar')),
              FilledButton(
                  style: FilledButton.styleFrom(
                      backgroundColor: Colors.red.shade700),
                  onPressed: () => Navigator.pop(context, true),
                  child: const Text('Eliminar')),
            ],
          ),
        );
        if (ok == true) p.delete(routine.id!);
    }
  }
}

enum _Action { edit, delete }
