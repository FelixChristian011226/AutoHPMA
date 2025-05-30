using System;
using System.IO;
using System.Text.Json;

namespace AutoHPMA.Config
{
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoHPMA",
            "settings.json");

        public bool HasShownTermsOfUse { get; set; }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // 如果读取失败，返回默认设置
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // 保存失败时的处理
            }
        }
    }
} 