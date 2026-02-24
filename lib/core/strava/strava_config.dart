import 'strava_secrets.dart';

/// Configuración de la API de Strava
class StravaConfig {
  static const clientId = stravaClientId;
  static const clientSecret = stravaClientSecret;
  static const redirectUri = 'rowmate://rowmate';
  static const scopes = 'activity:read_all,activity:write';

  static const authorizeUrl = 'https://www.strava.com/oauth/mobile/authorize';
  static const tokenUrl = 'https://www.strava.com/oauth/token';
  static const apiBase = 'https://www.strava.com/api/v3';

  /// True si el usuario configuró sus credenciales de Strava
  static bool get isConfigured =>
      clientSecret.isNotEmpty &&
      clientSecret != 'TU_CLIENT_SECRET';
}
