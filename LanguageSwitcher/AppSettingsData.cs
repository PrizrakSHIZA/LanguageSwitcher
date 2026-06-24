using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Xml.Serialization;

namespace LanguageSwitcher
{
    [Serializable]
    public sealed class AppSettingsData
    {
        public List<LanguageSetting> Languages { get; set; }
        public string CycleHotkey { get; set; }
        public string WindowsFallbackHotkey { get; set; }
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
                data.WindowsFallbackHotkey = DetectWindowsLanguageHotkey();
            }
            else if (string.IsNullOrWhiteSpace(data.WindowsFallbackHotkey))
            {
                data.WindowsFallbackHotkey = DetectWindowsLanguageHotkey();
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

            MigrateLegacyLayoutIds(data, installedIds);
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

        private static void MigrateLegacyLayoutIds(AppSettingsData data, HashSet<string> installedIds)
        {
            foreach (LanguageSetting language in data.Languages)
            {
                if (string.IsNullOrWhiteSpace(language.LayoutId) || installedIds.Contains(language.LayoutId))
                {
                    continue;
                }

                string languageId = language.LayoutId.Length >= 4
                    ? language.LayoutId.Substring(language.LayoutId.Length - 4)
                    : language.LayoutId;

                if (installedIds.Contains(languageId))
                {
                    language.LayoutId = languageId;
                }
            }

            var duplicates = data.Languages
                .GroupBy(l => l.LayoutId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var duplicateGroup in duplicates)
            {
                LanguageSetting primary = duplicateGroup.First();
                foreach (LanguageSetting duplicate in duplicateGroup.Skip(1).ToList())
                {
                    if (!duplicate.Enabled)
                    {
                        primary.Enabled = false;
                    }

                    if (string.IsNullOrWhiteSpace(primary.Hotkey) && !string.IsNullOrWhiteSpace(duplicate.Hotkey))
                    {
                        primary.Hotkey = duplicate.Hotkey;
                    }

                    data.Languages.Remove(duplicate);
                }
            }
        }

        private static string DetectWindowsLanguageHotkey()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Toggle"))
            {
                string value = key == null ? null : key.GetValue("Language Hotkey") as string;
                switch (value)
                {
                    case "1":
                        return "Alt+Shift";
                    case "2":
                        return "Ctrl+Shift";
                    case "4":
                        return "Oemtilde";
                    default:
                        return "Ctrl+Shift";
                }
            }
        }
    }
}
