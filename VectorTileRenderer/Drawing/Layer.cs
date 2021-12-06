using System.Collections.Generic;

namespace AliFlex.VectorTileRenderer.Drawing
{
    public class Layer
    {
        public int Index { get; set; } = -1;
        public string ID { get; set; } = "";
        public string Type { get; set; } = "";
        public string SourceName { get; set; } = "";
        public Source Source { get; set; } = null;
        public string SourceLayer { get; set; } = "";
        public Dictionary<string, object> Paint { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Layout { get; set; } = new Dictionary<string, object>();
        public object[] Filter { get; set; } = new object[0];
        public double? MinZoom { get; set; } = null;
        public double? MaxZoom { get; set; } = null;
    }
}
