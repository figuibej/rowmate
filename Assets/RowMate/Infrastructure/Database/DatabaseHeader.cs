using System;
using System.IO;

namespace RowMate.Infrastructure.Database
{
    // Los primeros 100 bytes de un archivo de SQLite (https://www.sqlite.org/fileformat.html §1.3), leídos con E/S común y
    // FileShare.ReadWrite ANTES de abrir con SQLite (02-tech-design §2.3): así un archivo que no es una base nunca pasa por
    // SQLite en modo escritura.
    // - Un archivo de 1 a 99 bytes es Unreadable, empiece o no con la firma (encabezado incompleto: QA-G3-13 e).
    // - user_version: offset 60, big-endian, con signo.
    internal readonly struct DatabaseHeader
    {
        public const int Size = 100;
        public const int UserVersionOffset = 60;

        // "SQLite format 3\0".
        private static readonly byte[] Signature = { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00 };

        private DatabaseHeader(long length, bool complete, bool hasSignature, int userVersion)
        {
            Length = length;
            IsComplete = complete;
            HasSignature = hasSignature;
            UserVersion = userVersion;
        }

        // Largo del archivo en bytes.
        public long Length { get; }

        // Se leyeron los 100 bytes.
        public bool IsComplete { get; }

        public bool HasSignature { get; }

        // Solo vale con IsComplete y HasSignature.
        public int UserVersion { get; }

        // Archivo vacío (0 bytes): SQLite lo trata como una base vacía.
        public bool IsEmpty => Length == 0;

        // Lee el encabezado; IOException (o UnauthorizedAccessException) si no se puede abrir.
        public static DatabaseHeader Read(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var length = stream.Length;
                var buffer = new byte[Size];
                var read = 0;
                while (read < Size)
                {
                    var n = stream.Read(buffer, read, Size - read);
                    if (n == 0)
                    {
                        break;
                    }

                    read += n;
                }

                var complete = read == Size;
                var hasSignature = read >= Signature.Length && StartsWithSignature(buffer);
                var userVersion = complete
                    ? (buffer[UserVersionOffset] << 24) | (buffer[UserVersionOffset + 1] << 16) | (buffer[UserVersionOffset + 2] << 8)
                      | buffer[UserVersionOffset + 3]
                    : 0;
                return new DatabaseHeader(length, complete, hasSignature, userVersion);
            }
        }

        private static bool StartsWithSignature(byte[] buffer)
        {
            for (var i = 0; i < Signature.Length; i++)
            {
                if (buffer[i] != Signature[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
