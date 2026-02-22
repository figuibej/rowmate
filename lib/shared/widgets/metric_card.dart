import 'package:flutter/material.dart';

/// Tarjeta de una métrica individual para la pantalla de workout
class MetricCard extends StatelessWidget {
  final String label;
  final String value;
  final String? unit;
  final IconData? icon;
  final Color? color;

  const MetricCard({
    super.key,
    required this.label,
    required this.value,
    this.unit,
    this.icon,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final c = color ?? cs.primary;
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                if (icon != null) ...[
                  Icon(icon, size: 14, color: c),
                  const SizedBox(width: 6),
                ],
                Text(label,
                    style: TextStyle(
                        fontSize: 11,
                        color: cs.onSurface.withOpacity(0.6),
                        letterSpacing: 0.5)),
              ],
            ),
            const SizedBox(height: 6),
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Text(value,
                    style: TextStyle(
                        fontSize: 32,
                        fontWeight: FontWeight.w700,
                        color: c,
                        height: 1)),
                if (unit != null) ...[
                  const SizedBox(width: 4),
                  Text(unit!,
                      style: TextStyle(
                          fontSize: 13,
                          color: cs.onSurface.withOpacity(0.5))),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}
