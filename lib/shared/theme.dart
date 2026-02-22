import 'package:flutter/material.dart';

const _seed = Color(0xFF0077B6); // azul agua

ThemeData buildTheme() => ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: _seed,
        brightness: Brightness.dark,
      ),
      scaffoldBackgroundColor: const Color(0xFF0D1B2A),
      cardTheme: CardThemeData(
        color: const Color(0xFF1A2E45),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        elevation: 0,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: Color(0xFF0D1B2A),
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: Color(0xFF12243A),
        selectedItemColor: Color(0xFF00B4D8),
        unselectedItemColor: Color(0xFF607D8B),
      ),
    );

// Colores por tipo de paso
Color stepColor(String typeName) {
  switch (typeName) {
    case 'work':
      return const Color(0xFFEF476F);
    case 'rest':
      return const Color(0xFF06D6A0);
    case 'warmup':
      return const Color(0xFFFFD166);
    case 'cooldown':
      return const Color(0xFF118AB2);
    default:
      return const Color(0xFF607D8B);
  }
}
