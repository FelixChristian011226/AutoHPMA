using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace AutoHPMA.ViewModels.Windows
{
    public class LogMessage
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
        public LogLevel Level { get; set; }
    }

    public class LogViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<LogMessage> _logMessages = new();
        private bool _showDebugLogs;

        public ObservableCollection<LogMessage> LogMessages => _logMessages;
        
        public bool ShowDebugLogs
        {
            get => _showDebugLogs;
            set
            {
                if (_showDebugLogs != value)
                {
                    _showDebugLogs = value;
                    OnPropertyChanged();
                    FilterLogMessages();
                }
            }
        }

        public void AddLogMessage(LogLevel level, string category, string content)
        {
            if (level == LogLevel.Debug && !_showDebugLogs)
                return;

            var message = new LogMessage
            {
                Timestamp = DateTime.Now,
                Category = category,
                Content = content,
                Level = level
            };

            _logMessages.Add(message);
        }

        private void FilterLogMessages()
        {
            var filteredMessages = new ObservableCollection<LogMessage>();
            foreach (var log in _logMessages)
            {
                if (log.Level == LogLevel.Debug && !_showDebugLogs)
                    continue;

                filteredMessages.Add(log);
            }

            _logMessages.Clear();
            foreach (var msg in filteredMessages)
            {
                _logMessages.Add(msg);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 