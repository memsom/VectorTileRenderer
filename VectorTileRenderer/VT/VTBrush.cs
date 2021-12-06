namespace VectorTileRenderer
{
    public class VTBrush
    {
        public int ZIndex { get; set; } = 0;
        public VTPaint Paint { get; set; }
        public string TextField { get; set; }
        public string Text { get; set; }
        //public string GlyphsDirectory { get; set; } = null;
        public VTLayer Layer { get; set; }
    }
}
