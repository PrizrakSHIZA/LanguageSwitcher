using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LanguageSwitcher
{
    public sealed class HotkeyPressedEventArgs : EventArgs
    {
        public string LayoutId { get; private set; }
        public bool IsCycle { get; private set; }

        public HotkeyPressedEventArgs(bool isCycle, string layoutId)
        {
            IsCycle = isCycle;
            LayoutId = layoutId;
        }
    }

    public sealed class GlobalHotkeyManager : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private readonly LowLevelKeyboardProc proc;
        private readonly HashSet<Keys> pressedKeys;
        private IntPtr hookId;
        private Hotkey cycleHotkey;
        private List<Tuple<Hotkey, string>> languageHotkeys;
        private string activeSignature;

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;

        public GlobalHotkeyManager()
        {
            proc = HookCallback;
            pressedKeys = new HashSet<Keys>();
            languageHotkeys = new List<Tuple<Hotkey, string>>();
        }

        public void Start()
        {
            if (hookId != IntPtr.Zero)
            {
                return;
            }

            hookId = SetHook(proc);
        }

        public void ApplySettings(AppSettingsData settings)
        {
            cycleHotkey = Hotkey.Parse(settings.CycleHotkey);
            languageHotkeys = settings.Languages
                .Where(l => !string.IsNullOrWhiteSpace(l.Hotkey))
                .Select(l => Tuple.Create(Hotkey.Parse(l.Hotkey), l.LayoutId))
                .Where(t => !t.Item1.IsEmpty)
                .ToList();
        }

        public void Dispose()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc callback)
        {
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, callback, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                Keys key = NormalizeKey((Keys)Marshal.ReadInt32(lParam));

                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                {
                    pressedKeys.Add(key);
                    var match = FindMatch();
                    if (match != null)
                    {
                        string signature = GetPressedSignature();
                        if (!string.Equals(activeSignature, signature, StringComparison.Ordinal))
                        {
                            activeSignature = signature;
                            OnHotkeyPressed(match);
                        }

                        return (IntPtr)1;
                    }
                }
                else if (message == WM_KEYUP || message == WM_SYSKEYUP)
                {
                    pressedKeys.Remove(key);
                    activeSignature = null;
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private HotkeyPressedEventArgs FindMatch()
        {
            if (Matches(cycleHotkey))
            {
                return new HotkeyPressedEventArgs(true, null);
            }

            foreach (var item in languageHotkeys)
            {
                if (Matches(item.Item1))
                {
                    return new HotkeyPressedEventArgs(false, item.Item2);
                }
            }

            return null;
        }

        private bool Matches(Hotkey hotkey)
        {
            if (hotkey == null || hotkey.IsEmpty)
            {
                return false;
            }

            if (GetCurrentModifiers() != (hotkey.Modifiers & ~HotkeyModifiers.NoRepeat))
            {
                return false;
            }

            return hotkey.Key == Keys.None || pressedKeys.Contains(NormalizeKey(hotkey.Key));
        }

        private HotkeyModifiers GetCurrentModifiers()
        {
            var modifiers = HotkeyModifiers.None;
            if (pressedKeys.Contains(Keys.ControlKey)) modifiers |= HotkeyModifiers.Control;
            if (pressedKeys.Contains(Keys.ShiftKey)) modifiers |= HotkeyModifiers.Shift;
            if (pressedKeys.Contains(Keys.Menu)) modifiers |= HotkeyModifiers.Alt;
            if (pressedKeys.Contains(Keys.LWin) || pressedKeys.Contains(Keys.RWin)) modifiers |= HotkeyModifiers.Win;
            return modifiers;
        }

        private string GetPressedSignature()
        {
            return string.Join(",", pressedKeys.OrderBy(k => k).Select(k => ((int)k).ToString()).ToArray());
        }

        private void OnHotkeyPressed(HotkeyPressedEventArgs args)
        {
            var handler = HotkeyPressed;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private static Keys NormalizeKey(Keys key)
        {
            if (key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey) return Keys.ControlKey;
            if (key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.ShiftKey) return Keys.ShiftKey;
            if (key == Keys.LMenu || key == Keys.RMenu || key == Keys.Menu) return Keys.Menu;
            return key;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
