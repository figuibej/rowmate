import 'package:flutter/material.dart';
import 'package:rowmate/l10n/app_localizations.dart';
import 'package:provider/provider.dart';
import 'routine_editor_screen.dart';
import 'routines_provider.dart';

class RoutinesScreen extends StatelessWidget {
  const RoutinesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final p = context.watch<RoutinesProvider>();
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.routinesTitle)),
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
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.fitness_center, size: 56, color: Colors.white12),
                      const SizedBox(height: 12),
                      Text(l10n.routinesEmpty,
                          style: const TextStyle(color: Colors.white38)),
                      const SizedBox(height: 8),
                      Text(l10n.routinesEmptyHint,
                          style: const TextStyle(color: Colors.white24, fontSize: 13)),
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
                          itemBuilder: (_) => [
                            PopupMenuItem(
                                value: _Action.edit, child: Text(l10n.routineEdit)),
                            PopupMenuItem(
                                value: _Action.delete,
                                child: Text(l10n.routineDelete,
                                    style: const TextStyle(color: Colors.redAccent))),
                          ],
                        ),
                        onTap: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (_) => RoutineEditorScreen(existing: r)),
                        ),
                      ),
                    );
                  },
                ),
    );
  }

  void _onAction(BuildContext context, _Action action, routine, p) async {
    final l10n = AppLocalizations.of(context)!;
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
            title: Text(l10n.routineDeleteTitle),
            content: Text(l10n.routineDeleteContent(routine.name)),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(context, false),
                  child: Text(l10n.cancel)),
              FilledButton(
                  style: FilledButton.styleFrom(
                      backgroundColor: Colors.red.shade700),
                  onPressed: () => Navigator.pop(context, true),
                  child: Text(l10n.delete)),
            ],
          ),
        );
        if (ok == true) p.delete(routine.id!);
    }
  }
}

enum _Action { edit, delete }
