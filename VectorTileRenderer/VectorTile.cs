using System.Collections.Generic;
using System.Windows;
using Xamarin.Forms;

namespace VectorTileRenderer
{
    public class VectorTile
    {
        public bool IsOverZoomed { get; set; } = false;
        public List<VectorTileLayer> Layers = new List<VectorTileLayer>();

        public VectorTile ApplyExtent(Rect extent)
        {
            VectorTile newTile = new VectorTile();
            newTile.IsOverZoomed = this.IsOverZoomed;
            
            foreach(var layer in Layers)
            {
                var vectorLayer = new VectorTileLayer
                {
                    Name = layer.Name
                };

                foreach (var feature in layer.Features)
                {
                    var vectorFeature = new VectorTileFeature
                    {
                        Attributes = new Dictionary<string, object>(feature.Attributes),
                        Extent = feature.Extent,
                        GeometryType = feature.GeometryType
                    };

                    var vectorGeometry = new List<List<Point>>();
                    foreach (var geometry in feature.Geometry)
                    {
                        var vectorPoints = new List<Point>();

                        foreach (var point in geometry)
                        {

                            var newX = Utils.ConvertRange(point.X, extent.Left, extent.Right, 0, vectorFeature.Extent);
                            var newY = Utils.ConvertRange(point.Y, extent.Top, extent.Bottom, 0, vectorFeature.Extent);

                            vectorPoints.Add(new Point(newX, newY));
                        }

                        vectorGeometry.Add(vectorPoints);
                    }

                    vectorFeature.Geometry = vectorGeometry;
                    vectorLayer.Features.Add(vectorFeature);
                }

                newTile.Layers.Add(vectorLayer);
            }

            return newTile;
        }
    }

    public class VectorTileLayer
    {
        public string Name { get; set; }

        public List<VectorTileFeature> Features = new List<VectorTileFeature>();
    }

    public class VectorTileFeature
    {
        public double Extent { get; set; }
        public string GeometryType { get; set; }

        public Dictionary<string, object> Attributes = new Dictionary<string, object>();

        public List<List<Point>> Geometry = new List<List<Point>>();
    }
}
