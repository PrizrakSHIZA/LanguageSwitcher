using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LanguageSwitcher
{
    [Flags]
    public enum HotkeyModifiers : uint
    {
        None = 0,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000
    }

    public sealed class Hotkey
    {
        public HotkeyModifiers Modifiers { get; set; }
        public Keys Key { get; set; }

        public bool IsEmpty
        {
            get { return Modifiers == HotkeyModifiers.None && Key == Keys.None; }
        }

        public static Hotkey Empty
        {
            get { return new Hotkey { Modifiers = HotkeyModifiers.None, Key = Keys.None }; }
        }

        public static Hotkey FromKeyEvent(Keys keyData)
        {
            var keyCode = keyData & Keys.KeyCode;
            var modifiers = HotkeyModifiers.None;
            if ((keyData & Keys.Control) == Keys.Control) modifiers |= HotkeyModifiers.Control;
            if ((keyData & Keys.Shift) == Keys.Shift) modifiers |= HotkeyModifiers.Shift;
            if ((keyData & Keys.Alt) == Keys.Alt) modifiers |= HotkeyModifiers.Alt;

            return new Hotkey
            {
                Modifiers = modifiers,
                Key = IsModifierKey(keyCode) ? Keys.None : keyCode
            };
        }

        public override string ToString()
        {
            if (IsEmpty) return string.Empty;

            var parts = new List<string>();
            if ((Modifiers & HotkeyModifiers.Control) == HotkeyModifiers.Control) parts.Add("Ctrl");
            if ((Modifiers & HotkeyModifiers.Shift) == HotkeyModifiers.Shift) parts.Add("Shift");
            if ((Modifiers & HotkeyModifiers.Alt) == HotkeyModifiers.Alt) parts.Add("Alt");
            if ((Modifiers & HotkeyModifiers.Win) == HotkeyModifiers.Win) parts.Add("Win");
            if (Key != Keys.None) parts.Add(KeyToDisplayName(Key));
            return string.Join("+", parts);
        }

        public static Hotkey Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Empty;
            }

            var hotkey = Empty;
            foreach (string rawPart in value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string part = rawPart.Trim();
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("XCtrl", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Modifiers |= HotkeyModifiers.Control;
                }
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Modifiers |= HotkeyModifiers.Shift;
                }
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Modifiers |= HotkeyModifiers.Alt;
                }
                else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Modifiers |= HotkeyModifiers.Win;
                }
                else
                {
                    Keys parsedKey;
                    if (Enum.TryParse(part, true, out parsedKey))
                    {
                        hotkey.Key = parsedKey;
                    }
                    else if (part.Length == 1)
                    {
                        hotkey.Key = (Keys)char.ToUpperInvariant(part[0]);
                    }
                }
            }

            return hotkey;
        }

        private static bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey ||
                   key == Keys.ShiftKey ||
                   key == Keys.Menu ||
                   key == Keys.LWin ||
                   key == Keys.RWin;
        }

        private static string KeyToDisplayName(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9) return ((char)('0' + key - Keys.D0)).ToString();
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return "Num " + (key - Keys.NumPad0);
            return key.ToString();
        }
    }

    public sealed class HotkeyTextBox : TextBox
    {
        public Hotkey Hotkey { get; private set; }

        public HotkeyTextBox()
        {
            ReadOnly = true;
            ShortcutsEnabled = false;
            Hotkey = Hotkey.Empty;
        }

        public void SetHotkey(Hotkey hotkey)
        {
            Hotkey = hotkey ?? Hotkey.Empty;
            Text = Hotkey.ToString();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Escape)
            {
                SetHotkey(Hotkey.Empty);
            }
            else
            {
                SetHotkey(Hotkey.FromKeyEvent(e.KeyData));
            }

            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Back || keyData == Keys.Delete || keyData == Keys.Escape)
            {
                SetHotkey(Hotkey.Empty);
                return true;
            }

            var hotkey = Hotkey.FromKeyEvent(keyData);
            if (!hotkey.IsEmpty)
            {
                SetHotkey(hotkey);
                return true;
            }

            return true;
        }
    }
}
