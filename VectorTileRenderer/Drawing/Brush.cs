﻿namespace AliFlex.VectorTileRenderer.Drawing
{
    public class Brush
    {
        public int ZIndex { get; set; } = 0;
        public Paint Paint { get; set; }
        public string TextField { get; set; }
        public string Text { get; set; }
        public Layer Layer { get; set; }
    }
}
