using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;

namespace VectorTileRenderer
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

