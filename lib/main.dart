import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:rowmate/l10n/app_localizations.dart';
import 'package:provider/provider.dart';
import 'package:wakelock_plus/wakelock_plus.dart';

import 'core/bluetooth/ble_service.dart';
import 'core/bluetooth/hrm_service.dart';
import 'core/database/database_service.dart';
import 'features/device/device_provider.dart';
import 'features/device/device_screen.dart';
import 'features/history/history_provider.dart';
import 'features/history/history_screen.dart';
import 'features/routines/routines_provider.dart';
import 'features/routines/routines_screen.dart';
import 'features/workout/workout_provider.dart';
import 'features/workout/workout_screen.dart';
import 'shared/theme.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  WakelockPlus.enable();
  runApp(const RowerApp());
}

class RowerApp extends StatelessWidget {
  const RowerApp({super.key});

  @override
  Widget build(BuildContext context) {
    final ble = BleService();
    final hrm = HrmService();
    final db = DatabaseService();

    return MultiProvider(
      providers: [
        Provider<BleService>(
          create: (_) => ble,
          dispose: (_, s) => s.dispose(),
        ),
        Provider<HrmService>(
          create: (_) => hrm,
          dispose: (_, s) => s.dispose(),
        ),
        Provider<DatabaseService>(
          create: (_) => db,
          dispose: (_, s) => s.close(),
        ),
        ChangeNotifierProvider(create: (_) => DeviceProvider(ble, hrm)),
        ChangeNotifierProvider(create: (_) => WorkoutProvider(ble, db)),
        ChangeNotifierProvider(create: (_) => RoutinesProvider(db)),
        ChangeNotifierProvider(create: (_) => HistoryProvider(db)),
      ],
      child: MaterialApp(
        title: 'RowMate',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(),
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        supportedLocales: const [
          Locale('en'),
          Locale('es'),
        ],
        home: const MainShell(),
      ),
    );
  }
}

class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _tab = 0;

  static const _screens = [
    DeviceScreen(),
    WorkoutScreen(),
    RoutinesScreen(),
    HistoryScreen(),
  ];

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Scaffold(
      body: IndexedStack(index: _tab, children: _screens),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (i) {
          if (i == 3) context.read<HistoryProvider>().load();
          setState(() => _tab = i);
        },
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.bluetooth),
            selectedIcon: const Icon(Icons.bluetooth_connected),
            label: l10n.navDevice,
          ),
          NavigationDestination(
            icon: const Icon(Icons.timer_outlined),
            selectedIcon: const Icon(Icons.timer),
            label: l10n.navWorkout,
          ),
          NavigationDestination(
            icon: const Icon(Icons.fitness_center),
            selectedIcon: const Icon(Icons.fitness_center),
            label: l10n.navRoutines,
          ),
          NavigationDestination(
            icon: const Icon(Icons.history),
            selectedIcon: const Icon(Icons.history),
            label: l10n.navHistory,
          ),
        ],
      ),
    );
  }
}
