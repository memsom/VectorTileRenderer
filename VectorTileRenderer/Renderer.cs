using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VectorTileRenderer
{
    public class Renderer
    {
        // TODO make it instance based... maybe
        static object cacheLock = new object();

        public async static Task<byte[]> RenderCached(string cachePath, VectorStyle style, ICanvas canvas, int x, int y, double zoom, double sizeX = 512, double sizeY = 512, double scale = 1, List<string> whiteListLayers = null)
        {
            string layerString = whiteListLayers == null ? "" : string.Join(",-", whiteListLayers.ToArray());

            var bundle = new
            {
                style.Hash,
                sizeX,
                sizeY,
                scale,
                layerString,
            };

            lock (cacheLock)
            {
                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(bundle);
            var hash = Utils.Sha256(json).Substring(0, 12); // get 12 digits to avoid fs length issues

            var fileName = x + "x" + y + "-" + zoom + "-" + hash + ".png";
            var path = Path.Combine(cachePath, fileName);

            lock (cacheLock)
            {
                if (File.Exists(path))
                {
                    return LoadBitmap(path);
                }
            }

            var bitmap = await Render(style, canvas, x, y, zoom, sizeX, sizeY, scale, whiteListLayers);

            // save to file in async fashion
            var _t = Task.Run(() =>
              {

                  if (bitmap != null)
                  {
                      try
                      {
                          lock (cacheLock)
                          {
                              if (File.Exists(path))
                              {
                                  return;
                              }

                              using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                              using (var br = new BinaryWriter(fileStream))
                              {
                                  br.Write(bitmap);
                                  br.Flush();
                                  br.Close();
                                  fileStream.Close();
                              }
                          }
                      }
                      catch (Exception e)
                      {
                          return;
                      }
                  }

              });




            return bitmap;
        }

        static byte[] LoadBitmap(string path)
        {
            return File.ReadAllBytes(path);
        }

        public async static Task<byte[]> Render(VectorStyle style, ICanvas canvas, int x, int y, double zoom, double sizeX = 512, double sizeY = 512, double scale = 1, List<string> whiteListLayers = null)
        {
            Dictionary<VTSource, Stream> rasterTileCache = new Dictionary<VTSource, Stream>();
            Dictionary<VTSource, VectorTile> vectorTileCache = new Dictionary<VTSource, VectorTile>();
            Dictionary<string, List<VectorTileLayer>> categorizedVectorLayers = new Dictionary<string, List<VectorTileLayer>>();

            double actualZoom = zoom;

            if (sizeX < 1024)
            {
                var ratio = 1024 / sizeX;
                var zoomDelta = Math.Log(ratio, 2);

                actualZoom = zoom - zoomDelta;
            }

            sizeX *= scale;
            sizeY *= scale;

            canvas.StartDrawing(sizeX, sizeY);

            var visualLayers = new List<VTVisualLayer>();

            // TODO refactor this messy block
            foreach (var layer in style.Layers)
            {
                if (whiteListLayers != null && layer.Type != "background" && layer.SourceLayer != "")
                {
                    if (!whiteListLayers.Contains(layer.SourceLayer))
                    {
                        continue;
                    }
                }
                if (layer.Source != null)
                {
                    if (layer.Source.Type == "vector")
                    {
                        if (!vectorTileCache.ContainsKey(layer.Source))
                        {
                            if (layer.Source.Provider is Sources.IVectorTileSource)
                            {
                                var tile = await (layer.Source.Provider as Sources.IVectorTileSource).GetVectorTile(x, y, (int)zoom);

                                if (tile == null)
                                {
                                    return null;
                                    // throwing exceptions screws up the performance
                                    throw new FileNotFoundException("Could not load tile : " + x + "," + y + "," + zoom + " of " + layer.SourceName);
                                }

                                // magic sauce! :p
                                if (tile.IsOverZoomed)
                                {
                                    canvas.ClipOverflow = true;
                                }

                                //canvas.ClipOverflow = true;

                                vectorTileCache[layer.Source] = tile;

                                // normalize the points from 0 to size
                                foreach (var vectorLayer in tile.Layers)
                                {
                                    foreach (var feature in vectorLayer.Features)
                                    {
                                        foreach (var geometry in feature.Geometry)
                                        {
                                            for (int i = 0; i < geometry.Count; i++)
                                            {
                                                var point = geometry[i];
                                                geometry[i] = new VTPoint(point.X / feature.Extent * sizeX, point.Y / feature.Extent * sizeY);
                                            }
                                        }
                                    }
                                }

                                foreach (var tileLayer in tile.Layers)
                                {
                                    if (!categorizedVectorLayers.ContainsKey(tileLayer.Name))
                                    {
                                        categorizedVectorLayers[tileLayer.Name] = new List<VectorTileLayer>();
                                    }
                                    categorizedVectorLayers[tileLayer.Name].Add(tileLayer);
                                }
                            }
                        }
                    }
                    else if (layer.Source.Type == "raster")
                    {
                        if (!rasterTileCache.ContainsKey(layer.Source))
                        {
                            if (layer.Source.Provider != null)
                            {
                                if (layer.Source.Provider is Sources.ITileSource)
                                {
                                    var tile = await (layer.Source.Provider as Sources.ITileSource).GetTile(x, y, (int)zoom);

                                    if (tile == null)
                                    {
                                        continue;
                                        // throwing exceptions screws up the performance
                                        throw new FileNotFoundException("Could not load tile : " + x + "," + y + "," + zoom + " of " + layer.SourceName);
                                    }

                                    rasterTileCache[layer.Source] = tile;
                                }
                            }
                        }

                        if (rasterTileCache.ContainsKey(layer.Source))
                        {
                            if (style.ValidateLayer(layer, (int)zoom, null))
                            {
                                var brush = style.ParseStyle(layer, scale, new Dictionary<string, object>());

                                if (!brush.Paint.Visibility)
                                {
                                    continue;
                                }

                                visualLayers.Add(new VTVisualLayer()
                                {
                                    Type = VTVisualLayerType.Raster,
                                    RasterStream = rasterTileCache[layer.Source],
                                    Brush = brush,
                                });
                            }
                        }
                    }

                    if (categorizedVectorLayers.ContainsKey(layer.SourceLayer))
                    {
                        var tileLayers = categorizedVectorLayers[layer.SourceLayer];

                        foreach (var tileLayer in tileLayers)
                        {
                            foreach (var feature in tileLayer.Features)
                            {
                                //var geometry = localizeGeometry(feature.Geometry, sizeX, sizeY, feature.Extent);
                                var attributes = new Dictionary<string, object>(feature.Attributes);

                                attributes["$type"] = feature.GeometryType;
                                attributes["$id"] = layer.ID;
                                attributes["$zoom"] = actualZoom;

                                //if ((string)attributes["$type"] == "Point")
                                //{
                                //    if (attributes.ContainsKey("class"))
                                //    {
                                //        if ((string)attributes["class"] == "country")
                                //        {
                                //            if (layer.ID == "country_label")
                                //            {

                                //            }
                                //        }
                                //    }
                                //}

                                if (style.ValidateLayer(layer, actualZoom, attributes))
                                {
                                    var brush = style.ParseStyle(layer, scale, attributes);

                                    if (!brush.Paint.Visibility)
                                    {
                                        continue;
                                    }

                                    visualLayers.Add(new VTVisualLayer()
                                    {
                                        Type = VTVisualLayerType.Vector,
                                        VectorTileFeature = feature,
                                        Geometry = feature.Geometry,
                                        Brush = brush,
                                    });
                                }
                            }
                        }
                    }

                }
                else if (layer.Type == "background")
                {
                    var brushes = style.GetStyleByType("background", actualZoom, scale);
                    foreach (var brush in brushes)
                    {
                        canvas.DrawBackground(brush);
                    }
                }
            }

            // defered rendering to preserve text drawing order
            foreach (var layer in visualLayers.OrderBy(item => item.Brush.ZIndex))
            {
                if (layer.Type == VTVisualLayerType.Vector)
                {
                    var feature = layer.VectorTileFeature;
                    var geometry = layer.Geometry;
                    var brush = layer.Brush;

                    var attributesDict = feature.Attributes.ToDictionary(key => key.Key, value => value.Value);

                    if (!brush.Paint.Visibility)
                    {
                        continue;
                    }

                    try
                    {
                        if (feature.GeometryType == "Point")
                        {
                            foreach (var point in geometry)
                            {
                                canvas.DrawPoint(point.First(), brush);
                            }
                        }
                        else if (feature.GeometryType == "LineString")
                        {
                            foreach (var line in geometry)
                            {
                                canvas.DrawLineString(line, brush);
                            }
                        }
                        else if (feature.GeometryType == "Polygon")
                        {

                            foreach (var polygon in geometry)
                            {
                                canvas.DrawPolygon(polygon, brush);
                            }
                        }
                        else if (feature.GeometryType == "Unknown")
                        {
                            canvas.DrawUnknown(geometry, brush);
                        }
                        else
                        {

                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                else if (layer.Type == VTVisualLayerType.Raster)
                {
                    canvas.DrawImage(layer.RasterStream, layer.Brush);
                    layer.RasterStream.Close();
                }
            }

            foreach (var layer in visualLayers.OrderBy(item => item.Brush.ZIndex).Reverse())
            {
                if (layer.Type == VTVisualLayerType.Vector)
                {
                    var feature = layer.VectorTileFeature;
                    var geometry = layer.Geometry;
                    var brush = layer.Brush;

                    var attributesDict = feature.Attributes.ToDictionary(key => key.Key, value => value.Value);

                    if (!brush.Paint.Visibility)
                    {
                        continue;
                    }

                    if (feature.GeometryType == "Point")
                    {
                        foreach (var point in geometry)
                        {
                            if (brush.Text != null)
                            {
                                canvas.DrawText(point.First(), brush);
                            }
                        }
                    }
                    else if (feature.GeometryType == "LineString")
                    {
                        foreach (var line in geometry)
                        {
                            if (brush.Text != null)
                            {
                                canvas.DrawTextOnPath(line, brush);
                            }
                        }
                    }
                }
            }

            return canvas.FinishDrawing();
        }

        static List<List<VTPoint>> LocalizeGeometry(List<List<VTPoint>> coordinates, double sizeX, double sizeY, double extent)
        {
            return coordinates.Select(list =>
            {
                return list.Select(point =>
                {
                    VTPoint newPoint = new VTPoint(0, 0);

                    var x = Utils.ConvertRange(point.X, 0, extent, 0, sizeX, false);
                    var y = Utils.ConvertRange(point.Y, 0, extent, 0, sizeY, false);

                    newPoint.X = x;
                    newPoint.Y = y;

                    return newPoint;
                }).ToList();
            }).ToList();
        }


    }
}

