using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Waccy.Services;

namespace Waccy.Services
{
    public class TrayIconService
    {
        private NotifyIcon notifyIcon;
        private readonly ClipboardService clipboardService;

        public event EventHandler ExitRequested;
        public event EventHandler OpenRequested;

        public TrayIconService(ClipboardService clipboardService)
        {
            this.clipboardService = clipboardService;
            InitializeTrayIcon();
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
            
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(clearItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);
            
            notifyIcon.ContextMenuStrip = contextMenu;
            
            // 双击托盘图标打开主窗口
            notifyIcon.DoubleClick += (s, e) => OpenRequested?.Invoke(this, EventArgs.Empty);
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
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
} 