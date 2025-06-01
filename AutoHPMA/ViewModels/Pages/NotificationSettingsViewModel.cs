using AutoHPMA.Config;
using AutoHPMA.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

        [RelayCommand]
        private void TestNotification()
        {
            ToastNotificationHelper.ShowToast(
                "测试通知",
                "这是一条测试通知，用于验证通知功能是否正常工作。"
            );
        }

        partial void OnNotificationEnabledChanged(bool value)
        {
            _settings.NotificationEnabled = value;
            _settings.Save();
        }

    }
} 