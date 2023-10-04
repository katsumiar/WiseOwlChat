using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace WiseOwlChat
{
    /// <summary>
    /// MessageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MessageWindow : MetroWindow
    {
        public static bool MessageDialog(string message, string title = "Confirmation", bool isCancel = false)
        {
            MessageWindow messageWindow = new MessageWindow(message, title, isCancel);
            messageWindow.Owner = MainWindow.Instance;
            bool? dialogResult = messageWindow.ShowDialog();
            return dialogResult.HasValue ? dialogResult.Value : false;
        }

        public string Message { get; set; } = string.Empty;

        private bool clickedOkButton = false;

        public MessageWindow(string messsage, string title, bool showCancellation = false)
        {
            InitializeComponent();

            DataContext = this;

            Title = title;
            Message = messsage;
            clickedOkButton = !showCancellation;

            cancelButton.Visibility = showCancellation ? Visibility.Visible : Visibility.Collapsed;

            this.Closing += _Closing;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            clickedOkButton = false;

            Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            clickedOkButton = true;

            Close();
        }

        private void _Closing(object? sender, CancelEventArgs e)
        {
            DialogResult = clickedOkButton;
        }
    }
}
