using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;

namespace Waccy.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_WIN = 0x0008;
        private const int MOD_NOREPEAT = 0x4000;
        private const int HOTKEY_ID = 9000;

        private IntPtr windowHandle;
        private HwndSource source;
        private bool isHotkeyRegistered = false;
        private string hotkeyDescription = string.Empty;

        public event EventHandler<EventArgs> HotkeyPressed;

        public HotkeyService(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            // 窗口可能尚未初始化，所以我们需要等待它完成加载
            if (window.IsLoaded)
            {
                Initialize(window);
            }
            else
            {
                window.Loaded += (sender, e) => Initialize(window);
            }

            // 确保在窗口关闭时释放资源
            window.Closed += (sender, e) => Dispose();
        }

        private void Initialize(Window window)
        {
            windowHandle = new WindowInteropHelper(window).Handle;
            
            if (windowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法获取窗口句柄。");
            }
            
            source = HwndSource.FromHwnd(windowHandle);
            source?.AddHook(WndProc);
        }

        public bool RegisterHotkey(char key)
        {
            // 先尝试注销之前的热键
            if (isHotkeyRegistered)
            {
                UnregisterHotkey();
            }

            // Alt+Shift+指定按键
            hotkeyDescription = $"Alt+Shift+{key}";
            isHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT | MOD_SHIFT, (int)key);
            
            if (!isHotkeyRegistered)
            {
                int error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"注册热键失败，错误码: {error}");
            }
            
            return isHotkeyRegistered;
        }

        public bool RegisterFunctionKey(Keys functionKey)
        {
            // 先尝试注销之前的热键
            if (isHotkeyRegistered)
            {
                UnregisterHotkey();
            }

            System.Diagnostics.Debug.WriteLine($"尝试注册热键: {functionKey}，键值: {(int)functionKey}");
            
            // 单个功能键（尝试不同的修饰符组合）
            hotkeyDescription = functionKey.ToString();
            
            int vk = (int)functionKey;
            
            // 检查是否以管理员权限运行
            bool isElevated = IsRunAsAdministrator();
            System.Diagnostics.Debug.WriteLine($"应用程序管理员权限: {isElevated}");
            
            // 尝试无修饰符注册
            isHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, 0, vk);
            
            if (!isHotkeyRegistered)
            {
                int error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"无修饰符注册热键{functionKey}失败，错误码: {error}");
                
                // 尝试使用NOREPEAT修饰符
                isHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_NOREPEAT, vk);
                
                if (!isHotkeyRegistered)
                {
                    error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"使用NOREPEAT注册热键{functionKey}失败，错误码: {error}");
                    
                    // 尝试使用ALT修饰符（ALT+F7）
                    isHotkeyRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT, vk);
                    
                    if (isHotkeyRegistered)
                    {
                        hotkeyDescription = $"Alt+{functionKey}";
                        System.Diagnostics.Debug.WriteLine($"成功注册Alt+{functionKey}热键");
                    }
                    else
                    {
                        error = Marshal.GetLastWin32Error();
                        System.Diagnostics.Debug.WriteLine($"使用ALT修饰符注册热键{functionKey}失败，错误码: {error}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"成功注册热键{functionKey}（使用NOREPEAT）");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"成功注册热键{functionKey}（无修饰符）");
            }
            
            return isHotkeyRegistered;
        }

        private bool IsRunAsAdministrator()
        {
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查管理员权限时出错: {ex.Message}");
                return false;
            }
        }

        public void UnregisterHotkey()
        {
            if (isHotkeyRegistered && windowHandle != IntPtr.Zero)
            {
                isHotkeyRegistered = !UnregisterHotKey(windowHandle, HOTKEY_ID);
                
                if (isHotkeyRegistered)
                {
                    int error = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"注销热键失败，错误码: {error}");
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 输出所有窗口消息用于调试
            //Console.WriteLine($"收到窗口消息: 0x{msg:X}, wParam: 0x{wParam.ToInt32():X}, lParam: 0x{lParam.ToInt32():X}");
            
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                System.Diagnostics.Debug.WriteLine($"收到热键消息: ID={hotkeyId}, 描述={hotkeyDescription}");
                Console.WriteLine($"收到热键消息: ID={hotkeyId}, 描述={hotkeyDescription}");
                
                if (hotkeyId == HOTKEY_ID)
                {
                    OnHotkeyPressed();
                    handled = true;
                    return new IntPtr(1); // 返回非零值表示消息已处理
                }
            }
            return IntPtr.Zero;
        }

        private void OnHotkeyPressed()
        {
            System.Diagnostics.Debug.WriteLine("触发热键按下事件");
            Console.WriteLine("触发热键按下事件");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            try
            {
                UnregisterHotkey();
                
                if (source != null)
                {
                    source.RemoveHook(WndProc);
                    source.Dispose();
                    source = null;
                }
            }
            catch (Exception ex)
            {
                // 可以考虑记录异常
                System.Diagnostics.Debug.WriteLine($"释放HotkeyService资源时发生错误: {ex.Message}");
            }
        }

        #region Native Methods
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
    }
} 