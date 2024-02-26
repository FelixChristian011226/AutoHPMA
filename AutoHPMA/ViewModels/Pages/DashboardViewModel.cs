// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Views;
using AutoHPMA.GameTask;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Diagnostics;
using Wpf.Ui.Controls;
using AutoHPMA.Views.Windows;
using Microsoft.Extensions.Logging;
using AutoHPMA.ViewModels.Windows;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        //[ObservableProperty]
        //private int _counter = 0;

        //[RelayCommand]
        //private void OnCounterIncrement()
        //{
        //    Counter++;
        //}

        private bool _taskDispatcherEnabled = false;

        [ObservableProperty] private Visibility _startButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _stopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
        private bool _startButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
        private bool _stopButtonEnabled = true;

        private MaskWindow? _maskWindow;
        //private readonly ILogger<HomePageViewModel> _logger = App.GetLogger<HomePageViewModel>();

        //private readonly TaskTriggerDispatcher _taskDispatcher;
        //private readonly MouseKeyMonitor _mouseKeyMonitor = new();

        //public DashboardViewModel(IConfigService configService, TaskTriggerDispatcher taskTriggerDispatcher)
        //{
        //    _taskDispatcher = taskTriggerDispatcher;
        //    Config = configService.Get();
        //    ReadGameInstallPath();
        //    WeakReferenceMessenger.Default.Register<PropertyChangedMessage<object>>(this, (sender, msg) =>
        //    {
        //        if (msg.PropertyName == "Close")
        //        {
        //            OnClosed();
        //        }
        //        else if (msg.PropertyName == "SwitchTriggerStatus")
        //        {
        //            if (_taskDispatcherEnabled)
        //            {
        //                OnStopTrigger();
        //            }
        //            else
        //            {
        //                _ = OnStartTriggerAsync();
        //            }
        //        }
        //    });
        //}


        private bool CanStartTrigger() => StartButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStartTrigger))]
        private async Task OnStartTriggerAsync()
        {
            var hWnd = SystemControl.FindMumuSimulatorHandle();
            if (hWnd == IntPtr.Zero)
            {
                if (hWnd == IntPtr.Zero)
                {
                    System.Windows.MessageBox.Show("未找到Mumu模拟器窗口，请先启动Mumu模拟器！");
                    return;
                }
            }
            if (!_taskDispatcherEnabled)
            {
                //_taskDispatcher.Start(hWnd, Config.CaptureMode.ToCaptureMode(), Config.TriggerInterval);
                _maskWindow = MaskWindow.Instance();
                _maskWindow.RefreshPosition(hWnd);
                //_mouseKeyMonitor.Subscribe(hWnd);
                _taskDispatcherEnabled = true;
                StartButtonVisibility = Visibility.Collapsed;
                StopButtonVisibility = Visibility.Visible;
            }

        }

        private bool CanStopTrigger() => StopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStopTrigger))]
        private void OnStopTrigger()
        {
            if (_taskDispatcherEnabled)
            {
                _maskWindow?.Hide();
                //_taskDispatcher.Stop();
                _taskDispatcherEnabled = false;
                //_mouseKeyMonitor.Unsubscribe();
                StartButtonVisibility = Visibility.Visible;
                StopButtonVisibility = Visibility.Collapsed;
            }
        }

        [RelayCommand]
        public void OnGoToWikiUrl()
        {
            Process.Start(new ProcessStartInfo("https://baidu.com") { UseShellExecute = true });
        }

        public void OnNavigatedFrom()
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedTo()
        {
            //throw new NotImplementedException();
        }
    }
}
