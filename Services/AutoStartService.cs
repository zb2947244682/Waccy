using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Security.Principal;

namespace Waccy.Services
{
    public class AutoStartService
    {
        private const string TaskName = "WaccyClipboardManager";
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppValueName = "WaccyClipboardManager";

        /// <summary>
        /// 检查应用程序是否已设置为开机启动
        /// </summary>
        /// <returns>如果已设置为开机启动则返回true，否则返回false</returns>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                // 使用schtasks查询任务是否存在
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/query /tn \"{TaskName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    // 如果进程退出代码为0表示成功（任务存在）
                    return process.ExitCode == 0 && output.Contains(TaskName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查开机启动设置时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用或禁用应用程序的开机启动
        /// </summary>
        /// <param name="enable">如果为true则启用开机启动，为false则禁用</param>
        /// <returns>设置是否成功</returns>
        public static bool SetAutoStart(bool enable)
        {
            try
            {
                if (enable)
                {
                    // 获取当前用户
                    string currentUser = WindowsIdentity.GetCurrent().Name;
                    
                    // 获取应用程序路径
                    string appPath = GetExecutablePath();
                    string appDir = Path.GetDirectoryName(appPath);
                    
                    // 创建开机启动任务，使用最高权限
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/create /tn \"{TaskName}\" /tr \"\\\"{appPath}\\\"\" /sc onlogon /ru \"{currentUser}\" /rl highest /f",
                        CreateNoWindow = true,
                        UseShellExecute = true, // 使用true以显示UAC提示
                        Verb = "runas" // 请求管理员权限
                    };

                    using (Process process = Process.Start(psi))
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }
                else
                {
                    // 删除任务
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/delete /tn \"{TaskName}\" /f",
                        CreateNoWindow = true,
                        UseShellExecute = true, // 使用true以显示UAC提示
                        Verb = "runas" // 请求管理员权限
                    };

                    using (Process process = Process.Start(psi))
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置开机启动时出错: {ex.Message}");
                System.Windows.MessageBox.Show($"设置开机启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 清除旧的注册表启动项
        /// </summary>
        /// <returns>如果成功删除则返回true，否则返回false</returns>
        public static bool CleanupOldRegistryStartupItem()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                {
                    if (key == null) return false;
                    
                    // 检查是否存在旧的启动项
                    if (key.GetValue(AppValueName) != null)
                    {
                        // 删除旧的启动项
                        key.DeleteValue(AppValueName);
                        Debug.WriteLine("已删除旧的注册表启动项");
                        return true;
                    }
                    
                    // 如果没有找到启动项，也视为成功
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清除旧的注册表启动项时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取应用程序可执行文件的完整路径
        /// </summary>
        /// <returns>应用程序可执行文件的完整路径</returns>
        private static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }
    }
} 