using BruTile;
using System;
using VectorTileRenderer;

namespace Mapsui.Demo.WPF
{
    class VectorMbTilesProvider : ITileProvider
    {

        Style style;
        VectorTileRenderer.Sources.MbTilesSource provider;
        string cachePath;

        public VectorMbTilesProvider(string path, string stylePath, string cachePath)
        {
            this.cachePath = cachePath;
            style = new Style(stylePath);
            style.FontDirectory = @"styles/fonts/";

            provider = new VectorTileRenderer.Sources.MbTilesSource(path);
            style.SetSourceProvider("openmaptiles", provider);

        }
        
        public byte[] GetTile(TileInfo tileInfo)
        {
            //var newY = (int)Math.Pow(2, zoom) - pos.Y - 1;

            var canvas = new SkiaCanvas();

            try
            {
                return Renderer.RenderCached(cachePath, style, canvas, (int)tileInfo.Index.Col, (int)tileInfo.Index.Row, Convert.ToInt32(tileInfo.Index.Level), 256, 256, 1).Result;
            }
            catch
            {
                return null;
            }
        }
    }
}
