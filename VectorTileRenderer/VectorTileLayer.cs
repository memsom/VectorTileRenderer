using System.Collections.Generic;

namespace AliFlex.VectorTileRenderer
{
    public class VectorTileLayer
    {
        public string Name { get; set; }

        public List<VectorTileFeature> Features = new List<VectorTileFeature>();
    }
}
