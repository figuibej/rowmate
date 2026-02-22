import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:wakelock_plus/wakelock_plus.dart';

import 'core/bluetooth/ble_service.dart';
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
  WakelockPlus.enable(); // pantalla siempre activa durante el remo
  runApp(const RowerApp());
}

class RowerApp extends StatelessWidget {
  const RowerApp({super.key});

  @override
  Widget build(BuildContext context) {
    final ble = BleService();
    final db = DatabaseService();

    return MultiProvider(
      providers: [
        Provider<BleService>(
          create: (_) => ble,
          dispose: (_, s) => s.dispose(),
        ),
        Provider<DatabaseService>(
          create: (_) => db,
          dispose: (_, s) => s.close(),
        ),
        ChangeNotifierProvider(create: (_) => DeviceProvider(ble)),
        ChangeNotifierProvider(create: (_) => WorkoutProvider(ble, db)),
        ChangeNotifierProvider(create: (_) => RoutinesProvider(db)),
        ChangeNotifierProvider(create: (_) => HistoryProvider(db)),
      ],
      child: MaterialApp(
        title: 'Rower Monitor',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(),
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
    return Scaffold(
      body: IndexedStack(index: _tab, children: _screens),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (i) {
          // Recarga el historial al abrir la pestaña
          if (i == 3) context.read<HistoryProvider>().load();
          setState(() => _tab = i);
        },
        destinations: const [
          NavigationDestination(
            icon: Icon(Icons.bluetooth),
            selectedIcon: Icon(Icons.bluetooth_connected),
            label: 'Dispositivo',
          ),
          NavigationDestination(
            icon: Icon(Icons.timer_outlined),
            selectedIcon: Icon(Icons.timer),
            label: 'Workout',
          ),
          NavigationDestination(
            icon: Icon(Icons.fitness_center),
            selectedIcon: Icon(Icons.fitness_center),
            label: 'Rutinas',
          ),
          NavigationDestination(
            icon: Icon(Icons.history),
            selectedIcon: Icon(Icons.history),
            label: 'Historial',
          ),
        ],
      ),
    );
  }
}
