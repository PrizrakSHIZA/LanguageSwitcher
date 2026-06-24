using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LanguageSwitcher
{
    public static class SystemHotkeySender
    {
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static bool Send(Hotkey hotkey)
        {
            if (hotkey == null || hotkey.IsEmpty)
            {
                return false;
            }

            Keys key = hotkey.Key;
            Keys[] modifiers = GetModifiers(hotkey.Modifiers);
            int inputCount = (modifiers.Length * 2) + (key == Keys.None ? 0 : 2);
            var inputs = new INPUT[inputCount];
            int index = 0;

            foreach (Keys modifier in modifiers)
            {
                inputs[index++] = CreateKeyboardInput(modifier, false);
            }

            if (key != Keys.None)
            {
                inputs[index++] = CreateKeyboardInput(key, false);
                inputs[index++] = CreateKeyboardInput(key, true);
            }

            for (int i = modifiers.Length - 1; i >= 0; i--)
            {
                inputs[index++] = CreateKeyboardInput(modifiers[i], true);
            }

            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) == inputs.Length;
        }

        private static Keys[] GetModifiers(HotkeyModifiers modifiers)
        {
            var keys = new System.Collections.Generic.List<Keys>();
            if ((modifiers & HotkeyModifiers.Control) == HotkeyModifiers.Control) keys.Add(Keys.ControlKey);
            if ((modifiers & HotkeyModifiers.Shift) == HotkeyModifiers.Shift) keys.Add(Keys.ShiftKey);
            if ((modifiers & HotkeyModifiers.Alt) == HotkeyModifiers.Alt) keys.Add(Keys.Menu);
            if ((modifiers & HotkeyModifiers.Win) == HotkeyModifiers.Win) keys.Add(Keys.LWin);
            return keys.ToArray();
        }

        private static INPUT CreateKeyboardInput(Keys key, bool keyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
