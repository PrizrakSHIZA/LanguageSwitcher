using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LanguageSwitcher
{
    public partial class SettingsForm : Form
    {
        private readonly AppSettingsData originalSettings;
        private readonly AppSettingsData workingSettings;
        private string selectedLanguageId;

        public event EventHandler SettingsSaved;

        public SettingsForm(AppSettingsData settings)
        {
            InitializeComponent();

            originalSettings = settings;
            workingSettings = Clone(settings);

            cycleHotkeyBox.SetHotkey(Hotkey.Parse(workingSettings.CycleHotkey));
            fallbackHotkeyBox.SetHotkey(Hotkey.Parse(workingSettings.WindowsFallbackHotkey));
            chkRunAtStartup.Checked = workingSettings.RunAtStartup;

            WireEvents();
            ReloadLists();
        }

        private void WireEvents()
        {
            lbActiveSwitch.SelectedIndexChanged += OnLanguageSelectionChanged;
            lbDisabledSwitch.SelectedIndexChanged += OnLanguageSelectionChanged;
            btnDisableLanguage.Click += delegate { MoveSelected(false); };
            btnEnableLanguage.Click += delegate { MoveSelected(true); };
            btnMoveUp.Click += delegate { MoveSelectedActiveLanguage(-1); };
            btnMoveDown.Click += delegate { MoveSelectedActiveLanguage(1); };
            btnClearLanguageHotkey.Click += delegate { languageHotkeyBox.SetHotkey(Hotkey.Empty); };
            btnSave.Click += OnSaveClick;
            btnCancel.Click += delegate { Close(); };
        }

        private void ReloadLists()
        {
            string activeSelection = GetSelectedLanguageId(lbActiveSwitch);
            string disabledSelection = GetSelectedLanguageId(lbDisabledSwitch);

            lbActiveSwitch.BeginUpdate();
            lbDisabledSwitch.BeginUpdate();
            lbActiveSwitch.Items.Clear();
            lbDisabledSwitch.Items.Clear();

            foreach (LanguageSetting language in workingSettings.Languages)
            {
                if (language.Enabled)
                {
                    lbActiveSwitch.Items.Add(language);
                }
                else
                {
                    lbDisabledSwitch.Items.Add(language);
                }
            }

            SelectById(lbActiveSwitch, activeSelection);
            SelectById(lbDisabledSwitch, disabledSelection);
            lbActiveSwitch.EndUpdate();
            lbDisabledSwitch.EndUpdate();

            RefreshLanguageHotkeyPanel();
        }

        private void OnLanguageSelectionChanged(object sender, EventArgs e)
        {
            CommitSelectedLanguageHotkey();

            ListBox source = (ListBox)sender;
            ListBox other = source == lbActiveSwitch ? lbDisabledSwitch : lbActiveSwitch;
            if (source.SelectedItem != null)
            {
                other.ClearSelected();
            }

            selectedLanguageId = GetSelectedLanguageId(source);
            RefreshLanguageHotkeyPanel();
        }

        private void MoveSelected(bool enabled)
        {
            CommitSelectedLanguageHotkey();

            ListBox source = enabled ? lbDisabledSwitch : lbActiveSwitch;
            var language = source.SelectedItem as LanguageSetting;
            if (language == null)
            {
                return;
            }

            language.Enabled = enabled;
            selectedLanguageId = language.LayoutId;
            ReloadLists();
            SelectById(enabled ? lbActiveSwitch : lbDisabledSwitch, language.LayoutId);
        }

        private void MoveSelectedActiveLanguage(int direction)
        {
            CommitSelectedLanguageHotkey();

            var language = lbActiveSwitch.SelectedItem as LanguageSetting;
            if (language == null)
            {
                return;
            }

            int index = workingSettings.Languages.IndexOf(language);
            int targetIndex = FindEnabledTargetIndex(index, direction);
            if (targetIndex < 0)
            {
                return;
            }

            workingSettings.Languages.RemoveAt(index);
            workingSettings.Languages.Insert(targetIndex, language);
            selectedLanguageId = language.LayoutId;
            ReloadLists();
            SelectById(lbActiveSwitch, language.LayoutId);
        }

        private int FindEnabledTargetIndex(int index, int direction)
        {
            for (int i = index + direction; i >= 0 && i < workingSettings.Languages.Count; i += direction)
            {
                if (workingSettings.Languages[i].Enabled)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshLanguageHotkeyPanel()
        {
            LanguageSetting language = FindSelectedLanguage();
            bool hasSelection = language != null;
            languageHotkeyBox.Enabled = hasSelection;
            btnClearLanguageHotkey.Enabled = hasSelection;
            lblLanguageHotkey.Enabled = hasSelection;
            languageHotkeyBox.SetHotkey(hasSelection ? Hotkey.Parse(language.Hotkey) : Hotkey.Empty);
        }

        private void CommitSelectedLanguageHotkey()
        {
            if (string.IsNullOrWhiteSpace(selectedLanguageId))
            {
                return;
            }

            LanguageSetting language = workingSettings.FindLanguage(selectedLanguageId);
            if (language != null)
            {
                language.Hotkey = languageHotkeyBox.Hotkey.ToString();
            }
        }

        private LanguageSetting FindSelectedLanguage()
        {
            string layoutId = GetSelectedLanguageId(lbActiveSwitch);
            if (string.IsNullOrWhiteSpace(layoutId))
            {
                layoutId = GetSelectedLanguageId(lbDisabledSwitch);
            }

            selectedLanguageId = layoutId;
            return string.IsNullOrWhiteSpace(layoutId) ? null : workingSettings.FindLanguage(layoutId);
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            CommitSelectedLanguageHotkey();

            workingSettings.CycleHotkey = cycleHotkeyBox.Hotkey.ToString();
            workingSettings.WindowsFallbackHotkey = fallbackHotkeyBox.Hotkey.ToString();
            workingSettings.RunAtStartup = chkRunAtStartup.Checked;

            originalSettings.CycleHotkey = workingSettings.CycleHotkey;
            originalSettings.WindowsFallbackHotkey = workingSettings.WindowsFallbackHotkey;
            originalSettings.RunAtStartup = workingSettings.RunAtStartup;
            originalSettings.Languages.Clear();
            originalSettings.Languages.AddRange(workingSettings.Languages.Select(CloneLanguage));

            var handler = SettingsSaved;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }

            Close();
        }

        private static string GetSelectedLanguageId(ListBox listBox)
        {
            var language = listBox.SelectedItem as LanguageSetting;
            return language == null ? null : language.LayoutId;
        }

        private static void SelectById(ListBox listBox, string layoutId)
        {
            if (string.IsNullOrWhiteSpace(layoutId))
            {
                return;
            }

            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var language = listBox.Items[i] as LanguageSetting;
                if (language != null && string.Equals(language.LayoutId, layoutId, StringComparison.OrdinalIgnoreCase))
                {
                    listBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private static AppSettingsData Clone(AppSettingsData source)
        {
            return new AppSettingsData
            {
                CycleHotkey = source.CycleHotkey,
                WindowsFallbackHotkey = source.WindowsFallbackHotkey,
                RunAtStartup = source.RunAtStartup,
                Languages = source.Languages.Select(CloneLanguage).ToList()
            };
        }

        private static LanguageSetting CloneLanguage(LanguageSetting source)
        {
            return new LanguageSetting
            {
                LayoutId = source.LayoutId,
                DisplayName = source.DisplayName,
                Enabled = source.Enabled,
                Hotkey = source.Hotkey
            };
        }
    }
}
