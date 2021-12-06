using AliFlex.VectorTileRenderer.Sources;

namespace AliFlex.VectorTileRenderer.Drawing
{
    public class Source
    {
        public string URL { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public ITileSource Provider { get; set; } = null;
        public double? MinZoom { get; set; } = null;
        public double? MaxZoom { get; set; } = null;
    }
}
