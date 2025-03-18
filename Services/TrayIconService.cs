using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Waccy.Services;
using System.Windows.Threading;

namespace Waccy.Services
{
    public class TrayIconService
    {
        private NotifyIcon notifyIcon;
        private readonly ClipboardService clipboardService;
        private ToolStripMenuItem autoStartItem;
        private DispatcherTimer autoStartCheckTimer;

        public event EventHandler ExitRequested;
        public event EventHandler OpenRequested;

        public TrayIconService(ClipboardService clipboardService)
        {
            this.clipboardService = clipboardService;
            InitializeTrayIcon();
            
            // 创建定时器，定期检查自启动状态
            autoStartCheckTimer = new DispatcherTimer();
            autoStartCheckTimer.Interval = TimeSpan.FromMinutes(2); // 每2分钟检查一次
            autoStartCheckTimer.Tick += AutoStartCheckTimer_Tick;
            autoStartCheckTimer.Start();
        }

        private void AutoStartCheckTimer_Tick(object sender, EventArgs e)
        {
            // 定期更新自启动状态
            if (autoStartItem != null)
            {
                autoStartItem.Checked = AutoStartService.IsAutoStartEnabled();
            }
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = GetAppIcon(),
                Visible = true,
                Text = "Waccy 剪贴板管理器"
            };

            // 创建上下文菜单
            var contextMenu = new ContextMenuStrip();
            
            var openItem = new ToolStripMenuItem("打开剪贴板管理器");
            openItem.Click += (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty);
            
            var clearItem = new ToolStripMenuItem("清空剪贴板历史");
            clearItem.Click += (s, e) => clipboardService.ClearHistory();
            
            // 添加开机自启动选项
            autoStartItem = new ToolStripMenuItem("开机时启动本程序");
            // 设置初始勾选状态
            autoStartItem.Checked = AutoStartService.IsAutoStartEnabled();
            autoStartItem.Click += AutoStartItem_Click;
            
            // 添加清理旧启动项选项
            var cleanupItem = new ToolStripMenuItem("清理旧的启动项");
            cleanupItem.Click += CleanupItem_Click;

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(clearItem);
            contextMenu.Items.Add(autoStartItem);
            // contextMenu.Items.Add(cleanupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            
            // 双击托盘图标打开主窗口
            notifyIcon.DoubleClick += (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CleanupItem_Click(object sender, EventArgs e)
        {
            // 显示正在清理的消息
            notifyIcon.ShowBalloonTip(
                2000,
                "Waccy 剪贴板管理器",
                "正在清理旧的启动项...",
                ToolTipIcon.Info);
                
            // 清理旧的注册表启动项
            bool result = AutoStartService.CleanupOldRegistryStartupItem();
            
            // 显示结果消息
            string message = result ? 
                "已成功清理旧的启动项" : 
                "清理旧的启动项失败，可能是权限不足或没有旧启动项";
                
            notifyIcon.ShowBalloonTip(
                3000,
                "Waccy 剪贴板管理器",
                message,
                result ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private void AutoStartItem_Click(object sender, EventArgs e)
        {
            bool newState = !autoStartItem.Checked;
            
            // 显示正在设置的消息
            notifyIcon.ShowBalloonTip(
                2000,
                "Waccy 剪贴板管理器",
                newState ? "正在设置开机启动..." : "正在取消开机启动...",
                ToolTipIcon.Info);
            
            // 设置自启动状态
            if (AutoStartService.SetAutoStart(newState))
            {
                // 设置成功，更新菜单项勾选状态
                autoStartItem.Checked = newState;
                
                // 显示提示消息
                string message = newState ? 
                    "已设置为开机启动" : 
                    "已取消开机启动";
                    
                notifyIcon.ShowBalloonTip(
                    2000,
                    "Waccy 剪贴板管理器",
                    message,
                    ToolTipIcon.Info);
            }
        }

        private Icon GetAppIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            
            if (File.Exists(iconPath))
            {
                try
                {
                    return new Icon(iconPath);
                }
                catch (Exception)
                {
                    // 如果加载自定义图标失败，返回系统图标
                }
            }
            
            // 默认使用系统图标
            return SystemIcons.Application;
        }

        public void Dispose()
        {
            // 停止定时器
            if (autoStartCheckTimer != null)
            {
                autoStartCheckTimer.Stop();
                autoStartCheckTimer = null;
            }
            
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
} 