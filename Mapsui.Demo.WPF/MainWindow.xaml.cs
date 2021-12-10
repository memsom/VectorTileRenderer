using AliFlex.VectorTileRenderer.Enums;
using Mapsui.Layers;
using Mapsui.Projection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mapsui.Demo.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var items = Enum.GetValues(typeof(VectorStyleKind)).Cast<VectorStyleKind>();

            styleBox.ItemsSource = items;
            styleBox.SelectedIndex = 0;

            var point = new Point(8.542693, 47.368659); // zurich
            var sphericalPoint = SphericalMercator.FromLonLat(point.X, point.Y);

            MyMapControl.Navigator.NavigateTo(sphericalPoint, 12.0);
        }


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var styleName = (VectorStyleKind)styleBox.SelectedItem;
            var mainDir = "../../../";


            var source = new VectorMbTilesSource(mainDir + @"tiles/zurich.mbtiles", mainDir + @"tile-cache/", styleName);
            MyMapControl.Map.Layers.Clear();
            MyMapControl.Map.Layers.Add(new TileLayer(source));
            MyMapControl.Refresh();
        }
    }
}
