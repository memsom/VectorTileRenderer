using System.Threading.Tasks;

namespace AliFlex.VectorTileRenderer.Sources
{
    public interface IVectorTileSource : ITileSource
    {
        Task<VectorTile> GetVectorTile(int x, int y, int zoom);
    }
}
