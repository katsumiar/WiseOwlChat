using Markdig;
using WiseOwlChat.Control;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WiseOwlChat
{
    public class MarkdownViewerViewModel : INotifyPropertyChanged
    {
        private string? _markdownText;
        public string? MarkdownText
        {
            get { return _markdownText; }
            set
            {
                if (_markdownText != value)
                {
                    _markdownText = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _role;
        public string? Role
        {
            get { return _role; }
            set
            {
                if (_role != value)
                {
                    _role = value;
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
}
