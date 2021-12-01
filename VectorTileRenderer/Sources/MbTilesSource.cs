using SQLite;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VectorTileRenderer.Sources
{
    // MbTiles loading code in GIST by geobabbler
    // https://gist.github.com/geobabbler/9213392

    public class MbTilesSource : IVectorTileSource
    {
        public GlobalMercator.GeoExtent Bounds { get; private set; }
        public GlobalMercator.CoordinatePair Center { get; private set; }
        public int MinZoom { get; private set; }
        public int MaxZoom { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string MBTilesVersion { get; private set; }
        public string Path { get; private set; }

        ConcurrentDictionary<string, VectorTile> tileCache = new ConcurrentDictionary<string, VectorTile>();

        readonly GlobalMercator gmt = new GlobalMercator();
        readonly SQLiteConnection sharedConnection;

        // converted to use Sqlite-Net
        public MbTilesSource(string path)
        {
            this.Path = path;

            var connectionstring = new SQLiteConnectionString(this.Path, SQLiteOpenFlags.ReadOnly, false);
            sharedConnection = new SQLiteConnection(connectionstring);

            LoadMetadata();
        }

        // converted to use Sqlite-Net
        void LoadMetadata()
        {
            try
            {
                foreach (var item in sharedConnection.Table<MetaData>())
                {
                    string name = item.Name;
                    switch (name.ToLower())
                    {
                        case "bounds":
                            string val = item.Value;
                            string[] vals = val.Split(new char[] { ',' });
                            this.Bounds = new GlobalMercator.GeoExtent() { West = Convert.ToDouble(vals[0]), South = Convert.ToDouble(vals[1]), East = Convert.ToDouble(vals[2]), North = Convert.ToDouble(vals[3]) };
                            break;
                        case "center":
                            val = item.Value;
                            vals = val.Split(new char[] { ',' });
                            this.Center = new GlobalMercator.CoordinatePair() { X = Convert.ToDouble(vals[0]), Y = Convert.ToDouble(vals[1]) };
                            break;
                        case "minzoom":
                            this.MinZoom = Convert.ToInt32(item.Value);
                            break;
                        case "maxzoom":
                            this.MaxZoom = Convert.ToInt32(item.Value);
                            break;
                        case "name":
                            this.Name = item.Value;
                            break;
                        case "description":
                            this.Description = item.Value;
                            break;
                        case "version":
                            this.MBTilesVersion = item.Value;
                            break;

                    }
                }

            }
            catch (Exception e)
            {
                throw new MemberAccessException("Could not load Mbtiles source file");
            }
        }

        // converted to use Sqlite-Net
        public Stream GetRawTile(int x, int y, int zoom)
        {
            try
            {
                var found = sharedConnection.Table<Tiles>().FirstOrDefault(t => t.X == x && t.Y == y && t.Zoom == zoom);

                if (found is Tiles tile)
                {
                    var data = found.TileData;
                    var stream = new MemoryStream(data);
                    return stream;
                }
            }
            catch
            {
                throw new MemberAccessException("Could not load tile from Mbtiles");
            }

            return null;
        }

        public void ExtractTile(int x, int y, int zoom, string path)
        {
            if (File.Exists(path))
                System.IO.File.Delete(path);

            using (var fileStream = File.Create(path))
            using (Stream tileStream = GetRawTile(x, y, zoom))
            {
                tileStream.Seek(0, SeekOrigin.Begin);
                tileStream.CopyTo(fileStream);
            }
        }

        public async Task<VectorTile> GetVectorTile(int x, int y, int zoom)
        {
            var extent = new Rect(0, 0, 1, 1);
            bool overZoomed = false;

            if (zoom > MaxZoom)
            {
                var bounds = gmt.TileLatLonBounds(x, y, zoom);

                var northEast = new GlobalMercator.CoordinatePair();
                northEast.X = bounds.East;
                northEast.Y = bounds.North;

                var northWest = new GlobalMercator.CoordinatePair();
                northWest.X = bounds.West;
                northWest.Y = bounds.North;

                var southEast = new GlobalMercator.CoordinatePair();
                southEast.X = bounds.East;
                southEast.Y = bounds.South;

                var southWest = new GlobalMercator.CoordinatePair();
                southWest.X = bounds.West;
                southWest.Y = bounds.South;

                var center = new GlobalMercator.CoordinatePair();
                center.X = (northEast.X + southWest.X) / 2;
                center.Y = (northEast.Y + southWest.Y) / 2;

                var biggerTile = gmt.LatLonToTile(center.Y, center.X, MaxZoom);

                var biggerBounds = gmt.TileLatLonBounds(biggerTile.X, biggerTile.Y, MaxZoom);

                var newL = Utils.ConvertRange(northWest.X, biggerBounds.West, biggerBounds.East, 0, 1);
                var newT = Utils.ConvertRange(northWest.Y, biggerBounds.North, biggerBounds.South, 0, 1);

                var newR = Utils.ConvertRange(southEast.X, biggerBounds.West, biggerBounds.East, 0, 1);
                var newB = Utils.ConvertRange(southEast.Y, biggerBounds.North, biggerBounds.South, 0, 1);

                extent = new Rect(new Point(newL, newT), new Size(newR - newL, newB - newT)); //TODO - is this correct?
                //thisZoom = MaxZoom;

                x = biggerTile.X;
                y = biggerTile.Y;
                zoom = MaxZoom;

                overZoomed = true;
            }

            try
            {
                var actualTile = await GetCachedVectorTile(x, y, zoom);

                if (actualTile != null)
                {
                    actualTile.IsOverZoomed = overZoomed;
                    actualTile = actualTile.ApplyExtent(extent);
                }

                return actualTile;

            }
            catch (Exception e)
            {
                return null;
            }
        }

        async Task<VectorTile> GetCachedVectorTile(int x, int y, int zoom)
        {
            return await Task.Run(() =>
            {
                var key = x.ToString() + "," + y.ToString() + "," + zoom.ToString();

                lock (key)
                {
                    if (tileCache.ContainsKey(key))
                    {
                        return tileCache[key];
                    }

                    using (var rawTileStream = GetRawTile(x, y, zoom))
                    {
                        var pbfTileProvider = new PbfTileSource(rawTileStream);
                        var tile = pbfTileProvider.GetVectorTile(x, y, zoom).Result;
                        tileCache[key] = tile;

                        return tile;
                    }
                }
            });
        }

        async Task<Stream> ITileSource.GetTile(int x, int y, int zoom)
        {
            return await Task.Run(() =>
            {
                return GetRawTile(x, y, zoom);
            });
        }
    }
}
