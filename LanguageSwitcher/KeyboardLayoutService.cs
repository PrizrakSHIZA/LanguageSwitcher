using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LanguageSwitcher
{
    public sealed class KeyboardLayoutInfo
    {
        public string LayoutId { get; set; }
        public string KeyboardLayoutId { get; set; }
        public IntPtr Handle { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public sealed class KeyboardLayoutService
    {
        private const int WM_INPUTLANGCHANGEREQUEST = 0x0050;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetKeyboardLayoutName([Out] StringBuilder pwszKLID);

        [DllImport("user32.dll")]
        private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public List<KeyboardLayoutInfo> GetInstalledLayouts()
        {
            int count = (int)GetKeyboardLayoutList(0, null);
            if (count <= 0)
            {
                return new List<KeyboardLayoutInfo>();
            }

            var handles = new IntPtr[count];
            GetKeyboardLayoutList(count, handles);
            IntPtr originalLayout = GetKeyboardLayout(0);

            try
            {
                return handles
                    .Select(CreateInfo)
                    .Where(l => !string.IsNullOrWhiteSpace(l.LayoutId))
                    .GroupBy(l => l.LayoutId, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }
            finally
            {
                if (originalLayout != IntPtr.Zero)
                {
                    ActivateKeyboardLayout(originalLayout, 0);
                }
            }
        }

        public bool Activate(string layoutId)
        {
            if (string.IsNullOrWhiteSpace(layoutId))
            {
                return false;
            }

            IntPtr handle = FindHandleByLanguageId(layoutId);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return false;
            }

            SendMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, handle);
            return true;
        }

        private KeyboardLayoutInfo CreateInfo(IntPtr handle)
        {
            ActivateKeyboardLayout(handle, 0);

            var layoutName = new StringBuilder(9);
            string keyboardLayoutId = GetKeyboardLayoutName(layoutName) ? layoutName.ToString() : null;
            string languageId = GetLanguageId(handle);
            return new KeyboardLayoutInfo
            {
                Handle = handle,
                LayoutId = languageId,
                KeyboardLayoutId = keyboardLayoutId,
                DisplayName = GetDisplayName(languageId)
            };
        }

        private IntPtr FindHandleByLanguageId(string languageId)
        {
            int count = (int)GetKeyboardLayoutList(0, null);
            if (count <= 0)
            {
                return IntPtr.Zero;
            }

            var handles = new IntPtr[count];
            GetKeyboardLayoutList(count, handles);

            foreach (IntPtr handle in handles)
            {
                if (string.Equals(GetLanguageId(handle), languageId, StringComparison.OrdinalIgnoreCase))
                {
                    return handle;
                }
            }

            return IntPtr.Zero;
        }

        private string GetLanguageId(IntPtr handle)
        {
            return ((ushort)((long)handle & 0xFFFF)).ToString("X4");
        }

        private string GetDisplayName(string layoutId)
        {
            int languageId;
            string cultureName = null;
            if (int.TryParse(layoutId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out languageId))
            {
                try
                {
                    cultureName = new CultureInfo(languageId).EnglishName;
                }
                catch (CultureNotFoundException)
                {
                    cultureName = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(cultureName))
            {
                return cultureName + " (" + layoutId + ")";
            }

            return "Language " + layoutId;
        }
    }
}
