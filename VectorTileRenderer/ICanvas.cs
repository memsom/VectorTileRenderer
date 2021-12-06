using System.Collections.Generic;
using System.IO;

namespace VectorTileRenderer
{
    public interface ICanvas
    {
        bool ClipOverflow { get; set; }

        void StartDrawing(double sizeX, double sizeY);

        void DrawBackground(VTBrush style);

        void DrawLineString(List<VTPoint> geometry, VTBrush style);

        void DrawPolygon(List<VTPoint> geometry, VTBrush style);

        void DrawPoint(VTPoint geometry, VTBrush style);

        void DrawText(VTPoint geometry, VTBrush style);

        void DrawTextOnPath(List<VTPoint> geometry, VTBrush style);

        void DrawImage(Stream imageStream, VTBrush style);

        void DrawUnknown(List<List<VTPoint>> geometry, VTBrush style);

        byte[] FinishDrawing();
    }
}
