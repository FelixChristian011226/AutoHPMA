using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHPMA.Helpers.DataHelper
{
    public static class CacheHelper
    {
        private static readonly string BaseCacheDir = Path.Combine(Path.GetTempPath(), "AutoHPMA");

        public static string GetBaseCacheDir()
        {
            EnsureDirectoryExists(BaseCacheDir);
            return BaseCacheDir;
        }

        public static string GetSubCacheDir(string subDirName)
        {
            string subDir = Path.Combine(BaseCacheDir, subDirName);
            EnsureDirectoryExists(subDir);
            return subDir;
        }

        public static void ClearAllCache()
        {
            if (Directory.Exists(BaseCacheDir))
            {
                try
                {
                    Directory.Delete(BaseCacheDir, recursive: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清理缓存失败: {ex.Message}");
                }
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
