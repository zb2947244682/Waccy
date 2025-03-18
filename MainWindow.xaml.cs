using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        clipboardService = new ClipboardService(this);
        hotkeyService = new HotkeyService(this);
        trayIconService = new TrayIconService(clipboardService);

        // 注册热键 Alt+Shift+C
        hotkeyService.RegisterHotkey('C');

        // 绑定事件
        hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
        trayIconService.OpenRequested += TrayIconService_OpenRequested;
        trayIconService.ExitRequested += TrayIconService_ExitRequested;
    }

    private void HotkeyService_HotkeyPressed(object? sender, EventArgs e)
    {
        ToggleWindowVisibility();
    }

    private void TrayIconService_OpenRequested(object? sender, EventArgs e)
    {
        ShowWindow();
    }

    private void TrayIconService_ExitRequested(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ToggleWindowVisibility()
    {
        if (this.IsVisible)
        {
            this.Hide();
        }
        else
        {
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        this.Show();
        this.Activate();
        this.Focus();
        
        // 如果有项目，默认选择第一项
        if (clipboardService.History.Count > 0)
        {
            HistoryListView.SelectedIndex = 0;
            HistoryListView.Focus();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // 窗口失去焦点时隐藏
        this.Hide();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
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
        PasteSelectedItem();
    }

    private void PasteSelectedItem()
    {
        if (HistoryListView.SelectedItem is ClipboardItem selectedItem)
        {
            this.Hide();
            clipboardService.PasteSelected(selectedItem);
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

    protected override void OnClosed(EventArgs e)
    {
        // 清理资源
        hotkeyService.Dispose();
        trayIconService.Dispose();
        clipboardService.Dispose();
        
        base.OnClosed(e);
    }
}