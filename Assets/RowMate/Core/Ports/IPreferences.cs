using System;

namespace RowMate.Core.Ports
{
    // Las preferencias de la app con las claves del legacy (ARCHITECTURE §9.4; ADR 021; 02-tech-design §2.7 de la It. 5). Hilo
    // principal. Distingue "no existe" de "existe con otro tipo" solo por dentro: los dos dan false en TryGet* (el llamador usa
    // su valor por defecto, como el `?? false` del legacy). Set* y Remove cambian el valor en memoria EN EL ACTO y piden el
    // guardado, que puede terminar después y fallar sin aviso (AC-WKT-18-11: como el caché de shared_preferences, que cambia
    // antes de escribir).
    // Implementaciones: MemoryPreferences (Core: dotnet, las pruebas, la composición sin archivo) y JsonFilePreferences
    // (Infrastructure: prefs.json en la carpeta de datos de la app). Nunca se leen las preferencias del legacy (Q-11).
    public interface IPreferences
    {
        // false mientras la lectura no terminó (JsonFilePreferences lee al construirse; un doble de prueba puede demorarla).
        bool IsLoaded { get; }

        // Una vez, cuando termina una lectura que no había terminado al construirse.
        event Action? Loaded;

        bool TryGetBool(string key, out bool value);

        bool TryGetString(string key, out string value);

        void SetBool(string key, bool value);

        void SetString(string key, string value);

        void Remove(string key);
    }
}
