using AliFlex.VectorTileRenderer.Enums;
using System.Collections.Generic;
using System.IO;

namespace AliFlex.VectorTileRenderer.Drawing
{
    public class VisualLayer
    {
        public VisualLayerType Type { get; set; }

        public Stream RasterStream { get; set; } = null;

        public VectorTileFeature VectorTileFeature { get; set; } = null;

        public List<List<Point>> Geometry { get; set; } = null;

        public Brush Brush { get; set; } = null;
    }
}

