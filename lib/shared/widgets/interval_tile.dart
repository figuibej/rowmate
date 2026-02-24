import 'package:flutter/material.dart';
import 'package:rowmate/l10n/app_localizations.dart';
import '../../core/models/interval_step.dart';
import '../theme.dart';

/// Tile que representa un paso de intervalo en el editor de rutinas
class IntervalTile extends StatelessWidget {
  final IntervalStep step;
  final bool isActive;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;

  const IntervalTile({
    super.key,
    required this.step,
    this.isActive = false,
    this.onEdit,
    this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    final color = stepColor(step.type.name);
    return Container(
      margin: const EdgeInsets.symmetric(vertical: 4),
      decoration: BoxDecoration(
        color: isActive
            ? color.withOpacity(0.15)
            : Theme.of(context).cardTheme.color,
        borderRadius: BorderRadius.circular(12),
        border: Border(left: BorderSide(color: color, width: 4)),
      ),
      child: ListTile(
        dense: true,
        leading: Container(
          width: 36,
          height: 36,
          decoration: BoxDecoration(
            color: color.withOpacity(0.2),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Center(
            child: Text(
              '${step.order + 1}',
              style: TextStyle(
                  color: color, fontWeight: FontWeight.bold, fontSize: 14),
            ),
          ),
        ),
        title: Text(stepTypeLocalized(step.type, AppLocalizations.of(context)!),
            style: TextStyle(
                color: color, fontWeight: FontWeight.w600, fontSize: 13)),
        subtitle: Text(
          step.targetLabel.isEmpty
              ? step.durationLabel
              : '${step.durationLabel}  ·  ${step.targetLabel}',
          style: const TextStyle(fontSize: 12),
        ),
        trailing: onEdit != null
            ? Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                      icon: const Icon(Icons.edit, size: 18),
                      onPressed: onEdit),
                  IconButton(
                      icon: const Icon(Icons.delete_outline, size: 18),
                      color: Colors.red.shade300,
                      onPressed: onDelete),
                ],
              )
            : null,
      ),
    );
  }
}
