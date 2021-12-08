using AliFlex.VectorTileRenderer;
using AliFlex.VectorTileRenderer.Enums;
using AliFlex.VectorTileRenderer.Sources;
using BruTile;
using System;

namespace TileTest
{
    class VectorMbTilesProvider : ITileProvider
    {

        VectorStyle style;
        MbTilesSource provider;
        IVectorCache cache;

        public VectorMbTilesProvider(string path, string cachePath, VectorStyleKind kind, string customStyle = default)
        {
            cache = new SimpleVectorCache(cachePath); 
            style = new VectorStyle(kind)
            {
                CustomStyle = customStyle
            };

            provider = new MbTilesSource(path);
            style.SetSourceProvider("openmaptiles", provider);
        }

        public byte[] GetTile(TileInfo tileInfo)
        {
            //var newY = (int)Math.Pow(2, zoom) - pos.Y - 1;

            var canvas = new SkiaCanvas();

            try
            {
                return Renderer.RenderCached(cache, style, canvas, (int)tileInfo.Index.Col, (int)tileInfo.Index.Row, Convert.ToInt32(tileInfo.Index.Level), 256, 256, 1).Result;
            }
            catch
            {
                return null;
            }
        }
    }
}
