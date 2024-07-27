using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace WiseOwlChat.Control
{
    /// <summary>
    /// ChatControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ChatControl : UserControl
    {
        private ChatViewModel chatViewModel;

        public static ChatControl? Instance { get; private set; }

        public ChatControl()
        {
            InitializeComponent();

            chatViewModel = new ChatViewModel();

            chatViewModel.popupMessageAction = (msg) => { PopupMessage(msg, 2.0); };

            ((INotifyCollectionChanged)chatViewModel.MarkdownViewers).CollectionChanged += OnMarkdownViewersChanged;

            userMessageEdit.SendCommand = new RelayCommand(
                message =>
                {
                    SetRequest(message);
                });

            chatViewModel.StopCommand = new RelayCommand(
                param =>
                {
                    chatViewModel.IsStop = true;
                    chatViewModel.StopVisibility = Visibility.Collapsed;
                });

            DataContext = chatViewModel;

            scrollViewer.ScrollToEnd();

            modelSelector.Dispatcher.BeginInvoke(new Action(() =>
            {
                chatViewModel.ModelType = OpenAIChat.MODEL_TYPE.GPT_4o_mini.ToString();
            }), DispatcherPriority.Loaded);

            Instance = this;
        }

        private void SetRequest(object? message)
        {
            chatViewModel.Request(
                () =>
                {
                    // どうしてもリアルタイムに更新されないので強制更新する。
                    DoEvents();
                    DoNewLine();
                },
                message
                );

            training();

            modelSelector.Visibility = Visibility.Collapsed;
        }

        private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(ExitFrames);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        public void PopupMessage(string message, double displayTime)
        {
            Snackbar.MessageQueue?.Enqueue(
                        message,
                        null,
                        null,
                        null,
                        false,
                        true,
                        TimeSpan.FromSeconds(displayTime));
        }

        private object? ExitFrames(object obj)
        {
            ((DispatcherFrame)obj).Continue = false;

            return null;
        }

        private void training()
        {
            if (chatViewModel.TrainingMode)
            {
                chatViewModel.PartnerReflection();
            }
        }

        public void ClearMessage(ObservableCollection<ConversationInfo> ConversationInfos)
        {
            if (!IsBusy())
            {
                training();
                chatViewModel.ClearMessage(ConversationInfos);
                modelSelector.Visibility = Visibility.Visible;
            }
        }

        public void SetupMessage(ConversationInfo conversation)
        {
            if (!IsBusy())
            {
                training();
                chatViewModel.SetupMessage(conversation);
                modelSelector.Visibility = Visibility.Collapsed;
                chatViewModel.FunctionMode = false;
            }
        }

        public bool IsBusy()
        {
            return userMessageEdit.IsBusy;
        }

        private void OnMarkdownViewersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                DoNewLine();
            }
        }

        private void DoNewLine()
        {
            if (scrollViewer.VerticalOffset.Equals(scrollViewer.ScrollableHeight))
            {
                scrollViewer.ScrollToEnd(); // ScrollViewerを最下部へスクロール
            }
        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
                scrollViewer.LineDown();
            else
                scrollViewer.LineUp();
        }

        private void OnHyperlinkExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is string uri)
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);

                e.Handled = true;
            }
        }

        private void OnImageExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show($"URL: {e.Parameter}");
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            userMessageEdit.MaxHeight = this.ActualHeight / 2;
        }
    }

    public class RoleToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ConversationEntry.BrushesConvert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RoleTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ExpanderTemplate { get; set; }
        public DataTemplate? DefaultTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is MarkdownViewerViewModel entry)
            {
                if (ConversationEntry.IsAdvice(entry.Role))
                {
                    return ExpanderTemplate;
                }
            }
            return DefaultTemplate;
        }
    }
}
