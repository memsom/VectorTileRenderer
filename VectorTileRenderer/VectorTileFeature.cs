using System.Collections.Generic;

namespace VectorTileRenderer
{
    public class VectorTileFeature
    {
        public double Extent { get; set; }
        public string GeometryType { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public List<List<VTPoint>> Geometry = new List<List<VTPoint>>();
    }
}
