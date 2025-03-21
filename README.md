# Waccy 剪贴板管理器 📋

Waccy是一个轻量级Windows平台剪贴板管理器，类似于macOS上的Maccy，用于记录和管理剪贴板历史记录。让复制粘贴更加高效！ ⚡

## ✨ 功能特点

- 📝 实时监控系统剪贴板，自动捕获文本、图片和文件路径
- 🔄 记录最近复制的10条历史记录
- ⌨️ 使用F7(或自定义快捷键)快速呼出或隐藏管理器窗口
- 🔍 支持按文本内容和类型筛选历史记录
- 🎯 双击或按Enter键将选中内容粘贴到当前焦点位置
- 🏃 系统托盘运行，零闪烁，无干扰工作流
- 🚀 支持自启动并开机时最小化到托盘
- 📊 丰富的日志记录功能，方便调试和问题排查

## 🛠️ 开发环境

- .NET 8.0
- C# 12.0
- WPF (Windows Presentation Foundation)
- Visual Studio 2022

## 🚀 编译和运行

### 方法1：使用Visual Studio

1. 使用Visual Studio 2022打开`Waccy.sln`解决方案文件
2. 确保已安装.NET 8.0 SDK
3. 按F5运行或按Ctrl+Shift+B进行编译

### 方法2：使用命令行

1. 确保已安装.NET 8.0 SDK
2. 打开命令提示符或PowerShell，导航到项目文件夹
3. 运行以下命令：

```bash
dotnet build
dotnet run
```

## 📖 使用方法

1. 🚀 启动应用程序后，它将在系统托盘中运行（右下角通知区域）
2. ⌨️ 按下F7键呼出剪贴板历史记录窗口
3. ⬆️⬇️ 使用上下箭头键选择要粘贴的内容
4. ↩️ 按Enter键将选中内容粘贴到当前焦点位置
5. 🖱️ 双击列表项也可以粘贴内容
6. 🗑️ 点击"清空历史"按钮可以清除所有历史记录
7. ❌ 点击"关闭"按钮或按ESC键可以隐藏窗口（程序仍在后台运行）
8. 🖱️ 右键点击系统托盘图标可以打开、清空历史或退出程序

## ⚠️ 注意事项

- 💻 应用程序一次只能运行一个实例
- 🔄 窗口失去焦点时会自动隐藏
- 🖼️ 图片和文件路径会在列表中显示类型提示而非实际内容
- 📝 如需额外的技术日志，可展开窗口下方的日志区域

## 🛠️ 最近修复的问题

### 窗口显示与闪烁问题

应用程序经过多轮优化，解决了以下问题：

1. 🔧 启动时窗口闪烁问题 - 通过在App.xaml设置ShutdownMode为OnExplicitShutdown，并在MainWindow.xaml设置初始Visibility为Hidden解决
2. 🔧 首次按F7时UI未渲染问题 - 通过强制UI渲染更新和额外的状态检查机制解决
3. 🔧 托盘图标点击行为不一致问题 - 统一使用ToggleWindowVisibility处理所有显示/隐藏窗口的请求
4. 🔧 窗口加载与状态管理冲突 - 优化了Window_Loaded与ShowFromTray之间的交互

## 💡 开发心得与难点

### 主要难点

1. **🔄 WPF窗口可见性管理** - WPF中窗口的Visibility、IsVisible、Show()、Hide()等状态和方法之间的复杂交互是开发过程中最大的挑战。窗口可能设置了Visible但实际UI未渲染完成，导致黑屏问题。

2. **⚡ UI线程与渲染时机** - WPF的UI渲染是异步的，有时候设置了Visibility=Visible后，UI可能还未完成渲染就被其他事件（如失焦）中断，导致窗口闪烁。

3. **⏱️ 事件触发顺序** - Window_Loaded、Show()、Deactivated等事件的触发顺序和时机需要精确控制，否则会导致窗口显示行为不一致。

4. **🔄 系统托盘交互** - 确保应用程序在托盘中的行为稳定，特别是处理启动和退出时的状态管理。

### 解决思路

1. **🎯 状态标志管理** - 引入明确的状态标志（isInTray、blockDeactivation、isLoading等）来跟踪窗口的实际状态。

2. **🔄 强制UI更新** - 使用Dispatcher.Invoke和UpdateLayout()强制UI线程完成渲染，确保窗口在显示前UI已准备就绪。

3. **🛡️ 保护机制** - 实现临时的焦点保护机制，防止窗口在刚显示时就因失焦而隐藏。

4. **📝 详细日志** - 添加全面的日志系统，记录窗口状态变化的每个步骤，帮助排查问题。

5. **✅ 状态一致性检查** - 在关键点检查状态一致性，确保内部状态标志与实际窗口状态一致。

这些经验对于开发其他需要在系统托盘运行且需要平滑UI体验的WPF应用程序非常有价值。

## 📄 许可证

此软件采用MIT许可证。详见LICENSE文件。