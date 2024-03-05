// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Views;
using AutoHPMA.GameTask;
using AutoHPMA.Config;
using AutoHPMA.Views.Windows;
using AutoHPMA.ViewModels.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Diagnostics;
using Wpf.Ui.Controls;
using Microsoft.Extensions.Logging;
using System.Windows.Interop;
using System.Windows.Media;

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
        private LogWindow _logWindow; // 添加一个 LogWindow 变量
                                      //private readonly ILogger<HomePageViewModel> _logger = App.GetLogger<HomePageViewModel>();

        //private readonly TaskTriggerDispatcher _taskDispatcher;
        //private readonly MouseKeyMonitor _mouseKeyMonitor = new();

        private LogWindowConfig _logWindowConfig;

        public DashboardViewModel()
        {
            // 初始化配置信息
            _logWindowConfig = new LogWindowConfig();
        }

        public LogWindowConfig LogWindowConfig
        {
            get => _logWindowConfig;
            set
            {
                _logWindowConfig = value;
                OnPropertyChanged();
            }
        }
        public bool LogWindowEnabled
        {
            get => _logWindowConfig.LogWindowEnabled;
            set
            {
                if (_logWindowConfig.LogWindowEnabled != value)
                {
                    _logWindowConfig.LogWindowEnabled = value;
                    OnPropertyChanged(nameof(LogWindowEnabled));
                }
            }
        }

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
                // 在启动触发器时显示日志窗口
                if(LogWindowConfig.LogWindowEnabled)
                {
                    _logWindow = LogWindow.Instance();
                    _logWindow.Owner = GetMumuSimulatorWindow(); // 将Mumu模拟器窗口设置为LogWindow的Owner
                    _logWindow.RefreshPosition(hWnd);
                    _logWindow.AddLogMessage("开始触发器"); // 添加日志消息
                    for(int i=0; i<100; i++) { _logWindow.AddLogMessage("消息"+i); }
                }
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
                // 在停止触发器时隐藏日志窗口
                _logWindow?.AddLogMessage("停止触发器"); // 添加日志消息
                //_logWindow?.Hide();
                _logWindow.Close();
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

        private Window GetMumuSimulatorWindow()
        {
            var hWnd = SystemControl.FindMumuSimulatorHandle();
            if (hWnd != IntPtr.Zero)
            {
                var hwndSource = HwndSource.FromHwnd(hWnd);
                if (hwndSource != null)
                {
                    var rootVisual = hwndSource.RootVisual;
                    if (rootVisual != null)
                    {
                        var parent = VisualTreeHelper.GetParent(rootVisual);
                        if (parent is Window window)
                        {
                            return window;
                        }
                    }
                }
            }
            return null;
        }

    }
}
