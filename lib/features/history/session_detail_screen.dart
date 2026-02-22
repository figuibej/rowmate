import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import '../../core/database/database_service.dart';
import '../../core/models/workout_session.dart';
import '../../shared/theme.dart';

class SessionDetailScreen extends StatefulWidget {
  final WorkoutSession session;
  final DatabaseService db;
  const SessionDetailScreen({super.key, required this.session, required this.db});

  @override
  State<SessionDetailScreen> createState() => _SessionDetailScreenState();
}

class _SessionDetailScreenState extends State<SessionDetailScreen> {
  List<DataPoint> _points = [];
  bool _loading = true;
  int _chartMetric = 0; // 0=watts, 1=spm, 2=split, 3=distancia, 4=hr

  static const _metricLabels = ['Vatios', 'SPM', 'Split 500m', 'Distancia', 'Pulso'];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final pts = await widget.db.getDataPointsForSession(widget.session.id!);
    setState(() {
      _points = pts;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final s = widget.session;
    return Scaffold(
      appBar: AppBar(
        title: Text(s.routineName ?? 'Entrenamiento libre'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _points.isEmpty
              ? const Center(
                  child: Text('No hay datos de telemetría',
                      style: TextStyle(color: Colors.white38)))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    _buildSummary(s),
                    const SizedBox(height: 20),
                    if (_hasStepData) ...[
                      _buildStepBreakdown(),
                      const SizedBox(height: 20),
                    ],
                    _buildChartSelector(),
                    const SizedBox(height: 8),
                    _buildChart(),
                  ],
                ),
    );
  }

  bool get _hasStepData => _points.any((p) => p.stepIndex != null);

  // ─── Resumen general ───────────────────────────────────────────────────

  Widget _buildSummary(WorkoutSession s) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Resumen',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 12),
            Row(
              children: [
                _StatChip(label: 'Tiempo', value: s.durationFormatted),
                _StatChip(label: 'Distancia', value: '${s.totalDistanceMeters}m'),
                _StatChip(label: 'Vatios', value: '${s.avgPowerWatts}W'),
                _StatChip(label: 'SPM', value: s.avgStrokeRate.toStringAsFixed(1)),
                _StatChip(label: 'kcal', value: '${s.totalCalories}'),
              ],
            ),
          ],
        ),
      ),
    );
  }

  // ─── Desglose por paso ────────────────────────────────────────────────

  Widget _buildStepBreakdown() {
    // Agrupar data points por stepIndex
    final Map<int, List<DataPoint>> grouped = {};
    for (final p in _points) {
      if (p.stepIndex != null) {
        grouped.putIfAbsent(p.stepIndex!, () => []).add(p);
      }
    }

    if (grouped.isEmpty) return const SizedBox.shrink();

    final sortedKeys = grouped.keys.toList()..sort();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Rendimiento por paso',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 12),
            ...sortedKeys.map((idx) {
              final pts = grouped[idx]!;
              return _buildStepRow(idx, pts);
            }),
          ],
        ),
      ),
    );
  }

  Widget _buildStepRow(int stepIdx, List<DataPoint> pts) {
    final avgWatts = pts.map((p) => p.powerWatts).reduce((a, b) => a + b) / pts.length;
    final avgSpm = pts.map((p) => p.strokeRate).reduce((a, b) => a + b) / pts.length;
    final avgSplit = pts.map((p) => p.pace500mSeconds).reduce((a, b) => a + b) / pts.length;
    final duration = pts.length * 5; // cada punto = 5 segundos
    final dist = pts.last.distanceMeters - pts.first.distanceMeters;

    final durStr = '${duration ~/ 60}:${(duration % 60).toString().padLeft(2, '0')}';
    final splitMin = avgSplit.round() ~/ 60;
    final splitSec = avgSplit.round() % 60;
    final splitStr = '$splitMin:${splitSec.toString().padLeft(2, '0')}';

    return Container(
      margin: const EdgeInsets.only(bottom: 6),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white.withAlpha(10),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Container(
            width: 28,
            height: 28,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: const Color(0xFF00B4D8).withAlpha(40),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text('${stepIdx + 1}',
                style: const TextStyle(
                    fontWeight: FontWeight.w700,
                    color: Color(0xFF00B4D8),
                    fontSize: 12)),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$durStr  ·  ${dist}m',
                    style: const TextStyle(fontSize: 11, color: Colors.white54)),
                const SizedBox(height: 2),
                Row(
                  children: [
                    _MiniStat('${avgWatts.round()}W'),
                    _MiniStat('${avgSpm.toStringAsFixed(1)} spm'),
                    _MiniStat('$splitStr/500m'),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ─── Selector de métrica para gráfico ─────────────────────────────────

  Widget _buildChartSelector() {
    return SegmentedButton<int>(
      segments: List.generate(
        _metricLabels.length,
        (i) => ButtonSegment(value: i, label: Text(_metricLabels[i], style: const TextStyle(fontSize: 11))),
      ),
      selected: {_chartMetric},
      onSelectionChanged: (v) => setState(() => _chartMetric = v.first),
      style: SegmentedButton.styleFrom(
        selectedBackgroundColor: const Color(0xFF0077B6),
        selectedForegroundColor: Colors.white,
      ),
    );
  }

  // ─── Gráfico ──────────────────────────────────────────────────────────

  Widget _buildChart() {
    if (_points.isEmpty) return const SizedBox.shrink();

    final spots = _points.map((p) {
      final x = p.elapsedSeconds.toDouble();
      final y = switch (_chartMetric) {
        0 => p.powerWatts.toDouble(),
        1 => p.strokeRate,
        2 => p.pace500mSeconds.toDouble(),
        3 => p.distanceMeters.toDouble(),
        4 => p.heartRate.toDouble(),
        _ => 0.0,
      };
      return FlSpot(x, y);
    }).toList();

    // Computar regiones de pasos con tipo
    final stepRegions = <({double start, double end, String type})>[];
    if (_hasStepData) {
      int? prevStep;
      String? prevType;
      double startX = 0;
      for (final p in _points) {
        if (p.stepIndex != null && p.stepIndex != prevStep) {
          if (prevStep != null && prevType != null) {
            stepRegions.add((start: startX, end: p.elapsedSeconds.toDouble(), type: prevType!));
          }
          startX = p.elapsedSeconds.toDouble();
          prevStep = p.stepIndex;
          prevType = p.stepType ?? 'work';
        }
      }
      // Último segmento
      if (prevStep != null && prevType != null) {
        stepRegions.add((start: startX, end: _points.last.elapsedSeconds.toDouble(), type: prevType!));
      }
    }

    final lineColor = switch (_chartMetric) {
      0 => MetricColors.watts,
      1 => MetricColors.spm,
      2 => MetricColors.split,
      3 => MetricColors.distance,
      4 => MetricColors.heartRate,
      _ => Colors.white,
    };

    final unitLabel = switch (_chartMetric) {
      0 => 'W',
      1 => 'spm',
      2 => 's',
      3 => 'm',
      4 => 'bpm',
      _ => '',
    };

    // Determinar max Y para las RangeAnnotations
    double maxY = spots.map((s) => s.y).reduce((a, b) => a > b ? a : b);
    maxY = maxY * 1.1; // margen del 10%
    if (maxY <= 0) maxY = 100;

    return SizedBox(
      height: 220,
      child: LineChart(
        LineChartData(
          maxY: maxY,
          minY: 0,
          rangeAnnotations: RangeAnnotations(
            verticalRangeAnnotations: stepRegions
                .map((r) => VerticalRangeAnnotation(
                      x1: r.start,
                      x2: r.end,
                      color: stepColor(r.type).withAlpha(25),
                    ))
                .toList(),
          ),
          gridData: FlGridData(
            show: true,
            drawVerticalLine: false,
            getDrawingHorizontalLine: (_) => FlLine(
              color: Colors.white10,
              strokeWidth: 0.5,
            ),
          ),
          titlesData: FlTitlesData(
            bottomTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                reservedSize: 24,
                interval: _xInterval,
                getTitlesWidget: (value, meta) {
                  final m = value ~/ 60;
                  final s = (value % 60).toInt();
                  return Text('$m:${s.toString().padLeft(2, '0')}',
                      style: const TextStyle(fontSize: 9, color: Colors.white38));
                },
              ),
            ),
            leftTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                reservedSize: 40,
                getTitlesWidget: (value, meta) => Text(
                  '${value.toInt()}',
                  style: const TextStyle(fontSize: 9, color: Colors.white38),
                ),
              ),
            ),
            topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          ),
          borderData: FlBorderData(show: false),
          lineBarsData: [
            LineChartBarData(
              spots: spots,
              isCurved: true,
              curveSmoothness: 0.2,
              color: lineColor,
              barWidth: 2,
              dotData: const FlDotData(show: false),
              belowBarData: BarAreaData(
                show: true,
                color: lineColor.withAlpha(30),
              ),
            ),
          ],
          extraLinesData: ExtraLinesData(
            verticalLines: stepRegions
                .skip(1) // no mostrar línea al inicio
                .map((r) => VerticalLine(
                      x: r.start,
                      color: Colors.white24,
                      strokeWidth: 1,
                      dashArray: [4, 4],
                    ))
                .toList(),
          ),
          lineTouchData: LineTouchData(
            touchTooltipData: LineTouchTooltipData(
              getTooltipItems: (spots) => spots
                  .map((s) => LineTooltipItem(
                        '${s.y.toStringAsFixed(1)} $unitLabel',
                        const TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                            fontWeight: FontWeight.w600),
                      ))
                  .toList(),
            ),
          ),
        ),
      ),
    );
  }

  double get _xInterval {
    if (_points.isEmpty) return 60;
    final total = _points.last.elapsedSeconds;
    if (total < 300) return 30;
    if (total < 600) return 60;
    if (total < 1800) return 120;
    return 300;
  }
}

// ─── Helpers ──────────────────────────────────────────────────────────────

class _StatChip extends StatelessWidget {
  final String label;
  final String value;
  const _StatChip({required this.label, required this.value});

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

class _MiniStat extends StatelessWidget {
  final String text;
  const _MiniStat(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 12),
      child: Text(text,
          style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: Colors.white)),
    );
  }
}
