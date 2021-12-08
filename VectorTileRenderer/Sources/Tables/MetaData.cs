using SQLite;

namespace AliFlex.VectorTileRenderer.Sources.Tables
{
    [Table("metadata")]
    public class MetaData
    {
        [Column("name")]
        public string Name { get; set; }
        [Column("value")]
        public string Value { get; set; }
    }
}
