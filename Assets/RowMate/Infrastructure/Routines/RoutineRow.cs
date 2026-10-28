using SQLite;

namespace RowMate.Infrastructure.Routines
{
    // Una fila de `routines` como la lee sqlite-net (Query<T> genérico: sin MakeGenericMethod en IL2CPP, QA §6). Los
    // atributos [Column] del paquete de Unity heredan de PreserveAttribute (link.xml cubre el resto).
    [Table("routines")]
    internal sealed class RoutineRow
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public string? CreatedAt { get; set; }
    }
}
