using AliFlex.VectorTileRenderer.Enums;
using System;
using System.IO;
using System.Reflection;

namespace AliFlex.VectorTileRenderer
{
    public static class VectorStyleReader
    {
        public static string GetStyle(VectorStyleKind styleKind)
        {
            var name = styleKind.ToString().ToLower();
            var assembly = Assembly.GetExecutingAssembly();
            var nsname = assembly.GetName().Name;
            var resourceName = $"{nsname}.Styles.{name}-style.json";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool TryGetFont(string name, out Stream stream)
        {
            var result = false;
            try
            {
                name = name.Replace(' ', '-'); // spaces to dashes
                var assembly = Assembly.GetExecutingAssembly();
                var nsname = assembly.GetName().Name;
                var resourceName = $"{nsname}.Styles.fonts.{name}";
                using (var tstream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(tstream))
                {
                    stream = new MemoryStream();
                    tstream.CopyTo(stream);
                    stream.Seek(0, SeekOrigin.Begin); // make sure it is at stream start
                    result = true;
                }
            }
            catch (Exception)
            {
                stream = null;
                result = false;
            }

            return result;
        }
    }
}
