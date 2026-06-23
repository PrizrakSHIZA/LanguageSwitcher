using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace LanguageSwitcher
{
    [Serializable]
    public sealed class AppSettingsData
    {
        public List<LanguageSetting> Languages { get; set; }
        public string CycleHotkey { get; set; }
        public bool RunAtStartup { get; set; }

        public AppSettingsData()
        {
            Languages = new List<LanguageSetting>();
            CycleHotkey = "Ctrl+Shift";
        }

        public LanguageSetting FindLanguage(string layoutId)
        {
            return Languages.FirstOrDefault(l => string.Equals(l.LayoutId, layoutId, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Serializable]
    public sealed class LanguageSetting
    {
        public string LayoutId { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
        public string Hotkey { get; set; }
    }

    public static class AppSettingsStore
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LanguageSwitcher");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.xml");

        public static AppSettingsData Load(IEnumerable<KeyboardLayoutInfo> installedLayouts)
        {
            AppSettingsData data = null;

            try
            {
                if (File.Exists(SettingsPath))
                {
                    using (var stream = File.OpenRead(SettingsPath))
                    {
                        data = (AppSettingsData)new XmlSerializer(typeof(AppSettingsData)).Deserialize(stream);
                    }
                }
            }
            catch
            {
                data = null;
            }

            if (data == null)
            {
                data = new AppSettingsData();
            }

            MergeInstalledLayouts(data, installedLayouts);
            return data;
        }

        public static void Save(AppSettingsData data)
        {
            Directory.CreateDirectory(SettingsDirectory);
            using (var stream = File.Create(SettingsPath))
            {
                new XmlSerializer(typeof(AppSettingsData)).Serialize(stream, data);
            }
        }

        private static void MergeInstalledLayouts(AppSettingsData data, IEnumerable<KeyboardLayoutInfo> installedLayouts)
        {
            var installed = installedLayouts.ToList();
            var installedIds = new HashSet<string>(installed.Select(l => l.LayoutId), StringComparer.OrdinalIgnoreCase);

            data.Languages.RemoveAll(l => !installedIds.Contains(l.LayoutId));

            foreach (var layout in installed)
            {
                var language = data.FindLanguage(layout.LayoutId);
                if (language == null)
                {
                    data.Languages.Add(new LanguageSetting
                    {
                        LayoutId = layout.LayoutId,
                        DisplayName = layout.DisplayName,
                        Enabled = true,
                        Hotkey = string.Empty
                    });
                }
                else
                {
                    language.DisplayName = layout.DisplayName;
                }
            }
        }
    }
}
