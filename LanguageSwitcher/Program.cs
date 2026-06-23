using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LanguageSwitcher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        }
    }

    public sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly KeyboardLayoutService layoutService;
        private readonly GlobalHotkeyManager hotkeyManager;
        private AppSettingsData settings;
        private SettingsForm settingsForm;
        private int activeLanguageIndex = -1;

        public TrayApplicationContext()
        {
            layoutService = new KeyboardLayoutService();
            hotkeyManager = new GlobalHotkeyManager();
            hotkeyManager.HotkeyPressed += OnHotkeyPressed;

            LoadSettings();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Settings", null, OnSettingsClick);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, OnExitClick);

            trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Text = "Language Switcher",
                Visible = true
            };
            trayIcon.DoubleClick += OnSettingsClick;

            hotkeyManager.Start();
        }

        private void LoadSettings()
        {
            settings = AppSettingsStore.Load(layoutService.GetInstalledLayouts());
            settings.RunAtStartup = StartupManager.IsEnabled();
            hotkeyManager.ApplySettings(settings);
        }

        private void SaveSettings()
        {
            AppSettingsStore.Save(settings);
            StartupManager.SetEnabled(settings.RunAtStartup);
            hotkeyManager.ApplySettings(settings);
        }

        private void OnHotkeyPressed(object sender, HotkeyPressedEventArgs e)
        {
            if (e.IsCycle)
            {
                SwitchToNextEnabledLanguage();
            }
            else
            {
                layoutService.Activate(e.LayoutId);
            }
        }

        private void SwitchToNextEnabledLanguage()
        {
            List<LanguageSetting> enabledLanguages = settings.Languages.Where(l => l.Enabled).ToList();
            if (enabledLanguages.Count == 0)
            {
                return;
            }

            activeLanguageIndex++;
            if (activeLanguageIndex >= enabledLanguages.Count)
            {
                activeLanguageIndex = 0;
            }

            layoutService.Activate(enabledLanguages[activeLanguageIndex].LayoutId);
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            if (settingsForm != null && !settingsForm.IsDisposed)
            {
                settingsForm.Activate();
                return;
            }

            LoadSettings();
            settingsForm = new SettingsForm(settings);
            settingsForm.SettingsSaved += OnSettingsSaved;
            settingsForm.FormClosed += delegate { settingsForm = null; };
            settingsForm.Show();
        }

        private void OnSettingsSaved(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            hotkeyManager.Dispose();
            Application.Exit();
        }
    }
}
