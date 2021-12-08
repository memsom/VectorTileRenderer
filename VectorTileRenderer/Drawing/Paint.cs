using AliFlex.VectorTileRenderer.Enums;
using SkiaSharp;

namespace AliFlex.VectorTileRenderer.Drawing
{
    public class Paint
    {
        public SKColor BackgroundColor { get; set; }
        public string BackgroundPattern { get; set; }
        public double BackgroundOpacity { get; set; } = 1;

        public SKColor FillColor { get; set; }
        public string FillPattern { get; set; }
        public Point FillTranslate { get; set; } = new Point();
        public double FillOpacity { get; set; } = 1;

        public SKColor LineColor { get; set; }
        public string LinePattern { get; set; }
        public Point LineTranslate { get; set; } = new Point();
        public PenLineCap LineCap { get; set; } = PenLineCap.Flat;
        public double LineWidth { get; set; } = 1;
        public double LineOffset { get; set; } = 0;
        public double LineBlur { get; set; } = 0;
        public double[] LineDashArray { get; set; } = new double[0];
        public double LineOpacity { get; set; } = 1;

        public SymbolPlacement SymbolPlacement { get; set; } = SymbolPlacement.Point;
        public double IconScale { get; set; } = 1;
        public string IconImage { get; set; }
        public double IconRotate { get; set; } = 0;
        public Point IconOffset { get; set; } = new Point();
        public double IconOpacity { get; set; } = 1;

        public SKColor TextColor { get; set; }
        public string[] TextFont { get; set; } = new string[] { "Open Sans Regular", "Arial Unicode MS Regular" };
        public double TextSize { get; set; } = 16;
        public double TextMaxWidth { get; set; } = 10;
        public TextAlignment TextJustify { get; set; } = TextAlignment.Center;
        public double TextRotate { get; set; } = 0;
        public Point TextOffset { get; set; } = new Point();
        public SKColor TextStrokeColor { get; set; }
        public double TextStrokeWidth { get; set; } = 0;
        public double TextStrokeBlur { get; set; } = 0;
        public bool TextOptional { get; set; } = false;
        public TextTransform TextTransform { get; set; } = TextTransform.None;
        public double TextOpacity { get; set; } = 1;

        public bool Visibility { get; set; } = true; // visibility
    }
}
