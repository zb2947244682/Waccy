using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using Waccy.Services;

namespace Waccy;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Mutex appMutex;
    private const string MutexName = "WaccyClipboardManager";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 检查是否已有实例在运行
        bool createdNew;
        appMutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // 如果已经有一个实例在运行，则退出
            System.Windows.MessageBox.Show("Waccy 剪贴板管理器已经在运行。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 尝试在应用程序级别设置低级键盘钩子
        try
        {
            // 注册低级别键盘钩子
            LowLevelKeyboardHook.SetHook();
            Debug.WriteLine("应用程序级别已设置低级键盘钩子");
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"在应用程序级别设置键盘钩子时出错: {ex.Message}");
            // 继续运行，稍后会在主窗口中再次尝试设置钩子
        }

        // 继续正常启动
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 移除低级键盘钩子
        try
        {
            LowLevelKeyboardHook.Unhook();
            Debug.WriteLine("在应用程序退出时移除低级键盘钩子");
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"移除键盘钩子时出错: {ex.Message}");
        }

        // 释放互斥锁资源
        appMutex?.ReleaseMutex();
        appMutex?.Dispose();

        base.OnExit(e);
    }
}

