import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:rowmate/l10n/app_localizations.dart';
import '../../core/models/interval_step.dart';
import '../../core/models/routine.dart';
import '../../shared/theme.dart';
import 'routines_provider.dart';

class RoutineEditorScreen extends StatefulWidget {
  final Routine? existing;
  const RoutineEditorScreen({super.key, this.existing});

  @override
  State<RoutineEditorScreen> createState() => _RoutineEditorScreenState();
}

class _RoutineEditorScreenState extends State<RoutineEditorScreen> {
  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final List<IntervalStep> _steps = [];
  int _idCounter = 0; // para keys únicas

  @override
  void initState() {
    super.initState();
    if (widget.existing != null) {
      _nameCtrl.text = widget.existing!.name;
      _descCtrl.text = widget.existing!.description;
      _steps.addAll(widget.existing!.steps);
    }
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _descCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final l10n = AppLocalizations.of(context)!;
    if (_nameCtrl.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.editorNameEmpty)),
      );
      return;
    }
    if (_steps.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(l10n.editorNoSteps)),
      );
      return;
    }

    final provider = context.read<RoutinesProvider>();
    final stepsWithOrder = _steps
        .asMap()
        .entries
        .map((e) => e.value.copyWith(order: e.key, routineId: widget.existing?.id ?? 0))
        .toList();

    if (widget.existing == null) {
      await provider.create(_nameCtrl.text.trim(), _descCtrl.text.trim(), stepsWithOrder);
    } else {
      await provider.update(widget.existing!.copyWith(
        name: _nameCtrl.text.trim(),
        description: _descCtrl.text.trim(),
        steps: stepsWithOrder,
      ));
    }

    if (mounted) Navigator.of(context).pop();
  }

  void _addStep({String? groupId}) async {
    final result = await showDialog<IntervalStep>(
      context: context,
      builder: (_) => const _StepDialog(),
    );
    if (result != null) {
      setState(() {
        final step = groupId != null
            ? result.copyWith(groupId: groupId)
            : result;
        _steps.add(step.copyWith(order: _steps.length));
      });
    }
  }

  void _editStep(int index) async {
    final result = await showDialog<IntervalStep>(
      context: context,
      builder: (_) => _StepDialog(existing: _steps[index]),
    );
    if (result != null) {
      setState(() {
        // Preservar groupId/groupRepeatCount
        _steps[index] = result.copyWith(
          order: index,
          groupId: _steps[index].groupId,
          groupRepeatCount: _steps[index].groupRepeatCount,
        );
      });
    }
  }

  void _addSeries() {
    final gid = 'g${DateTime.now().millisecondsSinceEpoch}_${_idCounter++}';
    setState(() {
      // Agregar dos pasos de ejemplo: trabajo + descanso
      _steps.addAll([
        IntervalStep(
          routineId: 0,
          order: _steps.length,
          type: StepType.work,
          durationSeconds: 120,
          groupId: gid,
          groupRepeatCount: 3,
        ),
        IntervalStep(
          routineId: 0,
          order: _steps.length + 1,
          type: StepType.rest,
          durationSeconds: 60,
          groupId: gid,
          groupRepeatCount: 3,
        ),
      ]);
    });
  }

  void _addStepToGroup(String groupId) async {
    final result = await showDialog<IntervalStep>(
      context: context,
      builder: (_) => const _StepDialog(),
    );
    if (result == null) return;

    setState(() {
      // Insertar después del último paso del grupo
      final lastIdx = _steps.lastIndexWhere((s) => s.groupId == groupId);
      final repeatCount = _steps.firstWhere((s) => s.groupId == groupId).groupRepeatCount;
      final step = result.copyWith(groupId: groupId, groupRepeatCount: repeatCount);
      _steps.insert(lastIdx + 1, step);
    });
  }

  void _changeRepeatCount(String groupId) async {
    final l10n = AppLocalizations.of(context)!;
    final current = _steps.firstWhere((s) => s.groupId == groupId).groupRepeatCount ?? 1;
    final ctrl = TextEditingController(text: current.toString());
    final result = await showDialog<int>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(l10n.editorRepetitions),
        content: TextField(
          controller: ctrl,
          keyboardType: TextInputType.number,
          autofocus: true,
          decoration: InputDecoration(
            labelText: l10n.editorAmount,
            border: const OutlineInputBorder(),
          ),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text(l10n.cancel)),
          FilledButton(
              onPressed: () => Navigator.pop(context, int.tryParse(ctrl.text.trim()) ?? 1),
              child: Text(l10n.ok)),
        ],
      ),
    );
    ctrl.dispose();
    if (result != null && result > 0) {
      setState(() {
        for (var i = 0; i < _steps.length; i++) {
          if (_steps[i].groupId == groupId) {
            _steps[i] = _steps[i].copyWith(groupRepeatCount: result);
          }
        }
      });
    }
  }

  void _deleteStep(int index) {
    setState(() {
      _steps.removeAt(index);
    });
  }

  void _deleteGroup(String groupId) {
    setState(() {
      _steps.removeWhere((s) => s.groupId == groupId);
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.existing == null ? l10n.editorNewRoutine : l10n.editorEditRoutine),
        actions: [
          TextButton(onPressed: _save, child: Text(l10n.save)),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                TextField(
                  controller: _nameCtrl,
                  decoration: InputDecoration(
                    labelText: l10n.editorRoutineName,
                    border: const OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _descCtrl,
                  maxLines: 2,
                  decoration: InputDecoration(
                    labelText: l10n.editorDescription,
                    border: const OutlineInputBorder(),
                  ),
                ),
              ],
            ),
          ),

          const Divider(height: 1),

          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                Text(l10n.editorStepsCount(_steps.length),
                    style: const TextStyle(fontWeight: FontWeight.w600)),
                const Spacer(),
                TextButton.icon(
                  onPressed: () => _addStep(),
                  icon: const Icon(Icons.add, size: 18),
                  label: Text(l10n.editorStep),
                ),
                const SizedBox(width: 4),
                TextButton.icon(
                  onPressed: _addSeries,
                  icon: const Icon(Icons.repeat, size: 18),
                  label: Text(l10n.editorSeries),
                ),
              ],
            ),
          ),

          Expanded(
            child: _steps.isEmpty
                ? Center(
                    child: Text(l10n.editorEmptySteps,
                        textAlign: TextAlign.center,
                        style: const TextStyle(color: Colors.white38)))
                : _buildStepsList(),
          ),
        ],
      ),
    );
  }

  Widget _buildStepsList() {
    // Construir items visuales (pasos sueltos + grupos)
    final items = <_EditorItem>[];
    int i = 0;
    while (i < _steps.length) {
      if (_steps[i].groupId == null) {
        items.add(_EditorItem(type: _ItemType.singleStep, stepIndex: i));
        i++;
      } else {
        final gid = _steps[i].groupId!;
        final groupStart = i;
        // Header del grupo
        items.add(_EditorItem(type: _ItemType.groupHeader, stepIndex: i, groupId: gid));
        while (i < _steps.length && _steps[i].groupId == gid) {
          items.add(_EditorItem(type: _ItemType.groupStep, stepIndex: i, groupId: gid));
          i++;
        }
        // Footer del grupo (agregar paso)
        items.add(_EditorItem(type: _ItemType.groupFooter, stepIndex: groupStart, groupId: gid));
      }
    }

    return ListView.builder(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      itemCount: items.length,
      itemBuilder: (context, idx) {
        final item = items[idx];
        switch (item.type) {
          case _ItemType.singleStep:
            return _buildStepTile(item.stepIndex, inGroup: false);
          case _ItemType.groupHeader:
            return _buildGroupHeader(item.groupId!);
          case _ItemType.groupStep:
            return _buildStepTile(item.stepIndex, inGroup: true);
          case _ItemType.groupFooter:
            return _buildGroupFooter(item.groupId!);
        }
      },
    );
  }

  Widget _buildStepTile(int i, {required bool inGroup}) {
    final step = _steps[i];
    final color = stepColor(step.type.name);
    return Container(
      key: ValueKey('step_${step.hashCode}_$i'),
      margin: EdgeInsets.only(
        left: inGroup ? 16 : 0,
        top: 3,
        bottom: 3,
      ),
      decoration: BoxDecoration(
        color: Theme.of(context).cardTheme.color,
        borderRadius: BorderRadius.circular(10),
        border: Border(left: BorderSide(color: color, width: 3)),
      ),
      child: ListTile(
        dense: true,
        contentPadding: const EdgeInsets.symmetric(horizontal: 12),
        leading: Container(
          width: 28,
          height: 28,
          decoration: BoxDecoration(
            color: color.withOpacity(0.2),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Icon(_stepIcon(step.type), size: 14, color: color),
        ),
        title: Text(stepTypeLocalized(step.type, AppLocalizations.of(context)!),
            style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 13)),
        subtitle: Text(
          step.targetLabel.isEmpty
              ? step.durationLabel
              : '${step.durationLabel}  ·  ${step.targetLabel}',
          style: const TextStyle(fontSize: 11),
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            IconButton(
                icon: const Icon(Icons.edit, size: 16),
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(minWidth: 32),
                onPressed: () => _editStep(i)),
            IconButton(
                icon: const Icon(Icons.close, size: 16),
                padding: EdgeInsets.zero,
                constraints: const BoxConstraints(minWidth: 32),
                color: Colors.red.shade300,
                onPressed: () => _deleteStep(i)),
          ],
        ),
      ),
    );
  }

  Widget _buildGroupHeader(String groupId) {
    final step = _steps.firstWhere((s) => s.groupId == groupId);
    final repeatCount = step.groupRepeatCount ?? 1;

    return Container(
      key: ValueKey('gh_$groupId'),
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFF0077B6).withOpacity(0.12),
        borderRadius: const BorderRadius.vertical(top: Radius.circular(12)),
        border: const Border(left: BorderSide(color: Color(0xFF0077B6), width: 3)),
      ),
      child: Row(
        children: [
          const Icon(Icons.repeat, size: 16, color: Color(0xFF00B4D8)),
          const SizedBox(width: 6),
          Text(AppLocalizations.of(context)!.editorSeries,
              style: const TextStyle(
                  color: Color(0xFF00B4D8),
                  fontWeight: FontWeight.w700,
                  fontSize: 13)),
          const SizedBox(width: 8),
          InkWell(
            onTap: () => _changeRepeatCount(groupId),
            borderRadius: BorderRadius.circular(12),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: const Color(0xFF00B4D8).withOpacity(0.2),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text('×$repeatCount',
                  style: const TextStyle(
                      color: Color(0xFF00B4D8),
                      fontWeight: FontWeight.w800,
                      fontSize: 14)),
            ),
          ),
          const Spacer(),
          IconButton(
            icon: const Icon(Icons.delete_outline, size: 18),
            color: Colors.red.shade300,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(minWidth: 32),
            tooltip: AppLocalizations.of(context)!.editorDeleteSeries,
            onPressed: () => _deleteGroup(groupId),
          ),
        ],
      ),
    );
  }

  Widget _buildGroupFooter(String groupId) {
    return Container(
      key: ValueKey('gf_$groupId'),
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      decoration: BoxDecoration(
        color: const Color(0xFF0077B6).withOpacity(0.06),
        borderRadius: const BorderRadius.vertical(bottom: Radius.circular(12)),
        border: const Border(left: BorderSide(color: Color(0xFF0077B6), width: 3)),
      ),
      child: TextButton.icon(
        onPressed: () => _addStepToGroup(groupId),
        icon: const Icon(Icons.add, size: 14),
        label: Text(AppLocalizations.of(context)!.editorAddStepToSeries, style: const TextStyle(fontSize: 12)),
        style: TextButton.styleFrom(
          foregroundColor: const Color(0xFF00B4D8),
          padding: const EdgeInsets.symmetric(horizontal: 8),
          minimumSize: const Size(0, 28),
        ),
      ),
    );
  }

  IconData _stepIcon(StepType type) {
    switch (type) {
      case StepType.warmup:
        return Icons.whatshot;
      case StepType.work:
        return Icons.fitness_center;
      case StepType.rest:
        return Icons.pause_circle_outline;
      case StepType.cooldown:
        return Icons.ac_unit;
    }
  }
}

// ─── Tipos para el builder de la lista ────────────────────────────────────

enum _ItemType { singleStep, groupHeader, groupStep, groupFooter }

class _EditorItem {
  final _ItemType type;
  final int stepIndex;
  final String? groupId;
  const _EditorItem({required this.type, required this.stepIndex, this.groupId});
}

// ─── Dialog para crear / editar un paso ──────────────────────────────────

class _StepDialog extends StatefulWidget {
  final IntervalStep? existing;
  const _StepDialog({this.existing});

  @override
  State<_StepDialog> createState() => _StepDialogState();
}

class _StepDialogState extends State<_StepDialog> {
  StepType _type = StepType.work;
  bool _useDistance = false;
  final _minCtrl = TextEditingController();   // minutos
  final _secCtrl = TextEditingController();   // segundos
  final _distCtrl = TextEditingController();  // metros
  final _wMinCtrl = TextEditingController();  // watts min
  final _wMaxCtrl = TextEditingController();  // watts max
  final _spmCtrl = TextEditingController();   // spm
  final _splitMinCtrl = TextEditingController(); // split min
  final _splitSecCtrl = TextEditingController(); // split sec

  @override
  void initState() {
    super.initState();
    if (widget.existing != null) {
      final s = widget.existing!;
      _type = s.type;
      _useDistance = s.distanceMeters != null;
      if (s.durationSeconds != null) {
        _minCtrl.text = (s.durationSeconds! ~/ 60).toString();
        _secCtrl.text = (s.durationSeconds! % 60).toString();
      }
      if (s.distanceMeters != null) _distCtrl.text = s.distanceMeters.toString();
      if (s.targetWattsMin != null) _wMinCtrl.text = s.targetWattsMin.toString();
      if (s.targetWattsMax != null) _wMaxCtrl.text = s.targetWattsMax.toString();
      if (s.targetSpm != null) _spmCtrl.text = s.targetSpm.toString();
      if (s.targetSplitSeconds != null) {
        _splitMinCtrl.text = (s.targetSplitSeconds! ~/ 60).toString();
        _splitSecCtrl.text = (s.targetSplitSeconds! % 60).toString();
      }
    }
  }

  @override
  void dispose() {
    for (final c in [_minCtrl, _secCtrl, _distCtrl, _wMinCtrl, _wMaxCtrl, _spmCtrl, _splitMinCtrl, _splitSecCtrl]) {
      c.dispose();
    }
    super.dispose();
  }

  IntervalStep _build() {
    int? durationSeconds;
    int? distanceMeters;
    if (_useDistance) {
      distanceMeters = int.tryParse(_distCtrl.text.trim());
    } else {
      final m = int.tryParse(_minCtrl.text.trim()) ?? 0;
      final s = int.tryParse(_secCtrl.text.trim()) ?? 0;
      durationSeconds = m * 60 + s;
    }
    return IntervalStep(
      routineId: 0,
      order: 0,
      type: _type,
      durationSeconds: durationSeconds,
      distanceMeters: distanceMeters,
      targetWattsMin: int.tryParse(_wMinCtrl.text.trim()),
      targetWattsMax: int.tryParse(_wMaxCtrl.text.trim()),
      targetSpm: int.tryParse(_spmCtrl.text.trim()),
      targetSplitSeconds: _buildSplitSeconds(),
    );
  }

  int? _buildSplitSeconds() {
    final m = int.tryParse(_splitMinCtrl.text.trim()) ?? 0;
    final s = int.tryParse(_splitSecCtrl.text.trim()) ?? 0;
    final total = m * 60 + s;
    return total > 0 ? total : null;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return AlertDialog(
      title: Text(widget.existing == null ? l10n.editorNewStep : l10n.editorEditStep),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            DropdownButtonFormField<StepType>(
              value: _type,
              decoration: InputDecoration(labelText: l10n.editorStepType, border: const OutlineInputBorder()),
              items: StepType.values
                  .map((t) => DropdownMenuItem(value: t, child: Text(stepTypeLocalized(t, l10n))))
                  .toList(),
              onChanged: (v) => setState(() => _type = v!),
            ),

            const SizedBox(height: 12),

            SegmentedButton<bool>(
              segments: [
                ButtonSegment(value: false, label: Text(l10n.editorByTime)),
                ButtonSegment(value: true, label: Text(l10n.editorByDistance)),
              ],
              selected: {_useDistance},
              onSelectionChanged: (v) => setState(() => _useDistance = v.first),
            ),

            const SizedBox(height: 12),

            if (!_useDistance)
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _minCtrl,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                          labelText: l10n.editorMinutes, border: const OutlineInputBorder()),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextField(
                      controller: _secCtrl,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                          labelText: l10n.editorSeconds, border: const OutlineInputBorder()),
                    ),
                  ),
                ],
              ),

            if (_useDistance)
              TextField(
                controller: _distCtrl,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(
                    labelText: l10n.editorMeters, border: const OutlineInputBorder()),
              ),

            const SizedBox(height: 12),
            const Divider(),
            Text(l10n.editorOptionalTargets,
                style: const TextStyle(fontSize: 12, color: Colors.white54)),
            const SizedBox(height: 8),

            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _wMinCtrl,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                        labelText: l10n.editorWattsMin, border: const OutlineInputBorder()),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    controller: _wMaxCtrl,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                        labelText: l10n.editorWattsMax, border: const OutlineInputBorder()),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 8),

            TextField(
              controller: _spmCtrl,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                  labelText: l10n.editorTargetSpm, border: const OutlineInputBorder()),
            ),

            const SizedBox(height: 8),

            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _splitMinCtrl,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                        labelText: l10n.editorSplitMin, border: const OutlineInputBorder()),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    controller: _splitSecCtrl,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                        labelText: l10n.editorSplitSec, border: const OutlineInputBorder()),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
            onPressed: () => Navigator.pop(context),
            child: Text(l10n.cancel)),
        FilledButton(
            onPressed: () => Navigator.pop(context, _build()),
            child: Text(l10n.confirm)),
      ],
    );
  }
}
