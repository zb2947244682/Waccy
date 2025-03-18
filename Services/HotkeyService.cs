using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Waccy.Services
{
    public class HotkeyService
    {
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_SHIFT = 0x0004;
        private const int HOTKEY_ID = 9000;

        private IntPtr windowHandle;
        private HwndSource source;

        public event EventHandler<EventArgs> HotkeyPressed;

        public HotkeyService(Window window)
        {
            windowHandle = new WindowInteropHelper(window).Handle;
            source = HwndSource.FromHwnd(windowHandle);
            source?.AddHook(WndProc);
        }

        public bool RegisterHotkey(char key)
        {
            // Alt+Shift+指定按键
            return RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT | MOD_SHIFT, (int)key);
        }

        public void UnregisterHotkey()
        {
            UnregisterHotKey(windowHandle, HOTKEY_ID);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                OnHotkeyPressed();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void OnHotkeyPressed()
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            UnregisterHotkey();
            source?.RemoveHook(WndProc);
            source?.Dispose();
        }

        #region Native Methods
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
    }
} 