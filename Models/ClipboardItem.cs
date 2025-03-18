using System;
using System.Windows.Media.Imaging;

namespace Waccy.Models
{
    public enum ClipboardItemType
    {
        Text,
        Image,
        FilePath
    }

    public class ClipboardItem
    {
        public ClipboardItemType Type { get; set; }
        public object Content { get; set; }
        public DateTime Timestamp { get; set; }

        // 添加搜索支持
        public string SearchContent 
        { 
            get 
            {
                switch (Type)
                {
                    case ClipboardItemType.Text:
                        return Content as string;
                    case ClipboardItemType.FilePath:
                        return Content as string;
                    default:
                        return string.Empty;
                }
            }
        }

        // 图标字符 (使用 Segoe MDL2 Assets 字体)
        public string TypeIcon
        {
            get
            {
                switch (Type)
                {
                    case ClipboardItemType.Text:
                        return "\uE8C4"; // 文本图标
                    case ClipboardItemType.Image:
                        return "\uEB9F"; // 图片图标
                    case ClipboardItemType.FilePath:
                        return "\uE8B7"; // 文件图标
                    default:
                        return "\uE887"; // 默认图标
                }
            }
        }

        public string Preview
        {
            get
            {
                switch (Type)
                {
                    case ClipboardItemType.Text:
                        string text = Content as string;
                        if (text?.Length > 50)
                            return text.Substring(0, 50) + "...";
                        return text;
                    case ClipboardItemType.Image:
                        return "[图片]";
                    case ClipboardItemType.FilePath:
                        string path = Content as string;
                        return System.IO.Path.GetFileName(path);
                    default:
                        return "[未知]";
                }
            }
        }

        public ClipboardItem(ClipboardItemType type, object content)
        {
            Type = type;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }
} 