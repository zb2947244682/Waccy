using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Waccy.Models;

namespace Waccy.Services
{
    public class ClipboardService
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr windowHandle;
        private HwndSource source;
        private readonly int MAX_HISTORY_ITEMS = 10;
        private Window window;

        public ObservableCollection<ClipboardItem> History { get; } = new ObservableCollection<ClipboardItem>();

        public event EventHandler HistoryChanged;

        public ClipboardService(Window window)
        {
            this.window = window;
            
            // 等待窗口加载完成后再初始化
            if (window.IsLoaded)
            {
                InitializeClipboardListener();
            }
            else
            {
                window.Loaded += (s, e) => InitializeClipboardListener();
            }
        }

        private void InitializeClipboardListener()
        {
            windowHandle = new WindowInteropHelper(window).Handle;
            if (windowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法获取窗口句柄");
            }

            source = HwndSource.FromHwnd(windowHandle);
            source?.AddHook(WndProc);

            // 注册剪贴板监听
            AddClipboardFormatListener(windowHandle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                CaptureClipboard();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void CaptureClipboard()
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string text = System.Windows.Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AddToHistory(new ClipboardItem(ClipboardItemType.Text, text));
                    }
                }
                else if (System.Windows.Clipboard.ContainsImage())
                {
                    BitmapSource image = System.Windows.Clipboard.GetImage();
                    if (image != null)
                    {
                        AddToHistory(new ClipboardItem(ClipboardItemType.Image, image));
                    }
                }
                else if (System.Windows.Clipboard.ContainsFileDropList())
                {
                    var files = System.Windows.Clipboard.GetFileDropList();
                    if (files.Count > 0)
                    {
                        foreach (string file in files)
                        {
                            AddToHistory(new ClipboardItem(ClipboardItemType.FilePath, file));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理剪贴板访问异常
                Console.WriteLine($"剪贴板访问错误: {ex.Message}");
            }
        }

        private void AddToHistory(ClipboardItem item)
        {
            // 如果存在重复项则移除旧的
            var duplicate = History.FirstOrDefault(i => 
                i.Type == item.Type && 
                (i.Type == ClipboardItemType.Text && (string)i.Content == (string)item.Content) ||
                (i.Type == ClipboardItemType.FilePath && (string)i.Content == (string)item.Content));

            if (duplicate != null)
            {
                History.Remove(duplicate);
            }

            // 插入新项到最前面
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                History.Insert(0, item);

                // 保持历史记录数量限制
                while (History.Count > MAX_HISTORY_ITEMS)
                {
                    History.RemoveAt(History.Count - 1);
                }

                HistoryChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public void ClearHistory()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                History.Clear();
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public void PasteSelected(ClipboardItem item)
        {
            if (item == null) return;
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"准备粘贴项目类型: {item.Type}");
                
                // 先暂存选中项到剪贴板
                System.Windows.Clipboard.Clear();
                
                switch (item.Type)
                {
                    case ClipboardItemType.Text:
                        string text = (string)item.Content;
                        System.Diagnostics.Debug.WriteLine($"正在复制文本: {(text.Length > 20 ? text.Substring(0, 20) + "..." : text)}");
                        System.Windows.Clipboard.SetText(text);
                        break;
                    case ClipboardItemType.Image:
                        System.Diagnostics.Debug.WriteLine("正在复制图片");
                        System.Windows.Clipboard.SetImage((System.Windows.Media.Imaging.BitmapSource)item.Content);
                        break;
                    case ClipboardItemType.FilePath:
                        string path = (string)item.Content;
                        System.Diagnostics.Debug.WriteLine($"正在复制文件路径: {path}");
                        var fileCollection = new System.Collections.Specialized.StringCollection();
                        fileCollection.Add(path);
                        System.Windows.Clipboard.SetFileDropList(fileCollection);
                        break;
                }
                
                // 等待剪贴板内容更新
                System.Threading.Thread.Sleep(200);
                
                // 先尝试用SendKeys
                try
                {
                    // 允许切换到前一个窗口
                    System.Threading.Thread.Sleep(300);
                    System.Diagnostics.Debug.WriteLine("正在发送Alt+Tab快捷键...");
                    // 使用SendKeys模拟Alt+Tab
                    SendAltTab();
                    System.Threading.Thread.Sleep(400); // 等待窗口切换
                    
                    System.Diagnostics.Debug.WriteLine("正在发送Ctrl+V快捷键...");
                    // 使用SendKeys模拟Ctrl+V
                    SendCtrlV();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"使用SendKeys粘贴失败: {ex.Message}");
                    // 备用方案：使用SendInput
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("尝试使用SendInput方法粘贴...");
                        SimulateCtrlV();
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"使用SendInput粘贴也失败: {ex2.Message}");
                    }
                }
                
                // 将当前项移到历史记录的顶部
                if (History.Contains(item) && History.IndexOf(item) > 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        History.Remove(item);
                        History.Insert(0, item);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"粘贴失败: {ex.Message}");
            }
        }

        private void SendAltTab()
        {
            // 使用SendInput发送Alt+Tab组合键
            INPUT[] inputs = new INPUT[4];
            
            // 按下Alt
            inputs[0].type = 1; // INPUT_KEYBOARD
            inputs[0].ki.wVk = 0x12; // VK_ALT
            
            // 按下Tab
            inputs[1].type = 1; // INPUT_KEYBOARD
            inputs[1].ki.wVk = 0x09; // VK_TAB
            
            // 释放Tab
            inputs[2].type = 1; // INPUT_KEYBOARD
            inputs[2].ki.wVk = 0x09; // VK_TAB
            inputs[2].ki.dwFlags = 2; // KEYEVENTF_KEYUP
            
            // 释放Alt
            inputs[3].type = 1; // INPUT_KEYBOARD
            inputs[3].ki.wVk = 0x12; // VK_ALT
            inputs[3].ki.dwFlags = 2; // KEYEVENTF_KEYUP
            
            uint result = SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
            System.Diagnostics.Debug.WriteLine($"SendInput结果(Alt+Tab): {result}");
        }

        private void SendCtrlV()
        {
            // 使用SendInput发送Ctrl+V组合键
            INPUT[] inputs = new INPUT[4];
            
            // 按下Ctrl
            inputs[0].type = 1; // INPUT_KEYBOARD
            inputs[0].ki.wVk = 0x11; // VK_CONTROL
            
            // 按下V
            inputs[1].type = 1; // INPUT_KEYBOARD
            inputs[1].ki.wVk = 0x56; // VK_V
            
            // 释放V
            inputs[2].type = 1; // INPUT_KEYBOARD
            inputs[2].ki.wVk = 0x56; // VK_V
            inputs[2].ki.dwFlags = 2; // KEYEVENTF_KEYUP
            
            // 释放Ctrl
            inputs[3].type = 1; // INPUT_KEYBOARD
            inputs[3].ki.wVk = 0x11; // VK_CONTROL
            inputs[3].ki.dwFlags = 2; // KEYEVENTF_KEYUP
            
            uint result = SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
            System.Diagnostics.Debug.WriteLine($"SendInput结果(Ctrl+V): {result}");
        }

        private void SimulateCtrlV()
        {
            // 另一种方式模拟Ctrl+V
            System.Windows.Forms.SendKeys.SendWait("^v");
            System.Diagnostics.Debug.WriteLine("已发送System.Windows.Forms.SendKeys.SendWait(\"^v\")");
        }

        public void Dispose()
        {
            RemoveClipboardFormatListener(windowHandle);
            source?.RemoveHook(WndProc);
            source?.Dispose();
        }

        #region Native Methods
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
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
        #endregion
    }
} 