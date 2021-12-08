using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using System.IO;
using Android.Content.Res;
using SkiaSharp;

namespace TileTest.Droid
{
    [Activity(Label = "TileTest", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        readonly string mapName = "zurich.mbtiles";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // move the resources
            var basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            // where the map will live
            var mapPath = Path.Combine(basePath, "map");
            // what the app wants to find
            NativePaths.MapPath = Path.Combine(mapPath, mapName);
            // where we put tiles
            NativePaths.CachePath = Path.Combine(basePath, "tile-cache");
            // create the directories if they don't exist
            Directory.CreateDirectory(mapPath);
            Directory.CreateDirectory(NativePaths.CachePath);
            
            // copy the map from the Assets
            if(!File.Exists(NativePaths.MapPath) && this.Assets is AssetManager assets)
            {
                using (var source = assets.Open(mapName))
                using (var dest = File.Create(NativePaths.MapPath))
                {
                    var buffer = new byte[4096];

                    int b = buffer.Length;
                    int length;

                    while ((length = source.Read(buffer, 0, b)) > 0)
{
                        dest.Write(buffer, 0, length);
                    }
                    dest.Flush();
                    dest.Close();
                    source.Close();
                }
            }

#if DEBUG
            // when we DEBUG, we clear the tile cache to make it easier to see what is going on with rendering.
            var files = Directory.GetFiles(NativePaths.CachePath, "*.png");
            foreach(var file in files)
            {
                File.Delete(file);
            }
#endif



            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}