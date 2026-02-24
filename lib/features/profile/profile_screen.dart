import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:rowmate/l10n/app_localizations.dart';
import '../../core/database/database_service.dart';
import '../../core/models/workout_session.dart';
import '../history/history_provider.dart';
import 'profile_provider.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final p = context.watch<ProfileProvider>();
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.profileTitle)),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _StravaConnectionCard(p: p, l10n: l10n),
          if (p.isConnected) ...[
            const SizedBox(height: 16),
            _UploadSettingsCard(p: p, l10n: l10n),
            const SizedBox(height: 16),
            _SyncCard(p: p, l10n: l10n),
            const SizedBox(height: 16),
            _PendingUploadsCard(p: p, l10n: l10n),
          ],
        ],
      ),
    );
  }
}

// ─── Strava Connection ────────────────────────────────────────────────────

class _StravaConnectionCard extends StatelessWidget {
  final ProfileProvider p;
  final AppLocalizations l10n;
  const _StravaConnectionCard({required this.p, required this.l10n});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Strava',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 12),
            if (p.isConnected) ...[
              Row(
                children: [
                  if (p.athleteAvatar != null && p.athleteAvatar!.isNotEmpty)
                    CircleAvatar(
                      backgroundImage: NetworkImage(p.athleteAvatar!),
                      radius: 22,
                    )
                  else
                    const CircleAvatar(
                      radius: 22,
                      child: Icon(Icons.person),
                    ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(p.athleteName ?? l10n.profileConnected,
                            style: const TextStyle(
                                fontWeight: FontWeight.w600, fontSize: 15)),
                        Text(l10n.profileConnectedToStrava,
                            style: const TextStyle(
                                fontSize: 12, color: Colors.white54)),
                      ],
                    ),
                  ),
                  TextButton(
                    onPressed: () => _confirmDisconnect(context),
                    child: Text(l10n.profileDisconnect,
                        style: const TextStyle(color: Colors.redAccent)),
                  ),
                ],
              ),
            ] else ...[
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () => p.connectStrava(),
                  icon: const Icon(Icons.link),
                  label: Text(l10n.profileConnectStrava),
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFFFC4C02),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                ),
              ),
            ],
            if (p.lastError != null) ...[
              const SizedBox(height: 8),
              Text(p.lastError!,
                  style: const TextStyle(color: Colors.redAccent, fontSize: 12)),
            ],
          ],
        ),
      ),
    );
  }

  void _confirmDisconnect(BuildContext context) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(l10n.profileDisconnectTitle),
        content: Text(l10n.profileDisconnectContent),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: Text(l10n.cancel)),
          FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: Text(l10n.profileDisconnect)),
        ],
      ),
    );
    if (ok == true) p.disconnectStrava();
  }
}

// ─── Upload Settings ──────────────────────────────────────────────────────

class _UploadSettingsCard extends StatelessWidget {
  final ProfileProvider p;
  final AppLocalizations l10n;
  const _UploadSettingsCard({required this.p, required this.l10n});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(l10n.profileUploadSettings,
                style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 8),
            _PrefTile(
              label: l10n.profileAutoUpload,
              subtitle: l10n.profileAutoUploadDesc,
              value: UploadPreference.auto,
              groupValue: p.uploadPreference,
              onChanged: p.setUploadPreference,
            ),
            _PrefTile(
              label: l10n.profileAskUpload,
              subtitle: l10n.profileAskUploadDesc,
              value: UploadPreference.ask,
              groupValue: p.uploadPreference,
              onChanged: p.setUploadPreference,
            ),
            _PrefTile(
              label: l10n.profileManualUpload,
              subtitle: l10n.profileManualUploadDesc,
              value: UploadPreference.manual,
              groupValue: p.uploadPreference,
              onChanged: p.setUploadPreference,
            ),
          ],
        ),
      ),
    );
  }
}

class _PrefTile extends StatelessWidget {
  final String label;
  final String subtitle;
  final UploadPreference value;
  final UploadPreference groupValue;
  final ValueChanged<UploadPreference> onChanged;

  const _PrefTile({
    required this.label,
    required this.subtitle,
    required this.value,
    required this.groupValue,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return RadioListTile<UploadPreference>(
      title: Text(label, style: const TextStyle(fontSize: 14)),
      subtitle: Text(subtitle, style: const TextStyle(fontSize: 11, color: Colors.white54)),
      value: value,
      groupValue: groupValue,
      onChanged: (v) => onChanged(v!),
      dense: true,
      contentPadding: EdgeInsets.zero,
    );
  }
}

// ─── Sync ────────────────────────────────────────────────────────────────

class _SyncCard extends StatelessWidget {
  final ProfileProvider p;
  final AppLocalizations l10n;
  const _SyncCard({required this.p, required this.l10n});

  @override
  Widget build(BuildContext context) {
    final busy = p.isSyncing || p.isUploading;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(l10n.profileSync,
                style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 12),
            Text(l10n.profileSyncToDesc,
                style: const TextStyle(fontSize: 12, color: Colors.white54)),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: busy
                    ? null
                    : () async {
                        final count = await p.syncToStrava();
                        if (context.mounted) {
                          context.read<HistoryProvider>().load();
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(l10n.profileUploadResult(count)),
                            ),
                          );
                        }
                      },
                icon: p.isUploading
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white),
                      )
                    : const Icon(Icons.cloud_upload),
                label: Text(p.isUploading
                    ? l10n.profileUploading
                    : l10n.profileSyncToStrava),
                style: FilledButton.styleFrom(
                  backgroundColor: const Color(0xFFFC4C02),
                  foregroundColor: Colors.white,
                ),
              ),
            ),
            const SizedBox(height: 16),
            Text(l10n.profileSyncDesc,
                style: const TextStyle(fontSize: 12, color: Colors.white54)),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: busy
                    ? null
                    : () async {
                        final count = await p.syncFromStrava();
                        if (context.mounted) {
                          context.read<HistoryProvider>().load();
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(l10n.profileSyncResult(count)),
                            ),
                          );
                        }
                      },
                icon: p.isSyncing
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.cloud_download),
                label: Text(p.isSyncing
                    ? l10n.profileSyncing
                    : l10n.profileSyncFromStrava),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ─── Pending Uploads ──────────────────────────────────────────────────────

class _PendingUploadsCard extends StatefulWidget {
  final ProfileProvider p;
  final AppLocalizations l10n;
  const _PendingUploadsCard({required this.p, required this.l10n});

  @override
  State<_PendingUploadsCard> createState() => _PendingUploadsCardState();
}

class _PendingUploadsCardState extends State<_PendingUploadsCard> {
  List<WorkoutSession> _pending = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadPending();
  }

  Future<void> _loadPending() async {
    final db = context.read<DatabaseService>();
    final sessions = await db.getSessions();
    setState(() {
      _pending = sessions
          .where((s) => s.stravaActivityId == null && s.finishedAt != null)
          .toList();
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = widget.l10n;
    if (_loading) return const SizedBox.shrink();
    if (_pending.isEmpty) return const SizedBox.shrink();

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(l10n.profilePendingUploads,
                style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
            const SizedBox(height: 8),
            ..._pending.map((s) => _PendingSessionTile(
              session: s,
              onUpload: () async {
                final messenger = ScaffoldMessenger.of(context);
                final ok = await widget.p.uploadSession(s.id!);
                if (ok && mounted) {
                  _loadPending();
                  messenger.showSnackBar(
                    SnackBar(content: Text(l10n.profileUploaded)),
                  );
                }
              },
            )),
          ],
        ),
      ),
    );
  }
}

class _PendingSessionTile extends StatelessWidget {
  final WorkoutSession session;
  final VoidCallback onUpload;
  const _PendingSessionTile({required this.session, required this.onUpload});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          const Icon(Icons.rowing, size: 16, color: Color(0xFF00B4D8)),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(session.routineName ?? AppLocalizations.of(context)!.historyFree,
                    style: const TextStyle(fontSize: 13)),
                Text(session.durationFormatted,
                    style: const TextStyle(fontSize: 11, color: Colors.white54)),
              ],
            ),
          ),
          IconButton(
            icon: const Icon(Icons.cloud_upload_outlined, size: 20),
            onPressed: onUpload,
            tooltip: 'Upload',
          ),
        ],
      ),
    );
  }
}
