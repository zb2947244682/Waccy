using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using Waccy.Services;
using System;

namespace Waccy;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Mutex appMutex;
    private const string MutexName = "WaccyClipboardManagerMutex";
    private MainWindow mainWindow;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        Debug.WriteLine($"【应用启动】Application_Startup: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        
        // 在显示任何窗口之前先检查实例
        bool createdNew;
        appMutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // 如果已经有一个实例在运行，则退出
            Debug.WriteLine("已有Waccy实例在运行，显示提示并退出当前实例");
            System.Windows.MessageBox.Show("Waccy 剪贴板管理器已经在运行。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 尝试激活已有窗口
            Debug.WriteLine("尝试激活已有窗口");
            NativeMethods.ActivateExistingInstance();
            
            Shutdown();
            return;
        }

        // 清除旧的注册表启动项
        bool cleanupResult = AutoStartService.CleanupOldRegistryStartupItem();
        Debug.WriteLine($"清除旧的注册表启动项结果: {(cleanupResult ? "成功" : "失败")}");

        // 记录应用启动模式
        bool isAutoStartEnabled = AutoStartService.IsAutoStartEnabled();
        Debug.WriteLine($"开机自启动已启用: {isAutoStartEnabled}");

        // 记录启动信息
        Debug.WriteLine($"应用程序启动路径: {Process.GetCurrentProcess().MainModule.FileName}");
        Debug.WriteLine($"当前目录: {System.IO.Directory.GetCurrentDirectory()}");
        Debug.WriteLine($"是否具有管理员权限: {IsRunAsAdmin()}");
        
        // 注册异常处理
        RegisterExceptionHandlers();

        // 创建主窗口，参数表示直接启动到托盘
        Debug.WriteLine("创建主窗口实例");
        mainWindow = new MainWindow(true);
        
        // 设置为不在任务栏上显示
        mainWindow.ShowInTaskbar = false;
        
        // 设置主窗口
        this.MainWindow = mainWindow;
        
        // 直接让Window_Loaded事件处理隐藏到托盘的逻辑
        // 不需要在这里调用Show和Hide，避免闪烁
        Debug.WriteLine("初始化完成，由Window_Loaded事件处理窗口状态");
        
        Debug.WriteLine($"应用程序启动完成");
    }

    private void RegisterExceptionHandlers()
    {
        Debug.WriteLine("注册全局异常处理器");
        
        // 处理未处理的异常
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Debug.WriteLine($"【严重错误】未处理的异常: {exception?.Message}");
            Debug.WriteLine($"堆栈: {exception?.StackTrace}");
            System.Windows.MessageBox.Show($"发生未处理的异常: {exception?.Message}\n\n{exception?.StackTrace}", 
                            "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        
        Current.DispatcherUnhandledException += (s, args) =>
        {
            Debug.WriteLine($"【UI线程错误】未处理的异常: {args.Exception.Message}");
            Debug.WriteLine($"堆栈: {args.Exception.StackTrace}");
            System.Windows.MessageBox.Show($"发生UI线程未处理的异常: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                           "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    private bool IsRunAsAdmin()
    {
        // 检查当前进程是否具有管理员权限
        try
        {
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"检查管理员权限时出错: {ex.Message}");
            return false;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Debug.WriteLine($"【应用退出】OnExit: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        
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
        try 
        {
            Debug.WriteLine("释放互斥锁资源");
            appMutex?.ReleaseMutex();
            appMutex?.Dispose();
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"释放互斥锁时出错: {ex.Message}");
        }

        base.OnExit(e);
    }
}

