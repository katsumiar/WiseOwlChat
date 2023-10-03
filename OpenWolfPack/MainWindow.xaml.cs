using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using Markdig;
using MaterialDesignThemes.Wpf;
using Microsoft.Xaml.Behaviors;

namespace OpenWolfPack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static MainWindow? Instance;

        private bool isCancel = true;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            this.Closing += _Closing;

            AppDataStore.Instance.Load();

            HttpListenerSingleton.Instance.RegisterFileFromMyApp("Plugins/ReadFileAPI/ReadFileAPI.cs");
            HttpListenerSingleton.Instance.RegisterFileFromMyApp("Plugins/FetchUrlAPI/FetchUrlAPI.cs");
            HttpListenerSingleton.Instance.RegisterFileFromMyApp("OpenWolfPack.md");

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (App.API_Key == null)
                {
                    MessageWindow.MessageDialog("The API key obtained from OpenAI must be set in the environment variable `OPENAI_API_KEY` or `OPENAI_KEY`.\nFor more information about the API, please refer to the OpenAI official website.");
                    ForcedTermination();
                    return;
                }

                HttpListenerSingleton.Instance.Initialize();

            }), DispatcherPriority.Loaded);

            this.Title = $"Open Wolf Pack - {AppDataStore.Instance.getFullAddress()}";
        }

        public void BringToFront()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }

                this.Activate();

                this.Topmost = true;
                this.Topmost = false;
            }));
        }

        public void ForcedTermination()
        {
            isCancel = false;
            Close();
        }

        private void _Closing(object? sender, CancelEventArgs e)
        {
            bool isExit;
            if (isCancel)
            {
                isExit = MessageWindow.MessageDialog("Are you sure you want to quit?", "Confirmation", true);
            }
            else
            {
                isExit = MessageWindow.MessageDialog("A problem has occurred and will be forced to close.");
            }

            if (!isExit)
            {
                // 終了をキャンセル
                e.Cancel = true;
            }
            else
            {
                try
                {
                    chatContainer.Save();
                    AppDataStore.Instance.Save();
                }
                catch (Exception ex)
                {
                    if (!MessageWindow.MessageDialog($"Failed to save file. Do you want to terminate the termination process?\n[ {ex.Message} ]", "Confirmation", true))
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                HttpListenerSingleton.Instance.Stop();
                LogWindow.LogClose();
            }
        }
    }
}
