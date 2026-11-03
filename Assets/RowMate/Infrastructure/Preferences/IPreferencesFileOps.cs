using System.IO;
using System.Text;

namespace RowMate.Infrastructure.Preferences
{
    // Las operaciones de archivo de JsonFilePreferences (02-tech-design §1.1 de la It. 5): las de System.IO en la app; en las
    // pruebas, unas que fallan donde se les pide (leer, escribir el temporal, ponerlo en lugar del archivo) o que se demoran,
    // siempre sobre una carpeta temporal propia (AC-HIS-20-04).
    internal interface IPreferencesFileOps
    {
        bool Exists(string path);

        // El texto entero (UTF-8; un BOM inicial se ignora). Lanza si no se puede leer.
        string ReadAllText(string path);

        void WriteAllBytes(string path, byte[] bytes);

        // Pone source en lugar de destination, que ya existe (File.Replace: el viejo o el nuevo, nunca uno a medias).
        void Replace(string source, string destination);

        // Pone source donde no hay archivo (File.Move).
        void Move(string source, string destination);

        void Delete(string path);
    }

    internal sealed class SystemPreferencesFileOps : IPreferencesFileOps
    {
        public static readonly SystemPreferencesFileOps Instance = new SystemPreferencesFileOps();

        private SystemPreferencesFileOps()
        {
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path, new UTF8Encoding(false));
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        public void Replace(string source, string destination)
        {
            File.Replace(source, destination, null);
        }

        public void Move(string source, string destination)
        {
            File.Move(source, destination);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }
    }
}
