using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WiseOwlChat
{
    public class ConversationInfo : INotifyPropertyChanged
    {
        private string? _title;
        public string? Title
        {
            get { return _title; } 
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public List<ConversationEntry> Conversation { get; set; } = new List<ConversationEntry>();
        public DateTime RegistrationDate { get; }
        public string? ModelType { get; set; }

        public ConversationInfo()
        {
            Title = "";
            RegistrationDate = DateTime.Now;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
