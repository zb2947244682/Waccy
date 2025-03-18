using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Waccy.Services
{
    // 公开NativeMethods类以便在App.xaml.cs中访问
    public static class NativeMethods
    {
        private const int SW_RESTORE = 9;
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        /// <summary>
        /// 查找并激活已经存在的应用程序实例窗口
        /// </summary>
        public static void ActivateExistingInstance()
        {
            try
            {
                // 查找当前应用的所有进程
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
                
                // 遍历所有进程（除了当前进程）
                foreach (Process process in processes)
                {
                    if (process.Id != currentProcess.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        // 如果窗口处于最小化状态，则还原
                        if (IsIconic(process.MainWindowHandle))
                        {
                            ShowWindow(process.MainWindowHandle, SW_RESTORE);
                        }
                        
                        // 将窗口设置为前台窗口
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"激活现有窗口时出错: {ex.Message}");
            }
        }
    }
} 