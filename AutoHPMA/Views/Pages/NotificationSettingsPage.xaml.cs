using AutoHPMA.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace AutoHPMA.Views.Pages
{
    public partial class NotificationSettingsPage : INavigableView<NotificationSettingsViewModel>
    {
        public NotificationSettingsViewModel ViewModel { get; }

        public NotificationSettingsPage(NotificationSettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
} 