using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;

namespace WiseOwlChat.Control
{
    public class GroupViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> LanguageOptions { get; set; } = new ObservableCollection<string>
        {
            "English",
            "Español",
            "Français",
            "Deutsch",
            "简体中文",
            "繁體中文",
            "日本語",
            "Русский",
            "العربية",
            "हिन्दी",
            "Português"
        };

        public ICommand? DeleteCommand { get; set; }
        public ICommand? AddNewCommand { get; set; }

        public string? SelectedLanguage
        {
            get { return AppDataStore.Instance.SelectedLanguage; }
            set
            {
                if (AppDataStore.Instance.SelectedLanguage != value && value != null)
                {
                    AppDataStore.Instance.SelectedLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ChatContainerControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ChatContainerControl : UserControl
    {
        public GroupViewModel groupViewModel;

        private const string conversationsPath = "conversations.json";

        public ObservableCollection<ConversationInfo> ConversationInfos { get; set; }

        public static ChatContainerControl? Instance = null;

        public ChatContainerControl()
        {
            InitializeComponent();
            groupViewModel = new();
            groupViewModel.DeleteCommand = new RelayCommand((c) => DeleteItem(c as ConversationInfo));
            groupViewModel.AddNewCommand = new RelayCommand(param => addNew());
            DataContext = groupViewModel;
            ConversationInfos = new ObservableCollection<ConversationInfo>();
            Load();
            collections.ItemsSource = ConversationInfos;
            chat.ClearMessage(ConversationInfos);

            Instance = this;
        }

        public void DeleteItem(ConversationInfo? item)
        {
            if (item == null) return;

            ConversationInfos.Remove(item);
        }

        public static void SetRequest(string query)
        {
            if (App.API_Key != null && Instance != null && MessageInputControl.Instance != null)
            {
                MainWindow.Instance?.BringToFront();

                Instance.addNew(() =>
                {
                    MessageInputControl.Instance.SetRequest(query);
                });
            }
        }

        public static void SetUrlRequest(string query)
        {
            if (App.API_Key != null && Instance != null && MessageInputControl.Instance != null)
            {
                MainWindow.Instance?.BringToFront();

                Instance.addNew(() =>
                {
                    MessageInputControl.Instance.SetUrlRequest(query);
                });
            }
        }

        private void addNew(Action? action = null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!chat.IsBusy())
                {
                    chat.ClearMessage(ConversationInfos);
                    collections.SelectedItem = null;
                    action?.Invoke();
                }
            }));
        }

        private void collections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 一時的にSelectionChangedイベントを無効にする
            collections.SelectionChanged -= collections_SelectionChanged;

            var selectedItem = collections.SelectedItem as ConversationInfo;
            
            if (!chat.IsBusy())
            {
                if (selectedItem != null)
                {
                    chat.SetupMessage(selectedItem);
                }
            }
            else
            {
                if (e.RemovedItems.Count > 0)
                {
                    collections.SelectedItem = e.RemovedItems[0];
                }
            }

            // SelectionChangedイベントを再び有効にする
            collections.SelectionChanged += collections_SelectionChanged;
        }

        public class BooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public void Save()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, conversationsPath);
            string json = JsonSerializer.Serialize(ConversationInfos);
            if (File.Exists(path))
            {
                File.Copy(path, path + ".backup", true);
            }
            File.WriteAllText(path, json);
        }

        public void Load()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, conversationsPath);
            if (File.Exists(path))
            {
                string jsonFromFile = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<ObservableCollection<ConversationInfo>>(jsonFromFile);
                if (data != null)
                {
                    ConversationInfos = data;
                }
            }
        }
    }
}
