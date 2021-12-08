using AliFlex.VectorTileRenderer.Drawing;
using System.Collections.Generic;

namespace AliFlex.VectorTileRenderer
{
    public class VectorTileFeature
    {
        public double Extent { get; set; }
        public string GeometryType { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public List<List<Point>> Geometry = new List<List<Point>>();
    }
}
