using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using Waccy.Models;
using Waccy.Services;

namespace Waccy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ClipboardService clipboardService;
    private HotkeyService hotkeyService;
    private TrayIconService trayIconService;

    public MainWindow()
    {
        InitializeComponent();

        // 初始化服务
        InitializeServices();

        // 设置数据绑定
        HistoryListView.ItemsSource = clipboardService.History;

        // 隐藏窗口
        this.Hide();
    }

    private void InitializeServices()
    {
        // 为调试创建控制台窗口
        AllocConsole();
        System.Diagnostics.Debug.WriteLine("Waccy剪贴板管理器启动...");
        Console.WriteLine("Waccy剪贴板管理器启动...");

        clipboardService = new ClipboardService(this);
        hotkeyService = new HotkeyService(this);
        trayIconService = new TrayIconService(clipboardService);

        // 设置低级键盘钩子
        try
        {
            // 注册低级别键盘钩子事件处理程序
            LowLevelKeyboardHook.KeyDown += LowLevelKeyboardHook_KeyDown;
            
            // 设置钩子
            LowLevelKeyboardHook.SetHook();
            
            Console.WriteLine("低级键盘钩子已设置，按F7将呼出/隐藏程序窗口");
            System.Diagnostics.Debug.WriteLine("低级键盘钩子已设置，按F7将呼出/隐藏程序窗口");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置键盘钩子时出错: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"设置键盘钩子时出错: {ex.Message}");
            System.Windows.MessageBox.Show($"设置键盘钩子时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 绑定托盘图标事件
        trayIconService.OpenRequested += TrayIconService_OpenRequested;
        trayIconService.ExitRequested += TrayIconService_ExitRequested;
    }

    private void LowLevelKeyboardHook_KeyDown(object sender, Key e)
    {
        if (e == Key.F7)
        {
            Console.WriteLine("F7键被按下，切换窗口可见性");
            System.Diagnostics.Debug.WriteLine("F7键被按下，切换窗口可见性");
            
            // 在UI线程上运行
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ToggleWindowVisibility();
            });
        }
    }

    private void TrayIconService_OpenRequested(object? sender, System.EventArgs e)
    {
        ShowWindow();
    }

    private void TrayIconService_ExitRequested(object? sender, System.EventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void ToggleWindowVisibility()
    {
        System.Diagnostics.Debug.WriteLine($"切换窗口可见性，当前状态: {(IsVisible ? "可见" : "隐藏")}");
        
        if (this.IsVisible)
        {
            this.Hide();
            System.Diagnostics.Debug.WriteLine("窗口已隐藏");
        }
        else
        {
            ShowWindow();
            System.Diagnostics.Debug.WriteLine("窗口已显示");
        }
    }

    private void ShowWindow()
    {
        // 显示窗口
        this.Show();
        
        // 将窗口置于顶层
        this.Topmost = true;
        this.Activate();
        this.Focus();
        this.Topmost = false;
        
        // 如果有项目，默认选择第一项
        if (clipboardService.History.Count > 0)
        {
            HistoryListView.SelectedIndex = 0;
            HistoryListView.Focus();
        }
    }

    private void Window_Deactivated(object sender, System.EventArgs e)
    {
        // 窗口失去焦点时隐藏
        this.Hide();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // 按下ESC键隐藏窗口
            this.Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            // 按下Enter键粘贴选中项
            PasteSelectedItem();
            e.Handled = true;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 窗口加载后隐藏
        this.Hide();
    }

    private void HistoryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            PasteSelectedItem();
        }
    }

    private void PasteSelectedItem()
    {
        if (HistoryListView.SelectedItem is ClipboardItem selectedItem)
        {
            this.Hide();
            
            // 使用更长的延迟确保窗口完全隐藏并且系统可以切换到前一个窗口
            System.Threading.Tasks.Task.Delay(300).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("正在执行粘贴操作...");
                        clipboardService.PasteSelected(selectedItem);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"粘贴操作失败: {ex.Message}");
                    }
                });
            });
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        clipboardService.ClearHistory();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        // 移除低级键盘钩子
        try
        {
            LowLevelKeyboardHook.KeyDown -= LowLevelKeyboardHook_KeyDown;
            LowLevelKeyboardHook.Unhook();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"移除键盘钩子时出错: {ex.Message}");
        }
        
        // 清理资源
        hotkeyService?.Dispose();
        trayIconService?.Dispose();
        clipboardService?.Dispose();
        
        // 释放控制台
        FreeConsole();
        
        base.OnClosed(e);
    }

    #region Native Methods
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
    #endregion
}