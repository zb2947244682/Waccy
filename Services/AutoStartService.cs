using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Waccy.Services
{
    public class AutoStartService
    {
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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
                {
                    if (key == null) return false;
                    
                    object value = key.GetValue(AppValueName);
                    return value != null && value.ToString() == GetExecutablePath();
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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                {
                    if (key == null) return false;
                    
                    if (enable)
                    {
                        // 设置应用程序为开机启动
                        key.SetValue(AppValueName, GetExecutablePath());
                    }
                    else
                    {
                        // 取消开机启动设置
                        if (key.GetValue(AppValueName) != null)
                        {
                            key.DeleteValue(AppValueName);
                        }
                    }
                    
                    return true;
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
        /// 获取应用程序可执行文件的完整路径
        /// </summary>
        /// <returns>应用程序可执行文件的完整路径</returns>
        private static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }
    }
} 