using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WiseOwlChat
{
    public class PluginInfo : INotifyPropertyChanged
    {
        private bool _enabled = false;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Name
        {
            get => _functionCalling?.FunctionName;
        }

        public string? Description
        {
            get => _functionCalling?.Description;
        }

        private IFunctionCalling? _functionCalling;
        public IFunctionCalling? FunctionCalling
        {
            get => _functionCalling;
            set
            {
                if (_functionCalling != value)
                {
                    _functionCalling = value;
                    OnPropertyChanged();
                    OnPropertyChanged(Name);
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
