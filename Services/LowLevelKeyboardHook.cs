using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Waccy.Services
{
    public class LowLevelKeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyProc _hookProc = HookCallback;

        public delegate IntPtr LowLevelKeyProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static event EventHandler<Key> KeyDown;

        public static void SetHook()
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
            if (_hookID == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"设置键盘钩子失败，错误码: {error}");
                Console.WriteLine($"设置键盘钩子失败，错误码: {error}");
            }
            else
            {
                Debug.WriteLine("成功设置低级键盘钩子");
                Console.WriteLine("成功设置低级键盘钩子");
            }
        }

        public static void Unhook()
        {
            if (_hookID != IntPtr.Zero)
            {
                if (UnhookWindowsHookEx(_hookID))
                {
                    Debug.WriteLine("成功移除键盘钩子");
                    Console.WriteLine("成功移除键盘钩子");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"移除键盘钩子失败，错误码: {error}");
                    Console.WriteLine($"移除键盘钩子失败，错误码: {error}");
                }
                _hookID = IntPtr.Zero;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // 检查是否是F7键
                if (vkCode == (int)Keys.F7)
                {
                    Debug.WriteLine($"捕获到按键: F7 (VK: {vkCode})");
                    Console.WriteLine($"捕获到按键: F7 (VK: {vkCode})");
                    
                    // 触发键盘事件
                    KeyDown?.Invoke(null, Key.F7);
                    
                    // 返回1表示该消息已被处理，不再传递
                    return new IntPtr(1);
                }
            }
            
            // 如果不是F7键，则继续传递消息给其他钩子
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // P/Invoke 声明
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
} 