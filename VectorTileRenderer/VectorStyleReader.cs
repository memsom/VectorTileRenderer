using AliFlex.VectorTileRenderer.Enums;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AliFlex.VectorTileRenderer
{
    public static class VectorStyleReader
    {
        static string[] names = default;

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
            try
            {
                name = name.Replace(' ', '-'); // spaces to dashes
                var assembly = Assembly.GetExecutingAssembly();
                var nsname = assembly.GetName().Name;
                var resourceName = $"{nsname}.Styles.fonts.{name}";

                // init names
                if (names == default)
                {
                    names = assembly.GetManifestResourceNames();
                }

                // get the name from the names list
                var realName = names?.FirstOrDefault(x => x.StartsWith(resourceName));

                if (!string.IsNullOrWhiteSpace(realName))
                {
                    using (var tstream = assembly.GetManifestResourceStream(realName))
                    using (var reader = new StreamReader(tstream))
                    {
                        stream = new MemoryStream();
                        tstream.CopyTo(stream);
                        stream.Seek(0, SeekOrigin.Begin); // make sure it is at stream start
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }

            stream = null;
            return false;
        }
    }
}
