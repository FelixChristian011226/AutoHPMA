// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using AutoHPMA.Views;
using AutoHPMA.GameTask;
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
using System.Net.WebSockets;
using System.Windows.Threading;
using AutoHPMA.Helpers;
using System.IO;
using System.Drawing;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {

        private bool _taskDispatcherEnabled = false;
        private DispatcherTimer _syncWindowTimer;
        private DispatcherTimer _captureTimer;

        [ObservableProperty]
        private bool _logWindowEnabled = true;
        [ObservableProperty]
        private int _logWindowLeft = 50;
        [ObservableProperty]
        private int _logWindowTop = 50;

        [ObservableProperty]
        private int _captureInterval = 500;

        [ObservableProperty] private Visibility _startButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _stopButtonVisibility = Visibility.Collapsed;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
        private bool _startButtonEnabled = true;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
        private bool _stopButtonEnabled = true;

        private LogWindow? _logWindow; // 添加一个 LogWindow 变量
                                       //private readonly ILogger<HomePageViewModel> _logger = App.GetLogger<HomePageViewModel>();

        public static event Action<Bitmap> ScreenshotUpdated;

        private static void OnScreenshotUpdated(Bitmap bmp)
        {
            ScreenshotUpdated?.Invoke(bmp);
        }

        //private readonly TaskTriggerDispatcher _taskDispatcher;
        //private readonly MouseKeyMonitor _mouseKeyMonitor = new();

        public DashboardViewModel()
        {
            InitializeCaptureTimer();
            InitializeSyncWindowTimer();
        }
        private void InitializeCaptureTimer()
        {
            _captureTimer = new DispatcherTimer();
            _captureTimer.Interval = TimeSpan.FromMilliseconds(500); // 每500毫秒截图一次，可根据需要调整
            _captureTimer.Tick += CaptureTimer_Tick;
        }
        public void InitializeSyncWindowTimer()
        {
            _syncWindowTimer = new DispatcherTimer();
            _syncWindowTimer.Interval = TimeSpan.FromMilliseconds(50); // 每50毫秒检查一次
            _syncWindowTimer.Tick += SyncWindowTimer_Tick;
        }
        private void CaptureTimer_Tick(object sender, EventArgs e)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle(); // 获取Mumu模拟器窗口句柄
            if (mumuHwnd != IntPtr.Zero)
            {
                // 截取窗口图像
                Bitmap bmp = ScreenCaptureHelper.CaptureWindow(mumuHwnd);
                OnScreenshotUpdated(bmp); // 发布截图更新事件

                // 确保目标文件夹存在
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Captures");
                Directory.CreateDirectory(folderPath);

                // 为新图像生成唯一的文件名
                //string fileName = $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fileName = "capture.png";

                // 保存图像文件
                ScreenCaptureHelper.SaveBitmapToFile(bmp, folderPath, fileName);
            }
        }
        private void SyncWindowTimer_Tick(object? sender, EventArgs e)
        {
            var mumuHwnd = SystemControl.FindMumuSimulatorHandle(); // 获取Mumu模拟器的句柄
            if (mumuHwnd != IntPtr.Zero)
            {
                if (NativeMethodsService.IsIconic(mumuHwnd)) // 如果Mumu模拟器最小化
                {
                    if (_logWindow.WindowState != WindowState.Minimized)
                        _logWindow.WindowState = WindowState.Minimized;
                }
                else
                {
                    if (_logWindow.WindowState != WindowState.Normal)
                        _logWindow.WindowState = WindowState.Normal; // 恢复日志窗口
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
                //_mouseKeyMonitor.Subscribe(hWnd);
                _taskDispatcherEnabled = true;
                StartButtonVisibility = Visibility.Collapsed;
                StopButtonVisibility = Visibility.Visible;

                // 根据窗口当前状态选择适当的SW命令
                int showWindowCommand;
                if (NativeMethodsService.IsZoomed(hWnd))
                {
                    // 如果窗口已经是最大化状态，则只需要将其置于前端
                    showWindowCommand = NativeMethodsService.SW_SHOW;
                }
                else if (NativeMethodsService.IsIconic(hWnd))
                {
                    // 如果窗口是最小化的，则恢复
                    showWindowCommand = NativeMethodsService.SW_RESTORE;
                }
                else
                {
                    // 如果窗口既不是最小化也不是最大化，则不做状态改变，只尝试置于前端
                    showWindowCommand = NativeMethodsService.SW_SHOW;
                }
                // 从最小化状态恢复窗口并试图将其置于前端。
                NativeMethodsService.ShowWindow(hWnd, showWindowCommand);
                NativeMethodsService.SetForegroundWindow(hWnd);

                // 隐藏WPF应用的主窗口。基于你的项目设置，可能需要调整获取主窗口的方式。
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.MainWindow.Hide();
                });

                // 在启动触发器时显示日志窗口
                _taskDispatcherEnabled = true;
                if (_logWindowEnabled)
                {
                    _logWindow = LogWindow.Instance();
                    _logWindow.ShowInTaskbar = false;
                    _logWindow.Owner = GetMumuSimulatorWindow(); // 将Mumu模拟器窗口设置为LogWindow的Owner
                    _logWindow.RefreshPosition(hWnd, _logWindowLeft, _logWindowTop);
                    _logWindow.AddLogMessage("INF","开始触发器"); // 添加日志消息
                    for(int i=0; i<100; i++) { _logWindow.AddLogMessage("INF","消息"+i); }
                }
                _captureTimer.Interval = TimeSpan.FromMilliseconds(_captureInterval);
                _captureTimer.Start();
                _syncWindowTimer.Start();
            }

        }

        private bool CanStopTrigger() => StopButtonEnabled;

        [RelayCommand(CanExecute = nameof(CanStopTrigger))]
        private void OnStopTrigger()
        {
            if (_taskDispatcherEnabled)
            {
                //_taskDispatcher.Stop();
                _taskDispatcherEnabled = false;
                //_mouseKeyMonitor.Unsubscribe();
                StartButtonVisibility = Visibility.Visible;
                StopButtonVisibility = Visibility.Collapsed;
                // 在停止触发器时隐藏日志窗口
                _logWindow?.AddLogMessage("INF", "停止触发器"); // 添加日志消息
                _logWindow.Close();
                _captureTimer.Stop();
                _syncWindowTimer.Stop();
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
