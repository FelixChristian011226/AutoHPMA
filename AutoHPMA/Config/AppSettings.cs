using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoHPMA.Config;

public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoHPMA",
        "settings.json");

    [JsonPropertyName("hasShownTermsOfUse")]
    public bool HasShownTermsOfUse { get; set; }

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Light";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "zh-CN";

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // 如果读取失败，返回默认设置
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception)
        {
            // 处理保存失败的情况
        }
    }
}
