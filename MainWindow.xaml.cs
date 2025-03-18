using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
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
    private CollectionViewSource filteredItems;
    private string currentSearchText = string.Empty;
    private string currentCategory = "All";

    public MainWindow()
    {
        InitializeComponent();

        // 设置数据绑定
        InitializeServices();

        // 重要：在绑定数据源之前先设置为null，避免初始闪烁
        HistoryListView.ItemsSource = null;
        
        // 初始化CollectionViewSource进行过滤
        filteredItems = new CollectionViewSource
        {
            Source = clipboardService.History
        };
        
        // 设置过滤谓词
        filteredItems.Filter += FilterItems;
        
        // 设置数据绑定
        HistoryListView.ItemsSource = filteredItems.View;
        
        // 设置分类切换事件
        CategoryTabs.SelectionChanged += CategoryTabs_SelectionChanged;

        // 设置窗口样式，避免在任务栏显示
        this.ShowInTaskbar = false;

        // 重要：窗口初始化完成后，直接隐藏
        this.Loaded += (s, e) => {
            this.Hide();
            System.Diagnostics.Debug.WriteLine("窗口初始化完成并隐藏");
        };
    }

    private void InitializeServices()
    {
        // 为调试创建控制台窗口
        AllocConsole();
        System.Diagnostics.Debug.WriteLine("Waccy剪贴板管理器启动...");
        Console.WriteLine("Waccy剪贴板管理器启动...");

        clipboardService = new ClipboardService(this);
        
        // 设置历史记录变更事件
        clipboardService.HistoryChanged += ClipboardService_HistoryChanged;
        
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

    private void ClipboardService_HistoryChanged(object sender, EventArgs e)
    {
        // 当剪贴板历史变更时，刷新视图
        filteredItems.View.Refresh();
    }

    private void FilterItems(object sender, FilterEventArgs e)
    {
        if (e.Item is ClipboardItem item)
        {
            bool matchesCategory = currentCategory == "All" || 
                                  item.Type.ToString() == currentCategory;
            
            bool matchesSearch = string.IsNullOrEmpty(currentSearchText) ||
                                 (item.SearchContent != null && 
                                  item.SearchContent.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase));
            
            e.Accepted = matchesCategory && matchesSearch;
        }
        else
        {
            e.Accepted = false;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        currentSearchText = SearchBox.Text;
        filteredItems.View.Refresh();
    }

    private void CategoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryTabs.SelectedItem is TabItem selectedTab)
        {
            currentCategory = selectedTab.Tag.ToString();
            filteredItems.View.Refresh();
        }
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
        // 清空搜索框
        SearchBox.Text = string.Empty;
        
        // 重置为全部分类
        CategoryTabs.SelectedIndex = 0;
        
        // 显示窗口
        this.Show();
        
        // 将窗口置于顶层
        this.Topmost = true;
        this.Activate();
        this.Focus();
        this.Topmost = false;
        
        // 如果有项目，默认选择第一项
        if (filteredItems.View.Cast<ClipboardItem>().Any())
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
        clipboardService.HistoryChanged -= ClipboardService_HistoryChanged;
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