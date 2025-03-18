using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
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

        public ObservableCollection<ClipboardItem> History { get; } = new ObservableCollection<ClipboardItem>();

        public event EventHandler HistoryChanged;

        public ClipboardService(Window window)
        {
            windowHandle = new WindowInteropHelper(window).Handle;
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
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        AddToHistory(new ClipboardItem(ClipboardItemType.Text, text));
                    }
                }
                else if (Clipboard.ContainsImage())
                {
                    BitmapSource image = Clipboard.GetImage();
                    if (image != null)
                    {
                        AddToHistory(new ClipboardItem(ClipboardItemType.Image, image));
                    }
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
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
            Application.Current.Dispatcher.Invoke(() =>
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                History.Clear();
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public void PasteSelected(ClipboardItem item)
        {
            if (item == null) return;
            
            Clipboard.Clear();
            
            switch (item.Type)
            {
                case ClipboardItemType.Text:
                    Clipboard.SetText((string)item.Content);
                    break;
                case ClipboardItemType.Image:
                    Clipboard.SetImage((BitmapSource)item.Content);
                    break;
                case ClipboardItemType.FilePath:
                    var fileCollection = new System.Collections.Specialized.StringCollection();
                    fileCollection.Add((string)item.Content);
                    Clipboard.SetFileDropList(fileCollection);
                    break;
            }
            
            // 模拟按下Ctrl+V
            SendKeys(0x11, 0x56); // Ctrl+V
        }

        private void SendKeys(byte vk1, byte vk2)
        {
            // 定义键盘事件
            INPUT[] inputs = new INPUT[4];
            
            // 按下第一个键 (Ctrl)
            inputs[0].type = 1; // INPUT_KEYBOARD
            inputs[0].ki.wVk = vk1;
            
            // 按下第二个键 (V)
            inputs[1].type = 1; // INPUT_KEYBOARD
            inputs[1].ki.wVk = vk2;
            
            // 释放第二个键 (V)
            inputs[2].type = 1; // INPUT_KEYBOARD
            inputs[2].ki.wVk = vk2;
            inputs[2].ki.dwFlags = 2; // KEYEVENTF_KEYUP
            
            // 释放第一个键 (Ctrl)
            inputs[3].type = 1; // INPUT_KEYBOARD
            inputs[3].ki.wVk = vk1;
            inputs[3].ki.dwFlags = 2; // KEYEVENTF_KEYUP
            
            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
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