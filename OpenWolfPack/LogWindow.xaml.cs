using ControlzEx.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenWolfPack
{
    /// <summary>
    /// LogWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LogWindow : Window
    {
        private static LogWindow? instance;

        public static LogWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LogWindow();
                    instance.ClearLog();
                }
                if (!instance.IsActive)
                {
                    if (instance.LogRichTextBox.Document.Blocks.Count > 0)
                    {
                        instance.Show();
                    }
                }
                return instance;
            }
        }

        private LogWindow()
        {
            InitializeComponent();
        }

        public static void LogClose()
        {
            Instance.Close();
        }

        public LogWindow ClearLog()
        {
            LogRichTextBox.Document.Blocks.Clear();
            return this;
        }

        public LogWindow AppendLog(string message, Brush color, bool isNewLine)
        {
            Run run = new Run(message) { Foreground = color };
            Paragraph paragraph = new Paragraph(run);

            if (isNewLine)
            {
                paragraph.Inlines.Add(new Run("\n"));
            }

            LogRichTextBox.Document.Blocks.Add(paragraph);

            // スクロール位置を取得
            var scrollViewer = GetDescendantByType(LogRichTextBox, typeof(ScrollViewer)) as ScrollViewer;
            if (scrollViewer != null)
            {
                // 最下部にある場合のみスクロール
                if (scrollViewer.VerticalOffset.Equals(scrollViewer.ScrollableHeight))
                {
                    LogRichTextBox.ScrollToEnd();
                }
            }
            return this;
        }

        public static Visual? GetDescendantByType(Visual? element, Type type)
        {
            if (element == null) return null;
            if (element.GetType() == type) return element;
            Visual? foundElement = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual? visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }
    }
}