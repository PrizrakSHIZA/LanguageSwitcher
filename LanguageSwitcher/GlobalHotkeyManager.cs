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
        public bool IsModifierOnly { get; private set; }
        public bool Handled { get; set; }

        public HotkeyPressedEventArgs(bool isCycle, string layoutId, bool isModifierOnly)
        {
            IsCycle = isCycle;
            LayoutId = layoutId;
            IsModifierOnly = isModifierOnly;
        }
    }

    public sealed class GlobalHotkeyManager : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int LLKHF_INJECTED = 0x00000010;

        private readonly LowLevelKeyboardProc proc;
        private readonly HashSet<Keys> pressedKeys;
        private IntPtr hookId;
        private Hotkey cycleHotkey;
        private List<Tuple<Hotkey, string>> languageHotkeys;
        private string activeSignature;
        private HotkeyPressedEventArgs pendingModifierOnlyHotkey;
        private bool pendingModifierOnlyCancelled;

        public event EventHandler<HotkeyPressedEventArgs> HotkeyPressed;
        public event EventHandler<HotkeyPressedEventArgs> ModifierOnlyHotkeyArmed;
        public event EventHandler HotkeyReleased;

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
                var hookInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((hookInfo.flags & LLKHF_INJECTED) == LLKHF_INJECTED)
                {
                    return CallNextHookEx(hookId, nCode, wParam, lParam);
                }

                Keys key = NormalizeKey((Keys)hookInfo.vkCode);

                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                {
                    pressedKeys.Add(key);

                    if (pendingModifierOnlyHotkey != null && IsPendingModifierOnlyCancelledBy(key))
                    {
                        pendingModifierOnlyCancelled = true;
                    }

                    var match = FindMatch();
                    if (match != null)
                    {
                        if (match.IsModifierOnly)
                        {
                            if (pendingModifierOnlyHotkey == null)
                            {
                                pendingModifierOnlyHotkey = match;
                                pendingModifierOnlyCancelled = false;
                                OnModifierOnlyHotkeyArmed(match);
                            }

                            return CallNextHookEx(hookId, nCode, wParam, lParam);
                        }

                        string signature = GetPressedSignature();
                        if (!string.Equals(activeSignature, signature, StringComparison.Ordinal))
                        {
                            activeSignature = signature;
                            OnHotkeyPressed(match);
                        }

                        if (match.Handled)
                        {
                            return (IntPtr)1;
                        }
                    }
                }
                else if (message == WM_KEYUP || message == WM_SYSKEYUP)
                {
                    pressedKeys.Remove(key);
                    if (pressedKeys.Count == 0)
                    {
                        HotkeyPressedEventArgs modifierOnlyHotkeyToFire =
                            pendingModifierOnlyHotkey != null && !pendingModifierOnlyCancelled
                                ? pendingModifierOnlyHotkey
                                : null;

                        activeSignature = null;
                        pendingModifierOnlyHotkey = null;
                        pendingModifierOnlyCancelled = false;
                        OnHotkeyReleased();

                        if (modifierOnlyHotkeyToFire != null)
                        {
                            FireModifierOnlyHotkeyAfterRelease(modifierOnlyHotkeyToFire);
                        }
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private HotkeyPressedEventArgs FindMatch()
        {
            if (Matches(cycleHotkey))
            {
                return new HotkeyPressedEventArgs(true, null, cycleHotkey.Key == Keys.None);
            }

            foreach (var item in languageHotkeys)
            {
                if (Matches(item.Item1))
                {
                    return new HotkeyPressedEventArgs(false, item.Item2, item.Item1.Key == Keys.None);
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

        private void FireModifierOnlyHotkeyAfterRelease(HotkeyPressedEventArgs args)
        {
            var timer = new Timer();
            timer.Interval = 30;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                OnHotkeyPressed(args);
            };
            timer.Start();
        }

        private bool IsPendingModifierOnlyCancelledBy(Keys key)
        {
            if (!IsModifierKey(key))
            {
                return true;
            }

            HotkeyModifiers pressedModifier = GetModifierForKey(key);
            Hotkey pendingHotkey = pendingModifierOnlyHotkey == null
                ? null
                : (pendingModifierOnlyHotkey.IsCycle
                    ? cycleHotkey
                    : languageHotkeys
                        .Where(h => string.Equals(h.Item2, pendingModifierOnlyHotkey.LayoutId, StringComparison.OrdinalIgnoreCase))
                        .Select(h => h.Item1)
                        .FirstOrDefault());

            return pendingHotkey == null ||
                   (pendingHotkey.Modifiers & pressedModifier) != pressedModifier;
        }

        private static HotkeyModifiers GetModifierForKey(Keys key)
        {
            if (key == Keys.ControlKey) return HotkeyModifiers.Control;
            if (key == Keys.ShiftKey) return HotkeyModifiers.Shift;
            if (key == Keys.Menu) return HotkeyModifiers.Alt;
            if (key == Keys.LWin || key == Keys.RWin) return HotkeyModifiers.Win;
            return HotkeyModifiers.None;
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

        private void OnModifierOnlyHotkeyArmed(HotkeyPressedEventArgs args)
        {
            var handler = ModifierOnlyHotkeyArmed;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void OnHotkeyReleased()
        {
            var handler = HotkeyReleased;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static Keys NormalizeKey(Keys key)
        {
            if (key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey) return Keys.ControlKey;
            if (key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.ShiftKey) return Keys.ShiftKey;
            if (key == Keys.LMenu || key == Keys.RMenu || key == Keys.Menu) return Keys.Menu;
            return key;
        }

        private static bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey ||
                   key == Keys.ShiftKey ||
                   key == Keys.Menu ||
                   key == Keys.LWin ||
                   key == Keys.RWin;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public int flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

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
