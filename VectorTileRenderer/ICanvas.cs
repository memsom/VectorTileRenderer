using System.Collections.Generic;
using System.IO;

namespace VectorTileRenderer
{
    public interface ICanvas
    {
        bool ClipOverflow { get; set; }

        void StartDrawing(double sizeX, double sizeY);

        void DrawBackground(Brush style);

        void DrawLineString(List<VTPoint> geometry, Brush style);

        void DrawPolygon(List<VTPoint> geometry, Brush style);

        void DrawPoint(VTPoint geometry, Brush style);

        void DrawText(VTPoint geometry, Brush style);

        void DrawTextOnPath(List<VTPoint> geometry, Brush style);

        void DrawImage(Stream imageStream, Brush style);

        void DrawUnknown(List<List<VTPoint>> geometry, Brush style);

        byte[] FinishDrawing();
    }
}
