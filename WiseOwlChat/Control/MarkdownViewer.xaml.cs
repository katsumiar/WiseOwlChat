using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Wpf;
using Markdig.Wpf.ColorCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WiseOwlChat.Control
{
    /// <summary>
    /// MarkdownViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class MarkdownViewer : UserControl
    {
        public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
            "Markdown", typeof(string), typeof(MarkdownViewer), new PropertyMetadata(null, OnMarkdownChanged));

        public string Markdown
        {
            get { return (string)GetValue(MarkdownProperty); }
            set { SetValue(MarkdownProperty, value); }
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MarkdownViewer)d;
            control.UpdateMarkdown();
        }

        private const double CopiedDisplayTime = 0.6;

        public MarkdownViewer()
        {
            InitializeComponent();
            md.Pipeline = CustomPipeline();
        }

        string GetLastLine(string text)
        {
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return lines[^1];
        }

        private void UpdateMarkdown()
        {
            if (Markdown != null)
            {
                string lastLine = GetLastLine(Markdown);

                // Coltr.Markdig.Wpf.ColorCodeは、コードブロック時に空の情報を渡されると落ちるので条件から外す。
                bool cannotEnableBecauseOfColorCode = lastLine.StartsWith("```") && lastLine.Length > 3 || lastLine.Length == 0;
                
                if (!(cannotEnableBecauseOfColorCode))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        md.Markdown = Markdown;
                    }), DispatcherPriority.Loaded);
                }
            }
        }

        public MarkdownPipeline CustomPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseSupportedExtensions()
                .UseColorCodeWpf()
                .Build();
        }

        private void Paragraph_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Paragraph paragraph)
            {
                string textToCopy = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;

                ChatControl.Instance?.PopupMessage("Copied.", CopiedDisplayTime);
                Clipboard.SetText(textToCopy);
            }
        }

        private void PackIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ChatControl.Instance?.PopupMessage("Copied.", CopiedDisplayTime);
            Clipboard.SetText(Markdown);
        }
    }
}
