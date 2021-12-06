namespace VectorTileRenderer
{
    public class VTSource
    {
        public string URL { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public Sources.ITileSource Provider { get; set; } = null;
        public double? MinZoom { get; set; } = null;
        public double? MaxZoom { get; set; } = null;
    }
}
