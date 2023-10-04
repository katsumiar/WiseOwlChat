using HttpMultipartParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace WiseOwlChat.Control
{
    public class MessageEventArgs
    {
        public Action? Lock { get; set; }
        public string? Message { get; set; }
        public Directions? Direction { get; set; }
        public Action? Unlock { get; set; }
    }
    public delegate void MessageSentDelegate(MessageEventArgs args);

    public class MessageInputViewModel : INotifyPropertyChanged
    {
        private string? _inputText;
        public string? InputText
        {
            get { return _inputText; }
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAccept));
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAccept));
                    OnPropertyChanged(nameof(VisibilityBusy));
                }
            }
        }

        public Visibility VisibilityBusy
        {
            get
            {
                if (IsBusy)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public bool IsAccept
        {
            get
            {
                if (_inputText == null)
                {
                    return false;
                }
                return _inputText.Length != 0 && !IsBusy;
            }
        }

        public ICommand SendCommand { get; }
        public ICommand SendWebCommand { get; }

        public ICommand? UndoCommand { get; set; }
        public ICommand? RedoCommand { get; set; }
        public ICommand? PasteCommand { get; set; }
        public ICommand? UpHistoryCommand { get; set; }
        public ICommand? DownHistoryCommand { get; set; }

        public MessageInputViewModel()
        {
            SendCommand = new RelayCommand((c) => SendQuery());
            SendWebCommand = new RelayCommand((c) => SendUrlQuery());
        }

        public event MessageSentDelegate? MessageSent;

        public void SendQuery(string? text = null)
        {
            if (!IsBusy)
            {
                MessageSent?.Invoke(CreateMessageEvent(text));

                InputText = "";
            }
        }

        public void SendUrlQuery(string? text = null)
        {
            if (!IsBusy)
            {
                MessageSent?.Invoke(CreateUrlMessageEvent(text));

                InputText = "";
            }
        }

        private MessageEventArgs CreateMessageEvent(string? text)
        {
            return new MessageEventArgs
            {
                Lock = () => { IsBusy = true; },
                Message = text ?? _inputText,
                Unlock = () => { IsBusy = false; }
            };
        }

        public MessageEventArgs CreateUrlMessageEvent(string? text)
        {
            return new MessageEventArgs
            {
                Lock = () => { IsBusy = true; },
                Message = text ?? _inputText,
                Direction = Directions.URL_Explanation_Prompt,
                Unlock = () => { IsBusy = false; }
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// MessageInputControl.xaml の相互作用ロジック
    /// </summary>
    public partial class MessageInputControl : UserControl
    {
        public static readonly DependencyProperty SendCommandProperty =
            DependencyProperty.Register("SendCommand", typeof(ICommand), typeof(MessageInputControl), new PropertyMetadata(null));

        public ICommand SendCommand
        {
            get { return (ICommand)GetValue(SendCommandProperty); }
            set { SetValue(SendCommandProperty, value); }
        }

        private MessageInputViewModel? inputViewModel { get; set; }

        private UndoRedoStack<(string, int)> undoRedoStack = new();
        private History<string> queryHistory = new();

        public static MessageInputControl? Instance = null;

        public MessageInputControl()
        {
            InitializeComponent();

            inputViewModel = new MessageInputViewModel();

            inputViewModel.UndoCommand = new RelayCommand(param => ExecuteUndo());
            inputViewModel.RedoCommand = new RelayCommand(param => ExecuteRedo());
            inputViewModel.PasteCommand = new RelayCommand(param => ExecutePaste());
            inputViewModel.MessageSent +=
                (c) =>
                {
                    if (c.Message != null)
                    {
                        queryHistory.AddHistory(c.Message);
                    }
                    SendCommand?.Execute(c);
                };
            inputViewModel.UpHistoryCommand = new RelayCommand(
                param =>
                {
                    queryHistory.SetCurrentHistory(inputText.Text);
                    ExecuteHistory(queryHistory.TryPreviousHistory);
                });
            inputViewModel.DownHistoryCommand = new RelayCommand(param => ExecuteHistory(queryHistory.TryNextHistory));

            this.DataContext = inputViewModel;
            inputViewModel.IsBusy = false;

            undoRedoStack.Push((inputText.Text, inputText.CaretIndex));

            Instance = this;
        }

        private bool isHistoryMode = false;
        private void ExecuteHistory(History<string>.HistoryDelegate historyFunc)
        {
            if (historyFunc(out string? history))
            {
                isHistoryMode = true;
                inputText.Text = history;
                isHistoryMode = false;
            }
        }

        private void inputText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isHistoryMode)
            {
                undoRedoStack.Push((inputText.Text, inputText.CaretIndex));
            }
        }

        private void ExecuteUndo()
        {
            undoRedoStack.TryUndo((inputText.Text, inputText.CaretIndex),
                (o) => 
                {
                    inputText.Text = o.Item1; 
                    inputText.CaretIndex = o.Item2;
                });
        }

        private void ExecuteRedo()
        {
            undoRedoStack.TryRedo((inputText.Text, inputText.CaretIndex),
                (o) =>
                {
                    inputText.Text = o.Item1; 
                    inputText.CaretIndex = o.Item2;
                });
        }

        private void ExecutePaste()
        {
            string clipboardText = Clipboard.GetText();

            if (!string.IsNullOrEmpty(clipboardText))
            {
                clipboardText = clipboardText.Trim();
                
                if (clipboardText.IndexOf(Environment.NewLine) == -1)
                {
                    inputText.Text = inputText.Text.Insert(inputText.CaretIndex, clipboardText);
                }
                else
                {
                    string wrappedText = Environment.NewLine + "```" + Environment.NewLine + clipboardText + Environment.NewLine + "```";
                    inputText.Text = inputText.Text.Insert(inputText.CaretIndex, wrappedText);
                }
            }
        }

        public void SetRequest(string query)
        {
            inputViewModel?.SendQuery(query);
        }

        public void SetUrlRequest(string query)
        {
            inputViewModel?.SendUrlQuery(query);
        }

        public bool IsBusy
        { 
            get
            {
                if (inputViewModel == null)
                {
                    return false;
                }
                return inputViewModel.IsBusy;
            }
        }

        private void inputText_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void inputText_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                string insertContents = string.Empty;
                foreach (string file in files)
                {
                    string path = file;
                    (string? url, bool isSuccess) = HttpListenerSingleton.Instance.RegisterFile(path);
                    if (isSuccess)
                    {
                        path = $"![]({url})";
                        insertContents += path;
                        if (files.Length > 1)
                        {
                            insertContents += Environment.NewLine;
                        }
                    }
                }
                inputText.Text = inputText.Text.Insert(inputText.CaretIndex, insertContents);
            }
        }
    }
}
