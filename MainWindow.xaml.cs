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
using System.Text;

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
    private bool isAutoStarted = false; // 添加标志，标识是否通过开机自启动启动
    private bool startMinimizedToTray = false; // 添加标志，标识是否应该直接启动到托盘
    private bool isInTray = true; // 简单标记是否在托盘状态
    private bool blockDeactivation = false; // 防止窗口显示后立即因失焦而隐藏
    
    // 记录启动时间以便在日志中显示时间戳
    private DateTime appStartTime = DateTime.Now;
    
    // 日志缓冲区，避免过多UI更新
    private StringBuilder logBuffer = new StringBuilder();

    private bool isLoading = true; // 添加标记，表示窗口正在加载中

    public MainWindow(bool startMinimizedToTray = false)
    {
        appStartTime = DateTime.Now;
        
        // 输出第一条日志
        LogMessage($"应用程序构造函数开始: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        LogMessage($"传入参数 startMinimizedToTray={startMinimizedToTray}");
        
        // 设置是否直接启动到托盘
        this.startMinimizedToTray = startMinimizedToTray;
        
        // 初始时设为托盘状态
        isInTray = true;
        
        // 检查是否通过开机自启动运行
        isAutoStarted = AutoStartService.IsAutoStartEnabled();
        LogMessage($"isAutoStarted={isAutoStarted}");
        
        // 设置窗口不在任务栏显示
        this.ShowInTaskbar = false;
        
        // 确保窗口初始是隐藏的（即使XAML中设置了也再次确认）
        this.Visibility = Visibility.Hidden;
        
        // 初始化组件
        InitializeComponent();
        LogMessage("InitializeComponent完成");

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
        
        // 创建一个计时器，在应用启动后3秒将isLoading设为false
        System.Windows.Threading.DispatcherTimer initTimer = new System.Windows.Threading.DispatcherTimer();
        initTimer.Interval = TimeSpan.FromSeconds(1);
        initTimer.Tick += (s, e) => {
            LogMessage("应用程序加载完成，启用F7热键和托盘图标功能");
            isLoading = false;
            initTimer.Stop();
        };
        initTimer.Start();
        
        LogMessage($"构造函数完成，窗口状态: Visibility={this.Visibility}, WindowState={this.WindowState}, isInTray={isInTray}");
    }

    // 添加日志记录方法
    private void LogMessage(string message)
    {
        // 计算时间差
        TimeSpan elapsed = DateTime.Now - appStartTime;
        string timeStamp = $"[{elapsed:hh\\:mm\\:ss\\.fff}]";
        
        // 添加到缓冲区
        logBuffer.AppendLine($"{timeStamp} {message}");
        
        // 如果LogTextBox已经初始化，则更新UI
        if (LogTextBox != null)
        {
            // 使用Dispatcher确保在UI线程上更新
            this.Dispatcher.Invoke(() =>
            {
                LogTextBox.Text = logBuffer.ToString();
                LogTextBox.ScrollToEnd();
            });
        }
        
        // 同时输出到调试窗口
        System.Diagnostics.Debug.WriteLine($"{timeStamp} {message}");
    }

    private void InitializeServices()
    {
        LogMessage("开始初始化服务");
        
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
            
            LogMessage("低级键盘钩子已设置，按F7将呼出/隐藏程序窗口");
        }
        catch (Exception ex)
        {
            LogMessage($"设置键盘钩子时出错: {ex.Message}");
            System.Windows.MessageBox.Show($"设置键盘钩子时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 绑定托盘图标事件
        trayIconService.OpenRequested += TrayIconService_OpenRequested;
        trayIconService.ExitRequested += TrayIconService_ExitRequested;
        
        LogMessage("服务初始化完成");
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
            LogMessage("F7键被按下，切换窗口可见性");
            
            // 在UI线程上运行，使用Invoke而不是InvokeAsync，确保操作完成
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LogMessage($"准备切换窗口状态: isInTray={isInTray}, Visibility={this.Visibility}, IsVisible={this.IsVisible}, WindowState={this.WindowState}");
                
                // 检查应用是否仍在加载
                if (isLoading)
                {
                    LogMessage("应用程序正在加载中，稍后再试");
                    return;
                }
                
                // 简单切换窗口可见性
                ToggleWindowVisibility();
            });
        }
    }

    private void TrayIconService_OpenRequested(object? sender, System.EventArgs e)
    {
        // 使用Dispatcher确保UI线程上操作
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LogMessage($"托盘图标被点击，当前窗口状态: isInTray={isInTray}, Visibility={this.Visibility}, WindowState={this.WindowState}");
            
            // 检查应用是否仍在加载
            if (isLoading)
            {
                LogMessage("应用程序正在加载中，稍后再试");
                return;
            }
            
            // 切换窗口显示状态，与F7按键行为一致
            ToggleWindowVisibility();
        });
    }

    private void TrayIconService_ExitRequested(object? sender, System.EventArgs e)
    {
        // 设置真正关闭标志
        LogMessage("退出程序被请求");
        isReallyClosing = true;
        System.Windows.Application.Current.Shutdown();
    }

    private void ToggleWindowVisibility()
    {
        LogMessage($"切换窗口可见性，当前状态: isInTray={isInTray}, Visibility={this.Visibility}, WindowState={this.WindowState}");
        
        // 检查实际窗口状态而不仅仅依赖isInTray标志
        bool actuallyVisible = this.IsVisible && this.Visibility == Visibility.Visible;
        LogMessage($"实际窗口可见状态: IsVisible={this.IsVisible}, Visibility={this.Visibility}, actuallyVisible={actuallyVisible}");
        
        if (isInTray || !actuallyVisible)
        {
            // 如果状态不一致，重置状态
            if (actuallyVisible && isInTray)
            {
                LogMessage("状态不一致：窗口可见但isInTray=true，重置状态");
                isInTray = false;
            }
            
            ShowFromTray();
        }
        else
        {
            // 如果状态不一致，重置状态
            if (!actuallyVisible && !isInTray)
            {
                LogMessage("状态不一致：窗口不可见但isInTray=false，重置状态");
                isInTray = true;
            }
            
            HideToTray();
        }
    }

    private void ShowFromTray()
    {
        LogMessage($"调用ShowFromTray(), 当前状态: isInTray={isInTray}, Visibility={this.Visibility}, WindowState={this.WindowState}");
        
        // 标记不再是加载状态，防止与Loaded事件冲突
        isLoading = false;
        
        // 不检查isInTray状态，强制执行显示操作
        // 清空搜索框
        SearchBox.Text = string.Empty;
        
        // 重置为全部分类
        CategoryTabs.SelectedIndex = 0;
        
        // 确保窗口状态为正常
        this.WindowState = WindowState.Normal;
        
        // 确保窗口不在任务栏显示
        this.ShowInTaskbar = false;
        
        // 设置窗口位置居中
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        // 防止显示后立即因失焦而隐藏
        blockDeactivation = true;
        LogMessage("激活焦点保护");
        
        // 显示窗口前先设置为顶层窗口，强制获取焦点
        this.Topmost = true;
        
        // 强制显示窗口，确保可见性正确设置
        this.Visibility = Visibility.Visible;
        LogMessage($"设置Visibility=Visible后: Visibility={this.Visibility}");
        
        // 显示窗口并强制刷新UI
        this.Show();
        // 强制刷新UI线程，确保Visibility设置被应用
        System.Windows.Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        
        // 强制重新布局和渲染 - 添加额外验证
        this.UpdateLayout();
        LogMessage($"执行Show()和UpdateLayout()后状态: Visibility={this.Visibility}, WindowState={this.WindowState}, IsVisible={this.IsVisible}");
        
        // 再次确认可见性设置
        if (this.Visibility != Visibility.Visible)
        {
            LogMessage("可见性状态不正确，强制重新设置");
            this.Visibility = Visibility.Visible;
            this.Show();
            this.UpdateLayout();
            System.Windows.Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            LogMessage($"强制重设后状态: Visibility={this.Visibility}, IsVisible={this.IsVisible}");
        }
        
        // 激活窗口并设置焦点
        this.Activate();
        this.Focus();
        LogMessage("执行Activate()和Focus()");
        
        // 延迟取消Topmost状态和保护
        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(500); // 减少延迟时间
        timer.Tick += (s, e) => {
            this.Topmost = false;
            LogMessage("取消顶层状态，保持保护");
            timer.Stop();
            
            // 再延迟一秒取消保护
            System.Windows.Threading.DispatcherTimer protectionTimer = new System.Windows.Threading.DispatcherTimer();
            protectionTimer.Interval = TimeSpan.FromMilliseconds(1000);
            protectionTimer.Tick += (ps, pe) => {
                blockDeactivation = false;
                LogMessage("取消激活保护");
                protectionTimer.Stop();
            };
            protectionTimer.Start();
        };
        timer.Start();
        
        // 如果有项目，默认选择第一项
        if (filteredItems.View.Cast<ClipboardItem>().Any())
        {
            HistoryListView.SelectedIndex = 0;
            HistoryListView.Focus();
        }
        
        // 更新状态标志
        isInTray = false;
        
        LogMessage($"窗口已从托盘显示，设置isInTray=false，最终状态: Visibility={this.Visibility}, WindowState={this.WindowState}, IsVisible={this.IsVisible}");
    }
    
    private void HideToTray()
    {
        LogMessage($"调用HideToTray(), 当前状态: isInTray={isInTray}, Visibility={this.Visibility}, WindowState={this.WindowState}");
        
        // 不检查isInTray状态，强制执行隐藏操作
        // 首先将窗口状态重置为正常
        this.WindowState = WindowState.Normal;
        
        // 然后隐藏窗口
        this.Visibility = Visibility.Hidden;
        this.Hide();
        
        // 确保窗口不在任务栏显示
        this.ShowInTaskbar = false;
        
        // 更新状态标志
        isInTray = true;
        
        LogMessage($"窗口已隐藏到托盘, isInTray设为true, 最终状态: Visibility={this.Visibility}, WindowState={this.WindowState}");
    }

    private void Window_Deactivated(object sender, System.EventArgs e)
    {
        // 详细记录当前状态
        LogMessage($"窗口失去焦点事件: isLoading={isLoading}, blockDeactivation={blockDeactivation}, " + 
                  $"isInTray={isInTray}, Visibility={this.Visibility}, IsVisible={this.IsVisible}");
        
        // 如果窗口正在加载或处于保护状态，忽略失焦事件
        if (isLoading || blockDeactivation)
        {
            LogMessage("窗口失去焦点，但处于加载或保护状态，不隐藏");
            return;
        }
        
        // 增加额外检查 - 如果窗口设置为可见但实际上还不可见，可能是渲染还未完成
        if (this.Visibility == Visibility.Visible && !this.IsVisible)
        {
            LogMessage("窗口正在渲染过程中，暂不隐藏");
            return;
        }
        
        // 当窗口失去焦点时自动隐藏到托盘
        if (isInitialized && !isInTray)
        {
            // 记录详细信息以帮助诊断问题
            LogMessage($"窗口失去焦点，隐藏到托盘，当前状态: isInTray={isInTray}, Visibility={this.Visibility}, WindowState={this.WindowState}");
            
            // 获取当前活动窗口的信息
            try 
            {
                var activatedHandle = NativeMethods.GetForegroundWindow();
                uint processId = 0;
                NativeMethods.GetWindowThreadProcessId(activatedHandle, out processId);
                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                LogMessage($"当前获得焦点的窗口: {process.ProcessName} (PID: {processId})");
            }
            catch (Exception ex)
            {
                LogMessage($"获取前台窗口信息时出错: {ex.Message}");
            }
            
            HideToTray();
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            // 按下ESC键隐藏窗口到托盘
            LogMessage("ESC键按下，隐藏窗口");
            HideToTray();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            // 按下Enter键粘贴选中项
            LogMessage("Enter键按下，执行粘贴");
            PasteSelectedItem();
            e.Handled = true;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 标记窗口已初始化
        isInitialized = true;
        
        // 确保窗口不在任务栏显示
        this.ShowInTaskbar = false;
        
        // 详细记录当前窗口状态
        LogMessage($"Window_Loaded: isAutoStarted={isAutoStarted}, startMinimizedToTray={startMinimizedToTray}, " +
                   $"当前状态: Visibility={this.Visibility}, WindowState={this.WindowState}, isLoading={isLoading}, " +
                   $"IsVisible={this.IsVisible}, isInTray={isInTray}");
        
        // 检查是否处于显示操作中 - 如果刚刚调用了ShowFromTray或正在显示窗口，则不要干扰
        if (this.Visibility == Visibility.Visible && !isInTray && !startMinimizedToTray)
        {
            LogMessage("窗口已处于显示状态，不执行隐藏操作");
            isLoading = false;  // 确保标记加载完成
            return;
        }
        
        // 如果是通过开机自启动或设置了直接启动到托盘，保持托盘状态
        if (isAutoStarted || startMinimizedToTray)
        {
            LogMessage("程序直接启动到托盘");
            // 立即执行，不使用延迟
            this.Hide();
            isInTray = true;
            LogMessage($"窗口已隐藏到托盘，状态: Visibility={this.Visibility}, WindowState={this.WindowState}, isInTray={isInTray}");
        }
        
        // 无论如何，确保在Window_Loaded后将isLoading设为false
        LogMessage($"Window_Loaded完成，设置isLoading=false，当前状态: Visibility={this.Visibility}, IsVisible={this.IsVisible}");
        isLoading = false;
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
            LogMessage("执行粘贴选中项");
            HideToTray();
            
            // 使用更长的延迟确保窗口完全隐藏并且系统可以切换到前一个窗口
            System.Threading.Tasks.Task.Delay(300).ContinueWith(_ =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        LogMessage("正在执行粘贴操作...");
                        clipboardService.PasteSelected(selectedItem);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"粘贴操作失败: {ex.Message}");
                    }
                });
            });
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        LogMessage("清除历史记录");
        clipboardService.ClearHistory();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        LogMessage("关闭按钮点击，隐藏到托盘");
        HideToTray();
    }

    private void ToggleLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogExpander.IsExpanded = !LogExpander.IsExpanded;
        ToggleLogButton.Content = LogExpander.IsExpanded ? "隐藏日志" : "显示日志";
        LogMessage($"日志面板 {(LogExpander.IsExpanded ? "展开" : "折叠")}");
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        logBuffer.Clear();
        LogTextBox.Clear();
        LogMessage("日志已清空");
    }

    private void CopyLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(LogTextBox.Text);
            LogMessage("日志已复制到剪贴板");
        }
        catch (Exception ex)
        {
            LogMessage($"复制日志失败: {ex.Message}");
        }
    }

    private void Window_StateChanged(object sender, System.EventArgs e)
    {
        LogMessage($"窗口状态改变: WindowState={this.WindowState}");
        
        // 当窗口最小化时，隐藏窗口并显示在托盘
        if (this.WindowState == WindowState.Minimized)
        {
            LogMessage("窗口最小化，隐藏到托盘");
            HideToTray();
            this.WindowState = WindowState.Normal; // 重置窗口状态，以便下次显示时是正常大小
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        LogMessage($"Window_Closing: isReallyClosing={isReallyClosing}");
        
        // 只有当不是真正关闭时才取消关闭事件
        if (!isReallyClosing)
        {
            e.Cancel = true;
            LogMessage("取消关闭，隐藏到托盘");
            HideToTray();
        }
        else
        {
            LogMessage("应用程序正在真正关闭");
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        LogMessage("OnClosed被调用");
        
        // 移除低级键盘钩子
        try
        {
            LowLevelKeyboardHook.KeyDown -= LowLevelKeyboardHook_KeyDown;
            LowLevelKeyboardHook.Unhook();
        }
        catch (Exception ex)
        {
            LogMessage($"移除键盘钩子时出错: {ex.Message}");
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