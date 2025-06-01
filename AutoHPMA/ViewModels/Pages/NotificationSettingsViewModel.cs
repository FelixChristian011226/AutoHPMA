using AutoHPMA.Config;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class NotificationSettingsViewModel : ObservableObject
    {
        private readonly AppSettings _settings;

        [ObservableProperty]
        private bool _notificationEnabled = true;

        public NotificationSettingsViewModel(AppSettings settings)
        {
            _settings = settings;
            NotificationEnabled = _settings.NotificationEnabled;
        }

        partial void OnNotificationEnabledChanged(bool value)
        {
            _settings.NotificationEnabled = value;
            _settings.Save();
        }
    }
} 