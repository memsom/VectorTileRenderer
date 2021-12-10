using System;
using System.IO;
using System.Linq;

namespace AliFlex.VectorTileRenderer
{
    public class SimpleVectorCache : IVectorCache
    {
        DirectoryInfo directoryInfo = default;
        FileInfo[] files = default;


        public SimpleVectorCache(string path)
        {
            CachePath = path;

            Refresh();
        }

        public int Count
        {
            get
            {
                return files?.Length ?? 0;
            }
        }

        public int MaxFiles { get; set; } = 1024;
        public string CachePath { get; set; }

        public void Refresh()
        {
            if (directoryInfo == default)
            {
                directoryInfo = new DirectoryInfo(CachePath);
            }
            else
            {
                directoryInfo.Refresh();
            }

            files = directoryInfo.GetFiles();

            var count = Count;

            if (count > MaxFiles)
            {
                var cullList = files.OrderBy(x => x.CreationTime)
                                    .Take(count - MaxFiles)
                                    .ToArray();

                foreach (var file in cullList)
                {
                    if (file.Exists)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception)
                        {
                            // file is most likely locked
                        }
                    }
                }
            }
        }
    }
}

