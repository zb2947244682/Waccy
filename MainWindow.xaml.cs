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
    private bool isReallyClosing = false; // 添加标志，用于区分真正关闭和隐藏窗口
    private bool isInitialized = false;

    public MainWindow()
    {
        // 设置窗口不在任务栏显示
        this.ShowInTaskbar = false;
        this.Opacity = 1.0;
        
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

        // 窗口不在任务栏上显示
        this.ShowInTaskbar = false;
        
        // 设置窗口位置居中
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void InitializeServices()
    {
        // 移除控制台窗口创建，不再为调试创建控制台窗口
        // AllocConsole();
        System.Diagnostics.Debug.WriteLine("Waccy剪贴板管理器启动...");
        // Console.WriteLine("Waccy剪贴板管理器启动...");

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
            
            // Console.WriteLine("低级键盘钩子已设置，按F7将呼出/隐藏程序窗口");
            System.Diagnostics.Debug.WriteLine("低级键盘钩子已设置，按F7将呼出/隐藏程序窗口");
        }
        catch (Exception ex)
        {
            // Console.WriteLine($"设置键盘钩子时出错: {ex.Message}");
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
            // Console.WriteLine("F7键被按下，切换窗口可见性");
            System.Diagnostics.Debug.WriteLine("F7键被按下，切换窗口可见性");
            
            // 在UI线程上运行，使用InvokeAsync确保不阻塞键盘线程
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ToggleWindowVisibility();
            });
        }
    }

    private void TrayIconService_OpenRequested(object? sender, System.EventArgs e)
    {
        // 使用Dispatcher.InvokeAsync确保UI线程上操作
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ShowWindow();
        });
    }

    private void TrayIconService_ExitRequested(object? sender, System.EventArgs e)
    {
        // 设置真正关闭标志
        isReallyClosing = true;
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
        
        // 确保窗口状态为正常
        this.WindowState = WindowState.Normal;
        
        // 确保窗口不在任务栏显示
        this.ShowInTaskbar = false;
        
        // 重要：先设置窗口位置，然后再显示
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        // 先将窗口设置为完全透明但有效
        this.Opacity = 0.0;
        this.Visibility = Visibility.Visible;
        this.Topmost = true;
        
        // 使用动画平滑过渡显示窗口
        System.Windows.Media.Animation.DoubleAnimation fadeIn = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150))
        };
        
        this.BeginAnimation(OpacityProperty, fadeIn);
        
        // 创建定时器在短暂延迟后取消Topmost
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(300);
        timer.Tick += (s, e) => {
            this.Activate();
            this.Focus();
            this.Topmost = false;
            timer.Stop();
        };
        timer.Start();
        
        // 如果有项目，默认选择第一项
        if (filteredItems.View.Cast<ClipboardItem>().Any())
        {
            HistoryListView.SelectedIndex = 0;
            HistoryListView.Focus();
        }
    }

    private void Window_Deactivated(object sender, System.EventArgs e)
    {
        // 只有当窗口已初始化并且可见时，才在失去焦点时隐藏
        if (isInitialized && this.IsVisible)
        {
            this.Hide();
            System.Diagnostics.Debug.WriteLine("窗口失去焦点，已隐藏到托盘");
        }
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
        // 窗口加载后不再隐藏
        // this.Hide();
        
        // 标记窗口已初始化
        isInitialized = true;
        
        // 确保窗口不在任务栏显示
        this.ShowInTaskbar = false;
        
        // 如果有项目，默认选择第一项
        if (filteredItems.View.Cast<ClipboardItem>().Any())
        {
            HistoryListView.SelectedIndex = 0;
            HistoryListView.Focus();
        }
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

    private void Window_StateChanged(object sender, System.EventArgs e)
    {
        // 当窗口最小化时，隐藏窗口并显示在托盘
        if (this.WindowState == WindowState.Minimized)
        {
            this.Hide();
            this.WindowState = WindowState.Normal; // 重置窗口状态，以便下次显示时是正常大小
            System.Diagnostics.Debug.WriteLine("窗口最小化，已隐藏到托盘");
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 只有当不是真正关闭时才取消关闭事件
        if (!isReallyClosing)
        {
            e.Cancel = true;
            this.Hide();
            System.Diagnostics.Debug.WriteLine("窗口关闭被拦截，已隐藏到托盘");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("应用程序正在真正关闭");
        }
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
        
        // 不再需要释放控制台，因为我们没有创建它
        // FreeConsole();
        
        base.OnClosed(e);
    }

    #region Native Methods
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
    #endregion
}