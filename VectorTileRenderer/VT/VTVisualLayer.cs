using System.Collections.Generic;
using System.IO;

namespace VectorTileRenderer
{
    public class VTVisualLayer
    {
        public VTVisualLayerType Type { get; set; }

        public Stream RasterStream { get; set; } = null;

        public VectorTileFeature VectorTileFeature { get; set; } = null;

        public List<List<VTPoint>> Geometry { get; set; } = null;

        public VTBrush Brush { get; set; } = null;
    }
}

