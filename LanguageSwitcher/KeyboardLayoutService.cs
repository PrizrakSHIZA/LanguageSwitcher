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
        private const uint SMTO_ABORTIFHUNG = 0x0002;

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

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

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

            uint processId;
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out processId);
            IntPtr focusWindow = GetFocusedWindow(foregroundThreadId);
            if (focusWindow == IntPtr.Zero)
            {
                focusWindow = foregroundWindow;
            }

            bool activated = ActivateForForegroundThread(handle, foregroundThreadId);
            bool messageSent = RequestLanguageChange(focusWindow, handle);
            if (focusWindow != foregroundWindow)
            {
                messageSent = RequestLanguageChange(foregroundWindow, handle) || messageSent;
            }

            return activated || messageSent || IsThreadLanguageActive(foregroundThreadId, layoutId);
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

        private bool ActivateForForegroundThread(IntPtr handle, uint foregroundThreadId)
        {
            uint currentThreadId = GetCurrentThreadId();
            bool attached = false;

            try
            {
                if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
                {
                    attached = AttachThreadInput(currentThreadId, foregroundThreadId, true);
                }

                return ActivateKeyboardLayout(handle, 0) != IntPtr.Zero;
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
                }
            }
        }

        private bool RequestLanguageChange(IntPtr window, IntPtr handle)
        {
            if (window == IntPtr.Zero)
            {
                return false;
            }

            IntPtr result;
            return SendMessageTimeout(
                window,
                WM_INPUTLANGCHANGEREQUEST,
                IntPtr.Zero,
                handle,
                SMTO_ABORTIFHUNG,
                100,
                out result) != IntPtr.Zero;
        }

        private bool IsThreadLanguageActive(uint threadId, string layoutId)
        {
            if (threadId == 0)
            {
                return false;
            }

            IntPtr currentLayout = GetKeyboardLayout(threadId);
            return currentLayout != IntPtr.Zero &&
                   string.Equals(GetLanguageId(currentLayout), layoutId, StringComparison.OrdinalIgnoreCase);
        }

        private IntPtr GetFocusedWindow(uint threadId)
        {
            if (threadId == 0)
            {
                return IntPtr.Zero;
            }

            var info = new GUITHREADINFO();
            info.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
            return GetGUIThreadInfo(threadId, ref info) ? info.hwndFocus : IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
