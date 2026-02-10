using System;
using System.IO;
using System.Text.Json;

namespace Crosshair_MY
{
    public class GlobalSettings
    {
        public bool StartMinimized { get; set; } = false;

        // Gələcəkdə bura "Dark Mode" və ya başqa ümumi ayarlar əlavə edə bilərik

        private static string SettingsFile = "app_settings.json";

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }

        public static GlobalSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<GlobalSettings>(json) ?? new GlobalSettings();
                }
            }
            catch { }
            return new GlobalSettings();
        }
    }
}