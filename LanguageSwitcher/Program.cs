using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Threading;
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
        private string pendingFallbackLayoutId;
        private int pendingFallbackLanguageIndex = -1;
        private int armedModifierOnlyCycleBaseIndex = -1;
        private bool handlingReleasedModifierOnlyHotkey;

        public TrayApplicationContext()
        {
            layoutService = new KeyboardLayoutService();
            hotkeyManager = new GlobalHotkeyManager();
            hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            hotkeyManager.ModifierOnlyHotkeyArmed += OnModifierOnlyHotkeyArmed;
            hotkeyManager.HotkeyReleased += OnHotkeyReleased;

            LoadSettings();

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Settings", null, OnSettingsClick);
            var restartAsAdminItem = trayMenu.Items.Add("Restart as administrator", null, OnRestartAsAdministratorClick);
            restartAsAdminItem.Enabled = !IsRunningAsAdministrator();
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
            handlingReleasedModifierOnlyHotkey = e.IsModifierOnly;
            try
            {
                if (e.IsCycle)
                {
                    e.Handled = SwitchToNextEnabledLanguage(e.IsModifierOnly);
                }
                else
                {
                    e.Handled = ActivateLanguage(e.LayoutId);
                }
            }
            finally
            {
                handlingReleasedModifierOnlyHotkey = false;
            }
        }

        private void OnModifierOnlyHotkeyArmed(object sender, HotkeyPressedEventArgs e)
        {
            if (!e.IsCycle)
            {
                return;
            }

            List<LanguageSetting> enabledLanguages = settings.Languages.Where(l => l.Enabled).ToList();
            armedModifierOnlyCycleBaseIndex = GetCurrentEnabledLanguageIndex(enabledLanguages);
            if (armedModifierOnlyCycleBaseIndex < 0)
            {
                armedModifierOnlyCycleBaseIndex = activeLanguageIndex;
            }
        }

        private bool SwitchToNextEnabledLanguage(bool useArmedBaseIndex)
        {
            List<LanguageSetting> enabledLanguages = settings.Languages.Where(l => l.Enabled).ToList();
            if (enabledLanguages.Count == 0)
            {
                return false;
            }

            int currentLanguageIndex = useArmedBaseIndex
                ? armedModifierOnlyCycleBaseIndex
                : GetCurrentEnabledLanguageIndex(enabledLanguages);

            int nextLanguageIndex = currentLanguageIndex >= 0
                ? currentLanguageIndex + 1
                : activeLanguageIndex + 1;

            if (nextLanguageIndex >= enabledLanguages.Count)
            {
                nextLanguageIndex = 0;
            }

            if (!ActivateLanguage(enabledLanguages[nextLanguageIndex].LayoutId, nextLanguageIndex))
            {
                armedModifierOnlyCycleBaseIndex = -1;
                return false;
            }

            activeLanguageIndex = nextLanguageIndex;
            armedModifierOnlyCycleBaseIndex = -1;
            return true;
        }

        private bool ActivateLanguage(string layoutId, int languageIndex)
        {
            if (layoutService.Activate(layoutId))
            {
                return true;
            }

            if (layoutService.GetActiveForegroundLanguageId() == null)
            {
                return false;
            }

            if (handlingReleasedModifierOnlyHotkey)
            {
                BeginInvokeFallback(layoutId, languageIndex);
                return true;
            }

            pendingFallbackLayoutId = layoutId;
            pendingFallbackLanguageIndex = languageIndex;
            return true;
        }

        private bool ActivateLanguage(string layoutId)
        {
            return ActivateLanguage(layoutId, -1);
        }

        private void OnHotkeyReleased(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pendingFallbackLayoutId))
            {
                return;
            }

            string layoutId = pendingFallbackLayoutId;
            int languageIndex = pendingFallbackLanguageIndex;
            pendingFallbackLayoutId = null;
            pendingFallbackLanguageIndex = -1;

            BeginInvokeFallback(layoutId, languageIndex);
        }

        private void BeginInvokeFallback(string layoutId, int languageIndex)
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 40;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();

                if (ActivateLanguageUsingWindowsFallback(layoutId) && languageIndex >= 0)
                {
                    activeLanguageIndex = languageIndex;
                }
            };
            timer.Start();
        }

        private bool ActivateLanguageUsingWindowsFallback(string layoutId)
        {
            Hotkey fallbackHotkey = Hotkey.Parse(settings.WindowsFallbackHotkey);
            if (fallbackHotkey.IsEmpty)
            {
                return false;
            }

            int maxAttempts = Math.Max(settings.Languages.Count + 1, 4);
            for (int i = 0; i < maxAttempts; i++)
            {
                string currentLanguageId = layoutService.GetActiveForegroundLanguageId();
                if (string.Equals(currentLanguageId, layoutId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (currentLanguageId == null)
                {
                    return false;
                }

                if (!SystemHotkeySender.Send(fallbackHotkey))
                {
                    return false;
                }

                Thread.Sleep(40);
            }

            return string.Equals(
                layoutService.GetActiveForegroundLanguageId(),
                layoutId,
                StringComparison.OrdinalIgnoreCase);
        }

        private int GetCurrentEnabledLanguageIndex(List<LanguageSetting> enabledLanguages)
        {
            string currentLanguageId = layoutService.GetActiveForegroundLanguageId();
            if (string.IsNullOrWhiteSpace(currentLanguageId))
            {
                return -1;
            }

            for (int i = 0; i < enabledLanguages.Count; i++)
            {
                if (string.Equals(enabledLanguages[i].LayoutId, currentLanguageId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
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

        private void OnRestartAsAdministratorClick(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas"
                });

                OnExitClick(sender, e);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // UAC was cancelled or elevation is unavailable.
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
