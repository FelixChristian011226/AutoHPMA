// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace AutoHPMA.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "AutoHPMA";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "启动",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 },
                TargetPageType = typeof(Views.Pages.DashboardPage)
            },
            new NavigationViewItem()
            {
                Content = "任务",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TaskListLtr24 },
                TargetPageType = typeof(Views.Pages.TaskPage)
            },
            new NavigationViewItem()
            {
                Content = "截屏",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Screenshot24 },
                TargetPageType = typeof(Views.Pages.ScreenshotPage)
            },
            new NavigationViewItem()
            {
                Content = "日志",
                Icon = new SymbolIcon {Symbol = SymbolRegular.DocumentText24},
                TargetPageType = typeof(Views.Pages.LogPage)
            },
            new NavigationViewItem()
            {
                Content = "测试",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DesktopEdit24 },
                TargetPageType = typeof(Views.Pages.TestPage)
            },
            new NavigationViewItem()
            {
                Content = "热键",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Keyboard24 },
                TargetPageType = typeof(Views.Pages.HotkeySettingsPage)
            },
            new NavigationViewItem()
            {
                Content = "通知",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Alert24 },
                TargetPageType = typeof(Views.Pages.NotificationSettingsPage)
            },
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
