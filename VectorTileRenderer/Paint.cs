using SkiaSharp;

namespace VectorTileRenderer
{
    public class Paint
    {
        public SKColor BackgroundColor { get; set; }
        public string BackgroundPattern { get; set; }
        public double BackgroundOpacity { get; set; } = 1;

        public SKColor FillColor { get; set; }
        public string FillPattern { get; set; }
        public VTPoint FillTranslate { get; set; } = new VTPoint();
        public double FillOpacity { get; set; } = 1;

        public SKColor LineColor { get; set; }
        public string LinePattern { get; set; }
        public VTPoint LineTranslate { get; set; } = new VTPoint();
        public VTPenLineCap LineCap { get; set; } = VTPenLineCap.Flat;
        public double LineWidth { get; set; } = 1;
        public double LineOffset { get; set; } = 0;
        public double LineBlur { get; set; } = 0;
        public double[] LineDashArray { get; set; } = new double[0];
        public double LineOpacity { get; set; } = 1;

        public VTSymbolPlacement SymbolPlacement { get; set; } = VTSymbolPlacement.Point;
        public double IconScale { get; set; } = 1;
        public string IconImage { get; set; }
        public double IconRotate { get; set; } = 0;
        public VTPoint IconOffset { get; set; } = new VTPoint();
        public double IconOpacity { get; set; } = 1;

        public SKColor TextColor { get; set; }
        public string[] TextFont { get; set; } = new string[] { "Open Sans Regular", "Arial Unicode MS Regular" };
        public double TextSize { get; set; } = 16;
        public double TextMaxWidth { get; set; } = 10;
        public VTTextAlignment TextJustify { get; set; } = VTTextAlignment.Center;
        public double TextRotate { get; set; } = 0;
        public VTPoint TextOffset { get; set; } = new VTPoint();
        public SKColor TextStrokeColor { get; set; }
        public double TextStrokeWidth { get; set; } = 0;
        public double TextStrokeBlur { get; set; } = 0;
        public bool TextOptional { get; set; } = false;
        public VTTextTransform TextTransform { get; set; } = VTTextTransform.None;
        public double TextOpacity { get; set; } = 1;

        public bool Visibility { get; set; } = true; // visibility
    }
}
