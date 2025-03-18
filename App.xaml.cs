using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;

namespace Waccy;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
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
            MessageBox.Show("Waccy 剪贴板管理器已经在运行。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 继续正常启动
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 释放互斥锁资源
        appMutex?.ReleaseMutex();
        appMutex?.Dispose();

        base.OnExit(e);
    }
}

