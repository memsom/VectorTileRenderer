using AliFlex.VectorTileRenderer.Enums;
using Mapsui.Layers;
using Mapsui.Projection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TileTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            var items = Enum.GetValues(typeof(VectorStyleKind)).Cast<VectorStyleKind>();

            styleBox.ItemsSource = items.ToList();
            styleBox.SelectedIndex = 0;

            var point = new Point(8.542693, 47.368659);
            var sphericalPoint = SphericalMercator.FromLonLat(point.X, point.Y);

            MyMapControl.Navigator.NavigateTo(sphericalPoint, 12.0);
        }

        void styleBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var styleName = (VectorStyleKind)styleBox.SelectedItem;

            var source = new VectorMbTilesSource(NativePaths.MapPath, NativePaths.CachePath, styleName);
            MyMapControl.Map.Layers.Clear();
            MyMapControl.Map.Layers.Add(new TileLayer(source));
            MyMapControl.Refresh();
        }
    }

    public static class NativePaths
    {
        public static string MapPath { get; set; }
        public static string CachePath { get; set; }
    }
}
