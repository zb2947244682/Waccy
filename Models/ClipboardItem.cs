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

        public string Preview
        {
            get
            {
                switch (Type)
                {
                    case ClipboardItemType.Text:
                        string text = Content as string;
                        if (text.Length > 50)
                            return text.Substring(0, 50) + "...";
                        return text;
                    case ClipboardItemType.Image:
                        return "[图片]";
                    case ClipboardItemType.FilePath:
                        string path = Content as string;
                        return $"[文件] {System.IO.Path.GetFileName(path)}";
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